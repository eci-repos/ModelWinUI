using System;
using System.IO;
using System.Linq;

using Model.Validation;
using ModelConsole.ModelData;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 025 — JSON Schemas for both model representations: the shipped
   /// schemas (Schemas/array.schema.json, Schemas/grouped.schema.json)
   /// describe the array and grouped formats, every shipped sample validates
   /// against its own representation's schema, and a deliberately-invalid
   /// document produces violations instead of passing silently. Validation is
   /// a warning channel (R8 grace) — it never throws, and a document that is
   /// not JSON is itself a reported violation.
   /// </summary>
   public class SchemaValidationTests
   {

      private static string SchemasPath => Path.Combine(AppContext.BaseDirectory, "Schemas");

      private static string ReadSchema(string file) =>
         File.ReadAllText(Path.Combine(SchemasPath, file));

      private static string SamplePath(SampleModel sample) =>
         Path.Combine(AppContext.BaseDirectory, "Samples", sample.FileName);

      [Theory]
      [InlineData("[ { \"TableName\": \"A\" } ]", ModelSchemaKind.Array)]
      [InlineData("{ \"dataSource\": \"d\", \"schemas\": [] }", ModelSchemaKind.Array)]
      [InlineData("{ \"schemas\": [ { \"name\": \"S\", \"tables\": [] } ] }", ModelSchemaKind.Array)]
      [InlineData("{ \"entities\": {} }", ModelSchemaKind.Grouped)]
      [InlineData("{ \"repository\": \"r\", \"schemas\": { \"s\": {} } }", ModelSchemaKind.Grouped)]
      [InlineData("{ \"schemas\": { \"s\": {} } }", ModelSchemaKind.Grouped)]
      [InlineData("42", ModelSchemaKind.None)]
      [InlineData("{ \"foo\": 1 }", ModelSchemaKind.None)]
      [InlineData("not json", ModelSchemaKind.None)]
      public void DetectKindSelectsByRootShape(string json, ModelSchemaKind expected)
      {
         Assert.Equal(expected, ModelSchemaValidator.DetectKind(json));
      }

      [Fact]
      public void EveryShippedSampleValidatesAgainstItsOwnSchema()
      {
         foreach (var sample in SampleModels.All)
         {
            string json = File.ReadAllText(SamplePath(sample));

            // The detector must agree with the profile the sample ships under.
            ModelSchemaKind expected =
               sample.Profile != null ? ModelSchemaKind.Grouped : ModelSchemaKind.Array;
            Assert.Equal(expected, ModelSchemaValidator.DetectKind(json));

            string schema = ReadSchema((sample.Profile ?? "array") + ".schema.json");
            var violations = ModelSchemaValidator.Validate(json, schema);
            Assert.True(
               violations.Count == 0,
               sample.FileName + " failed its schema: " + String.Join("; ", violations));
         }
      }

      [Fact]
      public void SamplesFailAgainstTheOtherRepresentationsSchema()
      {
         // The two schemas must actually distinguish the representations — a
         // schema that accepted everything would not be doing its job.
         foreach (var sample in SampleModels.All)
         {
            string json = File.ReadAllText(SamplePath(sample));
            string wrongSchema = sample.Profile != null ? "array" : "grouped";

            var violations = ModelSchemaValidator.Validate(json, ReadSchema(wrongSchema + ".schema.json"));
            Assert.NotEmpty(violations);
         }
      }

      [Fact]
      public void InvalidDocumentProducesViolations()
      {
         // An entity that is not an object is structurally wrong for the
         // grouped representation.
         var groupedViolations = ModelSchemaValidator.Validate(
            @"{ ""entities"": { ""Patient"": 42 } }", ReadSchema("grouped.schema.json"));
         Assert.NotEmpty(groupedViolations);

         // A containerized array document missing its tables is wrong for the
         // array representation.
         var arrayViolations = ModelSchemaValidator.Validate(
            @"{ ""schemas"": [ { ""name"": ""S"" } ] }", ReadSchema("array.schema.json"));
         Assert.NotEmpty(arrayViolations);

         // An unrecognized root shape is not a valid array or grouped document.
         Assert.NotEmpty(ModelSchemaValidator.Validate(
            @"{ ""foo"": 1 }", ReadSchema("array.schema.json")));
         Assert.NotEmpty(ModelSchemaValidator.Validate(
            @"{ ""foo"": 1 }", ReadSchema("grouped.schema.json")));
      }

      [Fact]
      public void TagsArrayValidatesAgainstBothSchemas()
      {
         // Backlog 037: both representations' schemas accept a tags array on
         // a table/entity, and reject a non-string tag.
         string arrayJson = """
            [ { "TableName": "Incident", "Tags": [ "Core", "uml" ], "Columns": [] } ]
            """;
         Assert.Empty(ModelSchemaValidator.Validate(arrayJson, ReadSchema("array.schema.json")));
         Assert.NotEmpty(ModelSchemaValidator.Validate(
            "[ { \"TableName\": \"A\", \"Tags\": [ 42 ] } ]", ReadSchema("array.schema.json")));

         string groupedJson = """
            { "entities": { "Incident": { "tags": [ "Core", "uml" ], "Elements": [] } } }
            """;
         Assert.Empty(ModelSchemaValidator.Validate(groupedJson, ReadSchema("grouped.schema.json")));
         Assert.NotEmpty(ModelSchemaValidator.Validate(
            "{ \"entities\": { \"A\": { \"tags\": [ 42 ] } } }", ReadSchema("grouped.schema.json")));
      }

      [Fact]
      public void UnparseableJsonIsReportedAsAViolationNotThrown()
      {
         // Validation never throws — even a document that is not JSON at all
         // comes back as a single violation on the warn channel.
         var violations = ModelSchemaValidator.Validate(
            "{ not json", ReadSchema("array.schema.json"));

         Assert.Single(violations);
         Assert.Contains("not valid JSON", violations[0]);
      }
   }

}
