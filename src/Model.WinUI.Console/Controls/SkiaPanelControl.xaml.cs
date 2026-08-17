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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
using SkiaSharp.Views;
using SkiaSharp;
using Model.Data;
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

      public SkiaPanelControl()
      {
         this.InitializeComponent();

         _dataProvider = Ioc.Default.GetRequiredService<IModelDataProvider>();
         _tableFactory = Ioc.Default.GetRequiredService<ISkiaTableFactory>();
         _connectorFactory = Ioc.Default.GetRequiredService<ISkiaConnectorFactory>();
         _tables = _dataProvider.GetPublicSafetyTables();
         _tablesByName = _tables.ToDictionary(t => t.TableName, t => t);

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

         // Fit/zoom transform: center the content in the viewport at _zoom.
         // Table and Connector both draw through frame.Canvas in content
         // coordinates, so a Translate + Scale applies to everything.
         double viewW = e.Info.Width;
         double viewH = e.Info.Height;
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
            double offsetX = (viewW - contentW * _zoom) / 2 - minX * _zoom;
            double offsetY = (viewH - contentH * _zoom) / 2 - minY * _zoom;
            frame.Canvas.Translate((float)offsetX, (float)offsetY);
            frame.Canvas.Scale((float)_zoom);
         }

         foreach (var kv in _diagram.Layout)
         {
            _tableFactory.Create(frame, (float)kv.Value.X, (float)kv.Value.Y,
               BannerHeight, _tablesByName[kv.Key]);
         }

         foreach (var route in _diagram.Routes)
         {
            _connectorFactory.Create(frame, route);
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

      private void FitButton_Click(object sender, RoutedEventArgs e)
      {
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
