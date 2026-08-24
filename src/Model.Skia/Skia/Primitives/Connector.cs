using System.Collections.Generic;

using ModelConsole.Geometry;
using ModelConsole.Graph;
using ModelConsole.Palette;
using ModelConsole.Skia.GLibrary;

using SkiaSharp;

namespace ModelConsole.Skia.Primitives
{

    /// <summary>
    /// Draw a constraint connector as a polyline on the Skia stack. This is
    /// the Skia counterpart of the XAML <c>GlOrthoPath.DrawRouted</c> + its
    /// <c>GlEllipse</c> endpoint markers: a static, pre-computed route (the
    /// output of <see cref="OrthogonalRouter"/>) is stroked with visual-only
    /// rounded bends and no grips. The original route points stay unchanged.
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

        /// <summary>Visual-only radius used to soften orthogonal bends.</summary>
        public const float CornerRadius = 8;

        /// <summary>Offset between Crow's Foot marker parts, in pixels.</summary>
        private const float MarkerStep = 7;

        /// <summary>Half length of a required-one bar.</summary>
        private const float BarHalfLength = 6;

        /// <summary>Radius of an optionality circle.</summary>
        private const float OptionalCircleRadius = 4;

        /// <summary>Length of Crow's Foot prongs.</summary>
        private const float ManyProngLength = 12;

        /// <summary>Half spread of Crow's Foot outer prongs.</summary>
        private const float ManyProngHalfSpread = 6;

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
        /// Current-session style used when <see cref="Emphasized"/> is true.
        /// Rest-state connector drawing is unchanged.
        /// </summary>
        public ConnectorStyle SelectedStyle { get; set; } = ConnectorStyle.Default;

        /// <summary>
        /// Whether endpoint circles are drawn. ERD shows dependency endpoints;
        /// UML associations use a clean line with labels (backlog 040).
        /// </summary>
        public bool ShowEndpointMarkers { get; set; } = true;

        /// <summary>Optional Crow's Foot marker at the first route point.</summary>
        public CrowFootNotation.CardinalityMarker StartMarker { get; set; } =
            CrowFootNotation.CardinalityMarker.None;

        /// <summary>Optional Crow's Foot marker at the last route point.</summary>
        public CrowFootNotation.CardinalityMarker EndMarker { get; set; } =
            CrowFootNotation.CardinalityMarker.None;

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
                var commands = RoundedPolyline.Build(Points, CornerRadius);
                for (int i = 0; i < commands.Count; i++)
                {
                    var command = commands[i];
                    switch (command.Type)
                    {
                        case RoundedPathCommandType.MoveTo:
                            builder.MoveTo(ToPoint(command.Point));
                            break;
                        case RoundedPathCommandType.QuadraticTo:
                            builder.QuadTo(
                                ToPoint(command.ControlPoint),
                                ToPoint(command.Point));
                            break;
                        default:
                            builder.LineTo(ToPoint(command.Point));
                            break;
                    }
                }
                using (var path = builder.Detach())
                {
                    using SKPaint emphasizedLine = Emphasized
                        ? CreateEmphasizedLinePaint() : null;
                    SKPaint line = Emphasized ? emphasizedLine : LinePaint;
                    frame.Canvas.DrawPath(path, line);
                }
            }

            if (ShowEndpointMarkers)
            {
                using SKPaint emphasizedMarker = Emphasized
                    ? CreateEmphasizedMarkerPaint() : null;
                using SKPaint emphasizedMarkerStroke = Emphasized
                    ? CreateEmphasizedMarkerStrokePaint() : null;
                SKPaint marker = Emphasized ? emphasizedMarker : MarkerPaint;
                SKPaint markerStroke = Emphasized
                    ? emphasizedMarkerStroke : LinePaint;
                Point2 first = Points[0];
                Point2 last = Points[Points.Count - 1];
                if (StartMarker.IsNone && EndMarker.IsNone)
                {
                    float radius = Emphasized
                        ? EmphasizedEndpointRadius : EndpointRadius;
                    frame.Canvas.DrawCircle(
                        (float)first.X, (float)first.Y, radius, marker);
                    frame.Canvas.DrawCircle(
                        (float)last.X, (float)last.Y, radius, marker);
                }
                else
                {
                    DrawCardinalityMarker(
                        frame, first, Points[1], StartMarker, markerStroke);
                    DrawCardinalityMarker(
                        frame, last, Points[Points.Count - 2],
                        EndMarker, markerStroke);
                }
            }
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

        private static SKPoint ToPoint(Point2 point)
        {
            return new SKPoint((float)point.X, (float)point.Y);
        }

        private SKPaint CreateEmphasizedLinePaint()
        {
            ConnectorStyle style = SelectedStyle ?? ConnectorStyle.Default;
            return new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColor.Parse(style.SelectedHex),
                StrokeWidth = (float)style.SelectedWidth
            };
        }

        private SKPaint CreateEmphasizedMarkerPaint()
        {
            ConnectorStyle style = SelectedStyle ?? ConnectorStyle.Default;
            return new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColor.Parse(style.SelectedHex)
            };
        }

        private SKPaint CreateEmphasizedMarkerStrokePaint()
        {
            ConnectorStyle style = SelectedStyle ?? ConnectorStyle.Default;
            return new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColor.Parse(style.SelectedHex),
                StrokeWidth = (float)style.SelectedWidth
            };
        }

        private static void DrawCardinalityMarker(
            GlFrame frame,
            Point2 endpoint,
            Point2 adjacent,
            CrowFootNotation.CardinalityMarker marker,
            SKPaint stroke)
        {
            if (marker.IsNone)
            {
                return;
            }

            SKPoint origin = ToPoint(endpoint);
            SKPoint along = Unit(endpoint, adjacent);
            if (along.X == 0 && along.Y == 0)
            {
                return;
            }
            SKPoint perp = new SKPoint(-along.Y, along.X);
            float cursor = MarkerStep;

            if (marker.Optional)
            {
                SKPoint center = Add(origin, Scale(along, cursor));
                using var fill = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    Color = SKColors.White
                };
                frame.Canvas.DrawCircle(
                    center.X, center.Y, OptionalCircleRadius, fill);
                frame.Canvas.DrawCircle(
                    center.X, center.Y, OptionalCircleRadius, stroke);
                cursor += MarkerStep;
            }

            if (marker.One)
            {
                SKPoint center = Add(origin, Scale(along, cursor));
                frame.Canvas.DrawLine(
                    Add(center, Scale(perp, -BarHalfLength)),
                    Add(center, Scale(perp, BarHalfLength)),
                    stroke);
                cursor += MarkerStep;
            }

            if (marker.Many)
            {
                SKPoint tips = Add(origin, Scale(along, cursor));
                SKPoint root = Add(origin, Scale(along, cursor + ManyProngLength));
                frame.Canvas.DrawLine(root, tips, stroke);
                frame.Canvas.DrawLine(
                    root, Add(tips, Scale(perp, -ManyProngHalfSpread)), stroke);
                frame.Canvas.DrawLine(
                    root, Add(tips, Scale(perp, ManyProngHalfSpread)), stroke);
            }
        }

        private static SKPoint Unit(Point2 from, Point2 to)
        {
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double length = System.Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0)
            {
                return new SKPoint(0, 0);
            }
            return new SKPoint((float)(dx / length), (float)(dy / length));
        }

        private static SKPoint Add(SKPoint a, SKPoint b)
        {
            return new SKPoint(a.X + b.X, a.Y + b.Y);
        }

        private static SKPoint Scale(SKPoint p, float scale)
        {
            return new SKPoint(p.X * scale, p.Y * scale);
        }

    }

}
