using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
using SkiaSharp.Views;
using SkiaSharp;
using Model.Data;
using ModelConsole.Graph;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;
using ModelConsole.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace ModelConsole.Controls
{

   /// <summary>
   /// Skia renderer for the full public-safety ERD. The diagram (table
   /// layout + connector routes) is composed once — the routing pass takes
   /// a few seconds, so it runs off the UI thread and is cached; each paint
   /// just replays the cached drawables onto the fresh surface, fitted to
   /// the window (backlog 015).
   /// </summary>
   public sealed partial class SkiaPanelControl : UserControl
   {
      private const float BannerHeight = 40;

      /// <summary>Margin (px) left around the model when fitting to the window.</summary>
      private const double FitMargin = 5;

      private const double MinZoom = 0.1;
      private const double MaxZoom = 4.0;
      private const double ZoomStep = 1.25;

      /// <summary>
      /// Per-notch wheel zoom factor (1.1 = a gentle 10% step, proportional
      /// to the actual wheel delta so trackpads get even smaller steps).
      /// </summary>
      private const double WheelZoomStep = 1.1;

      /// <summary>
      /// Connector hover hit radius (DIPs): a constant on-screen distance,
      /// scaled to content space by the current zoom + DPI when hit-testing.
      /// </summary>
      private const double HoverHitRadius = 6;

      private readonly IModelDataProvider _dataProvider;
      private readonly ISkiaTableFactory _tableFactory;
      private readonly ISkiaConnectorFactory _connectorFactory;
      private IReadOnlyList<TableInfo> _tables;
      private Dictionary<string, TableInfo> _tablesByName;

      /// <summary>Composed off the UI thread on the first paint, then replayed on every paint.</summary>
      private ErdDiagram _diagram;

      /// <summary>True while a compose is in flight (off the UI thread).</summary>
      private bool _composing;

      /// <summary>True while the model is fitted to the window (recomputed on every paint).</summary>
      private bool _fitMode = true;

      /// <summary>Scale applied to content coordinates; 1.0 = actual size.</summary>
      private double _zoom = 1.0;

      /// <summary>Guards programmatic slider changes from re-entering the handler.</summary>
      private bool _syncingSlider;

      /// <summary>Set after the ctor body; guards init-time ValueChanged events.</summary>
      private bool _initialized;

      /// <summary>Pan offset (content-space, in surface px); 0 = centered.</summary>
      private double _panX;
      private double _panY;

      /// <summary>True while a drag-pan gesture is active.</summary>
      private bool _panning;

      /// <summary>Pan start: the press point (DIPs) and the pan offset at that moment.</summary>
      private Point _panStartPoint;
      private double _panStartX;
      private double _panStartY;

      /// <summary>Hover + panning cursors (mirrors the XAML path's GlContext).</summary>
      private InputCursor _handCursor;
      private InputCursor _moveCursor;

      /// <summary>Last paint's surface size (physical px) and DPI scale (px per DIP).</summary>
      private double _viewW;
      private double _viewH;
      private double _dpiScale = 1.0;

      /// <summary>
      /// Index of the connector route under the pointer (in <c>_diagram.Routes</c>),
      /// or −1 when none; drives the emphasized hover paint. Reset when the
      /// diagram is replaced and when the pointer leaves/pan ends.
      /// </summary>
      private int _hoveredRoute = -1;

      public SkiaPanelControl()
      {
         this.InitializeComponent();

         _dataProvider = Ioc.Default.GetRequiredService<IModelDataProvider>();
         _tableFactory = Ioc.Default.GetRequiredService<ISkiaTableFactory>();
         _connectorFactory = Ioc.Default.GetRequiredService<ISkiaConnectorFactory>();
         _tables = _dataProvider.GetPublicSafetyTables();
         _tablesByName = _tables.ToDictionary(t => t.TableName, t => t);

         _handCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
         _moveCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);

         _initialized = true;
         SyncZoomUI();
      }

      /// <summary>
      /// Replace the model and re-compose on the next paint (used by
      /// File → Open). The cached diagram is cleared so the routing pass
      /// runs once for the new model; an in-flight compose is discarded by
      /// the stale-compose guard.
      /// </summary>
      public void SetModel(IReadOnlyList<TableInfo> tables)
      {
         _tables = tables;
         _tablesByName = tables.ToDictionary(t => t.TableName, t => t);
         _diagram = null;
         _hoveredRoute = -1;
         SkiaCanvas.Invalidate();
      }

      private void SkiaCanvas_PaintSurface(
         object sender, SkiaSharp.Views.Windows.SKPaintSurfaceEventArgs e)
      {
         GlFrame frame = new GlFrame(e.Surface);

         // Compose once on the first paint, off the UI thread (the routing
         // pass takes a few seconds); every later paint replays the cached
         // layout + routes.
         if (_diagram == null)
         {
            if (!_composing)
            {
               StartCompose();
            }
            return; // nothing to draw until the compose finishes
         }

         // Fit/zoom transform: center the content in the viewport at _zoom
         // (plus any pan offset from wheel zooming). Table and Connector
         // both draw through frame.Canvas in content coordinates, so a
         // Translate + Scale applies to everything.
         _viewW = e.Info.Width;
         _viewH = e.Info.Height;
         if (SkiaCanvas.ActualWidth > 0)
         {
            _dpiScale = e.Info.Width / SkiaCanvas.ActualWidth;
         }
         double viewW = _viewW;
         double viewH = _viewH;
         double minX = 0, minY = 0, contentW = 0, contentH = 0;
         if (_diagram.Layout.Count > 0)
         {
            minX = _diagram.Layout.Values.Min(r => r.X);
            minY = _diagram.Layout.Values.Min(r => r.Y);
            contentW = _diagram.Layout.Values.Max(r => r.Right) - minX;
            contentH = _diagram.Layout.Values.Max(r => r.Bottom) - minY;
         }

         if (_fitMode && contentW > 0 && contentH > 0)
         {
            // Fit the whole model with a ~5 px margin, never upscaling
            // beyond actual size (matches the XAML path's FitToWindow).
            _zoom = Math.Min((viewW - 2 * FitMargin) / contentW,
                             (viewH - 2 * FitMargin) / contentH);
            _zoom = Math.Min(_zoom, 1.0);
            _zoom = Math.Max(_zoom, 0.01);
            SyncZoomUI();
         }

         if (contentW > 0 && contentH > 0)
         {
            double offsetX = _panX + (viewW - contentW * _zoom) / 2 - minX * _zoom;
            double offsetY = _panY + (viewH - contentH * _zoom) / 2 - minY * _zoom;
            frame.Canvas.Translate((float)offsetX, (float)offsetY);
            frame.Canvas.Scale((float)_zoom);
         }

         foreach (var kv in _diagram.Layout)
         {
            _tableFactory.Create(frame, (float)kv.Value.X, (float)kv.Value.Y,
               BannerHeight, _tablesByName[kv.Key]);
         }

         // Draw the routes, then the hovered route last, on top, emphasized
         // (thicker stroke + larger endpoint markers) so its start and end
         // are unambiguous under the pointer.
         for (int i = 0; i < _diagram.Routes.Count; i++)
         {
            if (i != _hoveredRoute)
            {
               _connectorFactory.Create(frame, _diagram.Routes[i]);
            }
         }
         if (_hoveredRoute >= 0 && _hoveredRoute < _diagram.Routes.Count)
         {
            new Connector(_diagram.Routes[_hoveredRoute]) { Emphasized = true }.Draw(frame);
         }
      }

      /// <summary>
      /// Start the ERD compose on a background thread. The measure probes
      /// only need the frame's font, so a 1×1 offscreen surface is enough.
      /// </summary>
      private void StartCompose()
      {
         _composing = true;
         ComposingText.Visibility = Visibility.Visible;
         var tables = _tables;
         ILogService log = Ioc.Default.GetRequiredService<ILogService>();
         log.WriteMessage("Skia render: composing " + tables.Count + " tables…");

         Task.Run(() =>
         {
            try
            {
               using (var surface = SKSurface.Create(new SKImageInfo(1, 1)))
               {
                  var measureFrame = new GlFrame(surface);
                  var diagram = ErdComposer.Compose(tables, measureFrame,
                     new ErdOptions { BannerHeight = BannerHeight });

                  DispatcherQueue.TryEnqueue(() =>
                  {
                     // Discard a stale compose (the model changed mid-route)
                     // and re-paint so the new model composes.
                     if (!ReferenceEquals(tables, _tables))
                     {
                        _composing = false;
                        ComposingText.Visibility = Visibility.Collapsed;
                        SkiaCanvas.Invalidate();
                        return;
                     }

                     _diagram = diagram;
                     _hoveredRoute = -1; // route indices change with the new diagram
                     _composing = false;
                     ComposingText.Visibility = Visibility.Collapsed;
                     foreach (var issue in diagram.Issues)
                     {
                        log.WriteMessage("FK issue: " + issue);
                     }
                     log.WriteMessage("Skia render: " + diagram.Layout.Count +
                        " tables and " + diagram.Edges.Count + " FK connectors.");
                     SkiaCanvas.Invalidate();
                  });
               }
            }
            catch (Exception ex)
            {
               DispatcherQueue.TryEnqueue(() =>
               {
                  _composing = false;
                  ComposingText.Visibility = Visibility.Collapsed;
                  log.WriteMessage("Skia render: compose failed: " + ex.Message);
                  SkiaCanvas.Invalidate();
               });
            }
         });
      }

      /// <summary>
      /// The mouse wheel zooms the drawing around the cursor instead of
      /// scrolling. Wheel up zooms out, wheel down zooms in (the user's
      /// requested direction); the pan offset keeps the content point under
      /// the cursor fixed as the zoom changes.
      /// </summary>
      private void SkiaCanvas_PointerWheelChanged(
         object sender, PointerRoutedEventArgs e)
      {
         if (_diagram == null || _diagram.Layout.Count == 0)
         {
            return;
         }

         var point = e.GetCurrentPoint(SkiaCanvas);
         double delta = point.Properties.MouseWheelDelta;
         if (delta == 0 || _viewW <= 0 || _viewH <= 0)
         {
            return;
         }

         // Content bounds (same as the paint handler).
         double minX = _diagram.Layout.Values.Min(r => r.X);
         double minY = _diagram.Layout.Values.Min(r => r.Y);
         double contentW = _diagram.Layout.Values.Max(r => r.Right) - minX;
         double contentH = _diagram.Layout.Values.Max(r => r.Bottom) - minY;

         // Cursor in surface px (the transform works in surface px).
         double cursorX = point.Position.X * _dpiScale;
         double cursorY = point.Position.Y * _dpiScale;

         double offsetX = _panX + (_viewW - contentW * _zoom) / 2 - minX * _zoom;
         double offsetY = _panY + (_viewH - contentH * _zoom) / 2 - minY * _zoom;

         // Content point under the cursor.
         double contentX = (cursorX - offsetX) / _zoom;
         double contentY = (cursorY - offsetY) / _zoom;

         // Smooth, delta-proportional step (backlog 018): one notch
         // (delta=120) = x/1.1 or x1.1; fractional deltas from trackpads
         // scale proportionally. Direction kept: up zooms out, down zooms in.
         double factor = Math.Pow(WheelZoomStep, -delta / 120.0);
         _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, _zoom * factor));
         _fitMode = false;

         // New offset keeps the content point under the cursor.
         double newOffsetX = cursorX - contentX * _zoom;
         double newOffsetY = cursorY - contentY * _zoom;
         _panX = newOffsetX - (_viewW - contentW * _zoom) / 2 + minX * _zoom;
         _panY = newOffsetY - (_viewH - contentH * _zoom) / 2 + minY * _zoom;

         SyncZoomUI();
         SkiaCanvas.Invalidate();
         e.Handled = true;
      }

      // ------------------------------------------------------------------
      // Drag-to-pan (mirrors the XAML path's backlog-011 gesture). The Skia
      // renderer is view-only — no per-object drag — so a left-drag anywhere
      // pans (map-like); middle-drag and space+drag pan too. The pan offset
      // (_panX/_panY) is the single source the paint transform consumes, so
      // the gesture composes with wheel-zoom and fit for free.
      // ------------------------------------------------------------------

      /// <summary>
      /// Start a pan on a left-drag anywhere, a middle-drag, or a space+drag
      /// (mouse/touchpad only). Capture the pointer and show the move cursor.
      /// </summary>
      private void SkiaCanvas_PointerPressed(
         object sender, PointerRoutedEventArgs e)
      {
         if (_diagram == null || _diagram.Layout.Count == 0)
         {
            return;
         }

         var device = e.Pointer.PointerDeviceType;
         bool mouse = device == PointerDeviceType.Mouse ||
                      device == PointerDeviceType.Touchpad;
         if (!mouse)
         {
            return;
         }

         var pt = e.GetCurrentPoint(SkiaCanvas);
         var props = pt.Properties;
         bool leftPan = props.IsLeftButtonPressed;
         bool middlePan = props.IsMiddleButtonPressed;
         bool spacePan = IsSpaceHeld() && props.IsLeftButtonPressed;

         if (leftPan || middlePan || spacePan)
         {
            _panning = true;
            _panStartPoint = pt.Position;
            _panStartX = _panX;
            _panStartY = _panY;
            SkiaCanvas.CapturePointer(e.Pointer);
            SetCursor(_moveCursor);
            e.Handled = true;
         }
      }

      /// <summary>
      /// While panning, move the drawing 1:1 with the pointer at any zoom:
      /// the DIP delta scaled to surface px (the transform works in surface
      /// px) is added to the pan offset captured at the pan start.
      /// </summary>
      private void SkiaCanvas_PointerMoved(
         object sender, PointerRoutedEventArgs e)
      {
         var pt = e.GetCurrentPoint(SkiaCanvas);

         if (_panning)
         {
            double dx = (pt.Position.X - _panStartPoint.X) * _dpiScale;
            double dy = (pt.Position.Y - _panStartPoint.Y) * _dpiScale;
            _panX = _panStartX + dx;
            _panY = _panStartY + dy;
            _fitMode = false;
            SkiaCanvas.Invalidate();
            e.Handled = true;
            return;
         }

         // Hover cursor: hand over empty space, default over a table.
         SetCursor(IsOverTable(pt.Position) ? null : _handCursor);

         // Connector hover highlight: pure distance-to-polyline hit-test with
         // a constant on-screen radius (6 DIPs, scaled to content space via
         // the current zoom + DPI). Invalidate only when the hovered route
         // changes so plain moves don't repaint.
         if (_diagram != null && _diagram.Routes.Count > 0 && _viewW > 0)
         {
            Point2 content = GetContentPoint(pt.Position);
            int hovered = RouteHitTest.Nearest(_diagram.Routes, content,
               HoverHitRadius * _dpiScale / _zoom);
            if (hovered != _hoveredRoute)
            {
               _hoveredRoute = hovered;
               SkiaCanvas.Invalidate();
            }
         }
      }

      private void SkiaCanvas_PointerReleased(
         object sender, PointerRoutedEventArgs e) => EndPan(e);

      private void SkiaCanvas_PointerCanceled(
         object sender, PointerRoutedEventArgs e) => EndPan(e);

      private void SkiaCanvas_PointerCaptureLost(
         object sender, PointerRoutedEventArgs e) => EndPan(e);

      /// <summary>
      /// Leave the canvas (not panning): restore the default cursor and drop
      /// the hover highlight.
      /// </summary>
      private void SkiaCanvas_PointerExited(
         object sender, PointerRoutedEventArgs e)
      {
         if (!_panning)
         {
            SetCursor(null);
         }
         if (_hoveredRoute != -1)
         {
            _hoveredRoute = -1;
            SkiaCanvas.Invalidate();
         }
      }

      /// <summary>
      /// End a pan gesture: release the pointer and restore the hover cursor.
      /// A capture-lost event may arrive with the pointer already gone, so the
      /// hit-test is guarded and falls back to the default cursor.
      /// </summary>
      private void EndPan(PointerRoutedEventArgs e)
      {
         if (!_panning)
         {
            return;
         }
         _panning = false;
         SkiaCanvas.ReleasePointerCapture(e.Pointer);
         try
         {
            var pt = e.GetCurrentPoint(SkiaCanvas);
            SetCursor(IsOverTable(pt.Position) ? null : _handCursor);
         }
         catch
         {
            SetCursor(null);
         }

         // The pan branch never updates the hover highlight; drop any stale
         // emphasis now that the gesture ended (the release point may be over
         // a different spot than the press).
         if (_hoveredRoute != -1)
         {
            _hoveredRoute = -1;
            SkiaCanvas.Invalidate();
         }
      }

      /// <summary>
      /// Whether the pointer (DIPs) is over a drawn table, using the same
      /// pointer → content mapping as the paint/wheel transform so the cursor
      /// feedback never drifts from what is drawn.
      /// </summary>
      private bool IsOverTable(Point point)
      {
         if (_diagram == null || _diagram.Layout.Count == 0 || _viewW <= 0)
         {
            return false;
         }
         return _diagram.Layout.Values.Any(r => r.Contains(GetContentPoint(point)));
      }

      /// <summary>
      /// The content-space point under a pointer position (DIPs), via the same
      /// mapping the paint/wheel transform uses (pan offset + centering minus
      /// content origin, then divide by zoom), so hover hit-testing never
      /// drifts from what is drawn.
      /// </summary>
      private Point2 GetContentPoint(Point dip)
      {
         Point offset = GetPanOffset();
         double x = (dip.X * _dpiScale - offset.X) / _zoom;
         double y = (dip.Y * _dpiScale - offset.Y) / _zoom;
         return new Point2(x, y);
      }

      /// <summary>
      /// The surface-px translation the paint handler applies before the
      /// scale — pan offset plus centering minus the content origin.
      /// </summary>
      private Point GetPanOffset()
      {
         if (_diagram == null || _diagram.Layout.Count == 0)
         {
            return new Point(0, 0);
         }
         double minX = _diagram.Layout.Values.Min(r => r.X);
         double minY = _diagram.Layout.Values.Min(r => r.Y);
         double contentW = _diagram.Layout.Values.Max(r => r.Right) - minX;
         double contentH = _diagram.Layout.Values.Max(r => r.Bottom) - minY;
         return new Point(
            _panX + (_viewW - contentW * _zoom) / 2 - minX * _zoom,
            _panY + (_viewH - contentH * _zoom) / 2 - minY * _zoom);
      }

      /// <summary>Swap the canvas cursor (the SkiaCanvasView exposes ProtectedCursor).</summary>
      private void SetCursor(InputCursor cursor)
      {
         SkiaCanvas.Cursor = cursor;
      }

      /// <summary>True while the space key is held (the space+drag pan convention).</summary>
      private static bool IsSpaceHeld()
      {
         return (InputKeyboardSource.GetKeyStateForCurrentThread(
            VirtualKey.Space) & CoreVirtualKeyStates.Down) ==
            CoreVirtualKeyStates.Down;
      }

      private void FitButton_Click(object sender, RoutedEventArgs e)
      {
         _panX = 0;
         _panY = 0;
         _fitMode = true;
         SkiaCanvas.Invalidate();
      }

      private void ZoomSlider_ValueChanged(
         object sender, RangeBaseValueChangedEventArgs e)
      {
         if (!_initialized || _syncingSlider)
         {
            return;
         }
         _fitMode = false;
         _zoom = ZoomSlider.Value / 100.0;
         ZoomPercent.Text = (int)Math.Round(_zoom * 100) + "%";
         SkiaCanvas.Invalidate();
      }

      /// <summary>Push the current zoom into the slider + % readout without re-entering.</summary>
      private void SyncZoomUI()
      {
         _syncingSlider = true;
         ZoomSlider.Value = _zoom * 100.0;
         _syncingSlider = false;
         ZoomPercent.Text = (int)Math.Round(_zoom * 100) + "%";
      }

   }

}
