# Sprint 2026-08-18 — Proof sample model

> Executed copy of the sprint. Definition: `docs/backlog/020-proof-sample-model.md`.

## Dates

- **Start:** 2026-08-18
- **End:** 2026-08-18

## Scope

Backlog items in this sprint:

- [x] `020` — Proof sample model (different domain, structure, vocabulary)

## Execution Log

- 2026-08-18 — Sprint defined from backlog item 020; implementation started.
- 2026-08-18 — Authored the grouped healthcare fixture (`HealthcareSchema`, 12 entities / 16 FKs, deliberately messy: inconsistent type strings, the R7 ambiguous name `Claim.PatientId`, two dependencies to the same entity with explicit roles, enumerations, metadata, provenance) and shipped `Samples/Healthcare.json`. Refined 019 for the gate: the grouped profile declares the enumerations/provenance/metadata paths and the `"Depends On"` dependency field; a missing optional section is now silent (present-but-malformed is still an issue). Wired the profile-aware load path (`SampleModel.Profile` + `MainWindow.OpenSample_Click`). Gate tests: loads with no issues, every entity has an identity, dependencies resolve (16 edges, no FK issues), renders through `ErdComposer.Compose`, R7 → Visit, distinct roles, extras captured, type map. Build 0/0; full suite 98/98 (was 88).

## Results

- **Completed:** `020`
- **Deferred:** `021` (type + enumeration wiring), `022` (cardinality/metadata/provenance readout) — next sprints.
- **Notes:** The gate passed — the ambiguous-name case resolved by R7 (declared beats inferred): `Claim.PatientId` → Visit, not Patient, with no ambiguity issue. The only app change was the profile-aware sample load path; renderers/explorer/`LoadModel` untouched.
