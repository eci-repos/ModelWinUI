using System;
using System.Collections.Generic;

namespace ModelConsole.Geometry
{

   /// <summary>
   /// Hover hit-testing over routed connector polylines — the Skia renderer
   /// has no XAML hit-testing, so a hover is a pure distance check. Portable
   /// and deterministic so the unit tests can pin it down.
   /// </summary>
   public static class RouteHitTest
   {
      /// <summary>
      /// The index of the route whose polyline is within
      /// <paramref name="maxDistance"/> of <paramref name="point"/>, or −1 when
      /// none is. When several routes qualify, the nearest wins.
      /// </summary>
      /// <param name="routes">the routed polylines (e.g. <c>ErdDiagram.Routes</c>)</param>
      /// <param name="point">hit point, in the same (content) space as the routes</param>
      /// <param name="maxDistance">hit radius, in the same space as the routes</param>
      public static int Nearest(
         IReadOnlyList<IReadOnlyList<Point2>> routes, Point2 point, double maxDistance)
      {
         if (routes == null)
         {
            return -1;
         }

         int nearest = -1;
         double best = maxDistance;
         for (int i = 0; i < routes.Count; i++)
         {
            double d = DistanceToRoute(routes[i], point);
            if (d < best)
            {
               best = d;
               nearest = i;
            }
         }
         return nearest;
      }

      /// <summary>Minimum distance from <paramref name="point"/> to the polyline.</summary>
      private static double DistanceToRoute(IReadOnlyList<Point2> route, Point2 point)
      {
         if (route == null || route.Count == 0)
         {
            return double.PositiveInfinity;
         }
         if (route.Count == 1)
         {
            return Distance(route[0], point);
         }

         double min = double.PositiveInfinity;
         for (int i = 0; i < route.Count - 1; i++)
         {
            min = Math.Min(min, DistanceToSegment(route[i], route[i + 1], point));
         }
         return min;
      }

      /// <summary>Euclidean distance between two points.</summary>
      private static double Distance(Point2 a, Point2 b)
      {
         double dx = a.X - b.X;
         double dy = a.Y - b.Y;
         return Math.Sqrt(dx * dx + dy * dy);
      }

      /// <summary>Distance from <paramref name="p"/> to the segment <paramref name="a"/>–<paramref name="b"/>.</summary>
      private static double DistanceToSegment(Point2 a, Point2 b, Point2 p)
      {
         double abx = b.X - a.X;
         double aby = b.Y - a.Y;
         double lenSq = abx * abx + aby * aby;
         if (lenSq <= 0)
         {
            return Distance(a, p);
         }

         // Parameter of the closest point on the infinite line, clamped to
         // the segment's [0, 1] range.
         double t = ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / lenSq;
         t = Math.Max(0, Math.Min(1, t));
         return Distance(new Point2(a.X + t * abx, a.Y + t * aby), p);
      }
   }

}
