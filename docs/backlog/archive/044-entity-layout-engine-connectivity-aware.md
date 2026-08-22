# 044 — Connectivity-aware entity layout engine (grid / serpentine / circular / cross)

## Summary

Today `TableLayoutEngine` (Model.Graph) places tables in an **edge-blind row-major grid** — a "city-block" arrangement that ignores the FK graph, so `SequentialRouter` pays for it with long detours and tangled crossings (routing is the ~1.9 s cost on the 50-table sample). This item replaces that single engine with a **connectivity-aware entity-layout engine**: a pure, name-driven `EntityLayout` abstraction (the `GroupingTheme` pattern) that (1) **orders** the entities so related entities sit close together (an edge-span-minimizing linear ordering), then (2) **projects** that order onto the chosen shape — **grid** (the current row-major, default), **serpentine** (boustrophedon fill, with an up/down sweep + tall/wide orientation), **circular** (ring with the seam at the graph's weakest cut), and **cross** (center hub + arms). Better placement → shorter connectors, fewer crossings, cheaper routes; the router layer is untouched. Per the naming directive, these layout artifacts are named with **"Entity"** (a name-component change scoped to the layout artifacts, not propagated to the data model, palette, or renderers yet).

## Goals

- [x] **Entity-layout abstraction:** an `EntityLayout` name registry (`FromName`, like `GroupingTheme`); each kind maps entities + edges → `IReadOnlyDictionary<string, Rect2>` — the existing layout contract (name-keyed; a collapsed group's `BoxKey` is just another key).
- [x] **Rename `TableLayoutEngine` → `EntityLayoutEngine`** (and `GridLayoutOptions` → `EntityLayoutOptions`): a scoped name-component change. Direct call sites update (ErdComposer, ModelPanelControl, the layout tests); `TableInfo`/`TablePalette`/the renderers are NOT renamed.
- [x] **Connectivity-aware ordering** (the "related entities close together" half): a deterministic linear order that minimizes FK edge-span — component-first (union-find over `FkEdgeExtractor.Extract` edges, reusing the 043 `Connectivity` grouping), greedy adjacency closure from the max-degree hub, BFS layering, tie-broken by name. Deterministic — the repo bans nondeterministic output.
- [x] **Grid projection (row-major)** — the existing arrangement, now fed the ordering; the default and the no-regression baseline.
- [x] **Serpentine projection** — alternating row direction so consecutive order items are spatially adjacent (the end-of-row → start-of-next-row jump disappears); **up/down** = sweep direction + column count (tall vs wide).
- [x] **Circle projection** — ring placement; the ring's seam (start) lands at the graph's weakest cut so no strong FK straddles it; radius sized from the measured entity/box sizes + count.
- [x] **Cross projection** — a center hub (max-degree entity) with four arms (or four-quadrant, one connected component per quadrant) — the dependency-hub view.
- [x] **Non-overlap in every shape** — every projection packs into non-overlapping `Rect2` slots using measured sizes (the same `maxWidth`/`maxHeight` inputs ErdComposer already measures); a collapsed group is still **one rect**.
- [x] **Tests** — ordering determinism; every shape non-overlapping; edge-span on PublicSafety is strictly better than the blind grid; box parity across shapes.

## Scope

**In scope:**
- The `EntityLayout` abstraction + `EntityLayoutEngine` in Model.Graph (pure, unit-tested) and the ordering pass.
- The four projections (grid / serpentine / circle / cross), fed by the ordering.
- Renaming the layout artifacts to "Entity" (engine + options), scoped to the layout + its direct call sites.
- Tests for ordering, non-overlap, and a grid-vs-circular route comparison.
- Docs/WORKLOG.

**Out of scope:**
- The UI switcher + both-renderer wiring — that is **045**.
- A force-directed / spring **refinement pass** (bounded local nudges that re-measure routed length) — a future item; the static order-then-shape approach carries the win deterministically.
- Router-layer or `ConnectorAnchors` changes — anchors stay relative-position-based; per-shape anchor tuning is a future item.
- Persisting a chosen layout or layout state to the model file.
- Propagating the "Entity" naming beyond the layout artifacts (`TableInfo`, `TablePalette`, `TableKindClassifier`, the XAML/Skia `Table` primitives, explorer/inspector) — later, deliberate.

## Approach / Notes

- **The seam is already there.** The layout input is the *visible, post-collapse* item set — ErdComposer builds `layoutTables` (visible entities + a synthetic `TableInfo { TableName = key }` per collapsed box) and ErdComposer already computes the projected/aggregated edge lists (`ModelProjection.Project` output; box-level external edges). Add those edges as the optional connectivity input and `EntityLayoutEngine.Layout(entities, edges, options)` returns the same `name → Rect2` map the current `TableLayoutEngine.Layout` does. Everything downstream (`ConnectorAnchors`, `OrthogonalRouter`, `SequentialRouter`) is untouched.
- **Ordering = minimum linear arrangement** (edge-span; NP-hard exactly, heuristics scale to 2000+ entities). Component-first (the 043 `Connectivity` union-find), then a greedy adjacency closure / BFS layering from a deterministic root (max degree, then name). This is the "always optimizing the connector paths" half — shorter edges before the router ever runs.
- **Seam for the "Entity" naming:** the rename is a name-component change: `TableLayoutEngine`→`EntityLayoutEngine`, `GridLayoutOptions`→`EntityLayoutOptions`, plus the new `EntityLayout*` types. Call sites in `ErdComposer`, `ModelPanelControl`, and the layout tests move with the artifact (they must, to compile); nothing else renames.
- **Open interpretation (confirm at design time):** "rectangular up or down" is read as *sweep direction* (top→down vs bottom→up) + *orientation* (tall vs wide via the column count). A serpentine projection covers both; if a different reading was intended, adjust the goals.
- **Router cost as the acceptance signal:** `RoutingDiagnosticTests`/`RoutingPerformanceTests` style before/after — the connectivity-ordered grid vs today's grid, then vs circle/cross on the PublicSafety sample. Shorter total route length + fewer crossings is the measure of "related entities close".
- **Relationships:** builds on 003/007 (routing), 033 (sequential routing order), 038/039 (visible projection + collapse boxes as layout inputs), 043 (the `Connectivity` union-find + the name-driven theme pattern this mirrors).
- **Tests:** ordering determinism + edge-span reduction; per-shape non-overlap; box parity (collapsed group = one rect) in every shape; grid default byte-for-byte the same layout as today. Manual: switch shapes on PublicSafety and confirm routes re-derive and nothing crosses a table.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; `dotnet test tests/ModelConsole.Tests` → all pass.
- [x] `EntityLayoutEngine` replaces `TableLayoutEngine`; the default **grid** produces the same layout as today (no regression).
- [x] Serpentine / circle / cross produce deterministic, non-overlapping layouts from the same ordering.
- [x] A connectivity-ordered layout measurably shortens routes vs the blind grid on the 50-table sample (a route-length/crossing comparison in the tests).
- [x] Collapsed groups still lay out as **one rect** in every shape; both renderers show the same result when wired in 045.
- [x] Naming: the layout artifacts are `Entity*`; `TableInfo`/`TablePalette`/renderers are untouched.
- [x] `docs/WORKLOG.md` updated.

## Status

- **State:** Completed
- **Sprint:** 2026-08-22 — Entity layouts and switcher
- **Completed:** 2026-08-22
