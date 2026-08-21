using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 038 — the projection of a full extracted edge set onto the
   /// visible subset. It runs AFTER FkEdgeExtractor (so diagnostics still
   /// surface for hidden edges) and keeps an edge only when BOTH endpoints
   /// are visible — the rule both renderers must agree on.
   /// </summary>
   public class ModelProjectionTests
   {

      private static TableInfo Table(string name, params string[] tags)
      {
         return new TableInfo
         {
            TableName = name,
            Tags = tags != null && tags.Length > 0 ? new List<string>(tags) : null
         };
      }

      private static FkRelation Edge(string child, string parent)
      {
         return new FkRelation(child, child + "Id", parent, "Id");
      }

      [Fact]
      public void ProjectsToTheVisibleTablesOnly()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("AuditLog", "Audit"),
            Table("Lookup")
         };
         var edges = new List<FkRelation>
         {
            Edge("Orders", "AuditLog")
         };
         var visibility = new EntityVisibility(new[] { "Core", "Audit" });
         visibility.SetGroupVisible("Audit", false);

         var (visibleTables, visibleEdges) =
            ModelProjection.Project(tables, edges, visibility);

         Assert.Contains(visibleTables, t => t.TableName == "Orders");
         Assert.DoesNotContain(visibleTables, t => t.TableName == "AuditLog");
         Assert.Contains(visibleTables, t => t.TableName == "Lookup");
         Assert.Equal(2, visibleTables.Count);
      }

      [Fact]
      public void EdgeDrawsOnlyWhenBothEndpointsAreVisible()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("AuditLog", "Audit")
         };
         var edges = new List<FkRelation>
         {
            Edge("Orders", "AuditLog")
         };
         var visibility = new EntityVisibility(new[] { "Core", "Audit" });
         visibility.SetGroupVisible("Audit", false);

         var (_, visibleEdges) = ModelProjection.Project(tables, edges, visibility);

         Assert.Empty(visibleEdges);
      }

      [Fact]
      public void EdgeDrawsWhenBothEndpointsAreVisible()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("Customer", "Core")
         };
         var edges = new List<FkRelation>
         {
            Edge("Orders", "Customer")
         };
         var visibility = new EntityVisibility(new[] { "Core" });

         var (_, visibleEdges) = ModelProjection.Project(tables, edges, visibility);

         Assert.Single(visibleEdges);
         Assert.Same(edges[0], visibleEdges[0]);
      }

      [Fact]
      public void NullVisibilityShowsEverything()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("AuditLog", "Audit")
         };
         var edges = new List<FkRelation> { Edge("Orders", "AuditLog") };

         var (visibleTables, visibleEdges) =
            ModelProjection.Project(tables, edges, null);

         Assert.Equal(2, visibleTables.Count);
         Assert.Single(visibleEdges);
      }

      [Fact]
      public void NullInputsYieldEmptyProjection()
      {
         var (visibleTables, visibleEdges) =
            ModelProjection.Project(null, null, new EntityVisibility(null));

         Assert.Empty(visibleTables);
         Assert.Empty(visibleEdges);
      }

      [Fact]
      public void NullEdgesAreSkipped()
      {
         var tables = new[] { Table("Orders", "Core"), Table("Customer", "Core") };
         var edges = new List<FkRelation> { Edge("Orders", "Customer"), null };

         var (_, visibleEdges) = ModelProjection.Project(tables, edges, null);

         Assert.Single(visibleEdges);
      }

      [Fact]
      public void KeepsTheOriginalTableOrder()
      {
         var tables = new[]
         {
            Table("Zebra", "A"),
            Table("Alpha", "B"),
            Table("Mango") // ungrouped — never hidden by groups
         };
         var visibility = new EntityVisibility(new[] { "A", "B" });

         var (visibleTables, _) = ModelProjection.Project(tables, null, visibility);

         Assert.Equal(new[] { "Zebra", "Alpha", "Mango" },
            visibleTables.Select(t => t.TableName));
      }

      [Fact]
      public void DoesNotMutateTheInputs()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("AuditLog", "Audit")
         };
         var edges = new List<FkRelation> { Edge("Orders", "AuditLog") };
         var visibility = new EntityVisibility(new[] { "Core", "Audit" });
         visibility.SetGroupVisible("Audit", false);

         ModelProjection.Project(tables, edges, visibility);

         Assert.Equal(2, tables.Length);
         Assert.Single(edges);
         Assert.Equal(2, visibility.Groups.Count);
      }

      [Fact]
      public void ProjectionConsumesAFreshEntityVisibilityForTheSameModel()
      {
         // A model replacement creates a new show-everything visibility; the
         // projection over it must draw every table and edge, exactly like a
         // fresh model load (backward compatibility with the pre-038 renderer).
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("AuditLog", "Audit")
         };
         var edges = new List<FkRelation> { Edge("Orders", "AuditLog") };
         var visibility = EntityVisibility.Create(tables);

         var (visibleTables, visibleEdges) =
            ModelProjection.Project(tables, edges, visibility);

         Assert.Equal(2, visibleTables.Count);
         Assert.Single(visibleEdges);
      }
   }

}
