using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Input;

using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

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

      public GlTextBox()
      {
         _textBlock.Tag = this;
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
