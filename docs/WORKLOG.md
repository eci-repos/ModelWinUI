# Worklog

Running record of work done and next pending tasks. **Read this first** when starting work; update it when you finish. This is the handoff document for other agents.

## How to use

- **Done** — append a dated entry for each piece of work you complete.
- **Pending** — keep the pending list current: add tasks you discover, mark them done as you go.
- **Handoff** — anything the next agent must know (decisions, gotchas, half-finished work) goes in the current entry.

---

## Done

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

1. **Backlog item 003 — ERD graphics primitives base library** (from project README roadmap)
   - Define and draw Table and constraint connectors (lines/symbols) as a reusable library.
   - Current state: `Table` primitive and `GlOrthoPath` connectors exist but are early-stage; `GlModel` collection is now functional behind `IGlModel`.
   - Candidate for the next sprint (`docs/sprints/CURRENT.md` is empty).
2. **Backlog item 004 — UI controls for viewing the data model** (roadmap)
   - Develop the controls needed to view a model (beyond the current sample drawing).
3. **Backlog item 005 — Non-trivial sample models** (roadmap)
   - Ship sample models showing the tool's capabilities.

### Known gaps / issues (candidates for backlog items)

- Deferred from backlog 002 scope: `Graphics/Primitives/Connector.cs` (unreferenced, mixes the two stacks); public API typos `IDiagnosticWritter` / `RoundCorderRadious`; no shared drawing-surface abstraction over the two graphics stacks.
- `SkiaPanelControl` (Skia stack) is DI-converted but not wired into `MainWindow`; the Skia stack is the one intended for the Uno/WebAssembly sibling.
- No test projects exist.
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
