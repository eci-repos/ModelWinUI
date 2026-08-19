# Model.Console.Data

The relational metadata model: `CatalogInfo` / `SchemaInfo` / `TableInfo` /
`ColumnInfo` / `ColumnList` / `ConstraintInfo` POCOs that round-trip through
JSON (`ToJson` / `FromJsonFile`), plus the database **container form**
`{ dataSource, schemas: [{ name, tables }] }` handled by `ModelFile` (schema
declared once; the legacy flat array of tables stays readable). Ships schema
interpretation (`ModelInterpretation` / `SchemaInterpreter` with built-in
profiles), load-time JSON Schema validation (`ModelSchemaValidator`), and the
shipped sample fixtures under `Samples/`.

**Dependencies:** `JsonSchema.Net`.

**Usage**

```csharp
var tables = ModelFile.Load("model.json");   // container or flat form
var json = ModelFile.ToJson(tables);         // round-trips exactly
```
