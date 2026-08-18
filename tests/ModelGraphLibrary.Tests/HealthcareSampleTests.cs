using System;
using System.IO;
using System.Linq;

using Model.Data;
using Model.Interpretation;
using ModelConsole.Graph;
using ModelConsole.ModelData;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

using SkiaSharp;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 020 — the proof sample model: a hand-authored grouped JSON
   /// document (healthcare clinic) that loads through the schema-driven
   /// interpreter with no code updates to the renderers or explorer. These
   /// tests are the gate: the sample must interpret cleanly, resolve every
   /// dependency, render, and exercise the R7 precedence rule (declared
   /// beats inferred).
   /// </summary>
   public class HealthcareSampleTests
   {

      private static readonly SampleModel Sample =
         SampleModels.All.First(s => s.FileName == "Healthcare.json");

      private static string ShippedPath =>
         Path.Combine(AppContext.BaseDirectory, "Samples", Sample.FileName);

      [Fact]
      public void LoadsThroughGroupedProfileWithNoIssues()
      {
         var result = Interpret();

         Assert.Empty(result.Issues);
         Assert.Equal(12, result.Tables.Count);
      }

      [Fact]
      public void EveryEntityHasAnIdentity()
      {
         var tables = Interpret().Tables;

         foreach (var table in tables)
         {
            Assert.True(
               table.Columns.Any(c => c.IsKey),
               table.TableName + " has no primary key.");
         }
      }

      [Fact]
      public void DependenciesResolveWithNoFkIssues()
      {
         var tables = Interpret().Tables;
         var (edges, issues) = FkEdgeExtractor.Extract(tables);

         Assert.Empty(issues);
         Assert.Equal(16, edges.Count);
      }

      [Fact]
      public void RendersThroughErdComposer()
      {
         var tables = Interpret().Tables;
         using var surface = SKSurface.Create(new SKImageInfo(4, 4));
         var frame = new GlFrame(surface);

         var diagram = ErdComposer.Compose(tables, frame, new ErdOptions());

         Assert.Equal(tables.Count, diagram.Layout.Count);
         Assert.Equal(diagram.Edges.Count, diagram.Routes.Count);
      }

      [Fact]
      public void R7AmbiguousNameResolvesToDeclaredReference()
      {
         // Claim.PatientId is named like a Patient reference but declares
         // "Depends On": "Visit". R7 — declared beats inferred — so the FK
         // points at Visit, not Patient.
         var claim = Interpret().Tables.First(t => t.TableName == "Claim");
         var patientId = claim.Columns.Single(c => c.ColumnName == "PatientId");

         var fk = patientId.Constraints.Single(c => c.IsForeignKey);
         Assert.Equal("Visit", fk.ReferencedTableName);
         Assert.DoesNotContain(patientId.Constraints, c => c.ReferencedTableName == "Patient");
      }

      [Fact]
      public void TwoDependenciesToSameEntityCarryExplicitRoles()
      {
         // Visit depends on Provider twice (admitting + attending); each
         // dependency carries its own role so the two edges stay distinct.
         var visit = Interpret().Tables.First(t => t.TableName == "Visit");
         var providerFks = visit.Columns
            .SelectMany(c => c.Constraints)
            .Where(c => c.IsForeignKey && c.ReferencedTableName == "Provider")
            .ToList();

         Assert.Equal(2, providerFks.Count);
         Assert.Equal("admitting", providerFks.Single(f => f.ChildRole == "admitting").ChildRole);
         Assert.Equal("attending", providerFks.Single(f => f.ChildRole == "attending").ChildRole);
      }

      [Fact]
      public void EnumerationsProvenanceAndMetadataAreCaptured()
      {
         var result = Interpret();

         Assert.NotNull(result.Provenance);
         Assert.Equal("clinic-schema.json", result.Provenance.Source);
         Assert.Equal("healthcare", result.Metadata["domain"]);

         Assert.True(result.Enumerations.ContainsKey("Gender"));
         Assert.Equal(3, result.Enumerations["Gender"].Values.Count);

         var patient = result.Tables.First(t => t.TableName == "Patient");
         Assert.Equal("true", patient.Metadata["sensitive"]);
         Assert.Equal("Gender", patient.Columns.Single(c => c.ColumnName == "gender").EnumerationName);
      }

      [Fact]
      public void TypeMapResolvesCommonTypesAndPassesOthersThrough()
      {
         var tables = Interpret().Tables;

         var patient = tables.First(t => t.TableName == "Patient");
         Assert.Equal("INT", patient.Columns.Single(c => c.ColumnName == "id").Type);
         Assert.Equal("VARCHAR", patient.Columns.Single(c => c.ColumnName == "name").Type);
         // "date" is not in the grouped profile's type map — pass-through.
         Assert.Equal("date", patient.Columns.Single(c => c.ColumnName == "dateOfBirth").Type);

         var visit = tables.First(t => t.TableName == "Visit");
         Assert.Equal("DATETIME", visit.Columns.Single(c => c.ColumnName == "visitDate").Type);
         Assert.Equal("text", visit.Columns.Single(c => c.ColumnName == "reason").Type);

         var prescription = tables.First(t => t.TableName == "Prescription");
         Assert.Equal("INT", prescription.Columns.Single(c => c.ColumnName == "refills").Type);
      }

      private static ModelInterpretation Interpret()
      {
         return SchemaInterpreter.Interpret(
            File.ReadAllText(ShippedPath), BuiltInProfiles.Grouped);
      }

   }

}
