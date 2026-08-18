# 025 — JSON Schemas for both model representations

## Summary

Ship a **`Schemas/` folder** (parallel to the existing `Samples/` folder) holding one **JSON Schema per document structure**, and **validate any input model against its schema** at load time so an invalid document is caught up front. The schemas describe the *source formats* (not the canonical hub) and cover both the flat and the containerized forms from `023` — so old documents stay valid.

Folder contents (one schema per representation, shipped to the app/tests output `Schemas/`):

- `grouped.schema.json` — the Entity–Element (grouped) representation: flat `entities` root **and** the containerized `Repository → Schema → Entities → Elements` shape (via `oneOf`).
- `array.schema.json` — the database (array) representation: flat array **and** the containerized `Data Source → Schema → Tables → Columns` shape (via `oneOf`).

## Goals

- [ ] `ModelGraphLibrary/Schemas/grouped.schema.json` + `array.schema.json` (draft-07 or 2020-12), each allowing both flat and container forms.
- [ ] The `Schemas` folder is content-included by the app and test projects (mirroring the `Samples\*.json` pattern) so it lands in output `Schemas/`.
- [ ] **Load-time validation of any input model**: File → Open Model and Open Sample validate the raw JSON against the detected schema before interpretation; violations surface via the existing issues/log channel (warn, not hard-block — consistent with the interpreter's grace philosophy).
- [ ] `JsonSchema.Net` (pure C#, net10.0-friendly) in `ModelGraphLibrary` so the app, library, and tests share one validator.
- [ ] Tests: every shipped sample validates against its representation's schema; a deliberately-invalid document produces violations.

## Scope

**In scope:**
- The `Schemas/` folder + content includes in both csprojs.
- `JsonSchema.Net` reference in ModelGraphLibrary + a small schema-selection helper (pick schema by root shape).
- Load-path validation feeding the issues/log channel (File → Open Model, Open Sample).
- Tests (samples validate; invalid document flagged).

**Out of scope:**
- Schema generation from the canonical types (hand-authored schemas are fine).
- Hard-blocking invalid documents (graceful issue reporting is the v1 default; a strict mode can come later).
- v2 concepts.

## Approach / Notes

- Schema selection by root shape: an array root, or an object with `dataSource` (or `schemas` containing `tables`) → array schema; an object with `entities` or `repository` (or `schemas` containing `entities`) → grouped schema.
- Validation runs on the **raw document before interpretation** — the interpreter stays the single authority on semantics, the schema the single authority on shape.
- The schemas describe the formats after `023` (container) *and* before it (flat), so the shipped samples validate whether or not 023's container change has landed.
- Load-time validation reuses the existing issues channel (`ModelInterpretation.Issues` / the log), so no new UI — violations show up where R8 ambiguity issues already do.

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**.
- [ ] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass.
- [ ] Every shipped sample validates against its representation's schema; an invalid document surfaces schema violations at load.

## Status

- **State:** Planned
- **Sprint:** 2026-08-18 (JSON schemas for both representations)
- **Completed:** (TBD)
