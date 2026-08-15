using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Data
{

   public class ConstraintInfo
   {
      public string SchemaName { get; set; }
      public string TableName { get; set; }
      public string ColumnName { get; set; }
      public string Description { get; set; }
      public string Type { get; set; } = null;

      public bool IsKey
      {
         get { return Type == DataInfo.PRIMARY_KEY; }
      }
      public bool IsForeignKey
      {
         get { return Type == DataInfo.FOREIGN_KEY; }
      }
   }

}
