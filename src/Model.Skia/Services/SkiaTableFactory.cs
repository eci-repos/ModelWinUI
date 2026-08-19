using Model.Data;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

namespace ModelConsole.Skia.Services
{
   /// <summary>
   /// Creates and draws <see cref="Table"/> primitives on the Skia graphics
   /// stack.
   /// </summary>
   public class SkiaTableFactory : ISkiaTableFactory
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
      public Table Create(GlFrame frame, float x, float y,
         float bannerHeight, TableInfo table)
      {
         Table t = new Table(frame, x, y, bannerHeight, table);
         t.DrawTable();
         return t;
      }
   }
}
