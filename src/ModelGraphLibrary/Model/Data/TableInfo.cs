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
