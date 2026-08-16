using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Unit tests for <see cref="FkEdgeExtractor"/>.
   /// </summary>
   public class FkEdgeExtractorTests
   {

      [Fact]
      public void ResolvesExplicitReferencedColumn()
      {
         var parent = Table("Parent", Pk("ParentId"), Plain("AltId"));
         var child = Table("Child", Pk("ChildId"),
            Fk("ParentId", "Parent", "AltId"));

         var (edges, issues) = FkEdgeExtractor.Extract(
            new List<TableInfo> { parent, child });

         Assert.Empty(issues);
         var edge = Assert.Single(edges);
         Assert.Equal("Child", edge.ChildTable);
         Assert.Equal("ParentId", edge.ChildColumn);
         Assert.Equal("Parent", edge.ParentTable);
         Assert.Equal("AltId", edge.ParentColumn);
      }

      [Fact]
      public void MissingReferencedColumnResolvesToParentPrimaryKey()
      {
         var parent = Table("Parent", Pk("ParentId"));
         var child = Table("Child", Pk("ChildId"),
            Fk("ParentId", "Parent", null));

         var (edges, issues) = FkEdgeExtractor.Extract(
            new List<TableInfo> { parent, child });

         Assert.Empty(issues);
         var edge = Assert.Single(edges);
         Assert.Equal("ParentId", edge.ParentColumn);
      }

      [Fact]
      public void MissingParentTableIsReportedAndSkipped()
      {
         var child = Table("Child", Pk("ChildId"),
            Fk("ParentId", "Missing", null));

         var (edges, issues) = FkEdgeExtractor.Extract(
            new List<TableInfo> { child });

         Assert.Empty(edges);
         var issue = Assert.Single(issues);
         Assert.Contains("missing table 'Missing'", issue);
      }

      [Fact]
      public void BadReferencedColumnIsReportedAndSkipped()
      {
         var parent = Table("Parent", Pk("ParentId"));
         var child = Table("Child", Pk("ChildId"),
            Fk("ParentId", "Parent", "NoSuchColumn"));

         var (edges, issues) = FkEdgeExtractor.Extract(
            new List<TableInfo> { parent, child });

         Assert.Empty(edges);
         var issue = Assert.Single(issues);
         Assert.Contains("missing column 'Parent.NoSuchColumn'", issue);
      }

      [Fact]
      public void ParentWithoutPrimaryKeyIsReportedAndSkipped()
      {
         var parent = Table("Parent", Plain("ParentId"));
         var child = Table("Child", Pk("ChildId"),
            Fk("ParentId", "Parent", null));

         var (edges, issues) = FkEdgeExtractor.Extract(
            new List<TableInfo> { parent, child });

         Assert.Empty(edges);
         var issue = Assert.Single(issues);
         Assert.Contains("has no primary key", issue);
      }

      [Fact]
      public void ExplicitReferencedColumnOverridesPrimaryKeyDefault()
      {
         var parent = Table("Parent", Pk("ParentId"), Plain("AltId"));
         var child = Table("Child", Pk("ChildId"),
            Fk("ParentId", "Parent", "AltId"));

         var (edges, _) = FkEdgeExtractor.Extract(
            new List<TableInfo> { parent, child });

         var edge = Assert.Single(edges);
         Assert.Equal("AltId", edge.ParentColumn);
      }

      [Fact]
      public void FkWithoutReferencedTableIsReportedAndSkipped()
      {
         var child = new TableInfo
         {
            SchemaName = "Test",
            TableName = "Child",
            Columns = new ColumnList
            {
               Pk("ChildId"),
               Fk("ParentId", null, null)
            }
         };

         var (edges, issues) = FkEdgeExtractor.Extract(
            new List<TableInfo> { child });

         Assert.Empty(edges);
         var issue = Assert.Single(issues);
         Assert.Contains("has no referenced table", issue);
      }

      [Fact]
      public void NonForeignKeyConstraintsAreIgnored()
      {
         var parent = Table("Parent", Pk("ParentId"));
         var child = new TableInfo
         {
            SchemaName = "Test",
            TableName = "Child",
            Columns = new ColumnList
            {
               Pk("ChildId"),
               Plain("Notes")   // no constraint at all
            }
         };
         child.Columns[0].Constraints.Add(
            new ConstraintInfo { Type = DataInfo.PRIMARY_KEY });

         var (edges, issues) = FkEdgeExtractor.Extract(
            new List<TableInfo> { parent, child });

         Assert.Empty(edges);
         Assert.Empty(issues);
      }

      [Fact]
      public void ExtractionOrderIsDeterministic()
      {
         var a = Table("A", Pk("AId"), Fk("PId", "P", null));
         var b = Table("B", Pk("BId"), Fk("PId", "P", null));
         var p = Table("P", Pk("PId"));

         var (edges1, _) = FkEdgeExtractor.Extract(
            new List<TableInfo> { a, b, p });
         var (edges2, _) = FkEdgeExtractor.Extract(
            new List<TableInfo> { a, b, p });

         Assert.Equal(edges1.Select(e => e.ChildTable), edges2.Select(e => e.ChildTable));
         Assert.Equal(new[] { "A", "B" }, edges1.Select(e => e.ChildTable));
      }

      #region -- helpers

      private static TableInfo Table(string name, params ColumnInfo[] columns)
      {
         var list = new ColumnList();
         list.AddRange(columns);
         return new TableInfo
         {
            SchemaName = "Test",
            TableName = name,
            Columns = list
         };
      }

      private static ColumnInfo Pk(string name)
      {
         var c = new ColumnInfo { ColumnName = name, IsKey = true };
         c.Constraints.Add(new ConstraintInfo { Type = DataInfo.PRIMARY_KEY });
         return c;
      }

      private static ColumnInfo Plain(string name)
      {
         return new ColumnInfo { ColumnName = name };
      }

      private static ColumnInfo Fk(string name, string refTable, string refColumn)
      {
         var c = new ColumnInfo { ColumnName = name, IsForeignKey = true };
         c.Constraints.Add(new ConstraintInfo
         {
            Type = DataInfo.FOREIGN_KEY,
            ReferencedTableName = refTable,
            ReferencedColumnName = refColumn
         });
         return c;
      }

      #endregion

   }

}
