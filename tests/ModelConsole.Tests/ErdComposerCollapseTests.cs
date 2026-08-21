using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Geometry;
using ModelConsole.Graph;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

using SkiaSharp;

using Xunit;

// The box model lives in Model.Graph; the drawable primitive in
// Model.Skia.Primitives — alias the primitive for the render test.
using SkiaGroupBox = ModelConsole.Skia.Primitives.GroupBox;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 039 — the Skia composer's collapse path: a collapsed group's
   /// members are replaced by one box rect in the layout, its FK edges
   /// aggregate to one connector per external target, and no connector
   /// crosses a collapsed box (the 012 invariant extended). The box model,
   /// aggregation, and collapse state themselves are covered by the pure
   /// GroupBox tests; this class verifies the composer wires them in.
   /// </summary>
   public class ErdComposerCollapseTests
   {

      [Fact]
      public void CollapsingAGroupReplacesItsMembersWithOneBox()
      {
         var tables = TaggedSchema();
         var collapse = new GroupCollapseState();
         collapse.SetCollapsed("Core", true);

         var diagram = Compose(tables, null, collapse);

         // Orders + Customer are Core members — hidden behind their box.
         Assert.Equal(2, diagram.Layout.Count);
         Assert.Contains(GroupBoxAggregation.BoxKey("Core"), diagram.Layout.Keys);
         Assert.Contains("AuditLog", diagram.Layout.Keys);
         Assert.DoesNotContain("Orders", diagram.Layout.Keys);
         Assert.DoesNotContain("Customer", diagram.Layout.Keys);

         var box = Assert.Single(diagram.Boxes.Values);
         Assert.Equal("Core", box.Group);
         Assert.Equal(2, box.MemberCount);

         // The one external edge (AuditLog → Orders) is a box edge now — no
         // table-level FK routes.
         Assert.Empty(diagram.Edges);
         Assert.Single(diagram.BoxEdges);
         Assert.Equal(diagram.Routes.Count, diagram.BoxEdges.Count);
      }

      [Fact]
      public void CollapsedBoxRoutesAroundItsOwnInterior()
      {
         var tables = TaggedSchema();
         var collapse = new GroupCollapseState();
         collapse.SetCollapsed("Core", true);

         var diagram = Compose(tables, null, collapse);
         var obstacles = diagram.Layout.Values.ToList();

         for (int i = 0; i < diagram.Routes.Count; i++)
         {
            var pts = diagram.Routes[i];
            for (int s = 0; s < pts.Count - 1; s++)
            {
               foreach (var o in obstacles)
               {
                  Assert.False(
                     Rect2.SegmentCrossesInterior(pts[s], pts[s + 1], o),
                     "route " + i + " segment " + pts[s] + " -> " +
                     pts[s + 1] + " crosses " + o);
               }
            }
         }
      }

      [Fact]
      public void CollapsedGroupHiddenByVisibilityDrawsNoBox()
      {
         var tables = TaggedSchema();
         var visibility = new EntityVisibility(new[] { "Core", "Audit" });
         visibility.SetGroupVisible("Core", false);
         var collapse = new GroupCollapseState();
         collapse.SetCollapsed("Core", true);

         var diagram = Compose(tables, visibility, collapse);

         Assert.Empty(diagram.Boxes);
         Assert.DoesNotContain(GroupBoxAggregation.BoxKey("Core"), diagram.Layout.Keys);
         Assert.Single(diagram.Layout.Keys); // only AuditLog remains visible
         Assert.True(diagram.Layout.ContainsKey("AuditLog"));
      }

      [Fact]
      public void BoxToBoxEdgeRoutesWhenBothGroupsCollapse()
      {
         var tables = new[]
         {
            TableWithFk("Orders", "AuditLog", "Core"),
            TableWithFk("Customer", "AuditEntry", "Core"),
            TableWithFk("AuditLog", null, "Audit"),
            TableWithFk("AuditEntry", null, "Audit")
         };
         var collapse = new GroupCollapseState();
         collapse.SetCollapsed("Core", true);
         collapse.SetCollapsed("Audit", true);

         var diagram = Compose(tables, null, collapse);

         Assert.Equal(2, diagram.Layout.Count);
         Assert.Contains(GroupBoxAggregation.BoxKey("Core"), diagram.Layout.Keys);
         Assert.Contains(GroupBoxAggregation.BoxKey("Audit"), diagram.Layout.Keys);
         Assert.Empty(diagram.Edges); // every edge became a box↔box connector
         Assert.Equal(2, diagram.BoxEdges.Count);
         Assert.Equal(2, diagram.Routes.Count);

         // One connector each direction, deduplicated per target box with the
         // shared count.
         Assert.All(diagram.BoxEdges,
            e => Assert.NotNull(e.TargetGroup));
         Assert.All(diagram.BoxEdges,
            e => Assert.Equal(2, e.Count));
      }

      [Fact]
      public void NullCollapseIsPre039()
      {
         var diagram = Compose(TaggedSchema(), null, null);

         Assert.Empty(diagram.Boxes);
         Assert.Empty(diagram.BoxEdges);
         Assert.Equal(3, diagram.Layout.Count);
         Assert.Equal(2, diagram.Edges.Count);
         Assert.Equal(diagram.Edges.Count, diagram.Routes.Count);
      }

      [Fact]
      public void CollapsedComposeRendersBoxesToBitmap()
      {
         var tables = TaggedSchema();
         var collapse = new GroupCollapseState();
         collapse.SetCollapsed("Core", true);

         var diagram = Compose(tables, null, collapse);
         double maxX = diagram.Layout.Values.Max(r => r.Right) + 80;
         double maxY = diagram.Layout.Values.Max(r => r.Bottom) + 80;

         using var surface = CreateSurface(
            (int)System.Math.Ceiling(maxX), (int)System.Math.Ceiling(maxY), out var bitmap);
         var frame = new GlFrame(surface);

         foreach (var kv in diagram.Layout)
         {
            if (diagram.Boxes.TryGetValue(kv.Key, out var box))
            {
               SkiaGroupBox.Draw(frame, (float)kv.Value.X, (float)kv.Value.Y,
                  box.Group, box.MemberCount);
            }
            else
            {
               var t = TaggedSchema().First(x => x.TableName == kv.Key);
               using var table = new Table(
                  frame, (float)kv.Value.X, (float)kv.Value.Y, 40, t);
               table.DrawTable();
            }
         }
         foreach (var route in diagram.Routes)
         {
            Connector.Draw(frame, route);
         }

         bool anyColored = false;
         for (int y = 0; y < bitmap.Height && !anyColored; y += 40)
         {
            for (int x = 0; x < bitmap.Width && !anyColored; x += 40)
            {
               if (bitmap.GetPixel(x, y) != SKColors.White)
               {
                  anyColored = true;
               }
            }
         }
         Assert.True(anyColored, "the rendered collapsed diagram should not be blank");
      }

      [Fact]
      public void SchemaThemeCollapseBoxesTheSchemasMembers()
      {
         // Backlog 043: the composer's collapse path is theme-aware — with the
         // schema theme, collapsing "Sales" boxes the Sales tables (Orders +
         // Customer) and the cross-schema edge (StockItem → Orders) becomes a
         // box edge, exactly as the tag theme boxes a tag's members.
         var tables = new[]
         {
            SchemaTableWithFk("Orders", "Customer", "Sales"),
            SchemaTableWithFk("Customer", null, "Sales"),
            SchemaTableWithFk("StockItem", "Orders", "Inventory")
         };
         var collapse = new GroupCollapseState();
         collapse.SetCollapsed("Sales", true);

         var diagram = Compose(tables, null, collapse, GroupingThemes.Schema);

         Assert.Equal(2, diagram.Layout.Count);
         Assert.Contains(GroupBoxAggregation.BoxKey("Sales"), diagram.Layout.Keys);
         Assert.Contains("StockItem", diagram.Layout.Keys);
         Assert.DoesNotContain("Orders", diagram.Layout.Keys);
         Assert.DoesNotContain("Customer", diagram.Layout.Keys);

         var box = Assert.Single(diagram.Boxes.Values);
         Assert.Equal("Sales", box.Group);
         Assert.Equal(2, box.MemberCount);

         // The one cross-schema edge (StockItem → Orders) is a box edge now.
         Assert.Empty(diagram.Edges);
         Assert.Single(diagram.BoxEdges);
      }

      // ------------------------------------------------------------------

      private static ErdDiagram Compose(
         IReadOnlyList<TableInfo> tables, EntityVisibility visibility,
         GroupCollapseState collapse, GroupingTheme theme = null)
      {
         using var surface = SKSurface.Create(new SKImageInfo(4, 4));
         var frame = new GlFrame(surface);
         return ErdComposer.Compose(tables, frame, new ErdOptions(),
            visibility, collapse, theme);
      }

      /// <summary>
      /// Orders → Customer (Core), AuditLog → Orders (Audit), Customer (Core)
      /// — the tiny tagged schema the aggregation tests share.
      /// </summary>
      private static IReadOnlyList<TableInfo> TaggedSchema()
      {
         return new[]
         {
            TableWithFk("Orders", "Customer", "Core"),
            TableWithFk("AuditLog", "Orders", "Audit"),
            TableWithFk("Customer", null, "Core")
         };
      }

      private static TableInfo TableWithFk(string name, string parent, string tag)
      {
         return TableWithFk(name, parent, tag, null);
      }

      private static TableInfo TableWithFk(
         string name, string parent, string tag, string schema)
      {
         var table = new TableInfo
         {
            TableName = name,
            SchemaName = schema,
            Tags = tag != null ? new List<string> { tag } : null,
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
            var fk = new ColumnInfo
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
            };
            table.Columns.Add(fk);
         }
         return table;
      }

      private static TableInfo SchemaTableWithFk(string name, string parent, string schema)
      {
         return TableWithFk(name, parent, null, schema);
      }

      private static SKSurface CreateSurface(int w, int h, out SKBitmap bitmap)
      {
         bitmap = new SKBitmap(w, h);
         return SKSurface.Create(
            new SKImageInfo(w, h), bitmap.GetPixels(), bitmap.RowBytes);
      }

   }

}
