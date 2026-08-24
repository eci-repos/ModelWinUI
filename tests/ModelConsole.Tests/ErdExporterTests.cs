using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Skia.Primitives;

using SkiaSharp;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 054 — the portable full-diagram export path (<see cref="ErdExporter"/>):
   /// compose an ERD and render it at full size to a PNG raster (and, in the
   /// PDF tests, a PDF page) through the shared Skia composition path.
   /// </summary>
   public class ErdExporterTests
   {

      [Fact]
      public void ToPngProducesNonBlankImageSizedToDiagram()
      {
         var tables = Schema();
         var options = new ErdExportOptions { Padding = 40 };

         byte[] png = ErdExporter.ToPng(tables, options);

         Assert.NotEmpty(png);
         using var bitmap = SKBitmap.Decode(png);
         Assert.NotNull(bitmap);

         // The PNG is sized to the composed diagram bounds + padding, so the
         // full diagram (tables + connectors) fits with no clipping.
         var diagram = ErdExporter.Compose(tables, options);
         var (w, h) = ErdExporter.GetSize(diagram, options.Padding);
         Assert.Equal(w, bitmap.Width);
         Assert.Equal(h, bitmap.Height);

         bool anyColored = false;
         for (int y = 0; y < bitmap.Height && !anyColored; y += 20)
         {
            for (int x = 0; x < bitmap.Width && !anyColored; x += 20)
            {
               if (bitmap.GetPixel(x, y) != SKColors.White)
               {
                  anyColored = true;
               }
            }
         }
         Assert.True(anyColored, "the exported PNG should not be blank");
      }

      [Fact]
      public void EmptyModelExportsEmptyPng()
      {
         byte[] png = ErdExporter.ToPng(new TableInfo[0]);

         Assert.Empty(png);
      }

      [Fact]
      public void GetSizeIsZeroForEmptyDiagram()
      {
         var diagram = ErdExporter.Compose(new TableInfo[0]);

         Assert.Equal((0, 0), ErdExporter.GetSize(diagram, 40));
      }

      // ------------------------------------------------------------------

      private static IReadOnlyList<TableInfo> Schema()
      {
         return new[]
         {
            TableWithFk("Orders", "Customer"),
            TableWithFk("Customer", null)
         };
      }

      private static TableInfo TableWithFk(string name, string parent)
      {
         var table = new TableInfo
         {
            TableName = name,
            Columns = new List<ColumnInfo>
            {
               new ColumnInfo
               {
                  ColumnName = "Id",
                  Type = DataInfo.VARCHAR,
                  IsKey = true,
                  Constraints = new List<ConstraintInfo>
                  {
                     new ConstraintInfo { Type = DataInfo.PRIMARY_KEY }
                  }
               }
            }
         };
         if (parent != null)
         {
            table.Columns.Add(new ColumnInfo
            {
               ColumnName = parent + "Id",
               Type = DataInfo.VARCHAR,
               IsForeignKey = true,
               Constraints = new List<ConstraintInfo>
               {
                  new ConstraintInfo
                  {
                     Type = DataInfo.FOREIGN_KEY,
                     ReferencedTableName = parent,
                     ReferencedColumnName = "Id"
                  }
               }
            });
         }
         return table;
      }

   }

}
