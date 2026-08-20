using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Editing;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 029 — the pure edit operations: atomic, invariant-preserving
   /// mutations over the canonical model. Renames cascade across referencing
   /// FKs; key flags are recomputed from constraints after add/remove; and a
   /// broken edit surfaces as a resolution issue through
   /// <see cref="FkEdgeExtractor"/> — never a crash (the DoD pipeline).
   /// </summary>
   public class ModelEditsTests
   {

      [Fact]
      public void RenameTableCascadesReferencingFks()
      {
         var parent = new TableInfo
         {
            TableName = "Doctor",
            Columns = new List<ColumnInfo> { new ColumnInfo { ColumnName = "Id" } }
         };
         parent.Columns[0].Add(new ConstraintInfo
         {
            Type = DataInfo.PRIMARY_KEY,
            TableName = "Doctor",
            ColumnName = "Id"
         });
         var child = new TableInfo
         {
            TableName = "Incident",
            Columns = new List<ColumnInfo> { new ColumnInfo { ColumnName = "DoctorId" } }
         };
         child.Columns[0].Add(new ConstraintInfo
         {
            Type = DataInfo.FOREIGN_KEY,
            ReferencedTableName = "Doctor",
            ReferencedColumnName = "Id"
         });
         var tables = new List<TableInfo> { parent, child };

         ModelEdits.RenameTable(tables, parent, "Physician");

         Assert.Equal("Physician", parent.TableName);
         // The renamed table's own constraints carry the new name.
         Assert.Equal("Physician", parent.Columns[0].Constraints[0].TableName);
         // Referencing FKs follow.
         Assert.Equal("Physician", child.Columns[0].Constraints[0].ReferencedTableName);

         var (edges, issues) = FkEdgeExtractor.Extract(tables);
         Assert.Empty(issues);
         var edge = Assert.Single(edges);
         Assert.Equal("Physician", edge.ParentTable);
      }

      [Fact]
      public void RenameColumnCascadesReferencingFks()
      {
         var parent = new TableInfo
         {
            TableName = "Doctor",
            Columns = new List<ColumnInfo> { new ColumnInfo { ColumnName = "Id", IsKey = true } }
         };
         var child = new TableInfo
         {
            TableName = "Incident",
            Columns = new List<ColumnInfo> { new ColumnInfo { ColumnName = "DoctorId" } }
         };
         child.Columns[0].Add(new ConstraintInfo
         {
            Type = DataInfo.FOREIGN_KEY,
            ReferencedTableName = "Doctor",
            ReferencedColumnName = "Id"
         });
         var tables = new List<TableInfo> { parent, child };

         ModelEdits.RenameColumn(tables, parent, parent.Columns[0], "DoctorKey");

         Assert.Equal("DoctorKey", parent.Columns[0].ColumnName);
         Assert.Equal("DoctorKey", child.Columns[0].Constraints[0].ReferencedColumnName);

         var (edges, issues) = FkEdgeExtractor.Extract(tables);
         Assert.Empty(issues);
         Assert.Equal("DoctorKey", edges[0].ParentColumn);
      }

      [Fact]
      public void AddColumnAppendsAndAssignsOrdinal()
      {
         var table = new TableInfo
         {
            TableName = "Patient",
            Columns = new List<ColumnInfo> { new ColumnInfo { ColumnName = "Id" } }
         };
         var column = new ColumnInfo { ColumnName = "Name" };

         ModelEdits.AddColumn(table, column);

         Assert.Equal(2, table.Columns.Count);
         Assert.Equal(1, column.OrdinalPosition);
         Assert.Same(column, table.Columns[1]);
      }

      [Fact]
      public void RemoveColumnRemovesFromTable()
      {
         var column = new ColumnInfo { ColumnName = "Name" };
         var table = new TableInfo
         {
            TableName = "Patient",
            Columns = new List<ColumnInfo>
            {
               new ColumnInfo { ColumnName = "Id" },
               column
            }
         };

         ModelEdits.RemoveColumn(table, column);

         Assert.Single(table.Columns);
         Assert.DoesNotContain(column, table.Columns);
      }

      [Fact]
      public void SetColumnTypeSetsTypeAndSize()
      {
         var column = new ColumnInfo { Type = "VARCHAR", Size = 256 };

         ModelEdits.SetColumnType(column, "INT", 0);

         Assert.Equal("INT", column.Type);
         Assert.Equal(0, column.Size);
      }

      [Fact]
      public void SetKeyAddsPkConstraintAndFlag()
      {
         var table = new TableInfo { TableName = "Patient" };
         var column = new ColumnInfo { ColumnName = "Id" };

         ModelEdits.SetKey(table, column, true);

         Assert.True(column.IsKey);
         Assert.Contains(column.Constraints, c => c.IsKey);
      }

      [Fact]
      public void SetKeyClearsPkConstraintAndFlag()
      {
         var table = new TableInfo { TableName = "Patient" };
         var column = new ColumnInfo { ColumnName = "Id" };
         column.Add(new ConstraintInfo { Type = DataInfo.PRIMARY_KEY });

         ModelEdits.SetKey(table, column, false);

         Assert.False(column.IsKey);
         Assert.DoesNotContain(column.Constraints, c => c.IsKey);
      }

      [Fact]
      public void AddForeignKeyCreatesConstraintAndSetsFlag()
      {
         var table = new TableInfo { TableName = "Incident" };
         var column = new ColumnInfo { ColumnName = "DoctorId" };

         ModelEdits.AddForeignKey(
            table, column, "Doctor", "Id", 1, 1, "admitting", "admits");

         Assert.True(column.IsForeignKey);
         var constraint = Assert.Single(column.Constraints);
         Assert.True(constraint.IsForeignKey);
         Assert.Equal("Doctor", constraint.ReferencedTableName);
         Assert.Equal("Id", constraint.ReferencedColumnName);
         Assert.Equal(1, constraint.MinCardinality);
         Assert.Equal(1, constraint.MaxCardinality);
         Assert.Equal("admitting", constraint.ChildRole);
         Assert.Equal("admits", constraint.ParentRole);
         Assert.Equal("Incident", constraint.TableName);
         Assert.Equal("DoctorId", constraint.ColumnName);
      }

      [Fact]
      public void AddForeignKeyBlankParentColumnResolvesToNull()
      {
         var table = new TableInfo { TableName = "Incident" };
         var column = new ColumnInfo { ColumnName = "DoctorId" };

         ModelEdits.AddForeignKey(
            table, column, "Doctor", "  ", null, null, null, null);

         Assert.Null(column.Constraints[0].ReferencedColumnName);
      }

      [Fact]
      public void RemoveForeignKeyRecomputesFlag()
      {
         var column = new ColumnInfo { ColumnName = "DoctorId" };
         var table = new TableInfo
         {
            TableName = "Incident",
            Columns = new List<ColumnInfo> { column }
         };
         var fk = new ConstraintInfo
         {
            Type = DataInfo.FOREIGN_KEY,
            ReferencedTableName = "Doctor"
         };
         column.Add(fk);

         ModelEdits.RemoveForeignKey(table, fk);

         Assert.False(column.IsForeignKey);
         Assert.Empty(column.Constraints);
      }

      [Fact]
      public void EditForeignKeyTargetRetargetsConstraint()
      {
         var constraint = new ConstraintInfo
         {
            Type = DataInfo.FOREIGN_KEY,
            ReferencedTableName = "Doctor",
            ReferencedColumnName = "Id"
         };

         ModelEdits.EditForeignKeyTarget(constraint, "Clinic", "ClinicId");

         Assert.Equal("Clinic", constraint.ReferencedTableName);
         Assert.Equal("ClinicId", constraint.ReferencedColumnName);
      }

      [Fact]
      public void EditForeignKeyTargetBlankColumnResolvesToNull()
      {
         var constraint = new ConstraintInfo
         {
            Type = DataInfo.FOREIGN_KEY,
            ReferencedTableName = "Doctor",
            ReferencedColumnName = "Id"
         };

         ModelEdits.EditForeignKeyTarget(constraint, "Clinic", "  ");

         Assert.Equal("Clinic", constraint.ReferencedTableName);
         Assert.Null(constraint.ReferencedColumnName);
      }

      [Fact]
      public void SetCardinalitySetsBounds()
      {
         var constraint = new ConstraintInfo();

         ModelEdits.SetCardinality(constraint, 0, null);

         Assert.Equal(0, constraint.MinCardinality);
         Assert.Null(constraint.MaxCardinality);
      }

      [Fact]
      public void SetRolesSetsRoleNames()
      {
         var constraint = new ConstraintInfo();

         ModelEdits.SetRoles(constraint, "admitting", "admits");

         Assert.Equal("admitting", constraint.ChildRole);
         Assert.Equal("admits", constraint.ParentRole);
      }

      [Fact]
      public void SetDescriptionSetsEntityAndColumnProse()
      {
         var table = new TableInfo { TableName = "Patient" };
         var column = new ColumnInfo { ColumnName = "Name" };

         ModelEdits.SetDescription(table, "A patient of the clinic.");
         ModelEdits.SetDescription(column, "The patient's name.");

         Assert.Equal("A patient of the clinic.", table.Description);
         Assert.Equal("The patient's name.", column.Description);
      }

      [Fact]
      public void SetTableTagsNormalizesAndApplies()
      {
         var table = new TableInfo { TableName = "Incident" };

         var applied = ModelEdits.SetTableTags(
            table, new[] { "  Core ", "", "core", "uml", " Core " }, out var rejected);

         // Trimmed, blanks dropped, duplicates dropped (case-insensitive,
         // first occurrence kept), order preserved.
         Assert.Equal(new[] { "Core", "uml" }, applied);
         Assert.Same(applied, table.Tags);
         Assert.Empty(rejected);
      }

      [Fact]
      public void SetTableTagsRejectsInvalidNames()
      {
         var table = new TableInfo { TableName = "Incident" };

         var applied = ModelEdits.SetTableTags(
            table, new[] { "Core", "1st", "has space", "has.dash", "ok-1", "ok_2" },
            out var rejected);

         Assert.Equal(new[] { "Core", "ok-1", "ok_2" }, applied);
         Assert.Equal(new[] { "1st", "has space", "has.dash" }, rejected);
      }

      [Fact]
      public void SetTableTagsNullInputClears()
      {
         var table = new TableInfo
         {
            TableName = "Incident",
            Tags = new List<string> { "Core" }
         };

         ModelEdits.SetTableTags(table, null, out var rejected);

         Assert.Empty(table.Tags);
         Assert.Empty(rejected);
      }

      [Theory]
      [InlineData("Core", true)]
      [InlineData("ok_1", true)]
      [InlineData("ok-1", true)]
      [InlineData("camelCaseTag", true)]
      [InlineData("1st", false)]
      [InlineData("has space", false)]
      [InlineData("has.dot", false)]
      [InlineData("has,comma", false)]
      [InlineData("", false)]
      [InlineData(null, false)]
      public void IsValidTagNameEnforcesUmlIdentifier(string tag, bool valid)
      {
         Assert.Equal(valid, ModelEdits.IsValidTagName(tag));
      }

      [Fact]
      public void RemoveTableThenExtractReportsDanglingFks()
      {
         var parent = new TableInfo
         {
            TableName = "Doctor",
            Columns = new List<ColumnInfo>
            {
               new ColumnInfo { ColumnName = "Id", IsKey = true }
            }
         };
         var child = new TableInfo
         {
            TableName = "Incident",
            Columns = new List<ColumnInfo> { new ColumnInfo { ColumnName = "DoctorId" } }
         };
         child.Columns[0].Add(new ConstraintInfo
         {
            Type = DataInfo.FOREIGN_KEY,
            ReferencedTableName = "Doctor",
            ReferencedColumnName = "Id"
         });
         var tables = new List<TableInfo> { parent, child };

         ModelEdits.RemoveTable(tables, parent);

         var (edges, issues) = FkEdgeExtractor.Extract(tables);
         Assert.Empty(edges);
         Assert.Contains(issues, i => i.Contains("missing table 'Doctor'"));
      }

      [Fact]
      public void RemoveColumnReferencedByFkProducesIssue()
      {
         var parent = new TableInfo
         {
            TableName = "Doctor",
            Columns = new List<ColumnInfo>
            {
               new ColumnInfo { ColumnName = "Id", IsKey = true }
            }
         };
         var child = new TableInfo
         {
            TableName = "Incident",
            Columns = new List<ColumnInfo> { new ColumnInfo { ColumnName = "DoctorId" } }
         };
         child.Columns[0].Add(new ConstraintInfo
         {
            Type = DataInfo.FOREIGN_KEY,
            ReferencedTableName = "Doctor",
            ReferencedColumnName = "Id"
         });
         var tables = new List<TableInfo> { parent, child };

         ModelEdits.RemoveColumn(parent, parent.Columns[0]);

         var (edges, issues) = FkEdgeExtractor.Extract(tables);
         Assert.Empty(edges);
         Assert.Contains(issues, i => i.Contains("missing column 'Doctor.Id'"));
      }

   }

}
