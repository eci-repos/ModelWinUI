# Sprint 2026-08-15 — Dependency Injection

> Executed copy of the sprint. Backlog item: `docs/backlog/archive/002-dependency-injection.md`.

## Dates

- **Start:** 2026-08-15
- **End:** 2026-08-15

## Scope

- [x] `002` — Dependency injection & coding-standards cleanup

## Execution Log

- 2026-08-15 — Sprint defined from backlog item `002`. Container decisions confirmed: `Microsoft.Extensions.DependencyInjection` 10.0.10 + `Ioc.Default` bridge; "services + factories" scope.
- 2026-08-15 — Added `Microsoft.Extensions.DependencyInjection` 10.0.10 to `ModelWinUI.csproj`.
- 2026-08-15 — Created `Services/` (namespace `ModelConsole.Services`): `ILogService`/`LogService`, `IModelDataProvider`/`ModelDataProvider`, `ITableFactory`/`TableFactory`, `ISkiaTableFactory`/`SkiaTableFactory`, `IConnectorFactory`/`ConnectorFactory`, `IRectangleFactory`/`RectangleFactory`.
- 2026-08-15 — Added `Graphics/GLibrary/IGlModel.cs`; rewrote `GlModel` so `Add(GlObject)` adds + returns the instance (was a null-returning stub).
- 2026-08-15 — Composition root in `App.xaml.cs`: `ConfigureServices()` + `Ioc.Default.ConfigureServices(Services)` before `MainWindow` is created.
- 2026-08-15 — Converted `GlContext` to ctor `GlContext(Canvas, ILogService)`; `WriteMessage` now logs through the service (was a silent no-op via an always-null `Writer`).
- 2026-08-15 — Converted `ModelPanelControl`, `DiagnosticsLogViewModel` (ctor-injected `ILogService`), `DiagnosticsLogControl`, and `SkiaPanelControl` to DI via `Ioc.Default.GetRequiredService<T>()`.
- 2026-08-15 — Reordered `ModelEditorControl.xaml` so `DiagnosticsLogControl` subscribes to the log before `ModelPanelControl` writes "GL Context Ready.".
- 2026-08-15 — Removed dead usings: `GlRectangle`, `GlOrthoPath`, `GlGrip`, `GlHandle`, `GlGrabberBase`, `GlFrame`, `GlText`, `Skia/Primitives/Table`.
- 2026-08-15 — Build verified: `dotnet build ... -c Debug -p:Platform=x64` → **0 errors**. App run verified: `ModelWinUI.exe` launches unpackaged, window "EDAM Studio" responding, sample drawing runs, log panel shows startup messages.

## Results

- **Completed:** `002`
- **Deferred:** none
- **Notes:**
  - `SkiaPanelControl` is DI-converted but remains unwired in `MainWindow` (out of scope).
  - Public API typos (`IDiagnosticWritter`, `RoundCorderRadious`) kept by decision; `Graphics/Primitives/Connector.cs` left as-is; both deferred.
  - Pre-existing `NETSDK1198` warning (missing `.pubxml`) persists — harmless.
