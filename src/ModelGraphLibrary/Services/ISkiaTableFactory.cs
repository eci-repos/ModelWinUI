using Model.Data;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

namespace ModelConsole.Services
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
      /// <returns>the created Table instance is returned</returns>
      Table Create(GlFrame frame, float x, float y,
         float bannerHeight, TableInfo table);
   }
}
