using ModelConsole.Graphics.GLibrary;

namespace ModelConsole.Services
{
   /// <summary>
   /// Creates and draws rectangle primitives on the XAML graphics stack.
   /// Replaces direct calls to the static <c>GlRectangle</c> factory members.
   /// </summary>
   public interface IRectangleFactory
   {
      /// <summary>
      /// Create a rectangle (not added to any canvas).
      /// </summary>
      GlRectangle Create(double x, double y, double width, double height,
         double cornerRadius = 0);

      /// <summary>
      /// Create and draw a rectangle on the given context.
      /// </summary>
      GlRectangle Draw(GlContext frame, double x, double y,
         double width, double height, double cornerRadius = 0);

      /// <summary>
      /// Add a title banner to a rectangle.
      /// </summary>
      void AddBanner(GlContext frame, GlRectangle rectangle, string title);
   }
}
