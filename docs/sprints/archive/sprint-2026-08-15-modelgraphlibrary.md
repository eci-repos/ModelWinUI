# Sprint 2026-08-15 — ModelGraphLibrary

> Executed copy of the sprint. Backlog item: `docs/backlog/archive/006-model-graph-library.md`.

## Dates

- **Start:** 2026-08-15
- **End:** 2026-08-15

## Scope

- [x] `006` — Extract the portable Skia stack into a ModelGraphLibrary project

## Execution Log

- 2026-08-15 — Sprint defined from backlog item `006`. Scope confirmed: split only the portable Skia stack + data model; XAML `Graphics` stack stays in the app.
- 2026-08-15 — Created `src/ModelGraphLibrary/ModelGraphLibrary.csproj` — plain `net10.0` class library, `RootNamespace=ModelConsole`, `SkiaSharp` 4.151.1 (core, not `Views.WinUI`).
- 2026-08-15 — `git mv`'d 18 files from the app: `Model/Data` (5, namespace `Model.Data`), `Skia/GLibrary` (9), `Skia/Primitives` (2), `Services/ISkiaTableFactory` + `Services/SkiaTableFactory` (the library's public factory contract).
- 2026-08-15 — Removed dead WinUI usings from `RectangleHalf.cs` — the only WinUI reference in the whole Skia stack — so the library is WinUI-free.
- 2026-08-15 — Wired `ModelWinUI.csproj` with a `ProjectReference` to ModelGraphLibrary; `dotnet sln add` registered it in `ModelWinUI.sln`.
- 2026-08-15 — Build verified: library alone → **0 errors, 0 warnings**; full solution `-c Debug -p:Platform=x64` → **0 errors, 0 warnings**.
- 2026-08-15 — App run verified: `ModelWinUI.exe` launches unpackaged, window "EDAM Studio" responding, sample drawing runs (Skia stack resolved through the referenced library). (Screenshot of rendered output declined.)

## Results

- **Completed:** `006`
- **Deferred:** none
- **Notes:**
  - Namespaces kept unchanged (`ModelConsole.*`, `Model.Data`) — the project name is the assembly identity; namespace reorganization is a possible follow-up.
  - `SkiaPanelControl` remains compiled-but-unwired in the app; DI registration of `ISkiaTableFactory` unchanged (`ModelConsole.Services`).
  - Pre-existing `NETSDK1198` warning (missing `.pubxml`) did not reappear on this build.
