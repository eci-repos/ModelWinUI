# 034 — Extract the WinUI ERD controls into a reusable class library

## Summary

The six ERD editor controls — `ModelEditorControl`, `ModelExplorerControl`, `ModelPanelControl`, `DiagnosticsLogControl`, `EntityInspectorControl`, `SkiaPanelControl` (+ the `SkiaCanvasView` helper) — all live in the app project, along with the three app-owned types they resolve from DI (`IModelDataProvider`, `DiagnosticsLogViewModel` and its `ObservableObject` base). That means **no other project can reuse the "forms"** — the only way to get an editable ERD today is to use the console app. Backlog 031 extracted the XAML *drawing stack* (`Gl*` + `Table` + factories) into `Model.Graphics.WinUI`; this item extracts the **controls themselves** into a new **`Model.Controls.WinUI`** — a `net10.0-windows` WinUI class library any host can reference and drop into its own window. The controls are already decoupled from `App`/`MainWindow` (they talk outward only via `Ioc.Default` service resolution, `SetModel`, and events), so this is a structural move with no behavioral change.

## Goals

- [ ] New `src/Model.Controls.WinUI/` project holding the 6 editor controls + `SkiaCanvasView`, the `DiagnosticsLogViewModel` + `ObservableObject` base + `DataElementName` constants, and the `IModelDataProvider` interface + default `ModelDataProvider` (18 files).
- [ ] Library compiles standalone against the six existing libraries (Model.Diagnostics, Model.Geometry, Model.Data, Model.Graph, Model.Skia, Model.Graphics.WinUI).
- [ ] App references the library; build stays 0 errors / 0 warnings, all tests pass, and both renderers behave identically (drag, hover-highlight, inspector, pan/zoom/fit, File → Open Sample).

## Scope

**In scope:**
- Project creation + file moves, namespace re-names for the moving app-owned types, `using` ripples (App, tests none), app csproj cleanup, DI `using` updates (registrations unchanged in shape), sln update, README + packaging, docs.
- Retiring the dead `GetPersonTable`/`GetPersonNameTable` surface + `Data_Table_Entity.cs` (nothing outside `ModelDataProvider` calls them — the controls only consume `GetPublicSafetyTables`).

**Out of scope:**
- `MainWindow` and its renderer bar (XAML ↔ Skia toggle) — thin app wiring, not the reusable value; a host composes the two controls itself.
- Decoupling the controls from auto-loading the default sample (the controls call `GetPublicSafetyTables` at construction). Hosts already override via DI (register their own `IModelDataProvider`); a full `SetModel`-only control is a future item if a host needs it.
- Publishing to a feed.
- A shared drawing-surface abstraction over the two stacks (known gap).

## Approach / Notes

The controls' only app-bound couplings (verified by grep across `Controls/`) are `IModelDataProvider` (`ModelPanelControl` + `SkiaPanelControl`) and `DiagnosticsLogViewModel` (`DiagnosticsLogControl`). Everything else they resolve — `ILogService`, `IGlModel`, `ITableFactory`/`IConnectorFactory`/`IRectangleFactory`, `ISkiaTableFactory`/`ISkiaConnectorFactory` — already lives in the six libraries. This is what makes the extraction possible.

- **csproj** mirrors `Model.Graphics.WinUI` (031) plus the two extra packages the controls need: `net10.0-windows10.0.19041.0`, `<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>`, `<RootNamespace>ModelConsole</RootNamespace>`, `<Nullable>disable</Nullable>`, `<UseWinUI>true</UseWinUI>`. Packages: `Microsoft.WindowsAppSDK 2.4.0`, `Microsoft.Windows.SDK.BuildTools 10.0.28000.2526`, **`SkiaSharp.Views.WinUI 4.151.1`** (`SkiaPanelControl`/`SkiaCanvasView`), **`CommunityToolkit.Mvvm 8.4.2`** (`Ioc.Default`, the `ObservableObject` base). Project references: all six libraries. Packaging metadata as 032: `PackageId Model.Console.Controls.WinUI`, `Version 0.1.0`, `Description`, `Authors`, `<IsPackable>true</IsPackable>`, `<PackageReadmeFile>README.md</PackageReadmeFile>` + a packed README (purpose, dependency edge list, consume snippet). No `Platforms`/`RuntimeIdentifiers` needed — the sln builds with `-p:Platform=x64` and the SDK-default `AnyCPU` maps fine (as 031).

- **Files moving from the app (18):**
  - `Controls/` (13): `ModelEditorControl.xaml/.xaml.cs`, `ModelExplorerControl.xaml/.xaml.cs`, `ModelPanelControl.xaml/.xaml.cs`, `DiagnosticsLogControl.xaml/.xaml.cs`, `EntityInspectorControl.xaml/.xaml.cs`, `SkiaPanelControl.xaml/.xaml.cs`, `SkiaCanvasView.cs`.
  - `ViewModels/DiagnosticsLogViewModel.cs`, `Model/Helpers/ObservableObject.cs`, `Model/DataObjects/DataElementName.cs`, `Services/IModelDataProvider.cs`, `Services/ModelDataProvider.cs`.
  - **Retires:** `Model/ModelData/Data_Table_Entity.cs` (its `GetPersonTable`/`GetPersonNameTable` are called only by `ModelDataProvider`; drop the two interface members — nothing in the controls uses them). `DataElementName.cs` stays (its constants are referenced by `ObservableObject`).

- **Namespace plan** (library owns `ModelConsole.Controls*`; "no namespace declared by two assemblies" holds — the app stops declaring the old ones):
  - Controls keep `ModelConsole.Controls` → `MainWindow.xaml`'s `xmlns:ct="using:ModelConsole.Controls"` and the control XAMLs' `xmlns:local="using:ModelConsole.Controls"` are **unchanged**.
  - `DiagnosticsLogViewModel`: `ModelConsole.ViewModels` → `ModelConsole.Controls.ViewModels` (ripples: `DiagnosticsLogControl.xaml.cs`, `App.xaml.cs`).
  - `ObservableObject`: `ModelConsole.Model.Helpers` → `ModelConsole.Controls.Helpers` (ripple: `DiagnosticsLogViewModel`).
  - `DataElementName`: `ModelConsole.Model.DataObjects` → `ModelConsole.Controls.DataObjects` (ripple: `ObservableObject`).
  - `IModelDataProvider`/`ModelDataProvider`: `ModelConsole.Services` → `ModelConsole.Controls.Services` (ripples: `ModelPanelControl.xaml.cs`, `SkiaPanelControl.xaml.cs`, `App.xaml.cs`).
  - After the move the app declares no `ModelConsole.Services` / `ModelConsole.ViewModels` / `ModelConsole.Model.*`. All control XAML `xmlns` are `using:`-based (no assembly-qualified mappings), so **no XAML file changes** — only `.cs` usings.

- **DI — the consume contract.** `App.ConfigureServices` keeps its exact registrations, only its `using`s change: `AddSingleton<ILogService, LogService>`, `AddSingleton<IModelDataProvider, ModelDataProvider>`, the five factory singletons, `AddTransient<IGlModel, GlModel>`, `AddSingleton<DiagnosticsLogViewModel>`. A **host** project must register the same set (the controls resolve them via `Ioc.Default` at construction); doing so yields a working editor — `new ModelEditorControl()` in any host window shows the public-safety sample immediately. This contract goes in the library README.

- **XAML risk (first in this repo):** unlike 031, this extraction **moves `.xaml` files**, so the WinUI XAML compiler now runs inside the class library — the WMC1509/WMC0909 class-library flakiness 031 deliberately avoided. The controls compile today in the app (same toolchain) and the WinUI class-library template is well-trodden; keep `x:Class` fully qualified and `RootNamespace` consistent, and all referenced types resolve within libraries/packages. If WMC1509/WMC0909 surfaces, the fix is resolving the type within the library (it already is).

- **App csproj cleanup:** the moved files drop out of the app's compile automatically via SDK globbing. The now-stale explicit items must go: the four `<None Remove="Controls\*.xaml" />` entries and the four `<Page Update="Controls\*.xaml">…</Page>` entries (ModelEditorControl, DiagnosticsLogControl, EntityInspectorControl, ModelPanelControl — SkiaPanelControl.xaml is auto-globbed). Add `<ProjectReference Include="..\Model.Controls.WinUI\...">`. Optionally drop the now-unused `SkiaSharp.Views.WinUI` package reference from the app (its last direct consumer, `SkiaCanvasView`, moves out; harmless to keep).
- **sln:** add the new project (mirror `Model.Graphics.WinUI`'s entry).
- **Docs:** `docs/WORKLOG.md` entry + `CLAUDE.md` "UI control hierarchy" and "reusable library collection" sections — the controls live in `Model.Controls.WinUI`, not the app; the six-library collection becomes seven.

## Definition of Done

- [ ] `dotnet build src/Model.Controls.WinUI/Model.Controls.WinUI.csproj -c Debug -p:Platform=x64` → 0 errors.
- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings (pre-existing `NETSDK1198` allowed).
- [ ] `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` → all tests pass (unchanged from 033).
- [ ] No namespace is declared by two assemblies; the app no longer declares `ModelConsole.Services` / `ModelConsole.ViewModels` / `ModelConsole.Model.*`.
- [ ] Library ships a README + NuGet metadata; `dotnet pack src/Model.Controls.WinUI/Model.Controls.WinUI.csproj` is clean.
- [ ] `ModelWinUI.sln` contains the new project.
- [ ] `docs/WORKLOG.md` updated; `CLAUDE.md` reflects the new library.
- [ ] Manual run: XAML renderer drag / hover-highlight / inspector / pan / zoom / fit all work; Skia renderer toggle works; `File → Open Sample` unchanged (human visual pass — CLI runs on the agent's non-interactive desktop).

## Status

- **State:** Completed
- **Sprint:** sprint-2026-08-20-controls-library-and-theme
- **Completed:** 2026-08-20
