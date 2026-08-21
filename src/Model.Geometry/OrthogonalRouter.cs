using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelConsole.Geometry
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

      /// <summary>Safety cap on A* cell expansions. When exceeded, the route
      /// falls back to the orthogonal Z path instead of exploring a huge grid
      /// (e.g. a table dragged far away grows the canvas quadratically -
      /// backlog 013).</summary>
      public long MaxExpansions { get; set; } = 100000;

      /// <summary>How much longer an already-routed-connector-avoiding detour
      /// may be than the crossing route before the crossing route wins
      /// (backlog 033). 1.0 = always take the shorter route (may cross other
      /// connectors); larger = keep the crossing-free route unless avoiding
      /// it is absurdly expensive. Table interiors are never crossed either
      /// way.</summary>
      public double CrossingTolerance { get; set; } = 1.5;
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
         long maxExpansions = options != null && options.MaxExpansions > 0
            ? options.MaxExpansions : 100000;

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
         Point2 startStub = SnapStub(start, startDir, stub, bounds, grid, inflated, thin, raw);
         Point2 endStub = SnapStub(end, endDir, stub, bounds, grid, inflated, thin, raw);

         // 3. Grid A* between the stub points. When the thin obstacles
         //    (already-routed connectors) form a barrier that makes the grid
         //    unreachable, retry without them: the hard invariant is "no
         //    connector crosses a table interior", so crossing a connector is
         //    acceptable when the alternative is crossing a table. The table
         //    walkability grid is built once and shared by both runs — the
         //    thin connectors are stamped onto a per-run copy.
         var walk = new WalkGrid(
            Math.Max(1, (int)Math.Ceiling(bounds.Width / grid)),
            Math.Max(1, (int)Math.Ceiling(bounds.Height / grid)));
         walk.Stamp(inflated, bounds, grid);

         List<Point2> gridPath = AStar(
            startStub, endStub, walk, thin, bounds, grid, maxExpansions);
         if (gridPath == null)
         {
            gridPath = AStar(
               startStub, endStub, walk, null, bounds, grid, maxExpansions);
         }
         if (gridPath == null)
         {
            // Genuinely unreachable (e.g. a full-height wall): best-effort Z
            // that avoids tables when possible.
            return ZFallback(start, startStub, endStub, end, raw);
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
      /// Route between two points, taking the shorter of the wall-avoiding
      /// route and the wall-ignoring route when the avoiding detour costs
      /// more than <see cref="RouterOptions.CrossingTolerance"/>× (backlog
      /// 033). Both candidates avoid every table interior; the wall-ignoring
      /// candidate may cross an already-routed connector. The returned
      /// polyline is what the caller feeds back as a thin obstacle, so later
      /// edges see whichever route actually won.
      /// </summary>
      /// <param name="start">anchor point (may sit on an obstacle boundary)
      /// </param>
      /// <param name="end">anchor point (may sit on an obstacle boundary)
      /// </param>
      /// <param name="obstacles">table rectangles the route must avoid
      /// (inflated by <see cref="RouterOptions.ObstacleMargin"/>)</param>
      /// <param name="bounds">bounds of the routing region</param>
      /// <param name="options">router options (the tolerance comes from
      /// <see cref="RouterOptions.CrossingTolerance"/>)</param>
      /// <param name="thinObstacles">already-routed connector segments that
      /// the route avoids when cheap</param>
      /// <returns>the chosen axis-aligned polyline from <paramref
      /// name="start"/> to <paramref name="end"/></returns>
      public static IReadOnlyList<Point2> RouteBest(
         Point2 start, Point2 end,
         IReadOnlyList<Rect2> obstacles, Rect2 bounds, RouterOptions options,
         IReadOnlyList<Rect2> thinObstacles)
      {
         double factor = options != null && options.CrossingTolerance > 0
            ? options.CrossingTolerance : 1.5;
         var withWalls = Route(start, end, obstacles, bounds, options, thinObstacles);
         var withoutWalls = Route(start, end, obstacles, bounds, options, null);
         return PolylineLength(withoutWalls) * factor < PolylineLength(withWalls)
            ? withoutWalls
            : withWalls;
      }

      /// <summary>
      /// Total length of an axis-aligned polyline (Manhattan distance summed
      /// over its segments).
      /// </summary>
      public static double PolylineLength(IReadOnlyList<Point2> pts)
      {
         double total = 0;
         for (int i = 0; i < pts.Count - 1; i++)
         {
            total += Math.Abs(pts[i + 1].X - pts[i].X) +
                     Math.Abs(pts[i + 1].Y - pts[i].Y);
         }
         return total;
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
      /// parallel-aligned). The stub is moved outward until its cell is not
      /// blocked and the anchor-to-stub segment does not cross a table
      /// interior, so the route never starts inside a blocked cell or crosses
      /// a neighbour table on its way out.
      /// </summary>
      private static Point2 SnapStub(
         Point2 anchor, Point2 dir, double stub, Rect2 bounds, double grid,
         IReadOnlyList<Rect2> inflated, IReadOnlyList<Rect2> thin,
         IReadOnlyList<Rect2> raw)
      {
         int cols = Math.Max(1, (int)Math.Ceiling(bounds.Width / grid));
         int rows = Math.Max(1, (int)Math.Ceiling(bounds.Height / grid));

         double dist = stub;
         double maxDist = Math.Max(bounds.Width, bounds.Height);
         while (dist <= maxDist)
         {
            Point2 candidate = SnapAt(anchor, dir, dist, bounds, grid, cols, rows);
            int ccol = ToCol(candidate.X, bounds.Left, grid, cols);
            int crow = ToRow(candidate.Y, bounds.Top, grid, rows);
            if (!IsBlocked(ccol, crow, inflated, thin, bounds, grid) &&
                !SegmentCrossesAny(anchor, candidate, raw, new List<Rect2>()))
            {
               return candidate;
            }
            dist += grid;
         }

         // Best effort: the original snap (e.g. the outward direction points
         // into a neighbour table, so no outward stub can avoid it).
         return SnapAt(anchor, dir, stub, bounds, grid, cols, rows);
      }

      /// <summary>
      /// The stub point at a given distance along the outward direction,
      /// snapped to the center of the grid cell it falls in.
      /// </summary>
      private static Point2 SnapAt(
         Point2 anchor, Point2 dir, double dist, Rect2 bounds, double grid,
         int cols, int rows)
      {
         if (dir.X != 0)
         {
            double rawX = anchor.X + dir.X * dist;
            int col = (int)Clamp(Math.Floor((rawX - bounds.X) / grid), 0, cols - 1);
            return new Point2(bounds.X + (col + 0.5) * grid, anchor.Y);
         }

         double rawY = anchor.Y + dir.Y * dist;
         int row = (int)Clamp(Math.Floor((rawY - bounds.Y) / grid), 0, rows - 1);
         return new Point2(anchor.X, bounds.Y + (row + 0.5) * grid);
      }

      /// <summary>
      /// Best-effort orthogonal Z route used when the grid is genuinely
      /// unreachable (e.g. a full-height wall). Tries both the HV and VH
      /// variants and returns the first that does not cross a table interior;
      /// when neither is clear, returns the variant with fewer crossings.
      /// </summary>
      private static List<Point2> ZFallback(
         Point2 start, Point2 startStub, Point2 endStub, Point2 end,
         IReadOnlyList<Rect2> raw)
      {
         var hv = new List<Point2>
         {
            start, startStub, new Point2(endStub.X, startStub.Y), endStub, end
         };
         if (IsClear(hv, raw, new List<Rect2>()))
         {
            return Simplify(hv);
         }
         var vh = new List<Point2>
         {
            start, startStub, new Point2(startStub.X, endStub.Y), endStub, end
         };
         if (IsClear(vh, raw, new List<Rect2>()))
         {
            return Simplify(vh);
         }
         return Simplify(CountCrossings(hv, raw) <= CountCrossings(vh, raw) ? hv : vh);
      }

      /// <summary>
      /// Number of obstacle interiors crossed by the polyline's segments.
      /// </summary>
      private static int CountCrossings(
         IReadOnlyList<Point2> points, IReadOnlyList<Rect2> obstacles)
      {
         int count = 0;
         for (int i = 0; i < points.Count - 1; i++)
         {
            foreach (var o in obstacles)
            {
               if (Rect2.SegmentCrossesInterior(points[i], points[i + 1], o))
               {
                  count++;
               }
            }
         }
         return count;
      }

      /// <summary>
      /// Precomputed walkability for one A* run: a flat grid of blocked cells
      /// (the cell centre is strictly inside an inflated/thin obstacle) and a
      /// flat grid of blocked directed steps (the segment between two adjacent
      /// cell centres crosses an obstacle interior). A* reads these O(1)
      /// instead of scanning the obstacle list on every expansion — the hot
      /// path that made a large-model compose take minutes, since the list
      /// grows to ~800 rects (every routed connector segment) by the end of a
      /// routing pass.
      /// </summary>
      private sealed class WalkGrid
      {
         private readonly int _cols;
         private readonly int _rows;
         private readonly bool[] _blocked;
         private readonly bool[] _stepX; // step (c, r) -> (c + 1, r) blocked
         private readonly bool[] _stepY; // step (c, r) -> (c, r + 1) blocked

         public WalkGrid(int cols, int rows)
         {
            _cols = cols;
            _rows = rows;
            _blocked = new bool[cols * rows];
            _stepX = new bool[cols * rows];
            _stepY = new bool[cols * rows];
         }

         public bool IsBlocked(int col, int row)
         {
            return _blocked[row * _cols + col];
         }

         public bool IsStepXBlocked(int col, int row)
         {
            return _stepX[row * _cols + col];
         }

         public bool IsStepYBlocked(int col, int row)
         {
            return _stepY[row * _cols + col];
         }

         /// <summary>
         /// A copy of the walkability grid, so a caller can share one base
         /// (the static tables) and stamp the per-route thin connectors onto
         /// a private copy without rebuilding the base each time.
         /// </summary>
         public WalkGrid Clone()
         {
            var copy = new WalkGrid(_cols, _rows);
            Array.Copy(_blocked, copy._blocked, _blocked.Length);
            Array.Copy(_stepX, copy._stepX, _stepX.Length);
            Array.Copy(_stepY, copy._stepY, _stepY.Length);
            return copy;
         }

         /// <summary>
         /// Stamp every rect into the grid. The predicates are exactly the
         /// tests they replace — <see cref="Rect2.ContainsStrict"/> on the cell
         /// centre and <see cref="Rect2.SegmentCrossesInterior"/> between
         /// adjacent centres — so the walkable set is identical to the old
         /// per-expansion scans (the win is skipping the rects that cannot
         /// possibly match).
         /// </summary>
         public void Stamp(IReadOnlyList<Rect2> rects, Rect2 bounds, double grid)
         {
            for (int i = 0; i < rects.Count; i++)
            {
               Stamp(rects[i], bounds, grid);
            }
         }

         private void Stamp(Rect2 rect, Rect2 bounds, double grid)
         {
            // Enumerate the cells whose centre lies within the rect expanded
            // by one cell. Every adjacent pair whose centre-segment crosses
            // the rect has at least one endpoint cell in this range (the
            // crossing midpoint's cell centre is within half a cell of the
            // rect), so stamping both directions from each enumerated cell
            // covers every crossing pair exactly once.
            int c0 = ToCol(rect.Left - grid, bounds.Left, grid, _cols);
            int c1 = ToCol(rect.Right + grid, bounds.Left, grid, _cols);
            int r0 = ToRow(rect.Top - grid, bounds.Top, grid, _rows);
            int r1 = ToRow(rect.Bottom + grid, bounds.Top, grid, _rows);

            for (int r = r0; r <= r1; r++)
            {
               for (int c = c0; c <= c1; c++)
               {
                  Point2 center = CellCenter(c, r, bounds, grid);
                  if (rect.ContainsStrict(center))
                  {
                     _blocked[r * _cols + c] = true;
                  }

                  // Directed steps, indexed by the "from" cell — a -x move
                  // from (col, row) reads the step stored at (col - 1, row).
                  if (c + 1 < _cols &&
                      Rect2.SegmentCrossesInterior(
                         center, CellCenter(c + 1, r, bounds, grid), rect))
                  {
                     _stepX[r * _cols + c] = true;
                  }
                  if (c - 1 >= 0 &&
                      Rect2.SegmentCrossesInterior(
                         center, CellCenter(c - 1, r, bounds, grid), rect))
                  {
                     _stepX[r * _cols + (c - 1)] = true;
                  }
                  if (r + 1 < _rows &&
                      Rect2.SegmentCrossesInterior(
                         center, CellCenter(c, r + 1, bounds, grid), rect))
                  {
                     _stepY[r * _cols + c] = true;
                  }
                  if (r - 1 >= 0 &&
                      Rect2.SegmentCrossesInterior(
                         center, CellCenter(c, r - 1, bounds, grid), rect))
                  {
                     _stepY[(r - 1) * _cols + c] = true;
                  }
               }
            }
         }
      }

      /// <summary>
      /// A* between two points on a coarse grid. Returns the cell-center
      /// polyline, or null when the grid is unreachable. The caller's base
      /// walkability grid (the static tables) is stamped with the route's
      /// thin connectors onto a private copy, so the A* hot loop reads arrays
      /// instead of scanning the obstacle list per expansion.
      /// </summary>
      private static List<Point2> AStar(
         Point2 start, Point2 end, WalkGrid baseWalk, IReadOnlyList<Rect2> thin,
         Rect2 bounds, double grid, long maxExpansions)
      {
         int cols = Math.Max(1, (int)Math.Ceiling(bounds.Width / grid));
         int rows = Math.Max(1, (int)Math.Ceiling(bounds.Height / grid));

         int sc = ToCol(start.X, bounds.Left, grid, cols);
         int sr = ToRow(start.Y, bounds.Top, grid, rows);
         int ec = ToCol(end.X, bounds.Left, grid, cols);
         int er = ToRow(end.Y, bounds.Top, grid, rows);

         var walk = baseWalk.Clone();
         if (thin != null && thin.Count > 0)
         {
            walk.Stamp(thin, bounds, grid);
         }

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

         long expansions = 0;
         while (open.Count > 0)
         {
            // Safety cap: a huge grid (e.g. a table dragged far away) must not
            // stall the UI thread for minutes. Fall back to the Z path.
            if (++expansions > maxExpansions)
            {
               return null;
            }

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
               if (!startEnd)
               {
                  if (walk.IsBlocked(ncol, nrow))
                  {
                     continue;
                  }

                  // Don't step across an obstacle between two cell centers. This
                  // stops a single grid step from jumping over a thin obstacle
                  // (e.g. an already-routed connector) whose centerline lies
                  // between the two cell centers.
                  if ((dc == 1 && walk.IsStepXBlocked(col, row)) ||
                      (dc == -1 && walk.IsStepXBlocked(ncol, nrow)) ||
                      (dr == 1 && walk.IsStepYBlocked(col, row)) ||
                      (dr == -1 && walk.IsStepYBlocked(ncol, nrow)))
                  {
                     continue;
                  }
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
