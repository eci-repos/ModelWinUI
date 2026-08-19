using ModelConsole.Graphics.GLibrary;

namespace ModelConsole.Graphics.Services
{
   /// <summary>
   /// Creates and draws rectangle primitives on the XAML graphics stack.
   /// </summary>
   public class RectangleFactory : IRectangleFactory
   {
      /// <summary>
      /// Create a rectangle (not added to any canvas).
      /// </summary>
      public GlRectangle Create(double x, double y, double width, double height,
         double cornerRadius = 0)
      {
         return GlRectangle.Create(x, y, width, height, cornerRadius);
      }

      /// <summary>
      /// Create and draw a rectangle on the given context.
      /// </summary>
      public GlRectangle Draw(GlContext frame, double x, double y,
         double width, double height, double cornerRadius = 0)
      {
         return GlRectangle.Draw(frame, x, y, width, height, cornerRadius);
      }

      /// <summary>
      /// Add a title banner to a rectangle.
      /// </summary>
      public void AddBanner(GlContext frame, GlRectangle rectangle, string title)
      {
         GlRectangle.AddBanner(frame, rectangle, title);
      }
   }
}
