using SkiaSharp;
using ModelConsole.Skia.GLibrary;

namespace ModelConsole.Skia.Primitives
{

    /// <summary>
    /// A half-round band: a rounded rectangle with one edge squared off. Used
    /// for the table banner (squared where the rows begin) and the footer
    /// (squared where the last row meets it) — see <see cref="Table"/>.
    /// </summary>
    public class RectangleHalf
    {
        private GlFrame surface;

        public RectangleHalf(GlFrame surface)
        {
            this.surface = surface;
        }

        /// <summary>
        /// Draw a band. The <paramref name="top"/> end is squared — in the
        /// caller's pre-rotation space the banner's straight edge is its top
        /// (<paramref name="top"/> true) and the footer's is its bottom
        /// (<paramref name="top"/> false); the 180° turn in
        /// <see cref="Table.DrawBorders"/> lands both squared edges against
        /// the table rows.
        /// </summary>
        /// <param name="x">x lower-left</param>
        /// <param name="y">y lower-left</param>
        /// <param name="w">width</param>
        /// <param name="h">height</param>
        /// <param name="r">rectangle corner radius</param>
        /// <param name="top">true squares the top edge, false the bottom</param>
        /// <param name="color">band fill color</param>
        public void Draw(
           float x, float y, float w, float h, float r, bool top, SKColor color)
        {
            var fill = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = color
            };

            surface.Canvas.DrawRoundRect(x, y, w, h, r, r, fill);

            var oy = top ? y : y + h - r;
            surface.Canvas.DrawRect(x, oy, w, r, fill);

            fill.Dispose();
        }

        public void DrawTop(float x, float y, float w, float h, float r,
           SKColor color)
        {
            Draw(x, y, w, h, r, true, color);
        }

        public void DrawBottom(float x, float y, float w, float h, float r,
           SKColor color)
        {
            Draw(x, y, w, h, r, false, color);
        }

        /// <summary>
        /// Stroke the table's card border (backlog 041). The width + color
        /// come from the shared <c>ModelConsole.Palette.TablePalette</c> — a
        /// thicker neutral line at rest, the DodgerBlue accent — thicker —
        /// while the table is hovered — so both renderers border identically.
        /// A local paint (never mutating the frame's shared DefaultStroke).
        /// </summary>
        public void DrawBorder(float x, float y, float w, float h, float r,
           SKColor color, float width)
        {
            var fill = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = color,
                StrokeWidth = width
            };

            surface.Canvas.DrawRoundRect(x, y, w, h, r, r, fill);

            fill.Dispose();
        }

    }

}
