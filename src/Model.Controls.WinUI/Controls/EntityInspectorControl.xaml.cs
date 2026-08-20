using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.System;

using Model.Data;
using ModelConsole.Editing;
using ModelConsole.Graph;

namespace ModelConsole.Controls
{
   /// <summary>
   /// Entity inspector: shows the metadata of the clicked graphic entity and
   /// lets the user edit it (backlog 029). For a table it lists the columns
   /// (name/type/size editable, PK toggle, add-FK, remove) plus add/remove
   /// entity actions; for a connector it shows the FK relationship with
   /// target/cardinality/roles editors and a delete action. Every edit
   /// mutates the live canonical model via <see cref="ModelEdits"/> and
   /// raises an event the host answers with a re-render — the edit surface
   /// is gated by the node's <see cref="NodeVerbs"/> (backlog 028), so the
   /// inspector only builds the controls a node kind actually supports.
   /// </summary>
   public sealed partial class EntityInspectorControl : UserControl
   {
      /// <summary>
      /// Raised when a POCO field was edited; the drawing should re-render.
      /// </summary>
      public event EventHandler ModelEdited;

      /// <summary>
      /// Raised when the user asks to delete a connector.
      /// </summary>
      public event EventHandler<FkRelation> DeleteRequested;

      /// <summary>
      /// Raised when the user renames an entity. The inspector does NOT
      /// mutate — the host applies the cascade (every referencing FK follows)
      /// and re-keys the layout.
      /// </summary>
      public event EventHandler<(TableInfo Table, string OldName, string NewName)> EntityRenamed;

      /// <summary>
      /// Raised when the user renames a column. The host applies the cascade
      /// (every referencing FK follows).
      /// </summary>
      public event EventHandler<(TableInfo Table, ColumnInfo Column, string OldName, string NewName)> ColumnRenamed;

      /// <summary>
      /// Raised when the user asks to remove an entity.
      /// </summary>
      public event EventHandler<TableInfo> EntityRemoved;

      /// <summary>
      /// Raised when the user adds a new entity (already scaffolded).
      /// </summary>
      public event EventHandler<TableInfo> EntityAdded;

      /// <summary>
      /// Raised after a structural edit to a table (column add/remove/rename,
      /// FK add/remove/target, key toggle): the host re-renders that table
      /// (partial re-route) and refreshes the explorer.
      /// </summary>
      public event EventHandler<TableInfo> StructureChanged;

      /// <summary>The table currently shown (or the connector's child table).</summary>
      private TableInfo _currentTable;

      /// <summary>The connector currently shown, when the inspector is in connector mode.</summary>
      private FkRelation _currentEdge;

      /// <summary>The model's tables, for the FK-target candidate lists.</summary>
      private IReadOnlyList<TableInfo> _tables;

      /// <summary>The model's enumerations, for the value-set readout (backlog 021).</summary>
      private IReadOnlyDictionary<string, Enumeration> _enumerations;

      public EntityInspectorControl()
      {
         this.InitializeComponent();
      }

      /// <summary>
      /// Show a table's metadata and edit surface: schema::table header, the
      /// entity's name/description editors, one section per column (name,
      /// editable type + size, PK toggle, make-FK row, remove), add-column /
      /// add-entity / remove-entity actions, and the read-only provenance +
      /// metadata sections. Each control is gated by the node's edit verbs
      /// (backlog 028), so the surface matches what the node kind supports.
      /// </summary>
      public void ShowTable(
         TableInfo table,
         IReadOnlyList<TableInfo> tables,
         IReadOnlyDictionary<string, Enumeration> enumerations = null)
      {
         _currentTable = table;
         _tables = tables;
         _enumerations = enumerations;
         HeaderText.Text = table.SchemaName + "::" + table.TableName;
         ContentPanel.Children.Clear();

         var verbs = GraphNodes.Entity(table).Verbs;

         // Name editor (rename) — the inspector does not mutate; the host
         // cascades the rename across referencing FKs and re-keys the layout.
         if (verbs.CanRename)
         {
            ContentPanel.Children.Add(new TextBlock
            {
               Text = "Name",
               FontWeight = FontWeights.SemiBold,
               FontSize = 12
            });
            var nameBox = new TextBox { Text = table.TableName, FontSize = 12 };
            nameBox.KeyDown += (s, e) =>
            {
               if (e.Key == VirtualKey.Enter)
               {
                  CommitTableName(table, nameBox);
                  e.Handled = true;
               }
            };
            nameBox.LostFocus += (s, e) => CommitTableName(table, nameBox);
            ContentPanel.Children.Add(nameBox);
         }

         // Description editor (backlog 024's readout becomes editable).
         if (verbs.CanEditDescription)
         {
            ContentPanel.Children.Add(new TextBlock
            {
               Text = "Description",
               FontWeight = FontWeights.SemiBold,
               FontSize = 12
            });
            var descBox = new TextBox
            {
               Text = table.Description ?? "",
               FontSize = 12,
               // WinUI 3's TextBox accepts only NoWrap/Wrap — WrapWholeWords
               // throws E_RUNTIME_SETVALUE at property-set time.
               TextWrapping = TextWrapping.Wrap
            };
            descBox.KeyDown += (s, e) =>
            {
               if (e.Key == VirtualKey.Enter)
               {
                  CommitDescription(table, descBox);
                  e.Handled = true;
               }
            };
            descBox.LostFocus += (s, e) => CommitDescription(table, descBox);
            ContentPanel.Children.Add(descBox);
         }
         else if (!string.IsNullOrEmpty(table.Description))
         {
            ContentPanel.Children.Add(new TextBlock
            {
               Text = table.Description,
               FontSize = 12,
               Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
               TextWrapping = TextWrapping.WrapWholeWords,
               Margin = new Thickness(0, 0, 0, 6)
            });
         }

         ContentPanel.Children.Add(new TextBlock
         {
            Text = "Column / Type / Size / Constraints",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4)
         });

         foreach (var column in table.Columns)
         {
            ContentPanel.Children.Add(BuildColumnSection(table, column));
            var description = BuildDescriptionReadout(column);
            if (description != null)
            {
               ContentPanel.Children.Add(description);
            }
            var enumReadout = BuildEnumReadout(column, enumerations);
            if (enumReadout != null)
            {
               ContentPanel.Children.Add(enumReadout);
            }
            var provenanceReadout = BuildProvenanceReadout(column);
            if (provenanceReadout != null)
            {
               ContentPanel.Children.Add(provenanceReadout);
            }
         }

         // Add column (scaffold a new column and append it).
         if (verbs.CanAddColumn)
         {
            var addColumnButton = new Button
            {
               Content = "Add column",
               HorizontalAlignment = HorizontalAlignment.Stretch,
               Margin = new Thickness(0, 6, 0, 0)
            };
            addColumnButton.Click += (s, e) =>
            {
               var column = new ColumnInfo
               {
                  ColumnName = UniqueColumnName(table, "NewColumn"),
                  Type = DataInfo.VARCHAR,
                  Size = 256
               };
               ModelEdits.AddColumn(table, column);
               StructureChanged?.Invoke(this, table);
               ShowTable(_currentTable, _tables, _enumerations);
            };
            ContentPanel.Children.Add(addColumnButton);
         }

         // Add entity (scaffold a new table with an Id PK column).
         ContentPanel.Children.Add(BuildAddEntityRow());

         // Remove entity.
         if (verbs.CanDelete)
         {
            var removeButton = new Button
            {
               Content = "Remove entity",
               HorizontalAlignment = HorizontalAlignment.Stretch,
               Margin = new Thickness(0, 4, 0, 0)
            };
            removeButton.Click += (s, e) =>
            {
               EntityRemoved?.Invoke(this, table);
               HeaderText.Text = "Entity";
               ContentPanel.Children.Clear();
            };
            ContentPanel.Children.Add(removeButton);
         }

         // Backlog 026: the entity's provenance, when the source declared one.
         // Shown next to the metadata section, mirroring the 022 readout.
         var provenanceText = ReadoutFormatter.Provenance(table.Provenance);
         if (provenanceText != null)
         {
            ContentPanel.Children.Add(new TextBlock
            {
               Text = "Provenance",
               FontWeight = FontWeights.SemiBold,
               FontSize = 12,
               Margin = new Thickness(0, 8, 0, 2)
            });
            ContentPanel.Children.Add(new TextBlock
            {
               Text = provenanceText,
               FontSize = 11,
               Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
               Margin = new Thickness(12, 0, 0, 0),
               TextWrapping = TextWrapping.WrapWholeWords
            });
         }

         // Backlog 022: the entity's metadata annotations, when the model
         // carried any. Read-only by design (the bag is session-only).
         var metadataLines = ReadoutFormatter.MetadataLines(table.Metadata);
         if (metadataLines.Count > 0)
         {
            ContentPanel.Children.Add(new TextBlock
            {
               Text = "Metadata",
               FontWeight = FontWeights.SemiBold,
               FontSize = 12,
               Margin = new Thickness(0, 8, 0, 2)
            });
            foreach (var line in metadataLines)
            {
               ContentPanel.Children.Add(new TextBlock
               {
                  Text = line,
                  FontSize = 11,
                  Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                  Margin = new Thickness(12, 0, 0, 0)
               });
            }
         }
      }

      /// <summary>
      /// Show a connector's FK relationship with target/cardinality/roles
      /// editors (gated by the dependency's verbs) and a delete action. When
      /// the edge carries its source constraint (backlog 022), the
      /// dependency's per-side cardinality/optionality and role names are
      /// shown beneath and edited in place.
      /// </summary>
      public void ShowConnector(FkRelation edge, IReadOnlyList<TableInfo> tables)
      {
         _currentEdge = edge;
         _tables = tables;
         _currentTable = tables?.FirstOrDefault(t => t.TableName == edge.ChildTable);
         HeaderText.Text = "Foreign Key";
         ContentPanel.Children.Clear();

         ContentPanel.Children.Add(new TextBlock
         {
            Text = edge.ChildTable + "." + edge.ChildColumn +
                   "  →  " + edge.ParentTable + "." + edge.ParentColumn,
            FontSize = 12,
            TextWrapping = TextWrapping.WrapWholeWords
         });

         var constraint = edge.Constraint;
         var verbs = GraphNodes.Dependency(edge).Verbs;

         if (constraint != null)
         {
            TextBlock cardinalityText = null;
            string cardinality = ReadoutFormatter.Cardinality(constraint);
            if (cardinality != null)
            {
               cardinalityText = new TextBlock
               {
                  Text = "Cardinality: " + cardinality,
                  FontSize = 12,
                  Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                  Margin = new Thickness(0, 4, 0, 0)
               };
               ContentPanel.Children.Add(cardinalityText);
            }

            TextBlock rolesText = null;
            string roles = ReadoutFormatter.Roles(constraint);
            if (roles != null)
            {
               rolesText = new TextBlock
               {
                  Text = "Roles: " + roles,
                  FontSize = 12,
                  Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                  Margin = new Thickness(0, 2, 0, 0)
               };
               ContentPanel.Children.Add(rolesText);
            }

            if (verbs.CanEditTarget)
            {
               ContentPanel.Children.Add(BuildTargetEditor(edge, constraint));
            }
            if (verbs.CanEditCardinality)
            {
               ContentPanel.Children.Add(BuildCardinalityEditor(constraint, cardinalityText));
            }
            if (verbs.CanEditRoles)
            {
               ContentPanel.Children.Add(BuildRolesEditor(constraint, rolesText));
            }
         }

         var deleteButton = new Button
         {
            Content = "Delete connector",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0)
         };
         deleteButton.Click += (s, e) => DeleteRequested?.Invoke(this, edge);
         ContentPanel.Children.Add(deleteButton);
      }

      /// <summary>
      /// Show the model-level readout (provenance + model metadata) — the
      /// inspector's idle state, shown when a model is loaded (backlog 022).
      /// Reads the live <see cref="Provenance"/> and metadata dictionary, so
      /// the readout can never drift from the model.
      /// </summary>
      public void ShowModel(
         Provenance provenance, IReadOnlyDictionary<string, string> metadata)
      {
         HeaderText.Text = "Model";
         ContentPanel.Children.Clear();

         string provenanceText = ReadoutFormatter.Provenance(provenance);
         if (provenanceText != null)
         {
            ContentPanel.Children.Add(new TextBlock
            {
               Text = provenanceText,
               FontSize = 12,
               TextWrapping = TextWrapping.WrapWholeWords
            });
            if (!string.IsNullOrEmpty(provenance.Notes))
            {
               ContentPanel.Children.Add(new TextBlock
               {
                  Text = "notes: " + provenance.Notes,
                  FontSize = 11,
                  Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                  Margin = new Thickness(0, 2, 0, 0),
                  TextWrapping = TextWrapping.WrapWholeWords
               });
            }
         }

         var metadataLines = ReadoutFormatter.MetadataLines(metadata);
         if (metadataLines.Count > 0)
         {
            ContentPanel.Children.Add(new TextBlock
            {
               Text = "Model metadata",
               FontWeight = FontWeights.SemiBold,
               FontSize = 12,
               Margin = new Thickness(0, 8, 0, 2)
            });
            foreach (var line in metadataLines)
            {
               ContentPanel.Children.Add(new TextBlock
               {
                  Text = line,
                  FontSize = 11,
                  Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                  Margin = new Thickness(12, 0, 0, 0)
               });
            }
         }

         if (provenanceText == null && metadataLines.Count == 0)
         {
            ContentPanel.Children.Add(new TextBlock
            {
               Text = "No model-level metadata or provenance.",
               FontSize = 12,
               Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
            });
         }
      }

      /// <summary>
      /// The per-column edit section: a name/type/size/remove row, a PK
      /// toggle + constraint readout row, and a make-FK row (when the column
      /// is not already a foreign key). Each control is gated by the
      /// element's edit verbs (backlog 028).
      /// </summary>
      private UIElement BuildColumnSection(TableInfo table, ColumnInfo column)
      {
         var verbs = GraphNodes.Element(table, column).Verbs;
         var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };

         // Row: name | type | size | remove.
         var row = new Grid { ColumnSpacing = 4 };
         row.ColumnDefinitions.Add(new ColumnDefinition
         {
            Width = new GridLength(1, GridUnitType.Star)
         });
         row.ColumnDefinitions.Add(new ColumnDefinition
         {
            Width = new GridLength(1, GridUnitType.Star)
         });
         row.ColumnDefinitions.Add(new ColumnDefinition
         {
            Width = new GridLength(1, GridUnitType.Star)
         });
         row.ColumnDefinitions.Add(new ColumnDefinition
         {
            Width = GridLength.Auto
         });

         FrameworkElement nameControl;
         if (verbs.CanRename)
         {
            var nameBox = new TextBox { Text = column.ColumnName, FontSize = 12 };
            nameBox.KeyDown += (s, e) =>
            {
               if (e.Key == VirtualKey.Enter)
               {
                  CommitColumnName(table, column, nameBox);
                  e.Handled = true;
               }
            };
            nameBox.LostFocus += (s, e) => CommitColumnName(table, column, nameBox);
            nameControl = nameBox;
         }
         else
         {
            nameControl = new TextBlock
            {
               Text = column.ColumnName,
               FontSize = 12,
               VerticalAlignment = VerticalAlignment.Center,
               TextTrimming = TextTrimming.CharacterEllipsis
            };
         }

         FrameworkElement typeControl;
         FrameworkElement sizeControl;
         if (verbs.CanEditType)
         {
            var typeBox = new TextBox { Text = column.Type, FontSize = 12 };
            var sizeBox = new TextBox
            {
               Text = column.Size > 0 ? column.Size.ToString() : "",
               FontSize = 12,
               PlaceholderText = "size"
            };
            Action commit = () => CommitTypeAndSize(column, typeBox, sizeBox);
            typeBox.KeyDown += (s, e) =>
            {
               if (e.Key == VirtualKey.Enter) { commit(); e.Handled = true; }
            };
            typeBox.LostFocus += (s, e) => commit();
            sizeBox.KeyDown += (s, e) =>
            {
               if (e.Key == VirtualKey.Enter) { commit(); e.Handled = true; }
            };
            sizeBox.LostFocus += (s, e) => commit();
            typeControl = typeBox;
            sizeControl = sizeBox;
         }
         else
         {
            typeControl = new TextBlock
            {
               Text = column.Type,
               FontSize = 12,
               VerticalAlignment = VerticalAlignment.Center
            };
            sizeControl = new TextBlock
            {
               Text = column.Size > 0 ? column.Size.ToString() : "",
               FontSize = 12,
               VerticalAlignment = VerticalAlignment.Center
            };
         }

         Grid.SetColumn(nameControl, 0);
         Grid.SetColumn(typeControl, 1);
         Grid.SetColumn(sizeControl, 2);
         row.Children.Add(nameControl);
         row.Children.Add(typeControl);
         row.Children.Add(sizeControl);

         if (verbs.CanRemoveColumn)
         {
            var removeButton = new Button
            {
               Content = "✕",
               FontSize = 10,
               Padding = new Thickness(6, 2, 6, 2),
               VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(removeButton, "Remove column");
            removeButton.Click += (s, e) =>
            {
               ModelEdits.RemoveColumn(table, column);
               StructureChanged?.Invoke(this, table);
               ShowTable(_currentTable, _tables, _enumerations);
            };
            Grid.SetColumn(removeButton, 3);
            row.Children.Add(removeButton);
         }

         panel.Children.Add(row);

         // Flags row: PK toggle + constraint readout.
         var flagsRow = new StackPanel
         {
            Orientation = Orientation.Horizontal,
            Spacing = 8
         };
         if (verbs.CanEditKey)
         {
            var pkCheck = new CheckBox
            {
               Content = "PK",
               IsChecked = column.IsKey,
               FontSize = 12,
               VerticalAlignment = VerticalAlignment.Center
            };
            pkCheck.Checked += (s, e) => ToggleKey(table, column, true);
            pkCheck.Unchecked += (s, e) => ToggleKey(table, column, false);
            flagsRow.Children.Add(pkCheck);
         }
         flagsRow.Children.Add(new TextBlock
         {
            Text = GetConstraintText(column),
            FontSize = 12,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            VerticalAlignment = VerticalAlignment.Center
         });
         panel.Children.Add(flagsRow);

         // Make-FK row: pick a parent table + column and add the constraint.
         if (verbs.CanAddForeignKey && !column.IsForeignKey)
         {
            panel.Children.Add(BuildAddForeignKeyRow(table, column));
         }

         return panel;
      }

      /// <summary>
      /// The make-FK row: a parent-table ComboBox, a parent-column ComboBox
      /// (repopulated when the table changes), and an Add button that creates
      /// the FK constraint on the column.
      /// </summary>
      private UIElement BuildAddForeignKeyRow(TableInfo table, ColumnInfo column)
      {
         var panel = new StackPanel
         {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(0, 2, 0, 0)
         };
         panel.Children.Add(new TextBlock
         {
            Text = "FK →",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
         });

         var tableCombo = new ComboBox { FontSize = 12, MinWidth = 90 };
         if (_tables != null)
         {
            foreach (var t in _tables)
            {
               tableCombo.Items.Add(t.TableName);
            }
         }
         if (tableCombo.Items.Count > 0)
         {
            tableCombo.SelectedIndex = 0;
         }

         var columnCombo = new ComboBox { FontSize = 12, MinWidth = 90 };
         PopulateColumnCombo(columnCombo, tableCombo.SelectedItem as string);
         tableCombo.SelectionChanged += (s, e) =>
            PopulateColumnCombo(columnCombo, tableCombo.SelectedItem as string);

         var addButton = new Button { Content = "Add", FontSize = 12 };
         addButton.Click += (s, e) =>
         {
            string parentTable = tableCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(parentTable))
            {
               return;
            }
            string parentColumn = columnCombo.SelectedItem as string;
            ModelEdits.AddForeignKey(
               table, column, parentTable, parentColumn, null, null, null, null);
            StructureChanged?.Invoke(this, table);
            ShowTable(_currentTable, _tables, _enumerations);
         };

         panel.Children.Add(tableCombo);
         panel.Children.Add(columnCombo);
         panel.Children.Add(addButton);
         return panel;
      }

      /// <summary>
      /// The add-entity row: a name TextBox + Add button that scaffolds a new
      /// table (the current table's schema, one Id PK column) and raises
      /// <see cref="EntityAdded"/>.
      /// </summary>
      private UIElement BuildAddEntityRow()
      {
         var panel = new StackPanel
         {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(0, 6, 0, 0)
         };
         panel.Children.Add(new TextBlock
         {
            Text = "Add entity:",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
         });

         var nameBox = new TextBox
         {
            PlaceholderText = "name",
            FontSize = 12,
            MinWidth = 100
         };
         var addButton = new Button { Content = "Add", FontSize = 12 };
         addButton.Click += (s, e) =>
         {
            string name = nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
               return;
            }
            var idColumn = new ColumnInfo { ColumnName = "Id", Type = "INT", Size = 0 };
            idColumn.Add(new ConstraintInfo
            {
               Type = DataInfo.PRIMARY_KEY,
               TableName = name,
               ColumnName = "Id"
            });
            var newTable = new TableInfo
            {
               SchemaName = _currentTable?.SchemaName,
               TableName = name,
               Columns = new List<ColumnInfo> { idColumn }
            };
            EntityAdded?.Invoke(this, newTable);
            ShowTable(_currentTable, _tables, _enumerations);
         };

         panel.Children.Add(nameBox);
         panel.Children.Add(addButton);
         return panel;
      }

      /// <summary>
      /// The connector's target editor: parent-table + parent-column
      /// ComboBoxes and an Apply button that retargets the constraint.
      /// </summary>
      private UIElement BuildTargetEditor(FkRelation edge, ConstraintInfo constraint)
      {
         var panel = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
         panel.Children.Add(new TextBlock
         {
            Text = "Target",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12
         });

         var row = new StackPanel
         {
            Orientation = Orientation.Horizontal,
            Spacing = 4
         };
         var tableCombo = new ComboBox { FontSize = 12, MinWidth = 90 };
         if (_tables != null)
         {
            foreach (var t in _tables)
            {
               tableCombo.Items.Add(t.TableName);
            }
         }
         tableCombo.SelectedItem = edge.ParentTable;

         var columnCombo = new ComboBox { FontSize = 12, MinWidth = 90 };
         PopulateColumnCombo(columnCombo, edge.ParentTable);
         if (edge.ParentColumn != null)
         {
            columnCombo.SelectedItem = edge.ParentColumn;
         }
         tableCombo.SelectionChanged += (s, e) =>
            PopulateColumnCombo(columnCombo, tableCombo.SelectedItem as string);

         var applyButton = new Button { Content = "Apply", FontSize = 12 };
         applyButton.Click += (s, e) =>
         {
            string parentTable = tableCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(parentTable))
            {
               return;
            }
            string parentColumn = columnCombo.SelectedItem as string;
            ModelEdits.EditForeignKeyTarget(constraint, parentTable, parentColumn);
            if (_currentTable != null)
            {
               StructureChanged?.Invoke(this, _currentTable);
            }
            ShowConnector(_currentEdge, _tables);
         };

         row.Children.Add(tableCombo);
         row.Children.Add(columnCombo);
         row.Children.Add(applyButton);
         panel.Children.Add(row);
         return panel;
      }

      /// <summary>
      /// The connector's cardinality editor: min/max TextBoxes (blank or
      /// '*' means unbounded). Commits update the readout line in place.
      /// </summary>
      private UIElement BuildCardinalityEditor(
         ConstraintInfo constraint, TextBlock readout)
      {
         var panel = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
         panel.Children.Add(new TextBlock
         {
            Text = "Cardinality",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12
         });

         var row = new StackPanel
         {
            Orientation = Orientation.Horizontal,
            Spacing = 4
         };
         var minBox = new TextBox
         {
            Text = constraint.MinCardinality?.ToString() ?? "",
            FontSize = 12,
            Width = 48,
            PlaceholderText = "min"
         };
         var maxBox = new TextBox
         {
            Text = constraint.MaxCardinality?.ToString() ?? "",
            FontSize = 12,
            Width = 48,
            PlaceholderText = "max"
         };
         Action commit = () => CommitCardinality(constraint, minBox, maxBox, readout);
         minBox.KeyDown += (s, e) =>
         {
            if (e.Key == VirtualKey.Enter) { commit(); e.Handled = true; }
         };
         minBox.LostFocus += (s, e) => commit();
         maxBox.KeyDown += (s, e) =>
         {
            if (e.Key == VirtualKey.Enter) { commit(); e.Handled = true; }
         };
         maxBox.LostFocus += (s, e) => commit();

         row.Children.Add(minBox);
         row.Children.Add(new TextBlock
         {
            Text = "..",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
         });
         row.Children.Add(maxBox);
         panel.Children.Add(row);
         return panel;
      }

      /// <summary>
      /// The connector's roles editor: child + parent role TextBoxes.
      /// Commits update the readout line in place.
      /// </summary>
      private UIElement BuildRolesEditor(
         ConstraintInfo constraint, TextBlock readout)
      {
         var panel = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
         panel.Children.Add(new TextBlock
         {
            Text = "Roles",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12
         });

         var childRow = new StackPanel
         {
            Orientation = Orientation.Horizontal,
            Spacing = 4
         };
         childRow.Children.Add(new TextBlock
         {
            Text = "child",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 40
         });
         var childBox = new TextBox
         {
            Text = constraint.ChildRole ?? "",
            FontSize = 12,
            MinWidth = 100
         };
         childRow.Children.Add(childBox);

         var parentRow = new StackPanel
         {
            Orientation = Orientation.Horizontal,
            Spacing = 4
         };
         parentRow.Children.Add(new TextBlock
         {
            Text = "parent",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 40
         });
         var parentBox = new TextBox
         {
            Text = constraint.ParentRole ?? "",
            FontSize = 12,
            MinWidth = 100
         };
         parentRow.Children.Add(parentBox);

         Action commit = () => CommitRoles(constraint, childBox, parentBox, readout);
         childBox.KeyDown += (s, e) =>
         {
            if (e.Key == VirtualKey.Enter) { commit(); e.Handled = true; }
         };
         childBox.LostFocus += (s, e) => commit();
         parentBox.KeyDown += (s, e) =>
         {
            if (e.Key == VirtualKey.Enter) { commit(); e.Handled = true; }
         };
         parentBox.LostFocus += (s, e) => commit();

         panel.Children.Add(childRow);
         panel.Children.Add(parentRow);
         return panel;
      }

      /// <summary>
      /// A read-only description line for a column (backlog 024): the column's
      /// prose, when the model carried one. Null when the column has none.
      /// </summary>
      private static UIElement BuildDescriptionReadout(ColumnInfo column)
      {
         if (string.IsNullOrEmpty(column.Description)) return null;
         return new TextBlock
         {
            Text = column.Description,
            FontSize = 11,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            Margin = new Thickness(12, 0, 0, 4),
            TextWrapping = TextWrapping.WrapWholeWords
         };
      }

      /// <summary>
      /// A read-only provenance line for a column (backlog 026): the column's
      /// source provenance, when the model carried one. Null when the column
      /// has none.
      /// </summary>
      private static UIElement BuildProvenanceReadout(ColumnInfo column)
      {
         string text = ReadoutFormatter.Provenance(column.Provenance);
         if (text == null) return null;
         return new TextBlock
         {
            Text = text,
            FontSize = 11,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            Margin = new Thickness(12, 0, 0, 4),
            TextWrapping = TextWrapping.WrapWholeWords
         };
      }

      /// <summary>
      /// A read-only value-set line for an enum-typed column (backlog 021):
      /// "enum Gender: M, F, OTHER". Null when the column is not enum-typed
      /// or the model's enumerations are not available.
      /// </summary>
      private static UIElement BuildEnumReadout(
         ColumnInfo column, IReadOnlyDictionary<string, Enumeration> enumerations)
      {
         if (string.IsNullOrEmpty(column.EnumerationName) || enumerations == null ||
             !enumerations.TryGetValue(column.EnumerationName, out var enumeration))
         {
            return null;
         }
         return new TextBlock
         {
            Text = "enum " + enumeration.Name + ": " + enumeration.ValueList,
            FontSize = 11,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            Margin = new Thickness(12, 0, 0, 4),
            TextWrapping = TextWrapping.WrapWholeWords
         };
      }

      /// <summary>
      /// Commit an edited entity name: raise <see cref="EntityRenamed"/> (the
      /// host cascades the rename) and update the header in place.
      /// </summary>
      private void CommitTableName(TableInfo table, TextBox box)
      {
         string newName = box.Text.Trim();
         if (string.IsNullOrEmpty(newName))
         {
            box.Text = table.TableName;
            return;
         }
         if (!string.Equals(table.TableName, newName, StringComparison.Ordinal))
         {
            string oldName = table.TableName;
            EntityRenamed?.Invoke(this, (table, oldName, newName));
            HeaderText.Text = table.SchemaName + "::" + newName;
         }
      }

      /// <summary>
      /// Commit an edited column name: raise <see cref="ColumnRenamed"/> (the
      /// host cascades the rename). The box already shows the new name.
      /// </summary>
      private void CommitColumnName(TableInfo table, ColumnInfo column, TextBox box)
      {
         string newName = box.Text.Trim();
         if (string.IsNullOrEmpty(newName))
         {
            box.Text = column.ColumnName;
            return;
         }
         if (!string.Equals(column.ColumnName, newName, StringComparison.Ordinal))
         {
            string oldName = column.ColumnName;
            ColumnRenamed?.Invoke(this, (table, column, oldName, newName));
         }
      }

      /// <summary>
      /// Commit an edited entity description to the shared POCO and ask for a
      /// re-render when it actually changed.
      /// </summary>
      private void CommitDescription(TableInfo table, TextBox box)
      {
         if (!string.Equals(table.Description ?? "", box.Text, StringComparison.Ordinal))
         {
            ModelEdits.SetDescription(table, box.Text);
            ModelEdited?.Invoke(this, EventArgs.Empty);
         }
      }

      /// <summary>
      /// Commit an edited column type + size to the shared POCO and ask for a
      /// re-render when either actually changed. An unparsable size keeps the
      /// current value.
      /// </summary>
      private void CommitTypeAndSize(ColumnInfo column, TextBox typeBox, TextBox sizeBox)
      {
         string type = typeBox.Text;
         int size = column.Size;
         if (int.TryParse(sizeBox.Text, out int parsed))
         {
            size = parsed;
         }
         if (!string.Equals(column.Type, type, StringComparison.Ordinal) ||
             column.Size != size)
         {
            ModelEdits.SetColumnType(column, type, size);
            ModelEdited?.Invoke(this, EventArgs.Empty);
         }
      }

      /// <summary>
      /// Toggle a column's key marking via <see cref="ModelEdits.SetKey"/>,
      /// then raise <see cref="StructureChanged"/> and re-show the table so
      /// the toggle reflects the new state.
      /// </summary>
      private void ToggleKey(TableInfo table, ColumnInfo column, bool isKey)
      {
         ModelEdits.SetKey(table, column, isKey);
         StructureChanged?.Invoke(this, table);
         ShowTable(_currentTable, _tables, _enumerations);
      }

      /// <summary>
      /// Commit edited cardinality bounds; update the readout line in place
      /// (or re-show the connector when the readout was not yet shown).
      /// </summary>
      private void CommitCardinality(
         ConstraintInfo constraint, TextBox minBox, TextBox maxBox, TextBlock readout)
      {
         int? min = ParseCardinality(minBox.Text, constraint.MinCardinality);
         int? max = ParseCardinality(maxBox.Text, constraint.MaxCardinality);
         ModelEdits.SetCardinality(constraint, min, max);
         if (readout != null)
         {
            string value = ReadoutFormatter.Cardinality(constraint);
            readout.Text = "Cardinality: " + (value ?? "");
         }
         else
         {
            ShowConnector(_currentEdge, _tables);
         }
         ModelEdited?.Invoke(this, EventArgs.Empty);
      }

      /// <summary>
      /// Commit edited role names; update the readout line in place (or
      /// re-show the connector when the readout was not yet shown).
      /// </summary>
      private void CommitRoles(
         ConstraintInfo constraint, TextBox childBox, TextBox parentBox, TextBlock readout)
      {
         ModelEdits.SetRoles(constraint, childBox.Text, parentBox.Text);
         if (readout != null)
         {
            string value = ReadoutFormatter.Roles(constraint);
            readout.Text = "Roles: " + (value ?? "");
         }
         else
         {
            ShowConnector(_currentEdge, _tables);
         }
         ModelEdited?.Invoke(this, EventArgs.Empty);
      }

      /// <summary>
      /// Parse a cardinality bound: blank or '*' means unbounded (null); an
      /// unparsable value keeps the current bound.
      /// </summary>
      private static int? ParseCardinality(string text, int? fallback)
      {
         text = text.Trim();
         if (string.IsNullOrEmpty(text) || text == "*")
         {
            return null;
         }
         return int.TryParse(text, out int value) ? value : fallback;
      }

      /// <summary>
      /// Fill a parent-column ComboBox from the named table, preselecting its
      /// primary key when it has one.
      /// </summary>
      private void PopulateColumnCombo(ComboBox combo, string tableName)
      {
         combo.Items.Clear();
         var table = _tables?.FirstOrDefault(t => t.TableName == tableName);
         if (table == null || table.Columns == null)
         {
            return;
         }
         foreach (var c in table.Columns)
         {
            combo.Items.Add(c.ColumnName);
         }
         var pk = table.Columns.FirstOrDefault(c => c.IsKey);
         if (pk != null)
         {
            combo.SelectedItem = pk.ColumnName;
         }
         else if (combo.Items.Count > 0)
         {
            combo.SelectedIndex = 0;
         }
      }

      /// <summary>
      /// A column name that does not collide with the table's existing
      /// columns (NewColumn, NewColumn1, ...).
      /// </summary>
      private static string UniqueColumnName(TableInfo table, string baseName)
      {
         var names = new HashSet<string>(table.Columns.Select(c => c.ColumnName));
         string name = baseName;
         int i = 1;
         while (names.Contains(name))
         {
            name = baseName + i;
            i++;
         }
         return name;
      }

      private static string GetConstraintText(ColumnInfo column)
      {
         var parts = new List<string>();
         if (column.IsKey)
         {
            parts.Add("PK");
         }
         if (column.IsForeignKey)
         {
            parts.Add("FK");
         }
         return string.Join(", ", parts);
      }
   }
}
