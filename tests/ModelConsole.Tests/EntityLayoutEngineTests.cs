using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Geometry;
using ModelConsole.Graph;
using ModelConsole.ModelData;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Unit tests for <see cref="EntityLayoutEngine"/>.
   /// </summary>
   public class EntityLayoutEngineTests
   {

      private static readonly EntityLayoutOptions Options = new()
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

      private static FkRelation Edge(string child, string parent)
      {
         return new FkRelation(child, "Id", parent, "Id");
      }

      [Fact]
      public void GridDefaultLaysOutRowMajor()
      {
         var layout = EntityLayoutEngine.Layout(
            Tables(10), Array.Empty<FkRelation>(), Options);

         double pitchX = 300 + 60;
         double pitchY = 200 + 60;

         Assert.Equal(new Rect2(0, 0, 300, 200), layout["T0"]);
         Assert.Equal(new Rect2(pitchX, 0, 300, 200), layout["T1"]);
         Assert.Equal(new Rect2(0, pitchY, 300, 200), layout["T4"]);
         Assert.Equal(new Rect2(pitchX, 2 * pitchY, 300, 200), layout["T9"]);
      }

      [Fact]
      public void FirstRowHoldsExactlyColumnsTables()
      {
         var layout = EntityLayoutEngine.Layout(
            Tables(12), Array.Empty<FkRelation>(), Options);

         Assert.Equal(Options.Columns, layout.Values.Count(r => r.Top == 0));
      }

      [Fact]
      public void EmptyInputProducesEmptyLayout()
      {
         var layout = EntityLayoutEngine.Layout(
            new List<TableInfo>(), Array.Empty<FkRelation>(), Options);
         Assert.Empty(layout);
      }

      [Fact]
      public void FromNameFallsBackToGrid()
      {
         Assert.Same(EntityLayout.Grid, EntityLayout.FromName("missing"));
         Assert.Same(EntityLayout.Serpentine,
            EntityLayout.FromName(EntityLayout.SerpentineName));
         Assert.Same(EntityLayout.Circle,
            EntityLayout.FromName(EntityLayout.CircleName));
         Assert.Same(EntityLayout.Cross,
            EntityLayout.FromName(EntityLayout.CrossName));
      }

      [Fact]
      public void ConnectivityOrderingIsDeterministic()
      {
         var tables = Tables(7);
         var edges1 = new[]
         {
            Edge("T2", "T3"),
            Edge("T0", "T1"),
            Edge("T1", "T2"),
            Edge("T5", "T6")
         };
         var edges2 = edges1.Reverse().ToArray();

         var order1 = EntityLayoutEngine.OrderEntities(tables, edges1);
         var order2 = EntityLayoutEngine.OrderEntities(tables, edges2);
         var ordered = order1.ToList();

         Assert.Equal(order1, order2);
         Assert.True(ordered.IndexOf("T0") < ordered.IndexOf("T5"));
         Assert.True(ordered.IndexOf("T1") < ordered.IndexOf("T5"));
      }

      [Theory]
      [InlineData(EntityLayout.GridName)]
      [InlineData(EntityLayout.SerpentineName)]
      [InlineData(EntityLayout.CircleName)]
      [InlineData(EntityLayout.CrossName)]
      public void LayoutsDoNotOverlap(string layoutName)
      {
         var (edges, _) = FkEdgeExtractor.Extract(PublicSafetySchema.Tables);
         var layout = EntityLayoutEngine.Layout(
            PublicSafetySchema.Tables, edges, Options, EntityLayout.FromName(layoutName));

         AssertNoOverlap(layout.Values.ToList());
      }

      [Fact]
      public void SerpentineAlternatesRowDirection()
      {
         var layout = EntityLayoutEngine.Layout(Tables(8), Array.Empty<FkRelation>(),
            Options, EntityLayout.Serpentine);

         double pitchX = 300 + 60;
         double pitchY = 200 + 60;

         Assert.Equal(new Rect2(0, 0, 300, 200), layout["T0"]);
         Assert.Equal(new Rect2(3 * pitchX, 0, 300, 200), layout["T3"]);
         Assert.Equal(new Rect2(3 * pitchX, pitchY, 300, 200), layout["T4"]);
         Assert.Equal(new Rect2(0, pitchY, 300, 200), layout["T7"]);
      }

      [Fact]
      public void CircleUsesFilledInteriorCells()
      {
         var options = new EntityLayoutOptions
         {
            Columns = 4,
            SlotWidth = 100,
            SlotHeight = 100,
            Gutter = 20
         };

         var layout = EntityLayoutEngine.Layout(
            Tables(25), Array.Empty<FkRelation>(), options, EntityLayout.Circle);

         var centers = layout.Values.Select(r => r.Center).ToList();
         var center = FootprintCenter(layout.Values);
         double maxDistance = centers.Max(p => Distance(p, center));
         int interiorCount = centers.Count(p => Distance(p, center) < maxDistance * 0.66);
         int radiusBands = centers
            .Select(p => Math.Round(Distance(p, center) / 10.0))
            .Distinct()
            .Count();

         Assert.True(interiorCount >= 5,
            "Circle should fill interior cells, not only perimeter cells.");
         Assert.True(radiusBands >= 3,
            "Circle should occupy multiple radius bands.");
      }

      [Fact]
      public void CirclePlacesConnectedHubNearCenter()
      {
         var tables = Tables(9);
         var edges = Enumerable.Range(1, 8)
            .Select(i => Edge("T" + i, "T0"))
            .ToArray();

         var layout = EntityLayoutEngine.Layout(
            tables, edges, Options, EntityLayout.Circle);
         var center = FootprintCenter(layout.Values);
         double hubDistance = Distance(layout["T0"].Center, center);
         double nearestDistance = layout.Values.Min(r => Distance(r.Center, center));

         Assert.Equal(nearestDistance, hubDistance, precision: 6);
      }

      [Fact]
      public void CrossUsesFilledBarCells()
      {
         var options = new EntityLayoutOptions
         {
            Columns = 4,
            SlotWidth = 100,
            SlotHeight = 100,
            Gutter = 20
         };

         var layout = EntityLayoutEngine.Layout(
            Tables(25), Array.Empty<FkRelation>(), options, EntityLayout.Cross);

         var center = FootprintCenter(layout.Values);
         int offLineCells = layout.Values.Count(r =>
            Math.Abs(r.Center.X - center.X) > 0.0001 &&
            Math.Abs(r.Center.Y - center.Y) > 0.0001);

         Assert.True(offLineCells >= 8,
            "Cross should fill rectangular bars, not only center row/column arms.");
      }

      [Fact]
      public void CrossPlacesConnectedHubNearCenter()
      {
         var tables = Tables(9);
         var edges = Enumerable.Range(1, 8)
            .Select(i => Edge("T" + i, "T0"))
            .ToArray();

         var layout = EntityLayoutEngine.Layout(
            tables, edges, Options, EntityLayout.Cross);
         var center = FootprintCenter(layout.Values);
         double hubDistance = Distance(layout["T0"].Center, center);
         double nearestDistance = layout.Values.Min(r => Distance(r.Center, center));

         Assert.Equal(nearestDistance, hubDistance, precision: 6);
      }

      [Fact]
      public void ConnectivityOrderedGridShortensPublicSafetyEdgeSpan()
      {
         var (edges, _) = FkEdgeExtractor.Extract(PublicSafetySchema.Tables);
         var blind = EntityLayoutEngine.Layout(
            PublicSafetySchema.Tables, edges, Options, EntityLayout.Grid);
         var ordered = EntityLayoutEngine.Layout(
            PublicSafetySchema.Tables, edges, new EntityLayoutOptions
            {
               Columns = Options.Columns,
               SlotWidth = Options.SlotWidth,
               SlotHeight = Options.SlotHeight,
               Gutter = Options.Gutter,
               UseConnectivityOrdering = true
            }, EntityLayout.Grid);

         double blindSpan = EdgeSpan(blind, edges);
         double orderedSpan = EdgeSpan(ordered, edges);
         Assert.True(orderedSpan < blindSpan,
            "ordered " + orderedSpan + " should be less than blind " + blindSpan);
      }

      [Theory]
      [InlineData(EntityLayout.GridName)]
      [InlineData(EntityLayout.SerpentineName)]
      [InlineData(EntityLayout.CircleName)]
      [InlineData(EntityLayout.CrossName)]
      public void CollapsedBoxKeysLayOutAsOneRect(string layoutName)
      {
         var tables = new[]
         {
            new TableInfo { TableName = "Person" },
            new TableInfo { TableName = "group::Core" },
            new TableInfo { TableName = "Incident" }
         };
         var edges = new[]
         {
            Edge("group::Core", "Incident"),
            Edge("Person", "group::Core")
         };

         var layout = EntityLayoutEngine.Layout(
            tables, edges, Options, EntityLayout.FromName(layoutName));

         Assert.Equal(3, layout.Count);
         Assert.Contains("group::Core", layout.Keys);
         AssertNoOverlap(layout.Values.ToList());
      }

      private static double EdgeSpan(
         IReadOnlyDictionary<string, Rect2> layout, IReadOnlyList<FkRelation> edges)
      {
         double total = 0;
         foreach (var edge in edges)
         {
            if (layout.TryGetValue(edge.ChildTable, out var child) &&
                layout.TryGetValue(edge.ParentTable, out var parent))
            {
               total += Math.Abs(child.Center.X - parent.Center.X) +
                  Math.Abs(child.Center.Y - parent.Center.Y);
            }
         }
         return total;
      }

      private static Point2 FootprintCenter(IEnumerable<Rect2> rects)
      {
         var list = rects.ToList();
         double minX = list.Min(r => r.Center.X);
         double maxX = list.Max(r => r.Center.X);
         double minY = list.Min(r => r.Center.Y);
         double maxY = list.Max(r => r.Center.Y);
         return new Point2((minX + maxX) / 2, (minY + maxY) / 2);
      }

      private static double Distance(Point2 a, Point2 b)
      {
         double dx = a.X - b.X;
         double dy = a.Y - b.Y;
         return Math.Sqrt(dx * dx + dy * dy);
      }

      private static void AssertNoOverlap(IReadOnlyList<Rect2> rects)
      {
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
   }

}
