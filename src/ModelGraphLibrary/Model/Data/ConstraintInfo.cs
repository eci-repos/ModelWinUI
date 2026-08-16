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

      /// <summary>
      /// Parent (referenced) table for a foreign key. Set on FK constraints
      /// only; null for every other constraint type.
      /// </summary>
      public string ReferencedTableName { get; set; }

      /// <summary>
      /// Parent (referenced) column for a foreign key. When null the parent's
      /// primary key is used as the resolution default.
      /// </summary>
      public string ReferencedColumnName { get; set; }

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
