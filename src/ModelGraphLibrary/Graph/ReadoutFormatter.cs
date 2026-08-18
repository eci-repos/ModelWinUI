using System.Collections.Generic;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// Formats the extended-model data (dependency cardinality/roles, metadata
   /// annotations, provenance) for the readout (backlog 022). Pure and
   /// deterministic so the inspector and the tests share one source of truth
   /// — the readout always reads the live model, never a frozen projection.
   /// </summary>
   public static class ReadoutFormatter
   {

      /// <summary>
      /// A dependency's child-side cardinality with its optionality folded in:
      /// "1..1 (required)", "0..1 (optional)", "1..* (required)", "0..* (optional)".
      /// Null when neither bound is set. Optionality is implied by the minimum
      /// (0 = optional, 1 = required).
      /// </summary>
      public static string Cardinality(ConstraintInfo constraint)
      {
         if (constraint == null) return null;
         int? min = constraint.MinCardinality;
         int? max = constraint.MaxCardinality;
         if (min == null && max == null) return null;

         string minText = min?.ToString() ?? "0";
         string maxText = max == null ? "*" : max.ToString();
         string text = minText + ".." + maxText;
         if (min == 0) text += " (optional)";
         else if (min == 1) text += " (required)";
         return text;
      }

      /// <summary>
      /// A dependency's role names: "admitting → admits" (child role → parent
      /// role). A single role is returned alone; null when neither is set.
      /// </summary>
      public static string Roles(ConstraintInfo constraint)
      {
         if (constraint == null) return null;
         bool hasChild = !string.IsNullOrEmpty(constraint.ChildRole);
         bool hasParent = !string.IsNullOrEmpty(constraint.ParentRole);
         if (!hasChild && !hasParent) return null;
         if (hasChild && hasParent) return constraint.ChildRole + " → " + constraint.ParentRole;
         return hasChild ? constraint.ChildRole : constraint.ParentRole;
      }

      /// <summary>
      /// "key: value" lines for a metadata dictionary, in insertion order.
      /// Empty when the dictionary is null or empty.
      /// </summary>
      public static IReadOnlyList<string> MetadataLines(
         IReadOnlyDictionary<string, string> metadata)
      {
         var lines = new List<string>();
         if (metadata != null)
         {
            foreach (var kv in metadata)
            {
               lines.Add(kv.Key + ": " + kv.Value);
            }
         }
         return lines;
      }

      /// <summary>
      /// A one-line provenance summary: "source: clinic-schema.json · version:
      /// 1.0 · loaded: 2026-08-18". Null when the provenance is null or has no
      /// populated fields.
      /// </summary>
      public static string Provenance(Provenance provenance)
      {
         if (provenance == null) return null;
         var parts = new List<string>();
         if (!string.IsNullOrEmpty(provenance.Source)) parts.Add("source: " + provenance.Source);
         if (!string.IsNullOrEmpty(provenance.Version)) parts.Add("version: " + provenance.Version);
         if (!string.IsNullOrEmpty(provenance.LoadedAt)) parts.Add("loaded: " + provenance.LoadedAt);
         if (parts.Count == 0) return null;
         return string.Join(" · ", parts);
      }

   }

}
