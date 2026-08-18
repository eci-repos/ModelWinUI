# Sprint 2026-08-18 — Schema-driven interpretation core

> Executed copy of the sprint. Definition: `docs/backlog/019-schema-driven-interpretation-core.md`.

## Dates

- **Start:** 2026-08-18
- **End:** (TBD)

## Scope

Backlog items in this sprint:

- [x] `019` — Schema-driven interpretation core

## Execution Log

- 2026-08-18 — Sprint defined from backlog item 019; implementation started.
- 2026-08-18 — Implemented: canonical v1 model members (Enumeration/Provenance siblings; `[JsonIgnore]` cardinality/roles/enum/metadata/extension members on the existing POCOs so the array format stays byte-stable); versioned `MappingSpec` + tolerant reader; `SchemaInterpreter` (R1–R8, R7 precedence, R8 ambiguity→issue, cardinality/roles, type map, metadata/provenance/enumerations, extension bags); two built-in profiles (array = `ModelFile` regression, grouped = `$.entities` object-or-array); 16 interpreter tests. Build 0/0; full suite 88/88 (was 72).

## Results

- **Completed:** `019`
- **Deferred:** `020`, `021`, `022` (next sprints — the `020` gate runs the grouped profile against a third-party-style model with no code updates)
- **Notes:** Interpreter output feeds `FkEdgeExtractor` unchanged (dependencies are FK `ConstraintInfo` on the element's column). No renderer/explorer/`LoadModel` changes.
