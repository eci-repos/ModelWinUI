using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Model.Data
{

   /// <summary>
   /// Loads and saves a whole model (a list of tables) as a JSON file.
   /// A model file is the database representation container (backlog 023):
   /// <c>{ "dataSource": …, "schemas": [ { "name": …, "tables": [ … ] } ] }</c>,
   /// with the schema declared once per group instead of on every table. The
   /// pre-container flat array of <see cref="TableInfo"/> stays readable for
   /// backward compatibility.
   /// </summary>
   public static class ModelFile
   {

      /// <summary>
      /// Serialize a model to the container form: tables grouped by schema under
      /// <c>schemas</c>, the data source (first non-null
      /// <see cref="TableInfo.CatalogName"/>) declared once. Per-table
      /// <c>SchemaName</c>/<c>CatalogName</c> are omitted — the container names
      /// them once.
      /// </summary>
      public static string ToJson(IReadOnlyList<TableInfo> tables)
      {
         tables ??= new List<TableInfo>();
         var dataSource = tables.FirstOrDefault(t => !string.IsNullOrEmpty(t.CatalogName))?.CatalogName;

         var node = new JsonObject();
         if (dataSource != null)
            node["dataSource"] = JsonValue.Create(dataSource);

         var schemas = new JsonArray();
         node["schemas"] = schemas;

         foreach (var group in tables.GroupBy(t => t.SchemaName))
         {
            var schema = new JsonObject();
            if (group.Key != null)
               schema["name"] = JsonValue.Create(group.Key);

            var tableNodes = new JsonArray();
            schema["tables"] = tableNodes;
            foreach (var table in group)
            {
               var tableNode = JsonSerializer.SerializeToNode(table);
               if (tableNode is JsonObject tableObject)
               {
                  tableObject.Remove("SchemaName");
                  tableObject.Remove("CatalogName");
               }
               tableNodes.Add(tableNode);
            }
            schemas.Add(schema);
         }

         return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
      }

      /// <summary>
      /// Deserialize a model from JSON. Accepts the container object form and
      /// the legacy flat array of tables.
      /// </summary>
      public static IReadOnlyList<TableInfo> LoadJson(string json)
      {
         using var doc = JsonDocument.Parse(json);
         var root = doc.RootElement;
         switch (root.ValueKind)
         {
            case JsonValueKind.Array:
               return ReadFlat(root);
            case JsonValueKind.Object:
               return ReadContainer(root);
            default:
               throw new JsonException("a model file must be a JSON array of tables or a container object.");
         }
      }

      /// <summary>
      /// Load a model from a JSON file on disk.
      /// </summary>
      public static IReadOnlyList<TableInfo> Load(string path)
      {
         return LoadJson(File.ReadAllText(path));
      }

      private static IReadOnlyList<TableInfo> ReadFlat(JsonElement root)
      {
         var list = new List<TableInfo>();
         foreach (var element in root.EnumerateArray())
         {
            if (element.Deserialize<TableInfo>() is TableInfo table)
               list.Add(table);
         }
         return list;
      }

      /// <summary>
      /// Read the container form, restoring each table's schema (and data
      /// source) from the container level. A per-table value survives only when
      /// the container does not name one.
      /// </summary>
      private static IReadOnlyList<TableInfo> ReadContainer(JsonElement root)
      {
         var list = new List<TableInfo>();
         var dataSource = ReadScalar(root, "dataSource");
         if (!root.TryGetProperty("schemas", out var schemas) ||
             schemas.ValueKind != JsonValueKind.Array) return list;

         foreach (var schemaElement in schemas.EnumerateArray())
         {
            if (schemaElement.ValueKind != JsonValueKind.Object) continue;
            var schemaName = ReadScalar(schemaElement, "name");
            if (!schemaElement.TryGetProperty("tables", out var tables) ||
                tables.ValueKind != JsonValueKind.Array) continue;

            foreach (var tableElement in tables.EnumerateArray())
            {
               if (tableElement.Deserialize<TableInfo>() is not TableInfo table) continue;
               if (schemaName != null) table.SchemaName = schemaName;
               if (dataSource != null) table.CatalogName = dataSource;
               list.Add(table);
            }
         }
         return list;
      }

      private static string ReadScalar(JsonElement element, string name)
      {
         if (!element.TryGetProperty(name, out var value)) return null;
         return value.ValueKind switch
         {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
         };
      }

   }

}
