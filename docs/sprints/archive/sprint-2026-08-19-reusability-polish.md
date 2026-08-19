# Sprint 2026-08-19 — Reusability polish

> Executed copy of the sprint. Definition: `docs/backlog/archive/032-reusability-polish-namespaces-readmes-packaging.md`.

## Dates

- **Start:** 2026-08-19
- **End:** 2026-08-19

## Scope

Backlog items in this sprint:

- [x] `032` — Reusability polish: namespaces, READMEs, packaging

## Execution Log

- 2026-08-19 — Sprint defined from backlog item 032 (third of the library-reusability series `030`–`032`; executed before `031` at the user's direction). After `030` the five libraries kept the app's namespaces — `ModelConsole.Graph` spanned Model.Geometry + Model.Graph and `ModelConsole.Services` spanned the app + Model.Diagnostics + Model.Skia. This item gives each library a single clean namespace identity, resolves the shared services namespace, ships a README per library, and adds NuGet packaging metadata.
- 2026-08-19 — **Namespace renames** (the `ModelConsole.Services` tri-collision resolved by candidate "a" — per-library services namespaces — combined with candidate "b" for `ILogService`): Model.Geometry's five files `ModelConsole.Graph` → `ModelConsole.Geometry`; Model.Graph keeps `ModelConsole.Graph` + `ModelConsole.Editing`; Model.Diagnostics unified under `ModelConsole.Diagnostics` (both the `ModelConsole.Model.Diagnostics` subsystem and `ILogService`/`LogService`, which moved from `Services/` to `Model/Diagnostics/`); Model.Skia's factory contracts → `ModelConsole.Skia.Services`; the app's XAML factories → `ModelConsole.Graphics.Services` (their `031` home namespace, making that extraction a pure file move later). `IModelDataProvider`/`ModelDataProvider` stay in `ModelConsole.Services` — the app is now its only declarer. **Result: no namespace is declared by two assemblies.**
- 2026-08-19 — **Ripple:** `using` updates across the app (`App.xaml.cs`, `MainWindow`, `ModelPanelControl`, `SkiaPanelControl`, `GlContext`, `GlOrthoPath`, `DiagnosticsLogViewModel`), the XAML `xmlns:diag="using:ModelConsole.Diagnostics"` in `DiagnosticsLogControl.xaml` (the WMC0909/WMC1111 fix — a stale mapping after the namespace rename), and 10 test files (geometry-only files swapped the `ModelConsole.Graph` using for `ModelConsole.Geometry`; both-type files added it).
- 2026-08-19 — **READMEs + packaging:** one README per library (`src/Model.X/README.md` — purpose, dependency edge list, 5-line usage snippet drawn from the existing tests). All five csprojs gained `<PackageId>Model.Console.<Layer></PackageId>`, `<Version>0.1.0</Version>`, `<Description>`, `<Authors>`, `<IsPackable>true</IsPackable>`, plus `<PackageReadmeFile>README.md</PackageReadmeFile>` + a packed `README.md` so each nupkg is self-documenting (clears NuGet's missing-readme warning).
- 2026-08-19 — **Verified:** `dotnet pack` per library → all five `Model.Console.{Diagnostics,Geometry,Data,Graph,Skia}.0.1.0.nupkg` created with **0 warnings**; `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors, 0 warnings**; `dotnet test tests/ModelConsole.Tests` → **176/176 pass**. Docs updated: WORKLOG entry + pending/handoff notes, CLAUDE.md architecture + DI sections, backlog 032 completed and archived.

## Results

- **Completed:** `032`
- **Deferred:** `031` (extract the XAML `Graphics` stack into a `Model.Graphics.WinUI` class library) remains defined in `docs/backlog/` — its factory services already live in `ModelConsole.Graphics.Services`, so it is now a pure file move. The library-reusability series (030–032) is complete except for `031`.
- **Notes:** No namespace is declared by two assemblies; each library packs as `Model.Console.<Layer>` 0.1.0. Manual verification (both renderers still open samples, XAML drag/hover/inspector unchanged, Skia pan/zoom/fit/hover unchanged, `File → Open Sample` still lists the shipped samples) needs a pass — CLI launch runs on the agent's non-interactive desktop.
