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
   /// layout + connector routes) is composed once and cached — the routing
   /// pass takes a few seconds, so it must never run per paint; each paint
   /// just replays the cached drawables onto the fresh surface.
   /// </summary>
   public sealed partial class SkiaPanelControl : UserControl
   {
      private const float BannerHeight = 40;

      private readonly IModelDataProvider _dataProvider;
      private readonly ISkiaTableFactory _tableFactory;
      private readonly ISkiaConnectorFactory _connectorFactory;
      private readonly IReadOnlyList<TableInfo> _tables;
      private readonly Dictionary<string, TableInfo> _tablesByName;

      /// <summary>Composed on the first paint, then replayed on every paint.</summary>
      private ErdDiagram _diagram;

      public SkiaPanelControl()
      {
         this.InitializeComponent();

         _dataProvider = Ioc.Default.GetRequiredService<IModelDataProvider>();
         _tableFactory = Ioc.Default.GetRequiredService<ISkiaTableFactory>();
         _connectorFactory = Ioc.Default.GetRequiredService<ISkiaConnectorFactory>();
         _tables = _dataProvider.GetPublicSafetyTables();
         _tablesByName = _tables.ToDictionary(t => t.TableName, t => t);
      }

      private void SkiaCanvas_PaintSurface(
         object sender, SkiaSharp.Views.Windows.SKPaintSurfaceEventArgs e)
      {
         GlFrame frame = new GlFrame(e.Surface);

         // Compose once on the first paint (the routing pass takes a few
         // seconds); every later paint replays the cached layout + routes.
         if (_diagram == null)
         {
            _diagram = ErdComposer.Compose(_tables, frame, new ErdOptions
            {
               BannerHeight = BannerHeight
            });

            ILogService log = Ioc.Default.GetRequiredService<ILogService>();
            foreach (var issue in _diagram.Issues)
            {
               log.WriteMessage("FK issue: " + issue);
            }
            log.WriteMessage("Skia render: " + _diagram.Layout.Count +
               " tables and " + _diagram.Edges.Count + " FK connectors.");
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

   }

}
