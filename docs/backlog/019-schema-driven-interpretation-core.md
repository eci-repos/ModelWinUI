# 019 — Schema-driven interpretation core

## Summary

The first slice of **schema-driven model interpretation** (design: `docs/design/schema-driven-model-interpretation.md`): extend the canonical model with the v1 concepts, define the versioned mapping-spec format, and implement a pure interpreter in `ModelGraphLibrary` that reads an arbitrary JSON document through a mapping spec and emits the canonical model + resolution issues. This item bakes in extensibility gears 1, 2, 4, and 5.

## Goals

- [ ] Canonical model extended for v1 concepts: Entity/Element/Identity/Dependency carry the new v1 members (see design doc), plus sibling `Enumeration`/`Provenance` types.
- [ ] A versioned mapping-spec format (`specVersion`, tolerant reader — unknown sections inert, not errors).
- [ ] A pure interpreter core in `ModelGraphLibrary` applying the grounding rules R1–R8, with **R7 precedence** (declared roles beat inferred).
- [ ] Two built-in profiles: (a) the existing `ModelFile` array format — regression-covered, and (b) the grouped `$.entities` shape (object keyed by entity name or `entities` array).
- [ ] Term-synonym layer (Table↔Entity, Column↔Element, FK↔"Depends On", …).
- [ ] Diagnostics: resolution issues surfaced the way FK issues already are; a schema is valid iff R1–R5 resolve every required concept unambiguously.
- [ ] Tests: mapper unit tests + the existing `ModelFile`-format profiles reproduce current behavior.

## Scope

**In scope:**
- Extend the `Model.Data` POCOs **additively** (optional members + an `Extensions` bag on entity/element/dependency; the canonical-hub decision from the design doc — do not introduce a parallel type set).
- New sibling types: `Enumeration`, `Provenance`.
- The mapping-spec format and its reader (sidecar `.map.json` + built-in profiles; embedded `x-` annotations as the escape hatch).
- The pure interpreter over the v1 concepts (Entity, Element, Identity, Dependency with per-side cardinality/optionality/roles, Enumeration, Group, Metadata, Provenance).
- Resolution-issues diagnostics.
- Tests (pure `net10.0`, no WinUI).

**Out of scope:**
- The new proof sample (that is `020`).
- Type + enumeration *wiring into the app* (that is `021`; the interpreter's type map is data here).
- Inspector/log readout of the new concepts (`022`).
- v2 concepts: generalization, uniqueness/alternate keys, referential-integrity rules, stereotypes.
- Any renderer, explorer, or `LoadModel` changes.

## Approach / Notes

- Follow the design doc's canonical-hub decision: grow the existing POCOs, keep internal type names, document Entity/Element/Dependency as the canonical aliases.
- The rule registry is an **ordered list** (R1–R8) — v2 appends rules; v1 output must stay byte-identical (gear 5).
- The old `ModelFile` array format becomes the first built-in profile; existing `ModelFileTests` become its regression guard.
- The interpreter is pure (`System.Text.Json` DOM), Windows.Foundation-free, sitting next to the Graph modules in `ModelGraphLibrary`.
- Deliverable shape: `interpreter(mapping spec, json) → canonical model + issues`.

## Definition of Done

- [ ] `dotnet build ModelGraphLibrary.csproj -c Debug` → **0 errors, 0 warnings**.
- [ ] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass (existing tests unchanged or updated only where the model additions require it).
- [ ] New interpreter tests: array profile, grouped profile, synonym mapping, R7 precedence case (declared beats inferred), ambiguous-name → issue not silent guess.
- [ ] Existing `ModelFile` load behavior preserved (regression).

## Status

- **State:** Planned
- **Sprint:** (TBD)
- **Completed:** (TBD)
