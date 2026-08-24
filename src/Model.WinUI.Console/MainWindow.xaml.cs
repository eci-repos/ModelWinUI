using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI;

using CommunityToolkit.Mvvm.DependencyInjection;

using Model.Data;
using Model.Interpretation;
using Model.Validation;
using ModelConsole.Diagnostics;
using ModelConsole.ModelData;
using ModelConsole.Controls.Helpers;
using ModelConsole.Graph;
using ModelConsole.Palette;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModelWinUI
{
   /// <summary>
   /// An empty window that can be used on its own or navigated to within a Frame.
   /// </summary>
   public sealed partial class MainWindow : Window
   {
      /// <summary>
      /// Index of the currently applied "Base:" preset in the renderer-bar
      /// drop-down (backlog 041); "Custom…" reverts to it while the color
      /// picker is open.
      /// </summary>
      private int _backgroundPresetIndex = 0;

      /// <summary>
      /// The applied drawing-surface background color (the last preset or the
      /// custom picker result); the picker opens seeded with it.
      /// </summary>
      private Color _currentBackgroundColor =
         HexColor.FromHex(TablePalette.CanvasBackgroundHex);

      /// <summary>
      /// Current-session selected/highlighted connector style (backlog 051).
      /// The host relays this to both renderers; it is not persisted.
      /// </summary>
      private ConnectorStyle _selectedConnectorStyle = ConnectorStyle.Default;

      /// <summary>Guards programmatic slider sync from re-entering.</summary>
      private bool _syncingConnectorWidth;

      /// <summary>
      /// The one modeless entity-details window (backlog 042), created lazily
      /// on the first double-click and reused across double-clicks and File →
      /// Model info. Its inspector is wired to <see cref="XamlEditor"/> on
      /// creation.
      /// </summary>
      private EntityDetailsWindow _detailsWindow;

      public MainWindow()
      {
         this.InitializeComponent();
         Title = "EDAM Studio";

         // Backlog 038: the XAML explorer drives the shared EntityVisibility;
         // every change there must re-compose the Skia renderer over the same
         // instance so both paths agree on the visible set (parity, backlog 003).
         XamlEditor.VisibilityChanged += (s, visibility) =>
            SkiaEditor.SetVisibility(visibility);

         // Backlog 039: the same relay for the shared GroupCollapseState —
         // explorer toggles and box clicks on the XAML drawing must re-compose
         // the Skia renderer over the identical collapsed set.
         XamlEditor.CollapseChanged += (s, collapse) =>
            SkiaEditor.SetCollapse(collapse);

         // Backlog 043: the same relay for the grouping theme — the explorer's
         // "Group by:" selector must re-compose the Skia renderer over the
         // identical groups. (The theme change also re-created the shared
         // visibility + collapse instances, which the two relays above already
         // forward.)
         XamlEditor.ThemeChanged += (s, name) =>
            SkiaEditor.SetTheme(name);

         // Backlog 040: notation is also a view-side choice. The XAML editor
         // applies it first, then the Skia renderer re-composes in parity.
         XamlEditor.NotationChanged += (s, notation) =>
            SkiaEditor.SetNotation(notation);

         // Backlog 045: layout is another view-side choice. The XAML editor
         // applies it first, then Skia re-composes from the same layout name.
         XamlEditor.LayoutChanged += (s, name) =>
            SkiaEditor.SetLayout(name);

         // Backlog 042: double-clicking a table (or FK connector) opens the
         // modeless entity-details window; an already-open window follows
         // subsequent single-click selection (the first click of a
         // double-click updates it too, harmlessly).
         XamlEditor.EntityDoubleClicked += (s, entity) => OpenDetails(entity);
         XamlEditor.EntitySelected += (s, entity) =>
         {
            if (_detailsWindow != null)
            {
               _detailsWindow.ShowEntity(entity);
            }
         };

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

         ApplySelectedConnectorStyle(_selectedConnectorStyle);
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
      /// Switch between ERD and UML notation (backlog 040). This is view-side
      /// only: the canonical model is not mutated or serialized differently.
      /// </summary>
      private void NotationToggle_Click(object sender, RoutedEventArgs e)
      {
         bool crowFoot = ReferenceEquals(sender, CrowFootNotationToggle);
         bool uml = ReferenceEquals(sender, UmlNotationToggle);

         ErdNotationToggle.IsChecked = !crowFoot && !uml;
         CrowFootNotationToggle.IsChecked = crowFoot;
         UmlNotationToggle.IsChecked = uml;
         XamlEditor.ApplyNotation(uml
            ? DiagramNotation.Uml
            : crowFoot ? DiagramNotation.ErdCrowFoot : DiagramNotation.Erd);
      }

      /// <summary>
      /// Renderer-bar "Base:" drop-down (backlog 041): apply the selected
      /// preset to both renderers. "Custom…" opens the WinUI color picker
      /// (seeded with the current color) and applies its result; the combo
      /// reverts to the last preset while the picker is open so its selection
      /// stays meaningful.
      /// </summary>
      private void BackgroundCombo_SelectionChanged(
         object sender, SelectionChangedEventArgs e)
      {
         var item = BackgroundComboBox.SelectedItem as ComboBoxItem;
         if (item == null)
         {
            return;
         }

         string tag = item.Tag as string;
         if (tag == "custom")
         {
            BackgroundComboBox.SelectedIndex = _backgroundPresetIndex;
            var picker = new ColorPicker
            {
               Color = _currentBackgroundColor,
               IsAlphaEnabled = false
            };
            var flyout = new Flyout
            {
               Content = picker,
               XamlRoot = RootGrid.XamlRoot
            };
            picker.ColorChanged += (s, args) => ApplyBackgroundColor(args.NewColor);
            flyout.ShowAt(BackgroundComboBox);
            return;
         }

         if (tag != null)
         {
            _backgroundPresetIndex = BackgroundComboBox.SelectedIndex;
            ApplyBackgroundColor(HexColor.FromHex(tag));
         }
      }

      /// <summary>
      /// Apply the drawing-surface base color to both renderers — the XAML
      /// editor and the Skia render honor the same color (backlog 041). The
      /// assignments are null-guarded because the "Base:" ComboBox's initial
      /// <c>IsSelected</c> fires <see cref="ComboBox.SelectionChanged"/>
      /// during <c>InitializeComponent</c>, before the row-2 editor controls
      /// are constructed — the initial selection (White) matches their default
      /// (<c>TablePalette.CanvasBackgroundHex</c>), so that early fire is a
      /// safe no-op.
      /// </summary>
      private void ApplyBackgroundColor(Color color)
      {
         _currentBackgroundColor = color;
         XamlEditor?.BackgroundColor = color;
         SkiaEditor?.BackgroundColor = color;
      }

      /// <summary>
      /// Open the current-session selected-connector color picker. The color
      /// is applied live to both renderers while the picker changes.
      /// </summary>
      private void ConnectorColorButton_Click(object sender, RoutedEventArgs e)
      {
         var picker = new ColorPicker
         {
            Color = HexColor.FromHex(_selectedConnectorStyle.SelectedHex),
            IsAlphaEnabled = false
         };
         var flyout = new Flyout
         {
            Content = picker,
            XamlRoot = RootGrid.XamlRoot
         };
         picker.ColorChanged += (s, args) =>
            ApplySelectedConnectorStyle(
               _selectedConnectorStyle.WithSelectedHex(ToHex(args.NewColor)));
         flyout.ShowAt(ConnectorColorButton);
      }

      /// <summary>Apply the selected/highlighted connector width.</summary>
      private void ConnectorWidthSlider_ValueChanged(
         object sender, RangeBaseValueChangedEventArgs e)
      {
         if (_syncingConnectorWidth || ConnectorWidthText == null)
         {
            return;
         }
         ApplySelectedConnectorStyle(
            _selectedConnectorStyle.WithSelectedWidth(e.NewValue));
      }

      /// <summary>
      /// Relay selected/highlighted connector style to both renderers and keep
      /// the renderer-bar controls synchronized.
      /// </summary>
      private void ApplySelectedConnectorStyle(ConnectorStyle style)
      {
         _selectedConnectorStyle = style ?? ConnectorStyle.Default;
         XamlEditor?.SetSelectedConnectorStyle(_selectedConnectorStyle);
         SkiaEditor?.SetSelectedConnectorStyle(_selectedConnectorStyle);

         if (ConnectorColorSwatch != null)
         {
            ConnectorColorSwatch.Background =
               new SolidColorBrush(HexColor.FromHex(_selectedConnectorStyle.SelectedHex));
         }
         if (ConnectorWidthText != null)
         {
            ConnectorWidthText.Text =
               _selectedConnectorStyle.SelectedWidth.ToString(
                  "0.#", CultureInfo.InvariantCulture);
         }
         if (ConnectorWidthSlider != null &&
             Math.Abs(ConnectorWidthSlider.Value -
                _selectedConnectorStyle.SelectedWidth) > 0.001)
         {
            _syncingConnectorWidth = true;
            ConnectorWidthSlider.Value = _selectedConnectorStyle.SelectedWidth;
            _syncingConnectorWidth = false;
         }
      }

      private static string ToHex(Color color)
      {
         return "#" + color.R.ToString("X2", CultureInfo.InvariantCulture) +
            color.G.ToString("X2", CultureInfo.InvariantCulture) +
            color.B.ToString("X2", CultureInfo.InvariantCulture);
      }

      /// <summary>
      /// File → Model info (backlog 042): open the modeless details window in
      /// model mode — the provenance + model-metadata readout.
      /// </summary>
      private void ModelInfo_Click(object sender, RoutedEventArgs e)
      {
         EnsureDetailsWindow();
         _detailsWindow.ShowModelInfo();
         _detailsWindow.Activate();
      }

      /// <summary>
      /// File → Export PlantUML… (backlog 040): write the current model as a
      /// deterministic PlantUML package diagram plus class diagram.
      /// </summary>
      private async void ExportPlantUml_Click(object sender, RoutedEventArgs e)
      {
         var picker = new FileSavePicker();
         picker.FileTypeChoices.Add("PlantUML", new List<string> { ".puml" });
         picker.SuggestedFileName = "EDAM-Studio-model";

         var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
         WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

         var file = await picker.PickSaveFileAsync();
         if (file == null)
         {
            return;
         }

         try
         {
            var theme = GroupingThemes.FromName(
               XamlEditor.CurrentThemeName, XamlEditor.Tables);
            string package = UmlPlantEmitter.EmitPackageDiagram(
               XamlEditor.Tables, theme, XamlEditor.CurrentVisibility,
               XamlEditor.CurrentCollapse);
            string classes = UmlPlantEmitter.EmitClassDiagram(XamlEditor.Tables);
            File.WriteAllText(file.Path, package + Environment.NewLine + classes);
         }
         catch (Exception ex)
         {
            var dialog = new ContentDialog
            {
               Title = "Could not export PlantUML",
               Content = ex.Message,
               CloseButtonText = "OK",
               XamlRoot = RootGrid.XamlRoot
            };
            await dialog.ShowAsync();
         }
      }

      /// <summary>
      /// File → Save As Model JSON… (backlog 054): write the live editable
      /// model as JSON in the same container shape File → Open Model… reads,
      /// so a saved model can be reopened and edited. A canceled picker is a
      /// silent no-op; a write failure surfaces in a dialog.
      /// </summary>
      private async void SaveModelJson_Click(object sender, RoutedEventArgs e)
      {
         var picker = new FileSavePicker();
         picker.FileTypeChoices.Add("Model JSON", new List<string> { ".json" });
         picker.SuggestedFileName = "EDAM-Studio-model";

         var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
         WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

         var file = await picker.PickSaveFileAsync();
         if (file == null)
         {
            return; // canceled — no error, no state change
         }

         try
         {
            string json = ModelFile.ToJson(XamlEditor.Tables);
            File.WriteAllText(file.Path, json);
            var log = Ioc.Default.GetRequiredService<ILogService>();
            log.WriteMessage("Saved model JSON: " + file.Path + " (" +
               XamlEditor.Tables.Count + " tables).");
         }
         catch (Exception ex)
         {
            var dialog = new ContentDialog
            {
               Title = "Could not save model",
               Content = ex.Message,
               CloseButtonText = "OK",
               XamlRoot = RootGrid.XamlRoot
            };
            await dialog.ShowAsync();
         }
      }

      /// <summary>
      /// Open the modeless details window on a double-clicked entity (backlog
      /// 042). The window is created once and reused; its inspector is wired
      /// to the XAML editor so edits re-render both renderers.
      /// </summary>
      private void OpenDetails(object entity)
      {
         EnsureDetailsWindow();
         _detailsWindow.ShowEntity(entity);
         _detailsWindow.Activate();
      }

      private void EnsureDetailsWindow()
      {
         if (_detailsWindow == null)
         {
            _detailsWindow = new EntityDetailsWindow();
            // A closed WinUI 3 window cannot be re-activated; forget it so the
            // next double-click builds a fresh one (and re-wires the new
            // inspector).
            _detailsWindow.Closed += (s, e) => _detailsWindow = null;
            _detailsWindow.Attach(XamlEditor);
         }
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
            string json = File.ReadAllText(file.Path);
            LoadModel(ModelFile.LoadJson(json), schemaIssues: ValidateModelJson(json));
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
            string json = File.ReadAllText(path);
            IReadOnlyList<string> schemaIssues = ValidateModelJson(json);
            if (sample.Profile != null)
            {
               var interpretation = SchemaInterpreter.Interpret(
                  json, BuiltInProfiles.FromName(sample.Profile));
               LoadModel(interpretation.Tables, interpretation.Enumerations,
                  interpretation.Provenance, interpretation.Metadata, interpretation.Issues,
                  schemaIssues);
            }
            else
            {
               LoadModel(ModelFile.LoadJson(json), schemaIssues: schemaIssues);
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
         IReadOnlyList<string> issues = null,
         IReadOnlyList<string> schemaIssues = null)
      {
         XamlEditor.SetModel(tables, enumerations, provenance, metadata);
         SkiaEditor.SetModel(tables);
         // Backlog 038: a fresh model starts show-everything; share the XAML
         // panel's visibility instance with the Skia renderer so both draw the
         // identical visible set from the first paint.
         SkiaEditor.SetVisibility(XamlEditor.CurrentVisibility);
         // Backlog 039: likewise share the collapse state (a fresh model starts
         // all-expanded), so both renderers collapse the same groups.
         SkiaEditor.SetCollapse(XamlEditor.CurrentCollapse);
         SkiaEditor.SetLayout(XamlEditor.CurrentLayoutName);
         LogModelLoad(provenance, tables, issues, schemaIssues);
      }

      /// <summary>
      /// Record a load-time line: provenance + resolution issues (backlog 022)
      /// + schema violations (backlog 025). The log panel is the stable home
      /// for model-level provenance and the load-time warning channel.
      /// </summary>
      private void LogModelLoad(
         Provenance provenance, IReadOnlyList<TableInfo> tables, IReadOnlyList<string> issues,
         IReadOnlyList<string> schemaIssues)
      {
         var log = Ioc.Default.GetRequiredService<ILogService>();
         string source = provenance != null && !string.IsNullOrEmpty(provenance.Source)
            ? provenance.Source : "array JSON";
         string version = provenance != null && !string.IsNullOrEmpty(provenance.Version)
            ? " (version " + provenance.Version + ")" : "";
         int issueCount = issues?.Count ?? 0;
         int schemaCount = schemaIssues?.Count ?? 0;
         string suffix = issueCount > 0 ? "; " + issueCount + " resolution issue(s)." : ".";
         if (schemaCount > 0)
         {
            suffix = suffix.TrimEnd('.') + "; " + schemaCount + " schema violation(s).";
         }
         log.WriteMessage("Loaded " + tables.Count + " tables from " + source + version + suffix);
         if (issues != null)
         {
            foreach (var issue in issues)
            {
               log.WriteMessage("  issue: " + issue);
            }
         }
         if (schemaIssues != null)
         {
            foreach (var violation in schemaIssues)
            {
               log.WriteMessage("  schema: " + violation);
            }
         }
      }

      /// <summary>
      /// Validate a model document against its representation's schema (backlog
      /// 025). The schema is selected by the document's root shape and loaded
      /// from the shipped <c>Schemas/</c> folder; violations are warnings on
      /// the log channel, never a hard block — a schema violation does not stop
      /// interpretation, mirroring the interpreter's R8 grace.
      /// </summary>
      private static IReadOnlyList<string> ValidateModelJson(string json)
      {
         ModelSchemaKind kind = ModelSchemaValidator.DetectKind(json);
         if (kind == ModelSchemaKind.None)
         {
            return new[]
            {
               "document root is neither an array nor a data-source/schema/entity container."
            };
         }

         string schemaFile = kind == ModelSchemaKind.Grouped
            ? "grouped.schema.json" : "array.schema.json";
         string path = Path.Combine(AppContext.BaseDirectory, "Schemas", schemaFile);
         if (!File.Exists(path))
         {
            return new[] { "shipped schema " + schemaFile + " was not found next to the app." };
         }
         return ModelSchemaValidator.Validate(json, File.ReadAllText(path));
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
