using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelConsole.Graph
{

   /// <summary>
   /// Options controlling the orthogonal router.
   /// </summary>
   public sealed class RouterOptions
   {
      /// <summary>Side length of each A* grid cell in pixels.</summary>
      public double GridSize { get; set; } = 16;

      /// <summary>Clearance kept around every obstacle, in pixels.</summary>
      public double ObstacleMargin { get; set; } = 14;

      /// <summary>How far the route travels straight out from an anchor before
      /// the first turn, in pixels.</summary>
      public double StubLength { get; set; } = 32;
   }

   /// <summary>
   /// Routes an orthogonal connector between two points so it avoids the
   /// interiors of a set of obstacle rectangles. Uses A* on a coarse grid
   /// with a Manhattan heuristic. Pure, deterministic, and unit-testable.
   /// </summary>
   public static class OrthogonalRouter
   {

      /// <summary>
      /// Compute an axis-aligned polyline from <paramref name="start"/> to
      /// <paramref name="end"/> that does not cross any obstacle interior.
      /// </summary>
      /// <param name="start">anchor point (may sit on an obstacle boundary)
      /// </param>
      /// <param name="end">anchor point (may sit on an obstacle boundary)
      /// </param>
      /// <param name="obstacles">rectangles the route must avoid (inflated by
      /// <see cref="RouterOptions.ObstacleMargin"/>)</param>
      /// <param name="bounds">bounds of the routing region (grid is clipped to
      /// this)</param>
      /// <param name="options">router options</param>
      /// <param name="thinObstacles">rectangles the route must avoid that are
      /// used as-is (not inflated) - e.g. already-routed connector segments
      /// </param>
      /// <returns>an axis-aligned polyline whose first point is
      /// <paramref name="start"/> and last point is <paramref name="end"/>
      /// </returns>
      public static IReadOnlyList<Point2> Route(
         Point2 start, Point2 end,
         IReadOnlyList<Rect2> obstacles, Rect2 bounds, RouterOptions options,
         IReadOnlyList<Rect2> thinObstacles = null)
      {
         if (start.Equals(end))
         {
            return new List<Point2> { start, end };
         }

         double grid = options != null && options.GridSize > 0 ? options.GridSize : 16;
         double margin = options != null ? options.ObstacleMargin : 0;
         double stub = options != null && options.StubLength > 0 ? options.StubLength : 0;

         var inflated = obstacles == null
            ? new List<Rect2>()
            : obstacles.Select(o => o.Inflate(margin)).ToList();
         var thin = thinObstacles ?? new List<Rect2>();

         // 1. Prefer the direct orthogonal routes (they produce the cleanest
         //    line and cover the straight-line case). The check uses the
         //    un-inflated obstacles so a route leaving an anchor that sits on
         //    a table edge is not rejected for crossing that table's own
         //    inflated margin.
         var raw = obstacles ?? new List<Rect2>();
         var hv = new List<Point2> { start, new Point2(end.X, start.Y), end };
         if (IsClear(hv, raw, thin))
         {
            return Simplify(hv);
         }
         var vh = new List<Point2> { start, new Point2(start.X, end.Y), end };
         if (IsClear(vh, raw, thin))
         {
            return Simplify(vh);
         }

         // 2. Move the anchors out of the obstacles so the grid path never
         //    starts inside a blocked cell. The stub's parallel coordinate is
         //    snapped to a grid center so the stitching segments (anchor ->
         //    stub -> first grid point) stay axis-aligned.
         Point2 startDir = OutwardDirection(start, obstacles);
         Point2 endDir = OutwardDirection(end, obstacles);
         Point2 startStub = SnapStub(start, startDir, stub, bounds, grid);
         Point2 endStub = SnapStub(end, endDir, stub, bounds, grid);

         // 3. Grid A* between the stub points.
         List<Point2> gridPath = AStar(startStub, endStub, inflated, thin, bounds, grid);
         if (gridPath == null)
         {
            // Unreachable on the grid (e.g. a full-height wall): fall back to
            // an orthogonal Z route with stubs.
            var fallback = new List<Point2>
            {
               start,
               startStub,
               new Point2(endStub.X, startStub.Y),
               endStub,
               end
            };
            return Simplify(fallback);
         }

         // 4. Stitch the anchors onto the grid path and simplify.
         var points = new List<Point2>(gridPath.Count + 4);
         points.Add(start);
         points.Add(startStub);
         points.AddRange(gridPath);
         points.Add(endStub);
         points.Add(end);
         return Simplify(points);
      }

      /// <summary>
      /// True when no segment of the polyline crosses the interior of any
      /// obstacle (inflated or thin).
      /// </summary>
      private static bool IsClear(
         IReadOnlyList<Point2> points,
         IReadOnlyList<Rect2> obstacles,
         IReadOnlyList<Rect2> thin)
      {
         for (int i = 0; i < points.Count - 1; i++)
         {
            foreach (var o in obstacles)
            {
               if (Rect2.SegmentCrossesInterior(points[i], points[i + 1], o))
               {
                  return false;
               }
            }
            foreach (var o in thin)
            {
               if (Rect2.SegmentCrossesInterior(points[i], points[i + 1], o))
               {
                  return false;
               }
            }
         }
         return true;
      }

      /// <summary>
      /// Drop points that lie on the straight run between their neighbours,
      /// leaving only the turns and the endpoints.
      /// </summary>
      private static List<Point2> Simplify(List<Point2> points)
      {
         if (points.Count <= 2)
         {
            return points;
         }

         var result = new List<Point2> { points[0] };
         for (int i = 1; i < points.Count - 1; i++)
         {
            if (IsCollinear(result[result.Count - 1], points[i], points[i + 1]))
            {
               continue;
            }
            result.Add(points[i]);
         }
         result.Add(points[points.Count - 1]);
         return result;
      }

      /// <summary>
      /// Axis-aligned collinearity: all three points share an X or a Y.
      /// </summary>
      private static bool IsCollinear(Point2 a, Point2 b, Point2 c)
      {
         return (a.X == b.X && b.X == c.X) || (a.Y == b.Y && b.Y == c.Y);
      }

      /// <summary>
      /// The axis direction the route must leave the anchor towards, found by
      /// the side of the nearest obstacle the anchor sits on.
      /// </summary>
      private static Point2 OutwardDirection(
         Point2 point, IReadOnlyList<Rect2> obstacles)
      {
         const double eps = 0.001;
         Rect2 nearest = default(Rect2);
         bool found = false;
         double bestDistance = double.MaxValue;

         if (obstacles != null)
         {
            foreach (var o in obstacles)
            {
               // anchor exactly on an edge -> leave along that edge's normal
               if (Math.Abs(point.X - o.Right) < eps)
               {
                  return new Point2(1, 0);
               }
               if (Math.Abs(point.X - o.Left) < eps)
               {
                  return new Point2(-1, 0);
               }
               if (Math.Abs(point.Y - o.Top) < eps)
               {
                  return new Point2(0, -1);
               }
               if (Math.Abs(point.Y - o.Bottom) < eps)
               {
                  return new Point2(0, 1);
               }

               // nearest-point distance for anchors not exactly on an edge
               double d = DistanceSquaredToRect(point, o);
               if (d < bestDistance)
               {
                  bestDistance = d;
                  nearest = o;
                  found = true;
               }
            }
         }

         if (found)
         {
            // direction away from the nearest point on the rect
            double closestX = Clamp(point.X, nearest.Left, nearest.Right);
            double closestY = Clamp(point.Y, nearest.Top, nearest.Bottom);
            double dx = point.X - closestX;
            double dy = point.Y - closestY;
            if (Math.Abs(dx) >= Math.Abs(dy) && dx != 0)
            {
               return new Point2(Math.Sign(dx), 0);
            }
            if (dy != 0)
            {
               return new Point2(0, Math.Sign(dy));
            }
         }

         return new Point2(1, 0);
      }

      /// <summary>
      /// Squared distance from a point to the closest point on a rectangle.
      /// </summary>
      private static double DistanceSquaredToRect(Point2 p, Rect2 r)
      {
         double dx = Math.Max(r.Left - p.X, Math.Max(0, p.X - r.Right));
         double dy = Math.Max(r.Top - p.Y, Math.Max(0, p.Y - r.Bottom));
         return dx * dx + dy * dy;
      }

      private static double Clamp(double v, double min, double max)
      {
         return v < min ? min : (v > max ? max : v);
      }

      private static Point2 Clamp(Point2 p, Rect2 bounds)
      {
         return new Point2(
            Clamp(p.X, bounds.Left, bounds.Right),
            Clamp(p.Y, bounds.Top, bounds.Bottom));
      }

      /// <summary>
      /// Anchor the stub along the outward direction. The coordinate parallel
      /// to the outward direction is snapped to the center of the grid cell
      /// A* will start from, so the stub point and the first grid point share
      /// that coordinate exactly (the anchor-to-stub segment stays
      /// perpendicular-aligned and the stub-to-first-point segment stays
      /// parallel-aligned).
      /// </summary>
      private static Point2 SnapStub(
         Point2 anchor, Point2 dir, double stub, Rect2 bounds, double grid)
      {
         int cols = Math.Max(1, (int)Math.Ceiling(bounds.Width / grid));
         int rows = Math.Max(1, (int)Math.Ceiling(bounds.Height / grid));

         if (dir.X != 0)
         {
            double rawX = anchor.X + dir.X * stub;
            int col = (int)Clamp(Math.Floor((rawX - bounds.X) / grid), 0, cols - 1);
            return new Point2(bounds.X + (col + 0.5) * grid, anchor.Y);
         }

         double rawY = anchor.Y + dir.Y * stub;
         int row = (int)Clamp(Math.Floor((rawY - bounds.Y) / grid), 0, rows - 1);
         return new Point2(anchor.X, bounds.Y + (row + 0.5) * grid);
      }

      /// <summary>
      /// A* between two points on a coarse grid. Returns the cell-center
      /// polyline, or null when the grid is unreachable.
      /// </summary>
      private static List<Point2> AStar(
         Point2 start, Point2 end, IReadOnlyList<Rect2> inflated,
         IReadOnlyList<Rect2> thin, Rect2 bounds, double grid)
      {
         int cols = Math.Max(1, (int)Math.Ceiling(bounds.Width / grid));
         int rows = Math.Max(1, (int)Math.Ceiling(bounds.Height / grid));

         int sc = ToCol(start.X, bounds.Left, grid, cols);
         int sr = ToRow(start.Y, bounds.Top, grid, rows);
         int ec = ToCol(end.X, bounds.Left, grid, cols);
         int er = ToRow(end.Y, bounds.Top, grid, rows);

         var gScore = new Dictionary<(int, int), double>();
         var cameFrom = new Dictionary<(int, int), (int, int)>();
         long seq = 0;

         var open = new PriorityQueue<(int, int), (double f, double g, long seq)>(
            Comparer<(double f, double g, long seq)>.Create((a, b) =>
            {
               int c = a.f.CompareTo(b.f);
               if (c != 0) return c;
               c = a.g.CompareTo(b.g);
               if (c != 0) return c;
               return a.seq.CompareTo(b.seq);
            }));

         gScore[(sc, sr)] = 0;
         open.Enqueue((sc, sr), (Heuristic(sc, sr, ec, er), 0, seq++));

         // deterministic neighbour order
         var neighbours = new (int dc, int dr)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };

         while (open.Count > 0)
         {
            (int col, int row) = open.Dequeue();
            if (col == ec && row == er)
            {
               return Reconstruct(sc, sr, ec, er, cameFrom, bounds, grid);
            }

            double g = gScore[(col, row)];
            foreach (var (dc, dr) in neighbours)
            {
               int ncol = col + dc;
               int nrow = row + dr;
               if (ncol < 0 || ncol >= cols || nrow < 0 || nrow >= rows)
               {
                  continue;
               }

               bool startEnd = (ncol == sc && nrow == sr) || (ncol == ec && nrow == er);
               if (!startEnd && IsBlocked(ncol, nrow, inflated, thin, bounds, grid))
               {
                  continue;
               }

               // Don't step across an obstacle between two cell centers. This
               // stops a single grid step from jumping over a thin obstacle
               // (e.g. an already-routed connector) whose centerline lies
               // between the two cell centers.
               if (!startEnd && SegmentCrossesAny(
                  CellCenter(col, row, bounds, grid),
                  CellCenter(ncol, nrow, bounds, grid), inflated, thin))
               {
                  continue;
               }

               double ng = g + 1.0;
               if (!gScore.TryGetValue((ncol, nrow), out double old) || ng < old)
               {
                  gScore[(ncol, nrow)] = ng;
                  cameFrom[(ncol, nrow)] = (col, row);
                  open.Enqueue((ncol, nrow), (ng + Heuristic(ncol, nrow, ec, er), ng, seq++));
               }
            }
         }

         return null;
      }

      /// <summary>
      /// Rebuild the cell-center polyline by walking the came-from links.
      /// </summary>
      private static List<Point2> Reconstruct(
         int sc, int sr, int ec, int er,
         IReadOnlyDictionary<(int, int), (int, int)> cameFrom,
         Rect2 bounds, double grid)
      {
         var path = new List<Point2>();
         (int col, int row) = (ec, er);
         while (true)
         {
            path.Add(CellCenter(col, row, bounds, grid));
            if (col == sc && row == sr)
            {
               break;
            }
            (col, row) = cameFrom[(col, row)];
         }
         path.Reverse();
         return path;
      }

      private static int ToCol(double x, double left, double grid, int cols)
      {
         return (int)Clamp(Math.Floor((x - left) / grid), 0, cols - 1);
      }

      private static int ToRow(double y, double top, double grid, int rows)
      {
         return (int)Clamp(Math.Floor((y - top) / grid), 0, rows - 1);
      }

      private static Point2 CellCenter(int col, int row, Rect2 bounds, double grid)
      {
         return new Point2(
            bounds.X + (col + 0.5) * grid,
            bounds.Y + (row + 0.5) * grid);
      }

      /// <summary>
      /// A cell is blocked when its center sits strictly inside an inflated
      /// or thin obstacle.
      /// </summary>
      private static bool IsBlocked(
         int col, int row, IReadOnlyList<Rect2> inflated,
         IReadOnlyList<Rect2> thin, Rect2 bounds, double grid)
      {
         Point2 center = CellCenter(col, row, bounds, grid);
         foreach (var o in inflated)
         {
            if (o.ContainsStrict(center))
            {
               return true;
            }
         }
         foreach (var o in thin)
         {
            if (o.ContainsStrict(center))
            {
               return true;
            }
         }
         return false;
      }

      /// <summary>
      /// True when the segment a-b crosses the interior of any inflated or
      /// thin obstacle.
      /// </summary>
      private static bool SegmentCrossesAny(
         Point2 a, Point2 b,
         IReadOnlyList<Rect2> inflated, IReadOnlyList<Rect2> thin)
      {
         foreach (var o in inflated)
         {
            if (Rect2.SegmentCrossesInterior(a, b, o))
            {
               return true;
            }
         }
         foreach (var o in thin)
         {
            if (Rect2.SegmentCrossesInterior(a, b, o))
            {
               return true;
            }
         }
         return false;
      }

      private static double Heuristic(int col, int row, int ec, int er)
      {
         return Math.Abs(col - ec) + Math.Abs(row - er);
      }

   }

}
