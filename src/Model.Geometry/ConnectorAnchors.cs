using System;

namespace ModelConsole.Geometry
{

   /// <summary>
   /// Which edge of a table a connector departs from / enters at.
   /// </summary>
   public enum AnchorSide
   {
      Left,
      Right,
      Top,
      Bottom
   }

   /// <summary>
   /// Resolves the anchor points of an FK connector from the child and parent
   /// table rectangles. The departure side is chosen from the relative
   /// position of the two tables (the side facing the target), so a parent
   /// directly to the side of the child yields a straight connector. Pure,
   /// deterministic, and unit-testable.
   /// </summary>
   public static class ConnectorAnchors
   {

      /// <summary>
      /// Compute the start (child) and end (parent) anchor points for a
      /// connector between two tables, plus the side each anchor sits on.
      /// </summary>
      /// <param name="child">child table rectangle</param>
      /// <param name="parent">parent table rectangle</param>
      /// <param name="childRowY">absolute Y of the child FK column row</param>
      /// <param name="parentRowY">absolute Y of the parent referenced column
      /// row</param>
      /// <returns>start/end anchors and the side each sits on</returns>
      public static (Point2 Start, Point2 End, AnchorSide ChildSide, AnchorSide ParentSide) Resolve(
         Rect2 child, Rect2 parent, double childRowY, double parentRowY)
      {
         Point2 cc = child.Center;
         Point2 pc = parent.Center;
         double dx = pc.X - cc.X;
         double dy = pc.Y - cc.Y;

         if (Math.Abs(dx) >= Math.Abs(dy))
         {
            if (dx >= 0)
            {
               return (new Point2(child.Right, childRowY),
                       new Point2(parent.Left, parentRowY),
                       AnchorSide.Right, AnchorSide.Left);
            }
            return (new Point2(child.Left, childRowY),
                    new Point2(parent.Right, parentRowY),
                    AnchorSide.Left, AnchorSide.Right);
         }

         if (dy >= 0)
         {
            return (new Point2(cc.X, child.Bottom),
                    new Point2(pc.X, parent.Top),
                    AnchorSide.Bottom, AnchorSide.Top);
         }
         return (new Point2(cc.X, child.Top),
                 new Point2(pc.X, parent.Bottom),
                 AnchorSide.Top, AnchorSide.Bottom);
      }

      /// <summary>
      /// Offset an anchor perpendicular to its side so that several
      /// connectors sharing one port separate instead of overlapping.
      /// </summary>
      /// <param name="anchor">anchor point to offset</param>
      /// <param name="side">side the anchor sits on</param>
      /// <param name="index">0-based index of this connector in its port group
      /// </param>
      /// <param name="count">number of connectors in the port group</param>
      /// <param name="spacing">pixel separation between fanned-out connectors
      /// </param>
      /// <returns>the offset anchor</returns>
      public static Point2 FanOut(
         Point2 anchor, AnchorSide side, int index, int count, double spacing)
      {
         double offset = (index - (count - 1) / 2.0) * spacing;
         if (side == AnchorSide.Left || side == AnchorSide.Right)
         {
            return new Point2(anchor.X, anchor.Y + offset);
         }
         return new Point2(anchor.X + offset, anchor.Y);
      }

   }

}
