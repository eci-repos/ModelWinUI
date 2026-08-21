using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 039 — the pure aggregation of a collapsed group into a box:
   /// its member tables (primary-tag membership, the UML owning-package
   /// rule) and its external edges, aggregated one connector per external
   /// target with a count. Internal edges (both endpoints in the box) are
   /// hidden; an entity in N groups renders in its primary group and shows
   /// as an external stub on the others.
   /// </summary>
   public class GroupBoxAggregationTests
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
      public void MembersAreThePrimaryTaggedTables()
      {
         // "Core" tables: Orders (primary Core) and Incidents (primary Core).
         // Lookup is tagged Audit (primary Audit) so it is not a Core member.
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("Incidents", "Core"),
            Table("Lookup", "Audit")
         };

         var box = GroupBoxAggregation.Build(tables, null, "Core", null);

         Assert.Equal(2, box.MemberCount);
         Assert.Contains(box.Members, t => t.TableName == "Orders");
         Assert.Contains(box.Members, t => t.TableName == "Incidents");
      }

      [Fact]
      public void PrimaryTagIsTheFirstTag()
      {
         var tables = new[]
         {
            Table("Reports", "Core", "Audit")
         };

         Assert.Equal("Core", GroupBoxAggregation.PrimaryTag(tables[0]));
         Assert.Null(GroupBoxAggregation.PrimaryTag(Table("Untagged")));
         Assert.Null(GroupBoxAggregation.PrimaryTag(null));
         Assert.Null(GroupBoxAggregation.PrimaryTag(Table("Blank", "  ")));
      }

      [Fact]
      public void InternalEdgesAreHiddenWhenCollapsed()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("Customer", "Core"),
            Table("Lookup", "Audit")
         };
         var edges = new List<FkRelation>
         {
            Edge("Orders", "Customer") // both Core members — internal
         };

         var box = GroupBoxAggregation.Build(tables, edges, "Core", null);

         Assert.Empty(box.ExternalEdges);
      }

      [Fact]
      public void ExternalOutboundEdgeIsAggregatedToTheTargetTable()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("AuditLog", "Audit")
         };
         var edges = new List<FkRelation> { Edge("Orders", "AuditLog") };

         var box = GroupBoxAggregation.Build(tables, edges, "Core", null);

         var edge = Assert.Single(box.ExternalEdges);
         Assert.Equal("AuditLog", edge.TargetTable);
         Assert.True(edge.Outbound);   // box → AuditLog
         Assert.Equal(1, edge.Count);
         Assert.Equal("AuditLog", edge.TargetKey);
         Assert.Null(edge.TargetGroup);
      }

      [Fact]
      public void ExternalInboundEdgeFlipsTheDirection()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("AuditLog", "Audit")
         };
         var edges = new List<FkRelation> { Edge("AuditLog", "Orders") };

         var box = GroupBoxAggregation.Build(tables, edges, "Core", null);

         var edge = Assert.Single(box.ExternalEdges);
         Assert.Equal("AuditLog", edge.TargetTable);
         Assert.False(edge.Outbound);  // AuditLog → box
      }

      [Fact]
      public void SharedTargetIsDeduplicatedWithACount()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("Incidents", "Core"),
            Table("AuditLog", "Audit")
         };
         var edges = new List<FkRelation>
         {
            Edge("Orders", "AuditLog"),
            Edge("Incidents", "AuditLog")
         };

         var box = GroupBoxAggregation.Build(tables, edges, "Core", null);

         var edge = Assert.Single(box.ExternalEdges);
         Assert.Equal("AuditLog", edge.TargetTable);
         Assert.Equal(2, edge.Count);
         Assert.Equal("Orders", edge.Sample.ChildTable); // first seen
      }

      [Fact]
      public void TargetInsideACollapsedGroupMergesUnderItsBox()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("AuditLog", "Audit"),
            Table("AuditEntry", "Audit")
         };
         var edges = new List<FkRelation>
         {
            Edge("Orders", "AuditLog"),
            Edge("Orders", "AuditEntry") // both Audit — same target box
         };
         var collapsed = new[] { "Audit" };

         var box = GroupBoxAggregation.Build(tables, edges, "Core", collapsed);

         var edge = Assert.Single(box.ExternalEdges);
         Assert.Equal("Audit", edge.TargetGroup);
         Assert.Equal("Audit", edge.TargetKey);
         Assert.Equal(2, edge.Count);
      }

      [Fact]
      public void TargetInAnExpandedGroupStaysATableTarget()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("AuditLog", "Audit")
         };
         var edges = new List<FkRelation> { Edge("Orders", "AuditLog") };
         var collapsed = new string[0]; // Audit NOT collapsed

         var box = GroupBoxAggregation.Build(tables, edges, "Core", collapsed);

         var edge = Assert.Single(box.ExternalEdges);
         Assert.Null(edge.TargetGroup);
         Assert.Equal("AuditLog", edge.TargetKey);
      }

      [Fact]
      public void OverlappedEntityIsAStubInItsSecondaryGroup()
      {
         // Reports is tagged [Core, Audit] → primary Core. In Audit's box it
         // is NOT a member — it appears only as an external target (a
         // reference stub on the Audit box).
         var tables = new[]
         {
            Table("Reports", "Core", "Audit"),
            Table("AuditLog", "Audit")
         };
         var edges = new List<FkRelation> { Edge("AuditLog", "Reports") };
         var collapsed = new[] { "Audit" };

         var auditBox = GroupBoxAggregation.Build(tables, edges, "Audit", collapsed);

         Assert.DoesNotContain(auditBox.Members, t => t.TableName == "Reports");
         var edge = Assert.Single(auditBox.ExternalEdges);
         Assert.Equal("Reports", edge.TargetTable);
         // Core is not collapsed, so the stub routes to the Reports table.
         Assert.Null(edge.TargetGroup);
      }

      [Fact]
      public void StubMergesUnderThePrimaryBoxWhenItIsCollapsed()
      {
         // Reports is primary Core; both Core and Audit are collapsed, so the
         // Audit-box stub aggregates under the Core box (box↔box connector).
         var tables = new[]
         {
            Table("Reports", "Core", "Audit"),
            Table("AuditLog", "Audit")
         };
         var edges = new List<FkRelation> { Edge("AuditLog", "Reports") };
         var collapsed = new[] { "Core", "Audit" };

         var auditBox = GroupBoxAggregation.Build(tables, edges, "Audit", collapsed);

         var edge = Assert.Single(auditBox.ExternalEdges);
         Assert.Equal("Core", edge.TargetGroup);
         Assert.Equal("Core", edge.TargetKey);
      }

      [Fact]
      public void EdgesWithBothEndpointsOutsideAreIgnored()
      {
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("Customer", "Core"),
            Table("AuditLog", "Audit"),
            Table("AuditEvent", "Audit")
         };
         var edges = new List<FkRelation> { Edge("AuditLog", "AuditEvent") };

         var box = GroupBoxAggregation.Build(tables, edges, "Core", null);

         Assert.Empty(box.ExternalEdges); // no edge touches the Core box
      }

      [Fact]
      public void NullInputsYieldAnEmptyBox()
      {
         var box = GroupBoxAggregation.Build(null, null, "Core", null);

         Assert.Empty(box.Members);
         Assert.Empty(box.ExternalEdges);
         Assert.Equal(0, box.MemberCount);
      }

      // ------------------------------------------------------------------
      // Backlog 043 — the theme-aware aggregation: primary membership derives
      // from the active grouping theme, so collapsing a schema/kind/connectivity
      // group boxes its members exactly as collapsing a tag group does. The
      // tag theme is the default and behaves identically to the pre-043 code
      // (the tests above pin that).
      // ------------------------------------------------------------------

      private static TableInfo SchemaTable(string name, string schema)
      {
         return new TableInfo { TableName = name, SchemaName = schema };
      }

      [Fact]
      public void SchemaThemeMembersAreTheSchemasTables()
      {
         var tables = new[]
         {
            SchemaTable("Orders", "Sales"),
            SchemaTable("Customer", "Sales"),
            SchemaTable("StockItem", "Inventory")
         };

         var box = GroupBoxAggregation.Build(
            tables, null, "Sales", null, GroupingThemes.Schema);

         Assert.Equal(2, box.MemberCount);
         Assert.Contains(box.Members, t => t.TableName == "Orders");
         Assert.Contains(box.Members, t => t.TableName == "Customer");
      }

      [Fact]
      public void SchemaThemeExternalEdgesDedupePerTarget()
      {
         var tables = new[]
         {
            SchemaTable("Orders", "Sales"),
            SchemaTable("Invoice", "Sales"),
            SchemaTable("StockItem", "Inventory")
         };
         var edges = new List<FkRelation>
         {
            Edge("Orders", "StockItem"),
            Edge("Invoice", "StockItem")
         };

         var box = GroupBoxAggregation.Build(
            tables, edges, "Sales", null, GroupingThemes.Schema);

         var edge = Assert.Single(box.ExternalEdges);
         Assert.Equal("StockItem", edge.TargetTable);
         Assert.Equal(2, edge.Count);
      }
   }

}
