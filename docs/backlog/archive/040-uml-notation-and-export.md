# 040 — Seamless UML: notation toggle, PlantUML export, import round-trip

## Summary

The capstone of the tag/group seam (037–039): because the model already carries everything UML needs — classes = entities, attributes = columns with types, associations = FKs with cardinality + role names, packages = tags, stereotypes = `TableKind` — **rendering or exporting the model in UML requires no remodeling**. This item delivers the "seamlessly UML it" promise: (1) a **UML notation toggle** in both renderers (package nodes from 039, class compartments, associations with multiplicity/role labels, stereotypes), (2) a **PlantUML text export** (package diagram + class diagram) that drops into any UML tooling, and (3) an **import path** that interprets a UML-ish grouped JSON (packages/stereotypes) back into tags through the existing interpreter (023). One model, two notations — ERD and UML — sharing the same geometry and routing.

## Goals

- [ ] The **UML profile** documented once (the mapping table) and enforced by one shared mapping module.
- [ ] UML notation rendering toggle in both renderers (class/association/package notation), defaulting to ERD view.
- [ ] PlantUML **package** + **class** diagram export as pure, deterministic, testable text.
- [ ] Association multiplicity + role labels from `ConstraintInfo.MinCardinality`/`MaxCardinality`/`ChildRole`/`ParentRole`; stereotypes from `TableKind` + tags.
- [ ] (Stretch) Import: grouped JSON with packages/stereotypes interpreted into tags via the 023 interpreter.
- [ ] **No remodeling:** the same model renders as ERD or UML; the tagged model (037) and the visibility/collapse state (038/039) drive the UML view unchanged.

## Scope

**In scope:**
- A portable UML mapping + PlantUML emitter (Model.Graph or Model.Data — pure, no UI), unit-tested.
- UML notation pass in both renderers: class compartments (name/attributes/associations), package nodes (reuse 039), multiplicity + role labels, stereotypes.
- A "UML view" toggle per renderer (view-side; ERD is the default).
- (Stretch) Interpreter extension: packages/stereotypes → tags.
- Docs/WORKLOG.

**Out of scope:**
- Full OMG UML/XMI export — noted as a future if a consumer needs it; PlantUML is the concrete seam.
- Code generation.
- Dependency closure ("show table + reachable within N hops") — still a future item.
- Attribute/operation modeling beyond columns-as-attributes (columns carry types/PK/FK; no synthetic operations).

## Approach / Notes

- **Canonical core, UML as a view (the lean-core rule):** the canonical ERD model is the single source of truth and stays lean — UML never enters it. The profile below is a *derived adapter* that reads the model at render/export time and writes nothing back. The JSON format gains nothing beyond `tags` (037): no `stereotypes`, `packages`, `visibility`, or `multiplicity` objects ever appear in the model file. Multiplicity/roles already exist as ERD-native `ConstraintInfo` members; stereotypes come from the derived `TableKind`. The UML notation toggle changes *rendering* only — never the model, never the serialized form.
- **The UML profile (document once, in Model.Graph):**
  | Model | UML |
  |---|---|
  | Entity (`TableInfo`) | **Class** (name `Schema::Table`, `<<entity>>`/`<<reference>>` stereotype from `TableKindClassifier`) |
  | Column | **Attribute** `name: type` with `{PK}` / `{FK}` tagged values |
  | FK (`ConstraintInfo`/`FkRelation`) | **Association** (directed), multiplicity from `Min/MaxCardinality`, role labels from `ChildRole`/`ParentRole` |
  | Tag | **Package** membership; surfaced as a stereotype/tagged value on the class |
  | Group (collapsed/expanded/hidden) | **Package** in a package diagram — the 039 box *is* the UML package node |
  | Visibility projection (038) | A package-diagram **viewpoint** |
- **PlantUML emitter (pure):** `UmlPlantEmitter.EmitPackageDiagram(tables, groups)` / `EmitClassDiagram(tables)` → PlantUML source strings. Deterministic (stable ordering — the repo bans nondeterministic output), testable with golden strings. Multiplicity formatting: `0..*`, `1`, `1..*` from `Min/MaxCardinality`; role labels omitted when null. Lives in a portable library (Model.Graph or Model.Data) so it is reusable and testable without a UI — consistent with the 030–034 layering.
- **UML notation toggle (renderers):** each renderer gains a "UML view" mode that swaps table→class-compartment rendering (name compartment + attribute rows, PK/FK markers already present) and group→package-node (reuse 039's `GroupBox`). Associations render with multiplicity/role labels instead of plain connector markers; the geometry/layout/routing stay the same — only the *look* of tables/boxes/edges changes, so no routing churn.
- **Import (stretch):** extend the 023 interpreter so a grouped JSON that declares packages (a `groups`/`packages` section or a per-entity `tags`) populates `TableInfo.Tags`; stereotypes like `<<reference>>` can drive `TableKind`. This closes the round trip: a UML-tool-exported JSON can be ingested as tags. **Flatten-and-discard:** packages → tags, stereotypes → at most a `TableKind` hint, UML-only attributes dropped or preserved best-effort in the existing `Extensions` bag — the model file never gains UML structure. Guarantee: a UML import → export cycle yields a valid, plain ERD model file (see DoD).
- **Relationships:** depends on 037 (tags), 038 (projection/viewpoints), 039 (package nodes). The mapping module is the single source both the renderers and the emitter read, so notation never drifts.
- **Tests:** emitter golden strings (package + class diagrams, cardinality formatting, stereotype emission), profile-mapping tests, and (stretch) grouped-JSON→tags round-trip. Manual: open the UML view in both renderers on the 50-table sample; PlantUML output opens in an external viewer.

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; `dotnet test tests/ModelConsole.Tests` → all pass.
- [ ] PlantUML package + class export from the 50-table tagged sample renders correctly in an external viewer; output is deterministic.
- [ ] UML view toggle in both renderers shows classes/associations/package nodes with stereotypes and multiplicity; ERD view is byte-for-byte today's drawing; toggling never re-routes.
- [ ] (Stretch) A UML-ish grouped JSON imports into tags and renders identically to the hand-tagged model.
- [ ] Canonical-core guarantee: a UML import → export cycle yields a valid, plain ERD model file — only `tags` ever entered the model; UML-only structure was flattened or discarded, not stored.
- [ ] The profile mapping is documented in Model.Graph and `CLAUDE.md` (the table above).
- [ ] `docs/WORKLOG.md` updated.

## Status

- **State:** Completed
- **Sprint:** 2026-08-22 — UML notation and export
- **Completed:** 2026-08-22
