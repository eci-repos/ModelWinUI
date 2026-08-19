# 029 — Editable entities: columns, dependencies, and metadata from the inspector

> Series: object-oriented drawing. Part 3 — the deep edit surface: beyond the column-type editing that exists today, the user can rename an entity, add/remove columns, edit FK references, cardinality/roles, and entity metadata.

## Summary

Make the tables/entities genuinely editable. Today the inspector edits a column's data type and deletes connectors; this item extends it to the rest: rename an entity, add/remove columns, create/delete/edit FK references (target, per-side cardinality, optionality, role names), and edit entity/element metadata, description, and key marking. Every edit **mutates the canonical model** → re-validate (backlog 025) → re-render + partial re-route (backlog 013) — and a bad edit (unresolvable reference, ambiguity) surfaces a resolution issue (R8), never silent corruption.

## Goals

- [x] Inspector edits columns: rename, change type (existing), add/remove a column on an entity.
- [x] Inspector edits the dependency: create/delete an FK, change its target (`ReferencedTable`/`ReferencedColumn`), per-side cardinality + optionality, and child/parent role names.
- [x] Inspector edits entity metadata: description, the metadata bag, key marking, and (when carried) provenance — riding the existing readout fields.
- [x] Every edit flows **model → validation (025) → re-render + partial re-route (013)**; a deliberately-broken edit produces a resolution issue, never a crash or a dangling connector.

## Scope

**In scope:**
- The edit surface: inspector fields/commands for the operations above (read-only readouts become editable inputs).
- The pure edit operations on the canonical model (ModelGraphLibrary) + the edit→validate→render pipeline in the app.
- Reuse of the 013 partial re-route (only the changed entity's edges re-route) and the 022 readout discipline (inspector reads live objects).

**Out of scope:**
- Undo/redo (deferred — the model/view separation keeps it possible).
- Drag-and-drop editing directly on the canvas (inspector is the edit surface; canvas stays derived-from-state).
- Editing through the Skia renderer (Skia stays view-only for now).

## Approach / Notes

- The canvas is already **derived-from-state** (backlog 010): `_tables` + `_layout` are the source of truth and `Render()` re-draws everything. An edit is literally "mutate the model, then `Render()`" — the same path `DeleteConnector` and type-editing already use.
- Add/remove/rename is a canonical-model operation (a `TableInfo`/`ColumnInfo` add/remove/rename); the layout engine and `ErdComposer` recompose on the next render.
- Editing a dependency mutates the `ConstraintInfo` (target, cardinality, roles) — the connector regenerates from it (the "regenerate by default" principle, backlog 010).
- Validation (025) runs on every committed edit: an FK pointing at a removed entity, an ambiguous name, or a malformed cardinality lands on the existing issue channel (`  issue:` / `  schema:` log lines + R8), never a dangling connector.
- The edit verbs come from the 028 node surface (028 defines the verbs; 029 wires them to real model operations).

## Definition of Done

- [x] Rename/add/remove an entity; add/remove/rename columns; add/delete/edit FKs (target, cardinality, roles) — all from the inspector, all re-render correctly.
- [x] A deliberately-broken edit (dangling FK target, ambiguity) surfaces a resolution issue — no crash, no connector left pointing at nothing.
- [x] New tests cover the edit pipeline (mutate → validate → re-route) as pure model operations.
- [x] `dotnet build` → **0 errors, 0 warnings**; `dotnet test` → all pass.

## Status

- **State:** Completed
- **Sprint:** 2026-08-19
- **Completed:** 2026-08-19
