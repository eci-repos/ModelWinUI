using ModelConsole.Skia.GLibrary;

using SkiaSharp;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 041 - the GlFrame clears its surface to the drawing-surface
   /// background color, so the Skia renderer honors the renderer-bar "Base:"
   /// drop-down on every paint (white by default).
   /// </summary>
   public class GlFrameTests
   {

      [Fact]
      public void ClearsToWhiteByDefault()
      {
         using var surface = CreateSurface(4, 4, out var bitmap);
         var frame = new GlFrame(surface);

         // Every pixel is the default white (nothing drawn, just the clear).
         Assert.Equal(SKColors.White, bitmap.GetPixel(0, 0));
         Assert.Equal(SKColors.White, bitmap.GetPixel(3, 3));
      }

      [Fact]
      public void ClearsToProvidedBackground()
      {
         using var surface = CreateSurface(4, 4, out var bitmap);
         var frame = new GlFrame(surface, SKColor.Parse("#F0FFF0"));

         // The host's background color reaches the surface clear.
         Assert.Equal(SKColor.Parse("#F0FFF0"), bitmap.GetPixel(0, 0));
         Assert.Equal(SKColor.Parse("#F0FFF0"), bitmap.GetPixel(3, 3));
      }

      private static SKSurface CreateSurface(int w, int h, out SKBitmap bitmap)
      {
         bitmap = new SKBitmap(w, h);
         return SKSurface.Create(
            new SKImageInfo(w, h), bitmap.GetPixels(), bitmap.RowBytes);
      }

   }

}
