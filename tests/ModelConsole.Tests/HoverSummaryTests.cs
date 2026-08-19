using System.Collections.Generic;

using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 027 — the hover readout provider: portable, deterministic lines
   /// for a graphic entity (a table or an FK connector), built from the live
   /// model objects and the shared <see cref="ReadoutFormatter"/> so the hover
   /// and the inspector never disagree.
   /// </summary>
   public class HoverSummaryTests
   {

      [Fact]
      public void ForTableShowsHeaderDescriptionCountsAndProvenance()
      {
         var table = new TableInfo
         {
            SchemaName = "clinic",
            TableName = "Patient",
            Description = "A patient of the clinic.",
            Columns = new List<ColumnInfo>
            {
               new ColumnInfo { ColumnName = "Id", IsKey = true },
               new ColumnInfo { ColumnName = "Name" },
               new ColumnInfo { ColumnName = "DoctorId", IsForeignKey = true }
            },
            Provenance = new Provenance
            {
               Source = "clinic-schema.json",
               Version = "1.0",
               LoadedAt = "2026-08-18"
            }
         };

         var lines = HoverSummary.ForTable(table);

         Assert.Equal(5, lines.Count);
         Assert.Equal("clinic::Patient", lines[0]);
         Assert.Equal("A patient of the clinic.", lines[1]);
         Assert.Equal("3 columns", lines[2]);
         Assert.Equal("PK: 1, FK: 1", lines[3]);
         Assert.Equal(
            "source: clinic-schema.json · version: 1.0 · loaded: 2026-08-18",
            lines[4]);
      }

      [Fact]
      public void ForTableMinimalShowsHeaderAndColumnCount()
      {
         var table = new TableInfo
         {
            SchemaName = "public",
            TableName = "Log",
            Columns = new List<ColumnInfo>
            {
               new ColumnInfo { ColumnName = "Entry" },
               new ColumnInfo { ColumnName = "CreatedAt" }
            }
         };

         var lines = HoverSummary.ForTable(table);

         Assert.Equal(2, lines.Count);
         Assert.Equal("public::Log", lines[0]);
         Assert.Equal("2 columns", lines[1]);
      }

      [Fact]
      public void ForTableWithoutColumnsOmitsKeyCounts()
      {
         var table = new TableInfo
         {
            SchemaName = "ref",
            TableName = "Empty",
            Columns = new List<ColumnInfo>()
         };

         var lines = HoverSummary.ForTable(table);

         Assert.Equal(2, lines.Count);
         Assert.Equal("ref::Empty", lines[0]);
         Assert.Equal("0 columns", lines[1]);
      }

      [Fact]
      public void ForConnectorShowsCardinalityAndRoles()
      {
         var constraint = new ConstraintInfo
         {
            MinCardinality = 1,
            MaxCardinality = 1,
            ChildRole = "admitting",
            ParentRole = "admits"
         };
         var edge = new FkRelation(
            "Incident", "AdmittingDoctor", "Doctor", "Id", constraint);

         var lines = HoverSummary.ForConnector(edge);

         Assert.Equal(3, lines.Count);
         Assert.Equal("Incident.AdmittingDoctor  →  Doctor.Id", lines[0]);
         Assert.Equal("Cardinality: 1..1 (required)", lines[1]);
         Assert.Equal("Roles: admitting → admits", lines[2]);
      }

      [Fact]
      public void ForConnectorWithoutConstraintShowsArrowOnly()
      {
         var edge = new FkRelation(
            "Incident", "DoctorId", "Doctor", "Id");

         var lines = HoverSummary.ForConnector(edge);

         Assert.Single(lines);
         Assert.Equal("Incident.DoctorId  →  Doctor.Id", lines[0]);
      }

      [Fact]
      public void ForColumnShowsTypeTagsAndDescription()
      {
         var column = new ColumnInfo
         {
            ColumnName = "name",
            Type = "VARCHAR",
            Size = 256,
            IsKey = true,
            EnumerationName = "Gender",
            Description = "The patient's name."
         };

         var lines = HoverSummary.ForColumn(column, "Patient");

         Assert.Equal(4, lines.Count);
         Assert.Equal("Patient.name", lines[0]);
         Assert.Equal("VARCHAR(256)", lines[1]);
         Assert.Equal("PK, enum:Gender", lines[2]);
         Assert.Equal("The patient's name.", lines[3]);
      }

      [Fact]
      public void ForColumnMinimalShowsNameAndType()
      {
         var column = new ColumnInfo
         {
            ColumnName = "Id",
            Type = "INT",
            Size = 0
         };

         var lines = HoverSummary.ForColumn(column, "Patient");

         Assert.Equal(2, lines.Count);
         Assert.Equal("Patient.Id", lines[0]);
         Assert.Equal("INT", lines[1]);
      }

      [Fact]
      public void ForColumnNullProducesNoReadout()
      {
         Assert.Empty(HoverSummary.ForColumn(null, "Patient"));
      }

      [Fact]
      public void ForGroupShowsSchemaAndTableCount()
      {
         var tables = new List<TableInfo>
         {
            new TableInfo { TableName = "Patient" },
            new TableInfo { TableName = "Visit" }
         };

         var lines = HoverSummary.ForGroup("clinic", tables);

         Assert.Equal(2, lines.Count);
         Assert.Equal("clinic", lines[0]);
         Assert.Equal("2 tables", lines[1]);
      }

      [Fact]
      public void ForGroupNullTablesProducesNoReadout()
      {
         Assert.Empty(HoverSummary.ForGroup("clinic", null));
      }

      [Fact]
      public void ForDispatchesPayloads()
      {
         var table = new TableInfo
         {
            SchemaName = "ref",
            TableName = "Kind",
            Columns = new List<ColumnInfo>
            {
               new ColumnInfo { ColumnName = "Id", IsKey = true }
            }
         };
         var edge = new FkRelation("Child", "ParentId", "Parent", "Id");

         Assert.Equal(HoverSummary.ForTable(table), HoverSummary.For(table));
         Assert.Equal(HoverSummary.ForConnector(edge), HoverSummary.For(edge));
         Assert.Empty(HoverSummary.For(new object()));
      }

      [Fact]
      public void NullProducesNoReadout()
      {
         Assert.Empty(HoverSummary.ForTable(null));
         Assert.Empty(HoverSummary.ForConnector(null));
         Assert.Empty(HoverSummary.For(null));
      }

   }

}
