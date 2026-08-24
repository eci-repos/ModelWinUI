using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Graph;
using ModelConsole.ModelData;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 055 — the replaceable T-SQL DDL exporter (<see cref="ITsqlEmitter"/>/
   /// <see cref="TsqlEmitter"/>): emits schemas, tables, columns, PK, and FK
   /// DDL deterministically, with an annotated mode carrying the model's
   /// design metadata and diagnostics for unresolved FKs / unknown types.
   /// </summary>
   public class TsqlEmitterTests
   {
      private static ITsqlEmitter Emitter => new TsqlEmitter();

      [Fact]
      public void EmitsSchemasTablesColumnsAndFk()
      {
         var tables = new[]
         {
            new TableInfo
            {
               SchemaName = "dbo",
               TableName = "Parent",
               Columns = new ColumnList
               {
                  new ColumnInfo
                  {
                     ColumnName = "Id", Type = DataInfo.VARCHAR, Size = 20,
                     IsKey = true, IsNullable = false,
                     Constraints = { new ConstraintInfo { Type = DataInfo.PRIMARY_KEY } }
                  },
                  new ColumnInfo { ColumnName = "Name", Type = "VARCHAR", Size = 100 }
               }
            },
            new TableInfo
            {
               SchemaName = "dbo",
               TableName = "Child",
               Columns = new ColumnList
               {
                  new ColumnInfo
                  {
                     ColumnName = "Id", Type = DataInfo.VARCHAR, Size = 20,
                     IsKey = true, IsNullable = false,
                     Constraints = { new ConstraintInfo { Type = DataInfo.PRIMARY_KEY } }
                  },
                  new ColumnInfo
                  {
                     ColumnName = "ParentId", Type = "INT", IsForeignKey = true,
                     IsNullable = false,
                     Constraints =
                     {
                        new ConstraintInfo
                        {
                           Type = DataInfo.FOREIGN_KEY,
                           ReferencedTableName = "Parent",
                           ReferencedColumnName = "Id"
                        }
                     }
                  }
               }
            }
         };

         string script = Emitter.EmitCreateScript(tables).Script;

         Assert.Contains("CREATE SCHEMA [dbo];", script);
         Assert.Contains("CREATE TABLE [dbo].[Parent] (", script);
         Assert.Contains("CREATE TABLE [dbo].[Child] (", script);
         Assert.Contains("[Id] VARCHAR(20) NOT NULL PRIMARY KEY", script);
         Assert.Contains("[Name] VARCHAR(100) NULL", script);
         Assert.Contains("[ParentId] INT NOT NULL", script);
         Assert.Contains(
            "ALTER TABLE [dbo].[Child] WITH CHECK ADD CONSTRAINT [FK_Child_ParentId]",
            script);
         Assert.Contains("FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Parent] ([Id]);", script);
      }

      [Fact]
      public void NullReferencedColumnResolvesToParentPk()
      {
         var tables = new[]
         {
            new TableInfo
            {
               TableName = "Parent",
               Columns = new ColumnList
               {
                  new ColumnInfo { ColumnName = "KeyCol", IsKey = true,
                     Constraints = { new ConstraintInfo { Type = DataInfo.PRIMARY_KEY } } }
               }
            },
            new TableInfo
            {
               TableName = "Child",
               Columns = new ColumnList
               {
                  new ColumnInfo
                  {
                     ColumnName = "ParentKey", IsForeignKey = true,
                     Constraints =
                     {
                        new ConstraintInfo
                        {
                           Type = DataInfo.FOREIGN_KEY,
                           ReferencedTableName = "Parent",
                           ReferencedColumnName = null // → parent PK
                        }
                     }
                  }
               }
            }
         };

         string script = Emitter.EmitCreateScript(tables).Script;

         // The null referenced column resolved to the parent's PK column.
         Assert.Contains(
            "REFERENCES [Parent] ([KeyCol]);", script);
      }

      [Fact]
      public void CompositePrimaryKeyEmitsTableLevelConstraint()
      {
         var tables = new[]
         {
            new TableInfo
            {
               TableName = "Line",
               Columns = new ColumnList
               {
                  new ColumnInfo { ColumnName = "OrderId", IsKey = true, IsNullable = false, OrdinalPosition = 1 },
                  new ColumnInfo { ColumnName = "LineNo", IsKey = true, IsNullable = false, OrdinalPosition = 2 },
                  new ColumnInfo { ColumnName = "Qty", Type = "INT", OrdinalPosition = 3 }
               }
            }
         };

         string script = Emitter.EmitCreateScript(tables).Script;

         Assert.Contains("PRIMARY KEY ([OrderId], [LineNo])", script);
      }

      [Fact]
      public void IdentityColumnEmitsIdentity()
      {
         var tables = new[]
         {
            new TableInfo
            {
               TableName = "T",
               Columns = new ColumnList
               {
                  new ColumnInfo
                  {
                     ColumnName = "Id", Type = "INT", IsKey = true,
                     IsIdentity = true, IsNullable = false,
                     Constraints = { new ConstraintInfo { Type = DataInfo.PRIMARY_KEY } }
                  }
               }
            }
         };

         string script = Emitter.EmitCreateScript(tables).Script;

         Assert.Contains("[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY", script);
      }

      [Fact]
      public void UnknownTypePassesThroughWithDiagnostic()
      {
         var tables = new[]
         {
            new TableInfo
            {
               TableName = "T",
               Columns = new ColumnList
               {
                  new ColumnInfo { ColumnName = "Blob", Type = "MYBLOB" }
               }
            }
         };

         var result = Emitter.EmitCreateScript(tables);

         // Unknown type passes through verbatim (no crash) and is reported once.
         Assert.Contains("[Blob] MYBLOB NULL", result.Script);
         Assert.Contains(
            result.Issues,
            i => i.StartsWith("unknown column type 'MYBLOB'", StringComparison.Ordinal));
      }

      [Fact]
      public void AnnotatedModeEmitsCommentCards()
      {
         var tables = Fixture();

         string script = Emitter.EmitCreateScript(tables,
            new TsqlExportOptions { Annotated = true }).Script;

         Assert.Contains("-- TABLE : [dbo].[Parent]", script);
         Assert.Contains("-- KIND  : entity", script);
         Assert.Contains("-- FKs   :", script);
         Assert.Contains("-- FK : [Child].[ParentId] -> [Parent].[Id]", script);
         Assert.Contains("--     cardinality : 1 : 1", script);
         Assert.Contains("--     child role  :", script);
         Assert.Contains("--     parent role :", script);
      }

      [Fact]
      public void BareModeOmitsCommentCards()
      {
         var tables = Fixture();

         string script = Emitter.EmitCreateScript(tables,
            new TsqlExportOptions { Annotated = false }).Script;

         Assert.DoesNotContain("-- TABLE :", script);
         Assert.DoesNotContain("-- FK :", script);
      }

      [Fact]
      public void OutputIsDeterministic()
      {
         var tables = PublicSafetySchema.Tables;

         string a = Emitter.EmitCreateScript(tables).Script;
         string b = Emitter.EmitCreateScript(tables).Script;

         Assert.Equal(a, b);
      }

      [Fact]
      public void EmptyModelProducesEmptyScript()
      {
         string script = Emitter.EmitCreateScript(new TableInfo[0]).Script;

         Assert.True(string.IsNullOrWhiteSpace(script));
      }

      [Fact]
      public void FullPublicSafetyFixtureEmitsOneFkPerEdge()
      {
         var tables = PublicSafetySchema.Tables;
         var (edges, _) = FkEdgeExtractor.Extract(tables);

         string script = Emitter.EmitCreateScript(tables).Script;

         // Every table is created and every resolved FK has an ALTER TABLE.
         Assert.All(tables, t => Assert.Contains(
            "CREATE TABLE [" + (t.SchemaName ?? "") + "].[" + t.TableName + "] (", script));
         Assert.Equal(edges.Count,
            System.Text.RegularExpressions.Regex.Matches(script, "ADD CONSTRAINT \\[FK_")
               .Count);
      }

      // ------------------------------------------------------------------

      private static IReadOnlyList<TableInfo> Fixture()
      {
         return new[]
         {
            new TableInfo
            {
               SchemaName = "dbo",
               TableName = "Parent",
               Description = "The parent table.",
               Tags = new List<string> { "Core" },
               Columns = new ColumnList
               {
                  new ColumnInfo
                  {
                     ColumnName = "Id", Type = DataInfo.VARCHAR, Size = 20,
                     IsKey = true, IsNullable = false,
                     Constraints = { new ConstraintInfo { Type = DataInfo.PRIMARY_KEY } }
                  }
               }
            },
            new TableInfo
            {
               SchemaName = "dbo",
               TableName = "Child",
               Columns = new ColumnList
               {
                  new ColumnInfo
                  {
                     ColumnName = "Id", Type = DataInfo.VARCHAR, Size = 20,
                     IsKey = true, IsNullable = false,
                     Constraints = { new ConstraintInfo { Type = DataInfo.PRIMARY_KEY } }
                  },
                  new ColumnInfo
                  {
                     ColumnName = "ParentId", Type = "INT", IsForeignKey = true,
                     Constraints =
                     {
                        new ConstraintInfo
                        {
                           Type = DataInfo.FOREIGN_KEY,
                           ReferencedTableName = "Parent",
                           ReferencedColumnName = "Id",
                           MinCardinality = 1,
                           MaxCardinality = 1,
                           ChildRole = "parent",
                           ParentRole = "children"
                        }
                     }
                  }
               }
            }
         };
      }

   }

}
