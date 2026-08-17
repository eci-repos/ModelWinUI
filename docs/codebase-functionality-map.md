# Codebase Functionality Map

> Scan deliverable (backlog item `002`): each library/component in the codebase, its namespace, the functionality it provides, and its current status. Use this as the starting point when exploring a subsystem or planning work.

## Legend

| Status | Meaning |
|---|---|
| **Active** | Used by the running app's main path. |
| **Unwired** | Compiled, DI-registered, but not referenced by the live UI. |
| **Deferred** | Kept as-is by decision; candidate for a future backlog item. |
| **Inert** | Present but effectively dead code (compile-time `#if` disables it, or a stub). |
| **Experimental** | Working stack intended for a future target (Uno/WebAssembly). |

## Component map

| Library / component | Namespace | Functionality label | Status |
|---|---|---|---|
| **ModelGraphLibrary project** | — | Portable graph library (Skia stack + data model + factory contracts) | Active |
| `ModelGraphLibrary/Model/Data` | `Model.Data` | Relational metadata model (POCOs, JSON round-trip) + `TableKind`/`TableKindClassifier` (entity vs reference-code) — moved out of the app (backlog 006) | Active |
| `ModelGraphLibrary/Skia/GLibrary` | `ModelConsole.Skia.GLibrary` | Portable 2D vector graphics engine (Skia stack) — moved out of the app | Active (Skia render path) |
| `ModelGraphLibrary/Skia/Primitives` | `ModelConsole.Skia.Primitives` | Portable domain primitives — `Table`, `Connector`, `ErdComposer` | Active (Skia render path) |
| `ModelGraphLibrary/Services` | `ModelConsole.Services` | `ISkiaTableFactory` / `SkiaTableFactory` + `ISkiaConnectorFactory` / `SkiaConnectorFactory` — the library's public factory contracts | Active |
| `ModelGraphLibrary/Graph` | `ModelConsole.Graph` | Pure geometry + FK edge extraction + grid layout + A* orthogonal routing + sequential routing + connector anchors (unit-tested) | Active |
| `ModelGraphLibrary/ModelData` | `ModelConsole.ModelData` | `PublicSafetySchema` — 50-table / 74-FK public-safety fixture | Active |
| `tests/ModelGraphLibrary.Tests` | `ModelConsole.Tests` | xUnit unit tests over ModelGraphLibrary's pure modules | Active |
| `Model/Diagnostics` | `ModelConsole.Model.Diagnostics` | Diagnostics & logging infrastructure | Active |
| `Model/ModelData` | `ModelConsole.Model.Test` | Sample data fixtures | Active |
| `Model/DataObjects` | `ModelConsole.Model.DataObjects` | Domain value objects | Active |
| `Model/Helpers` | `ModelConsole.Model.Helpers` | MVVM base infrastructure | Active |
| `Graphics/GLibrary` | `ModelConsole.Graphics.GLibrary` | 2D vector graphics engine (XAML stack, stays in the app) | Active |
| `Graphics/GLibrary/GlOrtho` | `ModelConsole.Graphics.GLibrary.GlOrtho` | Orthogonal connector routing | Active |
| `Graphics/Primitives` | `ModelConsole.Graphics.Primitives` | Domain-specific primitives (Table, rows) | Active |
| `Services` | `ModelConsole.Services` | DI service contracts + implementations (XAML-stack factories, log, data provider) | Active |
| `Controls` | `ModelConsole.Controls` | Presentation layer (UserControls) | Active |
| `ViewModels` | `ModelConsole.ViewModels` | Presentation logic (MVVM) | Active |
| `App` / `MainWindow` | `ModelWinUI` | Application shell + DI composition root | Active |

## Per-area detail

### Diagnostics & logging infrastructure — `Model/Diagnostics` (Active)

Logging subsystem ported from the author's earlier framework.

| Type | Role |
|---|---|
| `ResultLog` | Process-wide log; `DefaultLog` singleton + static `LogMessageHandler` event |
| `Log` | File/event-log writer — **inert** (`EVENT_LOG_SUPPORT` not defined; `Write` returns false) |
| `IResultsLog`, `IDiagnosticWritter` | Log contracts (note: `IDiagnosticWritter` typo kept by decision) |
| `MessageLogEntry`, `LogMessageEvent`, `LogMessageEventArgs` | Message model + event plumbing |
| `SeverityLevel`, `Verbosity`, `EventCode`, `LogStatus`, `LogFormat`, `LogSettings`, `DiagnosticsInfo` | Enum/config support types |

Entry point: `ILogService` (DI, in `Services/`) wraps `ResultLog.DefaultLog`.

### Relational metadata model — `ModelGraphLibrary/Model/Data` (namespace `Model.Data`, Active)

POCOs describing a relational schema; `TableInfo` supports JSON round-tripping. Moved from the app into ModelGraphLibrary in backlog 006 because the Skia `Table` primitive inherits `TableInfo`.

| Type | Role |
|---|---|
| `CatalogInfo` | Catalog (schema container) |
| `TableInfo` | Table metadata + `ToJson` / `ToJsonFile` / `FromJsonFile` |
| `ColumnInfo` | Column metadata (name, type, constraints) |
| `ColumnList` | Ordered column collection |
| `ConstraintInfo` | Constraint metadata (PK/FK/unique, etc.) — now carries nullable FK parent refs `ReferencedTableName` / `ReferencedColumnName` |
| `TableKind`, `TableKindClassifier` | Entity vs reference-code classification — `Ref*` name prefix, or a small lookup (code/ID key + `Description` column + ≤ 3 columns); drives the pastel table-header colors |

### Pure graph modules — `ModelGraphLibrary/Graph` (namespace `ModelConsole.Graph`, Active)

Portable, deterministic geometry + routing that the app uses to lay out tables and route FK connectors around them. All Windows.Foundation-free — the unit-test target and the code that would move to the Uno/WASM sibling. Pure static library calls; the app invokes them directly (no DI registration).

| Type | Role |
|---|---|
| `Point2`, `Rect2` | Portable geometry structs — `Contains`, `Intersects`, `SegmentCrossesInterior` (strict interior), `Inflate` |
| `FkRelation`, `FkEdgeExtractor` | Resolves `ConstraintInfo` FK references into `FkRelation` edges; `ReferencedColumnName ?? parent PK`; reports issues and skips bad edges; deterministic order |
| `TableLayoutEngine` | Row-major non-overlapping grid layout of `TableInfo` into `Rect2` slots |
| `OrthogonalRouter` | A* grid pathfinding — obstacle inflation, outward stubs snapped to clear cell centers, collinear simplification; `thinObstacles` (non-inflated) + A* segment-crossing check so a grid step cannot jump over a thin obstacle. **No connector crosses a table interior (backlog 012):** when the thin obstacles form a barrier that makes the grid unreachable, A* retries without them (crossing a connector is acceptable when the alternative is crossing a table); the Z fallback tries HV/VH variants and returns the first that avoids tables |
| `ConnectorAnchors` | `AnchorSide` enum; `Resolve` picks the departure side from the child/parent relative position; `FanOut` offsets shared-column anchors perpendicular to the side |
| `SequentialRouter` | `RouteAll` — routes edges in deterministic order, feeding each routed polyline back as a thin obstacle so later edges avoid crossing it |

### Schema fixture — `ModelGraphLibrary/ModelData` (namespace `ModelConsole.ModelData`, Active)

`PublicSafetySchema.Tables` — exactly 50 tables, 74 FK edges across 8 domain areas (Identity, Reference data, Agencies & personnel, Geography & facilities, Incidents & dispatch, Enforcement, Offenses & case, Courts & sentencing). One FK (SentenceCondition→Sentence) deliberately omits `ReferencedColumnName` to exercise the PK-default rule. Reached through `IModelDataProvider.GetPublicSafetyTables()` (DI).

### Unit tests — `tests/ModelGraphLibrary.Tests` (namespace `ModelConsole.Tests`, Active)

The repo's first test project (xUnit, net10.0, references ModelGraphLibrary only — no WinUI). 49 tests across `SchemaIntegrityTests`, `FkEdgeExtractorTests`, `TableLayoutEngineTests`, `OrthogonalRouterTests`, `ConnectorAnchorsTests`, `SequentialRouterTests`, `NoCrossingInvariantTests` (backlog 012 — asserts no routed segment crosses any table rect across the 50-table schema and tight/adversarial layouts). Run: `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug`.

### Sample data fixtures — `Model/ModelData` (Active)

`Data_Table_Entity` (`namespace Model.Test`) — `GetPersonTable()` / `GetPersonNameTable()`, the fixtures used by the Skia sample drawings. Reached through `IModelDataProvider` (DI). Kept intact alongside the new public-safety schema.

### Domain value objects — `Model/DataObjects` (Active)

`DataElementName` — reusable value object for element names.

### MVVM base infrastructure — `Model/Helpers` (Active)

`ObservableObject` — hand-rolled MVVM base (CommunityToolkit.Mvvm is also referenced; this is used by `DiagnosticsLogViewModel`).

### 2D vector graphics engine (XAML stack) — `Graphics/GLibrary` (Active)

WinUI XAML `Shape`-based rendering onto a `Canvas`. The active rendering path.

| Type | Role |
|---|---|
| `GlContext` | Wraps the `Canvas`; owns pointer handling, selection, grabber dispatch, logging (via `ILogService`); raises `ShapeReleased`/`ShapeClicked` (drag vs click via a 2 px movement threshold) and `PanRequested(dx, dy)` (pan gesture — left-drag on empty space, middle-drag, or space+drag; mouse only, delta in content units from the pan start); `Reset()` clears interaction state before a full re-render |
| `GlObject` (abstract) | Base drawable: `Move`, `DeltaMove`, `PointerEvent`, `Reshape`, `Selected`; carries a `Data` payload (e.g. a connector's `FkRelation`) |
| `GlModel`, `IGlModel` | Drawable collection — `Add` now functional (was a null-returning stub) |
| `GlRectangle` | Rectangle primitive (+ banner via `AddBanner`) |
| `GlEllipse` | Small ellipse primitive — `Draw(context, centerX, centerY, diameter, fill)` positions it centered on a point (used for connector endpoint markers) |
| `GlCanvas` | `Canvas` subclass exposing the protected `UIElement.ProtectedCursor` as a public `Cursor` property — the official pattern for swapping the cursor (hand over empty space, move cursor while panning) |
| `GlBoundingBox`, `GlBoxInfo`, `GlObjectInfo`, `GlPointerEvent`, `GlColor`, `GlDirection`, `GlSide` | Geometry/state support types |
| `GlGrip`, `GlHandle`, `GlGrabberBase`, `IGlGrip`, `IGlGrabber` | Resize/move interaction model |
| `GlTextBox` | Text primitive |

### Orthogonal connector routing — `Graphics/GLibrary/GlOrtho` (Active)

| Type | Role |
|---|---|
| `GlOrthoPath` | Orthogonal rounded-edge connector between shapes, three grip nodes (start, end, middle); `DrawRouted` builds a connector from a pre-computed absolute polyline (obstacle-avoiding route) |
| `GlOrthoPathBuilder`, `GlOrthoPathItem`, `GlOrthoPathItemType`, `GlOrthoPathShape` | Path construction support |

### Domain-specific primitives — `Graphics/Primitives` (Active, partly deferred)

| Type | Role |
|---|---|
| `Table` | Renders a `TableInfo` as rounded rectangle + banner + one `TableRowPanel` per column; exposes `ComputedWidth`/`ComputedHeight` (valid pre-layout, unlike `ActualWidth`), `GetRowCenterY(columnName)` for connector anchoring, and `TableInfo` (the metadata it renders); rows panel + banner are hit-test-transparent so the whole table drags. The banner sits on a pastel header band colored by `TableKindClassifier` (light blue `#DCE9F7` for entities, light green `#E2EFDA` for reference codes) |
| `TableRowPanel` | Single-column row rendering |
| `Connector` | **Deleted** (backlog 003) — unreferenced stub referencing the Skia stack; superseded by the Skia `Connector` primitive |

### Portable 2D vector graphics engine (Skia stack) — `ModelGraphLibrary/Skia/GLibrary` (Active — the Skia render path)

SkiaSharp rendering onto an `SKSurface`. Lives in the ModelGraphLibrary project (plain `net10.0`). The stack intended for the Uno/WebAssembly sibling — keep it free of WinUI-specific dependencies.

| Type | Role |
|---|---|
| `GlFrame` | Wraps `SKCanvas`; default paints + coordinate system |
| `GlModel` | Skia drawable collection (separate type from the XAML `GlModel`) |
| `GlObject` | Skia drawable base |
| `GlText` | Text drawing (SkiaSharp 4.x: `SKFont` + `DefaultTextPaint`) |
| `GlBoxInfo`, `GlMatrix`, `GlObjectGeometryInfo`, `GlObjectInfo`, `GlPalette` | Geometry/state support types |

### Portable domain primitives — `ModelGraphLibrary/Skia/Primitives` (Active — the Skia render path)

`Table` (Skia counterpart of the XAML `Table`; backlog 003 added `ComputedWidth`/`ComputedHeight`/`GetRowCenterY` so tables can be measured and anchored before drawing), `Connector` (strokes a routed `Point2` polyline + filled endpoint markers via `GlFrame.Canvas` — the stack's first connector primitive, backlog 003), `RectangleHalf` — all WinUI-free. `ErdComposer` composes a full ERD as pure data (`ErdDiagram` — Layout/Edges/Routes/Issues): measure probes → `TableLayoutEngine` → `FkEdgeExtractor` → `ConnectorAnchors.Resolve`/`FanOut` → `SequentialRouter.RouteAll`. The "define and draw an ERD by writing code" API (backlog 003).

### DI service contracts + implementations — `Services` (Active, split across two projects)

Introduced by backlog item `002`. All registered in `App.ConfigureServices`. The Skia factory contract now lives **inside ModelGraphLibrary** (backlog 006) as the library's public interface; the XAML-stack factories stay in the app's `Services/`.

| Service | Project | Implements |
|---|---|---|
| `ILogService` / `LogService` | app | Logging via `ResultLog.DefaultLog`; instance `LogMessageHandler` event bridges the static one |
| `IModelDataProvider` / `ModelDataProvider` | app | Sample fixtures — `GetPersonTable`, `GetPersonNameTable`, `GetPublicSafetyTables` (50-table schema) |
| `ITableFactory` / `TableFactory` | app | XAML `Table` creation over a `GlContext` |
| `ISkiaTableFactory` / `SkiaTableFactory` | **ModelGraphLibrary** | Skia `Table` creation over a `GlFrame` |
| `ISkiaConnectorFactory` / `SkiaConnectorFactory` | **ModelGraphLibrary** | Skia `Connector` creation over a `GlFrame` (backlog 003) |
| `IConnectorFactory` / `ConnectorFactory` | app | `GlOrthoPath` connector creation — `Create` (fixed path) and `CreateRouted` (pre-computed polyline) |
| `IRectangleFactory` / `RectangleFactory` | app | `GlRectangle` create/draw/banner |

### Presentation layer — `Controls` (Active)

| Control | Role |
|---|---|
| `ModelEditorControl` | Layout shell: `ModelPanelControl` (left) + right column with `DiagnosticsLogControl` (top) and `EntityInspectorControl` (bottom); wires canvas clicks → inspector and inspector edit/delete → re-render |
| `ModelPanelControl` | Hosts the drawing `GlCanvas` (in a zoomable ScrollViewer) + a zoom toolbar (fit button + slider + % box); constructs `GlContext`; state-driven `Render()` pipeline — `_tables` (model) + `_layout` (positions) are the source of truth, the drawing is always derived from them. Draws the 50-table public-safety schema and routes every FK around the tables — anchors via `ConnectorAnchors.Resolve` + `FanOut`, sequential routing via `SequentialRouter.RouteAll`, 8 px `GlEllipse` endpoint markers. Drag a table → re-route on release; click an entity → `EntitySelected`; `DeleteConnector` removes the FK constraint and re-renders. Zoom via ScrollViewer native zoom (`ChangeView`), Ctrl+0/1/Plus/Minus accelerators. Pan via `GlContext.PanRequested` → `ChangeView(offset - delta, null, true)` (left-drag on empty space, middle-drag, space+drag; the `GlCanvas` has `Background="Transparent"` so empty-space presses hit-test). The canvas is a large fixed "paper" (`CanvasSize = 20000`) with the content centered in it, so panning has room in all directions; the router region stays tight around the content (`_contentBounds` ± `ExtentMargin`) so the A* grid does not grow with the paper. `FitToWindow` fits to `_contentBounds` (all tables), not the canvas extent; the app starts at 100% zoom showing the content's top-left |
| `EntityInspectorControl` | Entity inspector: clicking a table lists its columns with editable data types (commit → `ModelEdited`); clicking a connector shows the FK relationship with a Delete button (`DeleteRequested`) |
| `DiagnosticsLogControl` | Log list view bound to `DiagnosticsLogViewModel.Items` |
| `SkiaPanelControl` | Skia render of the full public-safety ERD (50 tables, 74 FKs): composes once on first paint (routing is seconds — never per paint), caches the `ErdDiagram`, replays tables + connectors per paint; logs counts + FK issues. Wired into `MainWindow`'s renderer bar (backlog 003) — "XAML model" / "Skia render" toggle swaps it with `ModelEditorControl` |

### Presentation logic — `ViewModels` (Active)

`DiagnosticsLogViewModel` — the only view model; subscribes to `ILogService.LogMessageHandler` and appends entries to an `ObservableCollection`. **Must be a singleton** in the container (a transient would leak a static-event subscription per instance).

### Application shell — `App` / `MainWindow` (Active)

- `App.xaml.cs` — **DI composition root**: `ConfigureServices()` builds the container and registers all services; `Ioc.Default.ConfigureServices(Services)` bridges resolution for XAML-instantiated controls.
- `MainWindow` — the "EDAM Studio" window hosting `ModelEditorControl`.
