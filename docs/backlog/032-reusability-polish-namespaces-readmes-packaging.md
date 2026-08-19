# 032 — Reusability polish: namespaces, READMEs, packaging (deferred)

## Summary

Once backlogs 030 and 031 settle the project shape, make each library read as a **first-class reusable artifact**: rename the generic geometry namespace out of `ModelConsole.Graph`, resolve the `ModelConsole.Services` namespace that now spans four assemblies, give each library a README with the dependency graph and a consume-snippet, and add NuGet package metadata. Deliberately parked until the structural split is proven — it is the "make it pretty for external consumers" pass over the shape that 030/031 establish.

## Goals

- [ ] Clean namespace identity per library (generic geometry no longer under a name that implies the ERD).
- [ ] Each library ships with a README: purpose, dependency graph, one "reference this library" snippet.
- [ ] NuGet package metadata (`PackageId`, `Version`, `Description`, `Authors`) so each library is publishable.
- [ ] The shared `ModelConsole.Services` namespace is no longer ambiguous across assemblies.

## Scope

**In scope:**
- Namespace renames and the mechanical `using` updates they ripple through the app, tests, and sibling libraries.
- Per-library READMEs + csproj packaging metadata.
- `dotnet pack` verification (pack locally, no publish).

**Out of scope:**
- Publishing to a feed.
- Behavioral changes.

## Approach / Notes

- **Namespace candidates:**
  - `Point2`/`Rect2`/`OrthogonalRouter`/`ConnectorAnchors`/`SequentialRouter`/`RouteHitTest` (Model.Geometry) → `ModelConsole.Geometry`.
  - ERD logic (Model.Graph) keeps `ModelConsole.Graph`; `ModelEdits` keeps `ModelConsole.Editing`.
  - `ModelConsole.Services` spans app, Model.Skia, Model.Graphics.WinUI, and Model.Diagnostics after 030/031. Candidates: rename each library's services namespace to the library (`ModelConsole.Skia.Services`, `ModelConsole.Graphics.Services`, ...), or give `ILogService` its natural home in `ModelConsole.Model.Diagnostics`. Decide once the split is in place.
- **Ripple discipline:** namespaces are referenced by `using` in the app (`App.xaml.cs`, `MainWindow`, `Controls/*`), tests, and across the libraries — a rename must update every `using` and every fully-qualified mention in one commit so the build stays green.
- **README template:** one paragraph (what the library provides), the dependency edge list from 030/031, a 5-line usage example from the existing tests (e.g. `OrthogonalRouterTests`, `SkiaConnectorTests`, `ModelEditsTests`).
- **Packaging metadata:** `AssemblyName`, `<Version>`, `<Description>`, `<PackageId>Model.Console.<Layer></PackageId>` (or similar); `<IsPackable>true</IsPackable>`. Verify with `dotnet pack` per library — no feed push.

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; all tests pass.
- [ ] `dotnet pack` succeeds for every library.
- [ ] Each library has a README; no namespace is declared by two assemblies.
- [ ] `docs/WORKLOG.md` + `CLAUDE.md` updated.

## Status

- **State:** Planned (parked until 030 and 031 complete)
- **Sprint:** (not scheduled)
- **Completed:** —
