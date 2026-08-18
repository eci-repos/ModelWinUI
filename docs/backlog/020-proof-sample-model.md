# 020 — Proof sample model (different domain, structure, vocabulary)

## Summary

The **gate** for the schema-driven interpretation work: author a new sample model that is deliberately different from public-safety and library — a different domain, a **grouped** structure (`$.entities`), and a different vocabulary (`Entity` / `Elements` / `Depends On`), written as a "third-party" model with real messiness. Loaded through item `019` with **no code updates**, it is the demonstration that the concept-equivalence works. Ships as a sample + tests.

## Goals

- [ ] A third sample model (≈ 8–15 entities) in a new domain, **not** public-safety or library.
- [ ] Grouped JSON shape (`$.entities`), authored in the `Entity` / `Elements` / `Depends On` vocabulary with term synonyms exercised.
- [ ] Deliberately messy: inconsistent type strings, at least one ambiguous name that exercises **R7 precedence** (declared beats inferred), and at least one dependency needing an explicit role (two dependencies to the same entity).
- [ ] Includes a small enumeration (a value-set) so the type system has something to resolve.
- [ ] Includes at least one metadata annotation and a provenance block (so `022`'s readout has data).
- [ ] Registered in `SampleModels` (appears in File → Open Sample), shipped as fixture-generated JSON, and covered by tests (loads, is valid, dependencies resolve, renders).

## Scope

**In scope:**
- A new fixture schema in `ModelGraphLibrary/ModelData/` mirroring the `PublicSafetySchema` / `LibrarySchema` builder pattern.
- Its grouped `$.entities` JSON generated via the new interpreter/profile (or a generator test), shipped to `ModelGraphLibrary/Samples/`.
- `SampleModels` registry entry + the built-in grouped profile wired so the sample loads through `019` untouched.
- Tests: loads + valid; `FkEdgeExtractor.Extract` (or the interpreter's dependency resolution) reports no issues; renders; no-code-updates assertion (no app renderer/explorer changes).

**Out of scope:**
- v2 concepts (no generalization/uniqueness/RI rules).
- The inspector readout of the new concepts (`022`).
- Any renderer or explorer changes.

## Approach / Notes

- **Author it as a third party, not as the interpreter's author:** the point is to prove the rules resolve a schema we did not design around. Include the ambiguous name case on purpose — if R7 handles it, the gate is honest; if not, that is a real finding that feeds back into `019` before `021`/`022` start.
- Keep the domain small enough that the generated JSON is human-inspectable.
- Reuse the existing "samples are generated from fixtures and kept in sync by a test" pattern (`SampleModelTests.ShippedJsonMatchesFixture`).

## Definition of Done

- [ ] Sample loads through `019` with no code updates to renderer/explorer/`LoadModel`.
- [ ] `dotnet test` → all pass (new sample tests + the existing suite).
- [ ] File → Open Sample lists and renders the new model (both renderers).
- [ ] `docs/WORKLOG.md` records whether the ambiguous-name case passed R7 (the gate result).

## Status

- **State:** Planned
- **Sprint:** (TBD)
- **Completed:** (TBD)
