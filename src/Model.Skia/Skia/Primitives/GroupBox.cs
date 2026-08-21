using System;

using ModelConsole.Palette;
using ModelConsole.Skia.GLibrary;

using SkiaSharp;

namespace ModelConsole.Skia.Primitives
{

   /// <summary>
   /// Draw a collapsed group's box as a UML package node on the Skia stack
   /// (backlog 039): a name compartment tinted from the shared
   /// <see cref="GroupPalette"/> (the one hex both renderers parse, so the
   /// XAML and Skia boxes tint a group identically), a body compartment
   /// carrying the «package» stereotype + member count, and the shared table
   /// border (rest neutral, DodgerBlue accent while hovered — the box reads
   /// as a sibling of the table cards).
   /// <para>Measured before drawing — <see cref="ComputedWidth"/> /
   /// <see cref="ComputedHeight"/> parity with <see cref="Table"/> — so the
   /// composer lays boxes out in the same grid as tables. Drawn in plain
   /// content coordinates like <see cref="Connector"/>, so the box occupies
   /// exactly its <c>Rect2</c>: a connector anchored on that rect (a box↔box
   /// or box↔table route) touches its boundary.</para>
   /// </summary>
   public sealed class GroupBox
   {
      private readonly GlFrame _frame;
      private readonly string _group;
      private readonly int _memberCount;

      private float _x;
      private float _y;

      private bool _hovered;

      /// <summary>
      /// True while the pointer is over this box (backlog 039 + 041): the
      /// border draws the DodgerBlue accent, thicker, so the hovered box
      /// reads like a hovered table card. The Skia stack redraws, so it is
      /// set before each draw instead of toggled live.
      /// </summary>
      public bool Hovered
      {
         get { return _hovered; }
         set { _hovered = value; }
      }

      /// <summary>
      /// Measured box width: the widest of the group name / count text (plus
      /// the shared padding) vs the group palette's minimum, so a short name
      /// still reads as a card.
      /// </summary>
      public float ComputedWidth { get; }

      /// <summary>Measured box height: name + body compartments.</summary>
      public float ComputedHeight { get; }

      /// <summary>
      /// The member-count body line, e.g. "12 tables".
      /// </summary>
      private string CountText
      {
         get { return _memberCount == 1 ? "1 table" : _memberCount + " tables"; }
      }

      /// <summary>
      /// GroupBox class initialization. Measures the box immediately (no draw
      /// required), mirroring <see cref="Table"/>'s measure-then-draw contract.
      /// </summary>
      /// <param name="frame">drawing context (font source for measure)</param>
      /// <param name="x">x lower-left of the box</param>
      /// <param name="y">y lower-left of the box</param>
      /// <param name="group">the collapsed group's name</param>
      /// <param name="memberCount">the box's member table count</param>
      public GroupBox(GlFrame frame, float x, float y, string group, int memberCount)
      {
         _frame = frame;
         _x = x;
         _y = y;
         _group = group ?? "";
         _memberCount = memberCount;

         float nameW = frame.DefaultFont.MeasureText(_group);
         float countW = frame.DefaultFont.MeasureText(CountText);
         float textW = Math.Max(nameW, countW);
         ComputedWidth = Math.Max(
            GroupPalette.MinWidth, textW + 2 * GroupPalette.TextPadding);
         ComputedHeight = GroupPalette.HeaderHeight + GroupPalette.BodyHeight;
      }

      /// <summary>
      /// Draw the box: tinted name compartment, light body with the
      /// «package» stereotype + member count, and the shared rest/hovered
      /// border. Plain content coordinates (no origin flip) so the box
      /// occupies its rect exactly.
      /// </summary>
      public void Draw()
      {
         float w = ComputedWidth;
         float h = ComputedHeight;
         float corner = _frame.DefaultRoundCorderRadious;

         // Name compartment — the per-group pastel from the shared palette.
         SKColor tint = SKColor.Parse(GroupPalette.BoxHex(_group));
         var band = new RectangleHalf(_frame);
         band.DrawTop(_x, _y, w, GroupPalette.HeaderHeight, corner, tint);

         // Body compartment: light fill behind the stereotype + count.
         var bodyFill = new SKPaint
         {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = SKColor.Parse(TablePalette.PlainRowHex)
         };
         _frame.Canvas.DrawRect(
            _x, _y + GroupPalette.HeaderHeight, w, GroupPalette.BodyHeight, bodyFill);
         bodyFill.Dispose();

         // Border: the shared table border, rest neutral / hovered accent.
         band.DrawBorder(
            _x, _y, w, h, corner,
            SKColor.Parse(_hovered ? TablePalette.HoveredBorderHex : TablePalette.BorderHex),
            _hovered ? TablePalette.HoveredBorderWidth : TablePalette.BorderWidth);

         // Text: the group name in the name compartment; «package» + the
         // member count stacked in the body (the stereotype line is the UML
         // seam — this box is a package node). Baselines follow the Table's
         // text convention (plain canvas, top-left origin).
         _frame.Canvas.DrawText(_group,
            _x + GroupPalette.TextPadding,
            _y + (GroupPalette.HeaderHeight + _frame.DefaultFont.Size) / 2.0f,
            SKTextAlign.Left, _frame.DefaultFont, _frame.DefaultTextPaint);

         float bodyTop = _y + GroupPalette.HeaderHeight;
         _frame.Canvas.DrawText("«package»",
            _x + GroupPalette.TextPadding,
            bodyTop + 12 + _frame.DefaultFont.Size,
            SKTextAlign.Left, _frame.DefaultFont, _frame.DefaultTextPaint);
         _frame.Canvas.DrawText(CountText,
            _x + GroupPalette.TextPadding,
            bodyTop + 30 + _frame.DefaultFont.Size,
            SKTextAlign.Left, _frame.DefaultFont, _frame.DefaultTextPaint);
      }

      /// <summary>
      /// Create and draw a collapsed group box.
      /// </summary>
      /// <param name="frame">drawing context</param>
      /// <param name="x">x lower-left</param>
      /// <param name="y">y lower-left</param>
      /// <param name="group">the collapsed group's name</param>
      /// <param name="memberCount">the box's member table count</param>
      /// <returns>the created GroupBox instance</returns>
      public static GroupBox Draw(GlFrame frame, float x, float y,
         string group, int memberCount)
      {
         GroupBox g = new GroupBox(frame, x, y, group, memberCount);
         g.Draw();
         return g;
      }
   }

}
