# 026 — Per-node provenance (entity → element level)

## Summary

Provenance is currently **model-level only** (`ModelInterpretation.Provenance`). Make it available at the **entity → element (table → column) level** too, so a consumer can tell where a specific table or column came from. This extends the v1 `Provenance` concept down the node hierarchy and surfaces it in the 022 readout.

## Goals

- [x] `[JsonIgnore]` `Provenance` members on `TableInfo` and `ColumnInfo` (additive canonical members).
- [x] Interpreter captures per-node provenance from the source (e.g. a `provenance` field on an entity/element in the grouped representation).
- [x] Inspector surfaces table/column provenance (reusing the 022 readout pattern).
- [x] Tests: per-node provenance capture + readout.

## Scope

**In scope:**
- `Provenance` on `TableInfo`/`ColumnInfo` + mapping-spec path for grouped capture.
- Inspector display.
- Tests.

**Out of scope:**
- Editing provenance (read-only).
- Provenance *attribution* algorithms (which file/line each node came from is the source document's own declaration).
- v2 concepts.

## Approach / Notes

- Reuse the existing `Provenance` type (Source/Version/LoadedAt/Notes) unchanged; only the attachment point is new.
- The readout shows a node's provenance alongside its cardinality/metadata lines (022), so a table's "Metadata" section gains a provenance line when present.
- `[JsonIgnore]` keeps the array JSON format byte-stable unless a node provenance is explicitly part of the round-trip (open decision; lean additive).

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**.
- [x] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass.
- [x] A table/element that declares provenance shows it in the inspector.

## Status

- **State:** Completed
- **Sprint:** 2026-08-18 (per-node provenance)
- **Completed:** 2026-08-18
