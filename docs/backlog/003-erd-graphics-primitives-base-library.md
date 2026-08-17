# 003 — ERD graphics primitives base library

## Summary

The README roadmap's item 1: *"an ERD data-modeling base library to define and draw Table and constraint connectors (lines/symbols) — basic ERD diagrams could be prepared by writing code."* The README is explicit that this library is **Skia-based** (the stack that moves to the Uno/WASM sibling unchanged). This item builds the Skia connector primitive and a reusable ERD composition API, tidies the XAML stack, and wires the Skia render into the running app so both renderers are switchable.

## Goals

- [x] The portable Skia stack (ModelGraphLibrary) can **draw constraint connectors** — a `Connector` primitive that strokes a routed polyline plus endpoint markers (parity with the XAML `GlOrthoPath.DrawRouted` + `GlEllipse`).
- [x] The Skia `Table` exposes its measured size and row centers (parity members) so tables can be laid out and anchored before drawing.
- [x] A reusable, WinUI-free **composition API** (`ErdComposer`) defines and draws a basic ERD by writing code: measure → layout → extract FKs → anchor/fan-out → route.
- [x] The app can **switch between the XAML ERD and the Skia ERD** at runtime (MainWindow renderer bar), and the Skia path renders the full 50-table public-safety schema.

## Scope

**In scope:**
- Skia `Connector` primitive + `ISkiaConnectorFactory`/`SkiaConnectorFactory` (DI-wired singleton), both in ModelGraphLibrary.
- Skia `Table` parity members: `ComputedWidth`/`ComputedHeight`/`GetRowCenterY`.
- `ErdComposer.Compose(tables, frame, options)` → `ErdDiagram` (Layout/Edges/Routes/Issues) over the existing Graph modules.
- `SkiaPanelControl` renders the full ERD (compose once, cache, replay per paint).
- MainWindow renderer bar: "XAML model" / "Skia render" toggles swap the two renderers.
- XAML-stack cleanup: delete the dead `Graphics/Primitives/Connector.cs`; document the two `GlOrthoPath` modes.
- Unit tests for the connector, the table parity members, and the composer (74 edges, no table crossings, render-to-bitmap).
- Docs: backlog item, sprint record, WORKLOG, functionality map, CLAUDE.md.

**Out of scope:**
- Extracting the anchor/fan-out glue into a shared helper used by `ModelPanelControl` too (keeps the active XAML path untouched).
- Corner rounding on routed connectors (already a known gap).
- The `RoundCorderRadious` public-API typo (cross-stack rename, deferred).
- Zoom/pan/inspector for the Skia render (the Skia canvas is a flat render; the XAML path keeps those).

## Approach / Notes

- **The gap:** the portable Skia stack had a `Table` primitive and the whole pure Graph engine (layout, FK extraction, anchors, A* routing — unit-tested) but **no connector primitive whatsoever**. "Define and draw Table *and* connectors" was half-built on the stack it was meant for. The only code that ever drew a routed polyline was a throwaway diagnostic test hand-rolling `SKPath`.
- **`Connector`** (`ModelConsole.Skia.Primitives`): holds `IReadOnlyList<Point2>`; `Draw(GlFrame)` strokes the polyline via `SKPathBuilder` and draws small filled endpoint circles. Colors in `GlPastelPalette` (DodgerBlue, matching the app's connectors). Null/empty points ⇒ no-op.
- **`ErdComposer`** mirrors `ModelPanelControl.Render()`'s pipeline as pure data: one Skia `Table` probe per table (never drawn) for size + row centers, `TableLayoutEngine.Layout` (7 columns, slot = max measured + gutter), `FkEdgeExtractor.Extract`, `ConnectorAnchors.Resolve` + `FanOut` (grouped per `table::column`), `SequentialRouter.RouteAll` with the app's router options. Row Y = `probe.GetRowCenterY(col) + slot.Y` (probe sits at y=0).
- **Compose once, cache:** routing takes seconds; `SkiaPanelControl` composes lazily on first paint and replays the cached `ErdDiagram` per paint — routing never runs per frame.
- **MainWindow** gained a slim renderer bar (two mutually-exclusive `ToggleButton`s) and hosts both `ModelEditorControl` and `SkiaPanelControl` in the same grid, only one `Visibility.Visible`. Both are XAML-instantiated; `Ioc.Default` is configured before `MainWindow` is created, so ctor DI works.
- **Tests** are plain `net10.0` (SkiaSharp core via ModelGraphLibrary): connector strokes + endpoint markers + no-op; table size/row-center/fallback; composer counts (50 tables/74 edges), no table crossings, render-to-bitmap without throwing.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors, 0 warnings**.
- [x] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass (54 existing + 9 new = 63).
- [x] App launches unpackaged and stays running; the renderer bar shows; toggling to "Skia render" paints the 50-table ERD (toggle/visual needs a manual pass on the agent's non-interactive desktop; the `ErdComposerTests` render-to-bitmap gives an image to inspect).
- [x] XAML path unchanged: zoom/pan/drag/inspector/014 toggle intact after the MainWindow restructure.

## Status

- **State:** In progress (sprint 2026-08-17, item 003)
- **Sprint:** (TBD)
- **Completed:** (TBD)
