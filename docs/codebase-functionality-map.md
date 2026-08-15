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
| **ModelGraphLibrary project** | — | Portable graph library (Skia stack + data model + factory contract) | Experimental / unwired |
| `ModelGraphLibrary/Model/Data` | `Model.Data` | Relational metadata model (POCOs, JSON round-trip) — moved out of the app (backlog 006) | Active |
| `ModelGraphLibrary/Skia/GLibrary` | `ModelConsole.Skia.GLibrary` | Portable 2D vector graphics engine (Skia stack) — moved out of the app | Experimental / unwired |
| `ModelGraphLibrary/Skia/Primitives` | `ModelConsole.Skia.Primitives` | Portable domain primitives | Experimental / unwired |
| `ModelGraphLibrary/Services` | `ModelConsole.Services` | `ISkiaTableFactory` / `SkiaTableFactory` — the library's public factory contract | Active |
| `Model/Diagnostics` | `ModelConsole.Model.Diagnostics` | Diagnostics & logging infrastructure | Active |
| `Model/ModelData` | `ModelConsole.Model.Test` | Sample data fixtures | Active |
| `Model/DataObjects` | `ModelConsole.Model.DataObjects` | Domain value objects | Active |
| `Model/Helpers` | `ModelConsole.Model.Helpers` | MVVM base infrastructure | Active |
| `Graphics/GLibrary` | `ModelConsole.Graphics.GLibrary` | 2D vector graphics engine (XAML stack, stays in the app) | Active |
| `Graphics/GLibrary/GlOrtho` | `ModelConsole.Graphics.GLibrary.GlOrtho` | Orthogonal connector routing | Active |
| `Graphics/Primitives` | `ModelConsole.Graphics.Primitives` | Domain-specific primitives (Table, rows) | Active (partly deferred) |
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
| `ConstraintInfo` | Constraint metadata (PK/FK/unique, etc.) |

### Sample data fixtures — `Model/ModelData` (Active)

`Data_Table_Entity` (`namespace Model.Test`) — `GetPersonTable()` / `GetPersonNameTable()`, the fixtures used by the sample drawings. Reached through `IModelDataProvider` (DI).

### Domain value objects — `Model/DataObjects` (Active)

`DataElementName` — reusable value object for element names.

### MVVM base infrastructure — `Model/Helpers` (Active)

`ObservableObject` — hand-rolled MVVM base (CommunityToolkit.Mvvm is also referenced; this is used by `DiagnosticsLogViewModel`).

### 2D vector graphics engine (XAML stack) — `Graphics/GLibrary` (Active)

WinUI XAML `Shape`-based rendering onto a `Canvas`. The active rendering path.

| Type | Role |
|---|---|
| `GlContext` | Wraps the `Canvas`; owns pointer handling, selection, grabber dispatch, logging (via `ILogService`) |
| `GlObject` (abstract) | Base drawable: `Move`, `DeltaMove`, `PointerEvent`, `Reshape`, `Selected` |
| `GlModel`, `IGlModel` | Drawable collection — `Add` now functional (was a null-returning stub) |
| `GlRectangle` | Rectangle primitive (+ banner via `AddBanner`) |
| `GlBoundingBox`, `GlBoxInfo`, `GlObjectInfo`, `GlPointerEvent`, `GlColor`, `GlDirection`, `GlSide` | Geometry/state support types |
| `GlGrip`, `GlHandle`, `GlGrabberBase`, `IGlGrip`, `IGlGrabber` | Resize/move interaction model |
| `GlTextBox` | Text primitive |

### Orthogonal connector routing — `Graphics/GLibrary/GlOrtho` (Active)

| Type | Role |
|---|---|
| `GlOrthoPath` | Orthogonal rounded-edge connector between shapes, three grip nodes (start, end, middle) |
| `GlOrthoPathBuilder`, `GlOrthoPathItem`, `GlOrthoPathItemType`, `GlOrthoPathShape` | Path construction support |

### Domain-specific primitives — `Graphics/Primitives` (Active, partly deferred)

| Type | Role |
|---|---|
| `Table` | Renders a `TableInfo` as rounded rectangle + banner + one `TableRowPanel` per column |
| `TableRowPanel` | Single-column row rendering |
| `Connector` | **Deferred** — mixes the two stacks and is unreferenced |

### Portable 2D vector graphics engine (Skia stack) — `ModelGraphLibrary/Skia/GLibrary` (Experimental, unwired)

SkiaSharp rendering onto an `SKSurface`. Lives in the ModelGraphLibrary project (plain `net10.0`). The stack intended for the Uno/WebAssembly sibling — keep it free of WinUI-specific dependencies.

| Type | Role |
|---|---|
| `GlFrame` | Wraps `SKCanvas`; default paints + coordinate system |
| `GlModel` | Skia drawable collection (separate type from the XAML `GlModel`) |
| `GlObject` | Skia drawable base |
| `GlText` | Text drawing (SkiaSharp 4.x: `SKFont` + `DefaultTextPaint`) |
| `GlBoxInfo`, `GlMatrix`, `GlObjectGeometryInfo`, `GlObjectInfo`, `GlPalette` | Geometry/state support types |

### Portable domain primitives — `ModelGraphLibrary/Skia/Primitives` (Experimental, unwired)

`Table` (Skia counterpart of the XAML `Table`), `RectangleHalf` — both WinUI-free.

### DI service contracts + implementations — `Services` (Active, split across two projects)

Introduced by backlog item `002`. All registered in `App.ConfigureServices`. The Skia factory contract now lives **inside ModelGraphLibrary** (backlog 006) as the library's public interface; the XAML-stack factories stay in the app's `Services/`.

| Service | Project | Implements |
|---|---|---|
| `ILogService` / `LogService` | app | Logging via `ResultLog.DefaultLog`; instance `LogMessageHandler` event bridges the static one |
| `IModelDataProvider` / `ModelDataProvider` | app | Sample fixtures (`GetPersonTable`, `GetPersonNameTable`) |
| `ITableFactory` / `TableFactory` | app | XAML `Table` creation over a `GlContext` |
| `ISkiaTableFactory` / `SkiaTableFactory` | **ModelGraphLibrary** | Skia `Table` creation over a `GlFrame` |
| `IConnectorFactory` / `ConnectorFactory` | app | `GlOrthoPath` connector creation |
| `IRectangleFactory` / `RectangleFactory` | app | `GlRectangle` create/draw/banner |

### Presentation layer — `Controls` (Active)

| Control | Role |
|---|---|
| `ModelEditorControl` | Layout shell: `ModelPanelControl` (left) + `DiagnosticsLogControl` (right) |
| `ModelPanelControl` | Hosts the drawing `Canvas`; constructs `GlContext`; draws the sample model |
| `DiagnosticsLogControl` | Log list view bound to `DiagnosticsLogViewModel.Items` |
| `SkiaPanelControl` | **Unwired** alternative rendering path using the Skia stack (not referenced by `MainWindow`) |

### Presentation logic — `ViewModels` (Active)

`DiagnosticsLogViewModel` — the only view model; subscribes to `ILogService.LogMessageHandler` and appends entries to an `ObservableCollection`. **Must be a singleton** in the container (a transient would leak a static-event subscription per instance).

### Application shell — `App` / `MainWindow` (Active)

- `App.xaml.cs` — **DI composition root**: `ConfigureServices()` builds the container and registers all services; `Ioc.Default.ConfigureServices(Services)` bridges resolution for XAML-instantiated controls.
- `MainWindow` — the "EDAM Studio" window hosting `ModelEditorControl`.
