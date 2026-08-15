using Model.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Data
{

   public class ColumnList : List<ColumnInfo>
   {

      public ColumnInfo Add(
         string schemaName, string columnName, 
         int size = 0, bool isKey = false, string type = null)
      {
         var c = new ColumnInfo();
         c.SchemaName = schemaName;
         c.ColumnName = columnName;
         c.Size = size;
         c.IsKey = isKey;
         c.Type = String.IsNullOrWhiteSpace(type) ? c.Type : type;
         this.Add(c);
         return c;
      }

   }

}
