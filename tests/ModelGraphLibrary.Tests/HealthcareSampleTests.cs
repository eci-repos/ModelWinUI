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
      public void EnumTypedColumnsResolveToRealValueSets()
      {
         // Backlog 021: every column that declares an enumeration resolves to
         // a real value-set in the interpretation — the inspector's readout
         // depends on this dictionary lookup succeeding.
         var result = Interpret();

         var enumColumns = result.Tables
            .SelectMany(t => t.Columns)
            .Where(c => !string.IsNullOrEmpty(c.EnumerationName))
            .ToList();

         Assert.Equal(4, enumColumns.Count);
         foreach (var column in enumColumns)
         {
            Assert.True(
               result.Enumerations.ContainsKey(column.EnumerationName),
               column.EnumerationName + " is not in the interpretation's enumerations.");
         }

         var visit = result.Tables.First(t => t.TableName == "Visit");
         Assert.Equal("VisitStatus", visit.Columns.Single(c => c.ColumnName == "status").EnumerationName);
         Assert.Equal(4, result.Enumerations["VisitStatus"].Values.Count);
      }

      [Fact]
      public void ValueListFormatsCommaSeparatedCodes()
      {
         // Backlog 021: the inspector's readout line is built from
         // Enumeration.ValueList — "enum Gender: M, F, OTHER".
         var result = Interpret();

         Assert.Equal("M, F, OTHER", result.Enumerations["Gender"].ValueList);
         Assert.Equal(
            "SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED",
            result.Enumerations["VisitStatus"].ValueList);
         Assert.Equal("SUBMITTED, PAID, DENIED", result.Enumerations["ClaimStatus"].ValueList);
      }

      [Fact]
      public void DependencyReadoutReadsTheExtendedModel()
      {
         // Backlog 022: the inspector's dependency readout reads the live
         // constraint — cardinality/optionality and role names — never a
         // frozen projection.
         var visit = Interpret().Tables.First(t => t.TableName == "Visit");

         var patient = visit.Columns.Single(c => c.ColumnName == "patient")
            .Constraints.Single(c => c.IsForeignKey);
         Assert.Equal(1, patient.MinCardinality);
         Assert.Equal(1, patient.MaxCardinality);
         Assert.Equal("1..1 (required)", ReadoutFormatter.Cardinality(patient));

         var department = visit.Columns.Single(c => c.ColumnName == "department")
            .Constraints.Single(c => c.IsForeignKey);
         Assert.Equal(0, department.MinCardinality);
         Assert.Equal(1, department.MaxCardinality);
         Assert.Equal("0..1 (optional)", ReadoutFormatter.Cardinality(department));

         var admitting = visit.Columns.Single(c => c.ColumnName == "admittingProvider")
            .Constraints.Single(c => c.IsForeignKey);
         Assert.Equal("admitting", admitting.ChildRole);
         Assert.Equal("admits", admitting.ParentRole);
         Assert.Equal("admitting → admits", ReadoutFormatter.Roles(admitting));
      }

      [Fact]
      public void ExtractedEdgesCarryTheirConstraint()
      {
         // Backlog 022: the connector the user clicks is the FkRelation the
         // extractor produced — it must carry the source constraint so the
         // inspector can show the dependency details.
         var tables = Interpret().Tables;
         var (edges, _) = FkEdgeExtractor.Extract(tables);

         var admitting = edges.Single(e =>
            e.ChildTable == "Visit" && e.ChildColumn == "admittingProvider");

         Assert.NotNull(admitting.Constraint);
         Assert.Equal("admitting", admitting.Constraint.ChildRole);
         Assert.Equal("admits", admitting.Constraint.ParentRole);
      }

      [Fact]
      public void ProvenanceAndModelMetadataFormatForReadout()
      {
         // Backlog 022: the model-level readout (inspector idle state + the
         // load-time log line) is built from the formatter's output.
         var result = Interpret();

         Assert.Equal(
            "source: clinic-schema.json · version: 1.0 · loaded: 2026-08-18",
            ReadoutFormatter.Provenance(result.Provenance));

         var lines = ReadoutFormatter.MetadataLines(result.Metadata);
         Assert.Equal(3, lines.Count);
         Assert.Equal("domain: healthcare", lines[0]);
         Assert.Equal("owner: clinic-it", lines[1]);
         Assert.Equal("standard: HL7-ish", lines[2]);
      }

      [Fact]
      public void DescriptionsAreCapturedFromTheSample()
      {
         // Backlog 024: the shipped Healthcare sample carries descriptions on
         // Patient (table + name element) and on Visit/Claim/Appointment; the
         // inspector reads them straight off the interpreted tables.
         var result = Interpret();

         var patient = result.Tables.First(t => t.TableName == "Patient");
         Assert.Equal("A person receiving care at the clinic.", patient.Description);
         Assert.Equal(
            "Full legal name.",
            patient.Columns.Single(c => c.ColumnName == "name").Description);

         Assert.Equal(
            "A patient's encounter at the clinic.",
            result.Tables.First(t => t.TableName == "Visit").Description);
         Assert.Equal(
            "An insurance claim submitted for a visit.",
            result.Tables.First(t => t.TableName == "Claim").Description);
         Assert.Equal(
            "A scheduled patient-provider appointment.",
            result.Tables.First(t => t.TableName == "Appointment").Description);

         // An entity without a description stays null — optionality.
         Assert.Null(result.Tables.First(t => t.TableName == "Provider").Description);
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
