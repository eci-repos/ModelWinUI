using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Model.Data;
using ModelConsole.Graph;
using ModelConsole.Palette;
using ModelConsole.Skia.GLibrary;

namespace ModelConsole.Skia.Primitives
{

    public class Panel
    {
        public float x;
        public float y;
        public float width;
        public float height;
    }

    public class TablePanel : Panel
    {
        public ColumnInfo Column { get; set; }
        public static string GetDataType(ColumnInfo column)
        {
            return column.Type + (column.Size > 0 ?
               "(" + column.Size.ToString() + ")" : "");
        }
    }

    /// <summary>
    /// Draw a database table
    /// </summary>
    public class Table : TableInfo, IDisposable
    {
        private GlFrame _frame;
        private SKFont _font = null;
        private float _leftPadding = 66;
        private float _rightPadding = 24;

        public float _bannerHeight;
        public float _cornerRadious;

        private TableInfo _table;

        /// <summary>
        /// The notation used to render this card. ERD is the default; UML uses
        /// class-style attribute rows over the same table metadata (backlog 040).
        /// </summary>
        public DiagramNotation Notation { get; set; } = DiagramNotation.Erd;

        /// <summary>
        /// Table kind, captured when the table is set — colors the banner and
        /// footer bands (defaults to entity for the geometry-only constructors
        /// that have no metadata).
        /// </summary>
        private TableKind _kind = TableKind.Entity;

        /// <summary>
        /// True while the pointer is over this table (backlog 041): the border
        /// draws the DodgerBlue accent, thicker, so the hovered card reads at
        /// a glance (mirrors the XAML table's live Hovered toggle — the Skia
        /// stack redraws, so it is set before each draw instead).
        /// </summary>
        private bool _hovered;

        /// <summary>
        /// Whether this table is hovered (backlog 041). The border color and
        /// width are picked from the shared <see cref="TablePalette"/> when the
        /// table draws — rest neutral, hovered accent.
        /// </summary>
        public bool Hovered
        {
            get { return _hovered; }
            set { _hovered = value; }
        }

        private Panel _panel = new Panel();
        private List<TablePanel> _panels = new List<TablePanel>();

        /// <summary>
        /// Table class initialization.
        /// </summary>
        /// <param name="frame">frame</param>
        public Table(GlFrame frame)
        {
            _frame = frame;
        }

        /// <summary>
        /// Table class initialization using table information
        /// </summary>
        /// <param name="table">table information</param>
        public Table(GlFrame frame, float x, float y,
           float bannerHeight, TableInfo table,
           DiagramNotation notation = DiagramNotation.Erd)
        {
            _frame = frame;
            _panel.x = x;
            _panel.y = y;
            _bannerHeight = bannerHeight;
            Notation = notation;
            _cornerRadious = _frame.DefaultRoundCorderRadious;

            SetTable(table);
        }

        /// <summary>
        /// Set Table.
        /// </summary>
        /// <param name="table">table information</param>
        public void SetTable(TableInfo table)
        {
            _table = table;
            _kind = TableKindClassifier.Classify(table);
            Copy(table);
            AddColumns(table.Columns);
        }

        /// <summary>
        /// Set Panel font.
        /// </summary>
        /// <param name="font">font to set</param>
        public void SetFont(SKFont font)
        {
            if (_font != null)
            {
                _font.Dispose();
                _font = null;
            }

            _font = new SKFont(font.Typeface, font.Size);
        }

        /// <summary>
        /// Add Columns
        /// </summary>
        /// <param name="columns">columns list to add</param>
        public void AddColumns(List<ColumnInfo> columns)
        {
            if (_font == null)
            {
                SetFont(_frame.DefaultFont);
            }

            float heigth = _font.Size + _frame.DefaultTextPanelPadding;
            float maxLength = 0;
            float maxTypeLength = 0;

            foreach (var i in columns)
            {
                float w = _font.MeasureText(Notation == DiagramNotation.Uml
                   ? UmlProfile.Attribute(i)
                   : i.ColumnName);
                if (w > maxLength)
                {
                    maxLength = w;
                }

                if (Notation == DiagramNotation.Erd)
                {
                    w = _font.MeasureText(TablePanel.GetDataType(i));
                    if (w > maxTypeLength)
                    {
                        maxTypeLength = w;
                    }
                }
            }

            float x = _panel.x;
            float y = _panel.y + _bannerHeight + _cornerRadious +
               _frame.DefaultTextPanelPadding / 2.0f;

            foreach (var c in columns)
            {
                var p = new TablePanel();

                p.x = x;
                p.y = y;

                p.width = maxLength;
                p.height = heigth + _frame.DefaultTextPanelPadding * 2;

                p.Column = c;

                _panels.Add(p);
                y += p.height;
            }

            _panel.width = maxLength + _frame.DefaultTextPanelPadding * 2 +
               maxTypeLength + _frame.DefaultTextPanelPadding * 2 +
               _leftPadding + _rightPadding;
            // The footer band (backlog 036) is part of the measured height —
            // one shared F for both renderers, so the XAML and Skia tables
            // close identically. Row Y positions are unchanged; only the
            // bottom budget grows.
            _panel.height = y + _cornerRadious - _panel.y +
               TablePalette.FooterHeight;
        }

        /// <summary>
        /// Measured table width, valid right after <see cref="SetTable"/> —
        /// no draw is required (used to lay tables out before drawing).
        /// </summary>
        public float ComputedWidth
        {
            get { return _panel.width; }
        }

        /// <summary>
        /// Measured table height, valid right after <see cref="SetTable"/> —
        /// no draw is required (used to lay tables out before drawing).
        /// </summary>
        public float ComputedHeight
        {
            get { return _panel.height; }
        }

        /// <summary>
        /// Absolute Y of the vertical center of the row that renders the given
        /// column, used to anchor FK connectors at the column row. Falls back
        /// to the table's vertical midpoint (parity with the XAML Table).
        /// </summary>
        /// <param name="columnName">column to locate</param>
        /// <returns>the Y coordinate of the row center</returns>
        public float GetRowCenterY(string columnName)
        {
            foreach (var p in _panels)
            {
                if (p.Column != null &&
                   string.Equals(p.Column.ColumnName, columnName,
                      StringComparison.Ordinal))
                {
                    return p.y + p.height / 2.0f;
                }
            }
            return _panel.y + _panel.height / 2.0f;
        }

        /// <summary>
        /// Table class initialization with frame and geometry information.
        /// </summary>
        /// <param name="frame">frame</param>
        /// <param name="x">x lower-left</param>
        /// <param name="y">y lower-left</param>
        /// <param name="w">width</param>
        /// <param name="h">height</param>
        /// <param name="bannerHeight">top banner height</param>
        /// <param name="r">rectangle corder radius</param>
        public Table(GlFrame frame,
           float x, float y, float w, float h, float bannerHeight, float r)
        {
            _frame = frame;
            Initialize(x, y, w, h, bannerHeight, r);
            DrawBorders();
        }

        /// <summary>
        /// Table class initialization with geometry information.
        /// </summary>
        /// <param name="x">x lower-left</param>
        /// <param name="y">y lower-left</param>
        /// <param name="w">width</param>
        /// <param name="h">height</param>
        /// <param name="bannerHeight">top banner height</param>
        /// <param name="r">rectangle corder radius</param>
        public void Initialize(
           float x, float y, float w, float h, float bannerHeight, float r)
        {
            _panel.x = x;
            _panel.y = y;
            _panel.width = w;
            _panel.height = h;

            _bannerHeight = bannerHeight;
            _cornerRadious = r;
        }

        /// <summary>
        /// Using already defined geometry information, draw table.
        /// </summary>
        public void DrawBorders()
        {
            float dy = _panel.y + _panel.height - _bannerHeight;

            var spHalfRec = new RectangleHalf(_frame);

            var cx = _panel.x + _panel.width / 2;
            var cy = _panel.y + _panel.height / 2;

            // Compose the 180° rotation with the current canvas transform
            // (e.g. the fit/zoom transform) and restore afterward, instead
            // of replacing the matrix — otherwise the transform is wiped for
            // the rest of the draw (backlog 015). With an identity current
            // matrix this is identical to the old SetMatrix behavior.
            _frame.Canvas.Save();
            _frame.Canvas.Concat(GlFrame.GetOriginTransformMatrix(cx, cy));

            // Banner and footer bands, both tinted from the shared palette by
            // table kind (backlog 036). The footer was anticipated here —
            // RectangleHalf.DrawBottom existed but was never wired in.
            spHalfRec.DrawTop(_panel.x, dy, _panel.width, _bannerHeight,
               _cornerRadious, SKColor.Parse(TablePalette.BannerHex(_kind)));
            spHalfRec.DrawBottom(_panel.x, _panel.y, _panel.width,
               TablePalette.FooterHeight, _cornerRadious,
               SKColor.Parse(TablePalette.FooterHex(_kind)));
            // Card border (backlog 041): a thicker neutral line at rest, the
            // DodgerBlue accent — thicker — while the table is hovered, both
            // from the shared palette (parity with the XAML table).
            spHalfRec.DrawBorder(
               _panel.x, _panel.y, _panel.width, _panel.height, _cornerRadious,
               SKColor.Parse(_hovered ? TablePalette.HoveredBorderHex : TablePalette.BorderHex),
               _hovered ? TablePalette.HoveredBorderWidth : TablePalette.BorderWidth);

            // Hairslines: under the banner where the rows start, and over the
            // footer where the last row meets it.
            _frame.Canvas.DrawLine(
               _panel.x, dy, _panel.x + _panel.width, dy, _frame.DefaultStroke);
            _frame.Canvas.DrawLine(
               _panel.x, _panel.y + TablePalette.FooterHeight,
               _panel.x + _panel.width, _panel.y + TablePalette.FooterHeight,
               _frame.DefaultStroke);

            //_frame.Canvas.DrawCircle(cx, cy, 5, _frame.DefaultStroke);

            _frame.Canvas.Restore();
        }

        /// <summary>
        /// Table class initialization with frame and geometry information.
        /// </summary>
        /// <returns>returns instance of Table to further add other information
        /// </returns>
        /// <param name="frame">frame</param>
        /// <param name="x">x lower-left</param>
        /// <param name="y">y lower-left</param>
        /// <param name="w">width</param>
        /// <param name="h">height</param>
        /// <param name="bannerHeight">top banner height</param>
        /// <param name="r">rectangle corder radius</param>
        public static Table DrawBorders(GlFrame frame, float x, float y, float w,
           float h, float bannerHeight, float r)
        {
            Table t = new Table(frame, x, y, w, h, bannerHeight, r);
            t.DrawBorders();
            return t;
        }

        /// <summary>
        /// Set table Banner Text.
        /// </summary>
        /// <param name="schemaName">schema name</param>
        /// <param name="tableName">table name</param>
        public void DrawBannerText(string schemaName, string tableName)
        {
            SchemaName = schemaName;
            TableName = tableName;

            DrawBannerText();
        }

        /// <summary>
        /// Draw Table baesd on set info and columns...
        /// </summary>
        public void DrawTable()
        {
            DrawBorders();
            if (Notation == DiagramNotation.Uml)
            {
                _frame.Canvas.DrawText(UmlProfile.ClassBanner(_table),
                   _panel.x + 10, _panel.y + 10, SKTextAlign.Left,
                   _frame.DefaultFont, _frame.DefaultTextPaint);
            }
            else
            {
                DrawBannerText(_table.SchemaName, _table.TableName);
            }

            // Kind-tinted body rows (backlog 036): the alternating stripe
            // carries a whisper of the banner hue; the plain row stays white.
            // Before this the two row paints were both #efefef, so the Skia
            // table showed no alternation.
            var stripePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColor.Parse(TablePalette.StripeHex(_kind))
            };
            var plainPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColor.Parse(TablePalette.PlainRowHex)
            };

            SKPoint p = new SKPoint();
            SKPaint paint;
            bool everyOther = true;
            foreach (var i in _panels)
            {
                // fill everyother
                paint = everyOther ? stripePaint : plainPaint;
                everyOther = !everyOther;

                _frame.Canvas.DrawRect(
                   i.x, i.y, _panel.width, i.height, paint);

                // draw header

                if (Notation == DiagramNotation.Uml)
                {
                    p.X = i.x + _frame.DefaultTextPanelPadding + 10;
                    p.Y = i.y + _frame.DefaultTextPanelPadding + _cornerRadious +
                       _frame.DefaultTextPanelPadding;
                    _frame.Canvas.DrawText(UmlProfile.Attribute(i.Column), p.X, p.Y,
                       SKTextAlign.Left, _frame.DefaultFont, _frame.DefaultTextPaint);
                }
                else
                {
                    string header = null;
                    if (i.Column.IsKey)
                    {
                        header += "PK";
                    }
                    if (i.Column.IsForeignKey)
                    {
                        header += header == null ? "" : ", ";
                        header += "FK";
                    }

                    if (header != null)
                    {
                        p.X = i.x + _frame.DefaultTextPanelPadding + 10;
                        p.Y = i.y + _frame.DefaultTextPanelPadding + _cornerRadious +
                           _frame.DefaultTextPanelPadding;
                        _frame.Canvas.DrawText(header, p.X, p.Y, SKTextAlign.Left,
                           _frame.DefaultFont, _frame.DefaultTextPaint);
                    }

                    // draw column name text
                    p.X = i.x + _frame.DefaultTextPanelPadding + _leftPadding;
                    p.Y = i.y + _frame.DefaultTextPanelPadding + _cornerRadious +
                       _frame.DefaultTextPanelPadding;
                    _frame.Canvas.DrawText(i.Column.ColumnName, p.X, p.Y,
                       SKTextAlign.Left, _frame.DefaultFont, _frame.DefaultTextPaint);

                    // draw table name text
                    p.X += i.width + _frame.DefaultTextPanelPadding + 10;
                    _frame.Canvas.DrawText(TablePanel.GetDataType(i.Column),
                       p.X, p.Y, SKTextAlign.Left, _frame.DefaultFont,
                       _frame.DefaultTextPaint);
                }
            }

            stripePaint.Dispose();
            plainPaint.Dispose();
        }

        /// <summary>
        /// Draw Table based on given info and columns.
        /// </summary>
        /// <param name="frame">frame</param>
        /// <param name="x">x lower-left</param>
        /// <param name="y">y lower-left</param>
        /// <param name="bannerHeight">top banner height</param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static Table DrawTable(GlFrame frame, float x, float y,
           float bannerHeight, TableInfo table,
           DiagramNotation notation = DiagramNotation.Erd)
        {
            Table t = new Table(frame, x, y, bannerHeight, table, notation);
            t.DrawTable();
            return t;
        }

        /// <summary>
        /// Draw Banner Text as schema and table names.
        /// </summary>
        public void DrawBannerText()
        {
            GlText.DrawText(_frame, SchemaName + "::" + TableName,
               _panel.x + 10, _panel.y + 10);
        }

        /// <summary>
        /// Dispose of allocated resources.
        /// </summary>
        public void Dispose()
        {
            if (_font != null)
            {
                _font.Dispose();
                _font = null;
            }
        }

    }

}
