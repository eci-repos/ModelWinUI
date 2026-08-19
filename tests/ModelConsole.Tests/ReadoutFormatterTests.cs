using System.Collections.Generic;

using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 022 — the readout formatter: pure, deterministic formatting of
   /// the extended-model data (dependency cardinality/roles, metadata,
   /// provenance) that the inspector renders. The readout always reads the
   /// live model objects, never a frozen projection.
   /// </summary>
   public class ReadoutFormatterTests
   {

      [Fact]
      public void CardinalityFormatsBoundsAndOptionality()
      {
         Assert.Equal("1..1 (required)", ReadoutFormatter.Cardinality(Fk(1, 1)));
         Assert.Equal("0..1 (optional)", ReadoutFormatter.Cardinality(Fk(0, 1)));
         Assert.Equal("1..* (required)", ReadoutFormatter.Cardinality(Fk(1, null)));
         Assert.Equal("0..* (optional)", ReadoutFormatter.Cardinality(Fk(0, null)));
         // No cardinality declared → no readout line.
         Assert.Null(ReadoutFormatter.Cardinality(Fk(null, null)));
         Assert.Null(ReadoutFormatter.Cardinality(new ConstraintInfo()));
         Assert.Null(ReadoutFormatter.Cardinality(null));
      }

      [Fact]
      public void RolesFormatsChildAndParent()
      {
         var both = Fk(1, 1);
         both.ChildRole = "admitting";
         both.ParentRole = "admits";
         Assert.Equal("admitting → admits", ReadoutFormatter.Roles(both));

         var childOnly = Fk(1, 1);
         childOnly.ChildRole = "admitting";
         Assert.Equal("admitting", ReadoutFormatter.Roles(childOnly));

         var parentOnly = Fk(1, 1);
         parentOnly.ParentRole = "admits";
         Assert.Equal("admits", ReadoutFormatter.Roles(parentOnly));

         Assert.Null(ReadoutFormatter.Roles(Fk(1, 1)));
         Assert.Null(ReadoutFormatter.Roles(null));
      }

      [Fact]
      public void MetadataLinesFormatKeyValueInOrder()
      {
         var metadata = new Dictionary<string, string>
         {
            { "sensitive", "true" },
            { "retention", "7y" }
         };

         var lines = ReadoutFormatter.MetadataLines(metadata);

         Assert.Equal(2, lines.Count);
         Assert.Equal("sensitive: true", lines[0]);
         Assert.Equal("retention: 7y", lines[1]);
         Assert.Empty(ReadoutFormatter.MetadataLines(null));
         Assert.Empty(ReadoutFormatter.MetadataLines(new Dictionary<string, string>()));
      }

      [Fact]
      public void ProvenanceFormatsSourceVersionLoaded()
      {
         var provenance = new Provenance
         {
            Source = "clinic-schema.json",
            Version = "1.0",
            LoadedAt = "2026-08-18"
         };

         Assert.Equal(
            "source: clinic-schema.json · version: 1.0 · loaded: 2026-08-18",
            ReadoutFormatter.Provenance(provenance));
         Assert.Null(ReadoutFormatter.Provenance(new Provenance()));
         Assert.Null(ReadoutFormatter.Provenance(null));
      }

      private static ConstraintInfo Fk(int? min, int? max)
      {
         return new ConstraintInfo
         {
            Type = DataInfo.FOREIGN_KEY,
            MinCardinality = min,
            MaxCardinality = max
         };
      }

   }

}
