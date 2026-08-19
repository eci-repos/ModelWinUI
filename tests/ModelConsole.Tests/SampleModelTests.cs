using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Model.Data;
using Model.Interpretation;
using ModelConsole.Graph;
using ModelConsole.ModelData;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// The shipped sample models (backlog 005): the JSON files under
   /// <c>ModelGraphLibrary/Samples</c> are real artifacts — they must load,
   /// be valid, and stay in sync with the code fixtures they are generated
   /// from. The files are copied into the test output under <c>Samples/</c>.
   /// </summary>
   public class SampleModelTests
   {

      public static IEnumerable<object[]> Samples =>
         SampleModels.All.Select(s => new object[] { s });

      [Theory]
      [MemberData(nameof(Samples))]
      public void ShippedSamplesLoadAndAreValid(SampleModel sample)
      {
         var tables = LoadTables(sample);

         Assert.NotEmpty(tables);
         foreach (var table in tables)
         {
            Assert.True(
               table.Columns.Any(c => c.IsKey),
               table.TableName + " has no primary key.");
         }

         var (_, issues) = FkEdgeExtractor.Extract(tables);
         Assert.Empty(issues);
      }

      [Theory]
      [MemberData(nameof(Samples))]
      public void ShippedJsonMatchesFixture(SampleModel sample)
      {
         string shipped = Normalize(File.ReadAllText(ShippedPath(sample)));
         string fixture = sample.FixtureJson != null
            ? Normalize(sample.FixtureJson)
            : Normalize(ModelFile.ToJson(sample.Tables));
         Assert.Equal(fixture, shipped);
      }

      [Fact]
      public void PublicSafetySampleHasExpectedShape()
      {
         var tables = ModelFile.Load(ShippedPath(Sample("PublicSafety.json")));
         var (edges, _) = FkEdgeExtractor.Extract(tables);

         Assert.Equal(50, tables.Count);
         Assert.Equal(74, edges.Count);
      }

      [Fact]
      public void LibrarySampleIsNonTrivial()
      {
         var tables = ModelFile.Load(ShippedPath(Sample("Library.json")));
         var (edges, _) = FkEdgeExtractor.Extract(tables);

         Assert.True(tables.Count >= 15, "Library sample is too small.");
         Assert.True(edges.Count >= 15, "Library sample has too few FKs.");
      }

      private static SampleModel Sample(string fileName)
      {
         return SampleModels.All.First(s => s.FileName == fileName);
      }

      private static string ShippedPath(SampleModel sample)
      {
         return Path.Combine(
            AppContext.BaseDirectory, "Samples", sample.FileName);
      }

      /// <summary>
      /// Load a shipped sample the way the app does: array-format files via
      /// <see cref="ModelFile.Load"/>, grouped files through the interpreter
      /// (backlog 020). A grouped sample must interpret with no issues.
      /// </summary>
      private static IReadOnlyList<TableInfo> LoadTables(SampleModel sample)
      {
         if (sample.Profile != null)
         {
            var interpretation = SchemaInterpreter.Interpret(
               File.ReadAllText(ShippedPath(sample)),
               BuiltInProfiles.FromName(sample.Profile));
            Assert.Empty(interpretation.Issues);
            return interpretation.Tables;
         }
         return ModelFile.Load(ShippedPath(sample));
      }

      private static string Normalize(string text)
      {
         // Line endings and a trailing newline are cosmetic; the content is
         // what must stay in sync.
         return text.Replace("\r\n", "\n").TrimEnd('\n');
      }

   }

}
