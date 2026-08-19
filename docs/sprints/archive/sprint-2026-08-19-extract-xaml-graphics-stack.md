# Sprint 2026-08-19 — Extract the XAML Graphics stack

> Executed copy of the sprint. Definition: `docs/backlog/archive/031-extract-xaml-graphics-stack-into-winui-library.md`.

## Dates

- **Start:** 2026-08-19
- **End:** 2026-08-19

## Scope

Backlog items in this sprint:

- [x] `031` — Extract the XAML Graphics stack into a reusable WinUI class library

## Execution Log

- 2026-08-19 — Sprint defined from backlog item 031 (final item of the library-reusability series `030`–`032`). The XAML `Gl*` drawing stack (`Graphics/GLibrary` + `Graphics/Primitives`) and its six factory services lived inside the app project, so no other project could reference them. Extracted into `Model.Graphics.WinUI` — a `net10.0-windows10.0.19041.0` class library (WindowsAppSDK 2.4.0 + BuildTools, `UseWinUI`) referencing the portable layers from 030. Purely structural.
- 2026-08-19 — Moved 33 files: `Graphics/GLibrary/*` (20 + 5 in `GlOrtho/`), `Graphics/Primitives/Table.cs` + `TableRowPanel.cs`, and the six XAML factory services (`ITableFactory`/`TableFactory`, `IConnectorFactory`/`ConnectorFactory`, `IRectangleFactory`/`RectangleFactory`). Removed the dead usings that were compile blockers (`using Model.Data;` in `GlColor.cs`/`GlRectangle.cs`, stale `using Windows.UI.Xaml;` in `GlTextBox.cs`, unused `using Windows.ApplicationModel;` in `GlPointerEvent.cs`). A `using`-tally across all 33 files confirmed no other app-owned namespace references.
- 2026-08-19 — Wired it up: csproj references Model.Data, Model.Graph, Model.Geometry, Model.Diagnostics (no Skia, no cycle); app csproj gained a ProjectReference and the moved files dropped out of the app's compile automatically (SDK default globbing); added to `ModelWinUI.sln`. `App.ConfigureServices` byte-for-byte unchanged — the factory types resolve from the library under the same namespaces.
- 2026-08-19 — Packaging (032 consistency): README + `PackageId Model.Console.Graphics.WinUI`, `Version 0.1.0`. Verified: library standalone `-p:Platform=x64` → **0 errors, 0 warnings**; `dotnet build ModelWinUI.sln -p:Platform=x64` → **0/0**; `dotnet test tests/ModelConsole.Tests` → **176/176 pass**; `dotnet pack` → clean `Model.Console.Graphics.WinUI.0.1.0.nupkg`. No `.xaml` files moved, so the flaky WinUI XAML compiler has no new surface.

## Results

- **Completed:** `031`
- **Deferred:** — (the library-reusability series `030`–`032` is complete; the backlog is empty)
- **Notes:** Manual verification of the XAML renderer (drag / hover-highlight / inspector / pan / zoom / fit) and `File → Open Sample` needs a human pass — CLI launch runs on the agent's non-interactive desktop. All six libraries now pack as `Model.Console.<Layer>` 0.1.0 with READMEs embedded.
