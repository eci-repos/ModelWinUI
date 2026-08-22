using System;
using System.Collections.Generic;
using System.Linq;

using ModelConsole.Geometry;

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

      [Theory]
      [InlineData(100, 100, 300, 260, AnchorSide.Right, AnchorSide.Left)]
      [InlineData(300, 100, 100, 260, AnchorSide.Left, AnchorSide.Right)]
      [InlineData(100, 260, 300, 100, AnchorSide.Top, AnchorSide.Bottom)]
      [InlineData(100, 100, 300, 260, AnchorSide.Bottom, AnchorSide.Top)]
      public void SideAwareRoutesLeaveAndEnterPerpendicular(
         double sx, double sy, double ex, double ey,
         AnchorSide startSide, AnchorSide endSide)
      {
         var request = new ConnectorRouteRequest(
            new Point2(sx, sy), startSide, new Point2(ex, ey), endSide);

         var pts = OrthogonalRouter.Route(
            request, new Rect2[0], Bounds, Options);

         AssertDirection(pts, Direction(startSide), first: true);
         AssertDirection(pts, Opposite(Direction(endSide)), first: false);
      }

      [Fact]
      public void FannedAnchorsStillLeavePerpendicular()
      {
         var child = new Rect2(0, 0, 100, 200);
         var parent = new Rect2(300, 40, 100, 200);
         var (start, end, childSide, parentSide) =
            ConnectorAnchors.Resolve(child, parent, 100, 140);

         start = ConnectorAnchors.FanOut(start, childSide, 0, 3, 6);
         end = ConnectorAnchors.FanOut(end, parentSide, 2, 3, 6);

         var request = new ConnectorRouteRequest(start, childSide, end, parentSide);
         var pts = OrthogonalRouter.Route(
            request, new[] { child, parent }, Bounds, Options);

         Assert.Equal(start, pts[0]);
         Assert.Equal(end, pts[pts.Count - 1]);
         AssertDirection(pts, Direction(childSide), first: true);
         AssertDirection(pts, Opposite(Direction(parentSide)), first: false);
      }

      [Fact]
      public void TinyDoglegCollapsesWhenShortcutIsClear()
      {
         var pts = new[]
         {
            new Point2(0, 50),
            new Point2(20, 50),
            new Point2(40, 50),
            new Point2(40, 54),
            new Point2(100, 54),
            new Point2(100, 50),
            new Point2(140, 50),
            new Point2(160, 50)
         };

         var normalized = OrthogonalRouter.Normalize(pts, new Rect2[0], 8);

         Assert.Equal(new[] { new Point2(0, 50), new Point2(160, 50) }, normalized);
      }

      [Fact]
      public void TinyDoglegStaysWhenShortcutWouldCrossObstacle()
      {
         var pts = new[]
         {
            new Point2(0, 50),
            new Point2(20, 50),
            new Point2(40, 50),
            new Point2(40, 70),
            new Point2(100, 70),
            new Point2(100, 50),
            new Point2(140, 50),
            new Point2(160, 50)
         };
         var obstacle = new Rect2(60, 45, 20, 10);

         var normalized = OrthogonalRouter.Normalize(pts, new[] { obstacle }, 24);

         Assert.True(normalized.Count > 2);
         AssertNoCrossing(normalized, new[] { obstacle });
      }

      [Fact]
      public void TerminalMicroOffsetAtStartStraightens()
      {
         var request = new ConnectorRouteRequest(
            new Point2(100, 100), AnchorSide.Right,
            new Point2(300, 200), AnchorSide.Left);
         var pts = new[]
         {
            new Point2(100, 100),
            new Point2(124, 100),
            new Point2(124, 104),
            new Point2(180, 104),
            new Point2(180, 160),
            new Point2(260, 160),
            new Point2(260, 200),
            new Point2(300, 200)
         };

         var normalized = OrthogonalRouter.Normalize(request, pts, new Rect2[0], 8);

         Assert.Equal(new[]
         {
            new Point2(100, 100),
            new Point2(180, 100),
            new Point2(180, 160),
            new Point2(260, 160),
            new Point2(260, 200),
            new Point2(300, 200)
         }, normalized);
         AssertDirection(normalized, Direction(AnchorSide.Right), first: true);
      }

      [Fact]
      public void TerminalNudgeDiagnosticReportsRemovableCase()
      {
         var request = new ConnectorRouteRequest(
            new Point2(100, 100), AnchorSide.Right,
            new Point2(300, 200), AnchorSide.Left);
         var pts = new[]
         {
            new Point2(100, 100),
            new Point2(124, 100),
            new Point2(124, 104),
            new Point2(180, 104),
            new Point2(180, 160),
            new Point2(260, 160),
            new Point2(260, 200),
            new Point2(300, 200)
         };

         var diagnostics = OrthogonalRouter.DiagnoseNudges(
            request, pts, new Rect2[0], tinyJogLength: 8);

         var diagnostic = Assert.Single(diagnostics);
         Assert.Equal(RouteNudgeTerminal.Start, diagnostic.Terminal);
         Assert.Equal(RouteNudgeDisposition.Removable, diagnostic.Disposition);
         Assert.Equal(4, diagnostic.Offset);
         Assert.Equal(new Point2(180, 100), diagnostic.CandidatePoints[1]);
      }

      [Fact]
      public void TerminalMicroOffsetAtEndStraightens()
      {
         var request = new ConnectorRouteRequest(
            new Point2(100, 200), AnchorSide.Right,
            new Point2(300, 100), AnchorSide.Left);
         var pts = new[]
         {
            new Point2(100, 200),
            new Point2(140, 200),
            new Point2(140, 160),
            new Point2(220, 160),
            new Point2(220, 104),
            new Point2(260, 104),
            new Point2(260, 100),
            new Point2(300, 100)
         };

         var normalized = OrthogonalRouter.Normalize(request, pts, new Rect2[0], 8);

         Assert.Equal(new[]
         {
            new Point2(100, 200),
            new Point2(140, 200),
            new Point2(140, 160),
            new Point2(220, 160),
            new Point2(220, 100),
            new Point2(300, 100)
         }, normalized);
         AssertDirection(normalized, Opposite(Direction(AnchorSide.Left)), first: false);
      }

      [Fact]
      public void TerminalMicroOffsetStaysWhenShortcutWouldCrossObstacle()
      {
         var request = new ConnectorRouteRequest(
            new Point2(100, 100), AnchorSide.Right,
            new Point2(300, 200), AnchorSide.Left);
         var pts = new[]
         {
            new Point2(100, 100),
            new Point2(124, 100),
            new Point2(124, 104),
            new Point2(180, 104),
            new Point2(180, 160),
            new Point2(260, 160),
            new Point2(260, 200),
            new Point2(300, 200)
         };
         var obstacle = new Rect2(140, 96, 20, 8);

         var normalized = OrthogonalRouter.Normalize(request, pts, new[] { obstacle }, 8);

         Assert.Contains(new Point2(180, 104), normalized);
         AssertNoCrossing(normalized, new[] { obstacle });
      }

      [Fact]
      public void TerminalMicroOffsetKeepsFanOutAnchorsDistinct()
      {
         var request1 = new ConnectorRouteRequest(
            new Point2(100, 100), AnchorSide.Right,
            new Point2(300, 200), AnchorSide.Left);
         var request2 = new ConnectorRouteRequest(
            new Point2(100, 106), AnchorSide.Right,
            new Point2(300, 206), AnchorSide.Left);
         var pts1 = new[]
         {
            new Point2(100, 100),
            new Point2(124, 100),
            new Point2(124, 104),
            new Point2(180, 104),
            new Point2(180, 160),
            new Point2(260, 160),
            new Point2(260, 200),
            new Point2(300, 200)
         };
         var pts2 = new[]
         {
            new Point2(100, 106),
            new Point2(124, 106),
            new Point2(124, 110),
            new Point2(180, 110),
            new Point2(180, 166),
            new Point2(260, 166),
            new Point2(260, 206),
            new Point2(300, 206)
         };

         var normalized1 = OrthogonalRouter.Normalize(request1, pts1, new Rect2[0], 8);
         var normalized2 = OrthogonalRouter.Normalize(request2, pts2, new Rect2[0], 8);

         Assert.Equal(new Point2(100, 100), normalized1[0]);
         Assert.Equal(new Point2(180, 100), normalized1[1]);
         Assert.Equal(new Point2(100, 106), normalized2[0]);
         Assert.Equal(new Point2(180, 106), normalized2[1]);
         Assert.NotEqual(normalized1[1], normalized2[1]);
      }

      [Fact]
      public void TerminalNudgeDiagnosticRetainsEndpointAlignmentOffset()
      {
         var request = new ConnectorRouteRequest(
            new Point2(100, 100), AnchorSide.Right,
            new Point2(300, 104), AnchorSide.Left);
         var pts = new[]
         {
            new Point2(100, 100),
            new Point2(260, 100),
            new Point2(260, 104),
            new Point2(300, 104)
         };

         var diagnostics = OrthogonalRouter.DiagnoseNudges(
            request, pts, new Rect2[0], tinyJogLength: 8);
         var normalized = OrthogonalRouter.Normalize(
            request, pts, new Rect2[0], tinyJogLength: 8);

         var diagnostic = diagnostics.First(d => d.Terminal == RouteNudgeTerminal.End);
         Assert.Equal(RouteNudgeTerminal.End, diagnostic.Terminal);
         Assert.Equal(
            RouteNudgeDisposition.RequiredByEndpointAlignment,
            diagnostic.Disposition);
         Assert.Empty(diagnostic.CandidatePoints);
         Assert.Equal(pts, normalized);
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

      private static void AssertDirection(
         IReadOnlyList<Point2> pts, Point2 expected, bool first)
      {
         if (first)
         {
            for (int i = 0; i < pts.Count - 1; i++)
            {
               if (AssertDirection(pts[i], pts[i + 1], expected))
               {
                  return;
               }
            }
         }
         else
         {
            for (int i = pts.Count - 2; i >= 0; i--)
            {
               if (AssertDirection(pts[i], pts[i + 1], expected))
               {
                  return;
               }
            }
         }
         Assert.Fail("route has no non-zero segment");
      }

      private static bool AssertDirection(
         Point2 a, Point2 b, Point2 expected)
      {
         double dx = b.X - a.X;
         double dy = b.Y - a.Y;
         if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
         {
            return false;
         }

         if (expected.X != 0)
         {
            Assert.True(Math.Abs(dy) < 0.001 &&
               Math.Sign(dx) == Math.Sign(expected.X),
               "expected " + a + " -> " + b + " to move horizontally " +
               expected.X);
         }
         else
         {
            Assert.True(Math.Abs(dx) < 0.001 &&
               Math.Sign(dy) == Math.Sign(expected.Y),
               "expected " + a + " -> " + b + " to move vertically " +
               expected.Y);
         }
         return true;
      }

      private static Point2 Direction(AnchorSide side)
      {
         switch (side)
         {
            case AnchorSide.Left:
               return new Point2(-1, 0);
            case AnchorSide.Right:
               return new Point2(1, 0);
            case AnchorSide.Top:
               return new Point2(0, -1);
            default:
               return new Point2(0, 1);
         }
      }

      private static Point2 Opposite(Point2 p)
      {
         return new Point2(-p.X, -p.Y);
      }

   }

}
