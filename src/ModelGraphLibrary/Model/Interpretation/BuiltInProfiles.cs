using System.Collections.Generic;
using Model.Data;

namespace Model.Interpretation
{

   /// <summary>
   /// The two built-in mapping profiles shipped with the interpreter:
   /// <list type="bullet">
   /// <item><see cref="Array"/> — the existing <c>ModelFile</c> JSON array
   /// format (a JSON array of <see cref="Model.Data.TableInfo"/>, FKs as
   /// nested constraint objects). The regression guard for the interpreter:
   /// interpreting an array file must reproduce <c>ModelFile.Load</c>.</item>
   /// <item><see cref="Grouped"/> — the grouped <c>$.entities</c> shape,
   /// authored in the Entity / Elements / "Depends On" vocabulary. The
   /// container may be an object keyed by entity name or an array of entities;
   /// the interpreter reads whichever kind it finds.</item>
   /// </list>
   /// A profile is data, not code — adding a profile is a data change.
   /// </summary>
   public static class BuiltInProfiles
   {

      /// <summary>The existing <c>ModelFile</c> array format as a mapping spec.</summary>
      public static MappingSpec Array { get; } = BuildArray();

      /// <summary>The grouped <c>$.entities</c> shape.</summary>
      public static MappingSpec Grouped { get; } = BuildGrouped();

      /// <summary>
      /// Resolve a profile by name ("array" | "grouped"). Unknown names return
      /// null so callers can fall back to a sidecar or an explicit error.
      /// </summary>
      public static MappingSpec FromName(string name)
      {
         switch (name?.ToLowerInvariant())
         {
            case "array": return Array;
            case "grouped": return Grouped;
            default: return null;
         }
      }

      private static MappingSpec BuildArray()
      {
         return new MappingSpec
         {
            Name = "array",
            SpecVersion = MappingSpec.CurrentSpecVersion,
            Entities = new EntityContainerSpec
            {
               Path = "$",
               Kind = "array",
               NameField = "TableName",
               SchemaField = "SchemaName",
               ElementsField = "Columns",
            },
            Elements = new ElementSpec
            {
               NameField = "ColumnName",
               TypeField = "Type",
               NullableField = "IsNullable",
               // Keys are read from the IsKey boolean and/or the nested
               // Constraints list (a declared PK constraint), whichever the
               // file uses.
               KeyField = "IsKey",
               RefField = null,    // FKs come from the nested Constraints list
               EnumField = null,
               CardinalityField = null,
               ChildRoleField = null,
               ParentRoleField = null,
               ValueField = null,
            },
            Constraints = new ConstraintsSpec
            {
               Field = "Constraints",
               TypeField = "Type",
               KeyValue = DataInfo.PRIMARY_KEY,
               FkValue = DataInfo.FOREIGN_KEY,
               RefTableField = "ReferencedTableName",
               RefColumnField = "ReferencedColumnName",
            },
            // No inference: the array format declares everything. Conventions
            // off keeps the profile byte-for-byte faithful to ModelFile.Load.
            Conventions = new ConventionsSpec
            {
               IdentityName = null,
               ReferenceSuffix = null,
               ReferenceByValue = false,
            },
         };
      }

      private static MappingSpec BuildGrouped()
      {
         return new MappingSpec
         {
            Name = "grouped",
            SpecVersion = MappingSpec.CurrentSpecVersion,
            Entities = new EntityContainerSpec
            {
               Path = "$.entities",
               Kind = "object",
               NameField = "name",
               SchemaField = "schema",
               ElementsField = "Elements",
               MetadataField = "metadata",
            },
            Elements = new ElementSpec
            {
               NameField = "name",
               TypeField = "type",
               KeyField = "primaryKey",
               NullableField = "nullable",
               // The grouped vocabulary's dependency synonym is "Depends On"
               // (the canonical example in the design doc).
               RefField = "Depends On",
               RefColumnField = "refColumn",
               EnumField = "enum",
               CardinalityField = "cardinality",
               ChildRoleField = "childRole",
               ParentRoleField = "parentRole",
               ValueField = "value",
            },
            // The grouped shape's extras live at the document root. A document
            // without them is still valid — the interpreter treats a missing
            // optional section as silent (only a present-but-malformed one is
            // an issue).
            EnumerationsPath = "$.enumerations",
            ProvenancePath = "$.provenance",
            MetadataPath = "$.metadata",
            TypeMap = new Dictionary<string, string>
            {
               // The type map is data (gear 4), not a switch statement.
               { "string", DataInfo.VARCHAR },
               { "int", "INT" },
               { "integer", "INT" },
               { "bool", "BOOL" },
               { "boolean", "BOOL" },
               { "datetime", "DATETIME" },
            },
            Conventions = new ConventionsSpec
            {
               IdentityName = "Id",
               ReferenceSuffix = "Id",
               ReferenceByValue = false, // opt-in per model; see the R8 tests
            },
         };
      }

   }

}
