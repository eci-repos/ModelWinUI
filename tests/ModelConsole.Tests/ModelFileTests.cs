using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Model.Data;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// JSON round-trip of a whole model (a list of tables) through
   /// <see cref="ModelFile"/>.
   /// </summary>
   public class ModelFileTests
   {

      [Fact]
      public void RoundTripPreservesTablesColumnsAndConstraints()
      {
         var tables = BuildFixture();

         var json = ModelFile.ToJson(tables);
         var loaded = ModelFile.LoadJson(json);

         Assert.Equal(2, loaded.Count);

         var parent = loaded.First(t => t.TableName == "Parent");
         Assert.Equal("dbo", parent.SchemaName);
         Assert.Equal(2, parent.Columns.Count);
         Assert.Equal("int", parent.Columns[0].Type);
         Assert.True(parent.Columns[0].IsKey);
         Assert.Equal(100, parent.Columns[1].Size);

         var child = loaded.First(t => t.TableName == "Child");
         var fk = child.Columns.First(c => c.ColumnName == "ParentID");
         Assert.True(fk.IsForeignKey);
         var constraint = fk.Constraints.Single(c => c.IsForeignKey);
         Assert.Equal("Parent", constraint.ReferencedTableName);
         Assert.Equal("ID", constraint.ReferencedColumnName);
      }

      [Fact]
      public void LoadFromFileReturnsTheSameTables()
      {
         var tables = BuildFixture();
         string path = Path.Combine(Path.GetTempPath(),
            "model-console-model-file-" + Guid.NewGuid().ToString("N") + ".json");
         try
         {
            File.WriteAllText(path, ModelFile.ToJson(tables));

            var loaded = ModelFile.Load(path);

            Assert.Equal(2, loaded.Count);
            Assert.Equal(
               tables.Select(t => t.TableName).OrderBy(n => n),
               loaded.Select(t => t.TableName).OrderBy(n => n));
         }
         finally
         {
            File.Delete(path);
         }
      }

      [Fact]
      public void EmptyModelRoundTrips()
      {
         var loaded = ModelFile.LoadJson(ModelFile.ToJson(new TableInfo[0]));

         Assert.Empty(loaded);
      }

      // -------- Container form (backlog 023) --------------------------------

      [Fact]
      public void ContainerFormDeclaresSchemaOnceAndRestoresItOnLoad()
      {
         var json = ModelFile.ToJson(BuildFixture());

         using var doc = JsonDocument.Parse(json);
         var root = doc.RootElement;
         Assert.Equal(JsonValueKind.Object, root.ValueKind);

         var schemas = root.GetProperty("schemas");
         Assert.Equal(1, schemas.GetArrayLength());
         var schema = schemas[0];
         Assert.Equal("dbo", schema.GetProperty("name").GetString());
         Assert.Equal(2, schema.GetProperty("tables").GetArrayLength());
         // The schema is declared once at the container level — never repeated
         // on the tables inside.
         Assert.False(schema.GetProperty("tables")[0].TryGetProperty("SchemaName", out _));

         var loaded = ModelFile.LoadJson(json);
         Assert.Equal(2, loaded.Count);
         Assert.All(loaded, t => Assert.Equal("dbo", t.SchemaName));
      }

      [Fact]
      public void ContainerGroupsTablesBySchema()
      {
         var tables = new[]
         {
            new TableInfo
            {
               SchemaName = "dbo",
               TableName = "Parent",
               Columns = new ColumnList { new ColumnInfo { ColumnName = "ID", Type = "int" } }
            },
            new TableInfo
            {
               SchemaName = "sales",
               TableName = "Order",
               Columns = new ColumnList { new ColumnInfo { ColumnName = "ID", Type = "int" } }
            }
         };

         var json = ModelFile.ToJson(tables);
         using var doc = JsonDocument.Parse(json);
         var schemaNames = doc.RootElement.GetProperty("schemas")
            .EnumerateArray()
            .Select(s => s.GetProperty("name").GetString())
            .OrderBy(n => n)
            .ToArray();
         Assert.Equal(new[] { "dbo", "sales" }, schemaNames);

         var loaded = ModelFile.LoadJson(json);
         Assert.Equal("dbo", loaded.First(t => t.TableName == "Parent").SchemaName);
         Assert.Equal("sales", loaded.First(t => t.TableName == "Order").SchemaName);
      }

      [Fact]
      public void ContainerDataSourceRoundTripsIntoCatalogName()
      {
         var tables = new[]
         {
            new TableInfo
            {
               CatalogName = "Clinic",
               SchemaName = "dbo",
               TableName = "Patient",
               Columns = new ColumnList { new ColumnInfo { ColumnName = "ID", Type = "int" } }
            }
         };

         var json = ModelFile.ToJson(tables);
         Assert.Contains("\"dataSource\": \"Clinic\"", json);
         Assert.False(JsonDocument.Parse(json).RootElement.GetProperty("schemas")[0]
            .GetProperty("tables")[0].TryGetProperty("CatalogName", out _));

         var loaded = ModelFile.LoadJson(json);
         Assert.Equal("Clinic", loaded.Single().CatalogName);
      }

      // -------- Descriptions (backlog 024) ---------------------------------

      [Fact]
      public void TableAndColumnDescriptionsRoundTrip()
      {
         var tables = new[]
         {
            new TableInfo
            {
               SchemaName = "dbo",
               TableName = "Patient",
               Description = "A person receiving care at the clinic.",
               Columns = new ColumnList
               {
                  new ColumnInfo { ColumnName = "ID", Type = "int", IsKey = true },
                  new ColumnInfo { ColumnName = "name", Type = "string", Description = "Full legal name." }
               }
            }
         };

         var json = ModelFile.ToJson(tables);
         var loaded = ModelFile.LoadJson(json);

         var patient = loaded.Single();
         Assert.Equal("A person receiving care at the clinic.", patient.Description);
         Assert.Equal(
            "Full legal name.",
            patient.Columns.Single(c => c.ColumnName == "name").Description);
         // A column without a description stays null.
         Assert.Null(patient.Columns.Single(c => c.ColumnName == "ID").Description);
      }

      private static IReadOnlyList<TableInfo> BuildFixture()
      {
         var parent = new TableInfo
         {
            SchemaName = "dbo",
            TableName = "Parent",
            Columns = new ColumnList
            {
               new ColumnInfo
               {
                  ColumnName = "ID",
                  Type = "int",
                  IsKey = true,
                  Constraints = { new ConstraintInfo { Type = DataInfo.PRIMARY_KEY } }
               },
               new ColumnInfo
               {
                  ColumnName = "Name",
                  Type = "nvarchar",
                  Size = 100
               }
            }
         };

         var child = new TableInfo
         {
            SchemaName = "dbo",
            TableName = "Child",
            Columns = new ColumnList
            {
               new ColumnInfo
               {
                  ColumnName = "ID",
                  Type = "int",
                  IsKey = true,
                  Constraints = { new ConstraintInfo { Type = DataInfo.PRIMARY_KEY } }
               },
               new ColumnInfo
               {
                  ColumnName = "ParentID",
                  Type = "int",
                  IsForeignKey = true,
                  Constraints =
                  {
                     new ConstraintInfo
                     {
                        Type = DataInfo.FOREIGN_KEY,
                        ReferencedTableName = "Parent",
                        ReferencedColumnName = "ID"
                     }
                  }
               }
            }
         };

         return new[] { parent, child };
      }

   }

}
