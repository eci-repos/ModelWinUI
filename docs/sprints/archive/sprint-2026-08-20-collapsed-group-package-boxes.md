# Sprint 2026-08-20 — Collapsed group boxes

> Executed copy of the sprint. Definition: `docs/backlog/archive/039-collapsed-group-package-boxes.md`.

## Dates

- **Start:** 2026-08-20
- **End:** 2026-08-20

## Scope

Backlog items in this sprint:

- [x] `039` — Collapsed group boxes: UML package nodes as first-class drawables

## Execution Log

- 2026-08-20 — **The pure state + aggregation (`Model.Graph`):** `GroupCollapseState` (the collapsed-set rule object — `SetCollapsed`/`ExpandAll`/`IsCollapsed`, per-group, default all-expanded) and `GroupBoxAggregation.Build(tables, edges, group, collapsedSet)` → model `GroupBox` + `GroupBoxEdge`: members = primary-tagged tables (an entity's **primary group = its first tag**; other groups see it only as a reference **stub** — the UML owning-package rule), external edges deduplicated per external target (count label when several FKs share a target; a collapsed target merges under its box; a collapsed target group inverts to a box→box edge), both-endpoints-outside ignored. `BoxKey(group)` = `"group::"+group` — the shared layout key; the box replaces its members in the layout grid, drawn set, and router-obstacle set.
- 2026-08-20 — **`Model.Palette`:** `GroupPalette` — HeaderHeight 24 / BodyHeight 44 / MinWidth 150 / TextPadding 16 + `BoxHex(group)`, a stable per-group pastel distinct from the model banner colors.
- 2026-08-20 — **Skia path (`Model.Skia`):** `Skia/Primitives/GroupBox.cs` (name band, `<<package>>` + count, tint, rounded) + `ISkiaBoxFactory`/`SkiaBoxFactory`. `ErdComposer.Compose` gained the optional `collapse` param — collapses the visible projection into boxes **before** measure/layout/routing (tables + boxes lay out; table edges + box edges route, box anchors at box/target midlines; 012 extended to boxes). `SkiaPanelControl.SetCollapse` swaps the instance, clears the cached diagram, re-composes once off-thread.
- 2026-08-20 — **XAML path (`Model.Graphics.WinUI` + `Model.Controls.WinUI`):** `Graphics/Primitives/GroupBox.cs` extends `GlRectangle` (measured at construction, rounded, header tinted by `GroupPalette.BoxHex`, body with top hairline, name + `«package»` + count, `Hovered` border rest/hovered from `TablePalette`, `DeltaMove` repositions bands, `Node` cached). `IBoxFactory`/`BoxFactory` + DI registration. `ModelPanelControl`: `CurrentCollapse`/`SetCollapse`, `BuildBoxes` over the visible projection, box drag → full re-route, box hover, box click → `GroupExpandRequested`. `ModelExplorerControl`: `▾` collapse `ToggleButton` per group beside the 038 checkbox (`GroupCollapseRequested`).
- 2026-08-20 — **State wiring (parity, 003):** `ModelEditorControl.CurrentCollapse`/`CollapseChanged`/`ApplyCollapse` mirror the 038 visibility wiring; `MainWindow` relays `XamlEditor.CollapseChanged` → `SkiaEditor.SetCollapse` and seeds it on `LoadModel`, so both renderers collapse the identical set. Explorer toggles and box clicks both mutate the one shared instance.
- 2026-08-20 — **Tests:** `GroupCollapseStateTests` (6) + `GroupBoxAggregationTests` (12) + `ErdComposerCollapseTests` (6, Skia end-to-end incl. the 012 box invariant and box→box edges).

## Results

- **Completed:** `039`
- **Deferred:** — (the backlog now holds `040`, unscheduled)
- **Notes:** Verified `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors / 0 warnings**; `dotnet test tests/ModelConsole.Tests` → **256/256 pass**. Manual verification — collapse/expand/hide a group from the explorer and by clicking the box; pan/zoom/drag still work on boxes; both renderers agree across the renderer-bar toggle — needs a human run; CLI launch runs on the agent's non-interactive desktop.
