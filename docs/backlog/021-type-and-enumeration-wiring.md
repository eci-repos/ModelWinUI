# 021 — Type + enumeration wiring

## Summary

Wire the canonical type system and enumerations **end-to-end** in the app: the interpreter's type map (data, not code — gear 4) resolves schema type expressions to canonical types, enumerations (named value-sets) are modeled and resolve, and the app surfaces them — a column shows its resolved type and an enum shows its allowed values.

## Goals

- [ ] The interpreter's type map is data-driven (JSON), resolving arbitrary type strings/formats to canonical types.
- [ ] Enumerations are modeled (`Enumeration` type) and an element can resolve to an enum (value-set) reference.
- [ ] The app surfaces resolved types and enumeration values in the inspector/explorer (read-only; display only).
- [ ] Tests: type-map resolution, enum resolution, display.

## Scope

**In scope:**
- Data-driven canonical type map (in `ModelGraphLibrary`).
- `Enumeration` type + resolution from the mapping spec / schema.
- Minimal UI readout: inspector shows an element's resolved canonical type, and an enum's value list where the element resolves to one.
- Tests (pure + the app inspector reading the extended model).

**Out of scope:**
- Cardinality/optionality/metadata/provenance readout (that is `022`).
- Editing enums or types.
- v2 type concepts (money, temporal, units) beyond what the proof sample needs.

## Approach / Notes

- Requires `020`'s sample to include an enum and a mix of type expressions so the map is exercised by real data.
- Keep the inspector change minimal and additive — read the extended model, never reshape it.
- The type map lives as data (e.g. a shipped JSON or a built-in table), so adding types is data-only (gear 4).

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**.
- [ ] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass.
- [ ] Inspecting a column shows its resolved canonical type; inspecting an enum-typed element shows the value-set values.
- [ ] Type resolution is data-driven (no switch statement on types).

## Status

- **State:** Planned
- **Sprint:** (TBD)
- **Completed:** (TBD)
