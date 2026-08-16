using System;
using System.Collections.Generic;

namespace ModelConsole.Graph
{

   /// <summary>
   /// Routes a set of orthogonal connectors in a deterministic order so that
   /// each new connector avoids the ones already routed. Each routed polyline
   /// is converted into thin obstacle rectangles (its segments) and passed to
   /// the router as non-inflated obstacles, so later edges keep a small
   /// clearance from it instead of crossing it.
   /// </summary>
   public static class SequentialRouter
   {

      /// <summary>
      /// Route every edge in order, treating each routed polyline as an
      /// obstacle for the rest.
      /// </summary>
      /// <param name="edges">connector endpoints, in routing order</param>
      /// <param name="obstacles">table rectangles the routes must avoid
      /// (inflated by the router)</param>
      /// <param name="bounds">routing region bounds</param>
      /// <param name="options">router options</param>
      /// <param name="edgeMargin">half-thickness of the obstacle added around
      /// each routed segment, in pixels (used as-is, not inflated)</param>
      /// <returns>one polyline per edge, in the same order</returns>
      public static IReadOnlyList<IReadOnlyList<Point2>> RouteAll(
         IReadOnlyList<(Point2 Start, Point2 End)> edges,
         IReadOnlyList<Rect2> obstacles,
         Rect2 bounds,
         RouterOptions options,
         double edgeMargin = 4)
      {
         var result = new List<IReadOnlyList<Point2>>();
         var thin = new List<Rect2>();

         foreach (var edge in edges)
         {
            var pts = OrthogonalRouter.Route(
               edge.Start, edge.End, obstacles, bounds, options, thin);
            result.Add(pts);
            AddSegmentObstacles(thin, pts, edgeMargin);
         }

         return result;
      }

      /// <summary>
      /// Add each segment of the polyline to the thin-obstacle list as a
      /// rectangle around the segment.
      /// </summary>
      private static void AddSegmentObstacles(
         List<Rect2> thin, IReadOnlyList<Point2> pts, double margin)
      {
         for (int i = 0; i < pts.Count - 1; i++)
         {
            Point2 a = pts[i];
            Point2 b = pts[i + 1];
            if (a.Y == b.Y)
            {
               double x1 = Math.Min(a.X, b.X);
               double x2 = Math.Max(a.X, b.X);
               thin.Add(new Rect2(
                  x1 - margin, a.Y - margin,
                  (x2 - x1) + 2 * margin, 2 * margin));
            }
            else
            {
               double y1 = Math.Min(a.Y, b.Y);
               double y2 = Math.Max(a.Y, b.Y);
               thin.Add(new Rect2(
                  a.X - margin, y1 - margin,
                  2 * margin, (y2 - y1) + 2 * margin));
            }
         }
      }

   }

}
