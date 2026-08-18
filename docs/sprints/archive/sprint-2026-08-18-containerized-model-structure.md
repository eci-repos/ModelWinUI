# Sprint 2026-08-18 — Containerized model structure

> Executed copy of the sprint. Definition: `docs/backlog/archive/023-containerized-model-structure.md`.

## Dates

- **Start:** 2026-08-18
- **End:** 2026-08-18

## Scope

Backlog items in this sprint:

- [x] `023` — Containerized model structure (Repository → Schema → Entities → Elements / Data Source → Schema → Tables → Columns)

## Execution Log

- 2026-08-18 — Sprint defined from backlog items 023–026 (schema-driven interpretation follow-up series). `023` is the structural foundation; `024`/`026` enrich the nodes; `025` validates the containerized forms.
- 2026-08-18 — Mapping spec: `SchemasSpec` (Path/NameField/EntitiesField) + `RepositoryPath`; both built-in profiles configured (Array `$.dataSource`/`$.schemas`/`tables`, Grouped `$.repository`/`$.schemas`/`entities`). Interpreter auto-detects containerized vs flat, flattens into the canonical `List<TableInfo>` filling `SchemaName`, captures the Repository/Data Source into `ModelInterpretation.Catalog` (a `CatalogInfo`). `ModelFile` writes/reads `{ dataSource, schemas: [{ name, tables }] }` (flat array stays readable). Shipped samples + fixtures containerized (Healthcare authored as `repository`/`schemas`/`entities`; PublicSafety/Library regenerated from `ModelFile.ToJson` via a one-off generator test). Added 5 container tests. Build 0/0; full suite 112/112 (was 107).

## Results

- **Completed:** `023`
- **Deferred:** `024` (descriptions on entities/elements), `026` (per-node provenance), `025` (JSON Schemas for both representations) — the sprint's remaining scope, in the next sprint.
- **Notes:** The container is a source-format concern only — the canonical hub stays `List<TableInfo>`, the interpreter flattens the container filling `CatalogName`/`SchemaName`, so the renderers, explorer, and routing never change. Flat forms remain readable for backward compatibility (`SchemaInterpreterTests` flat-profile tests and `ModelFileTests` legacy paths are untouched). Visual pass on the containerized sample needs a manual look.