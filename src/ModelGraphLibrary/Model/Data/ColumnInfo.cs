using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Model.Data
{

   public struct DataInfo
   {
      public const string VARCHAR = "VARCHAR";
      public const string PRIMARY_KEY = "PK";
      public const string FOREIGN_KEY = "FK";
   }

   public class ColumnInfo
   {
      public string SchemaName { get; set; }
      public string ColumnName { get; set; }
      public string Description { get; set; }
      public int OrdinalPosition { get; set; }
      public string Type { get; set; } = DataInfo.VARCHAR;
      public int Size { get; set; } = 256;

      public bool IsNullable { get; set; } = true;
      public bool IsIdentity { get; set; } = false;
      public bool IsKey { get; set; } = false;
      public bool IsForeignKey { get; set; } = false;

      public List<ConstraintInfo> Constraints { get; set; } =
         new List<ConstraintInfo>();

      /// <summary>
      /// Optional canonical enumeration (value-set) this column resolves to.
      /// Carried by the interpreter (backlog 021); not part of the array
      /// JSON format (JsonIgnore keeps that format byte-stable).
      /// </summary>
      [JsonIgnore]
      public string EnumerationName { get; set; }

      /// <summary>
      /// Unmodeled source fields preserved verbatim (the per-node extension
      /// bag). Captured by the interpreter when the mapping spec names them;
      /// ignored by the array JSON format.
      /// </summary>
      [JsonIgnore]
      public Dictionary<string, string> Extensions { get; set; }

      /// <summary>
      /// Add Constraint.
      /// </summary>
      /// <param name="constraint">constraint to add</param>
      public void Add(ConstraintInfo constraint)
      {
         IsKey = constraint.IsKey;
         IsForeignKey = constraint.IsForeignKey;

         Constraints.Add(constraint);
      }

   }

}
