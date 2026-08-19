# 031 — Extract the XAML Graphics stack into a reusable WinUI class library

## Summary

The XAML `Gl*` drawing stack (`Graphics/GLibrary` + `Graphics/Primitives`) and its six factory services live inside the app project, so **no other project can reference them**. Extract them into **`Model.Graphics.WinUI`** — a `net10.0-windows` class library (WindowsAppSDK) that any WinUI host app can reuse — referencing the portable layers split out in backlog 030. The two app-bound dependencies of the stack (`ILogService`, `IDiagnosticWritter`/`DiagnosticsInfo`) are already in `Model.Diagnostics` after 030, which is what makes this extraction possible. Purely structural; the app's DI registrations and behavior are unchanged.

## Goals

- [ ] New `src/Model.Graphics.WinUI/` project containing the 27 graphics files + 6 factory services.
- [ ] Library compiles standalone against Model.Data, Model.Graph, Model.Geometry, Model.Diagnostics.
- [ ] App references the library; build stays 0 errors / 0 warnings, all tests pass, and the XAML renderer behaves identically (drag, hover-highlight, inspector, pan/zoom/fit).

## Scope

**In scope:**
- Project creation + file moves, dead-using removal (compile blockers), app csproj wiring, docs.
- The XAML factory services move with the stack so the library is self-contained (mirroring how Model.Skia ships `ISkiaTableFactory`/`ISkiaConnectorFactory`).

**Out of scope:**
- Separating the stack's generic drawing primitives from its ERD-coupled ones (`Table` renders `TableInfo`, `GlOrthoPath` renders `FkRelation`) — a future refinement, noted in 032.
- Namespace renames (backlog 032).
- NuGet packaging / READMEs (backlog 032).

## Approach / Notes

- **csproj** mirrors the app's WinUI settings: `net10.0-windows10.0.19041.0`, `<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>`, `<RootNamespace>ModelConsole</RootNamespace>`, `<UseWinUI>true</UseWinUI>`, packages `Microsoft.WindowsAppSDK 2.4.0` + `Microsoft.Windows.SDK.BuildTools 10.0.28000.2526`. **No `.xaml` files move**, so there is no XAML-compiler surface (the flaky WMC1509/WMC0909 issue cannot arise here). `Platforms`/`RuntimeIdentifiers` are not required for a class library, but the sln build passes `-p:Platform=x64` so the csproj must accept it (the SDK default `AnyCPU` builds fine under an x64 solution platform mapping).
- **Files moving from the app (33):**
  - `Graphics/GLibrary/*` (25) — GlBoundingBox, GlBoxInfo, GlCanvas, GlColor, GlContext, GlDirection, GlEllipse, GlGrabberBase, GlGrip, GlHandle, GlModel, GlObject, GlObjectInfo, GlPointerEvent, GlRectangle, GlSide, GlTextBox, IGlGrabber, IGlGrip, IGlModel, and `GlOrtho/` (GlOrthoPath, GlOrthoPathBuilder, GlOrthoPathItem, GlOrthoPathItemType, GlOrthoPathShape).
  - `Graphics/Primitives/*` (2) — Table, TableRowPanel.
  - `Services/*` (6) — ITableFactory, TableFactory, IConnectorFactory, ConnectorFactory, IRectangleFactory, RectangleFactory.
- **References:** Model.Data (`TableInfo`/`TableKindClassifier`), Model.Graph (`IGraphNode`, `FkRelation`, `GraphNodes`), Model.Geometry (`Point2`), Model.Diagnostics (`IDiagnosticWritter`, `DiagnosticsInfo`, `Verbosity`, `ILogService`).
- **Dead usings that are compile blockers** (the library cannot compile while they reference a namespace the app owns): remove `using Model.Data;` from `GlColor.cs` and `GlRectangle.cs` (both unused), and the stale `using Windows.UI.Xaml;` from `GlTextBox.cs` (legacy UWP directive). Nothing else in the stack references app-owned namespaces — verified by the coupling map.
- **Stays in the app:** `Controls/`, `ViewModels/`, `Model/Helpers/` (ObservableObject), `Model/ModelData/Data_Table_Entity.cs`, `Model/DataObjects/DataElementName.cs`, `Services/IModelDataProvider.cs` + `ModelDataProvider.cs` (fixture glue), `App.xaml.cs`, `MainWindow.xaml.cs`, the `.xaml` pages.
- **App csproj:** remove the moved files from compile, add `<ProjectReference Include="..\Model.Graphics.WinUI\...">`. DI registrations in `App.ConfigureServices` are byte-for-byte unchanged (the six factory interface types now resolve from Model.Graphics.WinUI; `ILogService`/`LogService` from Model.Diagnostics). `ModelPanelControl`'s `new GlContext(ModelCanvas, ...ILogService)` and the `Ioc.Default.GetRequiredService<...>()` call sites compile unchanged because namespaces are preserved.
- **Docs:** WORKLOG entry + `CLAUDE.md` "Two parallel graphics stacks" section — the XAML stack is now a WinUI class library, not app code.

## Definition of Done

- [ ] `dotnet build src/Model.Graphics.WinUI/Model.Graphics.WinUI.csproj -c Debug -p:Platform=x64` → 0 errors.
- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings (pre-existing `NETSDK1198` warning allowed).
- [ ] `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` → all tests pass (unchanged from 030).
- [ ] Manual run: XAML renderer drag / hover-highlight / inspector / pan / zoom / fit all work; Skia renderer unchanged; `File → Open Sample` unchanged.
- [ ] `docs/WORKLOG.md` updated; `CLAUDE.md` reflects the WinUI class library.

## Status

- **State:** Planned
- **Sprint:** (not scheduled)
- **Completed:** —
