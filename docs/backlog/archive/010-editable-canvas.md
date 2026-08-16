# 010 — Editable canvas: drag tables, connectors follow, entity inspector, delete-and-regenerate

## Summary

EDAM Studio is not just a viewer — the user (the chief) wants to work with the drawing. This sprint makes the canvas editable: drag tables around and their connectors follow, click any graphic entity to inspect its metadata, add/remove metadata, and delete a connector so the remaining paths regenerate as simply as possible. Editing table text is explicitly out of scope for now.

## Goals

- [ ] Drag a table; its connectors re-route and follow it.
- [ ] Click any graphic entity → an inspector shows its metadata (read-only first).
- [ ] Add/remove metadata from the inspector → the drawing re-renders.
- [ ] Delete a connector → the remaining connectors regenerate efficiently and as simply as possible, by default.
- [ ] The `Model.Data` POCOs are the source of truth; graphics are a view of them.

## Scope

**In scope:**
- **Drag tables:** wire `Table` into `GlContext`'s existing pointer/grabber machinery (`GlHandle` = move). On move, update the table's layout rect and re-route its connectors. Recompute row Ys on move (`GetRowCenterY` is static-layout correct only).
- **Re-route on change (the "regenerate by default" principle):** a route is always *derived* from the current state — a pure function of (edges, obstacles) — never a frozen artifact. Move / add / delete all just re-run the router. Start with full re-route on release (74 edges is cheap); optimize to only re-route edges touching the changed table later.
- **Delete a connector:** removing a relationship removes it from the model and re-routes the remaining connectors so they use the freed space and become as simple as possible — automatically, no manual re-route button. Default rule: simplest **non-crossing** path (crossings still win over absolute simplicity).
- **Entity inspector:** hit-test the clicked entity, show its `TableInfo` / `ColumnInfo` / `ConstraintInfo` in a details panel (the `DiagnosticsLogControl` slot is a natural home). Read-only first; then edit POCO fields → re-render.
- **Model/view separation:** dragging updates the layout rect, never the graphics directly; the graphics re-render from the model.
- Update `docs/WORKLOG.md`, `docs/codebase-functionality-map.md`, `CLAUDE.md` as needed.

**Out of scope:**
- Editing table text (explicitly deferred).
- Add/remove whole tables and columns (follow-up — inspector edit comes first).
- Undo/redo (defer, but keep the model/view separation so it's not painted into a corner).
- Live re-route during drag (re-route on release first; optimize later).

## Approach / Notes

- **Drag infrastructure mostly exists:** `GlContext` already has press/move/release/capture, selection, and grabbers (`GlHandle` = move, `GlGrip` = resize). The missing pieces are wiring `Table` into it and recomputing row Ys on move.
- **Routing is a pure function of (edges, obstacles):** `OrthogonalRouter.Route` already returns a `List<Point2>`; the XAML `Path` is just a view of it. Any change (move, add, delete) re-runs the router — this is what makes "regenerate by default" free.
- **"As simple as possible" is the router's default:** direct HV/VH path first, then A*, then collinear simplification. Deleting a connector frees space, so the remaining edges re-route to simpler paths automatically.
- **Sequencing:** this item depends on 008 (sequential routing + anchor-side selection make re-routing sane) and 009 (zoom's hit-testing fix is the same root cause dragging needs). It is the third item in the sprint.
- **Layout engine is static:** `TableLayoutEngine` is a one-shot grid layout; dragging is a manual override of the layout. The layout becomes user-driven once a table is moved.

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors.
- [ ] App launches unpackaged; a table can be dragged and its connectors follow (re-routed on release).
- [ ] Clicking a table/column shows its metadata in the inspector; editing a POCO field re-renders the drawing.
- [ ] Deleting a connector removes the relationship and the remaining connectors regenerate as simple non-crossing paths, automatically.

## Status

- **State:** Completed / Archived
- **Sprint:** `docs/sprints/CURRENT.md` (2026-08-16 connector routing)
- **Completed:** 2026-08-16
