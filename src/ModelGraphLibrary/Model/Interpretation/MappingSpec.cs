using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Model.Interpretation
{

   /// <summary>
   /// A versioned mapping spec: a declaration of how a JSON document maps to
   /// the canonical model. It is data, not code — adding a profile or a field
   /// synonym is a data change (extensibility gears 2 and 4 in
   /// <c>docs/design/schema-driven-model-interpretation.md</c>).
   /// </summary>
   /// <remarks>
   /// Tolerant reader: an unknown <c>specVersion</c> or an unknown section is
   /// carried forward, not an error (the interpreter reports it as an issue
   /// and proceeds). Unknown properties are ignored by the deserializer.
   /// </remarks>
   public class MappingSpec
   {
      /// <summary>Version understood by this interpreter. Higher versions are read tolerantly.</summary>
      public const int CurrentSpecVersion = 1;

      /// <summary>Spec-format version. A value higher than <see cref="CurrentSpecVersion"/> is read tolerantly.</summary>
      public int SpecVersion { get; set; } = CurrentSpecVersion;

      /// <summary>Informational profile name (e.g. "grouped").</summary>
      public string Name { get; set; }

      /// <summary>How the entity container is located and how entities are named.</summary>
      public EntityContainerSpec Entities { get; set; }

      /// <summary>How the elements of an entity are read.</summary>
      public ElementSpec Elements { get; set; }

      /// <summary>
      /// Optional nested-constraint source (the canonical array format's
      /// per-column <c>Constraints</c> list). When present, PK/FK constraints
      /// are read from here instead of (or in addition to) the element-level
      /// fields.
      /// </summary>
      public ConstraintsSpec Constraints { get; set; }

      /// <summary>
      /// Data-driven type map: source type expression → canonical type. Empty
      /// means pass-through. Lookups are case-insensitive.
      /// </summary>
      public Dictionary<string, string> TypeMap { get; set; } = new Dictionary<string, string>();

      /// <summary>Naming conventions applied only when nothing is declared (R7 precedence).</summary>
      public ConventionsSpec Conventions { get; set; } = new ConventionsSpec();

      /// <summary>Optional JSON path (dot-separated, "$" = root) to a name → value-set object.</summary>
      public string EnumerationsPath { get; set; }

      /// <summary>Optional JSON path to the provenance object (source/version/loadedAt/notes).</summary>
      public string ProvenancePath { get; set; }

      /// <summary>Optional JSON path to a model-level metadata object (string → string).</summary>
      public string MetadataPath { get; set; }

      /// <summary>
      /// Optional JSON path to the Repository / Data Source name (a scalar, e.g.
      /// "$.repository" or "$.dataSource"). Captured into
      /// <see cref="Model.Data.CatalogInfo"/> during interpretation (backlog 023).
      /// </summary>
      public string RepositoryPath { get; set; }

      /// <summary>
      /// Optional schema container (the containerized form, backlog 023): the
      /// schema is declared once and every entity under it inherits it. When
      /// this path resolves, entities are read from each schema's entities
      /// field instead of the flat <see cref="Entities"/> path; documents
      /// without the container fall back to the flat form (backward compatible).
      /// </summary>
      public SchemasSpec Schemas { get; set; }

      /// <summary>
      /// Deserialize a spec from JSON. The reader is tolerant: unknown
      /// properties and unknown spec versions are preserved or ignored, never
      /// fatal.
      /// </summary>
      public static MappingSpec Parse(string json)
      {
         return JsonSerializer.Deserialize<MappingSpec>(json, JsonOptions);
      }

      /// <summary>
      /// Deserialize a spec from a sidecar <c>.map.json</c> file on disk.
      /// </summary>
      public static MappingSpec FromFile(string path)
      {
         return Parse(File.ReadAllText(path));
      }

      private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
      {
         PropertyNameCaseInsensitive = true,
      };
   }

   /// <summary>
   /// How the entity container is located and how entities are named.
   /// The interpreter reads the container's JSON kind itself: an object is
   /// treated as keyed-by-entity-name, an array as a list whose entities are
   /// named by <see cref="NameField"/>. <see cref="Kind"/> is therefore
   /// advisory documentation, not a switch.
   /// </summary>
   public class EntityContainerSpec
   {
      /// <summary>JSON path to the entity container ("$" = root, or "$.entities").</summary>
      public string Path { get; set; } = "$";

      /// <summary>"array" or "object". Advisory — the reader auto-detects by the container's JSON kind.</summary>
      public string Kind { get; set; } = "array";

      /// <summary>Field that names an entity (array-form only; object-form names by key).</summary>
      public string NameField { get; set; } = "name";

      /// <summary>Optional field on each entity carrying its schema/group name.</summary>
      public string SchemaField { get; set; }

      /// <summary>
      /// Optional field carrying the entity's description (backlog 024). An
      /// entity and an element are not different from a table and a column —
      /// both are complemented by a description. Absent means no description
      /// is captured.
      /// </summary>
      public string DescriptionField { get; set; } = "description";

      /// <summary>Field on each entity that holds its elements.</summary>
      public string ElementsField { get; set; } = "elements";

      /// <summary>Optional field on each entity holding its metadata annotation object.</summary>
      public string MetadataField { get; set; } = "metadata";

      /// <summary>
      /// Extra entity-level field names to preserve verbatim into the entity's
      /// extension bag (unmodeled source data, readable by the readout).
      /// </summary>
      public List<string> ExtensionFields { get; set; } = new List<string>();
   }

   /// <summary>
   /// How the schema container is located and how schemas are named (backlog
   /// 023 — the containerized form). The container may be an object keyed by
   /// schema name (grouped form) or an array of schema objects named by
   /// <see cref="NameField"/> (array form); the interpreter reads whichever
   /// kind it finds, mirroring the entity-container auto-detection. Each schema
   /// holds its entities under <see cref="EntitiesField"/>, so the schema name
   /// is declared once instead of on every entity.
   /// </summary>
   public class SchemasSpec
   {
      /// <summary>JSON path to the schema container (e.g. "$.schemas").</summary>
      public string Path { get; set; } = "$.schemas";

      /// <summary>Field naming a schema (array-form only; object-form names by key).</summary>
      public string NameField { get; set; } = "name";

      /// <summary>Field on each schema holding its entities/tables.</summary>
      public string EntitiesField { get; set; } = "entities";
   }

   /// <summary>
   /// How an entity's elements are mapped. Canonical concept aliases: an
   /// Element is a Column, an Identity is a key, a Dependency is a foreign
   /// key — whatever the source calls them.
   /// </summary>
   public class ElementSpec
   {
      /// <summary>Field naming the element.</summary>
      public string NameField { get; set; } = "name";

      /// <summary>Field carrying the element's type expression (run through the type map).</summary>
      public string TypeField { get; set; } = "type";

      /// <summary>
      /// Optional field carrying the element's description (backlog 024).
      /// Absent means no description is captured.
      /// </summary>
      public string DescriptionField { get; set; } = "description";

      /// <summary>Boolean field marking the element as the entity's identity (primary key).</summary>
      public string KeyField { get; set; } = "primaryKey";

      /// <summary>Boolean field marking the element nullable. Absent defaults to true.</summary>
      public string NullableField { get; set; } = "nullable";

      /// <summary>Field whose value names the parent entity this element depends on (a dependency reference).</summary>
      public string RefField { get; set; }

      /// <summary>Optional field on the element naming the parent column of the reference (null = parent's identity).</summary>
      public string RefColumnField { get; set; }

      /// <summary>Optional field naming the enumeration (value-set) this element resolves to.</summary>
      public string EnumField { get; set; }

      /// <summary>Optional cardinality expression ("1", "N", "1:N", "0..*") on the element's dependency.</summary>
      public string CardinalityField { get; set; } = "cardinality";

      /// <summary>Optional role name for the child (departing) side of the dependency.</summary>
      public string ChildRoleField { get; set; }

      /// <summary>Optional role name for the parent (referenced) side of the dependency.</summary>
      public string ParentRoleField { get; set; }

      /// <summary>
      /// Field holding the element's "value". Used by the reference-by-value
      /// convention (a value matching an entity name reads as a dependency).
      /// </summary>
      public string ValueField { get; set; } = "value";

      /// <summary>
      /// Extra element-level field names to preserve verbatim into the
      /// element's extension bag (unmodeled source data, readable by the
      /// readout).
      /// </summary>
      public List<string> ExtensionFields { get; set; } = new List<string>();
   }

   /// <summary>
   /// The nested-constraint source: a per-element list of constraint objects,
   /// each typed PK or FK. This is the canonical array format's mechanism.
   /// </summary>
   public class ConstraintsSpec
   {
      /// <summary>Field on the element holding the constraint list.</summary>
      public string Field { get; set; } = "Constraints";

      /// <summary>Field on each constraint carrying its type.</summary>
      public string TypeField { get; set; } = "Type";

      /// <summary>Type value meaning primary key.</summary>
      public string KeyValue { get; set; } = "PK";

      /// <summary>Type value meaning foreign key.</summary>
      public string FkValue { get; set; } = "FK";

      /// <summary>Field on an FK constraint naming the parent entity.</summary>
      public string RefTableField { get; set; } = "ReferencedTableName";

      /// <summary>Field on an FK constraint naming the parent column (optional).</summary>
      public string RefColumnField { get; set; } = "ReferencedColumnName";
   }

   /// <summary>
   /// Naming conventions applied only when nothing is declared (R7 — declared
   /// beats inferred). A convention is a fallback, never a guess the author
   /// did not opt into.
   /// </summary>
   public class ConventionsSpec
   {
      /// <summary>An element named exactly this is the identity when no key is declared (null disables).</summary>
      public string IdentityName { get; set; } = "Id";

      /// <summary>
      /// An element whose name ends with this suffix references the entity of
      /// the same base name when no dependency is declared (e.g. "AuthorId" →
      /// "Author"). Null disables.
      /// </summary>
      public string ReferenceSuffix { get; set; } = "Id";

      /// <summary>
      /// When true, an element whose <c>value</c> equals an entity name is read
      /// as a dependency to that entity. Such a read is always reported as an
      /// ambiguity issue (declare an explicit reference to disambiguate).
      /// </summary>
      public bool ReferenceByValue { get; set; }
   }

}
