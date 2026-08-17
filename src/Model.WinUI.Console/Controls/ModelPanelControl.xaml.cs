using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Microsoft.UI;

using CommunityToolkit.Mvvm.DependencyInjection;
using Model.Data;
using ModelConsole.Graphics.GLibrary;
using ModelConsole.Graphics.GLibrary.GlOrtho;
using ModelConsole.Graphics.Primitives;
using ModelConsole.Graph;
using ModelConsole.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModelConsole.Controls
{
   public sealed partial class ModelPanelControl : UserControl
   {
      private const float BannerHeight = 40;
      private const double SlotPadding = 80;
      private const double Gutter = 80;
      private const double ExtentMargin = 80;

      /// <summary>
      /// Side length of the drawing "paper". The canvas is this large (with
      /// the content centered in it) so panning has room in all directions and
      /// only stops far from the drawing - the paper feels unlimited. The
      /// router region stays tight around the content, so the A* grid does
      /// not grow with the paper.
      /// </summary>
      private const double CanvasSize = 20000;

      private readonly GlContext _context;
      private readonly IModelDataProvider _dataProvider;
      private readonly ITableFactory _tableFactory;
      private readonly IConnectorFactory _connectorFactory;

      /// <summary>
      /// The model (source of truth). The drawing is always derived from this
      /// state, so any change (drag, delete, POCO edit) re-runs the render
      /// pipeline.
      /// </summary>
      private IReadOnlyList<TableInfo> _tables;

      /// <summary>
      /// Current table positions, keyed by table name. Initialized by the
      /// layout engine and updated when a table is dragged.
      /// </summary>
      private Dictionary<string, Rect2> _layout;

      /// <summary>
      /// Routes from the last render, keyed by edge. A drag release re-routes
      /// only the moved table's edges and reuses the rest, so the re-route
      /// cost stays proportional to the moved table's degree instead of the
      /// whole schema (backlog 013).
      /// </summary>
      private List<(FkRelation Edge, IReadOnlyList<Point2> Points)> _routes =
         new List<(FkRelation Edge, IReadOnlyList<Point2> Points)>();

      /// <summary>
      /// Bounding box of all drawn tables (content space). Drives the fit
      /// button and the router region; recomputed on every render.
      /// </summary>
      private Rect2 _contentBounds;

      /// <summary>
      /// Raised when the user clicks a graphic entity. The payload is the
      /// entity's <see cref="TableInfo"/> or <see cref="FkRelation"/>.
      /// </summary>
      public event EventHandler<object> EntitySelected;

      public ModelPanelControl()
      {
         this.InitializeComponent();

         _dataProvider = Ioc.Default.GetRequiredService<IModelDataProvider>();
         _tableFactory = Ioc.Default.GetRequiredService<ITableFactory>();
         _connectorFactory = Ioc.Default.GetRequiredService<IConnectorFactory>();
         _context = new GlContext(
            ModelCanvas, Ioc.Default.GetRequiredService<ILogService>());

         _context.ShapeReleased += OnShapeReleased;
         _context.ShapeClicked += OnShapeClicked;
         _context.PanRequested += OnPanRequested;

         _tables = _dataProvider.GetPublicSafetyTables();
         InitializeLayout();

         AddZoomAccelerators();
         Render();
         SyncZoomUI();
         WriteMessage("GL Context Ready.");

         // The canvas is a large "paper"; start with the content's top-left
         // visible at 100% zoom (the default offset (0,0) would show empty
         // canvas space). Deferred to Loaded so the ScrollViewer is laid out.
         Loaded += (s, e) =>
         {
            DispatcherQueue.TryEnqueue(() =>
            {
               ModelScrollViewer.ChangeView(
                  _contentBounds.X, _contentBounds.Y, 1.0f, true);
            });
         };
      }

      public void WriteMessage(string message)
      {
         _context.WriteMessage(message);
      }

      // ------------------------------------------------------------------
      // Zoom & fit (backlog 009). All zoom sources - slider, % box, fit
      // button, Ctrl+wheel/pinch (native ScrollViewer zoom), and the
      // keyboard shortcuts - reduce to ChangeView on the ScrollViewer.
      // ------------------------------------------------------------------

      private const double MinZoom = 0.1;
      private const double MaxZoom = 4.0;
      private const double ZoomStep = 1.25;

      /// <summary>
      /// Guards the slider against feedback: SyncZoomUI writes the slider
      /// value, which would otherwise re-enter ZoomSlider_ValueChanged.
      /// </summary>
      private bool _syncingZoom;

      private void AddZoomAccelerators()
      {
         AddZoomAccelerator(VirtualKey.Number0, VirtualKeyModifiers.Control, () => ApplyZoom(1.0));
         AddZoomAccelerator(VirtualKey.Number1, VirtualKeyModifiers.Control, FitToWindow);
         AddZoomAccelerator(VirtualKey.Add, VirtualKeyModifiers.Control, ZoomIn);
         AddZoomAccelerator(VirtualKey.Subtract, VirtualKeyModifiers.Control, ZoomOut);
         // Main-keyboard +/- (the SDK's VirtualKey enum omits the Oem* names,
         // so use the raw VK codes: 0xBB '=' and 0xBD '-'). Ctrl+Plus is
         // Ctrl+Shift+= on a US layout, so register both modifier sets.
         AddZoomAccelerator((VirtualKey)0xBB, VirtualKeyModifiers.Control, ZoomIn);
         AddZoomAccelerator((VirtualKey)0xBB, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, ZoomIn);
         AddZoomAccelerator((VirtualKey)0xBD, VirtualKeyModifiers.Control, ZoomOut);
      }

      private void AddZoomAccelerator(
         VirtualKey key, VirtualKeyModifiers modifiers, Action action)
      {
         var accel = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
         accel.Invoked += (s, e) => { action(); e.Handled = true; };
         KeyboardAccelerators.Add(accel);
      }

      private void ZoomIn()
      {
         if (ModelScrollViewer != null)
         {
            ApplyZoom(ModelScrollViewer.ZoomFactor * ZoomStep);
         }
      }

      private void ZoomOut()
      {
         if (ModelScrollViewer != null)
         {
            ApplyZoom(ModelScrollViewer.ZoomFactor / ZoomStep);
         }
      }

      /// <summary>
      /// Zoom to the given factor, keeping the content point under the
      /// viewport center fixed (zoom-around-center). Clamped to the
      /// ScrollViewer's zoom range.
      /// </summary>
      private void ApplyZoom(double zoom)
      {
         if (ModelScrollViewer == null)
         {
            return;
         }

         zoom = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));
         double oldZoom = ModelScrollViewer.ZoomFactor;
         if (Math.Abs(oldZoom - zoom) < 0.0001)
         {
            return;
         }

         // Offsets are in content units; the viewport shows
         // ViewportWidth / zoom content units, so the content point under
         // the viewport center is offset + viewport / (2 * zoom).
         double cx = ModelScrollViewer.HorizontalOffset +
            ModelScrollViewer.ViewportWidth / (2.0 * oldZoom);
         double cy = ModelScrollViewer.VerticalOffset +
            ModelScrollViewer.ViewportHeight / (2.0 * oldZoom);
         double newH = cx - ModelScrollViewer.ViewportWidth / (2.0 * zoom);
         double newV = cy - ModelScrollViewer.ViewportHeight / (2.0 * zoom);

         ModelScrollViewer.ChangeView(newH, newV, (float)zoom, true);
      }

      /// <summary>
      /// Scale the whole drawing to fit the viewport, capped at 100% so a
      /// small drawing does not blow up, then center it.
      /// </summary>
      private void FitToWindow()
      {
         if (ModelScrollViewer == null)
         {
            return;
         }

         double vw = ModelScrollViewer.ViewportWidth;
         double vh = ModelScrollViewer.ViewportHeight;
         var b = _contentBounds;
         if (vw <= 0 || vh <= 0 || b.Width <= 0 || b.Height <= 0)
         {
            return;
         }

         // Fit to the content (all tables), not the canvas: the canvas is a
         // large "paper" whose extent would zoom the drawing way out.
         double fit = Math.Min(vw / b.Width, vh / b.Height);
         fit = Math.Max(MinZoom, Math.Min(1.0, fit));
         double hOff = b.X + b.Width / 2.0 - vw / (2.0 * fit);
         double vOff = b.Y + b.Height / 2.0 - vh / (2.0 * fit);

         ModelScrollViewer.ChangeView(hOff, vOff, (float)fit, true);
      }

      /// <summary>
      /// Keep the slider and % box in step with the ScrollViewer's actual
      /// zoom (which also changes via native Ctrl+wheel / pinch).
      /// </summary>
      private void SyncZoomUI()
      {
         if (ModelScrollViewer == null || ZoomSlider == null)
         {
            return;
         }

         _syncingZoom = true;
         double zoom = ModelScrollViewer.ZoomFactor;
         ZoomSlider.Value = zoom * 100.0;
         ZoomTextBox.Text = ((int)Math.Round(zoom * 100.0)).ToString();
         _syncingZoom = false;
      }

      private void ZoomSlider_ValueChanged(
         object sender, RangeBaseValueChangedEventArgs e)
      {
         if (_syncingZoom || ModelScrollViewer == null)
         {
            return;
         }
         ApplyZoom(e.NewValue / 100.0);
      }

      private void ModelScrollViewer_ViewChanged(
         object sender, ScrollViewerViewChangedEventArgs e)
      {
         SyncZoomUI();
      }

      private void FitButton_Click(object sender, RoutedEventArgs e)
      {
         FitToWindow();
      }

      private void ZoomTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
      {
         if (e.Key == VirtualKey.Enter)
         {
            CommitZoomTextBox();
            e.Handled = true;
         }
      }

      private void ZoomTextBox_LostFocus(object sender, RoutedEventArgs e)
      {
         CommitZoomTextBox();
      }

      /// <summary>
      /// Parse the % box, clamp to the zoom range, and apply; revert to the
      /// current zoom when the text is not a valid number.
      /// </summary>
      private void CommitZoomTextBox()
      {
         if (double.TryParse(ZoomTextBox.Text, out double pct))
         {
            ApplyZoom(pct / 100.0);
         }
         else
         {
            SyncZoomUI();
         }
      }

      /// <summary>
      /// A pan gesture moved the drawing. Feed the delta (content units) to
      /// the ScrollViewer, preserving the current zoom so panning never resets
      /// it (backlog 011). The delta is measured from the pan start point in
      /// Canvas-local (content) space, so it is already zoom-independent.
      /// </summary>
      private void OnPanRequested(double dx, double dy)
      {
         if (ModelScrollViewer == null)
         {
            return;
         }
         // null zoom = "keep the current zoom"; only the offsets change.
         ModelScrollViewer.ChangeView(
            ModelScrollViewer.HorizontalOffset - dx,
            ModelScrollViewer.VerticalOffset - dy,
            null, true);
      }

      /// <summary>
      /// Compute the initial grid layout of the tables and store it as the
      /// current state (dragging updates it).
      /// </summary>
      private void InitializeLayout()
      {
         // Measure each table without drawing so every grid slot fits the
         // widest/tallest table and no table overlaps its neighbour.
         double maxWidth = 0, maxHeight = 0;
         foreach (var t in _tables)
         {
            var probe = new Table(_context, 0, 0, BannerHeight, t);
            maxWidth = Math.Max(maxWidth, probe.ComputedWidth);
            maxHeight = Math.Max(maxHeight, probe.ComputedHeight);
         }

         var layout = TableLayoutEngine.Layout(_tables, new GridLayoutOptions
         {
            Columns = 7,
            SlotWidth = maxWidth + SlotPadding,
            SlotHeight = maxHeight + SlotPadding,
            Gutter = Gutter
         });

         // Center the content in the large canvas so panning has room in all
         // directions (the "paper" is effectively unlimited - the view stops
         // only at the canvas edge, far from the content).
         double contentWidth = 0, contentHeight = 0;
         foreach (var kv in layout)
         {
            contentWidth = Math.Max(contentWidth, kv.Value.X + kv.Value.Width);
            contentHeight = Math.Max(contentHeight, kv.Value.Y + kv.Value.Height);
         }
         double offsetX = (CanvasSize - contentWidth) / 2.0;
         double offsetY = (CanvasSize - contentHeight) / 2.0;

         _layout = new Dictionary<string, Rect2>();
         foreach (var kv in layout)
         {
            _layout[kv.Key] = new Rect2(
               kv.Value.X + offsetX, kv.Value.Y + offsetY,
               kv.Value.Width, kv.Value.Height);
         }
      }

      /// <summary>
      /// Re-render the whole drawing from the current model state. The
      /// drawing is always derived from the state (table positions + FK
      /// constraints), never a frozen artifact, so any change - a drag, a
      /// deleted connector, an edited POCO field - re-runs this pipeline.
      /// </summary>
      /// <param name="onlyTable">when set (a drag release), re-route only the
      /// edges touching this table and reuse the stored routes for the rest,
      /// so the re-route cost is proportional to the moved table's degree
      /// rather than the whole schema (backlog 013)</param>
      private void Render(string onlyTable = null)
      {
         ModelCanvas.Children.Clear();
         _context.Reset();

         IGlModel model = Ioc.Default.GetRequiredService<IGlModel>();

         var drawn = new Dictionary<string, Table>();
         foreach (var t in _tables)
         {
            var rect = _layout[t.TableName];
            var table = _tableFactory.Create(
               _context, (float)rect.X, (float)rect.Y, BannerHeight, t);
            drawn[t.TableName] = table;
            model.Add(table);
         }

         // Content bounds (the bounding box of all drawn tables) drive the
         // fit button and the router region.
         double minX = double.MaxValue, minY = double.MaxValue;
         double maxX = 0, maxY = 0;
         foreach (var t in drawn.Values)
         {
            minX = Math.Min(minX, t.X);
            minY = Math.Min(minY, t.Y);
            maxX = Math.Max(maxX, t.X + t.ComputedWidth);
            maxY = Math.Max(maxY, t.Y + t.ComputedHeight);
         }
         if (drawn.Count == 0)
         {
            minX = 0; minY = 0; maxX = 0; maxY = 0;
         }
         _contentBounds = new Rect2(minX, minY, maxX - minX, maxY - minY);

         // The canvas is a large "paper" so panning has room in all
         // directions; the router bounds stay tight around the content so
         // the A* grid does not grow with the paper.
         ModelCanvas.Width = CanvasSize;
         ModelCanvas.Height = CanvasSize;
         var bounds = new Rect2(
            minX - ExtentMargin, minY - ExtentMargin,
            (maxX - minX) + 2 * ExtentMargin, (maxY - minY) + 2 * ExtentMargin);

         // Route every FK around the drawn tables, sequentially so no
         // connector crosses another.
         var obstacles = drawn.Values
            .Select(t => new Rect2(t.X, t.Y, t.ComputedWidth, t.ComputedHeight))
            .ToList();
         var routerOptions = new RouterOptions
         {
            GridSize = 16,
            ObstacleMargin = 14,
            StubLength = 20
         };

         var (edges, issues) = FkEdgeExtractor.Extract(_tables);

         // Resolve anchors (departure side chosen from the relative position
         // of the two tables) and fan out connectors that share a column.
         var startGroups = edges
            .GroupBy(e => e.ChildTable + "::" + e.ChildColumn)
            .ToDictionary(g => g.Key, g => g.ToList());
         var endGroups = edges
            .GroupBy(e => e.ParentTable + "::" + e.ParentColumn)
            .ToDictionary(g => g.Key, g => g.ToList());

         var anchorEdges = new List<(Point2 Start, Point2 End, FkRelation Edge)>();
         foreach (var edge in edges)
         {
            if (!drawn.TryGetValue(edge.ChildTable, out var child) ||
                !drawn.TryGetValue(edge.ParentTable, out var parent))
            {
               continue;
            }

            var (start, end, childSide, parentSide) = ConnectorAnchors.Resolve(
               new Rect2(child.X, child.Y, child.ComputedWidth, child.ComputedHeight),
               new Rect2(parent.X, parent.Y, parent.ComputedWidth, parent.ComputedHeight),
               child.GetRowCenterY(edge.ChildColumn),
               parent.GetRowCenterY(edge.ParentColumn));

            var startGroup = startGroups[edge.ChildTable + "::" + edge.ChildColumn];
            var endGroup = endGroups[edge.ParentTable + "::" + edge.ParentColumn];
            start = ConnectorAnchors.FanOut(
               start, childSide, startGroup.IndexOf(edge), startGroup.Count, 6);
            end = ConnectorAnchors.FanOut(
               end, parentSide, endGroup.IndexOf(edge), endGroup.Count, 6);

            anchorEdges.Add((start, end, edge));
         }

         if (onlyTable == null)
         {
            // Full re-route (initial render, delete, POCO edit).
            var routes = SequentialRouter.RouteAll(
               anchorEdges.Select(a => (a.Start, a.End)).ToList(),
               obstacles, bounds, routerOptions);
            _routes = anchorEdges
               .Select((a, i) => (a.Edge, (IReadOnlyList<Point2>)routes[i]))
               .ToList();
         }
         else
         {
            // Drag release: re-route only the moved table's edges, keeping
            // the stored routes for the rest as thin obstacles so the new
            // routes avoid them.
            var toRoute = anchorEdges
               .Where(a => a.Edge.ChildTable == onlyTable ||
                           a.Edge.ParentTable == onlyTable)
               .ToList();

            var thin = new List<Rect2>();
            foreach (var (edge, pts) in _routes)
            {
               if (edge.ChildTable == onlyTable || edge.ParentTable == onlyTable)
               {
                  continue;
               }
               AddSegmentObstacles(thin, pts, 4);
            }

            var newRoutes = new List<(FkRelation Edge, IReadOnlyList<Point2> Points)>();
            foreach (var a in toRoute)
            {
               var pts = OrthogonalRouter.Route(
                  a.Start, a.End, obstacles, bounds, routerOptions, thin);
               newRoutes.Add((a.Edge, pts));
               AddSegmentObstacles(thin, pts, 4);
            }

            _routes = _routes
               .Where(r => r.Edge.ChildTable != onlyTable &&
                           r.Edge.ParentTable != onlyTable)
               .Concat(newRoutes)
               .ToList();
         }

         foreach (var (edge, pts) in _routes)
         {
            var connector = _connectorFactory.CreateRouted(_context, pts);
            connector.Data = edge;

            // Endpoint markers; tag them with the connector so clicking a
            // circle also inspects the relationship.
            var startCircle = GlEllipse.Draw(
               _context, pts[0].X, pts[0].Y, 8, Colors.DodgerBlue);
            startCircle.NativeInstance.Tag = connector;
            var endCircle = GlEllipse.Draw(
               _context, pts[pts.Count - 1].X, pts[pts.Count - 1].Y, 8, Colors.DodgerBlue);
            endCircle.NativeInstance.Tag = connector;
         }

         foreach (var issue in issues)
         {
            WriteMessage("FK issue: " + issue);
         }
         WriteMessage("Drew " + _tables.Count + " tables and " +
            edges.Count + " FK connectors.");
      }

      /// <summary>
      /// A shape was dragged and released. Update the layout state for a
      /// moved table, then re-render so its connectors follow (and any
      /// connector drag snaps back to its routed position). Only the moved
      /// table's edges are re-routed (backlog 013).
      /// </summary>
      private void OnShapeReleased(GlObject obj)
      {
         if (obj is Table table)
         {
            string name = table.TableInfo.TableName;
            _layout[name] = new Rect2(
               table.X, table.Y, table.ComputedWidth, table.ComputedHeight);
            Render(onlyTable: name);
         }
      }

      /// <summary>
      /// Add each segment of a routed polyline to the thin-obstacle list as a
      /// rectangle around the segment (mirrors
      /// <see cref="SequentialRouter"/>'s internal helper).
      /// </summary>
      private static void AddSegmentObstacles(
         List<Rect2> thin, IReadOnlyList<Point2> pts, double margin)
      {
         for (int i = 0; i < pts.Count - 1; i++)
         {
            Point2 a = pts[i];
            Point2 b = pts[i + 1];
            if (a.Y == b.Y)
            {
               double x1 = Math.Min(a.X, b.X);
               double x2 = Math.Max(a.X, b.X);
               thin.Add(new Rect2(
                  x1 - margin, a.Y - margin,
                  (x2 - x1) + 2 * margin, 2 * margin));
            }
            else
            {
               double y1 = Math.Min(a.Y, b.Y);
               double y2 = Math.Max(a.Y, b.Y);
               thin.Add(new Rect2(
                  a.X - margin, y1 - margin,
                  2 * margin, (y2 - y1) + 2 * margin));
            }
         }
      }

      /// <summary>
      /// A shape was clicked. Raise <see cref="EntitySelected"/> with the
      /// underlying model entity (a <see cref="TableInfo"/> or
      /// <see cref="FkRelation"/>).
      /// </summary>
      private void OnShapeClicked(GlObject obj)
      {
         if (obj is Table table)
         {
            EntitySelected?.Invoke(this, table.TableInfo);
         }
         else if (obj is GlOrthoPath connector)
         {
            var edge = connector.Data as FkRelation;
            if (edge != null)
            {
               EntitySelected?.Invoke(this, edge);
            }
         }
      }

      /// <summary>
      /// Re-render after a POCO field was edited in the inspector.
      /// </summary>
      public void Refresh()
      {
         Render();
      }

      /// <summary>
      /// Remove the FK relationship behind a connector from the model and
      /// re-render; the remaining connectors regenerate as simple
      /// non-crossing routes.
      /// </summary>
      public void DeleteConnector(FkRelation edge)
      {
         var table = _tables.FirstOrDefault(t => t.TableName == edge.ChildTable);
         var column = table?.Columns?.FirstOrDefault(
            c => c.ColumnName == edge.ChildColumn);
         var constraint = column?.Constraints?.FirstOrDefault(
            c => c.IsForeignKey && c.ReferencedTableName == edge.ParentTable);
         if (constraint != null)
         {
            column.Constraints.Remove(constraint);
            column.IsForeignKey = column.Constraints.Any(c => c.IsForeignKey);
         }
         Render();
      }

   }
}
