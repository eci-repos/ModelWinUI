using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using ABI.Microsoft.UI.Xaml;
using Model.Data;
using Microsoft.UI.Text;

namespace ModelConsole.Graphics.GLibrary
{

   public class GlRectangle : GlObject
   {
      private Rectangle _rectangle = new Rectangle();

      public override bool Selected { get; set; }

      public double X { get; set; }
      public double Y { get; set; }
      public double Z { get; set; } = 0;

      public GlTextBox Banner { get; set; }

      public double Width
      {
         get { return _rectangle.ActualWidth; }
         set { _rectangle.MinWidth = value; }
      }
      public double Height
      {
         get { return _rectangle.ActualHeight; }
         set { _rectangle.MinHeight = value; }
      }

      public double CornerRadius = 10;

      public new object Instance
      {
         get { return _rectangle; }
         set { _rectangle = value as Rectangle; }
      }

      public Rectangle NativeInstance
      {
         get { return _rectangle; }
      }

      public GlRectangle() : base(null)
      {
         m_Instance = _rectangle;
      }

      /// <summary>
      /// Move Object to a relative position using given delta values.
      /// </summary>
      /// <param name="delta">DX and DY distance to move</param>
      public override void DeltaMove(Point? delta = null)
      {
         // move object
         if (delta.HasValue)
         {
            X = X + delta.Value.X;
            Y = Y + delta.Value.Y;
         }

         Canvas.SetLeft(_rectangle, X);
         Canvas.SetTop(_rectangle, Y);

         // move banner
         if (Banner != null)
         {
            Banner.DeltaMove(delta);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="point"></param>
      public override void Move(Point? point = null)
      {
         if (point.HasValue)
         {
            X = point.Value.X;
            Y = point.Value.Y;
            DeltaMove(null);
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

      public override void Reshape(object node)
      {
         
      }

      /// <summary>
      /// Set Instance information.
      /// </summary>
      /// <param name="x">x position</param>
      /// <param name="y">y positin</param>
      /// <param name="width">width</param>
      /// <param name="height">height</param>
      /// <param name="cornerRadius">corner radious (default: 0)</param>
      /// <returns>instance of GlRectangle is returned</returns>
      public GlRectangle SetInstance(
         double x, double y, double width, double height,
         double cornerRadius = 0)
      {
         X = x;
         Y = y;
         Width = width;
         Height = height;

         SetBackground(Colors.WhiteSmoke);
         _rectangle.RadiusX = cornerRadius;
         _rectangle.RadiusY = cornerRadius;
         _rectangle.Stroke = new SolidColorBrush(Colors.Black);
         _rectangle.StrokeThickness = 0.5;

         _rectangle.Tag = this;
         CornerRadius = cornerRadius;
         
         return this;
      }

      /// <summary>
      /// Set Background Color.
      /// </summary>
      /// <param name="color">color to set</param>
      public void SetBackground(Color color)
      {
         _rectangle.Fill = new SolidColorBrush(color);
      }

      /// <summary>
      /// Set object position.
      /// </summary>
      /// <param name="x">x origin</param>
      /// <param name="y">y origin</param>
      public void SetPosition(double x, double y)
      {
         Canvas.SetLeft(_rectangle, x);
         Canvas.SetTop(_rectangle, y);
      }

      /// <summary>
      /// Create and Draw a rectangle.
      /// </summary>
      /// <param name="x">x position</param>
      /// <param name="y">y positin</param>
      /// <param name="width">width</param>
      /// <param name="height">height</param>
      /// <param name="cornerRadius">corner radious (default: 0)</param>
      /// <returns>instance of GlRectangle is returned</returns>
      public static GlRectangle Create(
         double x, double y, double width, double height,
         double cornerRadius = 0)
      {
         GlRectangle r = new GlRectangle();
         return r.SetInstance(x,y,width,height,cornerRadius);
      }

      /// <summary>
      /// Create and Draw a rectangle.
      /// </summary>
      /// <param name="frame">frame that contains a canvas in where to draw it
      /// </param>
      /// <param name="x">x position</param>
      /// <param name="y">y positin</param>
      /// <param name="width">width</param>
      /// <param name="height">height</param>
      /// <param name="cornerRadius">corner radious (default: 0)</param>
      /// <returns>instance of GlRectangle is returned</returns>
      public static GlRectangle Draw(
         GlContext frame, double x, double y, double width, double height,
         double cornerRadius = 0)
      {
         GlRectangle r = Create(x, y, width, height, cornerRadius);

         r.SetPosition(x, y);

         frame.Instance.Children.Add(r.NativeInstance);
         return r;
      }

      /// <summary>
      /// Add Rectangle Banner.
      /// </summary>
      /// <param name="frame">frame that contains a canvas in where to draw it
      /// </param>
      /// <param name="rectangle">rectangle to add banner</param>
      /// <param name="title">banner title/text</param>
      public static void AddBanner(
         GlContext frame, GlRectangle rectangle, string title)
      {
         GlTextBox textBox = rectangle.Banner == null ? 
            new GlTextBox() : rectangle.Banner;

         textBox.Instance.FontSize = 16;
         textBox.Instance.FontWeight = FontWeights.Medium;

         textBox.Text = title;

         textBox.Width = rectangle.Width;
         textBox.Height = 25;
         textBox.Text = title;

         textBox.X = rectangle.X + 10;
         textBox.Y = rectangle.Y + 10;

         Canvas.SetLeft(textBox.Instance, textBox.X);
         Canvas.SetTop(textBox.Instance, textBox.Y);

         if (rectangle.Banner == null)
         {
            frame.Instance.Children.Add(textBox.Instance);
            rectangle.Banner = textBox;
         }

      }

   }

}
