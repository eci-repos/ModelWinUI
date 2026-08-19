using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace ModelConsole.Graphics.GLibrary
{

   /// <summary>
   /// A small ellipse primitive (used for connector endpoint markers). Wraps a
   /// native XAML <see cref="Ellipse"/> and positions it centered on a point.
   /// </summary>
   public class GlEllipse : GlObject
   {
      private readonly Ellipse _ellipse = new Ellipse();

      public override bool Selected { get; set; }

      public double X { get; set; }
      public double Y { get; set; }
      public double Width { get; set; }
      public double Height { get; set; }

      public Ellipse NativeInstance
      {
         get { return _ellipse; }
      }

      public GlEllipse() : base(null)
      {
         m_Instance = _ellipse;
      }

      /// <summary>
      /// Move Object to a relative position using given delta values.
      /// </summary>
      /// <param name="delta">DX and DY distance to move</param>
      public override void DeltaMove(Point? delta = null)
      {
         if (delta.HasValue)
         {
            X += delta.Value.X;
            Y += delta.Value.Y;
         }
         Canvas.SetLeft(_ellipse, X - Width / 2.0);
         Canvas.SetTop(_ellipse, Y - Height / 2.0);
      }

      /// <summary>
      /// Move Object to another position.
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
      public override void PointerEvent(
         GlPointerEvent poinerEvent, PointerPoint point = null)
      {
      }

      public override void Reshape(object node)
      {
      }

      /// <summary>
      /// Draw a filled circle centered on the given point.
      /// </summary>
      /// <param name="context">drawing context</param>
      /// <param name="centerX">center x</param>
      /// <param name="centerY">center y</param>
      /// <param name="diameter">circle diameter in pixels</param>
      /// <param name="fill">fill color</param>
      /// <returns>the created ellipse instance is returned</returns>
      public static GlEllipse Draw(
         GlContext context, double centerX, double centerY,
         double diameter, Color fill)
      {
         GlEllipse e = new GlEllipse();
         e.X = centerX;
         e.Y = centerY;
         e.Width = diameter;
         e.Height = diameter;

         e._ellipse.Width = diameter;
         e._ellipse.Height = diameter;
         e._ellipse.Fill = new SolidColorBrush(fill);
         e._ellipse.Stroke = new SolidColorBrush(Colors.Black);
         e._ellipse.StrokeThickness = 1;
         e._ellipse.Tag = e;

         Canvas.SetLeft(e._ellipse, centerX - diameter / 2.0);
         Canvas.SetTop(e._ellipse, centerY - diameter / 2.0);

         context.Instance.Children.Add(e._ellipse);
         return e;
      }

   }

}
