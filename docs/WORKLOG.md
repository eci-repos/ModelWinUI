# Worklog

Running record of work done and next pending tasks. **Read this first** when starting work; update it when you finish. This is the handoff document for other agents.

## How to use

- **Done** — append a dated entry for each piece of work you complete.
- **Pending** — keep the pending list current: add tasks you discover, mark them done as you go.
- **Handoff** — anything the next agent must know (decisions, gotchas, half-finished work) goes in the current entry.

---

## Done

### 2026-08-16 — Connectors never cross tables (backlog item 012)

- **Root cause (investigated):** 143 of 74 routed edges crossed a table, all from the **Z-path fallback**. A* returned null (grid unreachable) for ~50 edges because the **thin obstacles** (already-routed connectors) form barriers: e.g. the PersonAlias→RefId connector (vertical at x=581, horizontal at y=619) combined with the tables makes the grid genuinely unreachable for PersonName→Person. The Z fallback then drew a straight 4-point Z through tables. (A BFS reachability probe reached the goal only because it used an **empty** thin list — the barrier is real.)
- **Fix — retry A* without thin obstacles:** when A* returns null with thin obstacles, `OrthogonalRouter.Route` retries with only the inflated table obstacles. The hard invariant is "no connector crosses a table interior", so crossing a connector is acceptable when the alternative is crossing a table. This eliminated all 143 crossings.
- **Fix — clear-cell stubs:** `SnapStub` now moves the stub outward until its grid cell is not blocked and the anchor→stub segment does not cross a table interior, so a route never starts inside a blocked cell or crosses a neighbour table on its way out.
- **Fix — table-aware Z fallback:** when even the no-thin A* returns null (genuinely unreachable, e.g. a full-height wall), the fallback tries both HV and VH variants and returns the first that does not cross a table interior; when neither is clear, the variant with fewer crossings.
- **New tests** (`NoCrossingInvariantTests`, 4): the 50-table `PublicSafetySchema` fixture has **0 table crossings**; a tight layout (20 px slots) has 0; the PersonName→Person thin-barrier case routes without crossing a table; the direct HV path for side-by-side tables does not enter either interior.
- **Verified:** full solution `-c Debug -p:Platform=x64` → 0 errors; **49/49 tests pass** (was 45); app launches unpackaged and stays running.
- Backlog item: `docs/backlog/012-connectors-never-cross-tables.md`.

### 2026-08-16 — Fixed: dragging a table hangs the app (backlog item 013)

- **Root cause (investigated):** (1) `GetCurrentPoint(null)` returns **window-relative** coordinates (per WinUI docs), so at non-100% zoom the drag delta was applied in window pixels to content coordinates → the table moved `zoom×` too far and got flung across the canvas. (2) The A* re-route cost grows **quadratically** with canvas size; a flung table grows the canvas and the release re-route took minutes. Benchmark: full re-route 4.2 s baseline, 223 s at a 20000 px drag.
- **Fix 1 — coordinate space:** `GlContext` uses `e.GetCurrentPoint(_canvas)` (Canvas-relative → content space) in all six pointer handlers. Drag delta is now correct at any zoom.
- **Fix 2 — partial re-route:** `ModelPanelControl.Render(string onlyTable = null)` stores the last routes and, on drag release, re-routes **only the moved table's edges** against the stored routes as thin obstacles (full re-route for initial/delete/POCO-edit). Measured: 4.2 s → **2.2 s** for a drag release.
- **Fix 3 — node budget:** `RouterOptions.MaxExpansions` (default 100000) caps A* work; the 20000 px case that took 223 s is now bounded. New test `NodeBudgetCapsAStarWork`.
- **Verified:** full solution `-c Debug -p:Platform=x64` → 0 errors; **45/45 tests pass**; app launches unpackaged and stays running.
- Backlog item: `docs/backlog/013-drag-table-hangs-app.md` (fix implemented; per-edge timing diagnostics deferred).

### 2026-08-16 — Sprint executed: Editable canvas (backlog item 010)

- `GlContext` gained `ShapeReleased` / `ShapeClicked` events (drag vs click distinguished by a 2 px movement threshold) and a `Reset()` that clears interaction state (current/selected shape, grips, grabbers) before a full re-render. `GlObject` gained a `Data` payload so a connector carries its `FkRelation` edge.
- `Table` exposes `TableInfo` (the metadata it renders; columns shared with the model) and its rows panel + banner are now hit-test-transparent, so a press anywhere on a table reaches the rectangle and the whole table drags.
- `ModelPanelControl` refactored to a state-driven pipeline: `_tables` (model) + `_layout` (positions) are the source of truth; `Render()` clears the canvas and re-draws tables + routes connectors from that state. Drag release updates the layout and re-runs the pipeline, so connectors follow; a connector drag snaps back to its routed position.
- New `EntityInspectorControl` in the right column (below the log): clicking a table lists its columns with editable data types (Enter/LostFocus commits → re-render); clicking a connector shows the FK relationship with a Delete button. Endpoint circles are tagged with their connector so clicking a circle also inspects the relationship.
- `DeleteConnector` removes the FK `ConstraintInfo` from the model and re-renders; the remaining connectors regenerate as simple non-crossing routes automatically (the "regenerate by default" principle — a route is always derived from current state, never a frozen artifact).
- **Verified:** full solution `-c Debug -p:Platform=x64` → **0 errors, 0 warnings**; app launches unpackaged and stays running; 42/42 tests still pass.
- Backlog item archived: `docs/backlog/archive/010-editable-canvas.md`. Sprint record: `docs/sprints/CURRENT.md` (008/009/010 all complete).

### 2026-08-16 — Sprint executed: Zoom & fit (backlog item 009)

- `ModelPanelControl.xaml` restructured: a zoom toolbar row (fit-to-window button + scale slider + % entry box) above the ScrollViewer; the ScrollViewer now has `ZoomMode="Enabled"`, `MinZoomFactor=0.1`, `MaxZoomFactor=4.0` so Ctrl+wheel / pinch zoom around the cursor natively (no hand-rolled `ScaleTransform`).
- Zoom logic in `ModelPanelControl.xaml.cs`: `ApplyZoom` (zoom-around-viewport-center via `ChangeView`, clamped to the zoom range), `FitToWindow` (`min(viewportW/extentW, viewportH/extentH)` capped at 100%, centered), `ViewChanged` → `SyncZoomUI` (slider + % box follow the actual zoom from any source), `CommitZoomTextBox` (parse + clamp + revert on invalid), and `KeyboardAccelerator`s for Ctrl+0 (100%), Ctrl+1 (fit), Ctrl+Plus/Minus (numpad `Add`/`Subtract` + main-keyboard VK 0xBB/0xBD — the SDK's `VirtualKey` enum omits the `Oem*` names, so raw VK codes are used).
- **Pointer hit-testing verified at non-100% zoom:** `GlContext` handlers are attached to the Canvas and use `GetCurrentPoint(null)` (Canvas-local coordinates), so hit-testing and delta-move stay correct under ScrollViewer zoom — no changes needed.
- **Verified:** full solution `-c Debug -p:Platform=x64` → **0 errors, 0 warnings**; app launches unpackaged and stays running; 42/42 tests still pass.
- Backlog item archived: `docs/backlog/archive/009-zoom-and-fit.md`. Sprint record: `docs/sprints/CURRENT.md` (010 still pending in the same sprint).

### 2026-08-16 — Sprint executed: Connector routing order & readability (backlog item 008)

- New pure modules in ModelGraphLibrary (`ModelConsole.Graph`): `ConnectorAnchors` (`AnchorSide` enum, `Resolve` picks the departure side from the child/parent relative position, `FanOut` offsets shared-column anchors perpendicular to the side) and `SequentialRouter` (`RouteAll` routes edges in deterministic order, feeding each routed polyline back as a **thin** obstacle so later edges avoid crossing it).
- `OrthogonalRouter.Route` gained a `thinObstacles` parameter (non-inflated obstacles, 4 px margin) plus an A* segment-crossing check so a single grid step cannot jump over a thin obstacle (e.g. an already-routed connector).
- **Fixed a pre-existing `Rect2.SegmentCrossesInterior` bug:** for a segment parallel to an axis (`d == 0`), `GetStrictInterval` returned an empty interval even when the constant coordinate was strictly inside the rect, so axis-aligned segments were never detected as crossing a rect. Now returns the full interval when inside. The router's direct-path check now uses the **un-inflated** obstacles so a route leaving a table-edge anchor is not rejected for crossing that table's own inflated margin (keeps straight lines for side-by-side tables).
- App integration: `ModelPanelControl` routes via `ConnectorAnchors.Resolve` + `FanOut` (grouped by child/parent `table::column`), `SequentialRouter.RouteAll`, `StubLength = 20`, and draws 8 px `Colors.DodgerBlue` endpoint circles via the new `GlEllipse` primitive (XAML `Graphics` stack).
- New tests: `ConnectorAnchorsTests` (6) + `SequentialRouterTests` (4) — straight edges stay straight, later edge avoids earlier edge, routes avoid table obstacles, deterministic.
- **Verified:** library builds 0/0; **42/42 tests pass** (was 32); full solution `-c Debug -p:Platform=x64` → **0 errors, 0 warnings**; app launches unpackaged and stays running (routing executes without crashing).
- Backlog item archived: `docs/backlog/archive/008-connector-routing-order.md`. Sprint record: `docs/sprints/CURRENT.md` (009 + 010 still pending in the same sprint).

### 2026-08-15 — Sprint executed: Unit tests + A* FK routing + 50-table schema (backlog item 007)

- Extended `ConstraintInfo` with nullable `ReferencedTableName` / `ReferencedColumnName` (backward compatible; null column ⇒ parent PK default) so FKs can express their parent side.
- New pure modules in ModelGraphLibrary (`ModelConsole.Graph`): `Geometry` (`Point2`/`Rect2` + strict-interior segment test), `FkEdgeExtractor` (74 edges, deterministic, issue reporting), `TableLayoutEngine` (row-major grid), `OrthogonalRouter` (A* grid pathfinding, obstacle inflation, cell-snapped stubs, collinear simplification, orthogonal Z-path fallback). All Windows.Foundation-free.
- New fixture `ModelConsole.ModelData.PublicSafetySchema` — exactly 50 tables, 74 FK edges across 8 domain areas (Identity, Reference data, Agencies & personnel, Geography & facilities, Incidents & dispatch, Enforcement, Offenses & case, Courts & sentencing); SentenceCondition→Sentence omits `ReferencedColumnName` to exercise the PK-default rule.
- **First test project:** `tests/ModelGraphLibrary.Tests` (xUnit, net10.0, added to `ModelWinUI.sln`). 32 tests across `SchemaIntegrityTests`, `FkEdgeExtractorTests`, `TableLayoutEngineTests`, `OrthogonalRouterTests` — **all green (158 ms)**.
- App integration: `GlOrthoPath.DrawRouted` (absolute-point path), `IConnectorFactory.CreateRouted`, `Table.ComputedWidth/Height` + `GetRowCenterY`, `IModelDataProvider.GetPublicSafetyTables`, `ModelPanelControl` rewrite — measure → layout → draw → route the 50 tables + connectors; Canvas wrapped in a ScrollViewer. Fixed CS0246 (`using System.Collections.Generic;` in `IConnectorFactory.cs` / `ConnectorFactory.cs`).
- **Verified:** library builds 0/0; tests 32/32; full solution `-c Debug -p:Platform=x64` → **0 errors, 0 warnings**; app launches unpackaged, window "EDAM Studio" responding, 50 tables + ~74 routed FK connectors render without crashing. (Screenshot declined.)
- Sprint record: `docs/sprints/archive/sprint-2026-08-15-tests-and-fk-routing.md`. Backlog item archived: `docs/backlog/archive/007-unit-tests-and-fk-routing.md`. Functionality map + CLAUDE.md updated.

### 2026-08-15 — Sprint executed: ModelGraphLibrary split (backlog item 006)

- New project `src/ModelGraphLibrary/ModelGraphLibrary.csproj` — plain `net10.0` class library, `RootNamespace=ModelConsole`, `SkiaSharp` 4.151.1 (core). Added to `ModelWinUI.sln`.
- `git mv`'d 18 files from the app into it: `Model/Data` (5, namespace `Model.Data`), `Skia/GLibrary` (9), `Skia/Primitives` (2), and the `ISkiaTableFactory` / `SkiaTableFactory` contract (`ModelConsole.Services`) — the library's public interface.
- Removed the dead WinUI usings from `RectangleHalf.cs` — the only WinUI reference in the Skia stack — so the library is WinUI-free and portable to the Uno/WASM sibling.
- App wires it via `ProjectReference`; `SkiaPanelControl` + DI registration unchanged. XAML `Graphics` stack stays in the app (WinUI-bound).
- **Verified:** library builds 0/0; full solution `-c Debug -p:Platform=x64` → **0 errors, 0 warnings**; app launches unpackaged, window "EDAM Studio" responding, sample drawing runs. (Screenshot declined.)
- Sprint record: `docs/sprints/archive/sprint-2026-08-15-modelgraphlibrary.md`. Backlog item archived: `docs/backlog/archive/006-model-graph-library.md`.

### 2026-08-15 — Sprint executed: Dependency injection & coding-standards cleanup (backlog item 002)

- Added `Microsoft.Extensions.DependencyInjection` **10.0.10**; composition root in `App.xaml.cs` (`ConfigureServices()` + `Ioc.Default.ConfigureServices(Services)` bridge for XAML-instantiated controls).
- New `Services/` layer (namespace `ModelConsole.Services`): `ILogService`/`LogService`, `IModelDataProvider`/`ModelDataProvider`, `ITableFactory`/`TableFactory`, `ISkiaTableFactory`/`SkiaTableFactory`, `IConnectorFactory`/`ConnectorFactory`, `IRectangleFactory`/`RectangleFactory`.
- `GlContext` ctor → `GlContext(Canvas, ILogService)`; `WriteMessage` now actually logs ("GL Context Ready." appears in the log panel — was a silent no-op through an always-null `Writer`).
- Converted `ModelPanelControl`, `DiagnosticsLogViewModel` (ctor-injected `ILogService`), `DiagnosticsLogControl`, `SkiaPanelControl` to DI via `Ioc.Default.GetRequiredService<T>()`.
- Added `IGlModel`; `GlModel.Add(GlObject)` now adds + returns the instance (fixed the null-returning stub).
- Reordered `ModelEditorControl.xaml` so `DiagnosticsLogControl` subscribes to the log before `ModelPanelControl` writes its startup message.
- Removed dead usings: `GlRectangle`, `GlOrthoPath`, `GlGrip`, `GlHandle`, `GlGrabberBase`, `GlFrame`, `GlText`, `Skia/Primitives/Table`.
- **Verified:** `dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64` → **0 errors**. App launches unpackaged, window "EDAM Studio" responding, sample drawing runs, log panel shows startup messages. (Screenshot of rendered output declined.)
- Sprint record: `docs/sprints/archive/sprint-2026-08-15-dependency-injection.md`. Backlog item archived: `docs/backlog/archive/002-dependency-injection.md`. Functionality map added: `docs/codebase-functionality-map.md`.

### 2026-08-15 — Sprint executed: .NET 6 → .NET 10 migration (backlog item 001)

- `TargetFramework` → `net10.0-windows10.0.19041.0`; `RuntimeIdentifiers` `win10-*` → `win-*` (NETSDK1083 otherwise).
- Packages updated: Microsoft.WindowsAppSDK **2.4.0**, SkiaSharp.Views.WinUI **4.151.1**, CommunityToolkit.Mvvm **8.4.2**, Microsoft.Windows.SDK.BuildTools **10.0.28000.2526**.
- Fixed SkiaSharp 4.x breaking changes in the `Skia` stack (`SKPaint` text members removed; migrated to `SKFont` + `DefaultTextPaint` in `GlFrame`, `GlText`, `Table`).
- Fixed `CS8981` (`mvvm` alias → `Mvvm`).
- Added `<WindowsPackageType>None</WindowsPackageType>` so the app runs unpackaged (fixes WinAppSDK auto-init `0x80040154 Class not registered`).
- **Verified:** `dotnet build ./ModelWinUI.csproj -c Debug -p:Platform=x64` → 0 errors, no `NETSDK1138`. App launches unpackaged, window "EDAM Studio" created and responding; sample drawing runs without crashing. (Screenshot of rendered output declined.)
- Sprint record: `docs/sprints/archive/sprint-2026-08-15-net10-migration.md`. Backlog item archived: `docs/backlog/archive/001-migrate-to-latest-net.md`.

### 2026-08-15 — Docs structure: one current sprint + .NET migration sprint

- Added the **one current sprint** rule: `docs/sprints/CURRENT.md` is the single current sprint; promoted sprints move to `docs/sprints/archive/sprint-YYYY-MM-DD-<name>.md`.
- Added mandate to `Agents.md`: **instructions always go in `Agents.md`** (canonical instruction set), plus the one-current-sprint mandate.
- Created backlog item `docs/backlog/001-migrate-to-latest-net.md` — migrate from .NET 6 to .NET 10 (latest LTS, supported until Nov 2028).
- Created `docs/sprints/CURRENT.md` — the current sprint (the .NET 10 migration), in execution.
- Updated `docs/README.md`, `docs/backlog/README.md`, `docs/sprints/README.md` for the new rule.

### 2026-08-15 — Project scaffolding & documentation structure

- Created `CLAUDE.md` at repo root (build/run commands, architecture overview, conventions).
- Created the docs structure: `docs/backlog/` (+ `archive/`), `docs/sprints/`, `docs/README.md`, `docs/WORKLOG.md`, and templates for backlog items and sprint records.
- Created `Agents.md` at repo root (mandates + full instruction set for new agents).
- Verified the project builds: `dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64` succeeds (AnyCPU fails).

### 2026-08-15 — Initial project state (as found)

- WinUI 3 desktop app "EDAM Studio" (net6.0-windows10.0.19041.0, Windows App SDK 1.3, SkiaSharp).
- Two parallel graphics stacks exist: `ModelConsole.Graphics.GLibrary` (XAML `Shape`-based, active) and `ModelConsole.Skia.GLibrary` (SkiaSharp, experimental/unwired).
- `ModelPanelControl` draws two sample tables + orthogonal connectors programmatically.
- Data model (`Model/Data`) supports JSON round-tripping; sample fixtures in `Model/ModelData/Data_Table_Entity.cs`.

---

## Pending

### Next tasks (in priority order)

1. **Sprint 2026-08-16 (connector routing) is complete** — backlog items **008**, **009**, **010** all done. Sprint record: `docs/sprints/CURRENT.md`. Archived: `docs/backlog/archive/008-connector-routing-order.md`, `009-zoom-and-fit.md`, `010-editable-canvas.md`.
   - **010 — Editable canvas** ✅ **Done 2026-08-16** — drag tables (re-route on release), click an entity to inspect its `Model.Data` POCO in the new `EntityInspectorControl`, edit a column data type → re-render, delete a connector → remaining paths regenerate as simplest non-crossing routes by default. The drawing is always *derived* from the model state (`_tables` + `_layout`), never a frozen artifact.
2. **Backlog item 003 — ERD graphics primitives base library** (from project README roadmap)
   - Define and draw Table and constraint connectors (lines/symbols) as a reusable library.
   - Current state: `Table` primitive and `GlOrthoPath` connectors exist but are early-stage; `GlModel` collection is now functional behind `IGlModel`.
3. **Backlog item 004 — UI controls for viewing the data model** (roadmap)
   - Develop the controls needed to view a model (beyond the current sample drawing).
4. **Backlog item 005 — Non-trivial sample models** (roadmap)
   - Ship sample models showing the tool's capabilities.
5. **Backlog item 011 — Drawing panning** (planned 2026-08-16)
   - Mouse-driven panning of the drawing: left-drag on empty canvas, middle-mouse drag, space+drag; preserves zoom; table-drag (010) stays intact. Natural follow-up to zoom (009). File: `docs/backlog/011-drawing-panning.md`.
6. **Backlog item 012 — Connectors never cross tables** ✅ **Done 2026-08-16**
   - "No connector segment crosses a table interior" is now a hard invariant. `OrthogonalRouter.Route` retries A* without thin obstacles when they form a barrier, `SnapStub` finds clear cells, and the Z fallback tries HV/VH variants that avoid tables. 0 crossings across the 50-table schema (was 143). File: `docs/backlog/012-connectors-never-cross-tables.md`.
7. **Backlog item 013 — Dragging a table hangs the app** (bug report, **fixed 2026-08-16**)
   - **Root cause:** (1) `GetCurrentPoint(null)` is window-relative → drag delta wrong at non-100% zoom → table flung `zoom×` too far. (2) A* re-route cost grows quadratically with canvas size → minutes. **Fix:** `GetCurrentPoint(_canvas)`; partial re-route (only the moved table's edges); `RouterOptions.MaxExpansions` node budget. Measured: 4.2 s → 2.2 s drag release; 20000 px case capped. 45/45 tests pass. Remaining: per-edge timing diagnostics (deferred). File: `docs/backlog/013-drag-table-hangs-app.md`.

### Known gaps / issues (candidates for backlog items)

- Deferred from backlog 002 scope: `Graphics/Primitives/Connector.cs` (unreferenced, mixes the two stacks); public API typos `IDiagnosticWritter` / `RoundCorderRadious`; no shared drawing-surface abstraction over the two graphics stacks.
- Only the **Skia stack** is extracted into ModelGraphLibrary; the XAML `Graphics` stack still lives inside the app project (WinUI-bound). Splitting it out is possible but buys no portability.
- ModelGraphLibrary keeps the app's namespaces (`ModelConsole.*`, `Model.Data`) — namespace reorganization to the library's own identity is a candidate follow-up.
- `SkiaPanelControl` (Skia stack) is DI-converted but not wired into `MainWindow`; the Skia stack is the one intended for the Uno/WebAssembly sibling.
- Test project exists (backlog 007) but only covers ModelGraphLibrary's pure modules; the XAML `Graphics` stack and the WinUI app have no automated tests.
- Routed connectors have no corner rounding (deferred from backlog 007) — rounding can re-intersect obstacles in tight gaps.
- Deferred from backlog 010: editing table text; add/remove whole tables and columns (inspector edit comes first); undo/redo (the model/view separation keeps it possible); live re-route during drag (re-route on release first; optimize to only re-route edges touching the changed table later).
- The layout is user-driven once a table is dragged (dragging overrides the grid); there is no auto-arrange/re-layout after drags, so a moved table can overlap a neighbour.
- csproj references `PublishProfile=win10-$(Platform).pubxml` but no `.pubxml` files exist (`NETSDK1198` warning).
- `Log.Write` is inert (`EVENT_LOG_SUPPORT` not defined); diagnostics flow through `ResultLog.DefaultLog` instead.

---

## Handoff notes

- Build command that works: `dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64`.
- Run from Visual Studio via the **ModelWinUI (Unpackaged)** launch profile for fastest iteration.
- When starting a new piece of work: create a backlog item from `docs/backlog/_TEMPLATE.md`, then update this file when done.
- **DI (from backlog 002):** the composition root is `App.ConfigureServices()` in `App.xaml.cs`; `Ioc.Default.ConfigureServices(Services)` runs **before** `MainWindow` is created — keep that order or `Ioc.Default.GetRequiredService<T>()` throws at startup. Container is frozen after `BuildServiceProvider()`; `App.Services` and `Ioc.Default` are the same provider.
- **DI pattern:** `Ioc.Default` is the only sanctioned service-locator, for **XAML-instantiated controls only**; code-created objects use constructor injection. New services register in `App.ConfigureServices`.
- **Lifetime rules:** `DiagnosticsLogViewModel` and `LogService` **must be singletons** (transients double-wire the static log event and leak subscriptions); `IGlModel` must be **transient** (a singleton accumulates items across draws). `SkiaPanelControl` stays compiled-but-unwired.
- **XAML instantiation order matters:** `ModelEditorControl.xaml` declares `DiagnosticsLogControl` before `ModelPanelControl` so the log VM subscribes before "GL Context Ready." is written — keep that order.
- **Container package:** `Microsoft.Extensions.DependencyInjection` 10.0.10 (restored and building). Functionality map lives at `docs/codebase-functionality-map.md`.
- **ModelGraphLibrary (backlog 006):** the portable Skia stack + `Model.Data` now live in `src/ModelGraphLibrary` (plain `net10.0`, `SkiaSharp` core, `RootNamespace=ModelConsole`, namespaces unchanged). Build it alone with `dotnet build src/ModelGraphLibrary/ModelGraphLibrary.csproj`; the app references it. `SkiaPanelControl` and the `ISkiaTableFactory` DI registration still use `ModelConsole.Services` — unchanged.
- **Tests (backlog 007):** run with `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` (no WinUI needed — the test project targets plain `net10.0` and only references ModelGraphLibrary).
- **`ActualWidth` vs `ComputedWidth`:** `GlRectangle.Width/Height` return `ActualWidth`/`ActualHeight` — **0 until XAML layout**. All layout math in `ModelPanelControl` uses `Table.ComputedWidth`/`ComputedHeight` (valid right after construction).
- **Row-Y staleness (resolved by 010):** `Table.GetRowCenterY(columnName)` uses the row panels' absolute Ys (valid at draw time). It is static-layout correct only, but since 010 re-creates every table on each `Render()`, the row Ys are always recomputed from the current position — no staleness in practice.
