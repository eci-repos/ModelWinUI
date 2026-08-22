using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// The derived UML profile for the canonical ERD model (backlog 040).
   /// Nothing here is persisted: classes, attributes, associations,
   /// packages, stereotypes, multiplicity, and roles are read from the
   /// existing model at render/export time.
   /// </summary>
   public static class UmlProfile
   {
      /// <summary>Return the UML class name for an entity.</summary>
      public static string ClassName(TableInfo table)
      {
         if (table == null)
         {
            return "";
         }
         if (!string.IsNullOrEmpty(table.SchemaName))
         {
            return table.SchemaName + "::" + table.TableName;
         }
         return table.TableName ?? "";
      }

      /// <summary>Return the primary stereotype derived from table kind.</summary>
      public static string Stereotype(TableInfo table)
      {
         return TableKindClassifier.Classify(table) == TableKind.ReferenceCode
            ? "reference"
            : "entity";
      }

      /// <summary>Return the class banner text used by the UML renderers.</summary>
      public static string ClassBanner(TableInfo table)
      {
         return ClassName(table) + " <<" + Stereotype(table) + ">>";
      }

      /// <summary>Return an attribute line in UML-ish notation.</summary>
      public static string Attribute(ColumnInfo column)
      {
         if (column == null)
         {
            return "";
         }

         var marks = new List<string>();
         if (column.IsKey) marks.Add("PK");
         if (column.IsForeignKey) marks.Add("FK");

         string suffix = marks.Count == 0
            ? ""
            : " {" + string.Join(", ", marks) + "}";
         return "+ " + column.ColumnName + ": " + DataType(column) + suffix;
      }

      /// <summary>Return the column type with its optional size.</summary>
      public static string DataType(ColumnInfo column)
      {
         if (column == null)
         {
            return "";
         }
         return column.Type + (column.Size > 0 ? "(" + column.Size + ")" : "");
      }

      /// <summary>
      /// Format a UML multiplicity from the model cardinality fields.
      /// </summary>
      public static string Multiplicity(int? min, int? max)
      {
         if (min == null && max == null)
         {
            return null;
         }

         int lower = min ?? 0;
         if (max != null && lower == max.Value)
         {
            return lower.ToString();
         }

         return lower + ".." + (max == null ? "*" : max.ToString());
      }

      /// <summary>
      /// Return the child-side multiplicity carried by an FK constraint.
      /// </summary>
      public static string ChildMultiplicity(FkRelation edge)
      {
         return edge == null
            ? null
            : Multiplicity(edge.Constraint?.MinCardinality, edge.Constraint?.MaxCardinality);
      }

      /// <summary>
      /// Text label for a UML association in the on-canvas renderers.
      /// </summary>
      public static string AssociationLabel(FkRelation edge)
      {
         if (edge == null)
         {
            return "";
         }

         var parts = new List<string>();
         string child = ChildMultiplicity(edge);
         if (!string.IsNullOrEmpty(edge.Constraint?.ChildRole))
         {
            parts.Add(edge.Constraint.ChildRole);
         }
         if (!string.IsNullOrEmpty(child))
         {
            parts.Add(child);
         }
         if (!string.IsNullOrEmpty(edge.Constraint?.ParentRole))
         {
            parts.Add(edge.Constraint.ParentRole);
         }
         return parts.Count == 0
            ? edge.ChildColumn + " -> " + edge.ParentColumn
            : string.Join(" ", parts);
      }
   }

   /// <summary>
   /// Deterministic PlantUML export for the derived UML profile.
   /// </summary>
   public static class UmlPlantEmitter
   {
      /// <summary>Emit a class diagram for the whole model.</summary>
      public static string EmitClassDiagram(IReadOnlyList<TableInfo> tables)
      {
         tables = tables ?? new List<TableInfo>();
         var aliases = AliasMap(tables);
         var (edges, _) = FkEdgeExtractor.Extract(tables);
         var sb = new StringBuilder();

         sb.AppendLine("@startuml");
         sb.AppendLine("hide circle");
         sb.AppendLine("skinparam classAttributeIconSize 0");

         foreach (var table in tables.Where(t => t != null)
            .OrderBy(t => UmlProfile.ClassName(t), StringComparer.Ordinal))
         {
            sb.Append("class \"")
               .Append(Escape(UmlProfile.ClassName(table)))
               .Append("\" as ")
               .Append(aliases[table.TableName])
               .Append(" <<")
               .Append(UmlProfile.Stereotype(table))
               .AppendLine(">> {");

            foreach (var column in (table.Columns ?? new List<ColumnInfo>())
               .OrderBy(c => c.OrdinalPosition)
               .ThenBy(c => c.ColumnName, StringComparer.Ordinal))
            {
               sb.Append("  ")
                  .AppendLine(Escape(UmlProfile.Attribute(column)));
            }
            sb.AppendLine("}");
         }

         foreach (var edge in edges
            .OrderBy(e => e.ChildTable, StringComparer.Ordinal)
            .ThenBy(e => e.ChildColumn, StringComparer.Ordinal)
            .ThenBy(e => e.ParentTable, StringComparer.Ordinal)
            .ThenBy(e => e.ParentColumn, StringComparer.Ordinal))
         {
            if (!aliases.ContainsKey(edge.ChildTable) ||
                !aliases.ContainsKey(edge.ParentTable))
            {
               continue;
            }

            sb.Append(aliases[edge.ChildTable]);
            string childMultiplicity = UmlProfile.ChildMultiplicity(edge);
            if (!string.IsNullOrEmpty(childMultiplicity))
            {
               sb.Append(" \"").Append(Escape(childMultiplicity)).Append("\"");
            }
            sb.Append(" --> ");
            sb.Append("\"1\" ");
            sb.Append(aliases[edge.ParentTable]);

            var label = AssociationLabel(edge);
            if (!string.IsNullOrEmpty(label))
            {
               sb.Append(" : ").Append(Escape(label));
            }
            sb.AppendLine();
         }

         sb.AppendLine("@enduml");
         return sb.ToString();
      }

      /// <summary>
      /// Emit a package diagram using the same grouping theme used by the
      /// renderer. Expanded groups contain classes; collapsed groups are
      /// emitted as package nodes with aggregated inter-package edges.
      /// </summary>
      public static string EmitPackageDiagram(
         IReadOnlyList<TableInfo> tables,
         GroupingTheme theme = null,
         EntityVisibility visibility = null,
         GroupCollapseState collapse = null)
      {
         tables = tables ?? new List<TableInfo>();
         theme = theme ?? GroupingThemes.Tags;

         var (allEdges, _) = FkEdgeExtractor.Extract(tables);
         var (visibleTables, visibleEdges) =
            ModelProjection.Project(tables, allEdges, visibility);
         var collapsed = collapse == null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(collapse.CollapsedGroups, StringComparer.Ordinal);

         var aliases = AliasMap(visibleTables);
         var groups = theme.Groups(visibleTables).ToList();
         var boxed = new HashSet<string>(StringComparer.Ordinal);
         var boxes = new List<(string Key, GroupBox Box)>();
         foreach (var group in collapsed.OrderBy(g => g, StringComparer.Ordinal))
         {
            var box = GroupBoxAggregation.Build(
               visibleTables, visibleEdges, group, collapsed, theme);
            if (box.MemberCount == 0)
            {
               continue;
            }
            boxes.Add((GroupBoxAggregation.BoxKey(group), box));
            foreach (var member in box.Members)
            {
               boxed.Add(member.TableName);
            }
         }

         var groupAliases = GroupAliasMap(groups.Concat(boxes.Select(b => b.Box.Group)));
         var sb = new StringBuilder();
         sb.AppendLine("@startuml");
         sb.AppendLine("hide circle");

         foreach (var group in groups)
         {
            if (collapsed.Contains(group))
            {
               var box = boxes.FirstOrDefault(b => b.Box.Group == group).Box;
               if (box == null)
               {
                  continue;
               }
               sb.Append("package \"")
                  .Append(Escape(group + " (" + box.MemberCount + ")"))
                  .Append("\" as ")
                  .Append(groupAliases[group])
                  .AppendLine(" <<package>>");
               continue;
            }

            var members = visibleTables
               .Where(t => !boxed.Contains(t.TableName) &&
                           string.Equals(theme.PrimaryGroupOf(t), group, StringComparison.Ordinal))
               .OrderBy(t => UmlProfile.ClassName(t), StringComparer.Ordinal)
               .ToList();
            if (members.Count == 0)
            {
               continue;
            }
            sb.Append("package \"")
               .Append(Escape(group))
               .AppendLine("\" {");
            foreach (var table in members)
            {
               EmitClassStub(sb, table, aliases);
            }
            sb.AppendLine("}");
         }

         foreach (var table in visibleTables
            .Where(t => !boxed.Contains(t.TableName) &&
                        string.IsNullOrEmpty(theme.PrimaryGroupOf(t)))
            .OrderBy(t => UmlProfile.ClassName(t), StringComparer.Ordinal))
         {
            EmitClassStub(sb, table, aliases);
         }

         foreach (var edge in visibleEdges
            .Where(e => !boxed.Contains(e.ChildTable) && !boxed.Contains(e.ParentTable))
            .OrderBy(e => e.ChildTable, StringComparer.Ordinal)
            .ThenBy(e => e.ParentTable, StringComparer.Ordinal)
            .ThenBy(e => e.ChildColumn, StringComparer.Ordinal))
         {
            if (aliases.ContainsKey(edge.ChildTable) && aliases.ContainsKey(edge.ParentTable))
            {
               sb.Append(aliases[edge.ChildTable])
                  .Append(" --> ")
                  .Append(aliases[edge.ParentTable])
                  .AppendLine();
            }
         }

         foreach (var (_, box) in boxes.OrderBy(b => b.Box.Group, StringComparer.Ordinal))
         {
            foreach (var edge in box.ExternalEdges)
            {
               string from = groupAliases[box.Group];
               string target = edge.TargetGroup != null
                  ? groupAliases[edge.TargetGroup]
                  : aliases.TryGetValue(edge.TargetTable, out var a) ? a : null;
               if (target == null)
               {
                  continue;
               }
               sb.Append(edge.Outbound ? from : target)
                  .Append(" --> ")
                  .Append(edge.Outbound ? target : from);
               if (edge.Count > 1)
               {
                  sb.Append(" : x").Append(edge.Count);
               }
               sb.AppendLine();
            }
         }

         sb.AppendLine("@enduml");
         return sb.ToString();
      }

      private static void EmitClassStub(
         StringBuilder sb, TableInfo table, IReadOnlyDictionary<string, string> aliases)
      {
         sb.Append("class \"")
            .Append(Escape(UmlProfile.ClassName(table)))
            .Append("\" as ")
            .Append(aliases[table.TableName])
            .Append(" <<")
            .Append(UmlProfile.Stereotype(table))
            .AppendLine(">>");
      }

      private static string AssociationLabel(FkRelation edge)
      {
         var parts = new List<string>();
         if (!string.IsNullOrEmpty(edge.Constraint?.ChildRole))
         {
            parts.Add(edge.Constraint.ChildRole);
         }
         if (!string.IsNullOrEmpty(edge.Constraint?.ParentRole))
         {
            parts.Add(edge.Constraint.ParentRole);
         }
         return string.Join(" / ", parts);
      }

      private static Dictionary<string, string> AliasMap(IEnumerable<TableInfo> tables)
      {
         var result = new Dictionary<string, string>(StringComparer.Ordinal);
         var used = new HashSet<string>(StringComparer.Ordinal);
         foreach (var table in tables.Where(t => t != null)
            .OrderBy(t => UmlProfile.ClassName(t), StringComparer.Ordinal))
         {
            string baseAlias = "C_" + Sanitize(UmlProfile.ClassName(table));
            string alias = baseAlias;
            int i = 2;
            while (!used.Add(alias))
            {
               alias = baseAlias + "_" + i++;
            }
            result[table.TableName] = alias;
         }
         return result;
      }

      private static Dictionary<string, string> GroupAliasMap(IEnumerable<string> groups)
      {
         var result = new Dictionary<string, string>(StringComparer.Ordinal);
         var used = new HashSet<string>(StringComparer.Ordinal);
         foreach (var group in groups.Where(g => !string.IsNullOrEmpty(g))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(g => g, StringComparer.Ordinal))
         {
            string baseAlias = "P_" + Sanitize(group);
            string alias = baseAlias;
            int i = 2;
            while (!used.Add(alias))
            {
               alias = baseAlias + "_" + i++;
            }
            result[group] = alias;
         }
         return result;
      }

      private static string Sanitize(string value)
      {
         var sb = new StringBuilder();
         foreach (char ch in value ?? "")
         {
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
         }
         return sb.Length == 0 ? "Unnamed" : sb.ToString();
      }

      private static string Escape(string value)
      {
         return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
      }
   }

}
