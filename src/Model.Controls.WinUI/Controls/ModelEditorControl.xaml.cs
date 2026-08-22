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

using Windows.UI;

using Model.Data;
using ModelConsole.Graph;
using ModelConsole.Palette;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModelConsole.Controls
{
   public sealed partial class ModelEditorControl : UserControl
   {
      public ModelEditorControl()
      {
         this.InitializeComponent();

         // Backlog 042: the editor no longer hosts the entity inspector — the
         // host opens it in a modeless details window. The editor relays
         // selection and double-click so the host (MainWindow) can drive the
         // window, and offers WireInspector for the window's inspector to
         // mutate the model through the panel exactly as before.
         ModelPanel.EntitySelected += (s, entity) => EntitySelected?.Invoke(this, entity);
         ModelPanel.EntityDoubleTapped += (s, entity) => EntityDoubleClicked?.Invoke(this, entity);

         // Explorer → canvas selection (backlog 004). The details-window host
         // shows the table in its own inspector.
         ExplorerPanel.TableSelected += (s, table) =>
            ModelPanel.SelectTable(table.TableName);

         // Backlog 038: the explorer's Groups section and (via WireInspector)
         // the inspector's Show/Hide pins never mutate the model — they ask
         // this control to apply the change to the shared EntityVisibility and
         // re-render every consumer (panel + explorer + inspector, then host).
         ExplorerPanel.GroupVisibilityChanged += (s, args) =>
            ApplyVisibility(() =>
               ModelPanel.CurrentVisibility.SetGroupVisible(args.Group, args.Visible));
         ExplorerPanel.FocusRequested += (s, args) =>
            ApplyVisibility(() =>
               ModelPanel.CurrentVisibility.SetFocus(new[] { args.Group }));
         ExplorerPanel.ShowAllRequested += (s, args) =>
            ApplyVisibility(() => ModelPanel.CurrentVisibility.ShowAll());

         // Backlog 039: the explorer's collapse toggles and a box click on the
         // drawing both mutate the shared GroupCollapseState and re-render
         // every consumer (panel + explorer, then the host).
         ExplorerPanel.GroupCollapseRequested += (s, args) =>
            ApplyCollapse(() =>
               ModelPanel.CurrentCollapse.SetCollapsed(args.Group, args.Collapsed));
         ModelPanel.GroupExpandRequested += (s, group) =>
            ApplyCollapse(() => ModelPanel.CurrentCollapse.SetCollapsed(group, false));

         // Backlog 043: the explorer's "Group by:" selector applies the theme
         // to both renderers; Collapse all / Expand all drive the shared
         // collapse state (the package overview).
         ExplorerPanel.ThemeRequested += (s, name) => ApplyTheme(name);
         ExplorerPanel.LayoutRequested += (s, name) => ApplyLayout(name);
         ExplorerPanel.CollapseAllRequested += (s, collapsed) =>
            ApplyCollapse(() =>
            {
               var c = ModelPanel.CurrentCollapse;
               if (collapsed)
               {
                  foreach (var g in ModelPanel.CurrentTheme.Groups(ModelPanel.Tables))
                  {
                     c.SetCollapsed(g, true);
                  }
               }
               else
               {
                  c.ExpandAll();
               }
            });

         // Model replaced (File → Open) → refresh the explorer tree and the
         // visibility + collapse consumers (the new model starts
         // show-everything / all-expanded). An attached details inspector is
         // re-synced to the fresh visibility.
         ModelPanel.ModelChanged += (s, e) =>
         {
            ExplorerPanel.SetModel(ModelPanel.Tables);
            ExplorerPanel.SetVisibility(ModelPanel.CurrentVisibility);
            ExplorerPanel.SetCollapse(ModelPanel.CurrentCollapse);
            ExplorerPanel.SetLayout(ModelPanel.CurrentLayoutName);
            _inspector?.SetVisibility(ModelPanel.CurrentVisibility);
         };

         // Populate the explorer with the model loaded at startup (the
         // default sample). ModelPanelControl's ctor loads the default model
         // but does not raise ModelChanged (the store only fires on SetModel /
         // File → Open), so without this the left panel would stay empty
         // until the user opens a file.
         ExplorerPanel.SetModel(ModelPanel.Tables);
         ExplorerPanel.SetVisibility(ModelPanel.CurrentVisibility);
         ExplorerPanel.SetCollapse(ModelPanel.CurrentCollapse);
         ExplorerPanel.SetLayout(ModelPanel.CurrentLayoutName);
      }

      /// <summary>
      /// Raised when a graphic entity (table or connector) is clicked. A host
      /// with the details window open follows the selection through it
      /// (backlog 042).
      /// </summary>
      public event EventHandler<object> EntitySelected;

      /// <summary>
      /// Raised when a table or connector is double-clicked (backlog 042) —
      /// the host opens its modeless details window on this. Group boxes are
      /// excluded (a box double-click still expands the group).
      /// </summary>
      public event EventHandler<object> EntityDoubleClicked;

      /// <summary>The model's tables (the panel's live list).</summary>
      public IReadOnlyList<TableInfo> Tables
      {
         get { return ModelPanel.Tables; }
      }

      /// <summary>The model's enumerations, when the source declared them.</summary>
      public IReadOnlyDictionary<string, Enumeration> Enumerations
      {
         get { return ModelPanel.Enumerations; }
      }

      /// <summary>The model's provenance (the last SetModel's), for the details window's model readout.</summary>
      public Provenance CurrentProvenance
      {
         get { return _provenance; }
      }

      /// <summary>The model's metadata bag (the last SetModel's), for the details window's model readout.</summary>
      public IReadOnlyDictionary<string, string> CurrentMetadata
      {
         get { return _metadata; }
      }

      /// <summary>The last model-level provenance + metadata handed to SetModel.</summary>
      private Provenance _provenance;
      private IReadOnlyDictionary<string, string> _metadata;

      /// <summary>
      /// The externally hosted inspector (backlog 042), when a host wired one
      /// via <see cref="WireInspector"/>. Null when the editor is inspector-less.
      /// </summary>
      private EntityInspectorControl _inspector;

      /// <summary>
      /// Wire an entity inspector — e.g. the one inside the app's model details
      /// window — to this editor's model panel (backlog 042). The inspector's
      /// edit/delete/pin events mutate the shared model and re-render the
      /// drawing + explorer exactly as the in-editor inspector did before 042;
      /// the same <see cref="EntityVisibility"/> instance drives its Show/Hide
      /// pins, and its body follows the drawing-surface base color.
      /// </summary>
      public void WireInspector(EntityInspectorControl inspector)
      {
         if (inspector == null || ReferenceEquals(inspector, _inspector))
         {
            return;
         }
         _inspector = inspector;

         inspector.ModelEdited += (s, e) => ModelPanel.Refresh();
         inspector.DeleteRequested += (s, edge) => ModelPanel.DeleteConnector(edge);

         // Backlog 029: the inspector's edit surface wires to real model
         // operations on the panel (mutate → re-render → explorer refresh).
         inspector.EntityRenamed += (s, e) =>
            ModelPanel.RenameTable(e.Table, e.OldName, e.NewName);
         inspector.ColumnRenamed += (s, e) =>
            ModelPanel.RenameColumn(e.Table, e.Column, e.OldName, e.NewName);
         inspector.EntityRemoved += (s, table) => ModelPanel.RemoveTable(table);
         inspector.EntityAdded += (s, table) => ModelPanel.AddTable(table);
         inspector.StructureChanged += (s, table) =>
            ModelPanel.StructureChanged(table);

         // Backlog 038: the inspector's Show/Hide pins go through the shared
         // visibility (see the ApplyVisibility wiring above).
         inspector.VisibilityPinChanged += (s, args) =>
            ApplyVisibility(() =>
            {
               var v = ModelPanel.CurrentVisibility;
               if (args.Pinned == true)
               {
                  v.PinShow(args.TableName);
               }
               else if (args.Pinned == false)
               {
                  v.PinHide(args.TableName);
               }
               else
               {
                  v.ClearPin(args.TableName);
               }
            });

         // Start the external inspector synced to the drawing's state.
         inspector.SetVisibility(ModelPanel.CurrentVisibility);
         inspector.BackgroundColor = ModelPanel.BackgroundColor;
      }

      /// <summary>
      /// Replace the model in the drawing and the explorer (File → Open).
      /// The optional enumerations (backlog 021) flow to the panel so the
      /// inspector can show value-sets; the optional provenance + model
      /// metadata (backlog 022) seed the inspector's model-level readout.
      /// </summary>
      public void SetModel(
         IReadOnlyList<TableInfo> tables,
         IReadOnlyDictionary<string, Enumeration> enumerations = null,
         Provenance provenance = null,
         IReadOnlyDictionary<string, string> metadata = null)
      {
         _provenance = provenance;
         _metadata = metadata;
         ModelPanel.SetModel(tables, enumerations);
         ExplorerPanel.SetModel(tables);
         // The panel created a show-everything visibility for the new model;
         // the explorer and the (hosted) inspector consume the same instance.
         ExplorerPanel.SetVisibility(ModelPanel.CurrentVisibility);
         ExplorerPanel.SetCollapse(ModelPanel.CurrentCollapse);
         ExplorerPanel.SetLayout(ModelPanel.CurrentLayoutName);
         _inspector?.SetVisibility(ModelPanel.CurrentVisibility);
         _inspector?.ShowModel(provenance, metadata);
      }

      /// <summary>
      /// The shared view-side visibility (backlog 038) — the same instance the
      /// drawing uses. The host (MainWindow) forwards it to the Skia renderer
      /// so both paths draw the identical visible set.
      /// </summary>
      public EntityVisibility CurrentVisibility
      {
         get { return ModelPanel.CurrentVisibility; }
      }

      /// <summary>
      /// Raised after a visibility change (group toggle, focus, pin, Show all):
      /// the host forwards the shared instance to the Skia renderer so it
      /// re-composes over the same visible set.
      /// </summary>
      public event EventHandler<EntityVisibility> VisibilityChanged;

      /// <summary>
      /// Apply a visibility change: re-layout + re-render the XAML drawing,
      /// re-sync the explorer (checkbox states + grayed tree) and a hosted
      /// inspector's pin toggles, and raise <see cref="VisibilityChanged"/> so
      /// the host can re-compose the Skia renderer. Public so a host wiring an
      /// external inspector can apply visibility to it too (backlog 042).
      /// </summary>
      public void ApplyVisibility(Action mutate)
      {
         var visibility = ModelPanel.CurrentVisibility;
         mutate();
         ModelPanel.SetVisibility(visibility);
         ExplorerPanel.SetVisibility(visibility);
         _inspector?.SetVisibility(visibility);
         VisibilityChanged?.Invoke(this, visibility);
      }

      /// <summary>
      /// The shared view-side collapse state (backlog 039) — the same instance
      /// the drawing uses. The host (MainWindow) forwards it to the Skia
      /// renderer so both paths draw the identical collapsed set.
      /// </summary>
      public GroupCollapseState CurrentCollapse
      {
         get { return ModelPanel.CurrentCollapse; }
      }

      /// <summary>
      /// The active grouping theme's name (backlog 043) — the explorer's
      /// "Group by:" value. The host (MainWindow) forwards it to the Skia
      /// renderer so both paths group identically.
      /// </summary>
      public string CurrentThemeName
      {
         get { return ModelPanel.CurrentThemeName; }
      }

      /// <summary>
      /// The active presentation notation (backlog 040): ERD by default, UML
      /// when the host toggles the notation view.
      /// </summary>
      public DiagramNotation CurrentNotation
      {
         get { return ModelPanel.CurrentNotation; }
      }

      /// <summary>
      /// The active entity layout's name (backlog 045): Grid by default, or an
      /// alternate deterministic projection selected in the explorer header.
      /// </summary>
      public string CurrentLayoutName
      {
         get { return ModelPanel.CurrentLayoutName; }
      }

      /// <summary>
      /// Raised after the notation changes so the host can re-compose the
      /// Skia renderer in the identical mode.
      /// </summary>
      public event EventHandler<DiagramNotation> NotationChanged;

      /// <summary>
      /// Apply a presentation notation to the XAML renderer and notify the
      /// host. This never mutates the model; it only changes rendering.
      /// </summary>
      public void ApplyNotation(DiagramNotation notation)
      {
         ModelPanel.SetNotation(notation);
         NotationChanged?.Invoke(this, notation);
      }

      /// <summary>
      /// Raised after the layout changes so the host can re-compose the Skia
      /// renderer in the identical layout.
      /// </summary>
      public event EventHandler<string> LayoutChanged;

      /// <summary>
      /// Apply an entity layout to the XAML renderer and notify the host. This
      /// is a view-side re-layout only; it never mutates the model.
      /// </summary>
      public void ApplyLayout(string layoutName)
      {
         ModelPanel.SetLayout(layoutName);
         ExplorerPanel.SetLayout(ModelPanel.CurrentLayoutName);
         LayoutChanged?.Invoke(this, ModelPanel.CurrentLayoutName);
      }

      /// <summary>
      /// Apply the current-session selected/highlighted connector style to
      /// the XAML drawing panel (backlog 051).
      /// </summary>
      public void SetSelectedConnectorStyle(ConnectorStyle style)
      {
         ModelPanel.SetSelectedConnectorStyle(style);
      }

      /// <summary>
      /// Raised after a theme change (backlog 043): the host forwards the theme
      /// name to the Skia renderer so it re-composes over the same groups.
      /// </summary>
      public event EventHandler<string> ThemeChanged;

      /// <summary>
      /// Apply a grouping theme (backlog 043): the drawing re-creates its
      /// visibility + collapse over the new theme's groups (every new group
      /// starts visible / expanded), the explorer rebuilds its Groups section,
      /// and the host re-composes the Skia renderer. The theme change
      /// re-creates both shared instances, so <see cref="VisibilityChanged"/>
      /// and <see cref="CollapseChanged"/> fire too — the Skia renderer must
      /// get all three.
      /// </summary>
      public void ApplyTheme(string themeName)
      {
         ModelPanel.SetTheme(themeName);
         ExplorerPanel.SetTheme(themeName);
         ExplorerPanel.SetVisibility(ModelPanel.CurrentVisibility);
         ExplorerPanel.SetCollapse(ModelPanel.CurrentCollapse);
         _inspector?.SetVisibility(ModelPanel.CurrentVisibility);
         ThemeChanged?.Invoke(this, themeName);
         VisibilityChanged?.Invoke(this, ModelPanel.CurrentVisibility);
         CollapseChanged?.Invoke(this, ModelPanel.CurrentCollapse);
      }

      /// <summary>
      /// Raised after a collapse change (explorer toggle or a box click): the
      /// host forwards the shared instance to the Skia renderer so it
      /// re-composes over the same collapsed set.
      /// </summary>
      public event EventHandler<GroupCollapseState> CollapseChanged;

      /// <summary>
      /// Apply a collapse change: re-layout + re-render the XAML drawing,
      /// re-sync the explorer's collapse toggles, and raise
      /// <see cref="CollapseChanged"/> so the host can re-compose the Skia
      /// renderer. Public so a host can apply collapse from outside the editor
      /// (backlog 042).
      /// </summary>
      public void ApplyCollapse(Action mutate)
      {
         var collapse = ModelPanel.CurrentCollapse;
         mutate();
         ModelPanel.SetCollapse(collapse);
         ExplorerPanel.SetCollapse(collapse);
         CollapseChanged?.Invoke(this, collapse);
      }

      /// <summary>
      /// The drawing-surface (canvas) background color (backlog 041) —
      /// forwarded to the drawing panel and to the explorer, diagnostics, and
      /// inspector work panels, so the app's renderer bar "Base:" selection
      /// tints the whole XAML workspace through this control.
      /// </summary>
      public Color BackgroundColor
      {
         get { return ModelPanel.BackgroundColor; }
         set
         {
            ModelPanel.BackgroundColor = value;
            ExplorerPanel.BackgroundColor = value;
            DiagnosticsPanel.BackgroundColor = value;
            _inspector?.BackgroundColor = value;
         }
      }

      /// <summary>Whether the left panel (model explorer) is currently shown.</summary>
      private bool m_leftPanelOpen = true;

      /// <summary>
      /// Collapse/expand toggle for the left explorer panel (backlog 004),
      /// mirroring the right panel's toggle (backlog 014).
      /// </summary>
      private void ToggleLeftPanel_Click(object sender, RoutedEventArgs e)
      {
         m_leftPanelOpen = !m_leftPanelOpen;

         LeftPanel.Visibility = m_leftPanelOpen ? Visibility.Visible : Visibility.Collapsed;
         // ChevronLeft (E76C) while open (points at the panel), ChevronRight
         // (E76B) while collapsed (points back at the reopen button).
         LeftToggleGlyph.Glyph = m_leftPanelOpen ? "\uE76C" : "\uE76B";
         ToolTipService.SetToolTip(LeftPanelToggle,
            m_leftPanelOpen ? "Collapse model explorer" : "Expand model explorer");
      }

      /// <summary>Whether the right panel (log + inspector) is currently shown.</summary>
      private bool m_rightPanelOpen = true;

      /// <summary>
      /// Collapse/expand toggle for the right panel (backlog 014). Collapsing
      /// hides the panel so the star-sized drawing column reflows into the
      /// freed space; the chevron flips to point back at the reopen button.
      /// No ChangeView/FitToWindow is triggered, so zoom and pan are preserved.
      /// </summary>
      private void ToggleRightPanel_Click(object sender, RoutedEventArgs e)
      {
         m_rightPanelOpen = !m_rightPanelOpen;

         RightPanel.Visibility = m_rightPanelOpen ? Visibility.Visible : Visibility.Collapsed;
         // ChevronRight (E76B) while open, ChevronLeft (E76C) while collapsed.
         ToggleGlyph.Glyph = m_rightPanelOpen ? "\uE76B" : "\uE76C";
         ToolTipService.SetToolTip(RightPanelToggle,
            m_rightPanelOpen ? "Collapse right panel" : "Expand right panel");
      }
   }
}
