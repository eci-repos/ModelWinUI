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
using Windows.Storage.Pickers;

using CommunityToolkit.Mvvm.DependencyInjection;

using Model.Data;
using Model.Interpretation;
using ModelConsole.ModelData;
using ModelConsole.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModelWinUI
{
   /// <summary>
   /// An empty window that can be used on its own or navigated to within a Frame.
   /// </summary>
   public sealed partial class MainWindow : Window
   {
      public MainWindow()
      {
         this.InitializeComponent();
         Title = "EDAM Studio";

         // File → Open Sample (backlog 005): one item per shipped sample,
         // built from the registry so the menu and the shipped files can
         // never drift apart.
         foreach (var sample in SampleModels.All)
         {
            var item = new MenuFlyoutItem
            {
               Text = sample.Name,
               Tag = sample.FileName
            };
            item.Click += OpenSample_Click;
            OpenSampleMenu.Items.Add(item);
         }
      }

      /// <summary>
      /// Switch the main view between the XAML ERD and the Skia ERD (backlog
      /// 003). The two toggle buttons are mutually exclusive; the other
      /// control's <see cref="Visibility"/> is collapsed so it keeps its
      /// state (zoom/pan for the XAML path, the cached diagram for the Skia
      /// path) across switches.
      /// </summary>
      private void RendererToggle_Click(object sender, RoutedEventArgs e)
      {
         bool skia = ReferenceEquals(sender, SkiaToggle);

         XamlToggle.IsChecked = !skia;
         SkiaToggle.IsChecked = skia;
         XamlEditor.Visibility = skia ? Visibility.Collapsed : Visibility.Visible;
         SkiaEditor.Visibility = skia ? Visibility.Visible : Visibility.Collapsed;
      }

      /// <summary>
      /// File → Open Model…: pick a JSON model file and render it in both
      /// renderers (backlog 004).
      /// </summary>
      private async void OpenModel_Click(object sender, RoutedEventArgs e)
      {
         var picker = new FileOpenPicker();
         picker.FileTypeFilter.Add(".json");

         // Unpackaged apps must initialize the picker with the window handle,
         // otherwise it throws (0x80070005 / "class not registered").
         var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
         WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

         var file = await picker.PickSingleFileAsync();
         if (file == null)
         {
            return;
         }

         try
         {
            LoadModel(ModelFile.Load(file.Path));
         }
         catch (Exception ex)
         {
            await ShowLoadErrorAsync(ex);
         }
      }

      /// <summary>
      /// File → Open Sample: load one of the shipped sample models (backlog
      /// 005). The item's <see cref="FrameworkElement.Tag"/> carries the JSON
      /// file name; the file ships in the app output under Samples/. A sample
      /// that declares a mapping profile (backlog 020) is read through the
      /// interpreter instead of <see cref="ModelFile.Load"/> — the renderers
      /// and explorer consume the same canonical model either way.
      /// </summary>
      private async void OpenSample_Click(object sender, RoutedEventArgs e)
      {
         string fileName = (sender as MenuFlyoutItem)?.Tag as string;
         if (String.IsNullOrEmpty(fileName))
         {
            return;
         }

         var sample = SampleModels.All.FirstOrDefault(s => s.FileName == fileName);
         if (sample == null)
         {
            return;
         }

         string path = Path.Combine(
            AppContext.BaseDirectory, "Samples", fileName);
         try
         {
            if (sample.Profile != null)
            {
               var interpretation = SchemaInterpreter.Interpret(
                  File.ReadAllText(path), BuiltInProfiles.FromName(sample.Profile));
               LoadModel(interpretation.Tables, interpretation.Enumerations,
                  interpretation.Provenance, interpretation.Metadata, interpretation.Issues);
            }
            else
            {
               LoadModel(ModelFile.Load(path));
            }
         }
         catch (Exception ex)
         {
            await ShowLoadErrorAsync(ex);
         }
      }

      /// <summary>
      /// Feed a loaded model to both renderers (XAML + Skia). The optional
      /// enumerations (backlog 021) come from the schema-driven interpreter
      /// and feed the XAML inspector's value-set readout; the optional
      /// provenance + model metadata + resolution issues (backlog 022) seed
      /// the inspector's model-level readout and the load-time log line.
      /// </summary>
      private void LoadModel(
         IReadOnlyList<TableInfo> tables,
         IReadOnlyDictionary<string, Enumeration> enumerations = null,
         Provenance provenance = null,
         IReadOnlyDictionary<string, string> metadata = null,
         IReadOnlyList<string> issues = null)
      {
         XamlEditor.SetModel(tables, enumerations, provenance, metadata);
         SkiaEditor.SetModel(tables);
         LogModelLoad(provenance, tables, issues);
      }

      /// <summary>
      /// Record a load-time log line: provenance + resolution issues (backlog
      /// 022). The log panel is the stable home for model-level provenance.
      /// </summary>
      private void LogModelLoad(
         Provenance provenance, IReadOnlyList<TableInfo> tables, IReadOnlyList<string> issues)
      {
         var log = Ioc.Default.GetRequiredService<ILogService>();
         string source = provenance != null && !string.IsNullOrEmpty(provenance.Source)
            ? provenance.Source : "array JSON";
         string version = provenance != null && !string.IsNullOrEmpty(provenance.Version)
            ? " (version " + provenance.Version + ")" : "";
         int issueCount = issues?.Count ?? 0;
         log.WriteMessage("Loaded " + tables.Count + " tables from " + source + version +
            (issueCount > 0 ? "; " + issueCount + " resolution issue(s)." : "."));
         if (issues != null)
         {
            foreach (var issue in issues)
            {
               log.WriteMessage("  issue: " + issue);
            }
         }
      }

      /// <summary>
      /// Surface a model-load failure in a dialog.
      /// </summary>
      private async Task ShowLoadErrorAsync(Exception ex)
      {
         var dialog = new ContentDialog
         {
            Title = "Could not open model",
            Content = ex.Message,
            CloseButtonText = "OK",
            XamlRoot = RootGrid.XamlRoot
         };
         await dialog.ShowAsync();
      }
   }
}
