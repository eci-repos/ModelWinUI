using System.Collections.Generic;

using ModelConsole.Geometry;
using ModelConsole.Graph;
using ModelConsole.Palette;
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
      public void DrawEmphasizedThickensStrokeAndMarkers()
      {
         using var surface = CreateSurface(400, 200, out var bitmap);
         var frame = new GlFrame(surface);

         var points = new List<Point2>
         {
            new Point2(20, 100), new Point2(200, 100), new Point2(380, 100)
         };
         new Connector(points) { Emphasized = true }.Draw(frame);

         // The emphasized line is SlateBlue — the analogous violet neighbor
         // of the rest-state DodgerBlue, so the hovered connector stands out
         // from the connector tangle. Asserted exactly (a dead-center pixel of
         // the 3.5 px stroke is fully covered).
         Assert.Equal(SKColor.Parse("#6A5ACD"), bitmap.GetPixel(200, 100));
         // The 3.5 px emphasized stroke reaches a pixel the 1.5 px rest
         // stroke would leave white.
         Assert.True(IsColored(bitmap.GetPixel(200, 101)),
            "emphasized stroke should be thicker");
         // The 3.5 px emphasized stroke reaches a pixel the 1.5 px rest
         // stroke would leave white.
         Assert.True(IsColored(bitmap.GetPixel(200, 101)),
            "emphasized stroke should be thicker");
         // The radius-6 emphasized endpoint marker reaches a pixel the
         // radius-4 rest marker would leave white.
         Assert.True(IsColored(bitmap.GetPixel(20, 105)),
            "emphasized start marker should be larger");
         Assert.True(IsColored(bitmap.GetPixel(380, 105)),
            "emphasized end marker should be larger");
      }

      [Fact]
      public void DrawEmphasizedUsesSelectedConnectorStyle()
      {
         using var surface = CreateSurface(400, 200, out var bitmap);
         var frame = new GlFrame(surface);

         var points = new List<Point2>
         {
            new Point2(20, 100), new Point2(200, 100), new Point2(380, 100)
         };
         new Connector(points)
         {
            Emphasized = true,
            SelectedStyle = new ConnectorStyle("#FF00AA", 6)
         }.Draw(frame);

         Assert.Equal(SKColor.Parse("#FF00AA"), bitmap.GetPixel(200, 100));
         Assert.True(IsColored(bitmap.GetPixel(200, 102)),
            "custom emphasized stroke width should be honored");
         Assert.Equal(SKColor.Parse("#FF00AA"), bitmap.GetPixel(20, 100));
      }

      [Fact]
      public void DrawCrowFootMarkersUsesEndpointCardinalitySymbols()
      {
         using var surface = CreateSurface(400, 200, out var bitmap);
         var frame = new GlFrame(surface);

         var points = new List<Point2>
         {
            new Point2(20, 100), new Point2(200, 100), new Point2(380, 100)
         };
         new Connector(points)
         {
            StartMarker = new CrowFootNotation.CardinalityMarker(
               optional: true, one: false, many: true),
            EndMarker = CrowFootNotation.CardinalityMarker.RequiredOne
         }.Draw(frame);

         Assert.True(IsColored(bitmap.GetPixel(23, 100)),
            "optional circle stroke should be painted near the start endpoint");
         Assert.True(IsColored(bitmap.GetPixel(40, 103)),
            "crow-foot prongs should be painted near the start endpoint");
         Assert.True(IsColored(bitmap.GetPixel(373, 105)),
            "required-one bar should be painted near the end endpoint");
      }

      [Fact]
      public void ConnectorStyleNormalizesColorAndClampsWidth()
      {
         var style = new ConnectorStyle("00aa11", 20);

         Assert.Equal("#00AA11", style.SelectedHex);
         Assert.Equal(ConnectorStyle.MaxSelectedWidth, style.SelectedWidth);

         var fallback = new ConnectorStyle("not-a-color", double.NaN);
         Assert.Equal(ConnectorStyle.DefaultSelectedHex, fallback.SelectedHex);
         Assert.Equal(ConnectorStyle.DefaultSelectedWidth, fallback.SelectedWidth);
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
