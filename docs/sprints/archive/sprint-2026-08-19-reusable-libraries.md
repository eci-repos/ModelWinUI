# Sprint 2026-08-19 — Reusable libraries

> Executed copy of the sprint. Definition: `docs/backlog/archive/030-split-portable-library-into-reusable-layers.md`.

## Dates

- **Start:** 2026-08-19
- **End:** 2026-08-19

## Scope

Backlog items in this sprint:

- [x] `030` — Split ModelGraphLibrary into layered reusable libraries

## Execution Log

- 2026-08-19 — Sprint defined from backlog item 030 (first of the library-reusability series `030`–`032`). The user's goal for the graphics code is **reusability across their own projects**, and the single "portable library" (`ModelGraphLibrary`, plain `net10.0`, SkiaSharp + JsonSchema.Net) bundled five independent concerns in one assembly — reusing any one dragged the whole ERD model in. This item splits it into five layered, independently-reusable `net10.0` libraries under `src/`: `Model.Diagnostics` (logging subsystem + `ILogService`/`LogService`), `Model.Geometry` (2D + orthogonal routing + hit-test), `Model.Data` (relational metadata model + interpretation + validation + fixtures + `Samples/`/`Schemas/` JSON), `Model.Graph` (ERD logic + editing), `Model.Skia` (SkiaSharp render stack + factory contracts). Purely structural — no behavior change.
- 2026-08-19 — Created the five csprojs mirroring the old library's settings (`RootNamespace=ModelConsole`, `Nullable=disable`, plain `net10.0`; Model.Data adds `JsonSchema.Net 9.4.0`, Model.Skia adds `SkiaSharp 4.151.1`). Moved every file with its namespace intact. `ILogService`/`LogService` moved out of the app into `Model.Diagnostics` (namespace `ModelConsole.Services` preserved) — the prerequisite for extracting `GlContext` in backlog `031`. App csproj swapped the single ProjectReference for the five and retargeted the Samples/Schemas content links to `..\Model.Data\`; app DI registrations untouched.
- 2026-08-19 — Test project renamed `ModelGraphLibrary.Tests` → `ModelConsole.Tests` (matches its RootNamespace); references now Model.Data, Model.Geometry, Model.Graph, Model.Skia; content links retargeted to `..\..\src\Model.Data\`. Solution rewired (`dotnet sln` remove ModelGraphLibrary, add the five under `src` + the renamed test under `tests`); `src/ModelGraphLibrary/` deleted with every file accounted for. Cleaned stale `obj`/`bin` before the build to dodge the flaky WinUI XAML compiler.
- 2026-08-19 — Verified: each library builds standalone **0 errors, 0 warnings**; `dotnet build ModelWinUI.sln -p:Platform=x64` → **0/0**; `dotnet test tests/ModelConsole.Tests` → **176/176 pass**. `ModelConsole.Services` now spans the app + Model.Diagnostics + Model.Skia; `ModelConsole.Graph` spans Model.Geometry + Model.Graph — the cross-assembly namespace pattern is deliberate and deferred to `032`. Docs updated: WORKLOG entry + handoff notes, CLAUDE.md architecture section rewritten around the five-library collection.

## Results

- **Completed:** `030`
- **Deferred:** `031` (extract the XAML `Graphics` stack into a `Model.Graphics.WinUI` class library) and `032` (namespace identity + READMEs + NuGet packaging) are defined in `docs/backlog/` and queued as the next sprints of the series.
- **Notes:** Namespaces are intentionally preserved across the split (`ModelConsole.Graph`, `ModelConsole.Services` span assemblies) — that polish is `032`'s job. Manual verification (both renderers still open samples, XAML drag/hover/inspector unchanged, Skia pan/zoom/fit/hover unchanged, `File → Open Sample` still lists the shipped samples) needs a pass — CLI launch runs on the agent's non-interactive desktop.
