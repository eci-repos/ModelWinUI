using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Model.Data
{

   /// <summary>
   /// Loads and saves a whole model (a list of tables) as a JSON file.
   /// A model file is a JSON array of <see cref="TableInfo"/> objects, so it
   /// round-trips the full relational metadata including FK constraints.
   /// </summary>
   public static class ModelFile
   {

      /// <summary>
      /// Serialize a model to a JSON array of tables.
      /// </summary>
      public static string ToJson(IReadOnlyList<TableInfo> tables)
      {
         var options = new JsonSerializerOptions { WriteIndented = true };
         return JsonSerializer.Serialize(tables, options);
      }

      /// <summary>
      /// Deserialize a model from a JSON array of tables.
      /// </summary>
      public static IReadOnlyList<TableInfo> LoadJson(string json)
      {
         return JsonSerializer.Deserialize<List<TableInfo>>(json);
      }

      /// <summary>
      /// Load a model from a JSON file on disk.
      /// </summary>
      public static IReadOnlyList<TableInfo> Load(string path)
      {
         return LoadJson(File.ReadAllText(path));
      }

   }

}
