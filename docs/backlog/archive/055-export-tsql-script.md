# 055 — Export T-SQL Script (with Annotated Comments)

## Summary

Add a **File → Export T-SQL…** command that generates a runnable SQL Server DDL script (schemas, tables, columns, PK, FK) from the live model, with an optional **annotated** mode that emits `--` comment cards carrying the model's design metadata (kind, tags, descriptions, cardinality, roles) that DDL cannot express. The emitter is a pure, portable, deterministic component exposed through an interface so it is replaceable via DI, keeping the "derived, never canonical" guarantee (the model and JSON are untouched).

## Goals

- [ ] Add a pure, portable T-SQL emitter behind an interface (`ITsqlEmitter` / `TsqlEmitter`) in `Model.Graph`, DI-registered, following the existing replaceable-component pattern.
- [ ] Emit `CREATE SCHEMA`, `CREATE TABLE` (columns with type/size/nullability/identity, inline PK), and `ALTER TABLE ... ADD CONSTRAINT ... FOREIGN KEY` for FKs.
- [ ] Support an **annotated** mode that emits `-- KEY : value` comment cards before each table and FK reference, carrying the model's metadata.
- [ ] Wire a **File → Export T-SQL…** menu command in the app that writes the script and reports success/failure through diagnostics.
- [ ] Keep the emitter deterministic (same model → same script) and unit-tested.

## Scope

In scope:

- A `TsqlEmitter` (and `ITsqlEmitter` contract) in `Model.Graph` — pure string generation, no WinUI/Skia dependency, portable to the WASM sibling.
- DDL for the containerized model: `CREATE SCHEMA` per schema, `CREATE TABLE` per table, inline `PRIMARY KEY`, and FK constraints added in a post-pass (`ALTER TABLE ... ADD CONSTRAINT`) so dependency ordering and cycles are handled robustly.
- FK reference resolution: `ReferencedColumnName` null → the parent's PK (same rule `FkEdgeExtractor` uses).
- Type mapping: known model types map to T-SQL (`VARCHAR` → `VARCHAR`, `DATETIMEOFFSET` → `DATETIMEOFFSET`, etc.); unknown types pass through verbatim and surface a diagnostic — never a crash.
- Annotated mode: a per-table header card (KIND, TAGS, DESC, FK count) and a per-FK annotation block (cardinality, child/parent roles, description), using a strictly regular `-- KEY : value` grammar so the output is greppable and re-parseable.
- A clean/bare DDL mode as the alternative to annotated.
- App wiring: a `File → Export T-SQL…` command (mirroring `Export PlantUML…`), a file picker, and diagnostics for success/failure. Canceled pickers do not log errors or mutate state.

Out of scope:

- DML (INSERT/UPDATE/DELETE) — the model carries no data.
- Views, stored procedures, triggers, defaults, computed columns, collation, indexes beyond PK/FK.
- Expressing cardinality/roles as DDL — they are view-level semantics (Crow's Foot 053, UML 040) and appear only as annotations.
- A round-trip importer that reads the script back into the model (the regular grammar keeps this *possible* later, but it is not built here).
- Persisting workspace state, dirty tracking, or a full Save command (deferred by 054).

## Approach / Notes

- **Separation of concerns:** the emitter is a pure, stateless component in `Model.Graph` (namespace `ModelConsole.Graph`), exactly parallel to the existing `UmlPlantEmitter` (backlog 040) but exposed through an interface. The app only calls `ITsqlEmitter`; it never depends on the concrete type.
- **Replaceable via interface:** `ITsqlEmitter` + `TsqlEmitter` live together in `Model.Graph` (portable, `net10.0`), registered as a **singleton** in `App.ConfigureServices()` (stateless — a singleton is safe and matches the factory/service convention). A host can substitute its own implementation by registering a different `ITsqlEmitter`. This mirrors how `ISkiaTableFactory`/`SkiaTableFactory` and the XAML `ITableFactory`/`IBoxFactory` are wired.
- **Proposed contract:**
  ```csharp
  public interface ITsqlEmitter
  {
      string EmitCreateScript(IReadOnlyList<TableInfo> tables, TsqlExportOptions options = null);
  }
  public class TsqlExportOptions
  {
      public bool Annotated { get; set; } = true;   // annotated by default; false = bare DDL
  }
  ```
- **Determinism:** stable ordering of schemas, tables (model order), columns (ordinal), and constraints; identical input → identical output, byte-for-byte.
- **Annotated grammar (strict `KEY : value`):** table header card before each `CREATE TABLE` (`TABLE`, `KIND`, `TAGS`, `DESC`, `FKs`); FK block before each `ALTER TABLE` (`FK`, `cardinality`, `child role`, `parent role`). Column-level annotations (`DESC`, `enum`, provenance) are inline `--` comments on the column line. `TableKindClassifier` supplies `KIND` (entity / reference-code).
- **Design decisions (adjustable):** annotated mode is the default (the self-documenting value is the point of this item); bare DDL is the toggle. The grammar is kept strictly regular so the output is re-parseable later.
- **App wiring:** `MainWindow` adds the menu command, opens a `FileSavePicker` (initialized for the unpackaged app via `WinRT.Interop.WindowNative.GetWindowHandle` + `InitializeWithWindow.Initialize`, like the existing open picker), writes the script, and logs success/failure through the diagnostics log. The command uses the current model's tables (the same set both renderers draw).
- **Tests:** `TsqlEmitterTests` in `tests/ModelConsole.Tests` — schema/table/column emission, PK/FK emission, FK parent-PK default resolution, type mapping (known + unknown pass-through), annotated vs bare output, determinism, and a full-script smoke test over the PublicSafety and Enterprise fixtures (multi-schema + cross-schema FKs).

## Definition of Done

- [ ] `ITsqlEmitter`/`TsqlEmitter` exist in `Model.Graph`, are DI-registered as a singleton, and the app consumes only the interface.
- [ ] **File → Export T-SQL…** writes a script that recreates the model's schemas, tables, columns, PKs, and FKs (including cross-schema FKs and null-`ReferencedColumnName` → parent-PK resolution).
- [ ] Annotated mode emits the table header cards and FK annotation blocks with the model's metadata; bare mode emits clean DDL.
- [ ] Unknown column types pass through verbatim with a diagnostic, never a crash.
- [ ] Canceled file pickers do not log errors or mutate state; export failures are reported plainly through diagnostics.
- [ ] `TsqlEmitterTests` cover emission, FK resolution, type mapping, annotated/bare, and determinism; the full suite passes.
- [ ] `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` passes.
- [ ] `dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64` passes or any failure is documented faithfully.

## Status

- **State:** Planned
- **Sprint:** -
- **Completed:** -
