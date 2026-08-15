using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Model.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModelConsole.Graphics.GLibrary;
using Windows.Foundation;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Input;

namespace ModelConsole.Graphics.Primitives
{

   /// <summary>
   /// Table Row Panel.
   /// </summary>
   public class TableRowPanel : Panel
   {
      private StackPanel _textPanel;
      public ColumnInfo Column { get; set; }

      private GlTextBox _column1;
      private GlTextBox _column2;
      private GlTextBox _column3;

      public GlTextBox ConstraintText
      {
         get => _column1;
      }
      public GlTextBox Text
      {
         get => _column2;
      }
      public GlTextBox DataTypeText
      {
         get => _column3;
      }

      public StackPanel Instance
      {
         get => _textPanel;
      }

      public StackPanel NativeInstance
      {
         get => _textPanel;
      }

      public TableRowPanel()
      {
         _textPanel = new StackPanel();
         _column1 = new GlTextBox();
         _column2 = new GlTextBox();
         _column3 = new GlTextBox();

         _textPanel.Children.Add(_column1.NativeInstance);
         _textPanel.Children.Add(_column2.NativeInstance);
         _textPanel.Children.Add(_column3.NativeInstance);

         SetBorderThickness(0.5);
         _textPanel.BorderBrush = new SolidColorBrush(Colors.LightGray);
      }

      /// <summary>
      /// Move Object to a relative position using given delta values.
      /// </summary>
      /// <param name="delta">DX and DY distance to move</param>
      public void DeltaMove(Point? delta)
      {
         // move text
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
      public virtual void PointerEvent(
         GlPointerEvent poinerEvent, PointerPoint point = null)
      {

      }

      public static string GetDataType(ColumnInfo column)
      {
         return column.Type + (column.Size > 0 ?
            "(" + column.Size.ToString() + ")" : "");
      }

      public void SetBorderThickness(double thickness)
      {
         Thickness t = new Thickness(thickness);
         t.Left = thickness;
         t.Top = thickness;
         t.Right = thickness;
         t.Bottom = thickness;

         _textPanel.BorderThickness = t;
      }

      public void SetBackground(Color color)
      {
         _textPanel.Background = new SolidColorBrush(color);
      }

      public void SetSize(TextBlock block, double width)
      {
         block.MinWidth = width;
         block.Measure(new Size(width, Height));
         block.Arrange(new Rect(0, 0, width, Height));
      }

      public void SetSize()
      {
         Instance.Measure(new Size(Width, Height));
         Instance.Arrange(new Rect(X, Y, Width, Height));
      }

      public void SetSize(
         double constraintWidth, double textWidth, double dataTypeWidth)
      {
         SetSize(ConstraintText.NativeInstance, constraintWidth);
         SetSize(Text.NativeInstance, textWidth);
         SetSize(DataTypeText.NativeInstance, dataTypeWidth);
      }
   }

}
