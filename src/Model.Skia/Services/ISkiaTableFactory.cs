using Model.Data;
using ModelConsole.Graph;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

namespace ModelConsole.Skia.Services
{
   /// <summary>
   /// Creates and draws <see cref="Table"/> primitives on the Skia graphics
   /// stack. The Skia <c>Table</c> is a different type from the XAML one, so
   /// it gets its own factory; this keeps the Skia stack free of WinUI
   /// dependencies while still being DI-wired.
   /// </summary>
   public interface ISkiaTableFactory
   {
      /// <summary>
      /// Create and draw a Table primitive.
      /// </summary>
      /// <param name="frame">drawing context</param>
      /// <param name="x">x lower-left</param>
      /// <param name="y">y lower-left</param>
      /// <param name="bannerHeight">top banner height</param>
      /// <param name="table">table information</param>
      /// <param name="notation">ERD or UML presentation mode</param>
      /// <param name="hovered">when true, the border draws the hovered accent
      /// (backlog 041); false (default) draws the rest-state border</param>
      /// <returns>the created Table instance is returned</returns>
      Table Create(GlFrame frame, float x, float y,
         float bannerHeight, TableInfo table,
         DiagramNotation notation = DiagramNotation.Erd, bool hovered = false);
   }
}
