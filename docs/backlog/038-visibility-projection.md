# 038 — Visibility projection: viewpoints over tagged groups

## Summary

With tags in the model (037), build the visibility layer that makes large models navigable: per-entity **pins** (always show / always hide), per-group **toggles** (a group = a tag, so membership is derived — an entity in N groups = N tags), and **focus mode** ("draw only these groups"). The heart is a pure `ModelProjection` in `Model.Graph` that filters tables and FK edges (both endpoints visible) *before* layout/routing, consumed by **both** renderers. Hidden entities are never deleted — only filtered from the view, still listed in the explorer, always recoverable via "Show all". **UML seam:** a projection is exactly a UML package-diagram **viewpoint** — the visible set is the set of shown packages; the composition rule aligns with package-diagram semantics, so the same state drives a UML view (040) with no reinterpretation.

## Goals

- [ ] Pure `EntityVisibility` (the composition rule) + `ModelProjection.Project(tables, edges, visibility)` in `Model.Graph`, unit-tested.
- [ ] Three levers compose: per-entity pin, per-group toggle, focus mode.
- [ ] Both renderers draw only the visible projection — `ModelPanelControl.Render()` and `ErdComposer.Compose` feed the same projection (parity, backlog 003).
- [ ] Explorer gains a **Groups** section (checkbox per group, focus selection, "Show all" reset); inspector gains per-table show/hide verb (gated by `NodeVerbs`).
- [ ] Hidden entities remain in the explorer, never dropped from the model; integrity diagnostics (R8) still computed on the full model.
- [ ] **UML:** the visible projection is expressible as a package-diagram viewpoint (the shown packages), so it drives the UML view unchanged.

## Scope

**In scope:**
- Model.Graph: `EntityVisibility`, `ModelProjection` (pure, deterministic), tests.
- Editor: view state (pins, group toggles, focus), explorer Groups UI, inspector hide/show verb.
- Both renderers: consume the projection (layout over the visible subset, route only visible edges).
- Docs/WORKLOG.

**Out of scope:**
- Collapsed group **boxes** (039 — this item shows/hides members; the box is next).
- UML export/notation (040).
- Saved named views / view profiles (future).
- Tag editing (037), table appearance (036).

## Approach / Notes

- **Composition rule** (draw entity E iff): **not pinned-hide** AND ( **pinned-show** OR **belongs to ≥ 1 visible group** OR **belongs to no group** ). Predictable, backward-compatible — an untagged model has no groups, so every entity draws, exactly as today.
- **Focus mode:** "only these groups" — implemented as *hide all groups except the selected set*, so it composes with the rule instead of adding a separate branch.
- **`ModelProjection.Project(tables, edges, visibility)`** → `(visibleTables, visibleEdges)`. Tables filtered by the rule; `FkRelation` edges filtered to those whose **both** endpoints are visible. Runs **after** `FkEdgeExtractor` (which still resolves the whole model) so unresolved-FK diagnostics (R8) keep surfacing even when the edge is hidden — visibility never masks integrity.
- **Pipeline hook (both renderers):** `ModelPanelControl` holds `_tables` + `_layout`; `ErdComposer.Compose(tables, …)` composes the Skia diagram. Each gains a visibility input: layout runs over `visibleTables` (compact re-layout — positions of the shown subset re-flow predictably), routing over `visibleEdges`. The XAML re-render and the Skia re-compose (off-thread) both already exist — this is a filter at their entry.
- **View state lives view-side** (pins, group toggles, focus selection) — not persisted in this item. Model file carries only tags (037). Saved view profiles are a clean future layer on top.
- **Explorer Groups section:** a "Groups (N)" block (above/below the FK section) with a checkbox per group, a focus toggle, and a global "Show all" reset. Per-table "Hide"/"Show" verb in the inspector. Hidden tables render grayed/struck in the explorer tree so nothing is lost.
- **Performance note:** routing is the expensive pass (A*); routing only the visible edges is the main win for large models. A 200-table subset is trivially routeable. The 2000-table *case* is fully answered by 039 (boxes), not by raw filtering.
- **Tests:** `EntityVisibilityTests` (rule table: pins vs groups vs ungrouped vs focus), `ModelProjectionTests` (edge dropped when either endpoint hidden; integrity edges still reported; ungrouped tables always present unless pinned-hide).

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; `dotnet test tests/ModelConsole.Tests` → all pass.
- [ ] Hiding a group in the app removes its tables and their connectors; the remaining tables re-layout; pins beat groups; "Show all" restores everything; hidden tables still in the explorer.
- [ ] Both renderers agree on the visible set (toggle the renderer bar with a group hidden → identical contents).
- [ ] Unresolved-FK diagnostics still surface for hidden edges.
- [ ] Manual run: tag a few tables (037), hide one group, verify the drawing narrows to the shown subset; untagged models behave exactly as before.
- [ ] `docs/WORKLOG.md` updated (and `CLAUDE.md` "Pure graph modules": `EntityVisibility`/`ModelProjection`).

## Status

- **State:** Planned
- **Sprint:** not yet scheduled
- **Completed:** —
