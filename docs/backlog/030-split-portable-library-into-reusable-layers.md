# 030 — Split ModelGraphLibrary into layered reusable libraries

## Summary

`ModelGraphLibrary` bundles four independent concerns in one assembly: a pure geometry/routing core, the relational metadata model (+ interpretation + validation + fixtures), the ERD domain logic (+ editing), and the Skia render stack. Reusing any one of them forces a consumer to drag in the whole ERD model. Split it into **five independently-reusable `net10.0` libraries** — `Model.Diagnostics`, `Model.Geometry`, `Model.Data`, `Model.Graph`, `Model.Skia` — so a project can reference exactly the layer it needs. Purely structural: files move with namespaces intact; behavior, the 0/0 build, and the 176 tests are unchanged. The XAML `Graphics` stack is handled by backlog 031; packaging/namespace polish by 032.

## Goals

- [ ] Five new `src/Model.*` projects replace `ModelGraphLibrary`; the dependency graph is acyclic (edges point only downward).
- [ ] Each library compiles standalone with only its declared references.
- [ ] App and test project reference the collection; `ModelWinUI.sln` builds 0 errors / 0 warnings and all tests pass.
- [ ] `src/ModelGraphLibrary/` is deleted; nothing references it.

## Scope

**In scope:**
- Project creation, file moves, project-reference wiring, solution registration.
- App csproj reference swap + Samples/Schemas content-link retarget.
- Test project rename `ModelGraphLibrary.Tests` → `ModelConsole.Tests` (matches its `ModelConsole.Tests` RootNamespace).
- Docs: `WORKLOG` entry + `CLAUDE.md` architecture section.

**Out of scope:**
- Namespace renames — every file keeps its current namespace (the generic geometry types stay in `ModelConsole.Graph`, now spanning two assemblies). Namespace identity is backlog 032.
- NuGet packaging / per-library READMEs (backlog 032).
- The XAML `Graphics` stack — it stays in the app for this item (backlog 031 extracts it).
- Any behavior change.

## Approach / Notes

- Each new csproj mirrors the current library: `<RootNamespace>ModelConsole</RootNamespace>`, `<Nullable>disable</Nullable>`, plain `net10.0`.
- **Model.Data** (`src/Model.Data/`) — `net10.0` + `JsonSchema.Net 9.4.0` (the only JsonSchema.Net consumer is `ModelSchemaValidator`). Files (18): `Model/Data/*` (CatalogInfo, ColumnList, ColumnInfo, ConstraintInfo, Enumeration, ModelFile, Provenance, TableInfo, TableKind), `Model/Interpretation/*` (BuiltInProfiles, MappingSpec, ModelInterpretation, SchemaInterpreter), `Model/Validation/ModelSchemaValidator.cs`, `ModelData/*` (HealthcareSchema, LibrarySchema, PublicSafetySchema, SampleModels). Also move `Samples/*.json` (3) + `Schemas/*.json` (2) into this project as content (`CopyToOutputDirectory="PreserveNewest"`) — the app and tests retarget their content links to `..\Model.Data\`.
- **Model.Geometry** (`src/Model.Geometry/`) — `net10.0`, no packages. Files (5): `Graph/Geometry.cs`, `Graph/OrthogonalRouter.cs`, `Graph/ConnectorAnchors.cs`, `Graph/SequentialRouter.cs`, `Graph/RouteHitTest.cs`.
- **Model.Graph** (`src/Model.Graph/`) — `net10.0`, refs Model.Data + Model.Geometry. Files (7): `Graph/FkRelation.cs`, `Graph/FkEdgeExtractor.cs`, `Graph/TableLayoutEngine.cs`, `Graph/GraphNode.cs`, `Graph/HoverSummary.cs`, `Graph/ReadoutFormatter.cs`, `Editing/ModelEdits.cs`.
- **Model.Skia** (`src/Model.Skia/`) — `net10.0` + `SkiaSharp 4.151.1` (core, not `SkiaSharp.Views.WinUI`), refs Model.Graph, Model.Data, Model.Geometry. Files (17): `Skia/GLibrary/*` (GlBoxInfo, GlFrame, GlMatrix, GlModel, GlObject, GlObjectGeometryInfo, GlObjectInfo, GlPalette, GlText), `Skia/Primitives/*` (Connector, ErdComposer, RectangleHalf, Table), `Services/*` (ISkiaConnectorFactory, ISkiaTableFactory, SkiaConnectorFactory, SkiaTableFactory).
- **Model.Diagnostics** (`src/Model.Diagnostics/`) — `net10.0`, no packages. Files (15) move **from the app**: `Model/Diagnostics/*` (DiagnosticsInfo, EventCode, IDiagnosticWritter, IResultsLog, Log, LogFormat, LogMessageEvent, LogSettings, LogStatus, MessageLogEntry, ResultLog, SeverityLevel, Verbosity) plus `Services/ILogService.cs` + `Services/LogService.cs`. `ILogService`/`LogService` keep namespace `ModelConsole.Services` — moving them into a library is what later unblocks extracting `GlContext` (backlog 031), whose constructor takes `ILogService`. The app's DI registration `AddSingleton<ILogService, LogService>()` is unchanged.
- **Namespaces span assemblies after this split** (`ModelConsole.Graph` across Model.Geometry + Model.Graph; `ModelConsole.Services` across app + Model.Skia + Model.Diagnostics) — the same cross-assembly pattern `ModelConsole.Services` already uses today; disjoint type names keep it unambiguous. This is deliberate and resolved by 032.
- **App csproj** (`ModelWinUI.csproj`): replace the single `ProjectReference` to ModelGraphLibrary with references to the five libraries; retarget the two `Content Include="..\ModelGraphLibrary\Samples\*.json"` / `Schemas\*.json` links to `..\Model.Data\`. Remove the `Model\Diagnostics\**` and `Services\ILogService.cs`/`LogService.cs` compile items.
- **Test project**: rename folder + csproj to `ModelConsole.Tests`; swap the ModelGraphLibrary reference for Model.Data, Model.Geometry, Model.Graph, Model.Skia; retarget Samples/Schemas content links to `..\..\src\Model.Data\`. The 23 test files exercise only these four layers (no diagnostics tests exist).
- **Solution**: `dotnet sln ModelWinUI.sln remove` ModelGraphLibrary; `add` the five libraries (src folder) and the renamed test project (tests folder).
- Delete `src/ModelGraphLibrary/` — every source file and JSON artifact is accounted for in the inventories above.

## Definition of Done

- [ ] Each new library builds standalone: `dotnet build src/Model.{Diagnostics,Geometry,Data,Graph,Skia}/{...}.csproj` → 0 errors.
- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings (pre-existing `NETSDK1198` warning allowed).
- [ ] `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` → all 176 tests pass.
- [ ] App launches unpackaged; both renderers and `File → Open Sample` unchanged.
- [ ] `docs/WORKLOG.md` updated; `CLAUDE.md` architecture section describes the five-library collection instead of "ModelGraphLibrary".

## Status

- **State:** Planned
- **Sprint:** (not scheduled)
- **Completed:** —
