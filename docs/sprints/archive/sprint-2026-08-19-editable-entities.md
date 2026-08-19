# Sprint 2026-08-19 — Editable entities

> Executed copy of the sprint. Definition: `docs/backlog/archive/029-entity-editing.md`.

## Dates

- **Start:** 2026-08-19
- **End:** 2026-08-19

## Scope

Backlog items in this sprint:

- [x] `029` — Editable entities (part 3 of the object-oriented drawing series)

## Execution Log

- 2026-08-19 — Sprint defined from backlog item 029 (object-oriented drawing series, part 3). The 028 node surface defined the edit **verbs**; 029 **wires them to real model operations**. Today the inspector edits only a column's data type and deletes connectors; this item extends it to the full edit surface: rename entities/columns, add/remove entities and columns, add/delete/edit FKs (target, cardinality, roles), and edit description + key marking. Every edit **mutates the live canonical model** → re-render (partial re-route where localized) → resolution issues surface via `FkEdgeExtractor` (R8), never a crash or a dangling connector. User decisions confirmed: **cascade renames** (renaming a table/column updates every FK's `ReferencedTableName`/`ReferencedColumnName` to follow) and **metadata bag stays read-only** (the inspector edits Description + key marking; the metadata section remains a read-only readout — it's `[JsonIgnore]`, session-only anyway).
- 2026-08-19 — Pure edit operations `ModelConsole.Editing.ModelEdits` (ModelGraphLibrary, unit-tested without UI): `RenameTable`/`RenameColumn` (cascade across referencing FKs), `AddColumn`/`RemoveColumn`, `SetColumnType`, `SetKey` (add/remove PK constraint + recompute `IsKey`), `SetDescription` (table + column), `AddForeignKey`/`EditForeignKeyTarget`/`SetCardinality`/`SetRoles`/`RemoveForeignKey` (recompute `IsForeignKey`), `RemoveTable`. All ops preserve the observed invariants — column `IsKey`/`IsForeignKey` are recomputed from `Constraints` after add/remove, never hand-set.
- 2026-08-19 — `NodeVerbs` gains the two verbs 029 actually needs: `CanAddForeignKey` (Element preset — a column can become an FK) and `CanDelete` (Entity preset — remove an entity). `GraphNodeTests` verb assertions updated.
- 2026-08-19 — The inspector (`EntityInspectorControl`) becomes stateful (`_currentTable`, `_currentEdge`, `_tables`, `_enumerations`) and builds its edit surface **gated by the 028 verbs**: `ShowTable`/`ShowConnector` consult `GraphNodes.Entity(table).Verbs` / `GraphNodes.Element(table, column).Verbs` / `GraphNodes.Dependency(edge).Verbs` and only build editable controls the verbs allow. New events: `EntityRenamed`, `ColumnRenamed`, `EntityRemoved`, `EntityAdded`, `StructureChanged`. Renames commit in place (no rebuild — avoids LostFocus/click races); structural edits re-show the current entity. Connector target/cardinality/roles editors mutate the live `ConstraintInfo` and update the readout lines in place.
- 2026-08-19 — Host edit operations (`ModelPanelControl`): `RenameTable` (cascade + re-key `_layout`), `RenameColumn` (cascade + `ReMeasureTable` + partial re-route), `RemoveTable`, `AddTable` (re-layout), `StructureChanged` (re-measure + `Render(onlyTable:)` + `ModelChanged`). All follow the `DeleteConnector` mutate-then-render pattern; structural edits raise `ModelChanged` so the explorer refreshes. `ModelEditorControl` wires the new inspector events to the panel methods and passes `ModelPanel.Tables` to `ShowTable`/`ShowConnector`.
- 2026-08-19 — Added 17 tests (`ModelEditsTests`): rename-table cascade (referencing FKs follow; `FkEdgeExtractor.Extract` yields clean edges), rename-column cascade, add/remove column, set type+size, set key (add/clear), add FK (target/cardinality/roles + flags), add FK blank parent column → null, remove FK (recompute flag), edit target, edit target blank column → null, set cardinality/roles, set description, and the DoD pipeline tests `RemoveTableThenExtractReportsDanglingFks` + `RemoveColumnReferencedByFkProducesIssue` (mutate → `FkEdgeExtractor.Extract` → issue, no crash). Library + app build **0 errors, 0 warnings**; full suite **171/171 pass** (was 154).

## Results

- **Completed:** `029`
- **Deferred:** none — the object-oriented drawing series (027–029) is complete.
- **Notes:** The metadata bag stays read-only (session-only `[JsonIgnore]`); provenance stays read-only (source-origin). The Skia renderer stays view-only — editing is on the XAML path. Visual verification (rename with inbound FKs following, add/remove columns, PK toggle, add FK, connector target/cardinality/roles, delete, dangling-FK issue line, drag/pan/wheel-zoom/hover/click unchanged) needs a manual pass — CLI launch runs on the agent's non-interactive desktop.
