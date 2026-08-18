# Schema-Driven Model Interpretation — Design (v1)

> Design doc for loading arbitrary JSON models through a **data-driven mapping spec** — the "no code updates" path. This is the plan behind backlog items `019`–`022`. Terms are free; roles are ruled.

## The idea

The drawing and the model explorer already derive entirely from a canonical model (`Model.Data` POCOs). Today that model is loaded from one fixed JSON shape (`ModelFile` — a JSON array of `TableInfo`). This design lets **any JSON document** describe the same concepts under its own shape and vocabulary, guided by a **mapping spec** (data, never code). A new vocabulary ships as new data — no code updates.

The pipeline:

```
model file (.json) + mapping spec (profile or .map.json)
        │
        ▼
interpreter (pure, ModelGraphLibrary)  — applies the grounding rules
        │
        ▼
canonical model (v1 POCOs, extended) + resolution issues
        │
        ▼
existing LoadModel → both renderers + explorer   (unchanged)
```

## Canonical vocabulary (v1)

The internal model is fixed; external schemas use their own words. Entity / Element / Dependency are canonical; Table / Column / FK are synonyms.

| Canonical concept | What it is | Authors might call it | Internal carrier |
|---|---|---|---|
| **Entity** | a named thing | Table, Node, Object | `TableInfo` |
| **Element** | a named, typed field of an entity | Column, Field, Property, Attribute | `ColumnInfo` |
| **Identity** | the entity's key (composite-ready) | PK, Key, Id | `ColumnInfo.IsKey` (extended for composite) |
| **Dependency** | "A depends on B" (child → parent) | FK, Reference, "Depends On", Assoc | `ConstraintInfo` (extended) |
| **Cardinality** | multiplicity per dependency side (`min`/`max`), plus optionality | Multiplicity, required | new members on the dependency |
| **Enumeration** | a named set of allowed values (code + label) | Value-set, Domain, Lookup | new `EnumerationInfo` |
| **Metadata** | annotations that describe, not structure | Tags, Description, Label, display hints | passthrough bag on each node |
| **Provenance** | where/when/how the model came to be | Source, Origin, Lineage, Version | new `Provenance` on the model |
| **Group** | a named collection of entities | Schema, Catalog, Namespace | `CatalogInfo` |

**Terminology note:** we adopt Entity / Element / Dependency as canonical and treat Table / Column / FK as synonyms — but the **internal type names are unchanged** (`TableInfo`, `ColumnInfo`, `ConstraintInfo`). Naming is an alias layer; concepts overlap freely (a Table *is* an Entity). This is the "overlap concepts" intent, made data, not code.

## Grounding rules (the contract)

Terms are free; **roles are ruled**. The rules are how a schema author's freedom stays recognizable to the interpreter.

- **R1 — Container.** Entities live at a *declared path* (`$`, `$.entities`, a keyed object). One path declaration; the container may be an array or an object of named entities. Two forms are auto-detected (backlog 023): the **flat** form (`Entities.Path`) and the **containerized** form (`Schemas.Path` — `repository`/`dataSource` → `schemas` → `entities`/`tables`), where each schema is declared once and every entity under it inherits the name. The containerized form is authoritative when its path resolves; flat is the backward-compatible fallback.
- **R2 — Identity.** Every entity and element has a name, resolved from a *declared field* or a conventional key (`name` / `id` / `title` / `key`).
- **R3 — Key.** An entity's identity is its key: a *declared flag* or a convention (a single element named `Id`). Composite-ready: a declared list of key elements.
- **R4 — Reference.** A dependency is **resolved by name, not by keyword**: any field whose value resolves to a known entity (+ optional element) is a dependency. This is what makes "FK" and "Depends On" the same rule.
- **R5 — Cardinality & optionality.** Declared on the dependency (explicit `min`/`max`/required), or inferred by convention (an element that is a list ⇒ 1:N / M:N).
- **R6 — Annotation.** Anything not caught by R1–R5 is *metadata*; a declared provenance block (any name) is captured as provenance. Neither ever blocks interpretation. Descriptions (backlog 024) ride the first-class canonical members instead of the bag: a `description` on an entity/element flows into `TableInfo.Description` / `ColumnInfo.Description` (round-tripped in the array format), with the metadata bag reserved for everything else.
- **R7 — Precedence.** Explicitly declared roles **always beat** inferred ones. Name-resolution is powerful but sharp (an element literally named `Type` whose value matches an entity name must not become a dependency). Declared references win; convention only when nothing is declared.
- **R8 — Grace.** Unknown fields are ignored. A schema is **valid iff R1–R5 resolve every required concept unambiguously**; ambiguity → a resolution issue at load, never a silent guess. (Extends the existing FK-issue diagnostics.)

## The mapping spec

Three layers, combined as needed:

- **Built-in profiles** — shipped mapping specs as data: the existing `ModelFile` array format (a v1 profile, so the current JSON keeps loading), and the grouped `$.entities` shape. The built-in profiles are what make the two starter shapes work with zero authoring.
- **Sidecar spec** (`.map.json` beside the model) — for custom vocabularies: container path, node kind, term synonyms, role fields, a canonical type map, and which conventions are enabled.
- **Embedded annotations** — `x-` keywords *inside* the author's schema as an escape hatch (e.g. `"x-role": "identity"`) when paths alone cannot describe a shape.

Every spec carries `specVersion`; unknown spec sections are inert (warned, not failed).

## Canonical-hub decision (the fork, resolved)

The app renders `TableInfo` today. Two options existed: grow those types in place, or introduce a separate canonical type set and remap the renderer. **For v1 we grow the existing types additively:**

- Existing POCOs gain **optional members** (cardinality/optionality on dependencies; an open `Extensions` bag on entity/element/dependency for annotations).
- New **sibling types** (`Enumeration`, `Provenance`) live beside them in `Model.Data`.
- The renderer and explorer read only what they always read — new members are inert until a consumer reads them (the v1 readout, item `022`).
- A `kind` discriminator on the dependency lets v2 relationship kinds (generalization, aggregation) be *new kinds*, not new classes.

This is the "overlap concepts" choice: least churn, renderer untouched, and the seam for v2 stays clean.

## Extensibility gears (built into v1, paying off in v2)

1. **Versioned, tolerant spec** — `specVersion` + unknown sections inert ⇒ v2 arrives as new optional spec sections, never a format break.
2. **Additive relationship kind** — a `kind` discriminator; new relationship concepts become new kinds, not new classes; the renderer switches on kind and ignores kinds it does not draw.
3. **Per-node extension bag** — open annotations on every entity/element/dependency; v2 concepts land as annotated data first, become first-class when v2 renders them.
4. **Data-driven type map** — canonical types + enumerations resolve from JSON tables, not a switch statement; the type lattice grows without code.
5. **Rule registry with fixed order** — grounding rules are an ordered list (R1–R8 today); v2 appends rules; v1 interpretations are byte-identical.

## v1 scope and the v2 roadmap

| | v1 (019–022) | v2 (designed-for, not built) |
|---|---|---|
| Concepts | Entity, Element (type + optionality), Identity, Dependency (per-side cardinality + roles + composite-ready), Enumeration, Group, Metadata, Provenance | Generalization (Is-A), uniqueness / alternate keys, referential-integrity rules, stereotypes / views, temporal validity |
| Deliverables | Interpreter core, versioned spec, two starter profiles, proof sample, type + enum wiring, inspector readout | Land through gears 2/3/5 — additive kinds, extension bag, new rules |
| Gate | Proof sample (020) loads through 019 with **no code updates** | Planned only after the gate passes |

## Known risks

1. **R4 name-resolution ambiguity** is the sharpest edge (an element value that equals an entity name reads as a dependency). Mitigation: R7 precedence + declared references; the proof sample (020) deliberately includes an ambiguous case so the resolver is proven, not assumed.
2. **Renderer-consumed vs canonical data** — the readout (022) must read the extended model, not a frozen projection. Keep the extended POCOs as the single source the inspector reads.
3. **The proof sample can be written to pass** — it is authored as a "third-party" model with real messiness (inconsistent types, an ambiguous name) so the gate is honest.
4. **Metadata sink discipline** — R6 keeps semantics from hiding in the metadata bag; cardinality roles stay explicit.

## Relationship to existing code

- `ModelFile` becomes the first built-in profile (its behavior is regression-covered by the existing `ModelFileTests`).
- `MainWindow.LoadModel` stays the entry point; both renderers are unchanged.
- The interpreter lives in `ModelGraphLibrary` (pure, `net10.0`), unit-tested like the existing Graph modules.
- Samples stay fixture-generated and registry-driven (`SampleModels`), so the proof sample appears in File → Open Sample automatically.
