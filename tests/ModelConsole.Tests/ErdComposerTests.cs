using System;
using System.Linq;

using ModelConsole.Geometry;
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

      private static ErdDiagram ComposeDiagram()
      {
         using var surface = SKSurface.Create(new SKImageInfo(4, 4));
         var frame = new GlFrame(surface);
         return ErdComposer.Compose(PublicSafetySchema.Tables, frame,
            new ErdOptions());
      }

      private static SKSurface CreateSurface(int w, int h, out SKBitmap bitmap)
      {
         bitmap = new SKBitmap(w, h);
         return SKSurface.Create(
            new SKImageInfo(w, h), bitmap.GetPixels(), bitmap.RowBytes);
      }

   }

}
