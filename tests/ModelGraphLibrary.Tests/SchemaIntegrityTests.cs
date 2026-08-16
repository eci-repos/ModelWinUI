using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Graph;
using ModelConsole.ModelData;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Integrity checks over the 50-table public-safety schema fixture.
   /// </summary>
   public class SchemaIntegrityTests
   {

      private static readonly IReadOnlyList<TableInfo> Tables = PublicSafetySchema.Tables;

      private static readonly Dictionary<string, string[]> AreaTables = new()
      {
         ["Identity"] = new[]
         {
            "Person", "PersonAlias", "PersonName", "PersonAddress",
            "PersonContact", "PersonIdentifier", "PersonPhysicalFeature"
         },
         ["AgenciesPersonnel"] = new[]
         {
            "Agency", "AgencyUnit", "Employee", "EmployeeAssignment",
            "EmployeeCertification", "EmployeeContact", "EmployeeTraining"
         },
         ["GeographyFacilities"] = new[]
         {
            "Jurisdiction", "Address", "GeographicArea", "Facility", "FacilitySection"
         },
         ["IncidentsDispatch"] = new[]
         {
            "Incident", "IncidentParticipant", "IncidentVehicle",
            "IncidentProperty", "IncidentNarrative", "DispatchCall", "DispatchUnit"
         },
         ["Enforcement"] = new[]
         {
            "Arrest", "ArrestCharge", "Citation", "CitationCharge",
            "Warrant", "FieldInterview"
         },
         ["OffensesCase"] = new[]
         {
            "Case", "CaseCharge", "Offense", "Statute", "Evidence", "CaseOfficer"
         },
         ["CourtsSentencing"] = new[]
         {
            "Court", "CourtAppearance", "Docket", "Sentence",
            "SentenceCondition", "Parole"
         },
      };

      [Fact]
      public void SchemaHasExactlyFiftyTables()
      {
         Assert.Equal(50, Tables.Count);
      }

      [Fact]
      public void TableNamesAreUnique()
      {
         Assert.Equal(
            Tables.Count,
            Tables.Select(t => t.TableName).Distinct().Count());
      }

      [Fact]
      public void EveryFkResolvesWithoutIssues()
      {
         var (_, issues) = FkEdgeExtractor.Extract(Tables);
         Assert.Empty(issues);
      }

      [Fact]
      public void SchemaProducesExactlySeventyFourEdges()
      {
         var (edges, _) = FkEdgeExtractor.Extract(Tables);
         Assert.Equal(74, edges.Count);
      }

      [Fact]
      public void EveryDomainAreaContributesAnEdge()
      {
         var (edges, _) = FkEdgeExtractor.Extract(Tables);
         var children = edges.Select(e => e.ChildTable).ToHashSet();

         foreach (var area in AreaTables)
         {
            Assert.True(
               area.Value.Any(children.Contains),
               "Domain area '" + area.Key + "' has no FK edge.");
         }
      }

      [Fact]
      public void FkWithoutReferencedColumnResolvesToParentPrimaryKey()
      {
         // SentenceCondition.SentenceID deliberately omits ReferencedColumnName.
         var (edges, _) = FkEdgeExtractor.Extract(Tables);

         var edge = Assert.Single(
            edges, e => e.ChildTable == "SentenceCondition" &&
                        e.ChildColumn == "SentenceID");
         Assert.Equal("Sentence", edge.ParentTable);
         Assert.Equal("SentenceID", edge.ParentColumn);
      }

      [Fact]
      public void LegacySampleWithoutParentReferencesYieldsNoEdges()
      {
         var person = new TableInfo
         {
            SchemaName = "Entity",
            TableName = "Person",
            Columns = new ColumnList { PkColumn("PersonID") }
         };
         var personName = new TableInfo
         {
            SchemaName = "Entity",
            TableName = "PersonName",
            Columns = new ColumnList
            {
               PkColumn("PersonNameID"),
               FkColumnWithoutReference("PersonID")
            }
         };

         var (edges, issues) = FkEdgeExtractor.Extract(
            new List<TableInfo> { person, personName });

         Assert.Empty(edges);
         Assert.Contains(issues, i => i.Contains("no referenced table"));
      }

      private static ColumnInfo PkColumn(string name)
      {
         var c = new ColumnInfo { ColumnName = name, IsKey = true };
         c.Constraints.Add(new ConstraintInfo { Type = DataInfo.PRIMARY_KEY });
         return c;
      }

      private static ColumnInfo FkColumnWithoutReference(string name)
      {
         var c = new ColumnInfo { ColumnName = name, IsForeignKey = true };
         c.Constraints.Add(new ConstraintInfo { Type = DataInfo.FOREIGN_KEY });
         return c;
      }

   }

}
