using Model.Data;
using ModelConsole.Graphics.GLibrary;
using ModelConsole.Graphics.Primitives;
using ModelConsole.Graph;

namespace ModelConsole.Graphics.Services
{
   /// <summary>
   /// Creates and draws <see cref="Table"/> primitives on the XAML graphics
   /// stack. Replaces direct calls to the static <c>Table.DrawTable</c>.
   /// </summary>
   public interface ITableFactory
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
      /// <returns>the created Table instance is returned</returns>
      Table Create(GlContext frame, float x, float y,
         float bannerHeight, TableInfo table,
         DiagramNotation notation = DiagramNotation.Erd);
   }
}
