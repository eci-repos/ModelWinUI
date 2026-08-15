# 006 — Extract the portable Skia stack into a ModelGraphLibrary project

## Summary

The portable Skia graphics stack (`Skia/GLibrary` + `Skia/Primitives`) currently lives inside the WinUI app project. It is the stack intended to move to the Uno/WebAssembly sibling unchanged, so separate it into its own class-library project — **ModelGraphLibrary** — that implements an interface giving access to the library (the Skia factory contract). The XAML `Graphics` stack stays in the app (it is WinUI-bound).

## Goals

- [ ] New `src/ModelGraphLibrary/ModelGraphLibrary.csproj` — plain `net10.0` class library (no Windows TFM).
- [ ] Move the portable Skia stack and the `Model.Data` metadata model (required by the Table primitive) into it.
- [ ] Move the `ISkiaTableFactory` / `SkiaTableFactory` contract into the library — the public interface that gives access to it.
- [ ] App references the library; solution builds and app runs unchanged.

## Scope

**In scope:**
- Project creation + file moves (data model, Skia GLibrary, Skia Primitives, Skia table factory contract).
- Removing the one dead WinUI `using` (`RectangleHalf.cs`) so the library is WinUI-free.
- App csproj `ProjectReference` + solution registration.
- Build + run verification.

**Out of scope:**
- The XAML `Graphics` stack — stays in the app (WinUI-bound).
- A shared drawing-surface abstraction over the two stacks (deferred from backlog 002).
- Namespace renames — code keeps `ModelConsole.*` / `Model.Data` namespaces; the project name is the assembly identity. Reorganization can be a follow-up.
- Wiring `SkiaPanelControl` into `MainWindow`.

## Approach / Notes

- **TFM:** `net10.0` (plain) — the moved code uses only `SkiaSharp` + `System.*`; no Windows APIs. Keep the Skia stack free of WinUI dependencies.
- **Package:** `SkiaSharp` 4.151.1 (core — not `SkiaSharp.Views.WinUI`, which stays in the app).
- **Files moving to the library (16 + 2 factories):**
  - `Model/Data/*.cs` (5) — `CatalogInfo`, `TableInfo`, `ColumnInfo`, `ColumnList`, `ConstraintInfo` (namespace `Model.Data`).
  - `Skia/GLibrary/*.cs` (9) — `GlFrame`, `GlText`, `GlModel`, `GlObject`, `GlBoxInfo`, `GlMatrix`, `GlObjectGeometryInfo`, `GlObjectInfo`, `GlPalette`.
  - `Skia/Primitives/*.cs` (2) — `Table`, `RectangleHalf`.
  - `Services/ISkiaTableFactory.cs`, `Services/SkiaTableFactory.cs` — the library's factory contract.
- **App project:** remove the moved files, add `<ProjectReference>` to ModelGraphLibrary, keep `SkiaPanelControl` (WinUI view) and DI registration in `App.ConfigureServices`.
- **RootNamespace:** `ModelConsole` so moved namespaces compile unchanged.
- `Model.DataObjects` and `Model/ModelData` sample fixtures stay in the app (app-level domain); `Data_Table_Entity` keeps working through the library's `Model.Data` namespace.

## Definition of Done

- [ ] `dotnet build src/ModelGraphLibrary/ModelGraphLibrary.csproj` → 0 errors.
- [ ] `dotnet build ModelWinUI.sln -c Debug -p:Platform=x64` → 0 errors (pre-existing `NETSDK1198` warning allowed).
- [ ] App launches unpackaged; window "EDAM Studio" responding; sample drawing runs.
- [ ] `docs/WORKLOG.md` updated; sprint promoted; functionality map + `CLAUDE.md` reflect the new project.

## Status

- **State:** Completed / Archived
- **Sprint:** `docs/sprints/archive/sprint-2026-08-15-modelgraphlibrary.md`
- **Completed:** 2026-08-15
