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

using Model.Data;
using ModelConsole.Graph;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModelConsole.Controls
{
   public sealed partial class ModelEditorControl : UserControl
   {
      public ModelEditorControl()
      {
         this.InitializeComponent();

         // Route clicks on the canvas to the inspector, and inspector
         // actions (edit / delete) back to the model panel for a re-render.
         ModelPanel.EntitySelected += (s, entity) =>
         {
            if (entity is TableInfo table)
            {
               InspectorPanel.ShowTable(table, ModelPanel.Enumerations);
            }
            else if (entity is FkRelation edge)
            {
               InspectorPanel.ShowConnector(edge);
            }
         };

         InspectorPanel.ModelEdited += (s, e) => ModelPanel.Refresh();
         InspectorPanel.DeleteRequested += (s, edge) => ModelPanel.DeleteConnector(edge);

         // Explorer → canvas selection + inspector (backlog 004).
         ExplorerPanel.TableSelected += (s, table) =>
         {
            ModelPanel.SelectTable(table.TableName);
            InspectorPanel.ShowTable(table, ModelPanel.Enumerations);
         };

         // Model replaced (File → Open) → refresh the explorer tree.
         ModelPanel.ModelChanged += (s, e) =>
            ExplorerPanel.SetModel(ModelPanel.Tables);

         // Populate the explorer with the model loaded at startup (the
         // default sample). ModelPanelControl's ctor loads the default model
         // but does not raise ModelChanged (that fires only on SetModel /
         // File → Open), so without this the left panel would stay empty
         // until the user opens a file.
         ExplorerPanel.SetModel(ModelPanel.Tables);
      }

      /// <summary>
      /// Replace the model in the drawing and the explorer (File → Open).
      /// The optional enumerations (backlog 021) flow to the panel so the
      /// inspector can show value-sets.
      /// </summary>
      public void SetModel(
         IReadOnlyList<TableInfo> tables,
         IReadOnlyDictionary<string, Enumeration> enumerations = null)
      {
         ModelPanel.SetModel(tables, enumerations);
         ExplorerPanel.SetModel(tables);
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
