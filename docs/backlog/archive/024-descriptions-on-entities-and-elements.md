# 024 — Descriptions on entities and elements

## Summary

Complete the Description story for the canonical model. An **Entity is not different from a Table**, and an **Element is not different from a Column** — both concepts are best complemented by a `Description` that explains what they are. `ColumnInfo.Description` already exists and round-trips in the array format; `TableInfo` has **no** Description. Add it, capture a `description` field per entity/element in the grouped representation, and surface descriptions in the readout (inspector/explorer).

## Goals

- [x] `TableInfo.Description` added (additive canonical member; `ColumnInfo.Description` already exists).
- [x] Interpreter captures a `description` field per entity (grouped) and it round-trips in the array format.
- [x] Inspector shows a table's description and a column's description (the 022 readout is the natural home).
- [x] Tests: description capture + round-trip + readout.

## Scope

**In scope:**
- `TableInfo.Description` (+ mapping-spec field for grouped capture).
- Inspector/explorer description display.
- Tests.

**Out of scope:**
- Editing descriptions (read-only display, like the 021/022 readout).
- v2 concepts.

## Approach / Notes

- `TableInfo`'s description should ride the existing JSON round-trip like `ColumnInfo.Description` does (part of the array format, not `[JsonIgnore]`).
- Grouped entities/elements that declare `"description": "…"` flow into the canonical member via the interpreter.
- Reuse the existing `ReadoutFormatter`/inspector patterns from 022.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**.
- [x] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass.
- [x] Inspecting a table/element with a description shows it.

## Status

- **State:** Completed
- **Sprint:** 2026-08-18 (descriptions on entities and elements)
- **Completed:** 2026-08-18
