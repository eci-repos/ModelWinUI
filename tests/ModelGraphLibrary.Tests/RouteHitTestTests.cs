using System.Collections.Generic;

using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// RouteHitTest — the pure distance-to-polyline hover hit-test the Skia
   /// renderer uses (it has no XAML hit-testing). Deterministic so the
   /// behavior is pinned down here.
   /// </summary>
   public class RouteHitTestTests
   {

      [Fact]
      public void NearestPicksTheRouteWithinThreshold()
      {
         var routes = new List<IReadOnlyList<Point2>>
         {
            new List<Point2> { new Point2(0, 0), new Point2(0, 100) },
            new List<Point2> { new Point2(100, 0), new Point2(100, 100) }
         };

         // Point 4 px from the second route: hits it.
         Assert.Equal(1, RouteHitTest.Nearest(routes, new Point2(96, 50), 6));
         // Point on the first route: hits it.
         Assert.Equal(0, RouteHitTest.Nearest(routes, new Point2(0, 50), 6));
      }

      [Fact]
      public void NearestReturnsMinusOneBeyondThreshold()
      {
         var routes = new List<IReadOnlyList<Point2>>
         {
            new List<Point2> { new Point2(0, 0), new Point2(0, 100) }
         };

         // 50 px from the line, well beyond a 6 px radius.
         Assert.Equal(-1, RouteHitTest.Nearest(routes, new Point2(50, 50), 6));
      }

      [Fact]
      public void NearestPicksTheClosestOfSeveral()
      {
         var routes = new List<IReadOnlyList<Point2>>
         {
            new List<Point2> { new Point2(0, 0), new Point2(0, 100) },
            new List<Point2> { new Point2(50, 0), new Point2(50, 100) },
            new List<Point2> { new Point2(100, 0), new Point2(100, 100) }
         };

         // Point 2 px from the middle route, 48 from the first, 52 from the
         // last; a 60 px radius lets all three qualify, and the nearest
         // (middle) wins.
         Assert.Equal(1, RouteHitTest.Nearest(routes, new Point2(48, 50), 60));
      }

      [Fact]
      public void NearestHandlesNullEmptyAndSinglePointRoutes()
      {
         Assert.Equal(-1, RouteHitTest.Nearest(null, new Point2(0, 0), 10));
         Assert.Equal(-1, RouteHitTest.Nearest(
            new List<IReadOnlyList<Point2>>(), new Point2(0, 0), 10));

         var single = new List<IReadOnlyList<Point2>>
         {
            new List<Point2> { new Point2(10, 10) }
         };
         // A single-point route hit-tests as its point.
         Assert.Equal(0, RouteHitTest.Nearest(single, new Point2(10, 10), 5));
         Assert.Equal(-1, RouteHitTest.Nearest(single, new Point2(20, 10), 5));
      }

   }

}
