using Model.Data;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

using SkiaSharp;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 003 - the Skia Table exposes its measured size and row centers
   /// (parity with the XAML Table) so tables can be laid out and connectors
   /// anchored before anything is drawn.
   /// </summary>
   public class SkiaTableTests
   {

      [Fact]
      public void ComputedSizeIsPositiveForFixture()
      {
         using var surface = SKSurface.Create(new SKImageInfo(10, 10));
         var frame = new GlFrame(surface);
         using var table = new Table(frame, 0, 0, 40, MakeTable());

         Assert.True(table.ComputedWidth > 0);
         Assert.True(table.ComputedHeight > 0);
      }

      [Fact]
      public void GetRowCenterYReturnsMatchedRowCenter()
      {
         using var surface = SKSurface.Create(new SKImageInfo(10, 10));
         var frame = new GlFrame(surface);
         using var table = new Table(frame, 0, 0, 40, MakeTable());

         // First row center: banner(40) + corner + padding/2 + rowHeight/2,
         // where rowHeight = font.Size + 3 * padding.
         float expected = 0 + 40 + frame.DefaultRoundCorderRadious +
            frame.DefaultTextPanelPadding / 2.0f +
            (frame.DefaultFont.Size + frame.DefaultTextPanelPadding +
               frame.DefaultTextPanelPadding * 2) / 2.0f;
         Assert.Equal(expected, table.GetRowCenterY("ID"), 3);

         // A later column sits lower than the first.
         Assert.True(table.GetRowCenterY("Code") > table.GetRowCenterY("ID"));
      }

      [Fact]
      public void GetRowCenterYFallsBackForUnknownColumn()
      {
         using var surface = SKSurface.Create(new SKImageInfo(10, 10));
         var frame = new GlFrame(surface);
         using var table = new Table(frame, 0, 0, 40, MakeTable());

         // The probe sits at y = 0, so the fallback is the table's vertical
         // midpoint.
         Assert.Equal(table.ComputedHeight / 2.0f,
            table.GetRowCenterY("NoSuchColumn"), 3);
      }

      private static TableInfo MakeTable()
      {
         var columns = new ColumnList();
         var pk = columns.Add("t", "ID", 20);
         pk.IsKey = true;
         columns.Add("t", "Name", 40);
         columns.Add("t", "Code", 10);
         return new TableInfo
         {
            SchemaName = "t",
            TableName = "Sample",
            Columns = columns
         };
      }

   }

}
