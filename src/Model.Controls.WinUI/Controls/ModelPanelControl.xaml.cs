using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Text;
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
using ModelConsole.Diagnostics;
using ModelConsole.Editing;
using ModelConsole.Geometry;
using ModelConsole.Graphics.GLibrary;
using ModelConsole.Graphics.GLibrary.GlOrtho;
using ModelConsole.Graphics.Primitives;
using ModelConsole.Graphics.Services;
using ModelConsole.Graph;
using ModelConsole.Controls.Services;
using ModelConsole.Controls.Helpers;
using ModelConsole.Palette;

// The collapsed-group box model lives in Model.Graph; the drawable primitive
// in Model.Graphics.WinUI.Primitives — alias the model so the panel's box
// list holds the metadata (Group/Members/ExternalEdges) while the factory
// draws the primitive.
using GroupBoxModel = ModelConsole.Graph.GroupBox;
using XamlGroupBox = ModelConsole.Graphics.Primitives.GroupBox;

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

      /// <summary>Margin (px) left around the model when fitting to the window.</summary>
      private const double FitMargin = 5;

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
      private readonly IBoxFactory _boxFactory;
      private readonly IConnectorFactory _connectorFactory;
      private readonly IRectangleFactory _rectangleFactory;

      /// <summary>
      /// The model (source of truth). The drawing is always derived from this
      /// state, so any change (drag, delete, POCO edit) re-runs the render
      /// pipeline.
      /// </summary>
      private IReadOnlyList<TableInfo> _tables;

      /// <summary>
      /// The model's enumerations (named value-sets), when the model was
      /// loaded through the schema-driven interpreter (backlog 021). Null for
      /// array-format models, which carry no enumerations.
      /// </summary>
      private IReadOnlyDictionary<string, Enumeration> _enumerations;

      /// <summary>
      /// View-side visibility state (backlog 038): which groups (tags) draw
      /// and which tables are pinned show/hide. Created from the model on
      /// <see cref="SetModel"/>; the host shares the same instance with the
      /// Skia renderer so both paths agree on the visible set. Layout and
      /// routing always run over the visible projection.
      /// </summary>
      private EntityVisibility _visibility;

      /// <summary>
      /// View-side collapse state (backlog 039): which visible groups draw as
      /// one package-style box. A collapsed group's members are replaced by
      /// the box in the layout grid, the drawn set, and the router obstacle
      /// set — one rect for the group, and no connector crosses a box (the
      /// 012 invariant extended). Composes with <see cref="_visibility"/>: a
      /// hidden group collapses to nothing.
      /// </summary>
      private GroupCollapseState _collapse;

      /// <summary>
      /// The active grouping theme's name (backlog 043). The theme is
      /// model-dependent (connectivity needs the FK graph), so shared state is
      /// the <b>name</b> and the concrete <see cref="GroupingTheme"/> is
      /// derived from <c>_tables + name</c> at each use site — a model change
      /// (File → Open) re-derives it automatically and never leaves a stale
      /// connectivity theme behind.
      /// </summary>
      private string _themeName = GroupingThemes.TagsName;

      /// <summary>
      /// The collapsed groups' box models (backlog 039), in collapsed-set
      /// order — rebuilt over the visible projection on layout and render.
      /// </summary>
      private List<(string Key, GroupBoxModel Box)> _boxes =
         new List<(string Key, GroupBoxModel Box)>();

      /// <summary>
      /// Every table hidden behind a collapsed box (union of the boxes'
      /// members). A boxed table neither draws nor routes its own edges — the
      /// box's aggregated external edges replace them.
      /// </summary>
      private HashSet<string> _boxed =
         new HashSet<string>(StringComparer.Ordinal);

      /// <summary>
      /// The collapsed boxes' external connectors from the last render,
      /// parallel to <see cref="_boxRoutes"/> — the group↔target edges that
      /// route after the table FKs so no connector crosses a box.
      /// </summary>
      private List<(GroupBoxEdge Edge, IReadOnlyList<Point2> Points)> _boxRoutes =
         new List<(GroupBoxEdge Edge, IReadOnlyList<Point2> Points)>();

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
      /// Name of the currently selected table, highlighted on the canvas
      /// (set by <see cref="SelectTable"/>).
      /// </summary>
      private string _selectedTable;

      /// <summary>
      /// The drawing-surface (canvas) background color (backlog 041). The
      /// renderer-bar drop-down drives this and both renderers honor it;
      /// defaults to the shared palette's canvas background.
      /// </summary>
      private Color _backgroundColor =
         HexColor.FromHex(TablePalette.CanvasBackgroundHex);

      /// <summary>
      /// Delay before a hover readout appears (ms) — long enough that sweeping
      /// the pointer across the drawing never flashes tooltips (backlog 027).
      /// </summary>
      private static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(400);

      /// <summary>
      /// The graph node currently hovered (an <see cref="IGraphNode"/> over a
      /// <see cref="TableInfo"/> or <see cref="FkRelation"/>), or null when
      /// nothing is hovered. The tooltip content is rebuilt only when the
      /// node's live model object changes (backlog 027/028).
      /// </summary>
      private IGraphNode _hoverNode;

      /// <summary>
      /// Latest hover pointer position in canvas (content) coordinates; the
      /// tooltip is positioned from it (converted to viewport px) so it
      /// follows the pointer (backlog 027).
      /// </summary>
      private Point _hoverPosition;

      /// <summary>
      /// Delay trigger: when it fires, the hover readout is shown. Restarted
      /// when the hovered object changes, cancelled when the pointer leaves.
      /// (<see cref="UIElement.DispatcherQueue"/> is the WinUI 3
      /// <c>Microsoft.UI.Dispatching</c> queue, so its timer is that type —
      /// fully qualified because the file also imports the WinRT
      /// <c>Windows.System</c> timer.)
      /// </summary>
      private Microsoft.UI.Dispatching.DispatcherQueueTimer _hoverTimer;

      /// <summary>
      /// The connector (<see cref="GlOrthoPath"/>) currently hover-highlighted,
      /// or null. Emphasis is a thicker DodgerBlue line plus the
      /// <see cref="_hoverCircles"/> endpoint markers, so the dependency's
      /// start and end are unambiguous under the pointer.
      /// </summary>
      private GlOrthoPath _hoverConnector;

      /// <summary>
      /// Endpoint highlight circles drawn over a hovered connector's start/end
      /// (12 px DodgerBlue, hit-test transparent so they never intercept clicks
      /// or re-enter hover hit-testing); removed when the highlight clears.
      /// </summary>
      private List<GlEllipse> _hoverCircles;

      /// <summary>
      /// The table (<see cref="Table"/>) currently hovered (backlog 041), or
      /// null. Its border draws the thicker DodgerBlue accent while hovered;
      /// cleared when the pointer leaves or the hover moves to another node.
      /// </summary>
      private Table _hoverTable;

      /// <summary>
      /// The collapsed group box (<see cref="XamlGroupBox"/>) currently
      /// hovered (backlog 039), or null. Its border draws the same DodgerBlue
      /// accent, so a hovered box reads like a hovered table card.
      /// </summary>
      private XamlGroupBox _hoverBox;

      /// <summary>
      /// Raised when the user clicks a graphic entity. The payload is the
      /// entity's <see cref="TableInfo"/> or <see cref="FkRelation"/>.
      /// </summary>
      public event EventHandler<object> EntitySelected;

      /// <summary>
      /// Raised when the user double-clicks a graphic entity (backlog 042).
      /// The payload is the entity's <see cref="TableInfo"/> or
      /// <see cref="FkRelation"/>. The first click already raised
      /// <see cref="EntitySelected"/> (selection), so a double-click is
      /// select + open-details, never open-instead-of-select.
      /// </summary>
      public event EventHandler<object> EntityDoubleTapped;

      /// <summary>
      /// Raised when a collapsed group's box is clicked (backlog 039): the
      /// host expands the group on the shared collapse state — click a box →
      /// show its members again (the box's own collapse/expand affordance).
      /// </summary>
      public event EventHandler<string> GroupExpandRequested;

      /// <summary>
      /// Raised when the model is replaced (via <see cref="SetModel"/>); the
      /// explorer refreshes its tree.
      /// </summary>
      public event EventHandler ModelChanged;

      public ModelPanelControl()
      {
         this.InitializeComponent();

         // Drawing-surface base color (backlog 041): paint the drawing host
         // grid (the transparent canvas shows it through). The renderer-bar
         // drop-down overrides this later via BackgroundColor.
         DrawingSurface.Background = new SolidColorBrush(_backgroundColor);

         _dataProvider = Ioc.Default.GetRequiredService<IModelDataProvider>();
         _tableFactory = Ioc.Default.GetRequiredService<ITableFactory>();
         _boxFactory = Ioc.Default.GetRequiredService<IBoxFactory>();
         _connectorFactory = Ioc.Default.GetRequiredService<IConnectorFactory>();
         _rectangleFactory = Ioc.Default.GetRequiredService<IRectangleFactory>();
         _context = new GlContext(
            ModelCanvas, Ioc.Default.GetRequiredService<ILogService>());

         _context.ShapeReleased += OnShapeReleased;
         _context.ShapeClicked += OnShapeClicked;
         _context.ShapeDoubleClicked += OnShapeDoubleClicked;
         _context.PanRequested += OnPanRequested;
         _context.HoverChanged += OnHoverChanged;

         // Hover readout delay trigger (backlog 027): show the tooltip only
         // after the pointer has rested on an object for the hover delay.
         _hoverTimer = DispatcherQueue.CreateTimer();
         _hoverTimer.Interval = HoverDelay;
         _hoverTimer.IsRepeating = false;
         _hoverTimer.Tick += (s, e) => ShowHover();

         _tables = _dataProvider.GetPublicSafetyTables();
         _visibility = EntityVisibility.Create(_tables, CurrentTheme);
         _collapse = new GroupCollapseState();
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

      /// <summary>
      /// The current model (source of truth for the drawing).
      /// </summary>
      public IReadOnlyList<TableInfo> Tables
      {
         get { return _tables; }
      }

      /// <summary>
      /// The current model's enumerations (backlog 021), for the inspector's
      /// value-set readout. Null when the model carries none.
      /// </summary>
      public IReadOnlyDictionary<string, Enumeration> Enumerations
      {
         get { return _enumerations; }
      }

      /// <summary>
      /// The view-side visibility state (backlog 038). The host creates it for
      /// a model and shares the same instance with the Skia renderer, so both
      /// paths draw the identical visible set.
      /// </summary>
      public EntityVisibility CurrentVisibility
      {
         get { return _visibility; }
      }

      /// <summary>
      /// Apply a (possibly mutated) visibility and re-render: the layout
      /// re-flows over the visible subset and the routes regenerate, so a
      /// group toggle or pin change narrows the drawing immediately.
      /// </summary>
      public void SetVisibility(EntityVisibility visibility)
      {
         _visibility = visibility ?? EntityVisibility.Create(_tables, CurrentTheme);
         InitializeLayout();
         Render();
      }

      /// <summary>
      /// The view-side collapse state (backlog 039). The host creates it for a
      /// model and shares the same instance with the Skia renderer, so both
      /// paths draw the identical collapsed boxes.
      /// </summary>
      public GroupCollapseState CurrentCollapse
      {
         get { return _collapse; }
      }

      /// <summary>
      /// Apply a (possibly mutated) collapse state and re-render: the collapsed
      /// groups' members become one box in the layout, the drawing, and the
      /// router obstacles, so a toggle narrows the drawing immediately
      /// (mirroring <see cref="SetVisibility"/>).
      /// </summary>
      public void SetCollapse(GroupCollapseState collapse)
      {
         _collapse = collapse ?? new GroupCollapseState();
         InitializeLayout();
         Render();
      }

      /// <summary>
      /// The active grouping theme's name (backlog 043) — the explorer's
      /// "Group by:" value. The host keeps the Skia renderer in parity via
      /// <see cref="SetTheme"/>.
      /// </summary>
      public string CurrentThemeName
      {
         get { return _themeName; }
      }

      /// <summary>
      /// The active grouping theme, derived from the current model + theme name
      /// (backlog 043). Never cached — the connectivity theme depends on the
      /// FK graph, so it is re-derived at each use site.
      /// </summary>
      public GroupingTheme CurrentTheme
      {
         get { return GroupingThemes.FromName(_themeName, _tables); }
      }

      /// <summary>
      /// Switch the grouping theme and re-render (backlog 043): the group
      /// universe changes, so the visibility is re-created over the new theme
      /// (every new group starts visible) and the collapse state resets (a
      /// collapsed group from the old theme may not exist in the new one).
      /// </summary>
      public void SetTheme(string themeName)
      {
         _themeName = themeName ?? GroupingThemes.TagsName;
         _visibility = EntityVisibility.Create(_tables, CurrentTheme);
         _collapse = new GroupCollapseState();
         InitializeLayout();
         Render();
      }

      /// <summary>
      /// The drawing-surface (canvas) background color (backlog 041). Painting
      /// the drawing host grid shows the color through the transparent canvas
      /// and behind the paper at low zoom. The renderer-bar drop-down sets
      /// this on both renderers.
      /// </summary>
      public Color BackgroundColor
      {
         get { return _backgroundColor; }
         set
         {
            _backgroundColor = value;
            if (DrawingSurface != null)
            {
               DrawingSurface.Background = new SolidColorBrush(value);
            }
         }
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
      /// Per-notch wheel zoom factor (1.1 = a gentle 10% step, proportional
      /// to the actual wheel delta so trackpads get even smaller steps).
      /// The keyboard accelerators use the coarser ZoomStep instead.
      /// </summary>
      private const double WheelZoomStep = 1.1;

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
      /// given anchor fixed (default: the viewport center). Clamped to the
      /// ScrollViewer's zoom range. The anchor is a viewport-px point — the
      /// wheel handler passes the cursor position so the drawing zooms
      /// around the mouse.
      ///
      /// ScrollViewer offsets are measured in viewport px (zoom-applied):
      /// a content point c sits at viewport px c*zoom - offset. So the
      /// content point under the anchor is (anchorPx + offset)/oldZoom and
      /// the offset that keeps it at the same viewport px is
      /// content*zoom - anchorPx (backlog 018 - the previous content-units
      /// model made the wheel zoom drift and the Fit button show blank).
      /// </summary>
      private void ApplyZoom(double zoom, double? anchorX = null, double? anchorY = null)
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

         double ax = anchorX ?? ModelScrollViewer.ViewportWidth / 2.0;
         double ay = anchorY ?? ModelScrollViewer.ViewportHeight / 2.0;
         double cx = (ax + ModelScrollViewer.HorizontalOffset) / oldZoom;
         double cy = (ay + ModelScrollViewer.VerticalOffset) / oldZoom;
         double newH = cx * zoom - ax;
         double newV = cy * zoom - ay;

         ModelScrollViewer.ChangeView(newH, newV, (float)zoom, true);
      }

      /// <summary>
      /// The mouse wheel zooms the drawing around the cursor instead of
      /// scrolling (the user's requested behavior). Handled on the canvas so
      /// the event never bubbles to the ScrollViewer's scroll handler.
      /// Wheel up zooms out, wheel down zooms in (the user's requested
      /// direction), keeping the content point under the cursor fixed.
      /// The cursor is read relative to the ScrollViewer (viewport px), the
      /// unambiguous anchor space for ApplyZoom (backlog 018); the step is
      /// smooth and proportional to the wheel delta.
      /// </summary>
      private void ModelCanvas_PointerWheelChanged(
         object sender, PointerRoutedEventArgs e)
      {
         var point = e.GetCurrentPoint(ModelScrollViewer);
         double delta = point.Properties.MouseWheelDelta;
         if (delta == 0)
         {
            return;
         }
         double factor = Math.Pow(WheelZoomStep, -delta / 120.0);
         ApplyZoom(ModelScrollViewer.ZoomFactor * factor,
            point.Position.X, point.Position.Y);
         e.Handled = true;
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
         // large "paper" whose extent would zoom the drawing way out. Leave
         // a ~5 px margin around the model. Center the content at the fit
         // zoom: offsets are viewport px, so the content center must land on
         // the viewport center - hOff = center*fit - vw/2 (backlog 018; the
         // previous content-units formula clamped to ~0 and showed blank).
         double fit = Math.Min((vw - 2 * FitMargin) / b.Width,
                               (vh - 2 * FitMargin) / b.Height);
         fit = Math.Max(MinZoom, Math.Min(1.0, fit));
         double hOff = (b.X + b.Width / 2.0) * fit - vw / 2.0;
         double vOff = (b.Y + b.Height / 2.0) * fit - vh / 2.0;

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
         // The view moved (pan / zoom / fit): the tooltip's content position
         // would be stale — close it (backlog 027).
         HideHover();
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
      /// A pan gesture moved the drawing. The delta is measured from the pan
      /// start point in Canvas-local (content) coordinates, but ScrollViewer
      /// offsets are viewport px, so a content delta dx shifts the viewport
      /// by dx * zoom - multiply to keep the pan 1:1 with the pointer at any
      /// zoom (backlog 018). Preserves the current zoom (null zoom in
      /// ChangeView = keep it).
      /// </summary>
      private void OnPanRequested(double dx, double dy)
      {
         if (ModelScrollViewer == null)
         {
            return;
         }
         double zoom = ModelScrollViewer.ZoomFactor;
         ModelScrollViewer.ChangeView(
            ModelScrollViewer.HorizontalOffset - dx * zoom,
            ModelScrollViewer.VerticalOffset - dy * zoom,
            null, true);
      }

      /// <summary>
      /// Compute the initial grid layout of the tables + collapsed boxes and
      /// store it as the current state (dragging updates it).
      /// </summary>
      private void InitializeLayout()
      {
         // Backlog 038: layout runs over the visible projection only, so hiding
         // a group re-flows the shown tables into a compact grid (the hidden
         // tables are simply not laid out; they come back on "Show all").
         // Backlog 039: a collapsed group's members are REPLACED by one box (the
         // 2000-table win) — a synthetic table per box feeds the grid engine
         // unchanged (it reads only TableName), so a boxed member neither draws
         // nor blocks the router.
         var (allEdges, _) = FkEdgeExtractor.Extract(_tables);
         var (visibleTables, visibleEdges) =
            ModelProjection.Project(_tables, allEdges, _visibility);
         (_boxes, _boxed) = BuildBoxes(visibleTables, visibleEdges);

         // Measure each table + box without drawing so every grid slot fits the
         // widest/tallest table or box and no table overlaps its neighbour.
         double maxWidth = 0, maxHeight = 0;
         foreach (var t in visibleTables)
         {
            if (_boxed.Contains(t.TableName))
            {
               continue;
            }
            var probe = new Table(_context, 0, 0, BannerHeight, t);
            maxWidth = Math.Max(maxWidth, probe.ComputedWidth);
            maxHeight = Math.Max(maxHeight, probe.ComputedHeight);
         }
         foreach (var (key, box) in _boxes)
         {
            var probe = new XamlGroupBox(_context, 0, 0, box.Group, box.Members);
            maxWidth = Math.Max(maxWidth, probe.ComputedWidth);
            maxHeight = Math.Max(maxHeight, probe.ComputedHeight);
         }

         var layoutTables = visibleTables
            .Where(t => !_boxed.Contains(t.TableName))
            .ToList();
         foreach (var (key, _) in _boxes)
         {
            layoutTables.Add(new TableInfo { TableName = key });
         }
         var layout = TableLayoutEngine.Layout(layoutTables, new GridLayoutOptions
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
      /// Build the collapsed boxes over the visible projection (backlog 039):
      /// one box per collapsed group with visible members, and the set of
      /// tables hidden behind them. A group hidden by 038 visibility
      /// collapses to nothing (zero visible members — no box draws).
      /// </summary>
      private (List<(string Key, GroupBoxModel Box)> Boxes, HashSet<string> Boxed)
         BuildBoxes(IReadOnlyList<TableInfo> visibleTables,
            IReadOnlyList<FkRelation> visibleEdges)
      {
         var collapsed = _collapse == null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(_collapse.CollapsedGroups, StringComparer.Ordinal);

         var boxes = new List<(string Key, GroupBoxModel Box)>();
         if (collapsed.Count > 0)
         {
            foreach (var group in collapsed)
            {
               var model = GroupBoxAggregation.Build(
                  visibleTables, visibleEdges, group, collapsed, CurrentTheme);
               if (model.MemberCount == 0)
               {
                  continue; // no visible members — nothing to collapse
               }
               boxes.Add((GroupBoxAggregation.BoxKey(group), model));
            }
         }

         var boxed = new HashSet<string>(StringComparer.Ordinal);
         foreach (var (_, box) in boxes)
         {
            foreach (var m in box.Members)
            {
               boxed.Add(m.TableName);
            }
         }
         return (boxes, boxed);
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

         // The shapes are being rebuilt, so any hovered node is stale — close
         // the readout (backlog 027/028) and drop any connector emphasis (the
         // canvas children were cleared, so the highlight circles are gone).
         // The hovered-table reference is stale too (the tables are rebuilt);
         // its border is redrawn by the new instances.
         HideHover();
         _hoverNode = null;
         _hoverConnector = null;
         _hoverCircles = null;
         _hoverTable = null;
         _hoverBox = null;

         IGlModel model = Ioc.Default.GetRequiredService<IGlModel>();

         // Backlog 038: extract the FULL edge set first so unresolved-FK
         // diagnostics still resolve for edges coming back from hidden tables,
         // then project to the visible subset both renderers agree on. Layout
         // and routing consume only the projection (parity with the Skia path).
         var (allEdges, issues) = FkEdgeExtractor.Extract(_tables);
         var (visibleTables, visibleEdges) =
            ModelProjection.Project(_tables, allEdges, _visibility);

         // Backlog 039: rebuild the collapsed boxes over the visible
         // projection — a collapsed group's members are replaced by one box in
         // the drawn set and the router obstacle set, so the box is the only
         // shape a connector must avoid (the 012 invariant extended).
         (_boxes, _boxed) = BuildBoxes(visibleTables, visibleEdges);

         var drawn = new Dictionary<string, Table>();
         foreach (var t in visibleTables)
         {
            if (_boxed.Contains(t.TableName))
            {
               continue;
            }
            if (!_layout.TryGetValue(t.TableName, out var rect))
            {
               continue;
            }
            var table = _tableFactory.Create(
               _context, (float)rect.X, (float)rect.Y, BannerHeight, t);
            drawn[t.TableName] = table;
            model.Add(table);
         }

         // Draw every collapsed box — a first-class drawable, draggable,
         // hoverable, and clickable like a table.
         var drawnBoxes = new Dictionary<string, XamlGroupBox>();
         foreach (var (key, box) in _boxes)
         {
            if (!_layout.TryGetValue(key, out var rect))
            {
               continue;
            }
            var g = _boxFactory.Create(
               _context, (float)rect.X, (float)rect.Y, box.Group, box.Members);
            drawnBoxes[key] = g;
            model.Add(g);
         }

         // Content bounds (the bounding box of all drawn tables + boxes) drive
         // the fit button and the router region.
         double minX = double.MaxValue, minY = double.MaxValue;
         double maxX = 0, maxY = 0;
         foreach (var t in drawn.Values)
         {
            minX = Math.Min(minX, t.X);
            minY = Math.Min(minY, t.Y);
            maxX = Math.Max(maxX, t.X + t.ComputedWidth);
            maxY = Math.Max(maxY, t.Y + t.ComputedHeight);
         }
         foreach (var b in drawnBoxes.Values)
         {
            minX = Math.Min(minX, b.X);
            minY = Math.Min(minY, b.Y);
            maxX = Math.Max(maxX, b.X + b.ComputedWidth);
            maxY = Math.Max(maxY, b.Y + b.ComputedHeight);
         }
         if (drawn.Count == 0 && drawnBoxes.Count == 0)
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

         // Route every connector around the drawn tables + boxes, sequentially
         // so no connector crosses another (nor a collapsed box — the box is
         // in the obstacle set).
         var obstacles = drawn.Values
            .Select(t => new Rect2(t.X, t.Y, t.ComputedWidth, t.ComputedHeight))
            .Concat(drawnBoxes.Values.Select(b =>
               new Rect2(b.X, b.Y, b.ComputedWidth, b.ComputedHeight)))
            .ToList();
         var routerOptions = new RouterOptions
         {
            GridSize = 16,
            ObstacleMargin = 14,
            StubLength = 20
         };

         // Resolve anchors (departure side chosen from the relative position
         // of the two tables) and fan out connectors that share a column —
         // over the table-level edges only (an edge touching a collapsed box
         // is represented by the box's aggregated external edge instead).
         var tableEdges = visibleEdges
            .Where(e => !_boxed.Contains(e.ChildTable) &&
                        !_boxed.Contains(e.ParentTable))
            .ToList();

         var startGroups = tableEdges
            .GroupBy(e => e.ChildTable + "::" + e.ChildColumn)
            .ToDictionary(g => g.Key, g => g.ToList());
         var endGroups = tableEdges
            .GroupBy(e => e.ParentTable + "::" + e.ParentColumn)
            .ToDictionary(g => g.Key, g => g.ToList());

         var anchorEdges = new List<(Point2 Start, Point2 End, FkRelation Edge)>();
         foreach (var edge in tableEdges)
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

         // Backlog 039: the boxes' external connectors — one anchor pair per
         // external target, the box side facing the target resolved from the
         // relative position of the two rects (the box anchors at its vertical
         // midpoint — it has no column rows). Target rects are the drawn
         // tables/boxes (actual sizes, not slots), so the connector touches
         // the box's real boundary.
         var boxAnchors = new List<(Point2 Start, Point2 End, GroupBoxEdge Edge)>();
         foreach (var (key, box) in _boxes)
         {
            if (!drawnBoxes.TryGetValue(key, out var boxObj))
            {
               continue;
            }
            var boxRect = new Rect2(
               boxObj.X, boxObj.Y, boxObj.ComputedWidth, boxObj.ComputedHeight);
            foreach (var be in box.ExternalEdges)
            {
               string targetKey = be.TargetGroup != null
                  ? GroupBoxAggregation.BoxKey(be.TargetGroup)
                  : be.TargetTable;
               Rect2 targetRect;
               if (drawn.TryGetValue(targetKey, out var targetTable))
               {
                  targetRect = new Rect2(targetTable.X, targetTable.Y,
                     targetTable.ComputedWidth, targetTable.ComputedHeight);
               }
               else if (drawnBoxes.TryGetValue(targetKey, out var targetBox))
               {
                  targetRect = new Rect2(targetBox.X, targetBox.Y,
                     targetBox.ComputedWidth, targetBox.ComputedHeight);
               }
               else
               {
                  continue; // target box collapsed to nothing (all hidden)
               }

               var (start, end, _, _) = ConnectorAnchors.Resolve(
                  boxRect, targetRect, boxRect.Center.Y, targetRect.Center.Y);
               boxAnchors.Add((start, end, be));
            }
         }

         if (onlyTable == null || onlyTable.StartsWith("group::"))
         {
            // Full re-route (initial render, delete, POCO edit, box drag):
            // table FKs first, then the boxes' external connectors,
            // sequentially so no connector crosses another — nor any box,
            // which is in the obstacle set.
            var allAnchors = anchorEdges
               .Select(a => (a.Start, a.End))
               .Concat(boxAnchors.Select(a => (a.Start, a.End)))
               .ToList();
            var routes = SequentialRouter.RouteAll(
               allAnchors, obstacles, bounds, routerOptions);
            _routes = anchorEdges
               .Select((a, i) => (a.Edge, (IReadOnlyList<Point2>)routes[i]))
               .ToList();
            _boxRoutes = boxAnchors
               .Select((a, i) => (a.Edge,
                  (IReadOnlyList<Point2>)routes[anchorEdges.Count + i]))
               .ToList();
         }
         else
         {
            // Drag release: re-route only the moved table's edges, keeping
            // the stored routes for the rest as thin obstacles so the new
            // routes avoid them. Box connectors touching the moved table
            // re-route too (their anchors moved with it).
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
            foreach (var (edge, pts) in _boxRoutes)
            {
               if (edge.TargetTable == onlyTable)
               {
                  continue;
               }
               AddSegmentObstacles(thin, pts, 4);
            }

            var newRoutes = new List<(FkRelation Edge, IReadOnlyList<Point2> Points)>();
            foreach (var a in toRoute)
            {
               var pts = OrthogonalRouter.RouteBest(
                  a.Start, a.End, obstacles, bounds, routerOptions, thin);
               newRoutes.Add((a.Edge, pts));
               AddSegmentObstacles(thin, pts, 4);
            }

            _routes = _routes
               .Where(r => r.Edge.ChildTable != onlyTable &&
                           r.Edge.ParentTable != onlyTable)
               .Concat(newRoutes)
               .ToList();

            var toBoxRoute = boxAnchors
               .Where(a => a.Edge.TargetTable == onlyTable)
               .ToList();
            var newBoxRoutes = new List<(GroupBoxEdge Edge, IReadOnlyList<Point2> Points)>();
            foreach (var a in toBoxRoute)
            {
               var pts = OrthogonalRouter.RouteBest(
                  a.Start, a.End, obstacles, bounds, routerOptions, thin);
               newBoxRoutes.Add((a.Edge, pts));
               AddSegmentObstacles(thin, pts, 4);
            }
            _boxRoutes = _boxRoutes
               .Where(r => r.Edge.TargetTable != onlyTable)
               .Concat(newBoxRoutes)
               .ToList();
         }

         foreach (var (edge, pts) in _routes)
         {
            DrawRoutedConnector(pts, edge);
         }
         foreach (var (edge, pts) in _boxRoutes)
         {
            DrawRoutedConnector(pts, edge);
         }

         // Selection highlight: an accent outline around the selected table,
         // drawn on top and hit-test transparent so it never intercepts
         // clicks (the table itself stays clickable).
         if (_selectedTable != null &&
             drawn.TryGetValue(_selectedTable, out var selected))
         {
            var highlight = _rectangleFactory.Draw(
               _context, selected.X, selected.Y,
               selected.ComputedWidth, selected.ComputedHeight, 10);
            highlight.NativeInstance.Stroke =
               new SolidColorBrush(Colors.DodgerBlue);
            highlight.NativeInstance.StrokeThickness = 2;
            highlight.NativeInstance.Fill = null;
            highlight.NativeInstance.IsHitTestVisible = false;
         }

         foreach (var issue in issues)
         {
            WriteMessage("FK issue: " + issue);
         }
         WriteMessage("Drew " + drawn.Count + " tables, " +
            drawnBoxes.Count + " collapsed groups and " +
            (_routes.Count + _boxRoutes.Count) + " FK connectors.");
      }


      /// <summary>
      /// Draw one routed connector: the polyline plus its endpoint markers,
      /// tagged with the route's data (an <see cref="FkRelation"/> for a table
      /// edge, a <see cref="GroupBoxEdge"/> for a collapsed box's connector)
      /// so clicking a circle also inspects the relationship.
      /// </summary>
      private void DrawRoutedConnector(IReadOnlyList<Point2> pts, object data)
      {
         var connector = _connectorFactory.CreateRouted(_context, pts);
         connector.Data = data;

         var startCircle = GlEllipse.Draw(
            _context, pts[0].X, pts[0].Y, 8, Colors.DodgerBlue);
         startCircle.NativeInstance.Tag = connector;
         var endCircle = GlEllipse.Draw(
            _context, pts[pts.Count - 1].X, pts[pts.Count - 1].Y, 8, Colors.DodgerBlue);
         endCircle.NativeInstance.Tag = connector;
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
         else if (obj is XamlGroupBox box)
         {
            // A collapsed box dragged like a table: move the whole group by
            // moving its one rect (a full re-route — the box's external
            // connectors follow).
            string key = GroupBoxAggregation.BoxKey(box.Group);
            _layout[key] = new Rect2(
               box.X, box.Y, box.ComputedWidth, box.ComputedHeight);
            Render(onlyTable: key);
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
      /// node's live model object (a <see cref="TableInfo"/> for a table or
      /// an <see cref="FkRelation"/> for a connector) — the inspector
      /// contract is unchanged (backlog 028).
      /// </summary>
      private void OnShapeClicked(GlObject obj)
      {
         // A collapsed group's box has no inspector surface yet — clicking it
         // expands the group (backlog 039's box affordance).
         if (obj is XamlGroupBox box)
         {
            GroupExpandRequested?.Invoke(this, box.Group);
            return;
         }

         var node = obj?.Node;
         if (node != null)
         {
            EntitySelected?.Invoke(this, node.Model);
         }
      }

      /// <summary>
      /// A shape was double-clicked (backlog 042): raise
      /// <see cref="EntityDoubleTapped"/> with the node's live model object (a
      /// <see cref="TableInfo"/> or an <see cref="FkRelation"/>) so the host
      /// can open the details window. Double-clicking a collapsed box does
      /// nothing extra — a single click already expands it.
      /// </summary>
      private void OnShapeDoubleClicked(GlObject obj)
      {
         if (obj is XamlGroupBox)
         {
            return;
         }
         var node = obj?.Node;
         if (node != null)
         {
            EntityDoubleTapped?.Invoke(this, node.Model);
         }
      }

      /// <summary>
      /// The pointer moved over the drawing. Track the hovered node and drive
      /// the delay-triggered, pointer-following readout (backlog 027/028):
      /// a null node closes it; a changed node restarts the delay; moving
      /// within the same node only repositions an already-visible tooltip.
      /// Identity is the node's live model object (stable within a render;
      /// <see cref="Render"/> hides the readout on re-render).
      /// </summary>
      private void OnHoverChanged(GlObject obj, Point position)
      {
         _hoverPosition = position;

         var node = obj?.Node;
         if (node == null)
         {
            ClearConnectorHighlight();
            ClearTableHover();
            ClearBoxHover();
            _hoverNode = null;
            _hoverTimer.Stop();
            HideHover();
            return;
         }

         if (!ReferenceEquals(node.Model, _hoverNode?.Model))
         {
            // A different node is hovered: drop any connector emphasis and
            // any card-border emphasis (table or collapsed box), then apply
            // the new node's — a connector (GlOrthoPath) thickens + enlarges
            // its markers so the dependency's start and end are unambiguous; a
            // table or a collapsed group box (backlog 041/039) draws its
            // thicker DodgerBlue accent border.
            if (!ReferenceEquals(obj, _hoverConnector))
            {
               ClearConnectorHighlight();
               if (obj is GlOrthoPath connector)
               {
                  ApplyConnectorHighlight(connector);
               }
            }
            if (!ReferenceEquals(obj, _hoverTable))
            {
               ClearTableHover();
               if (obj is Table table)
               {
                  _hoverTable = table;
                  table.Hovered = true;
               }
            }
            if (!ReferenceEquals(obj, _hoverBox))
            {
               ClearBoxHover();
               if (obj is XamlGroupBox box)
               {
                  _hoverBox = box;
                  box.Hovered = true;
               }
            }

            _hoverNode = node;
            _hoverTimer.Stop();
            HideHover();
            _hoverTimer.Start();
         }
         else if (HoverTooltip.Visibility == Visibility.Visible)
         {
            PositionHover();
         }
      }

      /// <summary>
      /// Emphasize a hovered connector: thicken its line to SlateBlue — the
      /// analogous (violet) neighbor of DodgerBlue, so the hovered connector
      /// pops out of the DodgerBlue rest-state connectors (the same #6A5ACD
      /// the Skia renderer's emphasized <c>Connector</c> uses) — and draw
      /// larger DodgerBlue endpoint circles over the route's first/last points
      /// so start/end read at a glance. The circles are hit-test transparent so
      /// they never intercept clicks or re-enter hover hit-testing.
      /// </summary>
      private void ApplyConnectorHighlight(GlOrthoPath connector)
      {
         _hoverConnector = connector;
         connector.Path.Stroke = new SolidColorBrush(Colors.SlateBlue);
         connector.Path.StrokeThickness = 3.5;

         // The highlighted route is a table FK or a collapsed box's external
         // connector — look in both route lists (the data is the route's
         // FkRelation / GroupBoxEdge).
         IReadOnlyList<Point2> pts = null;
         foreach (var (edge, routePts) in _routes)
         {
            if (ReferenceEquals(edge, connector.Data))
            {
               pts = routePts;
               break;
            }
         }
         if (pts == null)
         {
            foreach (var (edge, routePts) in _boxRoutes)
            {
               if (ReferenceEquals(edge, connector.Data))
               {
                  pts = routePts;
                  break;
               }
            }
         }
         if (pts == null || pts.Count == 0)
         {
            ClearConnectorHighlight();
            return;
         }

         _hoverCircles = new List<GlEllipse>
         {
            GlEllipse.Draw(_context, pts[0].X, pts[0].Y, 12, Colors.DodgerBlue),
            GlEllipse.Draw(
               _context, pts[pts.Count - 1].X, pts[pts.Count - 1].Y, 12, Colors.DodgerBlue)
         };
         foreach (var circle in _hoverCircles)
         {
            circle.NativeInstance.IsHitTestVisible = false;
         }
      }

      /// <summary>
      /// Restore the highlighted connector's line to its rest state (1 px
      /// black — the <see cref="GlOrthoPath"/> defaults) and remove its
      /// endpoint highlight circles. Safe to call when nothing is highlighted.
      /// </summary>
      private void ClearConnectorHighlight()
      {
         if (_hoverConnector != null)
         {
            _hoverConnector.Path.Stroke = new SolidColorBrush(Colors.Black);
            _hoverConnector.Path.StrokeThickness = 1;
            _hoverConnector = null;
         }

         if (_hoverCircles != null)
         {
            foreach (var circle in _hoverCircles)
            {
               _context.Instance.Children.Remove(circle.NativeInstance);
            }
            _hoverCircles = null;
         }
      }

      /// <summary>
      /// Restore the hovered table's border to its rest state (the shared
      /// neutral border). Safe to call when no table is emphasized.
      /// </summary>
      private void ClearTableHover()
      {
         if (_hoverTable != null)
         {
            _hoverTable.Hovered = false;
            _hoverTable = null;
         }
      }

      /// <summary>
      /// Restore the hovered collapsed box's border to its rest state (the
      /// shared neutral border). Safe to call when no box is emphasized.
      /// </summary>
      private void ClearBoxHover()
      {
         if (_hoverBox != null)
         {
            _hoverBox.Hovered = false;
            _hoverBox = null;
         }
      }

      /// <summary>
      /// Show the hover readout at the pointer: build the content from the
      /// hovered node's portable summary (backlog 027/028) and position it.
      /// The first line is the header; the rest are gray detail lines — the
      /// same readout lines the inspector shows.
      /// </summary>
      private void ShowHover()
      {
         if (_hoverNode == null)
         {
            return;
         }

         var lines = _hoverNode.Summary();
         if (lines.Count == 0)
         {
            HideHover();
            return;
         }

         HoverTooltipContent.Children.Clear();
         for (int i = 0; i < lines.Count; i++)
         {
            HoverTooltipContent.Children.Add(new TextBlock
            {
               Text = lines[i],
               FontSize = i == 0 ? 12 : 11,
               FontWeight = i == 0 ? FontWeights.SemiBold : FontWeights.Normal,
               Foreground = i == 0
                  ? new SolidColorBrush(Microsoft.UI.Colors.Black)
                  : new SolidColorBrush(Microsoft.UI.Colors.Gray),
               TextWrapping = TextWrapping.WrapWholeWords
            });
         }

         HoverTooltip.Visibility = Visibility.Visible;
         HoverTooltip.UpdateLayout();
         PositionHover();
      }

      /// <summary>
      /// Position the tooltip near the pointer. The pointer position is in
      /// canvas (content) coordinates; ScrollViewer offsets are viewport px, so
      /// content is mapped with c*zoom - offset (backlog 018's mapping) and
      /// clamped to the overlay so the tooltip stays on screen.
      /// </summary>
      private void PositionHover()
      {
         if (ModelScrollViewer == null ||
             HoverTooltip.Visibility != Visibility.Visible)
         {
            return;
         }

         double zoom = ModelScrollViewer.ZoomFactor;
         double x = _hoverPosition.X * zoom -
            ModelScrollViewer.HorizontalOffset + 12;
         double y = _hoverPosition.Y * zoom -
            ModelScrollViewer.VerticalOffset + 12;

         double maxX = HoverOverlay.ActualWidth - HoverTooltip.ActualWidth - 4;
         double maxY = HoverOverlay.ActualHeight - HoverTooltip.ActualHeight - 4;
         if (maxX > 0) x = Math.Min(x, maxX);
         if (maxY > 0) y = Math.Min(y, maxY);

         Canvas.SetLeft(HoverTooltip, Math.Max(0, x));
         Canvas.SetTop(HoverTooltip, Math.Max(0, y));
      }

      /// <summary>
      /// Hide the hover readout and cancel its pending delay trigger.
      /// </summary>
      private void HideHover()
      {
         _hoverTimer.Stop();
         HoverTooltip.Visibility = Visibility.Collapsed;
      }

      /// <summary>
      /// Re-render after a POCO field was edited in the inspector.
      /// </summary>
      public void Refresh()
      {
         Render();
      }

      /// <summary>
      /// Replace the model and re-render from scratch (used by File → Open).
      /// The optional enumerations (backlog 021) come from the schema-driven
      /// interpreter and feed the inspector's value-set readout.
      /// </summary>
      public void SetModel(
         IReadOnlyList<TableInfo> tables,
         IReadOnlyDictionary<string, Enumeration> enumerations = null)
      {
         _tables = tables;
         _enumerations = enumerations;
         _selectedTable = null;
         // A fresh model starts with every group visible and no pins (backlog
         // 038) and every group expanded (backlog 039); the host may share its
         // own instances via SetVisibility / SetCollapse after. The grouping
         // theme (backlog 043) re-derives from the new tables + theme name.
         _visibility = EntityVisibility.Create(tables, CurrentTheme);
         _collapse = new GroupCollapseState();
         InitializeLayout();
         Render();
         ModelChanged?.Invoke(this, EventArgs.Empty);
      }

      /// <summary>
      /// Select a table by name: highlight it on the canvas and raise
      /// <see cref="EntitySelected"/> so the inspector shows it.
      /// </summary>
      public void SelectTable(string tableName)
      {
         if (_tables == null || _tables.All(t => t.TableName != tableName))
         {
            return;
         }
         _selectedTable = tableName;
         Render();
         var table = _tables.First(t => t.TableName == tableName);
         EntitySelected?.Invoke(this, table);
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

      /// <summary>
      /// Rename a table (backlog 029): cascade the rename across every
      /// referencing FK, re-key the layout, and re-render (full — the name
      /// is part of the table's identity).
      /// </summary>
      public void RenameTable(TableInfo table, string oldName, string newName)
      {
         ModelEdits.RenameTable(_tables, table, newName);
         if (_layout.TryGetValue(oldName, out var rect))
         {
            _layout[newName] = rect;
            _layout.Remove(oldName);
         }
         if (_selectedTable == oldName)
         {
            _selectedTable = newName;
         }
         Render();
         ModelChanged?.Invoke(this, EventArgs.Empty);
      }

      /// <summary>
      /// Rename a column (backlog 029): cascade the rename across every
      /// referencing FK, re-measure the table (the name is part of its
      /// width), and re-route only its edges.
      /// </summary>
      public void RenameColumn(
         TableInfo table, ColumnInfo column, string oldName, string newName)
      {
         ModelEdits.RenameColumn(_tables, table, column, newName);
         ReMeasureTable(table);
         Render(onlyTable: table.TableName);
         ModelChanged?.Invoke(this, EventArgs.Empty);
      }

      /// <summary>
      /// Remove a table from the model and re-render; referencing FKs surface
      /// as resolution issues (never a crash or a dangling connector).
      /// </summary>
      public void RemoveTable(TableInfo table)
      {
         _tables = _tables.Where(t => !ReferenceEquals(t, table)).ToList();
         _layout.Remove(table.TableName);
         if (_selectedTable == table.TableName)
         {
            _selectedTable = null;
         }
         Render();
         ModelChanged?.Invoke(this, EventArgs.Empty);
      }

      /// <summary>
      /// Add a table to the model, re-layout (the new table needs a slot),
      /// and re-render.
      /// </summary>
      public void AddTable(TableInfo table)
      {
         _tables = _tables.Concat(new[] { table }).ToList();
         InitializeLayout();
         Render();
         ModelChanged?.Invoke(this, EventArgs.Empty);
      }

      /// <summary>
      /// A structural edit to a table (column add/remove/rename, FK
      /// add/remove/target, key toggle): re-measure the table and re-route
      /// only its edges (backlog 013 partial re-route).
      /// </summary>
      public void StructureChanged(TableInfo table)
      {
         ReMeasureTable(table);
         Render(onlyTable: table.TableName);
         ModelChanged?.Invoke(this, EventArgs.Empty);
      }

      /// <summary>
      /// Re-measure a table after a structural edit (a column rename or type
      /// change alters its width) and update the layout rect so the drawing
      /// and the router agree on its size.
      /// </summary>
      private void ReMeasureTable(TableInfo table)
      {
         if (table == null || !_layout.TryGetValue(table.TableName, out var old))
         {
            return;
         }
         var probe = new Table(_context, 0, 0, BannerHeight, table);
         _layout[table.TableName] = new Rect2(
            old.X, old.Y, probe.ComputedWidth, probe.ComputedHeight);
      }

   }
}
