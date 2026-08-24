# Sprint - Current

## Dates

- **Start:** 2026-08-23
- **End:** 2026-08-23

## Scope

Backlog items in this sprint:

- `055-export-tsql-script.md` — Export T-SQL Script (with Annotated Comments).

## Execution Log

- **Implementation:** added the replaceable `ITsqlEmitter`/`TsqlEmitter` in `Model.Graph` — a pure, deterministic T-SQL DDL exporter (`CREATE SCHEMA`, `CREATE TABLE` with type/size/nullability/identity + inline/composite PK, and `ALTER TABLE ... ADD CONSTRAINT ... FOREIGN KEY` for every resolved FK). FK references resolve null `ReferencedColumnName` to the parent PK via `FkEdgeExtractor`. Registered as a singleton in `App.ConfigureServices()` (hosts substitute their own `ITsqlEmitter`). **Annotated mode** (default) emits strictly regular `-- KEY : value` comment cards before each table (TABLE/KIND/TAGS/DESC/FKs) and each FK (cardinality, child/parent roles); bare mode emits clean DDL. Unknown column types pass through verbatim with a diagnostic; unresolved FKs are reported and skipped. Added `File → Export T-SQL…` writing the script through the interface and logging resolution/type-mapping issues.
- **Tests:** `TsqlEmitterTests` (10) cover schema/table/column/PK/FK emission, parent-PK resolution, composite PK, identity, unknown-type pass-through, annotated vs bare, determinism, empty model, and a 50-table/74-FK smoke test (one `ALTER TABLE` per resolved FK).

## Results

- **Completed:** 055 — Export T-SQL Script (with Annotated Comments).
- **Deferred:** DML, views/procs/triggers, defaults/computed columns, cardinality-as-DDL, a round-trip importer, and 054's deferred workspace-state/Save concerns (all explicitly out of scope in 055).
- **Notes:** `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` → **338/338 pass** (was 328; +10 new tests). `dotnet build ModelWinUI.sln -c Debug -p:Platform=x64` → **0 errors / 0 warnings**. Manual pass (open a sample, Export T-SQL, open the `.sql` in SSMS/a viewer) needs a human run.
