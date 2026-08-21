using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Geometry;
using ModelConsole.Graph;
using ModelConsole.ModelData;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

using SkiaSharp;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 003 - the reusable ERD composition API composes the full
   /// public-safety schema into a measured layout, 74 FK edges, and routed
   /// connectors that cross no table interior. Rendering the composed diagram
   /// to a bitmap exercises the Skia primitives end-to-end (no WinUI).
   /// </summary>
   public class ErdComposerTests
   {

      [Fact]
      public void ComposeYieldsExpectedCounts()
      {
         var diagram = ComposeDiagram();

         Assert.Equal(50, diagram.Layout.Count);
         Assert.Equal(74, diagram.Edges.Count);
         Assert.Equal(diagram.Edges.Count, diagram.Routes.Count);
         Assert.All(diagram.Routes, r => Assert.True(r.Count >= 2));
      }

      [Fact]
      public void LayoutPlacesEveryTableAtItsMeasuredSize()
      {
         var diagram = ComposeDiagram();

         foreach (var t in PublicSafetySchema.Tables)
         {
            Assert.True(diagram.Layout.ContainsKey(t.TableName),
               "missing layout for " + t.TableName);
            var rect = diagram.Layout[t.TableName];
            Assert.True(rect.Width > 0 && rect.Height > 0,
               t.TableName + " should have a positive measured size");
         }
      }

      [Fact]
      public void RoutesDoNotCrossTableInteriors()
      {
         var diagram = ComposeDiagram();
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
      public void ComposeRendersToBitmapWithoutThrowing()
      {
         var diagram = ComposeDiagram();
         double maxX = diagram.Layout.Values.Max(r => r.Right) + 80;
         double maxY = diagram.Layout.Values.Max(r => r.Bottom) + 80;

         using var surface = CreateSurface(
            (int)Math.Ceiling(maxX), (int)Math.Ceiling(maxY), out var bitmap);
         var frame = new GlFrame(surface);

         foreach (var kv in diagram.Layout)
         {
            var t = PublicSafetySchema.Tables.First(x => x.TableName == kv.Key);
            using var table = new Table(
               frame, (float)kv.Value.X, (float)kv.Value.Y, 40, t);
            table.DrawTable();
         }
         foreach (var route in diagram.Routes)
         {
            Connector.Draw(frame, route);
         }

         // The tables' light-gray fills cover large areas, so the bitmap
         // cannot be all white after the draw.
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
         Assert.True(anyColored, "the rendered diagram should not be blank");
      }

      [Fact]
      public void ComposeDrawsOnlyTheVisibleProjection()
      {
         var tables = TaggedSchema();
         var visibility = new EntityVisibility(new[] { "Core", "Audit" });
         visibility.SetGroupVisible("Audit", false);

         using var surface = SKSurface.Create(new SKImageInfo(4, 4));
         var frame = new GlFrame(surface);
         var diagram = ErdComposer.Compose(tables, frame, new ErdOptions(), visibility);

         Assert.Equal(2, diagram.Layout.Count);
         Assert.Contains("Orders", diagram.Layout.Keys);
         Assert.Contains("Customer", diagram.Layout.Keys);
         Assert.DoesNotContain("AuditLog", diagram.Layout.Keys);
         Assert.Single(diagram.Edges); // only Orders → Customer draws
         Assert.Single(diagram.Routes);
      }

      [Fact]
      public void ComposeWithoutVisibilityDrawsEverything()
      {
         var tables = TaggedSchema();

         using var surface = SKSurface.Create(new SKImageInfo(4, 4));
         var frame = new GlFrame(surface);
         var diagram = ErdComposer.Compose(tables, frame, new ErdOptions());

         Assert.Equal(3, diagram.Layout.Count);
         Assert.Equal(2, diagram.Edges.Count);
         Assert.Equal(2, diagram.Routes.Count);
      }

      [Fact]
      public void ComposeReportsIssuesForHiddenEdges()
      {
         // A dangling FK (R8) must still surface even when its tables would be
         // hidden — visibility never masks integrity (backlog 038 DoD).
         var tables = new[]
         {
            TableWithFk("Orders", "Customer", "Core"),
            TableWithFk("AuditLog", "Missing", "Audit"),
            TableWithFk("Customer", null, "Core")
         };
         var visibility = new EntityVisibility(new[] { "Core", "Audit" });
         visibility.SetGroupVisible("Audit", false);

         using var surface = SKSurface.Create(new SKImageInfo(4, 4));
         var frame = new GlFrame(surface);
         var diagram = ErdComposer.Compose(tables, frame, new ErdOptions(), visibility);

         Assert.DoesNotContain("AuditLog", diagram.Layout.Keys);
         Assert.Contains(diagram.Issues,
            i => i.Contains("Missing") || i.Contains("AuditLog"));
      }

      private static ErdDiagram ComposeDiagram()
      {
         using var surface = SKSurface.Create(new SKImageInfo(4, 4));
         var frame = new GlFrame(surface);
         return ErdComposer.Compose(PublicSafetySchema.Tables, frame,
            new ErdOptions());
      }

      /// <summary>
      /// A tiny tagged schema: Orders → Customer (Core), AuditLog → Orders
      /// (Audit). The visibility tests need real tags + FK constraints.
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
         var table = new TableInfo
         {
            TableName = name,
            Tags = new List<string> { tag },
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

      private static SKSurface CreateSurface(int w, int h, out SKBitmap bitmap)
      {
         bitmap = new SKBitmap(w, h);
         return SKSurface.Create(
            new SKImageInfo(w, h), bitmap.GetPixels(), bitmap.RowBytes);
      }

   }

}
