using System;
using System.Collections.Generic;
using System.Linq;

using ModelConsole.Geometry;
using ModelConsole.ModelData;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

using SkiaSharp;

using Xunit;
using Xunit.Abstractions;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 033 - sequential routing must not push connectors to the
   /// drawing's extremes. Crossing another connector is a cost, not a ban:
   /// <see cref="OrthogonalRouter.RouteBest"/> takes the shorter of the
   /// wall-avoiding and wall-ignoring routes when the avoiding detour costs
   /// more than the crossing tolerance, while a table interior is never
   /// crossed in either candidate. Verified on the 50-table public-safety
   /// model (no route may exceed 2x its anchor distance) and on targeted
   /// wall scenarios (the tolerance decides whether a route crosses a wall).
   /// </summary>
   public class OptimalRoutingTests
   {
      private readonly ITestOutputHelper _output;

      public OptimalRoutingTests(ITestOutputHelper output)
      {
         _output = output;
      }

      private static double Length(IReadOnlyList<Point2> pts)
      {
         return OrthogonalRouter.PolylineLength(pts);
      }

      private static double Straight(IReadOnlyList<Point2> pts)
      {
         Point2 a = pts[0];
         Point2 b = pts[pts.Count - 1];
         return Math.Sqrt((b.X - a.X) * (b.X - a.X) +
                          (b.Y - a.Y) * (b.Y - a.Y));
      }

      [Fact]
      public void PublicSafetyRoutesStayNearOptimal()
      {
         using var surface = SKSurface.Create(new SKImageInfo(4, 4));
         var frame = new GlFrame(surface);
         var diagram = ErdComposer.Compose(PublicSafetySchema.Tables, frame,
            new ErdOptions());

         Assert.Equal(74, diagram.Routes.Count);

         // No connector may meander 2x beyond its anchors' straight distance
         // (+ 40 px absolute allowance for the 20 px stub at each end on
         // very short routes).
         double maxRatio = 0;
         string worst = "";
         for (int i = 0; i < diagram.Routes.Count; i++)
         {
            var pts = diagram.Routes[i];
            double len = Length(pts);
            double straight = Straight(pts);
            double ratio = len / Math.Max(1, straight);
            if (ratio > maxRatio)
            {
               maxRatio = ratio;
               worst = i + " " + diagram.Edges[i].ChildTable + "->" +
                  diagram.Edges[i].ParentTable + " len=" + len.ToString("F0");
            }
            Assert.True(len <= 2.0 * straight + 40,
               "route " + i + " (" + diagram.Edges[i].ChildTable + "->" +
               diagram.Edges[i].ParentTable + ") is " + len.ToString("F0") +
               " px for a " + straight.ToString("F0") + " px direct path");
         }
         _output.WriteLine("max route/straight ratio: " + maxRatio.ToString("F2") +
            "  (" + worst + ")");

         // The measured 9.6x case must now route near the optimum (1212 px).
         int emp = diagram.Edges.FindIndex(e =>
            e.ChildTable == "Employee" && e.ParentTable == "Person");
         Assert.True(emp >= 0, "Employee->Person edge present");
         double empLen = Length(diagram.Routes[emp]);
         Assert.True(empLen <= 1500,
            "Employee->Person routed " + empLen.ToString("F0") +
            " px (was 8252 px, optimum ~1212 px)");
         _output.WriteLine("Employee->Person length: " + empLen.ToString("F0"));
      }

      private static RouterOptions Options(double crossingTolerance)
      {
         return new RouterOptions
         {
            GridSize = 16,
            ObstacleMargin = 14,
            StubLength = 20,
            CrossingTolerance = crossingTolerance
         };
      }

      private static readonly Rect2 Bounds = new Rect2(0, 0, 500, 500);

      /// <summary>A horizontal wall at y=250 spanning most of the width
      /// (x 40..450 in a 500-px region), so avoiding it forces a walk around
      /// an end (~730 px) versus the 350-px direct crossing.</summary>
      private static readonly List<Rect2> Wall = new()
      {
         new Rect2(36, 246, 418, 8) // x 36..454, y 246..254
      };

      [Fact]
      public void AbsurdDetourCrossesTheWall()
      {
         // Start and end share x=200, separated vertically across the wall.
         // Avoiding the wall forces a long walk around an end; the direct
         // ~350-px crossing is well under the 1.5x tolerance, so the default
         // tolerance must take the crossing route.
         Point2 start = new Point2(200, 50);
         Point2 end = new Point2(200, 400);

         var avoiding = OrthogonalRouter.Route(
            start, end, new Rect2[0], Bounds, Options(1.5), Wall);
         var best = OrthogonalRouter.RouteBest(
            start, end, new Rect2[0], Bounds, Options(1.5), Wall);

         double avoidLen = Length(avoiding);
         double bestLen = Length(best);
         _output.WriteLine("avoiding=" + avoidLen.ToString("F0") +
            " best=" + bestLen.ToString("F0"));

         Assert.True(avoidLen > 600, "wall-avoiding route should detour");
         Assert.True(bestLen < 450, "tolerance 1.5 should take the crossing route");
         Assert.True(Crosses(best, Wall), "the chosen route crosses the wall");
      }

      [Fact]
      public void CheapDetourKeepsTheWallAvoidingRoute()
      {
         Point2 start = new Point2(200, 50);
         Point2 end = new Point2(200, 400);

         // A large tolerance keeps the wall-avoiding (crossing-free) route
         // even though the direct crossing is shorter.
         var best = OrthogonalRouter.RouteBest(
            start, end, new Rect2[0], Bounds, Options(10.0), Wall);

         Assert.True(Length(best) > 600,
            "tolerance 10 should keep the wall-avoiding route");
         Assert.False(Crosses(best, Wall), "the avoiding route crosses no wall");
      }

      [Fact]
      public void CrossingRouteNeverCrossesATable()
      {
         // Even when the tolerance gives the crossing route, the route must
         // not cross a table interior - the 012 invariant is unconditional.
         var table = new Rect2(180, 100, 40, 300);
         var start = new Point2(200, 50);
         var end = new Point2(200, 400);

         var best = OrthogonalRouter.RouteBest(
            start, end, new[] { table }, Bounds, Options(1.5), Wall);

         for (int i = 0; i < best.Count - 1; i++)
         {
            Assert.False(
               Rect2.SegmentCrossesInterior(best[i], best[i + 1], table),
               "segment " + best[i] + " -> " + best[i + 1] + " crosses the table");
         }
      }

      private static bool Crosses(
         IReadOnlyList<Point2> pts, IReadOnlyList<Rect2> rects)
      {
         for (int i = 0; i < pts.Count - 1; i++)
         {
            foreach (var r in rects)
            {
               if (Rect2.SegmentCrossesInterior(pts[i], pts[i + 1], r))
               {
                  return true;
               }
            }
         }
         return false;
      }
   }

}
