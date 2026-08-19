using System.Collections.Generic;
using System.Linq;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// Builds the hover readout lines for a graphic entity (backlog 027).
   /// Portable and deterministic (pure net10.0, like <see cref="ReadoutFormatter"/>)
   /// so the XAML tooltip and the Skia/WASM sibling share one source of text —
   /// the hover always reads the live model object, never a frozen projection.
   /// </summary>
   public static class HoverSummary
   {

      /// <summary>
      /// Hover readout for a table: the "schema::table" header, the entity's
      /// description (when it has one), its column count, its PK/FK counts
      /// (when any), and its provenance (when present). Empty when the table
      /// is null.
      /// </summary>
      public static IReadOnlyList<string> ForTable(TableInfo table)
      {
         var lines = new List<string>();
         if (table == null) return lines;

         lines.Add(table.SchemaName + "::" + table.TableName);

         if (!string.IsNullOrEmpty(table.Description))
         {
            lines.Add(table.Description);
         }

         int columns = table.Columns?.Count ?? 0;
         lines.Add(columns + " columns");

         if (columns > 0)
         {
            int pk = table.Columns.Count(c => c.IsKey);
            int fk = table.Columns.Count(c => c.IsForeignKey);
            if (pk > 0 || fk > 0)
            {
               lines.Add("PK: " + pk + ", FK: " + fk);
            }
         }

         string provenance = ReadoutFormatter.Provenance(table.Provenance);
         if (provenance != null)
         {
            lines.Add(provenance);
         }

         return lines;
      }

      /// <summary>
      /// Hover readout for an FK connector: the "child.col → parent.col"
      /// arrow, plus the dependency's cardinality and role names from its
      /// source constraint (when the edge carries one). Empty when the edge
      /// is null.
      /// </summary>
      public static IReadOnlyList<string> ForConnector(FkRelation edge)
      {
         var lines = new List<string>();
         if (edge == null) return lines;

         lines.Add(edge.ChildTable + "." + edge.ChildColumn +
            "  →  " + edge.ParentTable + "." + edge.ParentColumn);

         var constraint = edge.Constraint;
         if (constraint != null)
         {
            string cardinality = ReadoutFormatter.Cardinality(constraint);
            if (cardinality != null)
            {
               lines.Add("Cardinality: " + cardinality);
            }
            string roles = ReadoutFormatter.Roles(constraint);
            if (roles != null)
            {
               lines.Add("Roles: " + roles);
            }
         }

         return lines;
      }

      /// <summary>
      /// Dispatch a canvas payload to its hover readout: a <see cref="TableInfo"/>
      /// (what a table renders) or an <see cref="FkRelation"/> (what a connector
      /// carries). Anything else produces no readout.
      /// </summary>
      public static IReadOnlyList<string> For(object payload)
      {
         if (payload is TableInfo table) return ForTable(table);
         if (payload is FkRelation edge) return ForConnector(edge);
         return new List<string>();
      }

   }

}
