# 027 — Pointer-hover metadata readout on the XAML drawing

> Series: object-oriented drawing. Part 1 — the visible feature: the drawing answers "what is this?" without a click.

## Summary

Hovering over a graphic entity shows a floating tooltip with that object's metadata, read from its **live** model object — over an FK connector you see the relationship (cardinality, roles), over a table you see its description, key columns, and provenance. The drawing becomes "alive": every object explains itself on hover, using the same readout lines the inspector already shows.

## Goals

- [x] Hover over a connector shows the FK relationship readout — `ReadoutFormatter.Cardinality` + `Roles` (the "admitting → admits" line) sourced from the connector's `FkRelation.Constraint`, the same live constraint the inspector shows.
- [x] Hover over a table shows its key metadata (description, column count, PK/FK count, provenance) from the live `TableInfo`.
- [x] The tooltip is delay-triggered (~400 ms, no flicker while sweeping), follows the pointer, and closes on exit / drag / pan / zoom.
- [x] Hover never interferes with existing gestures (press/drag/pan/click-select, wheel zoom) — it is strictly read-only.

## Scope

**In scope:**
- Hovered-object tracking in `GlContext`: hit-test the object under the pointer on move (no press needed), track exit and clearance.
- A tooltip host in `ModelPanelControl` positioned at the pointer, content built from a hover-summary provider over the object's `Data` payload.
- Hover readouts for tables (`TableInfo`) and connectors (`FkRelation` → `Constraint`); endpoint circles inherit their connector's readout.

**Out of scope:**
- Editing from the tooltip (read-only — the inspector stays the edit surface; backlog 029).
- Hover on the Skia/WASM path (027 is XAML-only; the summary provider must be portable so the sibling reuses it).
- Per-element (per-column) hover — object-level first.

## Approach / Notes

- `GlContext` already hit-tests shapes on press and exposes the pressed/selected object; hover is the same hit-test without a press — track a "hovered" object via `PointerMoved` and clear it on `PointerExited` / drag / pan.
- Reuse `ReadoutFormatter` (ModelGraphLibrary, backlog 022) — `Cardinality`, `Roles`, `Provenance`, `MetadataLines` — so the hover, the inspector, and the connector details never disagree.
- The 022 discipline holds: the tooltip reads the **live** model object, never a frozen snapshot. Connectors carry their source `Constraint` since 022, so the hover can show the exact dependency details.
- A positioned `TextBlock`/`Popup` rather than `ToolTipService` gives pointer-following and anchor control; it must be dismissed on any interaction to avoid stale anchors under zoom/pan.

## Definition of Done

- [x] Hovering an FK shows cardinality/roles from its constraint; hovering a table shows description/columns/provenance.
- [x] Tooltip is delay-triggered, pointer-following, and closes cleanly on drag / pan / zoom.
- [x] Existing gestures unchanged (drag, pan, click-select, wheel zoom).
- [x] `dotnet build` → **0 errors, 0 warnings**; `dotnet test` → all pass.

## Status

- **State:** Completed
- **Sprint:** 2026-08-19 (pointer-hover metadata readout)
- **Completed:** 2026-08-19
