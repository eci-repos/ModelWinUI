using System;
using System.Collections.Generic;

using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Input;
using Microsoft.UI.Text;

using Model.Data;
using ModelConsole.Graphics.GLibrary;
using ModelConsole.Graph;
using ModelConsole.Palette;

namespace ModelConsole.Graphics.Primitives
{

   /// <summary>
   /// Draw a collapsed group's box as a UML package node on the XAML stack
   /// (backlog 039): a name compartment tinted from the shared
   /// <see cref="GroupPalette"/> (the one hex both renderers parse, so the
   /// XAML and Skia boxes tint a group identically), a body compartment
   /// carrying the «package» stereotype + member count, and the shared table
   /// border (rest neutral, DodgerBlue accent while hovered — the box reads
   /// as a sibling of the table cards).
   /// <para>Measured before drawing — <see cref="ComputedWidth"/> /
   /// <see cref="ComputedHeight"/> parity with <see cref="Table"/> — so the
   /// panel lays boxes out in the same grid as tables. A first-class
   /// drawable like a table: draggable (<see cref="DeltaMove"/> repositions
   /// its bands), hoverable (<see cref="Hovered"/> mutates the border live),
   /// and clickable (its <see cref="Node"/> is the group's
   /// <see cref="GraphNodeKind.Group"/> node).</para>
   /// </summary>
   public class GroupBox : GlRectangle
   {
      private readonly string _group;
      private readonly IReadOnlyList<TableInfo> _members;

      /// <summary>Pastel header band behind the group name.</summary>
      private Border _headerBorder;

      /// <summary>Light body band behind the stereotype + count.</summary>
      private Border _bodyBorder;

      /// <summary>The box's text lines (name + stereotype + count), so a drag
      /// repositions them with the bands.</summary>
      private readonly List<GlTextBox> _textBoxes = new List<GlTextBox>();

      /// <summary>
      /// True while the pointer is over this box (backlog 039 + 041): the
      /// border draws the DodgerBlue accent, thicker, so the hovered box
      /// reads like a hovered table card. Mutated live on the drawn instance
      /// — hover never re-renders.
      /// </summary>
      private bool _hovered;

      /// <summary>
      /// Whether this box is hovered; the setter repaints the border from the
      /// shared <see cref="TablePalette"/> — rest neutral or hovered accent.
      /// </summary>
      public bool Hovered
      {
         get { return _hovered; }
         set
         {
            _hovered = value;
            ApplyBorderAppearance();
         }
      }

      private IGraphNode _node;

      /// <summary>
      /// The group node this box renders (backlog 028): a typed identity over
      /// the schema name and its live member tables. Cached — the box's model
      /// is stable for the life of the instance.
      /// </summary>
      public override IGraphNode Node
      {
         get { return _node ??= GraphNodes.Group(_group, _members); }
      }

      /// <summary>The group (tag) this box renders.</summary>
      public string Group
      {
         get { return _group; }
      }

      /// <summary>The box's member tables, in model order.</summary>
      public IReadOnlyList<TableInfo> Members
      {
         get { return _members; }
      }

      /// <summary>Member table count (rendered on the box).</summary>
      public int MemberCount
      {
         get { return _members == null ? 0 : _members.Count; }
      }

      /// <summary>
      /// The size the box will render at. Like <see cref="Table"/>'s, valid
      /// immediately after construction (measure does not need XAML layout).
      /// </summary>
      public double ComputedWidth { get; private set; }
      public double ComputedHeight { get; private set; }

      /// <summary>The member-count body line, e.g. "12 tables".</summary>
      private string CountText
      {
         get { return MemberCount == 1 ? "1 table" : MemberCount + " tables"; }
      }

      /// <summary>
      /// GroupBox class initialization. Measures the box immediately (no draw
      /// required), mirroring <see cref="Table"/>'s measure-then-draw contract.
      /// </summary>
      /// <param name="frame">drawing context</param>
      /// <param name="x">x lower-left of the box</param>
      /// <param name="y">y lower-left of the box</param>
      /// <param name="group">the collapsed group's name</param>
      /// <param name="members">the box's member tables (live model objects)</param>
      public GroupBox(GlContext frame, double x, double y,
         string group, IReadOnlyList<TableInfo> members) : base()
      {
         X = x;
         Y = y;
         _group = group ?? "";
         _members = members;
         CornerRadius = GlContext.DefaultRoundCorderRadious;

         Measure();
      }

      /// <summary>
      /// Measure the box: the wider of the group-name / count text (plus the
      /// shared padding) vs the group palette's minimum, and the fixed name +
      /// body compartment heights.
      /// </summary>
      private void Measure()
      {
         var tb = new GlTextBox();
         double nameW = 0, countW = 0;
         tb.Text = _group;
         nameW = tb.GetDesiredSize().Width;
         tb.Text = CountText;
         countW = tb.GetDesiredSize().Width;

         double textW = Math.Max(nameW, countW);
         ComputedWidth = Math.Max(
            GroupPalette.MinWidth, textW + 2 * GroupPalette.TextPadding);
         ComputedHeight = GroupPalette.HeaderHeight + GroupPalette.BodyHeight;
      }

      /// <summary>
      /// Convert a #RRGGBB hex string (from the shared <see cref="TablePalette"/>
      /// / <see cref="GroupPalette"/>) to a <see cref="Color"/>.
      /// </summary>
      private static Color FromHex(string hex)
      {
         hex = hex.TrimStart('#');
         return Color.FromArgb(255,
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
      }

      /// <summary>
      /// Stroke the card border from the shared palette: a soft neutral line
      /// at rest, the DodgerBlue accent — thicker — while hovered.
      /// </summary>
      private void ApplyBorderAppearance()
      {
         if (NativeInstance == null)
         {
            return;
         }
         NativeInstance.Stroke = new SolidColorBrush(FromHex(
            _hovered ? TablePalette.HoveredBorderHex : TablePalette.BorderHex));
         NativeInstance.StrokeThickness =
            _hovered ? TablePalette.HoveredBorderWidth : TablePalette.BorderWidth;
      }

      /// <summary>
      /// Move Object to a relative position using given delta values.
      /// </summary>
      /// <param name="delta">DX and DY distance to move</param>
      public override void DeltaMove(Point? delta = null)
      {
         base.DeltaMove(delta);
         if (_headerBorder != null)
         {
            Canvas.SetLeft(_headerBorder, X);
            Canvas.SetTop(_headerBorder, Y);
         }
         if (_bodyBorder != null)
         {
            Canvas.SetLeft(_bodyBorder, X);
            Canvas.SetTop(_bodyBorder, Y + GroupPalette.HeaderHeight);
         }
         foreach (var text in _textBoxes)
         {
            Canvas.SetLeft(text.Instance, text.X);
            Canvas.SetTop(text.Instance, text.Y);
         }
      }

      /// <summary>
      /// Manage pointer event.
      /// </summary>
      /// <param name="poinerEvent"></param>
      public override void PointerEvent(
         GlPointerEvent poinerEvent, PointerPoint point = null)
      {

      }

      /// <summary>
      /// Add one display-only text line to the box (hit-test transparent so a
      /// press anywhere on the box reaches the rectangle).
      /// </summary>
      private void AddText(GlContext frame, string text, double x, double y,
         double fontSize, Windows.UI.Text.FontWeight weight)
      {
         var tb = new GlTextBox();
         tb.Text = text;
         tb.Instance.FontSize = fontSize;
         tb.Instance.FontWeight = weight;
         tb.X = x;
         tb.Y = y;
         Canvas.SetLeft(tb.Instance, x);
         Canvas.SetTop(tb.Instance, y);
         tb.Instance.IsHitTestVisible = false;
         _textBoxes.Add(tb);
         frame.Instance.Children.Add(tb.Instance);
      }

      /// <summary>
      /// Draw the box: tinted name compartment, light body with the
      /// «package» stereotype + member count, and the shared rest/hovered
      /// border — mirroring the Skia box line for line and the XAML
      /// <see cref="Table"/>'s band pattern (banner, then the footer-style
      /// closing band).
      /// </summary>
      public void DrawBox(GlContext frame)
      {
         Width = ComputedWidth;
         Height = ComputedHeight;

         SetInstance(X, Y, Width, Height, GlContext.DefaultRoundCorderRadious);

         // The card border (backlog 041) comes from the shared palette, and
         // the hovered accent when the pointer is over the box.
         ApplyBorderAppearance();

         NativeInstance.Tag = this;
         frame.Instance.Children.Add(NativeInstance);

         // Name compartment: the per-group pastel, rounded on top to match the
         // card, square on the bottom where the body starts.
         var headerColor = FromHex(GroupPalette.BoxHex(_group));
         _headerBorder = new Border
         {
            Width = ComputedWidth,
            Height = GroupPalette.HeaderHeight,
            Background = new SolidColorBrush(headerColor),
            CornerRadius = new CornerRadius(CornerRadius, CornerRadius, 0, 0),
            IsHitTestVisible = false
         };
         Canvas.SetLeft(_headerBorder, X);
         Canvas.SetTop(_headerBorder, Y);
         frame.Instance.Children.Add(_headerBorder);

         // Body compartment: light fill behind the stereotype + count, rounded
         // on the bottom corners, with a hairline top edge where it meets the
         // header.
         var bodyColor = FromHex(TablePalette.PlainRowHex);
         _bodyBorder = new Border
         {
            Width = ComputedWidth,
            Height = GroupPalette.BodyHeight,
            Background = new SolidColorBrush(bodyColor),
            CornerRadius = new CornerRadius(0, 0, CornerRadius, CornerRadius),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
            BorderThickness = new Thickness(0, 1, 0, 0),
            IsHitTestVisible = false
         };
         Canvas.SetLeft(_bodyBorder, X);
         Canvas.SetTop(_bodyBorder, Y + GroupPalette.HeaderHeight);
         frame.Instance.Children.Add(_bodyBorder);

         // Text: the group name in the name compartment; «package» + the
         // member count stacked in the body (the stereotype line is the UML
         // seam — this box is a package node).
         var nameSize = new GlTextBox { Text = _group }.GetDesiredSize();
         double nameTop = Y + (GroupPalette.HeaderHeight - nameSize.Height) / 2.0;
         AddText(frame, _group, X + GroupPalette.TextPadding, nameTop,
            16, FontWeights.Medium);

         double bodyTop = Y + GroupPalette.HeaderHeight;
         AddText(frame, "«package»", X + GroupPalette.TextPadding,
            bodyTop + 10, 13, FontWeights.Normal);
         AddText(frame, CountText, X + GroupPalette.TextPadding,
            bodyTop + 28, 13, FontWeights.Normal);

         DeltaMove();
      }

      /// <summary>
      /// Create and draw a collapsed group's box.
      /// </summary>
      /// <param name="frame">drawing context</param>
      /// <param name="x">x lower-left of the box</param>
      /// <param name="y">y lower-left of the box</param>
      /// <param name="group">the collapsed group's name</param>
      /// <param name="members">the box's member tables</param>
      /// <returns>the created GroupBox instance</returns>
      public static GroupBox DrawBox(GlContext frame, float x, float y,
         string group, IReadOnlyList<TableInfo> members)
      {
         GroupBox g = new GroupBox(frame, x, y, group, members);
         g.DrawBox(frame);
         return g;
      }

   }

}
