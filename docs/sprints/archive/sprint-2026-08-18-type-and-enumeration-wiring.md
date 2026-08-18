# Sprint 2026-08-18 — Type + enumeration wiring

> Executed copy of the sprint. Definition: `docs/backlog/021-type-and-enumeration-wiring.md`.

## Dates

- **Start:** 2026-08-18
- **End:** 2026-08-18

## Scope

Backlog items in this sprint:

- [x] `021` — Type + enumeration wiring

## Execution Log

- 2026-08-18 — Sprint defined from backlog item 021; implementation started.
- 2026-08-18 — Plumbed the `Enumerations` dictionary through the load path (`Enumeration.ValueList`, `ModelPanelControl.Enumerations` + `SetModel` param, `ModelEditorControl.SetModel` + both `ShowTable` call sites, `MainWindow.LoadModel` + `OpenSample_Click`). Added the inspector's read-only value-set line under enum-typed columns (`BuildEnumReadout` — `"enum Gender: M, F, OTHER"`) and the explorer's `enum:<name>` column tag. Added 2 tests (enum columns resolve to real value-sets; `ValueList` formatting). Build 0/0; full suite 100/100 (was 98).

## Results

- **Completed:** `021`
- **Deferred:** `022` (cardinality/metadata/provenance readout in the inspector) — next sprint.
- **Notes:** The type map was already data-driven (no switch) and enumerations were already modeled; the work was plumbing + display. The array-format path (`ModelFile.Load`) passes null enumerations — no readout, no behavior change. Visual pass on the readout needs a manual look.
