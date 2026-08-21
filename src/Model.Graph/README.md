# Model.Console.Graph

ERD domain logic over `Model.Data`. `FkEdgeExtractor` resolves FK
`ConstraintInfo` references into `FkRelation` edges (parent PK default,
reporting resolution issues); `TableLayoutEngine` lays tables out into
non-overlapping `Rect2` slots; `EntityVisibility` / `ModelProjection` filter
the model to a visible set (the UML package-diagram viewpoint);
`GroupCollapseState` / `GroupBoxAggregation` collapse a group into one package
box (external edges aggregated per target); `GraphNode` / `HoverSummary` expose
the portable node surface (identity, live model, summary, edit verbs); and
`ModelEdits` is the pure, invariant-preserving edit surface — renames cascade
across referencing FKs, key flags recompute from constraints after add/remove,
and a broken edit surfaces as a resolution issue, never a crash.

**Dependencies:** `Model.Data`, `Model.Geometry`.

**Usage**

```csharp
var table = new TableInfo { TableName = "Doctor",
   Columns = new List<ColumnInfo> { new() { ColumnName = "Id" } } };
table.Columns[0].Add(new ConstraintInfo { Type = DataInfo.PRIMARY_KEY });
ModelEdits.RenameTable(tables, "Doctor", "Physician");   // cascades FKs
```
