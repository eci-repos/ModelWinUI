using System.Collections.Generic;
using Model.Data;

namespace Model.Interpretation
{

   /// <summary>
   /// Result of interpreting a JSON document through a mapping spec: the
   /// canonical model (tables, enumerations, provenance, model metadata) plus
   /// the resolution issues raised by the grounding rules R1–R8. The issues
   /// list is the diagnostic channel the app already uses for FK problems.
   /// </summary>
   public class ModelInterpretation
   {
      /// <summary>The canonical tables produced by the interpretation.</summary>
      public List<TableInfo> Tables { get; set; } = new List<TableInfo>();

      /// <summary>Named value-sets captured from the document (backlog 021 surfaces them).</summary>
      public Dictionary<string, Enumeration> Enumerations { get; set; } = new Dictionary<string, Enumeration>();

      /// <summary>Model-level provenance, when the document declared one (backlog 022).</summary>
      public Provenance Provenance { get; set; }

      /// <summary>Model-level metadata annotations, when the document declared one.</summary>
      public Dictionary<string, string> Metadata { get; set; }

      /// <summary>Resolution issues: ambiguous reads, dropped references, malformed input.</summary>
      public List<string> Issues { get; set; } = new List<string>();
   }

}
