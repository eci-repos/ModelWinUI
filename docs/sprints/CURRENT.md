# Sprint 2026-08-16 — Connector routing order & readability

> Executed copy of the sprint. Backlog items: `docs/backlog/008-connector-routing-order.md`, `docs/backlog/009-zoom-and-fit.md`, `docs/backlog/010-editable-canvas.md`.

## Dates

- **Start:** 2026-08-16
- **End:** (TBD)

## Scope

Backlog items in this sprint (reference by number):

- [x] `008` — Connector routing: logical order, port-based anchors, endpoint markers
- [x] `009` — Zoom & fit: scale slider, % entry, fit-to-window
- [x] `010` — Editable canvas: drag tables, connectors follow, entity inspector, delete-and-regenerate

## Execution Log

- 2026-08-16 — Sprint defined from backlog items `008` + `009`. Approach confirmed with user: the router already produces the desired stub-out / turn / bend-in shape; the crossing problem is **edge-ordering** (each edge routed independently), plus hardcoded right→left anchors and no port fan-out. Fixes: sequential routing (routed edges become obstacles), per-edge anchor-side selection, shared-column fan-out, `StubLength` → 20, endpoint circles.
- 2026-08-16 — Zoom added as item `009`: use the ScrollViewer's native zoom (`ZoomFactor` / `ChangeView`) driven by a slider + % box + fit button; zoom-around-cursor; verify pointer hit-testing at non-100% zoom.
- 2026-08-16 — Editable canvas added as item `010`: drag tables (connectors follow), entity inspector over the `Model.Data` POCOs, and delete-a-connector → remaining paths regenerate as simple non-crossing routes by default. Principle: a route is always *derived* from current state, never a frozen artifact. Depends on 008 + 009.
- 2026-08-16 — Sprint started. Executing item `008` first.
- 2026-08-16 — New pure modules in ModelGraphLibrary (`ModelConsole.Graph`): `ConnectorAnchors` (`AnchorSide` + `Resolve` + `FanOut`) and `SequentialRouter` (`RouteAll` — routes each edge, feeds the routed polyline back as a thin obstacle). `OrthogonalRouter.Route` gained a `thinObstacles` parameter (non-inflated obstacles) plus an A* segment-crossing check so a grid step cannot jump over a thin obstacle.
- 2026-08-16 — Fixed a pre-existing `Rect2.SegmentCrossesInterior` bug: for a segment parallel to an axis (`d == 0`), `GetStrictInterval` returned an empty interval even when the constant coordinate was strictly inside the rect, so axis-aligned segments were never detected as crossing. Now returns the full interval when inside. The router's direct-path check now uses the **un-inflated** obstacles so a route leaving a table-edge anchor is not rejected for crossing that table's own inflated margin.
- 2026-08-16 — App integration: `ModelPanelControl` routes via `ConnectorAnchors.Resolve` + `FanOut` (grouped by child/parent `table::column`), `SequentialRouter.RouteAll`, `StubLength = 20`, and draws 8 px `Colors.DodgerBlue` endpoint circles via the new `GlEllipse` primitive.
- 2026-08-16 — New tests: `ConnectorAnchorsTests` (6) + `SequentialRouterTests` (4). **42/42 pass** (was 32). Full solution `-c Debug -p:Platform=x64` → **0 errors, 0 warnings**; app launches unpackaged and stays running (routing executes without crashing).
- 2026-08-16 — Item `009` (zoom & fit) executed. `ModelPanelControl.xaml` restructured: a zoom toolbar row (fit button + slider + % box) above the ScrollViewer; the ScrollViewer now has `ZoomMode="Enabled"`, `MinZoomFactor=0.1`, `MaxZoomFactor=4.0` so Ctrl+wheel/pinch zoom around the cursor natively.
- 2026-08-16 — Zoom logic in `ModelPanelControl.xaml.cs`: `ApplyZoom` (zoom-around-viewport-center via `ChangeView`), `FitToWindow` (`min(viewport/extent)` capped at 100%, centered), `ViewChanged` → `SyncZoomUI` (slider + % box follow the actual zoom), `CommitZoomTextBox` (parse + clamp + revert on invalid), and `KeyboardAccelerator`s for Ctrl+0 (100%), Ctrl+1 (fit), Ctrl+Plus/Minus (numpad `Add`/`Subtract` + main-keyboard VK 0xBB/0xBD — the SDK's `VirtualKey` enum omits the `Oem*` names).
- 2026-08-16 — Verified pointer hit-testing at non-100% zoom: `GlContext` handlers are attached to the Canvas and use `GetCurrentPoint(null)` (Canvas-local coordinates), so hit-testing and delta-move stay correct under ScrollViewer zoom — no changes needed.
- 2026-08-16 — Item `010` (editable canvas) executed. `GlContext` gained `ShapeReleased`/`ShapeClicked` events (drag vs click distinguished by a 2 px movement threshold) and a `Reset()` that clears interaction state before a full re-render. `GlObject` gained a `Data` payload so a connector carries its `FkRelation`.
- 2026-08-16 — `Table` exposes `TableInfo` and its rows panel + banner are now hit-test-transparent, so a press anywhere on a table reaches the rectangle and the whole table drags. `ModelPanelControl` refactored to a state-driven `Render()` pipeline: `_tables` (model) + `_layout` (positions) are the source of truth; drag release updates the layout and re-runs the pipeline, so connectors follow and any connector drag snaps back.
- 2026-08-16 — New `EntityInspectorControl` (right column, below the log): clicking a table lists its columns with editable data types (commit re-renders); clicking a connector shows the FK relationship with a Delete button. `ModelEditorControl` wires `EntitySelected` → inspector and inspector `ModelEdited`/`DeleteRequested` → `ModelPanel.Refresh()`/`DeleteConnector()`.
- 2026-08-16 — `DeleteConnector` removes the FK `ConstraintInfo` from the model and re-renders; the remaining connectors regenerate as simple non-crossing routes automatically. Endpoint circles are tagged with their connector so clicking a circle also inspects the relationship.
- 2026-08-16 — Verified: full solution `-c Debug -p:Platform=x64` → **0 errors, 0 warnings**; app launches unpackaged and stays running; 42/42 tests still pass.

## Results

- **Completed:** `008`, `009`, `010`
- **Notes:**
  - App router options now `GridSize=16, ObstacleMargin=14, StubLength=20`.
  - Sequential edges are added as **thin** obstacles (4 px margin, not inflated) so port fan-out still fits through tight gaps.
  - `Rect2.SegmentCrossesInterior` fix changes behavior for axis-aligned segments — existing tests were self-consistent with the old bug; all still pass.
  - Zoom is built on the ScrollViewer's native zoom — no hand-rolled `ScaleTransform`. Slider/textbox/fit all reduce to `ChangeView`; the % box doubles as the zoom readout.
  - The drawing is always *derived* from the model state (`_tables` + `_layout`), never a frozen artifact — drag, delete, and POCO edits all just re-run `Render()`.
  - Backlog items archived: `docs/backlog/archive/008-connector-routing-order.md`, `docs/backlog/archive/009-zoom-and-fit.md`, `docs/backlog/archive/010-editable-canvas.md`.
