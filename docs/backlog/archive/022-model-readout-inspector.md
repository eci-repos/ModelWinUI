# 022 — Model readout: cardinality, metadata, provenance

## Summary

Make the v1 canonical richness **visible**: the inspector/log surface the dependency details (per-side cardinality + optionality + role names), metadata annotations, and provenance that the renderer naturally ignores. This is the consumer for the extended model the interpreter emits (items `019`–`020`) — the proof that the new data is real and inspectable, not just carried.

## Goals

- [x] Inspector shows a dependency's cardinality (`min`/`max` per side), optionality, and role names.
- [x] Inspector (or log) shows an entity's/element's metadata annotations.
- [x] Model-level provenance (source, version, loaded-at) is visible somewhere stable (inspector header or log).
- [x] Tests: readout reads the extended model, not a frozen projection.

## Scope

**In scope:**
- Read-only display of the extended-model data already produced by `019`.
- Small additive inspector additions (no reshape of the model).
- A load-time log line summarizing resolved issues/cardinalities/provenance (reusing the existing diagnostics channel).

**Out of scope:**
- Editing cardinality/metadata/provenance.
- Drawing cardinality markers on the canvas (a future visualization, not part of the readout).
- v2 concepts.

## Approach / Notes

- The single source for the readout is the **extended model itself** — never a frozen render projection (risk #2 in the design doc).
- Reuse the existing `EntityInspectorControl` sections; keep additions additive and read-only.
- Provenance lands where a user can find it without hunting — the inspector header and a startup log line.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**.
- [x] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass.
- [x] Inspecting a dependency in the proof sample (`020`) shows per-side cardinality/optionality/roles; metadata and provenance are visible; a log line records provenance + resolution issues at load.

## Status

- **State:** Completed
- **Sprint:** 2026-08-18 (model readout: cardinality, metadata, provenance)
- **Completed:** 2026-08-18
