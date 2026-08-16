using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// Derives FK edges from a set of tables. Resolves the referenced column
   /// using <see cref="ConstraintInfo.ReferencedColumnName"/> or, when null,
   /// the parent's primary key. Unresolvable constraints are reported as
   /// issues and skipped. Order is deterministic: input table order, then
   /// column order, then constraint order.
   /// </summary>
   public static class FkEdgeExtractor
   {

      /// <summary>
      /// Extract all FK edges from the given tables.
      /// </summary>
      /// <param name="tables">tables to scan</param>
      /// <returns>the resolved edges plus any issues found while resolving
      /// </returns>
      public static (List<FkRelation> Edges, List<string> Issues) Extract(
         IReadOnlyList<TableInfo> tables)
      {
         var edges = new List<FkRelation>();
         var issues = new List<string>();

         if (tables == null)
         {
            return (edges, issues);
         }

         var byName = tables
            .Where(t => t != null)
            .ToDictionary(t => t.TableName, t => t);

         foreach (var table in tables)
         {
            if (table == null || table.Columns == null)
            {
               continue;
            }

            foreach (var column in table.Columns)
            {
               if (column == null || column.Constraints == null)
               {
                  continue;
               }

               foreach (var constraint in column.Constraints)
               {
                  if (constraint == null || !constraint.IsForeignKey)
                  {
                     continue;
                  }

                  string parentName = constraint.ReferencedTableName;
                  if (String.IsNullOrWhiteSpace(parentName))
                  {
                     issues.Add(
                        "FK " + table.TableName + "." + column.ColumnName +
                        " has no referenced table.");
                     continue;
                  }

                  if (!byName.TryGetValue(parentName, out TableInfo parent))
                  {
                     issues.Add(
                        "FK " + table.TableName + "." + column.ColumnName +
                        " references missing table '" + parentName + "'.");
                     continue;
                  }

                  string parentColumn = constraint.ReferencedColumnName;
                  if (String.IsNullOrWhiteSpace(parentColumn))
                  {
                     parentColumn = FindPrimaryKeyColumn(parent);
                     if (parentColumn == null)
                     {
                        issues.Add(
                           "FK " + table.TableName + "." + column.ColumnName +
                           " references '" + parentName + "' which has no primary key.");
                        continue;
                     }
                  }
                  else if (!HasColumn(parent, parentColumn))
                  {
                     issues.Add(
                        "FK " + table.TableName + "." + column.ColumnName +
                        " references missing column '" + parentName + "." + parentColumn + "'.");
                     continue;
                  }

                  edges.Add(new FkRelation(
                     table.TableName, column.ColumnName, parentName, parentColumn));
               }
            }
         }

         return (edges, issues);
      }

      /// <summary>
      /// First column of the table marked as a key (or with a PK constraint),
      /// or null when the table has no primary key.
      /// </summary>
      private static string FindPrimaryKeyColumn(TableInfo table)
      {
         if (table.Columns == null)
         {
            return null;
         }

         foreach (var c in table.Columns)
         {
            if (c == null)
            {
               continue;
            }
            if (c.IsKey)
            {
               return c.ColumnName;
            }
            if (c.Constraints != null && c.Constraints.Any(k =>
               k != null && k.IsKey))
            {
               return c.ColumnName;
            }
         }
         return null;
      }

      /// <summary>
      /// True when the table has a column with the given name.
      /// </summary>
      private static bool HasColumn(TableInfo table, string columnName)
      {
         return table.Columns != null &&
            table.Columns.Any(c => c != null &&
               String.Equals(c.ColumnName, columnName, StringComparison.Ordinal));
      }

   }

}
