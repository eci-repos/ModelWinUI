# 023 — Containerized model structure (Repository → Schema → Entities → Elements)

## Summary

Both model representations gain a 4-level container so the **schema is declared once**, not repeated on every entity/table:

- **Entity–Element (grouped) representation:** `Repository → Schema → Entities → Elements`.
- **Database (array) representation:** `Data Source → Schema → Tables → Columns`.

The container is a **source-format concern only**: the canonical hub stays `List<TableInfo>` and the interpreter *flattens* the container into it (filling `CatalogName`/`SchemaName` from the container), so the renderers, explorer, and routing never change. Flat forms remain readable for backward compatibility.

## Goals

- [x] Grouped format containerized: `repository → schemas → entities → elements`; each schema declares its name once, entities no longer carry a per-entity `schema`.
- [x] Array format containerized: `dataSource → schemas → tables → columns`; `ModelFile` reads **and writes** the container form (the flat array stays readable).
- [x] Interpreter auto-detects flat vs container (root shape) and flattens either; `CatalogInfo` is the Repository / Data Source level.
- [x] Shipped samples + fixtures (Healthcare, PublicSafety, Library) updated to the container form; round-trip tests updated.
- [x] Tests: a containerized sample loads with no schema repetition; flat forms still load unchanged; `ModelFile` round-trips the container.

## Scope

**In scope:**
- Container structure for both representations (grouped + array).
- `ModelFile` container read/write (flat array still accepted).
- Interpreter container detection + flattening; `CatalogInfo` wiring.
- Sample fixtures/shipped JSON + sample tests to the container form.

**Out of scope:**
- Changing the canonical `TableInfo`/`ColumnInfo` shape or the renderers/explorer (the flattened table list is unchanged).
- v2 concepts.
- Description fields (that is `024`), per-node provenance (`026`), JSON Schemas (`025`).

## Approach / Notes

- Suggested grouped container shape (the interpreter's existing object/array container auto-detection already applies):
  ```json
  {
    "repository": "Clinic",
    "schemas": {
      "clinic": {
        "entities": { "Patient": { "Elements": [...] } }
      }
    }
  }
  ```
- Suggested array container shape:
  ```json
  {
    "dataSource": "Clinic",
    "schemas": [ { "name": "clinic", "tables": [ ... ] } ]
  }
  ```
- The canonical `TableInfo.SchemaName`/`CatalogName` are filled from the container during flattening, so downstream code (readout, renderers) reads them exactly as before.
- Requires the mapping spec to declare the container paths (`RepositoryPath`/`DataSourcePath`/`SchemasPath`), extending `BuiltInProfiles`.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**.
- [x] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass.
- [x] A containerized sample loads with no per-entity/table schema repetition; the flat forms still load unchanged.
- [x] `ModelFile` round-trips the container form.

## Status

- **State:** Completed
- **Sprint:** 2026-08-18 (containerized model structure)
- **Completed:** 2026-08-18
