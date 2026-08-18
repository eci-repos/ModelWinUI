using System;

namespace Model.Data
{

   /// <summary>
   /// Model-level provenance: where the model came from, what version, and
   /// when it was loaded. Canonical v1 concept captured by the interpreter
   /// and surfaced by the readout (backlog 022).
   /// </summary>
   public class Provenance
   {
      public string Source { get; set; }
      public string Version { get; set; }
      public string LoadedAt { get; set; }
      public string Notes { get; set; }
   }

}
