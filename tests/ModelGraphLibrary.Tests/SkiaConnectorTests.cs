using System.Collections.Generic;

using ModelConsole.Graph;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

using SkiaSharp;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 003 - the Skia Connector primitive strokes a routed polyline
   /// plus its endpoint markers onto a surface, and tolerates degenerate
   /// input.
   /// </summary>
   public class SkiaConnectorTests
   {

      [Fact]
      public void DrawStrokesPolylineAndEndpointMarkers()
      {
         using var surface = CreateSurface(400, 200, out var bitmap);
         var frame = new GlFrame(surface);

         var points = new List<Point2>
         {
            new Point2(20, 100), new Point2(200, 100), new Point2(380, 100)
         };
         Connector.Draw(frame, points);

         // Mid-segment pixel must be stroked (DodgerBlue, not the white
         // background).
         Assert.True(IsColored(bitmap.GetPixel(200, 100)),
            "mid-segment pixel should be painted");
         // Endpoint markers painted at both ends of the polyline.
         Assert.True(IsColored(bitmap.GetPixel(20, 100)),
            "start marker should be painted");
         Assert.True(IsColored(bitmap.GetPixel(380, 100)),
            "end marker should be painted");
      }

      [Fact]
      public void DrawWithNullOrEmptyPointsIsNoOp()
      {
         using var surface = CreateSurface(100, 100, out var bitmap);
         var frame = new GlFrame(surface);

         // Null, empty, and single-point input must all draw nothing and
         // never throw.
         Connector.Draw(frame, null);
         Connector.Draw(frame, new List<Point2>());
         Connector.Draw(frame, new List<Point2> { new Point2(10, 10) });

         for (int y = 0; y < 100; y += 10)
         {
            for (int x = 0; x < 100; x += 10)
            {
               Assert.True(bitmap.GetPixel(x, y) == SKColors.White,
                  "no points drawn for degenerate input");
            }
         }
      }

      private static bool IsColored(SKColor c)
      {
         // Not the white background (the frame clears to white).
         return c != SKColors.White;
      }

      private static SKSurface CreateSurface(int w, int h, out SKBitmap bitmap)
      {
         bitmap = new SKBitmap(w, h);
         return SKSurface.Create(
            new SKImageInfo(w, h), bitmap.GetPixels(), bitmap.RowBytes);
      }

   }

}
