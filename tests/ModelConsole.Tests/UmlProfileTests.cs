using System.Collections.Generic;

using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 040 — UML is a derived profile over the canonical ERD model:
   /// classes from tables, attributes from columns, associations from FKs,
   /// packages from groups, and PlantUML as deterministic text.
   /// </summary>
   public class UmlProfileTests
   {

      [Fact]
      public void AttributeIncludesTypeAndKeyTags()
      {
         var column = new ColumnInfo
         {
            ColumnName = "CustomerId",
            Type = DataInfo.VARCHAR,
            Size = 36,
            IsKey = true,
            IsForeignKey = true
         };

         Assert.Equal("+ CustomerId: VARCHAR(36) {PK, FK}",
            UmlProfile.Attribute(column));
      }

      [Theory]
      [InlineData(0, null, "0..*")]
      [InlineData(1, null, "1..*")]
      [InlineData(1, 1, "1")]
      [InlineData(0, 1, "0..1")]
      public void MultiplicityFormatsCardinality(int min, int? max, string expected)
      {
         Assert.Equal(expected, UmlProfile.Multiplicity(min, max));
      }

      [Fact]
      public void ClassDiagramExportIsDeterministic()
      {
         var tables = SampleTables();

         string plant = UmlPlantEmitter.EmitClassDiagram(tables);

         string expected =
            "@startuml\r\n" +
            "hide circle\r\n" +
            "skinparam classAttributeIconSize 0\r\n" +
            "class \"Sales::Customer\" as C_Sales__Customer <<entity>> {\r\n" +
            "  + Id: VARCHAR(36) {PK}\r\n" +
            "}\r\n" +
            "class \"Sales::Orders\" as C_Sales__Orders <<entity>> {\r\n" +
            "  + Id: VARCHAR(36) {PK}\r\n" +
            "  + CustomerId: VARCHAR(36) {FK}\r\n" +
            "}\r\n" +
            "C_Sales__Orders \"1..*\" --> \"1\" C_Sales__Customer : places / receives\r\n" +
            "@enduml\r\n";
         Assert.Equal(Normalize(expected), Normalize(plant));
      }

      [Fact]
      public void PackageDiagramExportUsesCollapsedGroupBoxes()
      {
         var tables = SampleTables();
         var collapse = new GroupCollapseState();
         collapse.SetCollapsed("Sales", true);

         string plant = UmlPlantEmitter.EmitPackageDiagram(
            tables, GroupingThemes.Tags, null, collapse);

         Assert.Contains("package \"Sales (2)\" as P_Sales <<package>>", plant);
         Assert.DoesNotContain("class \"Sales::Orders\"", plant);
         Assert.DoesNotContain("class \"Sales::Customer\"", plant);
      }

      private static string Normalize(string text)
      {
         return text.Replace("\r\n", "\n");
      }

      private static IReadOnlyList<TableInfo> SampleTables()
      {
         return new[]
         {
            new TableInfo
            {
               SchemaName = "Sales",
               TableName = "Orders",
               Tags = new List<string> { "Sales" },
               Columns = new List<ColumnInfo>
               {
                  Key("Id", 0),
                  new ColumnInfo
                  {
                     ColumnName = "CustomerId",
                     OrdinalPosition = 1,
                     Type = DataInfo.VARCHAR,
                     Size = 36,
                     IsForeignKey = true,
                     Constraints = new List<ConstraintInfo>
                     {
                        new ConstraintInfo
                        {
                           Type = DataInfo.FOREIGN_KEY,
                           ReferencedTableName = "Customer",
                           ReferencedColumnName = "Id",
                           MinCardinality = 1,
                           MaxCardinality = null,
                           ChildRole = "places",
                           ParentRole = "receives"
                        }
                     }
                  }
               }
            },
            new TableInfo
            {
               SchemaName = "Sales",
               TableName = "Customer",
               Tags = new List<string> { "Sales" },
               Columns = new List<ColumnInfo> { Key("Id", 0) }
            }
         };
      }

      private static ColumnInfo Key(string name, int ordinal)
      {
         return new ColumnInfo
         {
            ColumnName = name,
            OrdinalPosition = ordinal,
            Type = DataInfo.VARCHAR,
            Size = 36,
            IsKey = true,
            Constraints = new List<ConstraintInfo>
            {
               new ConstraintInfo { Type = DataInfo.PRIMARY_KEY }
            }
         };
      }
   }

}
