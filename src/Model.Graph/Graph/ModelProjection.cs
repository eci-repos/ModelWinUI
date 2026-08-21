using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// Project a full model + its extracted FK edges onto the visible subset
   /// (backlog 038). Both renderers feed the same projection, so they agree
   /// on the visible set by construction (parity, backlog 003).
   /// <para>This runs AFTER <see cref="FkEdgeExtractor.Extract"/> on the
   /// full table set, so unresolved-FK diagnostics still surface for edges
   /// whose endpoints are hidden — hiding a group never silences an R8 issue.</para>
   /// </summary>
   public static class ModelProjection
   {

      /// <summary>
      /// Compute the visible tables and the edges that draw between them.
      /// </summary>
      /// <param name="tables">the full model</param>
      /// <param name="edges">edges extracted from the full model (keep the
      /// diagnostics; the projection only decides what draws)</param>
      /// <param name="visibility">the view state; null means everything draws
      /// (the pre-038 / unset behavior)</param>
      /// <returns>the visible tables (original order) and the edges whose
      /// <b>both</b> endpoints are visible</returns>
      public static (IReadOnlyList<TableInfo> Tables, IReadOnlyList<FkRelation> Edges) Project(
         IReadOnlyList<TableInfo> tables, IReadOnlyList<FkRelation> edges, EntityVisibility visibility)
      {
         IReadOnlyList<TableInfo> visibleTables;
         if (tables == null)
         {
            visibleTables = Array.Empty<TableInfo>();
         }
         else if (visibility == null)
         {
            visibleTables = tables;
         }
         else
         {
            visibleTables = tables
               .Where(t => visibility.IsVisible(t))
               .ToList();
         }

         var visibleNames = new HashSet<string>(
            visibleTables.Select(t => t.TableName), StringComparer.Ordinal);
         IReadOnlyList<FkRelation> visibleEdges;
         if (edges == null)
         {
            visibleEdges = Array.Empty<FkRelation>();
         }
         else
         {
            visibleEdges = edges
               .Where(e => e != null &&
                           visibleNames.Contains(e.ChildTable) &&
                           visibleNames.Contains(e.ParentTable))
               .ToList();
         }
         return (visibleTables, visibleEdges);
      }
   }

}
