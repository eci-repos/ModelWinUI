using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Model.Data;

namespace Model.Interpretation
{

   /// <summary>
   /// Pure, Windows.Foundation-free interpreter that reads an arbitrary JSON
   /// document through a <see cref="MappingSpec"/> and emits the canonical
   /// model + resolution issues. It applies the grounding rules R1–R8 from
   /// <c>docs/design/schema-driven-model-interpretation.md</c>:
   /// <list type="bullet">
   /// <item>R1 container — locate the entity container by spec path.</item>
   /// <item>R2 identity — resolve the entity's identity (key) element.</item>
   /// <item>R3 key — declared key beats constraints beats convention.</item>
   /// <item>R4 reference-by-name — dependencies resolve by entity name.</item>
   /// <item>R5 cardinality/optionality — per-side occurrences/roles.</item>
   /// <item>R6 annotation — metadata/provenance/enumerations passthrough.</item>
   /// <item>R7 precedence — declared roles beat inferred ones.</item>
   /// <item>R8 grace — ambiguity becomes an issue, never a silent guess.</item>
   /// </list>
   /// The output feeds <see cref="ModelConsole.Graph.FkEdgeExtractor"/> directly:
   /// dependencies are emitted as FK <see cref="ConstraintInfo"/> entries on the
   /// element's column (null parent column ⇒ resolve to the parent's identity).
   /// </summary>
   public static class SchemaInterpreter
   {

      /// <summary>
      /// Interpret <paramref name="json"/> through <paramref name="spec"/>.
      /// Invalid JSON, a missing container, or an unresolvable reference never
      /// throws — each becomes an issue in the result.
      /// </summary>
      public static ModelInterpretation Interpret(string json, MappingSpec spec)
      {
         var result = new ModelInterpretation();
         if (spec == null)
         {
            result.Issues.Add("no mapping spec supplied; nothing interpreted.");
            return result;
         }
         if (spec.SpecVersion > MappingSpec.CurrentSpecVersion)
         {
            result.Issues.Add(
               $"mapping spec version {spec.SpecVersion} is newer than the " +
               $"interpreter's {MappingSpec.CurrentSpecVersion}; interpreting " +
               "best-effort (tolerant read).");
         }

         JsonDocument doc;
         try
         {
            doc = JsonDocument.Parse(json);
         }
         catch (JsonException ex)
         {
            result.Issues.Add($"document is not valid JSON: {ex.Message}");
            return result;
         }

         using (doc)
         {
            var root = doc.RootElement;
            ReadModelFields(root, spec, result);

            var slots = ReadEntitySlots(root, spec, result);
            if (slots == null) return result;

            BuildTables(slots, spec, result);
         }
         return result;
      }

      /// <summary>
      /// Read the document-level extras: enumerations, provenance, model
      /// metadata (R6). Each is optional and read tolerantly — a malformed
      /// section is an issue, never a failure.
      /// </summary>
      private static void ReadModelFields(JsonElement root, MappingSpec spec, ModelInterpretation result)
      {
         if (!string.IsNullOrEmpty(spec.EnumerationsPath))
         {
            var el = ResolvePath(root, spec.EnumerationsPath);
            if (el == null)
               result.Issues.Add($"enumerations container not found at '{spec.EnumerationsPath}'.");
            else if (el.Value.ValueKind == JsonValueKind.Object)
               ReadEnumerations(el.Value, result);
            else
               result.Issues.Add($"enumerations container at '{spec.EnumerationsPath}' is not an object.");
         }

         if (!string.IsNullOrEmpty(spec.ProvenancePath))
         {
            var el = ResolvePath(root, spec.ProvenancePath);
            if (el == null)
               result.Issues.Add($"provenance not found at '{spec.ProvenancePath}'.");
            else if (el.Value.ValueKind == JsonValueKind.Object)
               result.Provenance = ReadProvenance(el.Value);
            else
               result.Issues.Add($"provenance at '{spec.ProvenancePath}' is not an object.");
         }

         if (!string.IsNullOrEmpty(spec.MetadataPath))
         {
            var el = ResolvePath(root, spec.MetadataPath);
            if (el == null)
               result.Issues.Add($"metadata container not found at '{spec.MetadataPath}'.");
            else if (el.Value.ValueKind == JsonValueKind.Object)
               result.Metadata = ReadStringMap(el.Value, "model metadata");
            else
               result.Issues.Add($"metadata container at '{spec.MetadataPath}' is not an object.");
         }
      }

      /// <summary>
      /// First pass (R1): locate the entity container and collect (name, element)
      /// slots so dependency resolution (R4) can run against every known name.
      /// Returns null when the container cannot be resolved.
      /// </summary>
      private static List<EntitySlot> ReadEntitySlots(JsonElement root, MappingSpec spec, ModelInterpretation result)
      {
         var containerSpec = spec.Entities ?? new EntityContainerSpec();
         var container = ResolvePath(root, containerSpec.Path);
         if (container == null)
         {
            result.Issues.Add($"entities container not found at '{containerSpec.Path}'.");
            return null;
         }

         var slots = new List<EntitySlot>();
         switch (container.Value.ValueKind)
         {
            case JsonValueKind.Object:
               // Keyed form: one entity per property, named by its key.
               foreach (var prop in container.Value.EnumerateObject())
                  slots.Add(new EntitySlot { Name = prop.Name, Element = prop.Value });
               break;

            case JsonValueKind.Array:
               // List form: each item is named by the spec's name field.
               foreach (var item in container.Value.EnumerateArray())
               {
                  var name = ReadField(item, containerSpec.NameField);
                  if (string.IsNullOrEmpty(name))
                  {
                     result.Issues.Add("an entity in the container has no resolvable name; skipped.");
                     continue;
                  }
                  slots.Add(new EntitySlot { Name = name, Element = item });
               }
               break;

            default:
               result.Issues.Add($"entities container at '{containerSpec.Path}' is neither an object nor an array.");
               return null;
         }

         return slots;
      }

      /// <summary>
      /// Second pass: build a canonical table per slot, resolving each
      /// element's identity (R2/R3) and dependencies (R4) against the full
      /// entity-name set.
      /// </summary>
      private static void BuildTables(List<EntitySlot> slots, MappingSpec spec, ModelInterpretation result)
      {
         var entityNames = new HashSet<string>(
            slots.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);

         foreach (var slot in slots)
         {
            var table = BuildTable(slot, spec, entityNames, result);
            if (table != null) result.Tables.Add(table);
         }
      }

      private static TableInfo BuildTable(
         EntitySlot slot, MappingSpec spec, HashSet<string> entityNames, ModelInterpretation result)
      {
         var containerSpec = spec.Entities ?? new EntityContainerSpec();
         if (slot.Element.ValueKind != JsonValueKind.Object)
         {
            result.Issues.Add($"entity '{slot.Name}' is not an object; skipped.");
            return null;
         }

         var table = new TableInfo { TableName = slot.Name, Columns = new ColumnList() };
         if (!string.IsNullOrEmpty(containerSpec.SchemaField))
            table.SchemaName = ReadField(slot.Element, containerSpec.SchemaField);

         var elementsEl = FindField(slot.Element, containerSpec.ElementsField);
         if (elementsEl != null)
         {
            foreach (var pair in EnumerateElements(elementsEl.Value, spec, table.TableName, result))
            {
               var column = BuildElement(pair.Name, pair.Element, table, spec, entityNames, result);
               if (column != null) table.Columns.Add(column);
            }
         }

         if (!string.IsNullOrEmpty(containerSpec.MetadataField))
         {
            var metadataEl = FindField(slot.Element, containerSpec.MetadataField);
            if (metadataEl != null && metadataEl.Value.ValueKind == JsonValueKind.Object)
               table.Metadata = ReadStringMap(metadataEl.Value, $"entity '{slot.Name}'");
         }

         if (containerSpec.ExtensionFields != null && containerSpec.ExtensionFields.Count > 0)
         {
            var extensions = ReadFields(slot.Element, containerSpec.ExtensionFields);
            if (extensions.Count > 0) table.Extensions = extensions;
         }

         return table;
      }

      /// <summary>
      /// Yield the entity's elements as (name, element) pairs. Elements may be
      /// an array (each named by the spec's name field) or an object (each
      /// named by its key). A bare JSON string element stands for itself.
      /// </summary>
      private static IEnumerable<NameElement> EnumerateElements(
         JsonElement elementsEl, MappingSpec spec, string tableName, ModelInterpretation result)
      {
         var elementSpec = spec.Elements ?? new ElementSpec();
         switch (elementsEl.ValueKind)
         {
            case JsonValueKind.Array:
               foreach (var el in elementsEl.EnumerateArray())
               {
                  if (el.ValueKind == JsonValueKind.String)
                  {
                     yield return new NameElement { Name = el.GetString(), Element = el };
                     continue;
                  }
                  if (el.ValueKind != JsonValueKind.Object)
                  {
                     result.Issues.Add($"{tableName}: an element is neither an object nor a string; skipped.");
                     continue;
                  }
                  var name = ReadField(el, elementSpec.NameField);
                  if (string.IsNullOrEmpty(name))
                  {
                     result.Issues.Add($"{tableName}: an element has no name; skipped.");
                     continue;
                  }
                  yield return new NameElement { Name = name, Element = el };
               }
               break;

            case JsonValueKind.Object:
               foreach (var prop in elementsEl.EnumerateObject())
                  yield return new NameElement { Name = prop.Name, Element = prop.Value };
               break;

            default:
               result.Issues.Add($"{tableName}: elements are neither an array nor an object; ignored.");
               break;
         }
      }

      private static ColumnInfo BuildElement(
         string name, JsonElement element, TableInfo table, MappingSpec spec,
         HashSet<string> entityNames, ModelInterpretation result)
      {
         var elementSpec = spec.Elements ?? new ElementSpec();
         var column = new ColumnInfo { ColumnName = name };

         if (element.ValueKind == JsonValueKind.String)
         {
            // Bare element: the name is the element; type defaults.
            column.Type = DataInfo.VARCHAR;
         }
         else if (element.ValueKind == JsonValueKind.Object)
         {
            var type = ReadField(element, elementSpec.TypeField);
            if (type != null) column.Type = ApplyTypeMap(type, spec);

            var nullable = ReadField(element, elementSpec.NullableField);
            if (nullable != null)
            {
               if (bool.TryParse(nullable, out var isNullable)) column.IsNullable = isNullable;
               else result.Issues.Add(
                  $"{table.TableName}.{column.ColumnName}: nullable value '{nullable}' is not a boolean; defaulted to nullable.");
            }

            column.EnumerationName = ReadField(element, elementSpec.EnumField);
         }
         else
         {
            result.Issues.Add($"{table.TableName}: element '{name}' is neither an object nor a string; skipped.");
            return null;
         }

         // R2/R3 — identity resolution: declared key field, then a declared PK
         // constraint, then the identity-naming convention.
         var declaredKey =
            (elementSpec.KeyField != null && IsTrueField(element, elementSpec.KeyField)) ||
            (spec.Constraints != null && HasKeyConstraint(element, spec.Constraints));
         if (!declaredKey && spec.Conventions != null && spec.Conventions.IdentityName != null &&
             string.Equals(column.ColumnName, spec.Conventions.IdentityName, StringComparison.OrdinalIgnoreCase))
         {
            declaredKey = true;
         }
         if (declaredKey)
         {
            column.IsKey = true;
            column.Add(new ConstraintInfo
            {
               Type = DataInfo.PRIMARY_KEY,
               TableName = table.TableName,
               ColumnName = column.ColumnName,
            });
         }

         // R4 — dependencies. Declared sources first (nested constraints, then
         // the reference field); conventions apply only when nothing is declared
         // (R7 precedence).
         var dependencies = new List<DependencyRef>();
         if (spec.Constraints != null)
            dependencies.AddRange(ReadConstraintDependencies(element, spec.Constraints, table.TableName, column.ColumnName, result));

         if (elementSpec.RefField != null && TryReadField(element, elementSpec.RefField, out var refParent))
         {
            dependencies.Add(new DependencyRef
            {
               Name = refParent,
               ParentColumn = elementSpec.RefColumnField != null ? ReadField(element, elementSpec.RefColumnField) : null,
            });
         }

         if (dependencies.Count == 0 && spec.Conventions != null && spec.Conventions.ReferenceSuffix != null)
         {
            var inferred = ResolveSuffix(column.ColumnName, spec.Conventions.ReferenceSuffix, entityNames);
            if (inferred != null)
               dependencies.Add(new DependencyRef { Name = inferred, ParentColumn = null });
         }
         if (dependencies.Count == 0 && spec.Conventions != null && spec.Conventions.ReferenceByValue &&
             elementSpec.ValueField != null && TryReadField(element, elementSpec.ValueField, out var value) &&
             entityNames.Contains(value))
         {
            // R8 — a by-value read is an ambiguity: resolve, but say so.
            dependencies.Add(new DependencyRef { Name = value, ParentColumn = null });
            result.Issues.Add(
               $"{table.TableName}.{column.ColumnName}: value '{value}' matches an entity name and " +
               "was read as a dependency by convention. Declare an explicit reference to disambiguate.");
         }

         foreach (var dependency in dependencies)
         {
            if (!entityNames.Contains(dependency.Name))
            {
               result.Issues.Add(
                  $"{table.TableName}.{column.ColumnName}: dependency '{dependency.Name}' does not match a known entity; dropped.");
               continue;
            }

            var fk = new ConstraintInfo
            {
               Type = DataInfo.FOREIGN_KEY,
               TableName = table.TableName,
               ColumnName = column.ColumnName,
               ReferencedTableName = dependency.Name,
               ReferencedColumnName = dependency.ParentColumn,
            };

            // R5 — per-side cardinality/optionality and role names.
            var cardinality = elementSpec.CardinalityField != null ? ReadField(element, elementSpec.CardinalityField) : null;
            if (cardinality != null)
            {
               var parsed = ParseCardinality(cardinality);
               if (parsed != null)
               {
                  fk.MinCardinality = parsed.Min;
                  fk.MaxCardinality = parsed.Max;
               }
               else
               {
                  result.Issues.Add(
                     $"{table.TableName}.{column.ColumnName}: cardinality '{cardinality}' is not understood; left unset.");
               }
            }
            if (elementSpec.ChildRoleField != null) fk.ChildRole = ReadField(element, elementSpec.ChildRoleField);
            if (elementSpec.ParentRoleField != null) fk.ParentRole = ReadField(element, elementSpec.ParentRoleField);

            column.IsForeignKey = true;
            column.Constraints.Add(fk);
         }

         if (elementSpec.ExtensionFields != null && elementSpec.ExtensionFields.Count > 0)
         {
            var extensions = ReadFields(element, elementSpec.ExtensionFields);
            if (extensions.Count > 0) column.Extensions = extensions;
         }

         return column;
      }

      /// <summary>
      /// Read FK dependencies from the nested-constraint source (the canonical
      /// array format's <c>Constraints</c> list).
      /// </summary>
      private static IEnumerable<DependencyRef> ReadConstraintDependencies(
         JsonElement element, ConstraintsSpec constraintsSpec, string tableName, string columnName, ModelInterpretation result)
      {
         var list = FindField(element, constraintsSpec.Field);
         if (list == null || list.Value.ValueKind != JsonValueKind.Array) yield break;

         foreach (var constraint in list.Value.EnumerateArray())
         {
            if (constraint.ValueKind != JsonValueKind.Object) continue;
            var type = ReadField(constraint, constraintsSpec.TypeField);
            if (type == null || !string.Equals(type, constraintsSpec.FkValue, StringComparison.OrdinalIgnoreCase)) continue;

            var parent = ReadField(constraint, constraintsSpec.RefTableField);
            if (string.IsNullOrEmpty(parent))
            {
               result.Issues.Add($"{tableName}.{columnName}: a declared foreign key has no referenced table; skipped.");
               continue;
            }
            yield return new DependencyRef
            {
               Name = parent,
               ParentColumn = ReadField(constraint, constraintsSpec.RefColumnField),
            };
         }
      }

      private static bool HasKeyConstraint(JsonElement element, ConstraintsSpec constraints)
      {
         var list = FindField(element, constraints.Field);
         if (list == null || list.Value.ValueKind != JsonValueKind.Array) return false;
         foreach (var constraint in list.Value.EnumerateArray())
         {
            if (constraint.ValueKind != JsonValueKind.Object) continue;
            var type = ReadField(constraint, constraints.TypeField);
            if (type != null && string.Equals(type, constraints.KeyValue, StringComparison.OrdinalIgnoreCase))
               return true;
         }
         return false;
      }

      /// <summary>
      /// R4 by-convention: an element named "<c>base</c>+suffix" references the
      /// entity named "<c>base</c>" (e.g. "AuthorId" → "Author"). Returns null
      /// when there is no such entity.
      /// </summary>
      private static string ResolveSuffix(string elementName, string suffix, HashSet<string> entityNames)
      {
         if (elementName.Length <= suffix.Length ||
             !elementName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return null;
         var baseName = elementName.Substring(0, elementName.Length - suffix.Length);
         return entityNames.Contains(baseName) ? baseName : null;
      }

      private static string ApplyTypeMap(string type, MappingSpec spec)
      {
         if (spec.TypeMap == null || spec.TypeMap.Count == 0) return type;
         foreach (var entry in spec.TypeMap)
         {
            if (string.Equals(entry.Key, type, StringComparison.OrdinalIgnoreCase))
               return string.IsNullOrEmpty(entry.Value) ? type : entry.Value;
         }
         return type;
      }

      /// <summary>
      /// Parse a cardinality expression into (min, max). Supported forms:
      /// "1" → (1,1); "0"/"0:1"/"0..1" → (0,1); "N"/"*"/"1:N"/"0..N" → (0..1, unbounded).
      /// Unknown forms return null.
      /// </summary>
      private static Cardinality ParseCardinality(string value)
      {
         var v = value.Trim();
         if (v == "1") return new Cardinality { Min = 1, Max = 1 };
         if (v == "0" || v == "0:1" || v == "0..1") return new Cardinality { Min = 0, Max = 1 };
         if (v == "N" || v == "*" || v == "1:N" || v == "1..N" || v == "1:*" || v == "1..*" ||
             v == "0:N" || v == "0..N" || v == "0:*" || v == "0..*")
            return new Cardinality { Min = v.StartsWith("0") ? 0 : 1, Max = null };
         return null;
      }

      /// <summary>
      /// Resolve a dot-separated JSON path ("$" = root, "$.entities", ...).
      /// Returns null when the path does not resolve.
      /// </summary>
      private static JsonElement? ResolvePath(JsonElement root, string path)
      {
         if (string.IsNullOrEmpty(path) || path == "$") return root;
         var parts = path.TrimStart('$', '/', '.').Split('.', StringSplitOptions.RemoveEmptyEntries);
         var el = root;
         foreach (var part in parts)
         {
            if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(part, out var next))
               return null;
            el = next;
         }
         return el;
      }

      /// <summary>Find a named field on an object; null when absent or not an object.</summary>
      private static JsonElement? FindField(JsonElement el, string field)
      {
         if (string.IsNullOrEmpty(field) || el.ValueKind != JsonValueKind.Object) return null;
         return el.TryGetProperty(field, out var value) ? value : (JsonElement?)null;
      }

      /// <summary>Read a field as scalar text (string/number/bool → text; absent/other → null).</summary>
      private static string ReadField(JsonElement el, string field)
      {
         var found = FindField(el, field);
         return found.HasValue ? ScalarText(found.Value) : null;
      }

      private static bool TryReadField(JsonElement el, string field, out string value)
      {
         value = ReadField(el, field);
         return value != null;
      }

      private static bool IsTrueField(JsonElement el, string field)
      {
         var found = FindField(el, field);
         if (!found.HasValue) return false;
         if (found.Value.ValueKind == JsonValueKind.True) return true;
         if (found.Value.ValueKind == JsonValueKind.False) return false;
         return bool.TryParse(ScalarText(found.Value), out var parsed) && parsed;
      }

      /// <summary>
      /// Render a scalar JSON value as text: string → its value, number/bool →
      /// raw text, everything else → null.
      /// </summary>
      private static string ScalarText(JsonElement el)
      {
         switch (el.ValueKind)
         {
            case JsonValueKind.String: return el.GetString();
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False: return el.GetRawText();
            default: return null;
         }
      }

      private static void ReadEnumerations(JsonElement container, ModelInterpretation result)
      {
         foreach (var entry in container.EnumerateObject())
         {
            var enumeration = new Enumeration { Name = entry.Name };
            if (entry.Value.ValueKind != JsonValueKind.Array)
            {
               result.Issues.Add($"enumeration '{entry.Name}' is not a list of values; ignored.");
               continue;
            }
            foreach (var item in entry.Value.EnumerateArray())
            {
               if (item.ValueKind == JsonValueKind.String)
               {
                  enumeration.Values.Add(new EnumerationValue { Code = item.GetString(), Label = item.GetString() });
               }
               else if (item.ValueKind == JsonValueKind.Object)
               {
                  var code = FindField(item, "code") ?? FindField(item, "Code");
                  if (!code.HasValue || code.Value.ValueKind != JsonValueKind.String) continue;
                  var label = FindField(item, "label") ?? FindField(item, "Label");
                  enumeration.Values.Add(new EnumerationValue
                  {
                     Code = code.Value.GetString(),
                     Label = label.HasValue ? ScalarText(label.Value) : code.Value.GetString(),
                  });
               }
            }
            result.Enumerations[enumeration.Name] = enumeration;
         }
      }

      private static Provenance ReadProvenance(JsonElement el)
      {
         return new Provenance
         {
            Source = ReadField(el, "source"),
            Version = ReadField(el, "version"),
            LoadedAt = ReadField(el, "loadedAt"),
            Notes = ReadField(el, "notes"),
         };
      }

      private static Dictionary<string, string> ReadStringMap(JsonElement el, string context)
      {
         var map = new Dictionary<string, string>();
         foreach (var prop in el.EnumerateObject())
         {
            var text = ScalarText(prop.Value);
            if (text != null) map[prop.Name] = text;
         }
         return map;
      }

      /// <summary>
      /// Preserve a configured set of named fields into an extension bag,
      /// keeping only the fields that are present on the element.
      /// </summary>
      private static Dictionary<string, string> ReadFields(JsonElement el, List<string> names)
      {
         var bag = new Dictionary<string, string>();
         if (el.ValueKind != JsonValueKind.Object) return bag;
         foreach (var name in names)
         {
            var value = ReadField(el, name);
            if (value != null) bag[name] = value;
         }
         return bag;
      }

      private struct EntitySlot
      {
         public string Name;
         public JsonElement Element;
      }

      private struct NameElement
      {
         public string Name;
         public JsonElement Element;
      }

      private class DependencyRef
      {
         public string Name;
         public string ParentColumn;
      }

      private class Cardinality
      {
         public int? Min;
         public int? Max;
      }

   }

}
