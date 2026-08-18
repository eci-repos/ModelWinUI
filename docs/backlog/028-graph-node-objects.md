# 028 — Graph node objects: recognizing and typing what's under the pointer

> Series: object-oriented drawing. Part 2 — the structure underneath 027/029: every drawable is a first-class **node object** the input layer (hover, click, drag, edit) and the renderer agree on.

## Summary

Make every drawable on the XAML canvas a first-class **node object**: a single surface (identity, the live canonical object it renders, a hover/readout summary, edit verbs) shared by entities, columns, connectors (dependencies), and groups. Hit-testing stops returning a raw `Shape` and returns a typed graph node, so the drawing is "truly object oriented" — recognition, hover (027), selection, and editing (029) all talk to the same object. The node surface is portable so the Skia/WASM sibling can produce the same answers.

## Goals

- [ ] A canonical node surface (in ModelGraphLibrary) that any drawable exposes: node **kind** (entity / element / dependency / group — the design doc's canonical vocabulary), **identity** (name), the **live** canonical object it renders (`TableInfo` / `ConstraintInfo` / column), a hover-summary provider, and edit verbs.
- [ ] Every model-bearing XAML `GlObject` (table, connector, endpoint, group) implements/exposes its node; `GlContext` hit-testing returns the node, so hover, click, drag, and edit all read one object.
- [ ] The summary/recognition provider is a ModelGraphLibrary API (portable, unit-tested), so the Skia/WASM sibling reuses the same identification and text.
- [ ] No two sources of truth: hover, readout, inspector, and the edit surface all read the same live object (022 discipline).

## Scope

**In scope:**
- The node-object model + the mapping from the existing `GlObject.Data` payloads to typed nodes.
- `GlContext` recognition returning a node (typed identity) instead of a bare shape.
- The portable hover-summary/identity provider beside `ReadoutFormatter` in ModelGraphLibrary.
- Groups/entity containers as a node kind (the 023 container concept as a first-class object).

**Out of scope:**
- The editing behaviors themselves (backlog 029) — only the edit *verbs* on the node surface are defined here.
- Skia *rendering* of nodes (the portable provider is shared; the Skia renderer consumes it later).
- New drawing primitives.

## Approach / Notes

- Builds on the existing `GlObject.Data` payload (connectors already carry `FkRelation`, tables carry `TableInfo`): a node is a **typed, first-class** version of that, exposing live model references instead of a loose object.
- Node kinds map 1:1 to the canonical vocabulary (Entity=Table, Element=Column, Dependency=FK, Group=Schema), so the object-oriented drawing *is* the canonical model's own shape.
- The hover summary / identity / edit verbs sit beside `ReadoutFormatter` (pure, net10.0) so the Skia/WASM sibling inherits the same answers.

## Definition of Done

- [ ] Every table + connector on the XAML canvas exposes a node object with a live model reference + hover summary.
- [ ] Hover, click-select, and the inspector all read the **same** node (no drift, no second source of truth).
- [ ] The node/recognition surface is a ModelGraphLibrary API, unit-tested (identity, kind, summary per node type).
- [ ] `dotnet build` → **0 errors, 0 warnings**; `dotnet test` → all pass.

## Status

- **State:** Planned
- **Sprint:** (TBD)
- **Completed:** (TBD)
