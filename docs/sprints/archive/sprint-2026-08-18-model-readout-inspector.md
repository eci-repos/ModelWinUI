# Sprint 2026-08-18 — Model readout: cardinality, metadata, provenance

> Executed copy of the sprint. Definition: `docs/backlog/022-model-readout-inspector.md`.

## Dates

- **Start:** 2026-08-18
- **End:** 2026-08-18

## Scope

Backlog items in this sprint:

- [x] `022` — Model readout: cardinality, metadata, provenance

## Execution Log

- 2026-08-18 — Sprint defined from backlog item 022; implementation started.
- 2026-08-18 — Added the pure `ReadoutFormatter` (cardinality/optionality, roles, metadata lines, provenance) and made `FkRelation` carry its source `ConstraintInfo`. Inspector: `ShowConnector` shows cardinality + roles, `ShowTable` shows a Metadata section, new `ShowModel` is the idle state. Plumbed provenance/metadata/issues through `LoadModel` + `ModelEditorControl.SetModel`; `MainWindow.LogModelLoad` writes the load-time log line. Added 7 tests (4 formatter + 3 sample). Build 0/0; full suite 107/107 (was 100).

## Results

- **Completed:** `022`
- **Deferred:** none — the schema-driven interpretation series (019–022) is complete; the backlog is empty. v2 (generalization, uniqueness, referential-integrity, stereotypes) is designed-for, not built.
- **Notes:** The readout always reads the live model objects (the backlog's risk #2) — `ReadoutFormatter` is pure and shared by the inspector and the tests. The array-format path (`ModelFile.Load`) passes null provenance/metadata/issues — no readout, no log extras, no behavior change. Visual pass on the readout needs a manual look.
