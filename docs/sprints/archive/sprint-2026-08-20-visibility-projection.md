# Sprint 2026-08-20 — Visibility projection

> Executed copy of the sprint. Definition: `docs/backlog/archive/038-visibility-projection.md`.

## Dates

- **Start:** 2026-08-20
- **End:** 2026-08-20

## Scope

Backlog items in this sprint:

- [x] `038` — Visibility projection: viewpoints over tagged groups

## Execution Log

- 2026-08-20 — **The pure rule object:** `EntityVisibility` (`src/Model.Graph/Graph/EntityVisibility.cs`) implements the composition rule — **not pinned-hide** AND ( **pinned-show** OR **belongs to ≥ 1 visible group** OR **belongs to no group** ). Pins (`Dictionary<string,bool>`, true=show/false=hide) beat groups; `_visibleGroups` seeded to the full tag universe (default = everything, backward compatible); `SetFocus` replaces the set; `ShowAll` resets; `EntityVisibility.Create(tables)` derives the universe from the union of all `TableInfo.Tags`.
- 2026-08-20 — **`ModelProjection.Project(tables, edges, visibility)`** (`src/Model.Graph/Graph/ModelProjection.cs`) → `(visibleTables, visibleEdges)`: tables filtered by the rule, edges kept iff **both** endpoints are visible, original order preserved, inputs unmutated, null visibility = everything. Runs **after** `FkEdgeExtractor` on the full model so R8 diagnostics surface even for hidden edges.
- 2026-08-20 — **Shared-instance wiring (the one decision):** one `EntityVisibility` per model, owned by `ModelPanelControl` (`CurrentVisibility` — the old `Visibility` field shadowed `UIElement.Visibility`). `ModelEditorControl.ApplyVisibility(mutate)` mutates → `ModelPanelControl.SetVisibility` (re-layout + re-render) → explorer + inspector re-sync → `VisibilityChanged` → `MainWindow` relays to `SkiaEditor.SetVisibility` (clears the cached diagram; stale-compose guard checks both `tables` and `visibility` identity). Both renderers agree by construction.
- 2026-08-20 — **Pipeline hooks:** `ModelPanelControl.Render()` = extract → project → draw visible only. `ErdComposer.Compose(…, visibility = null)` gained the optional param — measure/layout/anchors/routing all feed the projection; null keeps pre-038 behavior.
- 2026-08-20 — **Explorer Groups section:** a "Groups (N)" block — checkbox per group, **Focus** toggle (`SetFocus` on check, exit-focus + show-all on uncheck), **"Show all"** reset. Hidden tables grayed in the tree, never dropped. Inspector gained a Show/Hide pin verb pair gated by the new `NodeVerbs.CanToggleVisibility` (`Entity` preset only).
- 2026-08-20 — **Tests:** `EntityVisibilityTests` (15) + `ModelProjectionTests` (9) + `ErdComposerTests` Skia-path visibility trio (3). (A first full-suite run failed to compile on a nonexistent `DataInfo.INTEGER` constant in the new test helpers — fixed to `DataInfo.VARCHAR` and re-run green.)

## Results

- **Completed:** `038`
- **Deferred:** — (the backlog now holds `039`–`040`, unscheduled)
- **Notes:** Verified `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors / 0 warnings**; `dotnet test tests/ModelConsole.Tests` → **all pass**. Manual verification — tag a few tables (037), hide a group, verify the drawing narrows + re-layouts + both renderers agree across the renderer-bar toggle, "Show all" restores, hidden tables stay in the explorer — needs a human run; CLI launch runs on the agent's non-interactive desktop.
