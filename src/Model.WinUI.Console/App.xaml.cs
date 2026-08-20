using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using ModelConsole.Diagnostics;
using ModelConsole.Graphics.GLibrary;
using ModelConsole.Graphics.Services;
using ModelConsole.Controls.Services;
using ModelConsole.Skia.Services;
using ModelConsole.Controls.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModelWinUI
{
   /// <summary>
   /// Provides application-specific behavior to supplement the default Application class.
   /// </summary>
   public partial class App : Application
   {
      /// <summary>
      /// Initializes the singleton application object.  This is the first line of authored code
      /// executed, and as such is the logical equivalent of main() or WinMain().
      /// </summary>
      public App()
      {
         this.InitializeComponent();
      }

      /// <summary>
      /// Composition root: the app-wide service provider. Both <see cref="Services"/>
      /// and <c>Ioc.Default</c> reference the same provider, which is frozen
      /// after startup.
      /// </summary>
      public static IServiceProvider Services { get; private set; }

      /// <summary>
      /// Invoked when the application is launched.
      /// </summary>
      /// <param name="args">Details about the launch request and process.</param>
      protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
      {
         Services = ConfigureServices();
         // Bridge for XAML-created controls, which cannot use constructor
         // injection. Code-created objects use constructor injection instead.
         Ioc.Default.ConfigureServices(Services);

         m_window = new MainWindow();
         m_window.Activate();
      }

      /// <summary>
      /// Register the application services. New services are added here so
      /// dependencies are wired in a single place.
      /// </summary>
      private static IServiceProvider ConfigureServices()
      {
         ServiceCollection services = new ServiceCollection();

         // Stateless, process-wide services — singleton.
         services.AddSingleton<ILogService, LogService>();
         services.AddSingleton<IModelDataProvider, ModelDataProvider>();
         services.AddSingleton<ITableFactory, TableFactory>();
         services.AddSingleton<ISkiaTableFactory, SkiaTableFactory>();
         services.AddSingleton<ISkiaConnectorFactory, SkiaConnectorFactory>();
         services.AddSingleton<IConnectorFactory, ConnectorFactory>();
         services.AddSingleton<IRectangleFactory, RectangleFactory>();

         // A fresh model per drawing operation; a singleton would accumulate
         // items across draws.
         services.AddTransient<IGlModel, GlModel>();

         // Single log panel: a transient would leak an event subscription per
         // instance, so the view model must stay a singleton.
         services.AddSingleton<DiagnosticsLogViewModel>();

         return services.BuildServiceProvider();
      }

      private Window m_window;
   }
}
