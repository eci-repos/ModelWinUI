# 002 — Dependency injection & coding-standards cleanup

## Summary

The codebase has no composition root and no DI: every component is `new`ed at point of use, services are reached through static singletons (`ResultLog.DefaultLog`, `ResultLog.LogMessageHandler`) and static factories (`Table.DrawTable`, `GlRectangle.Create/Draw/AddBanner`, `GlOrthoPath.Draw`, `Data_Table_Entity.GetPersonTable`). Make dependency injection the main mechanism for exposing functionality (services) and components (controls + graphics primitives), and fix coding-standard violations found in a full codebase scan.

## Goals

- [ ] Establish a DI composition root in `App` using `Microsoft.Extensions.DependencyInjection`.
- [ ] Expose services behind interfaces: logging, sample-data provider, and the graphics factories (XAML + Skia stacks).
- [ ] Bridge DI to XAML-instantiated controls via CommunityToolkit.Mvvm `Ioc.Default`; constructor injection for code-created objects.
- [ ] Make `IGlModel` functional (`GlModel.Add` was a null-returning stub).
- [ ] Remove dead usings found in the codebase scan.

## Scope

**In scope:**
- Composition root + container registration in `App.xaml.cs`.
- `Services/` interfaces + implementations: log, data provider, and factories for Table, Skia Table, connector, rectangle.
- Conversion of `GlContext`, `ModelPanelControl`, `DiagnosticsLogViewModel`, `DiagnosticsLogControl`, `SkiaPanelControl` to DI.
- Dead-using cleanup in the graphics libraries.

**Out of scope:**
- A shared drawing-surface abstraction over the two graphics stacks (they keep internal structure).
- Public API renames (`IDiagnosticWritter`, `RoundCorderRadious`) — typo kept by decision.
- `Graphics/Primitives/Connector.cs` (mixes stacks, unreferenced).
- Wiring `SkiaPanelControl` into `MainWindow`.

## Approach / Notes

- Container: `Microsoft.Extensions.DependencyInjection` 10.0.10; `Ioc.Default` (`CommunityToolkit.Mvvm`) is the **only sanctioned service-locator point**, for XAML-instantiated controls only. Code-created objects use constructor injection.
- `LogService` wraps the static `ResultLog.DefaultLog` and bridges its instance `LogMessageHandler` event to the static `ResultLog.LogMessageHandler` (read-modify-write delegate access; the singleton makes instance ≡ static).
- **Lifetimes:**
  - Singleton (stateless, one process-wide identity): `ILogService`, `IModelDataProvider`, `ITableFactory`, `ISkiaTableFactory`, `IConnectorFactory`, `IRectangleFactory`, `DiagnosticsLogViewModel`.
  - Transient (fresh per draw): `IGlModel` — a singleton would accumulate items across draws.
- `DiagnosticsLogViewModel` and `LogService` must be singletons — transients would double-wire the static log event and leak subscriptions.
- Behavior change (intended): `GlContext.WriteMessage` previously hit an always-null `Writer` field (silent no-op); it now logs through `ILogService` — "GL Context Ready." actually appears in the log panel.

## Definition of Done

- [x] `dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64` → **0 errors**.
- [x] App launches unpackaged; window "EDAM Studio" created/responding; sample tables + connectors render; log panel shows "Diagnostics Log Started" and "GL Context Ready.".
- [x] `docs/WORKLOG.md` updated; sprint record promoted to `docs/sprints/archive/`.

## Status

- **State:** Completed / Archived
- **Sprint:** `docs/sprints/archive/sprint-2026-08-15-dependency-injection.md`
- **Completed:** 2026-08-15
