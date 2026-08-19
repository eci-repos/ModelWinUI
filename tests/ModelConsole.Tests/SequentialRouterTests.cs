using System;
using System.Collections.Generic;
using System.Linq;

using ModelConsole.Geometry;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Unit tests for <see cref="SequentialRouter"/>.
   /// </summary>
   public class SequentialRouterTests
   {

      private static readonly RouterOptions Options = new()
      {
         GridSize = 16,
         ObstacleMargin = 14,
         StubLength = 20
      };

      private static readonly Rect2 Bounds = new Rect2(0, 0, 500, 500);

      [Fact]
      public void StraightEdgesStayStraight()
      {
         var edges = new (Point2, Point2)[]
         {
            (new Point2(0, 100), new Point2(300, 100)),
            (new Point2(0, 300), new Point2(300, 300))
         };

         var routes = SequentialRouter.RouteAll(edges, new Rect2[0], Bounds, Options);

         Assert.Equal(2, routes.Count);
         Assert.Equal(2, routes[0].Count);
         Assert.Equal(2, routes[1].Count);
      }

      [Fact]
      public void LaterEdgeAvoidsEarlierEdge()
      {
         // Routed independently these two cross at (300, 100): the first takes
         // the direct HV path along y=100, the second's vertical at x=300
         // would pass straight through it.
         var edges = new (Point2, Point2)[]
         {
            (new Point2(0, 100), new Point2(400, 300)),
            (new Point2(100, 0), new Point2(300, 400))
         };

         var routes = SequentialRouter.RouteAll(edges, new Rect2[0], Bounds, Options);

         Assert.False(PolylinesCross(routes[0], routes[1]),
            "second edge crosses the first");
      }

      [Fact]
      public void RoutesAvoidTableObstacles()
      {
         var table = new Rect2(150, 150, 100, 100);
         var edges = new (Point2, Point2)[]
         {
            (new Point2(0, 200), new Point2(400, 200))
         };

         var routes = SequentialRouter.RouteAll(edges, new[] { table }, Bounds, Options);

         AssertNoCrossing(routes[0], new[] { table });
      }

      [Fact]
      public void IsDeterministic()
      {
         var edges = new (Point2, Point2)[]
         {
            (new Point2(0, 100), new Point2(400, 300)),
            (new Point2(100, 0), new Point2(300, 400)),
            (new Point2(0, 300), new Point2(400, 100))
         };

         var r1 = SequentialRouter.RouteAll(edges, new Rect2[0], Bounds, Options);
         var r2 = SequentialRouter.RouteAll(edges, new Rect2[0], Bounds, Options);

         Assert.Equal(r1, r2);
      }

      private static bool PolylinesCross(
         IReadOnlyList<Point2> a, IReadOnlyList<Point2> b)
      {
         for (int i = 0; i < a.Count - 1; i++)
         {
            for (int j = 0; j < b.Count - 1; j++)
            {
               if (SegmentsCross(a[i], a[i + 1], b[j], b[j + 1]))
               {
                  return true;
               }
            }
         }
         return false;
      }

      private static bool SegmentsCross(Point2 a1, Point2 a2, Point2 b1, Point2 b2)
      {
         bool aH = a1.Y == a2.Y;
         bool bH = b1.Y == b2.Y;
         if (aH == bH)
         {
            return false;
         }
         if (aH)
         {
            // a is horizontal, b is vertical - strict interior intersection
            return a1.Y > Math.Min(b1.Y, b2.Y) && a1.Y < Math.Max(b1.Y, b2.Y) &&
                   b1.X > Math.Min(a1.X, a2.X) && b1.X < Math.Max(a1.X, a2.X);
         }
         return b1.Y > Math.Min(a1.Y, a2.Y) && b1.Y < Math.Max(a1.Y, a2.Y) &&
                a1.X > Math.Min(b1.X, b2.X) && a1.X < Math.Max(b1.X, b2.X);
      }

      private static void AssertNoCrossing(
         IReadOnlyList<Point2> pts, IReadOnlyList<Rect2> obstacles)
      {
         for (int i = 0; i < pts.Count - 1; i++)
         {
            foreach (var o in obstacles)
            {
               Assert.False(
                  Rect2.SegmentCrossesInterior(pts[i], pts[i + 1], o),
                  "segment " + pts[i] + " -> " + pts[i + 1] + " crosses " + o);
            }
         }
      }

   }

}
