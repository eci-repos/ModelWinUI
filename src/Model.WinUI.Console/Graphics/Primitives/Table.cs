using System.Collections.Generic;

using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Input;

using Model.Data;
using ModelConsole.Graphics.GLibrary;

namespace ModelConsole.Graphics.Primitives
{

   public class Panel : GlBoxInfo
   {
   }

   /// <summary>
   /// Table Primitive
   /// </summary>
   public class Table : GlRectangle
   {
      private double _leftPadding = 66;
      //private double _rightPadding = 24;

      private double _bannerHeight;

      private TableInfo _table;

      private StackPanel _rowsPanel = new StackPanel();
      public List<TableRowPanel> Rows { get; set; } = new List<TableRowPanel>();

      /// <summary>
      /// Pastel band behind the banner text, colored by the table kind
      /// (entity vs reference code). Hit-test transparent so presses reach
      /// the table rectangle.
      /// </summary>
      private Border _headerBorder;

      /// <summary>
      /// Header colors by table kind: light blue for entity tables, light
      /// green for reference-code lookups.
      /// </summary>
      private static readonly Color EntityHeaderColor =
         Color.FromArgb(255, 220, 233, 247);
      private static readonly Color ReferenceHeaderColor =
         Color.FromArgb(255, 226, 239, 218);

      /// <summary>
      /// The metadata this table renders. The columns list is shared with the
      /// model, so editing a column in the inspector is picked up on re-render.
      /// </summary>
      public TableInfo TableInfo
      {
         get { return _table; }
      }

      /// <summary>
      /// The size the table will render at, computed from its column content.
      /// Unlike <see cref="GlRectangle.Width"/>/<see cref="GlRectangle.Height"/>
      /// (which return ActualWidth/ActualHeight and are 0 until layout) these
      /// are valid immediately after construction.
      /// </summary>
      public double ComputedWidth { get; private set; }
      public double ComputedHeight { get; private set; }

      private double _column1Width;
      private double _column2Width;
      private double _column3Width;

      /// <summary>
      /// Table class initialization using table information
      /// </summary>
      /// <param name="table">table information</param>
      public Table(GlContext frame, double x, double y,
         double bannerHeight, TableInfo table) : base()
      {
         X = x;
         Y = y;
         _bannerHeight = bannerHeight;
         CornerRadius = GlContext.DefaultRoundCorderRadious;

         SetTable(table);
      }

      /// <summary>
      /// Move Object to a relative position using given delta values.
      /// </summary>
      /// <param name="delta">DX and DY distance to move if null the object
      /// will move to the current X,Y position</param>
      public override void DeltaMove(Point? delta = null)
      {
         base.DeltaMove(delta);
         Canvas.SetLeft(_rowsPanel, X);
         Canvas.SetTop(_rowsPanel, Y + _bannerHeight);
         if (_headerBorder != null)
         {
            Canvas.SetLeft(_headerBorder, X);
            Canvas.SetTop(_headerBorder, Y);
         }
      }

      /// <summary>
      /// Manage pointer event.
      /// </summary>
      /// <param name="poinerEvent"></param>
      public override void PointerEvent(
         GlPointerEvent poinerEvent, PointerPoint point = null)
      {

      }

      /// <summary>
      /// Set Table.
      /// </summary>
      /// <param name="table">table information</param>
      public void SetTable(TableInfo table)
      {
         _table = table;
         _table.Copy(table);
         AddColumns(table.Columns);
      }

      /// <summary>
      /// Draw Banner Text as schema and table names.
      /// </summary>
      public void DrawBannerText(GlContext frame)
      {
         AddBanner(
            frame, this, _table.SchemaName + "::" + _table.TableName);
      }

      /// <summary>
      /// Set table Banner Text.
      /// </summary>
      /// <param name="schemaName">schema name</param>
      /// <param name="tableName">table name</param>
      public void DrawBannerText(
         GlContext frame, string schemaName, string tableName)
      {
         _table.SchemaName = schemaName;
         _table.TableName = tableName;

         DrawBannerText(frame);
      }

      /// <summary>
      /// Add Columns
      /// </summary>
      /// <param name="columns">columns list to add</param>
      public void AddColumns(List<ColumnInfo> columns)
      {
         GlTextBox b = new GlTextBox();

         double heigth = b.FontSize + GlContext.DefaultTextPanelPadding;
         double maxLength = 0;
         double maxTypeLength = 0;
         Size size;

         foreach (var i in columns)
         {
            b.Text = i.ColumnName;
            size = b.GetDesiredSize();
            if (size.Width > maxLength)
            {
               maxLength = size.Width;
            }

            b.Text = TableRowPanel.GetDataType(i);
            size = b.GetDesiredSize();
            if (size.Width > maxTypeLength)
            {
               maxTypeLength = size.Width;
            }
         }

         double x = X;
         double y = Y + _bannerHeight + CornerRadius +
            GlContext.DefaultTextPanelPadding / 2.0;

         maxLength += 10;
         _column1Width = _leftPadding;
         _column2Width = maxLength;
         _column3Width = maxTypeLength;

         foreach (var c in columns)
         {
            var p = new TableRowPanel();

            p.Instance.Orientation = Orientation.Horizontal;
            p.X = x + 1;
            p.Y = y;

            p.Width = maxLength;
            p.Height = heigth + GlContext.DefaultTextPanelPadding * 2;

            p.Column = c;

            p.SetSize();
            p.SetSize(_column1Width, _column2Width, _column3Width);

            Rows.Add(p);
            y += p.Height;
         }

         // The render size depends only on the column content, so it is known
         // right after the columns are added (no XAML layout required).
         double totalHeight = _bannerHeight + CornerRadius * 2;
         foreach (var row in Rows)
         {
            totalHeight += row.Height;
         }
         ComputedWidth = _column1Width + _column2Width + _column3Width + 22;
         ComputedHeight = totalHeight + 40;

         _rowsPanel.Children.Clear();
      }

      /// <summary>
      /// Absolute canvas Y of the vertical center of the row that renders the
      /// given column, used to anchor FK connectors at the column row. Falls
      /// back to the table's vertical edge midpoint.
      /// </summary>
      /// <param name="columnName">column to locate</param>
      /// <returns>the absolute Y coordinate of the row center</returns>
      public double GetRowCenterY(string columnName)
      {
         foreach (var row in Rows)
         {
            if (row.Column != null &&
               string.Equals(row.Column.ColumnName, columnName,
                  System.StringComparison.Ordinal))
            {
               return row.Y + row.Height / 2.0;
            }
         }
         return Y + ComputedHeight / 2.0;
      }

      /// <summary>
      /// Draw Table baesd on set info and columns...
      /// </summary>
      public void DrawTable(GlContext frame)
      {
         bool everyOther = true;
         double height = _bannerHeight + CornerRadius * 2;

         foreach (var i in Rows)
         {
            // fill everyother
            everyOther = !everyOther;

            i.SetBackground(
               everyOther ? Microsoft.UI.Colors.WhiteSmoke : 
                  Microsoft.UI.Colors.White);

            // draw constraint
            string constraint = null;
            if (i.Column.IsKey)
            {
               constraint += "PK";
            }
            if (i.Column.IsForeignKey)
            {
               constraint += constraint == null ? "" : ", ";
               constraint += "FK";
            }

            if (constraint != null)
            {
               i.ConstraintText.Text = constraint;
            }

            // draw column name text
            i.Text.Text = i.Column.ColumnName;

            // draw data type text
            i.DataTypeText.Text = TableRowPanel.GetDataType(i.Column);

            // add panel padding to show space around text-blocks
            i.Instance.Padding = new Thickness(
               10, GlContext.DefaultTextPanelPadding, 
               10, GlContext.DefaultTextPanelPadding);

            // add row panel to rows-panel 
            _rowsPanel.Children.Add(i.NativeInstance);
            height += i.Height;
         }

         Width = ComputedWidth;
         Height = ComputedHeight;

         SetInstance(X, Y, Width, Height, GlContext.DefaultRoundCorderRadious);

         NativeInstance.Tag = this;
         frame.Instance.Children.Add(NativeInstance);

         // Header band: a pastel rectangle behind the banner text, colored by
         // the table kind (entity vs reference code). Rounded on top to match
         // the table's corner radius, square on the bottom where the rows
         // start.
         var kind = TableKindClassifier.Classify(_table);
         var headerColor = kind == TableKind.ReferenceCode
            ? ReferenceHeaderColor : EntityHeaderColor;
         _headerBorder = new Border
         {
            Width = ComputedWidth,
            Height = _bannerHeight,
            Background = new SolidColorBrush(headerColor),
            CornerRadius = new CornerRadius(
               CornerRadius, CornerRadius, 0, 0),
            IsHitTestVisible = false
         };
         Canvas.SetLeft(_headerBorder, X);
         Canvas.SetTop(_headerBorder, Y);
         frame.Instance.Children.Add(_headerBorder);

         frame.Instance.Children.Add(_rowsPanel);

         // The rows panel and banner are display-only; make them hit-test
         // transparent so a press anywhere on the table reaches the rectangle
         // and the whole table can be dragged.
         _rowsPanel.IsHitTestVisible = false;

         DeltaMove();
         DrawBannerText(frame, _table.SchemaName, _table.TableName);

         if (Banner != null && Banner.Instance is FrameworkElement bannerElement)
         {
            bannerElement.IsHitTestVisible = false;
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
      public static Table DrawTable(GlContext frame, float x, float y,
         float bannerHeight, TableInfo table)
      {
         Table t = new Table(frame, x, y, bannerHeight, table);
         t.DrawTable(frame);
         return t;
      }

   }

}
