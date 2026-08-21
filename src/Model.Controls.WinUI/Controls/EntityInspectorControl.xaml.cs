using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.System;
using Windows.UI;

using CommunityToolkit.Mvvm.DependencyInjection;

using Model.Data;
using ModelConsole.Controls.Helpers;
using ModelConsole.Diagnostics;
using ModelConsole.Editing;
using ModelConsole.Graph;
using ModelConsole.Palette;

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

      /// <summary>
      /// Raised when the user pins/unpins the shown table (backlog 038). The
      /// <see cref="EntityVisibility.PinState"/> intent: true = pinned-show,
      /// false = pinned-hide, null = unpinned (back to the group rule). The
      /// inspector does not mutate — the host applies the pin to the shared
      /// visibility and re-renders.
      /// </summary>
      public event EventHandler<(string TableName, bool? Pinned)> VisibilityPinChanged;

      /// <summary>The table currently shown (or the connector's child table).</summary>
      private TableInfo _currentTable;

      /// <summary>The connector currently shown, when the inspector is in connector mode.</summary>
      private FkRelation _currentEdge;

      /// <summary>The model's tables, for the FK-target candidate lists.</summary>
      private IReadOnlyList<TableInfo> _tables;

      /// <summary>The model's enumerations, for the value-set readout (backlog 021).</summary>
      private IReadOnlyDictionary<string, Enumeration> _enumerations;

      /// <summary>
      /// The shared view-side visibility (backlog 038); drives the Show/Hide
      /// pin toggles' checked state. The host passes the same instance it
      /// gives the drawing and the explorer.
      /// </summary>
      private EntityVisibility _visibility;

      /// <summary>Guards programmatic toggle updates from re-entering the handlers.</summary>
      private bool _syncingPins;

      /// <summary>The Show/Hide pin toggles of the table currently shown (null until built).</summary>
      private ToggleButton _showPinButton;
      private ToggleButton _hidePinButton;

      /// <summary>
      /// View/edit mode (backlog 042): false (default) shows the read-only
      /// readout; true reveals the 029 edit surface. The header's Edit/Done
      /// button flips it and re-renders the content.
      /// </summary>
      private bool _editMode;

      /// <summary>
      /// True while the edit surface is shown (backlog 042). A host opening
      /// the details window can default it read-only and let the Edit button
      /// switch.
      /// </summary>
      public bool IsEditMode => _editMode;

      /// <summary>
      /// The inspector's panel color (backlog 041) — the body follows the
      /// drawing-surface "Base:" color the renderer bar selects; defaults to
      /// the shared canvas background (white).
      /// </summary>
      private Color _backgroundColor =
         HexColor.FromHex(TablePalette.CanvasBackgroundHex);

      /// <summary>
      /// The diagnostics channel (backlog 037): rejected tag names surface
      /// here as a message, never as a crash. Resolved lazily at first use.
      /// </summary>
      private ILogService m_Log;

      public EntityInspectorControl()
      {
         this.InitializeComponent();

         // The header keeps its pastel chrome; the body (root + content
         // panel) paints the base color so the working area matches the
         // drawing surface.
         RootGrid.Background = new SolidColorBrush(_backgroundColor);
         ContentPanel.Background = new SolidColorBrush(_backgroundColor);

         m_Log = Ioc.Default.GetRequiredService<ILogService>();
      }

      /// <summary>
      /// The inspector's background color (backlog 041) — follows the
      /// renderer bar's "Base:" selection, defaulting to white.
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
               ContentPanel.Background = new SolidColorBrush(value);
            }
         }
      }

      // -------------------------------------------------------------------
      // Card layout (backlog 042): the inspector content is built as bordered
      // "cards" — one per logical section (entity, columns, foreign keys,
      // properties) — and the per-column rows/sub-cards alternate between the
      // body background (white by default) and a very light gray band, so the
      // eye can follow one column down the list. Pure presentation: every
      // control, gate, and commit handler is unchanged.
      // -------------------------------------------------------------------

      /// <summary>PK badge text color — a deep blue from the entity family.</summary>
      private const string PkBadgeTextHex = "#1E5F9A";

      /// <summary>FK badge text color — a deep green from the reference family.</summary>
      private const string FkBadgeTextHex = "#2E6B24";

      /// <summary>The muted "secondary" text brush used across the readouts.</summary>
      private static readonly Brush MutedForeground =
         new SolidColorBrush(Microsoft.UI.Colors.Gray);

      private SolidColorBrush _cardBorderBrush;
      private SolidColorBrush _alternateBandBrush;

      /// <summary>
      /// The card hairline border (backlog 042) — the shared control border
      /// from the theme dictionary, resolved lazily.
      /// </summary>
      private SolidColorBrush CardBorderBrush()
      {
         return _cardBorderBrush ??=
            Application.Current.Resources["ControlBorderBrush"] as SolidColorBrush;
      }

      /// <summary>
      /// The alternating light-gray band (backlog 042) — the "gray" of the
      /// white/gray zebra, from the theme dictionary so a host can retune it.
      /// </summary>
      private SolidColorBrush AlternateBandBrush()
      {
         return _alternateBandBrush ??=
            Application.Current.Resources["InspectorAlternateBandBrush"]
               as SolidColorBrush;
      }

      /// <summary>
      /// Wrap a section's content in a bordered, rounded card (backlog 042).
      /// </summary>
      private Border MakeCard(UIElement content)
      {
         return new Border
         {
            BorderBrush = CardBorderBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Child = content
         };
      }

      /// <summary>A section title line inside a card.</summary>
      private static TextBlock MakeSectionTitle(string text)
      {
         return new TextBlock
         {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4)
         };
      }

      /// <summary>
      /// A zebra band: the gray/white alternating row background. The
      /// non-alternating (white) row is transparent so the body's base color
      /// shows through.
      /// </summary>
      private Border MakeBand(bool alternate, UIElement content)
      {
         return new Border
         {
            Background = alternate ? AlternateBandBrush() : null,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 5, 8, 5),
            Margin = new Thickness(0, 2, 0, 2),
            Child = content
         };
      }

      /// <summary>
      /// A small PK/FK badge chip — tinted from the model's own color
      /// families (entity blue / reference green) so the key marking echoes
      /// the drawing.
      /// </summary>
      private static Border MakeChip(string text, string backgroundHex, string foregroundHex)
      {
         return new Border
         {
            Background = new SolidColorBrush(HexColor.FromHex(backgroundHex)),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
               Text = text,
               FontSize = 10,
               FontWeight = FontWeights.SemiBold,
               Foreground = new SolidColorBrush(HexColor.FromHex(foregroundHex)),
               VerticalAlignment = VerticalAlignment.Center
            }
         };
      }

      /// <summary>
      /// A read-only column row (backlog 042): line 1 = column name (bold) +
      /// PK/FK chips on the right; line 2 = type·size, muted and indented,
      /// then optional description/enum/provenance readouts. The row is a
      /// zebra band so columns read as alternating panels down the list.
      /// </summary>
      private UIElement BuildReadOnlyColumnRow(
         ColumnInfo column,
         bool alternate,
         IReadOnlyDictionary<string, Enumeration> enumerations)
      {
         var panel = new StackPanel { Spacing = 2 };

         var head = new Grid { ColumnSpacing = 6 };
         head.ColumnDefinitions.Add(new ColumnDefinition
         {
            Width = new GridLength(1, GridUnitType.Star)
         });
         head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

         var nameText = new TextBlock
         {
            Text = column.ColumnName,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
         };
         var chipRow = new StackPanel { Orientation = Orientation.Horizontal };
         if (column.IsKey)
         {
            chipRow.Children.Add(MakeChip(
               "PK", TablePalette.EntityBannerHex, PkBadgeTextHex));
         }
         if (column.IsForeignKey)
         {
            chipRow.Children.Add(MakeChip(
               "FK", TablePalette.ReferenceBannerHex, FkBadgeTextHex));
         }
         Grid.SetColumn(nameText, 0);
         Grid.SetColumn(chipRow, 1);
         head.Children.Add(nameText);
         head.Children.Add(chipRow);
         panel.Children.Add(head);

         string typeText = column.Type;
         if (column.Size > 0)
         {
            typeText += "(" + column.Size + ")";
         }
         panel.Children.Add(new TextBlock
         {
            Text = typeText,
            FontSize = 11,
            Foreground = MutedForeground,
            Margin = new Thickness(2, 0, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
         });

         var description = BuildDescriptionReadout(column);
         if (description != null)
         {
            panel.Children.Add(description);
         }
         var enumReadout = BuildEnumReadout(column, enumerations);
         if (enumReadout != null)
         {
            panel.Children.Add(enumReadout);
         }
         var provenanceReadout = BuildProvenanceReadout(column);
         if (provenanceReadout != null)
         {
            panel.Children.Add(provenanceReadout);
         }

         return MakeBand(alternate, panel);
      }

      /// <summary>
      /// Show a table's metadata — read-only by default, or the full 029 edit
      /// surface when <see cref="IsEditMode"/> is set (backlog 042): the
      /// schema::table header, the entity's name/description editors, one
      /// section per column (name, editable type + size, PK toggle, make-FK
      /// row, remove), add-column / add-entity / remove-entity actions, and
      /// the read-only provenance + metadata readouts. Each control is gated
      /// by the node's edit verbs (backlog 028), so the surface matches what
      /// the node kind supports. Both surfaces read the same live model — the
      /// readout can never drift from the drawing.
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
         _showPinButton = null;
         _hidePinButton = null;

         if (_editMode)
         {
            BuildEditTable(table, tables, enumerations);
         }
         else
         {
            BuildReadOnlyTable(table, tables, enumerations);
         }
         UpdateModeButton();
      }

      /// <summary>
      /// The 029 edit surface for a table, gated by the entity's edit verbs
      /// (backlog 028). Shown only in edit mode (backlog 042). Built as cards
      /// (backlog 042): one for the entity-level editors (name, description,
      /// tags, visibility), one for the columns (each a zebra-banded sub-card
      /// holding all of that column's controls), one for the actions, and the
      /// read-only properties card.
      /// </summary>
      private void BuildEditTable(
         TableInfo table,
         IReadOnlyList<TableInfo> tables,
         IReadOnlyDictionary<string, Enumeration> enumerations)
      {
         var verbs = GraphNodes.Entity(table).Verbs;

         // Entity card — all the entity-level editors in one bordered panel.
         var entityCard = new StackPanel { Spacing = 4 };

         // Name editor (rename) — the inspector does not mutate; the host
         // cascades the rename across referencing FKs and re-keys the layout.
         if (verbs.CanRename)
         {
            entityCard.Children.Add(new TextBlock
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
            entityCard.Children.Add(nameBox);
         }

         // Description editor (backlog 024's readout becomes editable).
         if (verbs.CanEditDescription)
         {
            entityCard.Children.Add(new TextBlock
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
            entityCard.Children.Add(descBox);
         }
         else if (!string.IsNullOrEmpty(table.Description))
         {
            entityCard.Children.Add(new TextBlock
            {
               Text = table.Description,
               FontSize = 12,
               Foreground = MutedForeground,
               TextWrapping = TextWrapping.WrapWholeWords
            });
         }

         // Tags editor (backlog 037): a comma-separated list of UML-ready
         // identifiers, committed through ModelEdits.SetTableTags (normalization +
         // identifier validation; rejected names surface on the diagnostics
         // log, never a crash).
         if (verbs.CanEditTags)
         {
            entityCard.Children.Add(new TextBlock
            {
               Text = "Tags (comma-separated)",
               FontWeight = FontWeights.SemiBold,
               FontSize = 12
            });
            var tagsBox = new TextBox
            {
               Text = table.Tags != null ? string.Join(", ", table.Tags) : "",
               FontSize = 12
            };
            tagsBox.KeyDown += (s, e) =>
            {
               if (e.Key == VirtualKey.Enter)
               {
                  CommitTags(table, tagsBox);
                  e.Handled = true;
               }
            };
            tagsBox.LostFocus += (s, e) => CommitTags(table, tagsBox);
            entityCard.Children.Add(tagsBox);
         }
         else if (table.Tags != null && table.Tags.Count > 0)
         {
            entityCard.Children.Add(new TextBlock
            {
               Text = "Tags: " + string.Join(", ", table.Tags),
               FontSize = 12,
               Foreground = MutedForeground,
               TextWrapping = TextWrapping.WrapWholeWords
            });
         }

         // Visibility pins (backlog 038): Show forces the table visible even
         // when its group is hidden; Hide forces it hidden even when its group
         // is visible; toggling a pin off returns the table to its group's
         // rule. The host applies the pin to the shared visibility.
         if (verbs.CanToggleVisibility)
         {
            entityCard.Children.Add(new TextBlock
            {
               Text = "Visibility",
               FontWeight = FontWeights.SemiBold,
               FontSize = 12
            });
            var showButton = new ToggleButton
            {
               Content = "Show",
               MinWidth = 0,
               Padding = new Thickness(10, 2, 10, 2)
            };
            var hideButton = new ToggleButton
            {
               Content = "Hide",
               MinWidth = 0,
               Padding = new Thickness(10, 2, 10, 2),
               Margin = new Thickness(6, 0, 0, 0)
            };
            bool? pin = _visibility?.PinState(table.TableName);
            showButton.IsChecked = pin == true;
            hideButton.IsChecked = pin == false;
            showButton.Click += (s, e) =>
               CommitPin(table, showButton.IsChecked == true ? true : (bool?)null);
            hideButton.Click += (s, e) =>
               CommitPin(table, hideButton.IsChecked == true ? false : (bool?)null);
            entityCard.Children.Add(new StackPanel
            {
               Orientation = Orientation.Horizontal,
               Spacing = 0,
               Children = { showButton, hideButton }
            });
            _showPinButton = showButton;
            _hidePinButton = hideButton;
         }

         ContentPanel.Children.Add(MakeCard(entityCard));

         // Columns card — one zebra-banded sub-card per column, holding all of
         // that column's edit controls (name, type/size, PK, make-FK, remove).
         var columnsCard = new StackPanel();
         columnsCard.Children.Add(MakeSectionTitle("Columns"));
         bool alternate = false;
         foreach (var column in table.Columns)
         {
            columnsCard.Children.Add(
               BuildColumnSection(table, column, alternate, enumerations));
            alternate = !alternate;
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
            columnsCard.Children.Add(addColumnButton);
         }
         ContentPanel.Children.Add(MakeCard(columnsCard));

         // Actions card — add entity (scaffold a new table with an Id PK
         // column) and remove entity.
         var actionsCard = new StackPanel { Spacing = 4 };
         actionsCard.Children.Add(BuildAddEntityRow());
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
            actionsCard.Children.Add(removeButton);
         }
         ContentPanel.Children.Add(MakeCard(actionsCard));

         // The read-only properties card (026 provenance + 022 metadata).
         var properties = BuildPropertiesCard(table);
         if (properties != null)
         {
            ContentPanel.Children.Add(properties);
         }
      }

      /// <summary>
      /// The read-only table readout (backlog 042): description, tags, the
      /// column list (two-line rows with PK/FK chips), a foreign-key list, and
      /// the provenance + metadata sections. No edit controls — the Edit
      /// button switches to the 029 surface. Built as cards: an entity
      /// summary, the zebra-banded columns, the foreign keys, and the
      /// properties.
      /// </summary>
      private void BuildReadOnlyTable(
         TableInfo table,
         IReadOnlyList<TableInfo> tables,
         IReadOnlyDictionary<string, Enumeration> enumerations)
      {
         // Entity card: a one-line stats summary, then description + tags.
         var entityCard = new StackPanel { Spacing = 4 };
         int fkCount = table.Columns.Count(c => c.IsForeignKey);
         entityCard.Children.Add(new TextBlock
         {
            Text = table.Columns.Count + " column" +
               (table.Columns.Count == 1 ? "" : "s") + " · " +
               fkCount + " foreign key" + (fkCount == 1 ? "" : "s"),
            FontSize = 11,
            Foreground = MutedForeground
         });
         if (!string.IsNullOrEmpty(table.Description))
         {
            entityCard.Children.Add(new TextBlock
            {
               Text = table.Description,
               FontSize = 12,
               Foreground = MutedForeground,
               TextWrapping = TextWrapping.WrapWholeWords
            });
         }
         if (table.Tags != null && table.Tags.Count > 0)
         {
            entityCard.Children.Add(new TextBlock
            {
               Text = "Tags: " + string.Join(", ", table.Tags),
               FontSize = 12,
               Foreground = MutedForeground,
               TextWrapping = TextWrapping.WrapWholeWords
            });
         }
         ContentPanel.Children.Add(MakeCard(entityCard));

         // Columns card: one zebra-banded two-line row per column.
         var columnsCard = new StackPanel();
         columnsCard.Children.Add(MakeSectionTitle("Columns"));
         bool alternate = false;
         foreach (var column in table.Columns)
         {
            columnsCard.Children.Add(
               BuildReadOnlyColumnRow(column, alternate, enumerations));
            alternate = !alternate;
         }
         ContentPanel.Children.Add(MakeCard(columnsCard));

         // A read-only foreign-key card: every column's outgoing FK(s).
         var fkLines = new List<string>();
         foreach (var column in table.Columns.Where(c => c.IsForeignKey))
         {
            foreach (var constraint in column.Constraints ??
               Enumerable.Empty<ConstraintInfo>())
            {
               if (!constraint.IsForeignKey)
               {
                  continue;
               }
               string target = constraint.ReferencedTableName +
                  (constraint.ReferencedColumnName != null
                     ? "." + constraint.ReferencedColumnName : "");
               fkLines.Add(column.ColumnName + "  →  " + target);
            }
         }
         if (fkLines.Count > 0)
         {
            var fkCard = new StackPanel { Spacing = 2 };
            fkCard.Children.Add(MakeSectionTitle("Foreign keys"));
            foreach (var line in fkLines)
            {
               fkCard.Children.Add(new TextBlock
               {
                  Text = line,
                  FontSize = 12,
                  Margin = new Thickness(2, 0, 0, 0),
                  TextWrapping = TextWrapping.WrapWholeWords
               });
            }
            ContentPanel.Children.Add(MakeCard(fkCard));
         }

         // The read-only properties card (026 provenance + 022 metadata).
         var properties = BuildPropertiesCard(table);
         if (properties != null)
         {
            ContentPanel.Children.Add(properties);
         }
      }

      /// <summary>
      /// The read-only properties card shared by both modes (backlog 042):
      /// the entity's provenance (026) and metadata annotations (022), when
      /// the model carried any. Returns null when neither is present.
      /// </summary>
      private Border BuildPropertiesCard(TableInfo table)
      {
         var card = new StackPanel { Spacing = 2 };
         bool any = false;

         var provenanceText = ReadoutFormatter.Provenance(table.Provenance);
         if (provenanceText != null)
         {
            card.Children.Add(MakeSectionTitle("Properties"));
            card.Children.Add(new TextBlock
            {
               Text = provenanceText,
               FontSize = 11,
               Foreground = MutedForeground,
               Margin = new Thickness(2, 0, 0, 0),
               TextWrapping = TextWrapping.WrapWholeWords
            });
            any = true;
         }

         var metadataLines = ReadoutFormatter.MetadataLines(table.Metadata);
         if (metadataLines.Count > 0)
         {
            if (!any)
            {
               card.Children.Add(MakeSectionTitle("Properties"));
            }
            foreach (var line in metadataLines)
            {
               card.Children.Add(new TextBlock
               {
                  Text = line,
                  FontSize = 11,
                  Foreground = MutedForeground,
                  Margin = new Thickness(2, 0, 0, 0),
                  TextWrapping = TextWrapping.WrapWholeWords
               });
            }
            any = true;
         }

         return any ? MakeCard(card) : null;
      }

      /// <summary>
      /// Show a connector's FK relationship — read-only by default, or the
      /// target/cardinality/roles editors and delete action in edit mode
      /// (backlog 042). When the edge carries its source constraint (backlog
      /// 022), the dependency's per-side cardinality/optionality and role
      /// names are shown beneath (and edited in place in edit mode).
      /// </summary>
      public void ShowConnector(FkRelation edge, IReadOnlyList<TableInfo> tables)
      {
         _currentEdge = edge;
         _tables = tables;
         _currentTable = tables?.FirstOrDefault(t => t.TableName == edge.ChildTable);
         HeaderText.Text = "Foreign Key";
         ContentPanel.Children.Clear();

         if (_editMode)
         {
            BuildEditConnector(edge, tables);
         }
         else
         {
            BuildReadOnlyConnector(edge);
         }
         UpdateModeButton();
      }

      /// <summary>
      /// The 029 edit surface for a connector: target/cardinality/roles
      /// editors (gated by the dependency's verbs) and a delete action.
      /// Shown only in edit mode (backlog 042).
      /// </summary>
      private void BuildEditConnector(FkRelation edge, IReadOnlyList<TableInfo> tables)
      {
         // Relationship card: the dependency line + the live readout lines the
         // per-editor cards below update in place.
         var relationship = new StackPanel { Spacing = 4 };
         relationship.Children.Add(new TextBlock
         {
            Text = edge.ChildTable + "." + edge.ChildColumn +
                   "  →  " + edge.ParentTable + "." + edge.ParentColumn,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.WrapWholeWords
         });

         var constraint = edge.Constraint;
         var verbs = GraphNodes.Dependency(edge).Verbs;

         TextBlock cardinalityText = null;
         TextBlock rolesText = null;
         if (constraint != null)
         {
            string cardinality = ReadoutFormatter.Cardinality(constraint);
            if (cardinality != null)
            {
               cardinalityText = new TextBlock
               {
                  Text = "Cardinality: " + cardinality,
                  FontSize = 12,
                  Foreground = MutedForeground
               };
               relationship.Children.Add(cardinalityText);
            }

            string roles = ReadoutFormatter.Roles(constraint);
            if (roles != null)
            {
               rolesText = new TextBlock
               {
                  Text = "Roles: " + roles,
                  FontSize = 12,
                  Foreground = MutedForeground
               };
               relationship.Children.Add(rolesText);
            }
         }
         ContentPanel.Children.Add(MakeCard(relationship));

         // Each verb-gated editor gets its own card.
         if (constraint != null)
         {
            if (verbs.CanEditTarget)
            {
               ContentPanel.Children.Add(
                  MakeCard(BuildTargetEditor(edge, constraint)));
            }
            if (verbs.CanEditCardinality)
            {
               ContentPanel.Children.Add(
                  MakeCard(BuildCardinalityEditor(constraint, cardinalityText)));
            }
            if (verbs.CanEditRoles)
            {
               ContentPanel.Children.Add(
                  MakeCard(BuildRolesEditor(constraint, rolesText)));
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
      /// The read-only connector readout (backlog 042): the dependency line,
      /// then cardinality/roles when the edge carries its source constraint.
      /// No delete action — that lives behind the Edit button.
      /// </summary>
      private void BuildReadOnlyConnector(FkRelation edge)
      {
         var card = new StackPanel { Spacing = 4 };
         card.Children.Add(new TextBlock
         {
            Text = edge.ChildTable + "." + edge.ChildColumn +
                   "  →  " + edge.ParentTable + "." + edge.ParentColumn,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.WrapWholeWords
         });

         var constraint = edge.Constraint;
         if (constraint != null)
         {
            string cardinality = ReadoutFormatter.Cardinality(constraint);
            if (cardinality != null)
            {
               card.Children.Add(new TextBlock
               {
                  Text = "Cardinality: " + cardinality,
                  FontSize = 12,
                  Foreground = MutedForeground
               });
            }

            string roles = ReadoutFormatter.Roles(constraint);
            if (roles != null)
            {
               card.Children.Add(new TextBlock
               {
                  Text = "Roles: " + roles,
                  FontSize = 12,
                  Foreground = MutedForeground
               });
            }
         }
         ContentPanel.Children.Add(MakeCard(card));
      }

      /// <summary>
      /// The header's Edit/Done toggle (backlog 042): flips the view/edit
      /// mode and re-renders the current content. A table re-enters the 029
      /// edit surface (or the read-only readout); a connector likewise.
      /// </summary>
      private void ModeButton_Click(object sender, RoutedEventArgs e)
      {
         _editMode = !_editMode;
         if (_currentTable != null)
         {
            ShowTable(_currentTable, _tables, _enumerations);
         }
         else if (_currentEdge != null)
         {
            ShowConnector(_currentEdge, _tables);
         }
      }

      /// <summary>
      /// Keep the header button honest: "Edit" in read-only mode, "Done"
      /// while the edit surface is up. Shown only when an entity/connector is
      /// selected (the model-level readout has nothing to edit).
      /// </summary>
      private void UpdateModeButton()
      {
         ModeButton.Visibility = Visibility.Visible;
         ModeButton.Content = _editMode ? "Done" : "Edit";
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
         ModeButton.Visibility = Visibility.Collapsed;

         var card = new StackPanel { Spacing = 4 };

         string provenanceText = ReadoutFormatter.Provenance(provenance);
         if (provenanceText != null)
         {
            card.Children.Add(new TextBlock
            {
               Text = provenanceText,
               FontSize = 12,
               TextWrapping = TextWrapping.WrapWholeWords
            });
            if (!string.IsNullOrEmpty(provenance.Notes))
            {
               card.Children.Add(new TextBlock
               {
                  Text = "notes: " + provenance.Notes,
                  FontSize = 11,
                  Foreground = MutedForeground,
                  TextWrapping = TextWrapping.WrapWholeWords
               });
            }
         }

         var metadataLines = ReadoutFormatter.MetadataLines(metadata);
         if (metadataLines.Count > 0)
         {
            card.Children.Add(MakeSectionTitle("Model metadata"));
            foreach (var line in metadataLines)
            {
               card.Children.Add(new TextBlock
               {
                  Text = line,
                  FontSize = 11,
                  Foreground = MutedForeground,
                  Margin = new Thickness(2, 0, 0, 0)
               });
            }
         }

         if (provenanceText == null && metadataLines.Count == 0)
         {
            card.Children.Add(new TextBlock
            {
               Text = "No model-level metadata or provenance.",
               FontSize = 12,
               Foreground = MutedForeground
            });
         }
         ContentPanel.Children.Add(MakeCard(card));
      }

      /// <summary>
      /// The per-column edit sub-card (backlog 042): a name/remove head row,
      /// a type/size + PK row, and a make-FK row (when the column is not
      /// already a foreign key), plus the description/enum/provenance
      /// readouts. Each control is gated by the element's edit verbs (backlog
      /// 028). The sub-cards alternate zebra bands down the columns card.
      /// </summary>
      private UIElement BuildColumnSection(
         TableInfo table, ColumnInfo column, bool alternate,
         IReadOnlyDictionary<string, Enumeration> enumerations)
      {
         var verbs = GraphNodes.Element(table, column).Verbs;
         var panel = new StackPanel { Spacing = 4 };

         // Head row: name editor (or text) + remove.
         var head = new Grid { ColumnSpacing = 6 };
         head.ColumnDefinitions.Add(new ColumnDefinition
         {
            Width = new GridLength(1, GridUnitType.Star)
         });
         head.ColumnDefinitions.Add(new ColumnDefinition
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
               FontWeight = FontWeights.SemiBold,
               VerticalAlignment = VerticalAlignment.Center,
               TextTrimming = TextTrimming.CharacterEllipsis
            };
         }
         Grid.SetColumn(nameControl, 0);
         head.Children.Add(nameControl);

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
            Grid.SetColumn(removeButton, 1);
            head.Children.Add(removeButton);
         }
         panel.Children.Add(head);

         // Type/size + PK row.
         var flagsRow = new StackPanel
         {
            Orientation = Orientation.Horizontal,
            Spacing = 6
         };

         FrameworkElement typeControl;
         FrameworkElement sizeControl;
         if (verbs.CanEditType)
         {
            var typeBox = new TextBox
            {
               Text = column.Type,
               FontSize = 12,
               MinWidth = 100
            };
            var sizeBox = new TextBox
            {
               Text = column.Size > 0 ? column.Size.ToString() : "",
               FontSize = 12,
               Width = 56,
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
         flagsRow.Children.Add(typeControl);
         flagsRow.Children.Add(sizeControl);

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
            FontSize = 11,
            Foreground = MutedForeground,
            VerticalAlignment = VerticalAlignment.Center
         });
         panel.Children.Add(flagsRow);

         // Make-FK row: pick a parent table + column and add the constraint.
         if (verbs.CanAddForeignKey && !column.IsForeignKey)
         {
            panel.Children.Add(BuildAddForeignKeyRow(table, column));
         }

         // Read-only description / enum / provenance readouts.
         var description = BuildDescriptionReadout(column);
         if (description != null)
         {
            panel.Children.Add(description);
         }
         var enumReadout = BuildEnumReadout(column, enumerations);
         if (enumReadout != null)
         {
            panel.Children.Add(enumReadout);
         }
         var provenanceReadout = BuildProvenanceReadout(column);
         if (provenanceReadout != null)
         {
            panel.Children.Add(provenanceReadout);
         }

         return MakeBand(alternate, panel);
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
            Margin = new Thickness(12, 0, 0, 0),
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
            Margin = new Thickness(12, 0, 0, 0),
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
            Margin = new Thickness(12, 0, 0, 0),
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
      /// Commit the comma-separated tags text through
      /// <see cref="ModelEdits.SetTableTags"/> (normalization + identifier
      /// validation) and ask for a re-render when the set changed. Rejected
      /// names surface on the diagnostics log — never a crash — and the box is
      /// rewritten to the applied (normalized) list.
      /// </summary>
      private void CommitTags(TableInfo table, TextBox box)
      {
         var parts = (box.Text ?? "").Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();

         bool unchanged =
            table.Tags != null && table.Tags.Count == parts.Length &&
            table.Tags.SequenceEqual(parts, StringComparer.OrdinalIgnoreCase);
         if (unchanged)
         {
            return;
         }

         var applied = ModelEdits.SetTableTags(table, parts, out var rejected);
         if (rejected.Count > 0)
         {
            m_Log?.WriteMessage("table '" + table.TableName + "': tag(s) '" +
               string.Join("', '", rejected) +
               "' rejected — use letters, digits, '_' or '-', no leading digit.");
         }

         box.Text = string.Join(", ", applied);
         ModelEdited?.Invoke(this, EventArgs.Empty);
      }

      /// <summary>
      /// Sync the Show/Hide pin toggles to a (possibly mutated) shared
      /// visibility (backlog 038). Called by the host after every visibility
      /// change so the buttons reflect the current pin state of the shown
      /// table.
      /// </summary>
      public void SetVisibility(EntityVisibility visibility)
      {
         _visibility = visibility;
         if (_showPinButton == null || _hidePinButton == null ||
             _currentTable == null)
         {
            return;
         }
         bool? pin = visibility?.PinState(_currentTable.TableName);
         _syncingPins = true;
         _showPinButton.IsChecked = pin == true;
         _hidePinButton.IsChecked = pin == false;
         _syncingPins = false;
      }

      /// <summary>
      /// Raise a pin change for the shown table (backlog 038): true =
      /// pinned-show, false = pinned-hide, null = unpinned. The host applies
      /// it to the shared visibility and re-renders.
      /// </summary>
      private void CommitPin(TableInfo table, bool? pinned)
      {
         if (_syncingPins)
         {
            return;
         }
         VisibilityPinChanged?.Invoke(this, (table.TableName, pinned));
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
