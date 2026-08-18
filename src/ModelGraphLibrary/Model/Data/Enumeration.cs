using System;
using System.Collections.Generic;
using System.Linq;

namespace Model.Data
{

   /// <summary>
   /// One allowed value of an <see cref="Enumeration"/>. A code is the
   /// stable identifier; a label is the display text. When a source only
   /// provides the code, the label equals the code.
   /// </summary>
   public class EnumerationValue
   {
      public string Code { get; set; }
      public string Label { get; set; }
   }

   /// <summary>
   /// A named value-set (enumeration) modeled by the schema-driven
   /// interpreter. Canonical v1 concept; carried so the app can surface the
   /// allowed values (backlog 021) and read them out (backlog 022).
   /// </summary>
   public class Enumeration
   {
      public string Name { get; set; }
      public List<EnumerationValue> Values { get; set; } = new List<EnumerationValue>();

      /// <summary>
      /// Comma-separated value codes, for readout (backlog 021). The
      /// inspector shows this where an element resolves to an enumeration.
      /// </summary>
      public string ValueList => string.Join(", ", Values.Select(v => v.Code));
   }

}
