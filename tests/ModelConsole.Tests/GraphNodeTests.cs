using System.Collections.Generic;

using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 028 — the portable graph-node surface: every drawable exposes a
   /// typed node (kind, identity, the live canonical object it renders, a
   /// hover summary, and the edit verbs it supports) that hover, click-select,
   /// and the inspector all read. Node summaries delegate to
   /// <see cref="HoverSummary"/> so the readouts never drift.
   /// </summary>
   public class GraphNodeTests
   {

      [Fact]
      public void EntityNodeExposesKindIdentityModelAndSummary()
      {
         var table = new TableInfo
         {
            SchemaName = "clinic",
            TableName = "Patient",
            Description = "A patient of the clinic.",
            Columns = new List<ColumnInfo>
            {
               new ColumnInfo { ColumnName = "Id", IsKey = true }
            }
         };

         var node = GraphNodes.Entity(table);

         Assert.IsType<EntityNode>(node);
         Assert.Equal(GraphNodeKind.Entity, node.Kind);
         Assert.Equal("clinic::Patient", node.Name);
         Assert.Same(table, node.Model);
         Assert.Equal(HoverSummary.ForTable(table), node.Summary());
      }

      [Fact]
      public void ElementNodeExposesKindIdentityModelAndSummary()
      {
         var table = new TableInfo { TableName = "Patient" };
         var column = new ColumnInfo
         {
            ColumnName = "name",
            Type = "VARCHAR",
            Size = 256,
            IsKey = true,
            EnumerationName = "Gender",
            Description = "The patient's name."
         };

         var node = GraphNodes.Element(table, column);

         Assert.IsType<ElementNode>(node);
         Assert.Equal(GraphNodeKind.Element, node.Kind);
         Assert.Equal("Patient.name", node.Name);
         Assert.Same(column, node.Model);
         Assert.Equal(HoverSummary.ForColumn(column, "Patient"), node.Summary());
      }

      [Fact]
      public void DependencyNodeExposesKindIdentityModelAndSummary()
      {
         var constraint = new ConstraintInfo
         {
            MinCardinality = 1,
            MaxCardinality = 1,
            ChildRole = "admitting",
            ParentRole = "admits"
         };
         var edge = new FkRelation(
            "Incident", "AdmittingDoctor", "Doctor", "Id", constraint);

         var node = GraphNodes.Dependency(edge);
         var dep = Assert.IsType<DependencyNode>(node);

         Assert.Equal(GraphNodeKind.Dependency, node.Kind);
         Assert.Equal("Incident.AdmittingDoctor  →  Doctor.Id", node.Name);
         Assert.Same(edge, node.Model);
         Assert.Same(constraint, dep.Constraint);
         Assert.Equal(HoverSummary.ForConnector(edge), node.Summary());
      }

      [Fact]
      public void DependencyNodeWithoutConstraintStillSummarizes()
      {
         var edge = new FkRelation("Child", "ParentId", "Parent", "Id");

         var node = GraphNodes.Dependency(edge);
         var dep = Assert.IsType<DependencyNode>(node);

         Assert.Equal(GraphNodeKind.Dependency, node.Kind);
         Assert.Null(dep.Constraint);
         Assert.Equal(HoverSummary.ForConnector(edge), node.Summary());
      }

      [Fact]
      public void GroupNodeExposesKindIdentityModelAndSummary()
      {
         var tables = new List<TableInfo>
         {
            new TableInfo { TableName = "Patient" },
            new TableInfo { TableName = "Visit" }
         };

         var node = GraphNodes.Group("clinic", tables);

         Assert.IsType<GroupNode>(node);
         Assert.Equal(GraphNodeKind.Group, node.Kind);
         Assert.Equal("clinic", node.Name);
         Assert.Same(tables, node.Model);
         Assert.Equal(HoverSummary.ForGroup("clinic", tables), node.Summary());
      }

      [Fact]
      public void FactoryReturnsNullForNullInputs()
      {
         Assert.Null(GraphNodes.Entity(null));
         Assert.Null(GraphNodes.Element(null, new ColumnInfo()));
         Assert.Null(GraphNodes.Element(new TableInfo(), null));
         Assert.Null(GraphNodes.Dependency(null));
         Assert.Null(GraphNodes.Group("clinic", null));
      }

      [Fact]
      public void EntityVerbsDeclareTheEditSurface()
      {
         var verbs = GraphNodes.Entity(new TableInfo()).Verbs;

         Assert.True(verbs.CanRename);
         Assert.True(verbs.CanAddColumn);
         Assert.True(verbs.CanRemoveColumn);
         Assert.True(verbs.CanEditKey);
         Assert.True(verbs.CanEditDescription);
         Assert.True(verbs.CanEditTags);
         Assert.True(verbs.CanEditMetadata);
         Assert.True(verbs.CanDelete);
         Assert.True(verbs.CanToggleVisibility); // backlog 038
         Assert.False(verbs.CanAddForeignKey);
         Assert.False(verbs.CanEditType);
         Assert.False(verbs.CanEditTarget);
         Assert.False(verbs.CanEditCardinality);
         Assert.False(verbs.CanEditRoles);
      }

      [Fact]
      public void ElementVerbsDeclareTheEditSurface()
      {
         var verbs = GraphNodes.Element(
            new TableInfo(), new ColumnInfo()).Verbs;

         Assert.True(verbs.CanRename);
         Assert.True(verbs.CanEditType);
         Assert.True(verbs.CanEditKey);
         Assert.True(verbs.CanEditDescription);
         Assert.False(verbs.CanEditTags);
         Assert.True(verbs.CanEditMetadata);
         Assert.True(verbs.CanAddForeignKey);
         Assert.False(verbs.CanAddColumn);
         Assert.False(verbs.CanRemoveColumn);
         Assert.False(verbs.CanEditTarget);
         Assert.False(verbs.CanEditCardinality);
         Assert.False(verbs.CanEditRoles);
         Assert.False(verbs.CanDelete);
         Assert.False(verbs.CanToggleVisibility);
      }

      [Fact]
      public void DependencyVerbsDeclareTheEditSurface()
      {
         var verbs = GraphNodes.Dependency(
            new FkRelation("Child", "ParentId", "Parent", "Id")).Verbs;

         Assert.True(verbs.CanEditTarget);
         Assert.True(verbs.CanEditCardinality);
         Assert.True(verbs.CanEditRoles);
         Assert.True(verbs.CanDelete);
         Assert.False(verbs.CanRename);
         Assert.False(verbs.CanAddColumn);
         Assert.False(verbs.CanRemoveColumn);
         Assert.False(verbs.CanEditType);
         Assert.False(verbs.CanEditKey);
         Assert.False(verbs.CanEditDescription);
         Assert.False(verbs.CanEditTags);
         Assert.False(verbs.CanEditMetadata);
         Assert.False(verbs.CanAddForeignKey);
         Assert.False(verbs.CanToggleVisibility);
      }

      [Fact]
      public void GroupVerbsDeclareNoEditSurface()
      {
         var verbs = GraphNodes.Group(
            "clinic", new List<TableInfo>()).Verbs;

         Assert.False(verbs.CanRename);
         Assert.False(verbs.CanAddColumn);
         Assert.False(verbs.CanRemoveColumn);
         Assert.False(verbs.CanEditType);
         Assert.False(verbs.CanEditKey);
         Assert.False(verbs.CanEditDescription);
         Assert.False(verbs.CanEditTags);
         Assert.False(verbs.CanEditMetadata);
         Assert.False(verbs.CanAddForeignKey);
         Assert.False(verbs.CanEditTarget);
         Assert.False(verbs.CanEditCardinality);
         Assert.False(verbs.CanEditRoles);
         Assert.False(verbs.CanDelete);
         Assert.False(verbs.CanToggleVisibility);
      }

   }

}
