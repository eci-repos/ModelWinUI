# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**EDAM Studio** — a data-model (ERD) visualization tool. This repo is the WinUI 3 desktop app (`ModelWinUI`), which serves as a fast-prototyping sibling of a planned Uno Platform WebAssembly app. The graphics code is written to be portable: SkiaSharp runs on both WinUI and WebAssembly, so the Skia-based stack is the one intended to move to the WebAssembly sibling unchanged.

The project is early-stage: the graphics primitives (tables, orthogonal connectors, grips/handles) are the focus. Sample tables are drawn programmatically in `ModelPanelControl`; there is no file I/O or model-editing UI yet.

## Build & Run

The project targets `net10.0-windows10.0.19041.0` (WinUI 3 / Windows App SDK 2.4.0). It runs **unpackaged** (`<WindowsPackageType>None</WindowsPackageType>` in the csproj — required for direct exe launch; without it the WinAppSDK auto-initializer throws `COMException 0x80040154`). A platform must be specified — AnyCPU builds fail with "cannot be ProcessorArchitecture neutral".

```powershell
# Build (x64; also x86 / ARM64)
dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64

# Build the whole solution
dotnet build ModelWinUI.sln -p:Platform=x64

# Build the portable graphics library alone (no WinUI dependencies)
dotnet build src/ModelGraphLibrary/ModelGraphLibrary.csproj -c Debug

# Run the unit tests (pure net10.0 — no WinUI needed)
dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug
```

Run from Visual Studio using the launch profiles in `Properties/launchSettings.json`:
- **ModelWinUI (Unpackaged)** — `commandName: Project` (fastest for dev)
- **ModelWinUI (Package)** — `commandName: MsixPackage`

The unit-test project `tests/ModelGraphLibrary.Tests` (xUnit, net10.0) covers ModelGraphLibrary's pure modules: schema integrity, FK edge extraction, table layout, and orthogonal routing.

Known build warnings (not errors):
- `NETSDK1198` — the csproj sets `PublishProfile=win10-$(Platform).pubxml` but no `.pubxml` files exist in the repo. Harmless for `dotnet build`.

Key package versions: `Microsoft.WindowsAppSDK` 2.4.0, `SkiaSharp.Views.WinUI` 4.151.1, `CommunityToolkit.Mvvm` 8.4.2, `Microsoft.Windows.SDK.BuildTools` 10.0.28000.2526. Runtime identifiers are `win-x86;win-x64;win-arm64` (the `win10-*` RID prefix was removed in .NET 8+).

## Architecture

### UI control hierarchy

```
App (App.xaml.cs)
└── MainWindow (title "EDAM Studio")
    └── ModelEditorControl            (Controls/ModelEditorControl.xaml)
        ├── ModelPanelControl         (left) — hosts the drawing Canvas
        └── right column
            ├── DiagnosticsLogControl (top) — log list view
            └── EntityInspectorControl (bottom) — entity metadata inspector
```

- `ModelPanelControl` creates a `GlContext` over its XAML `GlCanvas` and draws the sample model: a 50-table public-safety schema with every FK routed around the tables as an obstacle-avoiding `GlOrthoPath` connector (Canvas wrapped in a zoomable ScrollViewer). Connectors are anchored per-edge via `ConnectorAnchors`, routed sequentially via `SequentialRouter.RouteAll` (each routed edge becomes an obstacle for the next), and marked with 8 px `GlEllipse` endpoint circles. A zoom toolbar (fit button + slider + % box) drives the ScrollViewer's native zoom (`ChangeView`); Ctrl+0/1/Plus/Minus are wired as `KeyboardAccelerator`s. **Mouse panning (backlog 011):** left-drag on empty canvas space, middle-drag, or space+drag pans the drawing — `GlContext` raises `PanRequested(dx, dy)` (content-space delta from the pan start) and `ModelPanelControl` feeds `ChangeView(offset - delta, null, true)`, so panning is 1:1 with the pointer at any zoom and never resets it. The `GlCanvas` has `Background="Transparent"` (a Panel with a null Background is not hit-testable in its empty areas — without it, empty-space presses never reach the pan gesture). The canvas is a large fixed "paper" (`CanvasSize = 20000`) with the content centered in it, so panning has room in all directions; the router region stays tight around the content (`_contentBounds` ± `ExtentMargin`) so the A* grid does not grow with the paper. `FitToWindow` fits to `_contentBounds` (all tables), not the canvas extent; the app starts at 100% zoom showing the content's top-left. This is the entry point for exercising the graphics library.
- **The canvas is editable (backlog 010):** the drawing is always *derived* from the model state — `ModelPanelControl` holds `_tables` (the `TableInfo` model) + `_layout` (current table positions) and `Render()` re-draws everything from that state. Dragging a table updates the layout and re-runs the pipeline (connectors follow); clicking a table or connector raises `EntitySelected`; `DeleteConnector` removes the FK `ConstraintInfo` from the model and re-renders so the remaining connectors regenerate as simple non-crossing routes. `EntityInspectorControl` shows the clicked entity's metadata (editable column data types → re-render) and offers a Delete button for connectors.
- `MainWindow` hosts **both renderers** with a slim renderer bar (backlog 003): two mutually-exclusive `ToggleButton`s — "XAML model" (default) and "Skia render" — swap `ModelEditorControl` and `SkiaPanelControl` in a shared grid. Both are XAML-instantiated (`Ioc.Default` is configured before `MainWindow` is created).
- `SkiaPanelControl` is the **Skia render path** (backlog 003): renders the full public-safety ERD (50 tables, 74 FKs). It composes the diagram **once** on first paint — `ErdComposer.Compose` (routing takes seconds, never per frame) — caches the `ErdDiagram`, and replays tables + connectors per paint. It is a flat canvas (no zoom/pan/inspector); the XAML path keeps those.

### Two parallel graphics stacks

The codebase contains two independent graphics libraries. Do not confuse them — they share class names (`GlObject`, `GlModel`, `Table`) under different namespaces.

1. **`ModelConsole.Graphics.GLibrary`** (active) — WinUI XAML `Shape`-based rendering onto a `Canvas`.
   - `GlContext` wraps the `Canvas` and owns all pointer handling (press/move/release/capture), selection, and the current grabber. It implements `IDiagnosticWritter` and logs through `ILogService` (resolved via the DI container). It raises `ShapeReleased` / `ShapeClicked` (drag vs click via a 2 px movement threshold) and `PanRequested(dx, dy)` (pan gesture — left-drag on empty space, middle-drag, or space+drag; mouse only, delta in content units from the pan start) and `Reset()` clears interaction state before a full re-render. Cursor feedback (hand over empty space, move cursor while panning) goes through the `GlCanvas` subclass, which exposes the protected `UIElement.ProtectedCursor`.
   - `GlObject` (abstract) is the base for all drawable objects: `DeltaMove`, `Move`, `PointerEvent`, `Reshape`, `Selected`. Each object wraps a native XAML `Shape` (e.g. `GlRectangle` wraps a `Rectangle`, `GlOrthoPath` wraps a `Path`) and carries a `Data` payload (e.g. a connector's `FkRelation`).
   - Interaction model: `GlGrip` (resize nodes) and `GlHandle` (move) both implement `IGlGrabber`; `GlContext.SetPointerHandle` decides which one is active based on hit-testing `IGlGrip` nodes.
   - `GlOrtho/GlOrthoPath` draws orthogonal rounded-edge connector lines between shapes, with three grip nodes (start, end, middle) for reshaping.
   - `GlEllipse` is a small ellipse primitive — `Draw(context, centerX, centerY, diameter, fill)` positions it centered on a point (used for connector endpoint markers).
   - `Graphics/Primitives/Table.cs` renders a `TableInfo` as a rounded rectangle with a banner (`schema::table`) and one `TableRowPanel` per column (constraint text, name, data type). It exposes `TableInfo` (the metadata it renders; columns shared with the model) and its rows panel + banner are hit-test-transparent so the whole table drags. The banner sits on a pastel header band colored by `TableKindClassifier` — light blue `#DCE9F7` for entity tables, light green `#E2EFDA` for reference-code lookups.
   - `GlModel` implements `IGlModel` (`Items` list; `Add` adds and returns the instance). Resolved **transiently** from the DI container (a singleton would accumulate items across draws).

2. **`ModelConsole.Skia.GLibrary`** (portable — the Skia render path) — SkiaSharp rendering onto an `SKSurface`.
   - `GlFrame` wraps the `SKCanvas`, sets up default paints, and manages the coordinate system.
   - `Skia/Primitives/Table.cs` is the Skia counterpart of the XAML `Table`, with `ComputedWidth`/`ComputedHeight`/`GetRowCenterY` parity members (measure + anchor before drawing, backlog 003).
   - `Skia/Primitives/Connector.cs` (backlog 003) strokes a routed `Point2` polyline via `SKPathBuilder` + filled endpoint markers — the stack's first connector primitive. Null/empty points ⇒ no-op.
   - `Skia/Primitives/ErdComposer.cs` (backlog 003) is the reusable composition API: `Compose(tables, frame, options)` → `ErdDiagram` (Layout/Edges/Routes/Issues) — measure probes → `TableLayoutEngine` → `FkEdgeExtractor` → `ConnectorAnchors.Resolve`+`FanOut` → `SequentialRouter.RouteAll`. "Define and draw an ERD by writing code."
   - This is the stack intended for the Uno/WebAssembly sibling; keep it free of WinUI-specific dependencies.
   - **Lives in its own project `src/ModelGraphLibrary/`** (plain `net10.0`, `SkiaSharp` core) together with the `Model.Data` metadata model and the `ISkiaTableFactory` / `ISkiaConnectorFactory` contracts. The app references it via `ProjectReference`. The XAML `Graphics` stack stays in the app (WinUI-bound).

### Data model (`Model.Data`)

Relational metadata classes: `CatalogInfo`, `TableInfo`, `ColumnInfo`, `ColumnList`, `ConstraintInfo`. `TableInfo` supports JSON round-tripping (`ToJson`, `ToJsonFile`, `FromJsonFile`). **These POCOs live in `ModelGraphLibrary`** (namespace `Model.Data`). `ConstraintInfo` carries nullable FK parent references (`ReferencedTableName` / `ReferencedColumnName`; null column ⇒ resolve to the parent's PK). `TableKind` + `TableKindClassifier.Classify(TableInfo)` classify a table as an **entity** or a **reference-code** lookup (`Ref*` name prefix, or a small code/ID-key + `Description` table with ≤ 3 columns) — this drives the pastel table-header colors. Sample data stays in the app at `Model/ModelData/Data_Table_Entity.cs` (namespace `Model.Test`) — `GetPersonTable()` / `GetPersonNameTable()` are the fixtures used by the Skia path.

### Pure graph modules (`ModelConsole.Graph`)

Portable, deterministic, unit-tested geometry + routing in `ModelGraphLibrary` (no Windows.Foundation):
- `Geometry` — `Point2` / `Rect2` structs with strict-interior segment/rect tests.
- `FkEdgeExtractor` — resolves `ConstraintInfo` FK references into `FkRelation` edges (parent PK default), reporting issues and skipping bad edges.
- `TableLayoutEngine` — row-major grid layout of `TableInfo` into non-overlapping `Rect2` slots.
- `OrthogonalRouter` — A* grid pathfinding with obstacle inflation, outward stubs snapped to clear cells, collinear simplification, and an orthogonal Z-path fallback. Accepts `thinObstacles` (non-inflated, e.g. already-routed connectors) and checks segment crossings so a grid step cannot jump over a thin obstacle. **No connector crosses a table interior (backlog 012):** when the thin obstacles form a barrier that makes the grid unreachable, A* retries without them (crossing a connector is acceptable when the alternative is crossing a table); the Z fallback tries HV/VH variants and returns the first that avoids tables.
- `ConnectorAnchors` — `AnchorSide` + `Resolve` (departure side from child/parent relative position) + `FanOut` (offset shared-column anchors perpendicular to the side).
- `SequentialRouter` — `RouteAll` routes edges in deterministic order, feeding each routed polyline back as a thin obstacle so later edges avoid crossing it.

These are pure static library calls — the app calls them directly (no DI registration). The 50-table fixture lives in `ModelConsole.ModelData.PublicSafetySchema` (`ModelGraphLibrary/ModelData/`): 50 tables, 74 FK edges across the public-safety / criminal-justice domain.

### Diagnostics (`Model/Diagnostics`)

A logging subsystem ported from the author's earlier framework. The key wiring:
- `ResultLog.DefaultLog` is the process-wide log; `ResultLog.LogMessageHandler` is a static event.
- `DiagnosticsLogViewModel` subscribes through `ILogService.LogMessageHandler` (the DI wrapper that bridges the static event) and appends `IMessageLogEntry` items to an `ObservableCollection` bound by `DiagnosticsLogControl`.
- `GlContext.WriteMessage` is the path graphics code uses to log. `Log` (file/event-log writer) is largely inert here (`EVENT_LOG_SUPPORT` is not defined, so `Log.Write` returns false).

### MVVM

`CommunityToolkit.Mvvm` is referenced; `Model/Helpers/ObservableObject` is a hand-rolled base. `DiagnosticsLogViewModel` is the only view model so far.

### Dependency injection

**Dependency injection is the main mechanism for exposing functionality (services) and components (controls + graphics primitives).** The composition root is `App.ConfigureServices()` (`Microsoft.Extensions.DependencyInjection` 10.0.10). It runs **before** `MainWindow` is created and bridges to CommunityToolkit.Mvvm via `Ioc.Default.ConfigureServices(Services)`.

- **Resolution rules:** `Ioc.Default.GetRequiredService<T>()` is the **only sanctioned service-locator point**, and only for **XAML-instantiated controls** (they have no parameterized ctor). Code-created objects use **constructor injection**.
- **Service layer:** `Services/` (namespace `ModelConsole.Services`) holds interfaces + implementations — `ILogService` (wraps `ResultLog.DefaultLog`, bridges the static event), `IModelDataProvider` (sample fixtures), `ITableFactory` / `ISkiaTableFactory` (the two `Table` types), `IConnectorFactory`, `IRectangleFactory`.
- **Lifetimes:** stateless services + factories are **singletons**; `IGlModel` is **transient**; `DiagnosticsLogViewModel` and `LogService` must be singletons (transients double-wire the static log event and leak subscriptions).
- **Intended behavior change:** `GlContext.WriteMessage` now logs through `ILogService` — "GL Context Ready." actually reaches the log panel.

## Conventions

- Namespaces are `ModelConsole.*` (root namespace is `ModelWinUI` only for `App`/`MainWindow`).
- Graphics classes use the `Gl` prefix (`GlContext`, `GlRectangle`, `GlOrthoPath`).
- Code style is the author's: 3-space indentation in the graphics/data code, XML doc comments on most members, `m_` prefix on private fields.
- The `Skia` stack and the `Graphics` stack are meant to stay in sync conceptually; when adding a primitive, consider whether it needs a counterpart in both.
