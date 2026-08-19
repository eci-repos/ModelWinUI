# Sprint 2026-08-19 — Graph node objects

> Executed copy of the sprint. Definition: `docs/backlog/archive/028-graph-node-objects.md`.

## Dates

- **Start:** 2026-08-19
- **End:** 2026-08-19

## Scope

Backlog items in this sprint:

- [x] `028` — Graph node objects (part 2 of the object-oriented drawing series)

## Execution Log

- 2026-08-19 — Sprint defined from backlog item 028 (object-oriented drawing series, part 2). After 027 gave the drawing a hover readout, every drawable becomes a first-class **node object** — a single surface (kind, identity, the live canonical object it renders, a hover summary, edit verbs) that the input layer (hover, click, drag, edit) and the renderer agree on. The host's ad-hoc type-switching on `GlObject` subclasses (the "second source of truth") is removed.
- 2026-08-19 — Portable node surface `ModelConsole.Graph.GraphNode` (ModelGraphLibrary, pure net10.0, beside `HoverSummary`): `GraphNodeKind` (Entity/Element/Dependency/Group — mapping 1:1 to Table/Column/FK/Schema), `IGraphNode` (Kind, Name, Model, Summary(), Verbs), immutable `NodeVerbs` descriptor (rename/add/remove column/edit type/key/description/metadata/target/cardinality/roles/delete) with per-kind presets, four concrete sealed nodes (`EntityNode`, `ElementNode`, `DependencyNode`, `GroupNode`) each holding a strongly-typed live reference, and the `GraphNodes` factory (null for null inputs). Node summaries **delegate to `HoverSummary`** so hover, inspector, and node never drift (the 022 discipline).
- 2026-08-19 — `HoverSummary` gains two portable providers: `ForColumn(column, tableName)` → `table.column` header, `Type(Size)` (bare `Type` when `Size <= 0`, omitted when Type is null), a tags line (`PK, FK, enum:Name` — only the tags present), and the description; `ForGroup(schemaName, tables)` → schema header + `N tables`. Both empty for null input.
- 2026-08-19 — Recognition is `GlObject.Node` (virtual, null by default): `Table` overrides it with a cached `EntityNode` over its live `TableInfo`; `GlOrthoPath` overrides it with a lazily-built `DependencyNode` over the `FkRelation` in `Data` (null in grip mode — no model payload). `GlContext` keeps raising `GlObject` (drag/geometry need the shape-level object); the host consumes `obj.Node`, never type-checks raw objects.
- 2026-08-19 — `ModelPanelControl` host refactor: `OnShapeClicked` raises `EntitySelected` with `node.Model` (payload stays `TableInfo`/`FkRelation`, so the inspector contract is unchanged); `OnHoverChanged` tracks `_hoverNode` (IGraphNode) and compares identity via `ReferenceEquals(node.Model, _hoverNode?.Model)`; `ShowHover` renders `_hoverNode.Summary()`. `ResolveHoverPayload` deleted; `_hoverTarget` renamed `_hoverNode`. The drag handler (`OnShapeReleased`) keeps its shape-level `is Table` check — it needs the table's geometry, not its model.
- 2026-08-19 — Added 15 tests (`GraphNodeTests` + `HoverSummaryTests` additions): per-kind identity/kind/model/summary (entity, element, dependency with/without constraint, group), factory null-input → null, `NodeVerbs` presets per kind, `ForColumn` (full/minimal/null), `ForGroup` (header+count/null). Library + app build **0 errors, 0 warnings**; full suite **154/154 pass** (was 139).

## Results

- **Completed:** `028`
- **Deferred:** none — `029` (editable entities) is the next item in the object-oriented drawing series: the edit verbs defined here get wired to real model operations.
- **Notes:** Element and Group nodes are modeled + unit-tested (portable surface) but **not canvas-rendered** — columns are hit-test transparent by design (010 — the whole table drags), and no group drawable exists ("new drawing primitives" is out of scope). The canvas surfaces Entity + Dependency only. Visual verification (hover/click unchanged, now sourced from `obj.Node`) needs a manual pass — CLI launch runs on the agent's non-interactive desktop.
