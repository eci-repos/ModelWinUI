using ModelConsole.Geometry;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Unit tests for <see cref="ConnectorAnchors"/>.
   /// </summary>
   public class ConnectorAnchorsTests
   {

      [Fact]
      public void ParentToTheRightAnchorsOnChildRightAndParentLeft()
      {
         var child = new Rect2(0, 0, 100, 200);
         var parent = new Rect2(300, 0, 100, 200);

         var (start, end, childSide, parentSide) =
            ConnectorAnchors.Resolve(child, parent, 100, 100);

         Assert.Equal(new Point2(100, 100), start);
         Assert.Equal(new Point2(300, 100), end);
         Assert.Equal(AnchorSide.Right, childSide);
         Assert.Equal(AnchorSide.Left, parentSide);
      }

      [Fact]
      public void ParentToTheLeftAnchorsOnChildLeftAndParentRight()
      {
         var child = new Rect2(300, 0, 100, 200);
         var parent = new Rect2(0, 0, 100, 200);

         var (start, end, childSide, parentSide) =
            ConnectorAnchors.Resolve(child, parent, 100, 100);

         Assert.Equal(new Point2(300, 100), start);
         Assert.Equal(new Point2(100, 100), end);
         Assert.Equal(AnchorSide.Left, childSide);
         Assert.Equal(AnchorSide.Right, parentSide);
      }

      [Fact]
      public void ParentAboveAnchorsOnChildTopAndParentBottom()
      {
         var child = new Rect2(0, 300, 100, 200);
         var parent = new Rect2(0, 0, 100, 200);

         var (start, end, childSide, parentSide) =
            ConnectorAnchors.Resolve(child, parent, 400, 100);

         Assert.Equal(new Point2(50, 300), start);
         Assert.Equal(new Point2(50, 200), end);
         Assert.Equal(AnchorSide.Top, childSide);
         Assert.Equal(AnchorSide.Bottom, parentSide);
      }

      [Fact]
      public void ParentBelowAnchorsOnChildBottomAndParentTop()
      {
         var child = new Rect2(0, 0, 100, 200);
         var parent = new Rect2(0, 300, 100, 200);

         var (start, end, childSide, parentSide) =
            ConnectorAnchors.Resolve(child, parent, 100, 400);

         Assert.Equal(new Point2(50, 200), start);
         Assert.Equal(new Point2(50, 300), end);
         Assert.Equal(AnchorSide.Bottom, childSide);
         Assert.Equal(AnchorSide.Top, parentSide);
      }

      [Fact]
      public void FanOutOffsetsPerpendicularToSide()
      {
         // Right side -> offset Y, centered on the group.
         Assert.Equal(new Point2(100, 94),
            ConnectorAnchors.FanOut(new Point2(100, 100), AnchorSide.Right, 0, 3, 6));
         Assert.Equal(new Point2(100, 100),
            ConnectorAnchors.FanOut(new Point2(100, 100), AnchorSide.Right, 1, 3, 6));
         Assert.Equal(new Point2(100, 106),
            ConnectorAnchors.FanOut(new Point2(100, 100), AnchorSide.Right, 2, 3, 6));

         // Top side -> offset X.
         Assert.Equal(new Point2(53, 0),
            ConnectorAnchors.FanOut(new Point2(50, 0), AnchorSide.Top, 1, 2, 6));
      }

      [Fact]
      public void FanOutSingleConnectorIsUnchanged()
      {
         Assert.Equal(new Point2(100, 100),
            ConnectorAnchors.FanOut(new Point2(100, 100), AnchorSide.Right, 0, 1, 6));
      }

   }

}
