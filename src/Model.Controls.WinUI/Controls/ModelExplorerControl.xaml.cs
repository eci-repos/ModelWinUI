using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

using Model.Data;
using ModelConsole.Controls.Helpers;
using ModelConsole.Graph;
using ModelConsole.Palette;

namespace ModelConsole.Controls
{
   /// <summary>
   /// Model explorer: a tree of the whole model's structure — one node per
   /// table with a child node per column (name, type, PK/FK tags), a
   /// foreign-key list, and a Groups section (backlog 038) with a checkbox
   /// per group, a "Show only:" focus combo, and a "Show all" reset. The
   /// Groups panel lives behind the header's "Groups" button (hidden by
   /// default). Clicking a table node raises <see cref="TableSelected"/> so
   /// the drawing can select it. Hidden tables stay in the tree (grayed) —
   /// visibility never drops them from the model, only from the drawing.
   /// </summary>
   public sealed partial class ModelExplorerControl : UserControl
   {
      /// <summary>
      /// Raised when a table node is clicked in the tree.
      /// </summary>
      public event EventHandler<TableInfo> TableSelected;

      /// <summary>
      /// Raised when a group's checkbox is toggled: the host applies
      /// <c>SetGroupVisible</c> on the shared visibility.
      /// </summary>
      public event EventHandler<GroupVisibilityChangedEventArgs> GroupVisibilityChanged;

      /// <summary>
      /// Raised when the "Show only:" combo picks a group (backlog 043 UX): the
      /// host applies <c>SetFocus</c> so only that group's members draw.
      /// </summary>
      public event EventHandler<GroupFocusRequestedEventArgs> FocusRequested;

      /// <summary>
      /// Raised by the "Show all" button (or the "Show only:" combo returning
      /// to "All groups"): the host resets the shared visibility to
      /// show-everything.
      /// </summary>
      public event EventHandler ShowAllRequested;

      /// <summary>
      /// Raised when a group's collapse toggle is flipped (backlog 039): the
      /// host applies <c>SetCollapsed</c> on the shared collapse state.
      /// </summary>
      public event EventHandler<GroupCollapseRequestedEventArgs> GroupCollapseRequested;

      /// <summary>
      /// Raised when the "Group by:" selector changes (backlog 043): the host
      /// applies the theme to both renderers.
      /// </summary>
      public event EventHandler<string> ThemeRequested;

      /// <summary>
      /// Raised when the "Layout:" selector changes (backlog 045): the host
      /// applies the layout to both renderers.
      /// </summary>
      public event EventHandler<string> LayoutRequested;

      /// <summary>
      /// Raised by the Collapse all / Expand all buttons (backlog 043): true =
      /// collapse every group into a package box, false = expand them all.
      /// </summary>
      public event EventHandler<bool> CollapseAllRequested;

      private readonly Dictionary<string, TreeViewNode> _tableNodes =
         new Dictionary<string, TreeViewNode>();

      /// <summary>Maps each table node to its <see cref="TableInfo"/>.</summary>
      private readonly Dictionary<TreeViewNode, TableInfo> _nodeTables =
         new Dictionary<TreeViewNode, TableInfo>();

      /// <summary>Maps each group (tag) to its explorer checkbox.</summary>
      private readonly Dictionary<string, CheckBox> _groupBoxes =
         new Dictionary<string, CheckBox>();

      /// <summary>Maps each group (tag) to its collapse toggle (backlog 039).</summary>
      private readonly Dictionary<string, ToggleButton> _collapseButtons =
         new Dictionary<string, ToggleButton>();

      /// <summary>
      /// The shared view-side visibility (backlog 038); the host passes the
      /// same instance it gives the grid and the Skia renderer. The
      /// checkbox states and the grayed tree derive from it.
      /// </summary>
      private EntityVisibility _visibility;

      /// <summary>
      /// The shared view-side collapse state (backlog 039); the host passes
      /// the same instance it gives the grid and the Skia renderer. The
      /// per-group collapse toggles derive from it.
      /// </summary>
      private GroupCollapseState _collapse;

      /// <summary>
      /// The current model (stored on <see cref="SetModel"/>); the Groups
      /// section and the theme selector derive from it.
      /// </summary>
      private IReadOnlyList<TableInfo> _tables;

      /// <summary>
      /// The active grouping theme's name (backlog 043) — the "Group by:"
      /// selector's value. The concrete <see cref="GroupingTheme"/> is derived
      /// from <c>_tables + name</c> at each rebuild, so a model change
      /// re-derives it automatically.
      /// </summary>
      private string _themeName = GroupingThemes.TagsName;

      /// <summary>
      /// The active entity layout's name (backlog 045) — the compact selector
      /// in the explorer header.
      /// </summary>
      private string _layoutName = EntityLayout.GridName;

      /// <summary>Guards programmatic checkbox/content updates from re-entering the handlers.</summary>
      private bool _syncing;

      /// <summary>Whether the Groups panel is currently open (backlog 043 UX) —
      /// hidden by default behind the header's "Groups" button.</summary>
      private bool _groupsPanelOpen;

      /// <summary>The active theme's group universe, cached at each rebuild so
      /// the "Show only:" sync does not re-derive a connectivity theme on every
      /// visibility toggle.</summary>
      private List<string> _groupList = new List<string>();

      /// <summary>The "Show only:" combo's show-everything entry.</summary>
      private const string AllGroupsLabel = "All groups";

      /// <summary>Brush for table nodes whose entity is currently hidden.</summary>
      private static readonly SolidColorBrush HiddenTableForeground =
         new SolidColorBrush(Microsoft.UI.Colors.Gray);

      /// <summary>
      /// Brush for the tree's text — the shared palette's near-black. The tree
      /// sits on the light "Base:" background (backlog 041), so the text is
      /// pinned dark: the theme-default foreground goes white in dark OS mode
      /// and vanishes against the light panel (the 041 regression, the same
      /// pin the drawing-surface text uses). The XAML-side pin for lazily
      /// realized containers lives in the TreeView's Resources.
      /// </summary>
      private static readonly SolidColorBrush TreeTextForeground =
         new SolidColorBrush(HexColor.FromHex(TablePalette.TextHex));

      /// <summary>
      /// The explorer's panel color (backlog 041) — the tree area follows the
      /// drawing-surface "Base:" color the renderer bar selects; defaults to
      /// the shared canvas background (white).
      /// </summary>
      private Color _backgroundColor =
         HexColor.FromHex(TablePalette.CanvasBackgroundHex);

      public ModelExplorerControl()
      {
         this.InitializeComponent();

         // The header keeps its pastel chrome; the tree body (root + the
         // TreeView itself) paints the base color so the working area matches
         // the drawing surface and the other work panels.
         RootGrid.Background = new SolidColorBrush(_backgroundColor);
         ModelTree.Background = new SolidColorBrush(_backgroundColor);

         // The tree text is pinned to the shared near-black so no inherited
         // foreground can revert to the theme default (white in dark OS mode)
         // against the light "Base:" panel.
         ModelTree.Foreground = TreeTextForeground;

         // Backlog 043 UX: the grouping panel is hidden until the header's
         // "Groups" button opens it. The chevron hints at the state (▸ closed,
         // ▾ open).
         GroupsToggleButton.Content = "Groups ▸";

         // The "Group by:" selector (backlog 043): the four built-in themes.
         // The initial selection fires SelectionChanged before any model is
         // set — the _tables == null guard in the handler absorbs it.
         ThemeCombo.Items.Add(GroupingThemes.TagsName);
         ThemeCombo.Items.Add(GroupingThemes.SchemaName);
         ThemeCombo.Items.Add(GroupingThemes.KindName);
         ThemeCombo.Items.Add(GroupingThemes.ConnectivityName);
         ThemeCombo.SelectedIndex = 0;

         foreach (var name in EntityLayout.Names)
         {
            LayoutCombo.Items.Add(name);
         }
         LayoutCombo.SelectedIndex = 0;
      }

      /// <summary>
      /// The explorer's background color (backlog 041) — follows the renderer
      /// bar's "Base:" selection, defaulting to white.
      /// </summary>
      public Color BackgroundColor
      {
         get { return _backgroundColor; }
         set
         {
            _backgroundColor = value;
            if (RootGrid != null)
            {
               RootGrid.Background = new SolidColorBrush(value);
               ModelTree.Background = new SolidColorBrush(value);
            }
         }
      }

      /// <summary>
      /// Rebuild the tree + Groups section from a model (a list of tables).
      /// </summary>
      public void SetModel(IReadOnlyList<TableInfo> tables)
      {
         _tables = tables;
         _syncing = true;
         try
         {
            ModelTree.RootNodes.Clear();
            _tableNodes.Clear();
            _nodeTables.Clear();

            if (tables == null || tables.Count == 0)
            {
               BuildGroupsSection();
               return;
            }

            string schema = tables
               .FirstOrDefault(t => !string.IsNullOrEmpty(t.SchemaName))?.SchemaName
               ?? "Model";
            var root = new TreeViewNode { Content = schema, IsExpanded = true };
            ModelTree.RootNodes.Add(root);

            foreach (var table in tables)
            {
               var tableNode = new TreeViewNode
               {
                  Content = FormatTableContent(table)
               };
               _tableNodes[table.TableName] = tableNode;
               _nodeTables[tableNode] = table;

               foreach (var column in table.Columns)
               {
                  tableNode.Children.Add(new TreeViewNode
                  {
                     Content = FormatColumn(column)
                  });
               }
               root.Children.Add(tableNode);
            }

            // Foreign keys section (the FK edges the drawing routes).
            var (edges, _) = FkEdgeExtractor.Extract(tables);
            if (edges.Count > 0)
            {
               var fkRoot = new TreeViewNode
               {
                  Content = "Foreign Keys (" + edges.Count + ")",
                  IsExpanded = true
               };
               foreach (var edge in edges)
               {
                  fkRoot.Children.Add(new TreeViewNode
                  {
                     Content = edge.ChildTable + "." + edge.ChildColumn +
                               "  →  " + edge.ParentTable + "." + edge.ParentColumn
                  });
               }
               root.Children.Add(fkRoot);
            }

            BuildGroupsSection();
         }
         finally
         {
            _syncing = false;
         }
      }

      /// <summary>
      /// Rebuild the Groups section from the active theme (backlog 043): one
      /// row per group — a visibility checkbox (backlog 038) with a muted
      /// count readout (tables + internal FKs), and a right-aligned ▣ collapse
      /// button (backlog 039). The "Show only:" combo carries the explicit
      /// focus entry. The panel's open/closed state belongs to the header's
      /// "Groups" button — this never forces it open or closed.
      /// </summary>
      private void BuildGroupsSection()
      {
         GroupsList.Items.Clear();
         _groupBoxes.Clear();
         _collapseButtons.Clear();

         if (_tables == null || _tables.Count == 0)
         {
            GroupsHeader.Text = "Groups (0)";
            _groupList.Clear();
            PopulateSoloCombo();
            return;
         }

         var theme = GroupingThemes.FromName(_themeName, _tables);
         var groups = theme.Groups(_tables).ToList();
         GroupsHeader.Text = "Groups (" + groups.Count + ")";
         _groupList = groups;

         // Member sets per group (the owning-package rule) + the FK edges, so
         // each row's count readout is computed once.
         var membersByGroup = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
         foreach (var t in _tables)
         {
            var g = theme.PrimaryGroupOf(t);
            if (g == null)
            {
               continue;
            }
            if (!membersByGroup.TryGetValue(g, out var set))
            {
               set = new HashSet<string>(StringComparer.Ordinal);
               membersByGroup[g] = set;
            }
            set.Add(t.TableName);
         }
         var (edges, _) = FkEdgeExtractor.Extract(_tables);

         foreach (var group in groups)
         {
            // Backlog 038: the visibility checkbox carries the group name.
            var box = new CheckBox { Content = group, Tag = group, IsChecked = true };
            box.Checked += GroupBox_Checked;
            box.Unchecked += GroupBox_Unchecked;
            _groupBoxes[group] = box;

            // Backlog 043: the group's shape at a glance — member count and
            // internal FK count (both endpoints in the group).
            var members = membersByGroup.TryGetValue(group, out var memberSet)
               ? memberSet : new HashSet<string>(StringComparer.Ordinal);
            int fkCount = edges.Count(e =>
               memberSet.Contains(e.ChildTable) && memberSet.Contains(e.ParentTable));
            var countText = new TextBlock
            {
               Text = "(" + members.Count + " tables, " + fkCount + " FKs)",
               FontSize = 11,
               VerticalAlignment = VerticalAlignment.Center,
               Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };

            var left = new StackPanel
            {
               Orientation = Orientation.Horizontal,
               Spacing = 4
            };
            left.Children.Add(box);
            left.Children.Add(countText);

            // Backlog 039: the ▣ collapse toggle (right-aligned), independent
            // of the checkbox — one group hidden vs. one group collapsed into
            // a package box.
            var collapseButton = new ToggleButton
            {
               Content = "▣",
               Tag = group,
               MinWidth = 0,
               Padding = new Thickness(8, 2, 8, 2)
            };
            ToolTipService.SetToolTip(collapseButton,
               "Collapse this group into a package box");
            collapseButton.Checked += GroupCollapseButton_Checked;
            collapseButton.Unchecked += GroupCollapseButton_Unchecked;
            _collapseButtons[group] = collapseButton;

            var row = new Grid { ColumnSpacing = 4 };
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
               Width = new GridLength(1, GridUnitType.Star)
            });
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
               Width = GridLength.Auto
            });
            Grid.SetColumn(left, 0);
            Grid.SetColumn(collapseButton, 1);
            row.Children.Add(left);
            row.Children.Add(collapseButton);
            GroupsList.Items.Add(row);
         }

         PopulateSoloCombo(groups);
      }

      /// <summary>
      /// Rebuild the "Show only:" combo (backlog 043 UX): "All groups" plus the
      /// active theme's groups. The selection is synced from the shared
      /// visibility, so the combo always states the view that actually draws.
      /// </summary>
      private void PopulateSoloCombo(IEnumerable<string> groups = null)
      {
         // The item add (and its implicit first-selection) fires
         // SelectionChanged — guard the whole population so a rebuild during
         // SetModel/SetTheme never re-enters the host.
         _syncing = true;
         SoloCombo.Items.Clear();
         SoloCombo.Items.Add(AllGroupsLabel);
         foreach (var g in groups ?? _groupList)
         {
            SoloCombo.Items.Add(g);
         }
         _syncing = false;
         SyncSoloCombo();
      }

      /// <summary>
      /// Match the "Show only:" combo to the shared visibility: "All groups"
      /// when none (or several) groups are singled out, the one visible group
      /// when exactly one draws (focus). Guarded so the selection never
      /// re-enters the handler.
      /// </summary>
      private void SyncSoloCombo()
      {
         if (SoloCombo.Items.Count == 0)
         {
            return;
         }
         string selection = AllGroupsLabel;
         if (_visibility != null)
         {
            int visible = 0;
            string solo = null;
            foreach (var g in _groupList)
            {
               if (_visibility.IsGroupVisible(g))
               {
                  visible++;
                  solo = g;
               }
            }
            if (visible == 1)
            {
               selection = solo;
            }
         }
         _syncing = true;
         for (int i = 0; i < SoloCombo.Items.Count; i++)
         {
            if (string.Equals(SoloCombo.Items[i] as string, selection,
               StringComparison.Ordinal))
            {
               SoloCombo.SelectedIndex = i;
               break;
            }
         }
         _syncing = false;
      }

      /// <summary>
      /// The header's "Groups" button (backlog 043 UX): open/close the grouping
      /// panel on request. The panel stays closed by default.
      /// </summary>
      private void GroupsToggleButton_Click(object sender, RoutedEventArgs e)
      {
         _groupsPanelOpen = !_groupsPanelOpen;
         GroupsPanel.Visibility = _groupsPanelOpen
            ? Visibility.Visible : Visibility.Collapsed;
         GroupsToggleButton.Content = _groupsPanelOpen ? "Groups ▾" : "Groups ▸";
      }

      /// <summary>
      /// Switch the grouping theme and rebuild the Groups section (backlog
      /// 043). The host applies the theme to the drawing via
      /// <see cref="ThemeRequested"/>; this keeps the selector + group list
      /// in sync with the applied theme.
      /// </summary>
      public void SetTheme(string themeName)
      {
         _themeName = themeName ?? GroupingThemes.TagsName;
         SyncThemeCombo();
         BuildGroupsSection();
      }

      private void SyncThemeCombo()
      {
         _syncing = true;
         for (int i = 0; i < ThemeCombo.Items.Count; i++)
         {
            if (string.Equals(ThemeCombo.Items[i] as string, _themeName,
               StringComparison.Ordinal))
            {
               ThemeCombo.SelectedIndex = i;
               break;
            }
         }
         _syncing = false;
      }

      private void ThemeCombo_SelectionChanged(
         object sender, SelectionChangedEventArgs e)
      {
         if (_syncing || _tables == null)
         {
            return;
         }
         string name = ThemeCombo.SelectedItem as string;
         if (name != null)
         {
            ThemeRequested?.Invoke(this, name);
         }
      }

      /// <summary>
      /// Switch the layout selector to the applied layout without requesting a
      /// second host change.
      /// </summary>
      public void SetLayout(string layoutName)
      {
         _layoutName = EntityLayout.FromName(layoutName).Name;
         SyncLayoutCombo();
      }

      private void SyncLayoutCombo()
      {
         _syncing = true;
         for (int i = 0; i < LayoutCombo.Items.Count; i++)
         {
            if (string.Equals(LayoutCombo.Items[i] as string, _layoutName,
               StringComparison.Ordinal))
            {
               LayoutCombo.SelectedIndex = i;
               break;
            }
         }
         _syncing = false;
      }

      private void LayoutCombo_SelectionChanged(
         object sender, SelectionChangedEventArgs e)
      {
         if (_syncing || _tables == null)
         {
            return;
         }
         string name = LayoutCombo.SelectedItem as string;
         if (name != null)
         {
            LayoutRequested?.Invoke(this, name);
         }
      }

      /// <summary>
      /// The "Show only:" combo (backlog 043 UX): picking a group focuses the
      /// drawing on its members; picking "All groups" shows everything again —
      /// the Focus toggle's meaning without redefining what the checkboxes do.
      /// </summary>
      private void SoloCombo_SelectionChanged(
         object sender, SelectionChangedEventArgs e)
      {
         if (_syncing || _tables == null)
         {
            return;
         }
         string selection = SoloCombo.SelectedItem as string;
         if (selection == null)
         {
            return;
         }
         if (string.Equals(selection, AllGroupsLabel, StringComparison.Ordinal))
         {
            ShowAllRequested?.Invoke(this, EventArgs.Empty);
         }
         else
         {
            FocusRequested?.Invoke(this,
               new GroupFocusRequestedEventArgs(selection));
         }
      }

      private void CollapseAllButton_Click(object sender, RoutedEventArgs e)
      {
         CollapseAllRequested?.Invoke(this, true);
      }

      private void ExpandAllButton_Click(object sender, RoutedEventArgs e)
      {
         CollapseAllRequested?.Invoke(this, false);
      }

      /// <summary>
      /// Sync the checkbox states, the "Show only:" combo, and the grayed tree
      /// to a (possibly mutated) shared visibility (backlog 038). Called by the
      /// host after every toggle so the explorer never drifts from the drawing.
      /// </summary>
      public void SetVisibility(EntityVisibility visibility)
      {
         _visibility = visibility;
         SyncGroupCheckboxes();
         SyncSoloCombo();
         ApplyVisibilityToTree();
      }

      /// <summary>
      /// Sync the per-group collapse toggles to a (possibly mutated) shared
      /// collapse state (backlog 039). Called by the host after every toggle
      /// so the explorer never drifts from the drawing.
      /// </summary>
      public void SetCollapse(GroupCollapseState collapse)
      {
         _collapse = collapse;
         SyncCollapseButtons();
      }

      private void SyncCollapseButtons()
      {
         if (_collapseButtons.Count == 0)
         {
            return;
         }
         _syncing = true;
         foreach (var kv in _collapseButtons)
         {
            kv.Value.IsChecked =
               _collapse != null && _collapse.IsCollapsed(kv.Key);
         }
         _syncing = false;
      }

      /// <summary>
      /// Select the node for a table (canvas → explorer sync).
      /// </summary>
      public void SelectTable(string tableName)
      {
         if (_tableNodes.TryGetValue(tableName, out var node))
         {
            ModelTree.SelectedNode = node;
         }
      }

      private void SyncGroupCheckboxes()
      {
         if (_groupBoxes.Count == 0)
         {
            return;
         }
         _syncing = true;
         foreach (var kv in _groupBoxes)
         {
            kv.Value.IsChecked = _visibility == null ||
                                 _visibility.IsGroupVisible(kv.Key);
         }
         _syncing = false;
      }

      /// <summary>
      /// Gray the tree nodes of hidden entities so nothing is lost — a hidden
      /// table stays listed, just visibly struck from the drawing. The node
      /// content stays a plain string (a <see cref="TextBlock"/> content makes
      /// the WinUI 3 TreeView fall back to showing the node's type name), so
      /// the gray is applied to the realized <see cref="TreeViewItem"/>
      /// container — the only live surface a string node exposes. Nodes whose
      /// container is not yet realized (not yet expanded) simply show normal
      /// until the next sync.
      /// </summary>
      private void ApplyVisibilityToTree()
      {
         if (_visibility == null || _nodeTables.Count == 0)
         {
            return;
         }
         foreach (var kv in _nodeTables)
         {
            kv.Key.Content = FormatTableContent(kv.Value);
         }
         ApplyTreeForegrounds();
      }

      /// <summary>
      /// Pin every realized tree container's text to the near-black (the light
      /// panel background) — the root, column, and FK nodes that the visibility
      /// pass doesn't touch — and re-apply the visible/hidden table coloring.
      /// The theme-default foreground goes white in dark OS mode and vanishes
      /// against the light "Base:" panel, so no container is ever left on it.
      /// </summary>
      private void ApplyTreeForegrounds()
      {
         foreach (var root in ModelTree.RootNodes)
         {
            ApplyNodeForeground(root);
         }
      }

      private void ApplyNodeForeground(TreeViewNode node)
      {
         if (ModelTree.ContainerFromNode(node) is TreeViewItem item)
         {
            if (_nodeTables.TryGetValue(node, out var table))
            {
               bool visible = _visibility == null || _visibility.IsVisible(table);
               item.Foreground = visible ? TreeTextForeground : HiddenTableForeground;
            }
            else
            {
               item.Foreground = TreeTextForeground;
            }
         }
         foreach (var child in node.Children)
         {
            ApplyNodeForeground(child);
         }
      }

      private void ModelTree_Loaded(object sender, RoutedEventArgs e)
      {
         // The tree is only realized after it lays out; this is the first
         // moment the root + table containers exist to be pinned.
         ApplyTreeForegrounds();
      }

      private void ModelTree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
      {
         // Deferred until the expansion lays out, so the newly realized child
         // containers (a table's columns) get pinned too.
         DispatcherQueue.TryEnqueue(() => ApplyTreeForegrounds());
      }

      private static string FormatTableContent(TableInfo table)
      {
         return table.TableName + " (" + table.Columns.Count + " columns)" +
                (table.Tags != null && table.Tags.Count > 0
                   ? "  [" + string.Join(", ", table.Tags) + "]" : "");
      }

      private static string FormatColumn(ColumnInfo column)
      {
         var tags = new List<string>();
         if (column.IsKey)
         {
            tags.Add("PK");
         }
         if (column.IsForeignKey)
         {
            tags.Add("FK");
         }
         if (!string.IsNullOrEmpty(column.EnumerationName))
         {
            tags.Add("enum:" + column.EnumerationName);
         }
         string suffix = tags.Count > 0 ? " [" + string.Join(", ", tags) + "]" : "";
         return column.ColumnName + " : " + column.Type + suffix;
      }

      private void GroupBox_Checked(object sender, RoutedEventArgs e)
      {
         if (_syncing || _visibility == null)
         {
            return;
         }
         string group = (sender as CheckBox)?.Tag as string;
         if (group == null)
         {
            return;
         }
         GroupVisibilityChanged?.Invoke(this,
            new GroupVisibilityChangedEventArgs(group, true));
      }

      private void GroupBox_Unchecked(object sender, RoutedEventArgs e)
      {
         if (_syncing || _visibility == null)
         {
            return;
         }
         string group = (sender as CheckBox)?.Tag as string;
         if (group == null)
         {
            return;
         }
         GroupVisibilityChanged?.Invoke(this,
            new GroupVisibilityChangedEventArgs(group, false));
      }

      private void GroupCollapseButton_Checked(object sender, RoutedEventArgs e)
      {
         if (_syncing)
         {
            return;
         }
         string group = (sender as ToggleButton)?.Tag as string;
         if (group == null)
         {
            return;
         }
         GroupCollapseRequested?.Invoke(this,
            new GroupCollapseRequestedEventArgs(group, true));
      }

      private void GroupCollapseButton_Unchecked(object sender, RoutedEventArgs e)
      {
         if (_syncing)
         {
            return;
         }
         string group = (sender as ToggleButton)?.Tag as string;
         if (group == null)
         {
            return;
         }
         GroupCollapseRequested?.Invoke(this,
            new GroupCollapseRequestedEventArgs(group, false));
      }

      private void ShowAllButton_Click(object sender, RoutedEventArgs e)
      {
         ShowAllRequested?.Invoke(this, EventArgs.Empty);
      }

      private void ModelTree_ItemInvoked(
         TreeView sender, TreeViewItemInvokedEventArgs args)
      {
         // Walk up from the invoked node to the table node (the node itself
         // or an ancestor); column and FK nodes are not in the table map.
         var node = args.InvokedItem as TreeViewNode;
         while (node != null && !_nodeTables.ContainsKey(node))
         {
            node = node.Parent;
         }
         if (node != null && _nodeTables.TryGetValue(node, out var table))
         {
            TableSelected?.Invoke(this, table);
         }
      }
   }

   /// <summary>
   /// A group checkbox toggle (backlog 038): which group and whether it
   /// should now be visible.
   /// </summary>
   public sealed class GroupVisibilityChangedEventArgs : EventArgs
   {
      public string Group { get; }
      public bool Visible { get; }

      public GroupVisibilityChangedEventArgs(string group, bool visible)
      {
         Group = group;
         Visible = visible;
      }
   }

   /// <summary>
   /// A focus request (backlog 038): the group whose members should be the
   /// only ones drawn (plus ungrouped and pinned-show entities).
   /// </summary>
   public sealed class GroupFocusRequestedEventArgs : EventArgs
   {
      public string Group { get; }

      public GroupFocusRequestedEventArgs(string group)
      {
         Group = group;
      }
   }

   /// <summary>
   /// A collapse toggle (backlog 039): which group and whether it should now
   /// be collapsed into a package box.
   /// </summary>
   public sealed class GroupCollapseRequestedEventArgs : EventArgs
   {
      public string Group { get; }
      public bool Collapsed { get; }

      public GroupCollapseRequestedEventArgs(string group, bool collapsed)
      {
         Group = group;
         Collapsed = collapsed;
      }
   }
}
