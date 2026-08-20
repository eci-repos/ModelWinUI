using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
   /// table with a child node per column (name, type, PK/FK tags), plus a
   /// foreign-key list. Clicking a table node raises
   /// <see cref="TableSelected"/> so the drawing can select it.
   /// </summary>
   public sealed partial class ModelExplorerControl : UserControl
   {
      /// <summary>
      /// Raised when a table node is clicked in the tree.
      /// </summary>
      public event EventHandler<TableInfo> TableSelected;

      private readonly Dictionary<string, TreeViewNode> _tableNodes =
         new Dictionary<string, TreeViewNode>();

      /// <summary>Maps each table node to its <see cref="TableInfo"/>.</summary>
      private readonly Dictionary<TreeViewNode, TableInfo> _nodeTables =
         new Dictionary<TreeViewNode, TableInfo>();

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
      /// Rebuild the tree from a model (a list of tables).
      /// </summary>
      public void SetModel(IReadOnlyList<TableInfo> tables)
      {
         ModelTree.RootNodes.Clear();
         _tableNodes.Clear();
         _nodeTables.Clear();

         if (tables == null || tables.Count == 0)
         {
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
               Content = table.TableName + " (" + table.Columns.Count + " columns)" +
                         (table.Tags != null && table.Tags.Count > 0
                            ? "  [" + string.Join(", ", table.Tags) + "]" : "")
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
}
