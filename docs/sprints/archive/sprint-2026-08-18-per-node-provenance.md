# Sprint 2026-08-18 — Per-node provenance

> Executed copy of the sprint. Definition: `docs/backlog/archive/026-per-node-provenance.md`.

## Dates

- **Start:** 2026-08-18
- **End:** 2026-08-18

## Scope

Backlog items in this sprint:

- [x] `026` — Per-node provenance (source/version on each table/column, not just the model)

## Execution Log

- 2026-08-18 — Sprint defined from backlog item 026 (the node-enrichment follow-up series continues: `024` descriptions is done, `026` per-node provenance is next, then `025` JSON Schemas). `024` is promoted.
- 2026-08-18 — `[JsonIgnore] Provenance` members added to `TableInfo` (also copied in `Copy`) and `ColumnInfo` — additive canonical members; provenance is a source concern, not part of the array JSON format. Mapping spec: `ProvenanceField` on the entity-container and element specs (default null; the Grouped profile opts in with `"provenance"`, the Array profile stays off — the array format never declares node provenance). `SchemaInterpreter` captures a per-entity/element provenance object via the existing `ReadProvenance`; a present-but-not-object provenance is an issue, never a silent drop (the model-level rule). Inspector: a table's provenance shows as a labeled "Provenance" section beside the metadata section; a column's provenance as a read-only gray line after its description/enum readouts (`BuildProvenanceReadout`), both via `ReadoutFormatter.Provenance`. Sample + fixture: Healthcare gains provenance on Patient (entity: `patient-registry.json` v1.1) and on the `name` element (`demographics-transform.json` v2.0) — different origins, the point of per-node provenance; byte-for-byte fixture sync kept. Added 3 tests (entity+element capture, malformed → issue, sample gate incl. readout line). Library + app build 0/0; full suite 119/119 (was 116).

## Results

- **Completed:** `026`
- **Deferred:** `025` (JSON Schemas for both representations) — the node-enrichment series' final item, in the next sprint.
- **Notes:** Per-node provenance reuses the existing `Provenance` type and `ReadoutFormatter.Provenance` summary line (source/version/loaded) unchanged — only the attachment point is new. Node provenance is `[JsonIgnore]`, so the array JSON format is unchanged (it never round-tripped provenance anyway). Visual pass on the inspector provenance section needs a manual look.
