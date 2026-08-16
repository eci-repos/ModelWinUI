using System.Collections.Generic;
using System.Linq;

using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Unit tests for <see cref="OrthogonalRouter"/>.
   /// </summary>
   public class OrthogonalRouterTests
   {

      private static readonly RouterOptions Options = new()
      {
         GridSize = 16,
         ObstacleMargin = 14,
         StubLength = 32
      };

      private static readonly Rect2 Bounds = new Rect2(0, 0, 400, 400);

      private static IReadOnlyList<Point2> Route(
         Point2 start, Point2 end, params Rect2[] obstacles)
      {
         return OrthogonalRouter.Route(start, end, obstacles, Bounds, Options);
      }

      [Fact]
      public void StraightLineWhenClear()
      {
         var pts = Route(new Point2(0, 100), new Point2(300, 100));

         Assert.Equal(new[] { new Point2(0, 100), new Point2(300, 100) }, pts);
      }

      [Fact]
      public void DirectPathIsPreferredOverDetour()
      {
         var pts = Route(new Point2(0, 100), new Point2(300, 100));

         Assert.Equal(2, pts.Count);
      }

      [Fact]
      public void SegmentsAreAxisAligned()
      {
         var pts = Route(
            new Point2(0, 100), new Point2(400, 100),
            new Rect2(150, 50, 100, 100));

         for (int i = 0; i < pts.Count - 1; i++)
         {
            Assert.True(
               pts[i].X == pts[i + 1].X || pts[i].Y == pts[i + 1].Y,
               "segment " + pts[i] + " -> " + pts[i + 1] + " is not axis-aligned");
         }
      }

      [Fact]
      public void EndpointsAreExact()
      {
         var start = new Point2(0, 100);
         var end = new Point2(400, 300);
         var pts = Route(start, end, new Rect2(150, 50, 100, 100));

         Assert.Equal(start, pts[0]);
         Assert.Equal(end, pts[pts.Count - 1]);
      }

      [Fact]
      public void AvoidsObstacleBetweenEndpoints()
      {
         var obstacle = new Rect2(150, 50, 100, 100);
         var pts = Route(new Point2(0, 100), new Point2(400, 100), obstacle);

         AssertNoCrossing(pts, new[] { obstacle });
      }

      [Fact]
      public void NoSegmentCrossesAnyObstacleInterior()
      {
         var obstacles = new[]
         {
            new Rect2(60, 60, 80, 80),
            new Rect2(220, 60, 80, 80),
            new Rect2(140, 220, 80, 80)
         };
         var pts = Route(new Point2(0, 100), new Point2(400, 300), obstacles);

         AssertNoCrossing(pts, obstacles);
      }

      [Fact]
      public void AnchorsOnObstacleBoundaryStillRoute()
      {
         var child = new Rect2(0, 40, 100, 200);
         var parent = new Rect2(300, 40, 100, 200);
         var pts = Route(
            new Point2(100, 140),   // child's right edge
            new Point2(300, 140),   // parent's left edge
            child, parent);

         Assert.True(pts.Count >= 2);
         Assert.Equal(new Point2(100, 140), pts[0]);
         Assert.Equal(new Point2(300, 140), pts[pts.Count - 1]);
         AssertNoCrossing(pts, new[] { child, parent });
      }

      [Fact]
      public void UnreachableGridFallsBackToOrthogonalPath()
      {
         // Full-height wall splits the region; no grid route exists.
         var wall = new Rect2(100, 0, 50, 400);
         var pts = Route(new Point2(0, 200), new Point2(400, 200), wall);

         Assert.True(pts.Count >= 2);
         Assert.Equal(new Point2(0, 200), pts[0]);
         Assert.Equal(new Point2(400, 200), pts[pts.Count - 1]);
         for (int i = 0; i < pts.Count - 1; i++)
         {
            Assert.True(
               pts[i].X == pts[i + 1].X || pts[i].Y == pts[i + 1].Y);
         }
      }

      [Fact]
      public void IsDeterministic()
      {
         var obstacles = new[]
         {
            new Rect2(60, 60, 80, 80),
            new Rect2(220, 60, 80, 80),
            new Rect2(140, 220, 80, 80)
         };
         var p1 = Route(new Point2(0, 100), new Point2(400, 300), obstacles);
         var p2 = Route(new Point2(0, 100), new Point2(400, 300), obstacles);

         Assert.Equal(p1, p2);
      }

      [Fact]
      public void CollinearPointsAreSimplified()
      {
         var pts = Route(
            new Point2(0, 100), new Point2(400, 100),
            new Rect2(150, 50, 100, 100));

         for (int i = 1; i < pts.Count - 1; i++)
         {
            Assert.False(
               (pts[i - 1].X == pts[i].X && pts[i].X == pts[i + 1].X) ||
               (pts[i - 1].Y == pts[i].Y && pts[i].Y == pts[i + 1].Y),
               "redundant collinear point " + pts[i]);
         }
      }

      [Fact]
      public void SameStartAndEndReturnsBoth()
      {
         var pts = Route(new Point2(50, 50), new Point2(50, 50));
         Assert.Equal(new[] { new Point2(50, 50), new Point2(50, 50) }, pts);
      }

      [Fact]
      public void NodeBudgetCapsAStarWork()
      {
         // A tiny budget forces A* to give up and fall back to the Z path
         // (backlog 013 safety net). The route still has exact endpoints and
         // axis-aligned segments.
         var options = new RouterOptions
         {
            GridSize = 16,
            ObstacleMargin = 14,
            StubLength = 32,
            MaxExpansions = 1
         };
         var pts = OrthogonalRouter.Route(
            new Point2(0, 100), new Point2(400, 300),
            new[] { new Rect2(150, 50, 100, 100) },
            Bounds, options);

         Assert.True(pts.Count >= 2);
         Assert.Equal(new Point2(0, 100), pts[0]);
         Assert.Equal(new Point2(400, 300), pts[pts.Count - 1]);
         for (int i = 0; i < pts.Count - 1; i++)
         {
            Assert.True(
               pts[i].X == pts[i + 1].X || pts[i].Y == pts[i + 1].Y);
         }
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
