using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Background;

using Model.Data;
using System.Security.Cryptography;
using Windows.UI.Input.Inking;
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
        private SKPaint _font = null;
        private float _leftPadding = 66;
        private float _rightPadding = 24;

        public float _bannerHeight;
        public float _cornerRadious;

        private TableInfo _table;

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
           float bannerHeight, TableInfo table)
        {
            _frame = frame;
            _panel.x = x;
            _panel.y = y;
            _bannerHeight = bannerHeight;
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
            Copy(table);
            AddColumns(table.Columns);
        }

        /// <summary>
        /// Set Panel font.
        /// </summary>
        /// <param name="font">font to set</param>
        public void SetFont(SKPaint font)
        {
            if (_font != null)
            {
                _font.Dispose();
                _font = null;
            }

            _font = new SKPaint
            {
                IsAntialias = font.IsAntialias,
                TextSize = font.TextSize,
                Color = font.Color,
                Typeface = font.Typeface
            };
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

            float heigth = _font.TextSize + _frame.DefaultTextPanelPadding;
            float maxLength = 0;
            float maxTypeLength = 0;
            SKRect rect = new SKRect();

            foreach (var i in columns)
            {
                _font.MeasureText(i.ColumnName, ref rect);
                if (rect.Width > maxLength)
                {
                    maxLength = rect.Width;
                }

                _font.MeasureText(TablePanel.GetDataType(i), ref rect);
                if (rect.Width > maxTypeLength)
                {
                    maxTypeLength = rect.Width;
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
            _panel.height = y + _cornerRadious - _panel.y;
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

            _frame.Canvas.SetMatrix(GlFrame.GetOriginTransformMatrix(cx, cy));

            //spHalfRec.DrawBottom(
            //   _panel.x, _panel.y, _panel.width, dy, _cornerRadious);
            spHalfRec.DrawTop(
               _panel.x, dy, _panel.width, _bannerHeight, _cornerRadious);
            spHalfRec.DrawBorder(
               _panel.x, _panel.y, _panel.width, _panel.height, _cornerRadious);

            _frame.Canvas.DrawLine(
               _panel.x, dy, _panel.x + _panel.width, dy, _frame.DefaultStroke);

            //_frame.Canvas.DrawCircle(cx, cy, 5, _frame.DefaultStroke);

            _frame.Canvas.SetMatrix(SKMatrix.Identity);
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
            DrawBannerText(_table.SchemaName, _table.TableName);

            SKPoint p = new SKPoint();
            SKPaint paint;
            bool everyOther = true;
            foreach (var i in _panels)
            {
                // fill everyother
                paint = everyOther ?
                   _frame.DefaultLightFill : _frame.DefaultLightStroke;
                everyOther = !everyOther;

                _frame.Canvas.DrawRect(
                   i.x, i.y, _panel.width, i.height, paint);

                // draw header

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
                    _frame.Canvas.DrawText(header, p,
                       _frame.DefaultFont);
                }

                // draw column name text
                p.X = i.x + _frame.DefaultTextPanelPadding + _leftPadding;
                p.Y = i.y + _frame.DefaultTextPanelPadding + _cornerRadious +
                   _frame.DefaultTextPanelPadding;
                _frame.Canvas.DrawText(i.Column.ColumnName, p,
                   _frame.DefaultFont);

                // draw table name text
                p.X += i.width + _frame.DefaultTextPanelPadding + 10;
                _frame.Canvas.DrawText(TablePanel.GetDataType(i.Column), p,
                   _frame.DefaultFont);
            }
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
           float bannerHeight, TableInfo table)
        {
            Table t = new Table(frame, x, y, bannerHeight, table);
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
