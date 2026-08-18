# Sprint 2026-08-18 — JSON Schemas for model representations

> Executed copy of the sprint. Definition: `docs/backlog/archive/025-json-schemas-for-model-representations.md`.

## Dates

- **Start:** 2026-08-18
- **End:** 2026-08-18

## Scope

Backlog items in this sprint:

- [x] `025` — JSON Schemas for model representations (a JSON Schema per representation + load-time validation)

## Execution Log

- 2026-08-18 — Sprint defined from backlog item 025 (the node-enrichment follow-up series closes: `024` descriptions and `026` per-node provenance are done; `025` JSON Schemas is the final item). `026` is promoted. Not started.
- 2026-08-18 — Shipped `Schemas/array.schema.json` + `Schemas/grouped.schema.json` (draft 2020-12, each a `oneOf` over the flat and containerized forms; unknown fields allowed — R8 grace). `JsonSchema.Net` 9.4.0 added to ModelGraphLibrary; new `Model/Validation/ModelSchemaValidator` (`DetectKind` by root shape, `Validate` with per-schema-text caching to dodge the package's process-wide `$id` registration, leaf-error walk through `EvaluationResults.Details`). Both csprojs content-include `Schemas\*.json` (Samples pattern). `MainWindow`: Open Model and Open Sample read the text once, validate against the detected schema, and thread `schemaIssues` through `LoadModel` → `LogModelLoad` as "  schema: …" warn lines (never a hard block). Added 13 tests (`SchemaValidationTests`). Library + app build 0/0; full-solution `--no-incremental` 0/0; full suite 132/132 (was 119).

## Results

- **Completed:** `025`
- **Deferred:** none — the node-enrichment series (023–026, 025) is complete and the backlog is empty.
- **Notes:** The schema selection rule is the `schemas` container's JSON kind — the array format declares `schemas` as a list, the grouped format as an object keyed by schema name. Validation is a warning channel (R8 grace): an unparseable document is itself reported as a single violation rather than thrown, so Open Model's existing error dialog still owns truly-broken JSON. `JsonSchema.Net` registers built schemas in a process-wide registry keyed by `$id`, so the validator caches the parsed schema by its text — re-parsing the same schema on a second load would throw.
