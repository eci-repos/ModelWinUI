using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Data
{

   public class TableInfo
   {
      public string CatalogName { get; set; }
      public string SchemaName { get; set; }
      public string TableName { get; set; }
      public List<ColumnInfo> Columns { get; set; }

      /// <summary>
      /// Free-form metadata annotations attached to this entity. Canonical v1
      /// member; captured by the interpreter, surfaced by the readout
      /// (backlog 022). Ignored by the array JSON format.
      /// </summary>
      [JsonIgnore]
      public Dictionary<string, string> Metadata { get; set; }

      /// <summary>
      /// Unmodeled source fields preserved verbatim (the per-node extension
      /// bag). Captured by the interpreter when the mapping spec names them;
      /// ignored by the array JSON format.
      /// </summary>
      [JsonIgnore]
      public Dictionary<string, string> Extensions { get; set; }

      public void Copy(TableInfo table)
      {
         CatalogName = table.CatalogName;
         SchemaName = table.SchemaName;
         TableName = table.TableName;
         Columns = table.Columns;
      }

      public string ToJson()
      {
         var options = new JsonSerializerOptions { WriteIndented = true };
         return JsonSerializer.Serialize<TableInfo>(this, options);
      }

      public void ToJsonFile(string path)
      {
         var jtxt = ToJson();
         System.IO.File.WriteAllText(path, jtxt);
      }

      public static TableInfo FromJsonFile(string path)
      {
         string jsonText = System.IO.File.ReadAllText(path);
         return JsonSerializer.Deserialize<TableInfo>(jsonText);
      }

   }

}
