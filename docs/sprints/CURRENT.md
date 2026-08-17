# Sprint 2026-08-17 — ERD graphics primitives base library

> Executed copy of the sprint. Backlog item: `docs/backlog/003-erd-graphics-primitives-base-library.md`.

## Dates

- **Start:** 2026-08-17
- **End:** (TBD)

## Scope

Backlog items in this sprint (reference by number):

- [x] `003` — ERD primitives base library: Skia connector + reusable ERD composition API, Skia Table parity, app renderer toggle (XAML ⇄ Skia), XAML-stack cleanup, tests, docs

## Execution Log

- 2026-08-17 — Sprint defined from backlog item `003`. User scope decision: **both stacks** (build the Skia connector + composition API AND tidy the XAML stack) and **wire into the app** (a MainWindow renderer bar switches the running app between the XAML ERD and the Skia ERD). The portable Skia stack had a `Table` primitive + the full pure Graph engine but **no connector primitive** — "define and draw Table and constraint connectors" was half-built on the stack it was meant for.
- 2026-08-17 — Skia `Table` parity members (`ComputedWidth`/`ComputedHeight`/`GetRowCenterY`, mirroring the XAML `Table`) so tables can be measured and anchored before anything is drawn. Added `Connector` primitive (`ModelConsole.Skia.Primitives`): strokes a `Point2` polyline via `SKPathBuilder` + filled endpoint circles, no-op on empty/null; DodgerBlue colors in `GlPastelPalette`.
- 2026-08-17 — Reusable composition API: `ErdComposer.Compose(tables, frame, options)` → `ErdDiagram` (Layout/Edges/Routes/Issues) — measure probes → `TableLayoutEngine.Layout` (7 cols) → `FkEdgeExtractor.Extract` → `ConnectorAnchors.Resolve`+`FanOut` (grouped per `table::column`) → `SequentialRouter.RouteAll` (the app's router options). Row Y = `probe.GetRowCenterY(col) + slot.Y`. This is the "define and draw an ERD by writing code" heart of the roadmap item.
- 2026-08-17 — `ISkiaConnectorFactory`/`SkiaConnectorFactory` (parity with `ISkiaTableFactory`), registered **singleton** in `App.ConfigureServices`.
- 2026-08-17 — `SkiaPanelControl` renders the **full public-safety ERD** (50 tables, 74 FKs): composes once on first paint (routing is seconds — must not run per frame), caches the `ErdDiagram`, replays tables + connectors per paint; logs counts + FK issues.
- 2026-08-17 — MainWindow renderer bar: two mutually-exclusive `ToggleButton`s ("XAML model" / "Skia render") swap `ModelEditorControl` and `SkiaPanelControl` visibility in a shared grid; both XAML-instantiated (Ioc.Default configured before MainWindow). XAML-stack cleanup: deleted the dead `Graphics/Primitives/Connector.cs` (referenced the Skia stack, empty body, unreferenced); documented the two `GlOrthoPath` modes (`Draw`/`GetPath` = shaped+grips vs `DrawRouted` = static router polyline).
- 2026-08-17 — New tests (9): `SkiaConnectorTests` (strokes polyline + endpoint markers; null/empty no-op), `SkiaTableTests` (computed size, matched row center, unknown-column fallback), `ErdComposerTests` (50 tables / 74 edges, routes cross no table interior, render-to-bitmap without throwing). Fixed the pre-existing `SKPath.MoveTo/LineTo` obsolete warnings in `RoutingDiagnosticTests.cs` (same `SKPathBuilder` migration) so a clean rebuild stays warning-free. **63/63 tests pass** (was 54).
- 2026-08-17 — Verified: full solution `-c Debug -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**; **63/63 tests pass**; app launches unpackaged and stays running. (The renderer-bar toggle + Skia paint need a manual pass — a CLI launch runs on the agent's non-interactive desktop; `ErdComposerTests.ComposeRendersToBitmap` gives an inspectable image.)

## Results

- **Completed:** `003`
- **Notes:**
  - The composition is pure data (`ErdDiagram`) cached and replayed per paint — the routing pass runs once, never per frame.
  - `ErdOptions` defaults mirror the XAML path (`BannerHeight 40`, `Columns 7`, `Gutter/SlotPadding/ExtentMargin 80`, `RouterOptions { GridSize 16, ObstacleMargin 14, StubLength 20 }`).
  - The Skia render is a flat canvas (no zoom/pan/inspector) — the XAML path keeps those; the renderer bar is the switch.
  - Deferred: shared anchor/fan-out helper for `ModelPanelControl` too; corner rounding on routed connectors; the `RoundCorderRadious` typo rename.
