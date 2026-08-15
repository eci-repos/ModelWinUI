using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Devices.Bluetooth.Advertisement;

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
        public SKPaint DefaultFont { get; set; }

        public float DefaultRoundCorderRadious = 10.0f;
        public float DefaultTextPanelPadding = 4.0f;

        public SKCanvas Canvas
        {
            get { return canvas; }
        }

        /// <summary>
        /// Initialize canvas with white color and setup coordinate system by
        /// changing the origin on the left-bottom of the drawing area.
        /// </summary>
        /// <param name="surface">surface</param>
        public GlFrame(SKSurface surface)
        {
            canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.GetDeviceClipBounds(out SKRectI b);
            //canvas.Scale(1, -1);
            //canvas.Translate(0, -b.Height);

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

            DefaultFont = new SKPaint
            {
                IsAntialias = true,
                TextSize = 14,
                Color = SKColors.Black,
                Typeface = SKTypeface.FromFamilyName("Arial")
            };

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
