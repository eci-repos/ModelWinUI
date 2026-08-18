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
using ModelConsole.Graph;

namespace ModelConsole.Controls
{
   /// <summary>
   /// Entity inspector: shows the metadata of the clicked graphic entity.
   /// For a table it lists the columns (data type editable - committing
   /// re-renders the drawing); for a connector it shows the FK relationship
   /// and offers a delete action that regenerates the remaining routes.
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

      public EntityInspectorControl()
      {
         this.InitializeComponent();
      }

      /// <summary>
      /// Show a table's metadata: schema::table header plus one row per
      /// column (name, editable data type, constraints). A column that
      /// resolves to an enumeration (backlog 021) gets a read-only value-set
      /// line beneath it when the model's enumerations are supplied.
      /// </summary>
      public void ShowTable(
         TableInfo table,
         IReadOnlyDictionary<string, Enumeration> enumerations = null)
      {
         HeaderText.Text = table.SchemaName + "::" + table.TableName;
         ContentPanel.Children.Clear();

         ContentPanel.Children.Add(new TextBlock
         {
            Text = "Column / Type / Constraints",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4)
         });

         foreach (var column in table.Columns)
         {
            ContentPanel.Children.Add(BuildColumnRow(column));
            var enumReadout = BuildEnumReadout(column, enumerations);
            if (enumReadout != null)
            {
               ContentPanel.Children.Add(enumReadout);
            }
         }

         // Backlog 022: the entity's metadata annotations, when the model
         // carried any.
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
      /// Show a connector's FK relationship with a delete action. When the
      /// edge carries its source constraint (backlog 022), the dependency's
      /// per-side cardinality/optionality and role names are shown beneath.
      /// </summary>
      public void ShowConnector(FkRelation edge)
      {
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
         if (constraint != null)
         {
            string cardinality = ReadoutFormatter.Cardinality(constraint);
            if (cardinality != null)
            {
               ContentPanel.Children.Add(new TextBlock
               {
                  Text = "Cardinality: " + cardinality,
                  FontSize = 12,
                  Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                  Margin = new Thickness(0, 4, 0, 0)
               });
            }
            string roles = ReadoutFormatter.Roles(constraint);
            if (roles != null)
            {
               ContentPanel.Children.Add(new TextBlock
               {
                  Text = "Roles: " + roles,
                  FontSize = 12,
                  Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                  Margin = new Thickness(0, 2, 0, 0)
               });
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

      private UIElement BuildColumnRow(ColumnInfo column)
      {
         var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
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

         var nameText = new TextBlock
         {
            Text = column.ColumnName,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
         };

         var typeBox = new TextBox
         {
            Text = column.Type,
            FontSize = 12,
            Width = 90,
            VerticalAlignment = VerticalAlignment.Center
         };
         typeBox.KeyDown += (s, e) =>
         {
            if (e.Key == VirtualKey.Enter)
            {
               CommitType(column, typeBox);
               e.Handled = true;
            }
         };
         typeBox.LostFocus += (s, e) => CommitType(column, typeBox);

         var constraintText = new TextBlock
         {
            Text = GetConstraintText(column),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
         };

         Grid.SetColumn(nameText, 0);
         Grid.SetColumn(typeBox, 1);
         Grid.SetColumn(constraintText, 2);
         row.Children.Add(nameText);
         row.Children.Add(typeBox);
         row.Children.Add(constraintText);

         return row;
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
      /// Commit an edited data type to the shared POCO and ask for a
      /// re-render when it actually changed.
      /// </summary>
      private void CommitType(ColumnInfo column, TextBox box)
      {
         if (!string.Equals(column.Type, box.Text, StringComparison.Ordinal))
         {
            column.Type = box.Text;
            ModelEdited?.Invoke(this, EventArgs.Empty);
         }
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
