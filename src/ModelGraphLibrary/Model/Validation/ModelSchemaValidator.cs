using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;

using Json.Schema;

namespace Model.Validation
{

   /// <summary>
   /// Which built-in representation a document's root shape fits (backlog 025).
   /// The two shipped JSON Schemas (<c>Schemas/array.schema.json</c> and
   /// <c>Schemas/grouped.schema.json</c>) describe the two source formats; a
   /// document is validated against whichever one its root selects.
   /// </summary>
   public enum ModelSchemaKind
   {
      /// <summary>The <c>ModelFile</c> array format (flat array or dataSource → schemas → tables).</summary>
      Array,

      /// <summary>The grouped entity-element format (entities or repository → schemas → entities).</summary>
      Grouped,

      /// <summary>The document's root shape matches neither representation.</summary>
      None
   }

   /// <summary>
   /// Load-time validation of a model document against its representation's
   /// JSON Schema (backlog 025). Validation is a warning channel, never a
   /// hard block — a schema violation does not stop interpretation, mirroring
   /// the interpreter's R8 grace. <see cref="DetectKind"/> selects the schema
   /// by root shape; <see cref="Validate"/> evaluates a document against a
   /// schema's text and returns the violation messages (empty = valid).
   /// <para>The parsed <see cref="JsonSchema"/> is cached by its text: the
   /// package registers built schemas in a process-wide registry keyed by
   /// <c>$id</c>, so re-building the same schema on a second load would throw
   /// instead of returning the cached result.</para>
   /// </summary>
   public static class ModelSchemaValidator
   {

      private static readonly ConcurrentDictionary<string, JsonSchema> SchemaCache =
         new ConcurrentDictionary<string, JsonSchema>();

      /// <summary>
      /// Detect which representation's schema a document's root shape fits:
      /// <list type="bullet">
      /// <item>an array root → <see cref="ModelSchemaKind.Array"/></item>
      /// <item>an object whose <c>schemas</c> is an array (or that carries a
      /// <c>dataSource</c>) → <see cref="ModelSchemaKind.Array"/></item>
      /// <item>an object whose <c>schemas</c> is an object, or that carries
      /// <c>entities</c> or <c>repository</c> → <see cref="ModelSchemaKind.Grouped"/></item>
      /// <item>anything else (including unparseable JSON) → <see cref="ModelSchemaKind.None"/></item>
      /// </list>
      /// </summary>
      public static ModelSchemaKind DetectKind(string json)
      {
         try
         {
            using var doc = JsonDocument.Parse(json);
            return DetectKind(doc.RootElement);
         }
         catch (JsonException)
         {
            return ModelSchemaKind.None;
         }
      }

      private static ModelSchemaKind DetectKind(JsonElement root)
      {
         if (root.ValueKind == JsonValueKind.Array)
         {
            return ModelSchemaKind.Array;
         }
         if (root.ValueKind != JsonValueKind.Object)
         {
            return ModelSchemaKind.None;
         }

         // The schemas container's kind disambiguates the two containerized
         // forms (backlog 023): the array format declares schemas as a list,
         // the grouped format as an object keyed by schema name.
         if (root.TryGetProperty("schemas", out var schemas))
         {
            return schemas.ValueKind == JsonValueKind.Array
               ? ModelSchemaKind.Array
               : ModelSchemaKind.Grouped;
         }
         if (root.TryGetProperty("dataSource", out _))
         {
            return ModelSchemaKind.Array;
         }
         if (root.TryGetProperty("entities", out _) || root.TryGetProperty("repository", out _))
         {
            return ModelSchemaKind.Grouped;
         }
         return ModelSchemaKind.None;
      }

      /// <summary>
      /// Validate a document against a schema (the schema's JSON text). Returns
      /// the violation messages, or an empty list when the document is valid.
      /// Unparseable JSON is reported as a single violation, not thrown.
      /// </summary>
      public static IReadOnlyList<string> Validate(string json, string schemaJson)
      {
         var schema = SchemaCache.GetOrAdd(schemaJson, text => JsonSchema.FromText(text));

         try
         {
            using var doc = JsonDocument.Parse(json);
            var results = schema.Evaluate(doc.RootElement,
               new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (results.IsValid)
            {
               return Array.Empty<string>();
            }

            var violations = new List<string>();
            CollectErrors(results, violations);
            return violations;
         }
         catch (JsonException)
         {
            return new List<string> { "Document is not valid JSON." };
         }
      }

      /// <summary>
      /// JsonSchema.Net reports errors at the leaves of the evaluation tree
      /// (a root <c>Errors</c> map is empty even when evaluation failed); walk
      /// <c>Details</c> to gather every leaf message.
      /// </summary>
      private static void CollectErrors(EvaluationResults results, List<string> violations)
      {
         if (results.Errors != null)
         {
            foreach (var message in results.Errors.Values)
            {
               violations.Add(message);
            }
         }
         if (results.Details != null)
         {
            foreach (var detail in results.Details)
            {
               CollectErrors(detail, violations);
            }
         }
      }

   }

}
