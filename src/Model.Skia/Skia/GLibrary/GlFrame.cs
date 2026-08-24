using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using ModelConsole.Palette;

namespace ModelConsole.Skia.GLibrary
{

    /// <summary>
    /// Manage surface canvas defaults.
    /// </summary>
    public class GlFrame
    {
        public const float RADIANS_180DEGREES = 3.14159265f;

        private SKCanvas canvas;
        public SKPaint DefaultForeground { get; set; }
        public SKPaint DefaultBorder { get; set; }
        public SKPaint DefaultStroke { get; set; }
        public SKPaint DefaultLightStroke { get; set; }
        public SKPaint DefaultLightFill { get; set; }
        public SKFont DefaultFont { get; set; }
        public SKPaint DefaultTextPaint { get; set; }

        public float DefaultRoundCorderRadious = 10.0f;
        public float DefaultTextPanelPadding = 4.0f;

        public SKCanvas Canvas
        {
            get { return canvas; }
        }

        /// <summary>
        /// Initialize canvas and setup coordinate system by changing the
        /// origin on the left-bottom of the drawing area.
        /// </summary>
        /// <param name="surface">surface</param>
        /// <param name="backgroundColor">the surface clear color (default:
        /// white). The drawing surface background (backlog 041) comes from
        /// the host — e.g. the renderer-bar drop-down — so each paint clears
        /// to the chosen color.</param>
        public GlFrame(SKSurface surface, SKColor? backgroundColor = null)
        {
            canvas = surface.Canvas;
            canvas.Clear(backgroundColor ?? SKColors.White);
            canvas.GetDeviceClipBounds(out SKRectI b);
            //canvas.Scale(1, -1);
            //canvas.Translate(0, -b.Height);

            InitializeDefaults();
        }

        /// <summary>
        /// Wrap an existing <see cref="SKCanvas"/> (e.g. a PDF page canvas from
        /// <c>SKDocument.BeginPage</c>) instead of a surface's canvas. Used by
        /// the portable export path (backlog 054) so the same drawing code
        /// renders to a raster surface and a PDF page alike.
        /// </summary>
        /// <param name="canvas">the canvas to draw onto</param>
        /// <param name="backgroundColor">the clear color (default: white)</param>
        public GlFrame(SKCanvas canvas, SKColor? backgroundColor = null)
        {
            this.canvas = canvas;
            canvas.Clear(backgroundColor ?? SKColors.White);
            canvas.GetDeviceClipBounds(out SKRectI b);

            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            DefaultForeground = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColors.Black
            };

            DefaultBorder = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black
            };

            DefaultStroke = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Gray,
                StrokeWidth = 0.75f
            };

            DefaultLightStroke = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColor.Parse("#efefef"),
                StrokeWidth = 0.75f
            };

            DefaultLightFill = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColor.Parse("#efefef"),
                StrokeWidth = 0.75f
            };

            DefaultTextPaint = new SKPaint
            {
                IsAntialias = true,
                // The drawing-surface text is always on the light pastel
                // cards, so it renders in the shared palette's near-black —
                // the same pin the XAML stack applies (backlog 041 regression
                // fix: the XAML cards were using the theme-default foreground,
                // which goes white in dark OS mode and vanished).
                Color = SKColor.Parse(TablePalette.TextHex)
            };

            DefaultFont = new SKFont(
               SKTypeface.FromFamilyName("Arial"), 14);
        }

        /// <summary>
        /// Get transformation matrix that rotate the object around the center 
        /// of gravity.
        /// </summary>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <returns></returns>
        public static SKMatrix GetOriginTransformMatrix(float cx, float cy)
        {
            var ident = SKMatrix.Identity;
            var t1 = SKMatrix.CreateTranslation(-cx, -cy);
            var r1 = SKMatrix.CreateRotation(RADIANS_180DEGREES);
            var t2 = SKMatrix.CreateTranslation(cx, cy);

            var m1 = SKMatrix.Concat(ident, t1);
            var m2 = SKMatrix.Concat(r1, m1);
            var m3 = SKMatrix.Concat(t2, m2);
            return m3;
        }

        public void DrawRectFilled(float x, float y, float width, float height)
        {
            Canvas.DrawRect(x, y, width, height, DefaultForeground);
        }

        public void DrawRect(float x, float y, float width, float height)
        {
            Canvas.DrawRect(x, y, width, height, DefaultBorder);
        }

    }

}
