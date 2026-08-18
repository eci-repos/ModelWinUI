using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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

      /// <summary>
      /// Minimum occurrences on the child (departing) side of a dependency:
      /// 0 when optional, 1 when required. Canonical v1 member; read by the
      /// interpreter, surfaced by the readout (backlog 022). Ignored by the
      /// array JSON format.
      /// </summary>
      [JsonIgnore]
      public int? MinCardinality { get; set; }

      /// <summary>
      /// Maximum occurrences on the child side of a dependency (null = the
      /// default upper bound, conventionally 1 for a single reference).
      /// </summary>
      [JsonIgnore]
      public int? MaxCardinality { get; set; }

      /// <summary>
      /// Role name for the child (departing) side of the dependency.
      /// </summary>
      [JsonIgnore]
      public string ChildRole { get; set; }

      /// <summary>
      /// Role name for the parent (referenced) side of the dependency.
      /// </summary>
      [JsonIgnore]
      public string ParentRole { get; set; }

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
