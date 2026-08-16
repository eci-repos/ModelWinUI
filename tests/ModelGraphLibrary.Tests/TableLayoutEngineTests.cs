using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Unit tests for <see cref="TableLayoutEngine"/>.
   /// </summary>
   public class TableLayoutEngineTests
   {

      private static readonly GridLayoutOptions Options = new()
      {
         Columns = 4,
         SlotWidth = 300,
         SlotHeight = 200,
         Gutter = 60
      };

      private static List<TableInfo> Tables(int count)
      {
         return Enumerable.Range(0, count)
            .Select(i => new TableInfo { TableName = "T" + i })
            .ToList();
      }

      [Fact]
      public void LaysOutRowMajor()
      {
         var layout = TableLayoutEngine.Layout(Tables(10), Options);

         // pitch includes the gutter
         double pitchX = 300 + 60;
         double pitchY = 200 + 60;

         Assert.Equal(new Rect2(0, 0, 300, 200), layout["T0"]);
         Assert.Equal(new Rect2(pitchX, 0, 300, 200), layout["T1"]);
         Assert.Equal(new Rect2(0, pitchY, 300, 200), layout["T4"]);
         Assert.Equal(new Rect2(pitchX, 2 * pitchY, 300, 200), layout["T9"]);
      }

      [Fact]
      public void NoTwoTablesOverlap()
      {
         var layout = TableLayoutEngine.Layout(Tables(20), Options);

         var rects = layout.Values.ToList();
         for (int i = 0; i < rects.Count; i++)
         {
            for (int j = i + 1; j < rects.Count; j++)
            {
               Assert.False(
                  rects[i].Intersects(rects[j]),
                  rects[i] + " overlaps " + rects[j]);
            }
         }
      }

      [Fact]
      public void FirstRowHoldsExactlyColumnsTables()
      {
         var layout = TableLayoutEngine.Layout(Tables(12), Options);

         Assert.Equal(Options.Columns, layout.Values.Count(r => r.Top == 0));
      }

      [Fact]
      public void IsDeterministic()
      {
         var l1 = TableLayoutEngine.Layout(Tables(9), Options);
         var l2 = TableLayoutEngine.Layout(Tables(9), Options);

         Assert.Equal(l1, l2);
      }

      [Fact]
      public void EmptyInputProducesEmptyLayout()
      {
         var layout = TableLayoutEngine.Layout(new List<TableInfo>(), Options);
         Assert.Empty(layout);
      }

   }

}
