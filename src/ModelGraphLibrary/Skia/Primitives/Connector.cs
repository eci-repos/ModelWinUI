using System.Collections.Generic;

using ModelConsole.Graph;
using ModelConsole.Skia.GLibrary;

using SkiaSharp;

namespace ModelConsole.Skia.Primitives
{

    /// <summary>
    /// Draw a constraint connector as a polyline on the Skia stack. This is
    /// the Skia counterpart of the XAML <c>GlOrthoPath.DrawRouted</c> + its
    /// <c>GlEllipse</c> endpoint markers: a static, pre-computed route (the
    /// output of <see cref="OrthogonalRouter"/>) is stroked as-is, with no
    /// corner rounding and no grips.
    /// </summary>
    public class Connector
    {
        /// <summary>
        /// Radius of the endpoint marker circles, in pixels (the XAML path
        /// draws 8 px diameter markers).
        /// </summary>
        public const float EndpointRadius = 4;

        /// <summary>
        /// Endpoint marker radius while <see cref="Emphasized"/> (a hover
        /// highlight makes start and end unambiguous).
        /// </summary>
        public const float EmphasizedEndpointRadius = 6;

        /// <summary>Stroke paint for the polyline.</summary>
        private static readonly SKPaint LinePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            Color = GlPastelPalette.ConnectorStroke,
            StrokeWidth = 1.5f
        };

        /// <summary>Fill paint for the endpoint marker circles.</summary>
        private static readonly SKPaint MarkerPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = GlPastelPalette.ConnectorFill
        };

        /// <summary>
        /// Emphasized stroke paint (a thicker, SlateBlue line under the hover
        /// highlight — the analogous violet neighbor of the DodgerBlue rest
        /// color, so the hovered connector pops out of the connector tangle).
        /// </summary>
        private static readonly SKPaint LinePaintEmphasized = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            Color = GlPastelPalette.ConnectorHoverStroke,
            StrokeWidth = 3.5f
        };

        /// <summary>Emphasized endpoint marker paint (same fill as at rest).</summary>
        private static readonly SKPaint MarkerPaintEmphasized = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = GlPastelPalette.ConnectorFill
        };

        /// <summary>
        /// The polyline this connector renders. Never null; may be empty.
        /// </summary>
        public IReadOnlyList<Point2> Points { get; }

        /// <summary>
        /// Draw the connector under the hover highlight: a thicker stroke and
        /// larger endpoint markers so the dependency's start and end are
        /// unambiguous. Default false (normal drawing).
        /// </summary>
        public bool Emphasized { get; set; }

        /// <summary>
        /// Connector class initialization.
        /// </summary>
        /// <param name="points">polyline to draw</param>
        public Connector(IReadOnlyList<Point2> points)
        {
            Points = points ?? (IReadOnlyList<Point2>)new Point2[0];
        }

        /// <summary>
        /// Draw the polyline plus small filled endpoint circles onto the
        /// frame's canvas. Empty or single-point polylines are a no-op.
        /// </summary>
        /// <param name="frame">drawing context</param>
        public void Draw(GlFrame frame)
        {
            if (Points.Count < 2)
            {
                return;
            }

            using (var builder = new SKPathBuilder())
            {
                builder.MoveTo(new SKPoint((float)Points[0].X, (float)Points[0].Y));
                for (int i = 1; i < Points.Count; i++)
                {
                    builder.LineTo(new SKPoint((float)Points[i].X, (float)Points[i].Y));
                }
                using (var path = builder.Detach())
                {
                    frame.Canvas.DrawPath(
                        path, Emphasized ? LinePaintEmphasized : LinePaint);
                }
            }

            float radius = Emphasized ? EmphasizedEndpointRadius : EndpointRadius;
            SKPaint marker = Emphasized ? MarkerPaintEmphasized : MarkerPaint;
            Point2 first = Points[0];
            Point2 last = Points[Points.Count - 1];
            frame.Canvas.DrawCircle((float)first.X, (float)first.Y, radius, marker);
            frame.Canvas.DrawCircle((float)last.X, (float)last.Y, radius, marker);
        }

        /// <summary>
        /// Create and draw a connector from a polyline.
        /// </summary>
        /// <param name="frame">drawing context</param>
        /// <param name="points">polyline to draw</param>
        /// <returns>the created connector is returned</returns>
        public static Connector Draw(GlFrame frame, IReadOnlyList<Point2> points)
        {
            Connector c = new Connector(points);
            c.Draw(frame);
            return c;
        }

    }

}
