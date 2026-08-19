# Current Sprint

**No sprint is currently in execution.**

The last sprint (2026-08-19, optimal connector routes — backlog item `033`) was promoted to `docs/sprints/archive/sprint-2026-08-19-optimal-connector-routes.md`. `033` stops connectors detouring to the drawing's extremes: crossing an already-routed connector is now a **cost**, not a ban — `OrthogonalRouter.RouteBest` routes with and without the accumulated walls and takes the crossing route only when avoiding it costs more than `RouterOptions.CrossingTolerance` (1.5); `SequentialRouter.RouteAll` + the drag re-route use it, and the "no table crossing" invariant (012) is untouched. Measured: **0 routes > 2×** on the 50-table model (worst case `Employee → Person` 8252 → 1212 px).

**The backlog is empty.** Next work: any candidate from the WORKLOG "Known gaps" list (e.g. auto-arrange after drags, undo/redo, a shared drawing-surface abstraction over the two stacks), or whatever the user picks next.
