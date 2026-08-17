using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
