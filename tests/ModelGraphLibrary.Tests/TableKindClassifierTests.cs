using System.Linq;

using Model.Data;
using ModelConsole.ModelData;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Classification of tables into entity vs reference-code kinds (used to
   /// color table headers).
   /// </summary>
   public class TableKindClassifierTests
   {

      [Fact]
      public void RefPrefixedTablesClassifyAsReferenceCode()
      {
         var refTables = PublicSafetySchema.Tables
            .Where(t => t.TableName.StartsWith("Ref"))
            .ToList();

         Assert.NotEmpty(refTables);
         foreach (var t in refTables)
         {
            Assert.Equal(TableKind.ReferenceCode, TableKindClassifier.Classify(t));
         }
      }

      [Fact]
      public void SchemaEntityTablesClassifyAsEntity()
      {
         var entityTables = PublicSafetySchema.Tables
            .Where(t => !t.TableName.StartsWith("Ref"))
            .ToList();

         Assert.NotEmpty(entityTables);
         foreach (var t in entityTables)
         {
            Assert.Equal(TableKind.Entity, TableKindClassifier.Classify(t));
         }
      }

      [Fact]
      public void SmallCodeDescriptionTableClassifiesAsReference()
      {
         var table = new TableInfo
         {
            TableName = "MyLookup",
            Columns = new ColumnList
            {
               new ColumnInfo { ColumnName = "Code", IsKey = true },
               new ColumnInfo { ColumnName = "Description" }
            }
         };

         Assert.Equal(TableKind.ReferenceCode, TableKindClassifier.Classify(table));
      }

      [Fact]
      public void WideTableWithDescriptionColumnClassifiesAsEntity()
      {
         var table = new TableInfo
         {
            TableName = "MyEntity",
            Columns = new ColumnList
            {
               new ColumnInfo { ColumnName = "ID", IsKey = true },
               new ColumnInfo { ColumnName = "Name" },
               new ColumnInfo { ColumnName = "Description" },
               new ColumnInfo { ColumnName = "Other" }
            }
         };

         Assert.Equal(TableKind.Entity, TableKindClassifier.Classify(table));
      }

      [Fact]
      public void NullTableClassifiesAsEntity()
      {
         Assert.Equal(TableKind.Entity, TableKindClassifier.Classify(null));
      }

   }

}
