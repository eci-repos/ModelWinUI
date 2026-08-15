using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SkiaSharp;
using ModelConsole.Skia.GLibrary;

namespace ModelConsole.Skia.Primitives
{

    public class RectangleHalf
    {
        private GlFrame surface;

        public RectangleHalf(GlFrame surface)
        {
            this.surface = surface;
        }

        public void Draw(
           float x, float y, float w, float h, float r, bool top = true)
        {
            var cw = SKColors.White;
            var c = GlPastelPalette.LightGreen;

            var fill = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = top ? c : cw
            };

            surface.Canvas.DrawRoundRect(x, y, w, h, r, r, fill);

            var oy = top ? y : y;
            surface.Canvas.DrawRect(x, oy, w, r, fill);

            fill.Dispose();
        }

        public void DrawTop(float x, float y, float w, float h, float r)
        {
            Draw(x, y, w, h, r, true);
        }

        public void DrawBottom(float x, float y, float w, float h, float r)
        {
            Draw(x, y, w, h, r, false);
        }

        public void DrawBorder(float x, float y, float w, float h, float r)
        {
            var fill = surface.DefaultStroke;

            surface.Canvas.DrawRoundRect(x, y, w, h, r, r, fill);
        }

    }

}
