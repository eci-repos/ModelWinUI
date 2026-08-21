using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ModelConsole.Palette;

namespace ModelConsole.Graphics.GLibrary
{

   public class GlTextBox : GlBoxInfo
   {
      private TextBlock _textBlock = new TextBlock();
      public TextBlock Instance
      {
         get { return _textBlock; }
      }
      public TextBlock NativeInstance
      {
         get { return _textBlock; }
      }
      public string Text
      {
         get { return _textBlock.Text; }
         set { _textBlock.Text = value; }
      }
      public double FontSize
      {
         get { return _textBlock.FontSize; }
      }

      public object Tag { get; set; } = null;

      /// <summary>
      /// Convert a #RRGGBB hex string to a <see cref="Color"/> (the shared
      /// palette's format).
      /// </summary>
      private static Color FromHex(string hex)
      {
         hex = hex.TrimStart('#');
         return Color.FromArgb(255,
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
      }

      public GlTextBox()
      {
         _textBlock.Tag = this;

         // The drawing-surface text is always on the light pastel cards, so it
         // renders in the palette's near-black regardless of the app theme — a
         // theme-default foreground would go white in dark OS mode and vanish
         // against the light cards (backlog 041 regression fix).
         _textBlock.Foreground = new SolidColorBrush(FromHex(TablePalette.TextHex));
      }

      /// <summary>
      /// Move Object to a relative position using given delta values.
      /// </summary>
      /// <param name="delta">DX and DY distance to move</param>
      public void DeltaMove(Point? delta)
      {
         if (delta.HasValue)
         {
            X = X + delta.Value.X;
            Y = Y + delta.Value.Y;
         }

         Canvas.SetLeft(Instance, X);
         Canvas.SetTop(Instance, Y);
      }

      /// <summary>
      /// Manage pointer event.
      /// </summary>
      /// <param name="poinerEvent"></param>
      public void PointerEvent(
         GlPointerEvent poinerEvent, PointerPoint point = null)
      {

      }

      /// <summary>
      /// Get Text desired size based on the legth of the string and Font size.
      /// </summary>
      public Size GetDesiredSize()
      {
         _textBlock.Measure(
            new Size(Double.PositiveInfinity, Double.PositiveInfinity));
         return _textBlock.DesiredSize;
      }

   }

}
