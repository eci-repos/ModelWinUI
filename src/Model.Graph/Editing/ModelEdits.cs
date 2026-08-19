using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;

namespace ModelConsole.Editing
{

   /// <summary>
   /// Pure, atomic edit operations over the canonical model (backlog 029).
   /// Every operation mutates the live POCOs in place and preserves the
   /// observed invariants: a column's <see cref="ColumnInfo.IsKey"/> /
   /// <see cref="ColumnInfo.IsForeignKey"/> flags are recomputed from its
   /// <see cref="ColumnInfo.Constraints"/> after a constraint add/remove,
   /// and a rename cascades across every referencing FK so an innocent
   /// rename never dangles a connector. The operations are UI-free — the
   /// inspector calls them and the host re-renders; referential validation
   /// is <see cref="FkEdgeExtractor"/>'s job (a broken edit surfaces as an
   /// issue, never a crash).
   /// </summary>
   public static class ModelEdits
   {

      /// <summary>
      /// Rename a table and cascade the rename to every FK constraint that
      /// references it (its <see cref="ConstraintInfo.ReferencedTableName"/>
      /// follows the new name), so inbound connectors stay attached. The
      /// renamed table's own constraints also carry the new table name.
      /// </summary>
      public static void RenameTable(
         IReadOnlyList<TableInfo> tables, TableInfo table, string newName)
      {
         string oldName = table.TableName;
         if (String.Equals(oldName, newName, StringComparison.Ordinal))
         {
            return;
         }

         table.TableName = newName;

         foreach (var t in tables)
         {
            if (t == null || t.Columns == null)
            {
               continue;
            }
            foreach (var column in t.Columns)
            {
               if (column == null || column.Constraints == null)
               {
                  continue;
               }
               foreach (var constraint in column.Constraints)
               {
                  if (constraint == null)
                  {
                     continue;
                  }
                  if (constraint.IsForeignKey &&
                      String.Equals(constraint.ReferencedTableName, oldName,
                         StringComparison.Ordinal))
                  {
                     constraint.ReferencedTableName = newName;
                  }
                  if (ReferenceEquals(t, table) &&
                      String.Equals(constraint.TableName, oldName,
                         StringComparison.Ordinal))
                  {
                     constraint.TableName = newName;
                  }
               }
            }
         }
      }

      /// <summary>
      /// Rename a column and cascade the rename to every FK constraint that
      /// references it (its <see cref="ConstraintInfo.ReferencedColumnName"/>
      /// follows the new name), so inbound connectors stay attached. The
      /// renamed column's own constraints also carry the new column name.
      /// </summary>
      public static void RenameColumn(
         IReadOnlyList<TableInfo> tables, TableInfo table,
         ColumnInfo column, string newName)
      {
         string oldName = column.ColumnName;
         if (String.Equals(oldName, newName, StringComparison.Ordinal))
         {
            return;
         }

         column.ColumnName = newName;

         foreach (var t in tables)
         {
            if (t == null || t.Columns == null)
            {
               continue;
            }
            foreach (var c in t.Columns)
            {
               if (c == null || c.Constraints == null)
               {
                  continue;
               }
               foreach (var constraint in c.Constraints)
               {
                  if (constraint == null)
                  {
                     continue;
                  }
                  if (constraint.IsForeignKey &&
                      String.Equals(constraint.ReferencedTableName,
                         table.TableName, StringComparison.Ordinal) &&
                      String.Equals(constraint.ReferencedColumnName, oldName,
                         StringComparison.Ordinal))
                  {
                     constraint.ReferencedColumnName = newName;
                  }
                  if (ReferenceEquals(c, column) &&
                      String.Equals(constraint.ColumnName, oldName,
                         StringComparison.Ordinal))
                  {
                     constraint.ColumnName = newName;
                  }
               }
            }
         }
      }

      /// <summary>
      /// Append a column to a table, assigning its ordinal position.
      /// </summary>
      public static void AddColumn(TableInfo table, ColumnInfo column)
      {
         if (table.Columns == null)
         {
            table.Columns = new List<ColumnInfo>();
         }
         column.OrdinalPosition = table.Columns.Count;
         table.Columns.Add(column);
      }

      /// <summary>
      /// Remove a column from its table. Referential validation is left to
      /// <see cref="FkEdgeExtractor"/> — a removed referenced column surfaces
      /// as an issue on the next render, never a crash.
      /// </summary>
      public static void RemoveColumn(TableInfo table, ColumnInfo column)
      {
         table.Columns?.Remove(column);
      }

      /// <summary>
      /// Set a column's data type and size.
      /// </summary>
      public static void SetColumnType(ColumnInfo column, string type, int size)
      {
         column.Type = type;
         column.Size = size;
      }

      /// <summary>
      /// Mark a column as (or unmark it from being) the table's key: add or
      /// remove a <c>PK</c> constraint and recompute the key flags.
      /// </summary>
      public static void SetKey(TableInfo table, ColumnInfo column, bool isKey)
      {
         if (isKey)
         {
            if (!column.IsKey)
            {
               column.Add(new ConstraintInfo
               {
                  SchemaName = table.SchemaName,
                  TableName = table.TableName,
                  ColumnName = column.ColumnName,
                  Type = DataInfo.PRIMARY_KEY
               });
            }
         }
         else
         {
            column.Constraints?.RemoveAll(k => k != null && k.IsKey);
         }
         RecomputeKeyFlags(column);
      }

      /// <summary>Set an entity's description.</summary>
      public static void SetDescription(TableInfo table, string description)
      {
         table.Description = description;
      }

      /// <summary>Set a column's description.</summary>
      public static void SetDescription(ColumnInfo column, string description)
      {
         column.Description = description;
      }

      /// <summary>
      /// Add a foreign-key constraint to a column. A blank parent column
      /// resolves to the parent's primary key (the extractor's default).
      /// </summary>
      public static void AddForeignKey(
         TableInfo table, ColumnInfo column,
         string parentTable, string parentColumn,
         int? min, int? max, string childRole, string parentRole)
      {
         column.Add(new ConstraintInfo
         {
            SchemaName = table.SchemaName,
            TableName = table.TableName,
            ColumnName = column.ColumnName,
            Type = DataInfo.FOREIGN_KEY,
            ReferencedTableName = parentTable,
            ReferencedColumnName = String.IsNullOrWhiteSpace(parentColumn)
               ? null : parentColumn,
            MinCardinality = min,
            MaxCardinality = max,
            ChildRole = childRole,
            ParentRole = parentRole
         });
         RecomputeKeyFlags(column);
      }

      /// <summary>
      /// Retarget a foreign-key constraint. A blank parent column resolves to
      /// the parent's primary key (the extractor's default).
      /// </summary>
      public static void EditForeignKeyTarget(
         ConstraintInfo constraint, string parentTable, string parentColumn)
      {
         constraint.ReferencedTableName = parentTable;
         constraint.ReferencedColumnName = String.IsNullOrWhiteSpace(parentColumn)
            ? null : parentColumn;
      }

      /// <summary>Set a dependency's per-side cardinality bounds.</summary>
      public static void SetCardinality(
         ConstraintInfo constraint, int? min, int? max)
      {
         constraint.MinCardinality = min;
         constraint.MaxCardinality = max;
      }

      /// <summary>Set a dependency's per-side role names.</summary>
      public static void SetRoles(
         ConstraintInfo constraint, string childRole, string parentRole)
      {
         constraint.ChildRole = childRole;
         constraint.ParentRole = parentRole;
      }

      /// <summary>
      /// Remove a foreign-key constraint from its owning column and recompute
      /// the column's key flags.
      /// </summary>
      public static void RemoveForeignKey(TableInfo table, ConstraintInfo constraint)
      {
         var column = table.Columns?.FirstOrDefault(c =>
            c != null && c.Constraints != null && c.Constraints.Contains(constraint));
         if (column != null)
         {
            column.Constraints.Remove(constraint);
            RecomputeKeyFlags(column);
         }
      }

      /// <summary>Remove a table from the model's table list.</summary>
      public static void RemoveTable(IList<TableInfo> tables, TableInfo table)
      {
         tables.Remove(table);
      }

      /// <summary>
      /// Recompute a column's key flags from its constraints, so the flags can
      /// never drift from the constraint list (the observed invariant).
      /// </summary>
      private static void RecomputeKeyFlags(ColumnInfo column)
      {
         column.IsKey = column.Constraints != null &&
            column.Constraints.Any(k => k != null && k.IsKey);
         column.IsForeignKey = column.Constraints != null &&
            column.Constraints.Any(k => k != null && k.IsForeignKey);
      }

   }

}
