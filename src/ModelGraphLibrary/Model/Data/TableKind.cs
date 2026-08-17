using System;
using System.Linq;

namespace Model.Data
{

   /// <summary>
   /// The visual kind of a table, used to color its header. Entity tables are
   /// the "top items" (people, incidents, cases...); reference-code tables are
   /// small lookups holding a code/ID key plus a description.
   /// </summary>
   public enum TableKind
   {
      Entity,
      ReferenceCode
   }

   /// <summary>
   /// Classifies a <see cref="TableInfo"/> as an entity or a reference-code
   /// table. Pure logic over the metadata, so it lives in the portable library
   /// and is unit-testable.
   /// </summary>
   public static class TableKindClassifier
   {

      /// <summary>
      /// A table is a reference-code table when it is a small lookup: a
      /// code/ID key plus a description column, and little else. The Ref*
      /// name prefix is the strongest signal in the sample schema; the shape
      /// check (description + code/ID key + few columns) generalizes to other
      /// schemas.
      /// </summary>
      /// <param name="table">table metadata to classify</param>
      /// <returns>the table kind</returns>
      public static TableKind Classify(TableInfo table)
      {
         if (table == null)
         {
            return TableKind.Entity;
         }

         if (table.TableName != null &&
            table.TableName.StartsWith("Ref", StringComparison.OrdinalIgnoreCase))
         {
            return TableKind.ReferenceCode;
         }

         bool hasDescription = table.Columns != null && table.Columns.Any(c =>
            c.ColumnName != null &&
            c.ColumnName.IndexOf("Description", StringComparison.OrdinalIgnoreCase) >= 0);
         bool hasCodeKey = table.Columns != null && table.Columns.Any(c =>
            c.IsKey && c.ColumnName != null &&
            (c.ColumnName.EndsWith("Code", StringComparison.OrdinalIgnoreCase) ||
             c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase)));

         return hasDescription && hasCodeKey && table.Columns.Count <= 3
            ? TableKind.ReferenceCode
            : TableKind.Entity;
      }

   }

}
