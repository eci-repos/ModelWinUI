using System.Collections.Generic;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// Options controlling the grid layout of tables.
   /// </summary>
   public sealed class GridLayoutOptions
   {
      /// <summary>Number of columns in the grid (row-major fill).</summary>
      public int Columns { get; set; } = 1;

      /// <summary>Width of every grid cell.</summary>
      public double SlotWidth { get; set; } = 300;

      /// <summary>Height of every grid cell.</summary>
      public double SlotHeight { get; set; } = 200;

      /// <summary>Horizontal and vertical spacing between cells.</summary>
      public double Gutter { get; set; } = 60;
   }

   /// <summary>
   /// Places tables in a deterministic row-major grid so no two tables
   /// overlap. Pure and portable (no UI dependency).
   /// </summary>
   public static class TableLayoutEngine
   {

      /// <summary>
      /// Lay the tables out in a row-major grid keyed by table name.
      /// </summary>
      /// <param name="tables">tables to place (order preserved)</param>
      /// <param name="options">grid options</param>
      /// <returns>a table-name to rect mapping</returns>
      public static IReadOnlyDictionary<string, Rect2> Layout(
         IReadOnlyList<TableInfo> tables, GridLayoutOptions options)
      {
         var result = new Dictionary<string, Rect2>();

         if (tables == null || options == null)
         {
            return result;
         }

         int columns = options.Columns < 1 ? 1 : options.Columns;
         double pitchX = options.SlotWidth + options.Gutter;
         double pitchY = options.SlotHeight + options.Gutter;

         for (int i = 0; i < tables.Count; i++)
         {
            if (tables[i] == null)
            {
               continue;
            }

            int col = i % columns;
            int row = i / columns;
            double x = col * pitchX;
            double y = row * pitchY;

            result[tables[i].TableName] =
               new Rect2(x, y, options.SlotWidth, options.SlotHeight);
         }

         return result;
      }

   }

}
