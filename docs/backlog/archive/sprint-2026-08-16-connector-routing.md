# Sprint 2026-08-16 — Connector routing order & readability

> Sprint definition (planned). Backlog items: `docs/backlog/008-connector-routing-order.md`, `docs/backlog/009-zoom-and-fit.md`, `docs/backlog/010-editable-canvas.md`.
> When the sprint starts, copy this to `docs/sprints/CURRENT.md` and execute.

## Dates

- **Start:** 2026-08-16
- **End:** (TBD)

## Scope

Backlog items in this sprint (reference by number):

- [ ] `008` — Connector routing: logical order, port-based anchors, endpoint markers
- [ ] `009` — Zoom & fit: scale slider, % entry, fit-to-window
- [ ] `010` — Editable canvas: drag tables, connectors follow, entity inspector, delete-and-regenerate

## Execution Log

- 2026-08-16 — Sprint defined from backlog items `008` + `009`. Approach confirmed with user: the router already produces the desired stub-out / turn / bend-in shape; the crossing problem is **edge-ordering** (each edge routed independently), plus hardcoded right→left anchors and no port fan-out. Fixes: sequential routing (routed edges become obstacles), per-edge anchor-side selection, shared-column fan-out, `StubLength` → 20, endpoint circles.
- 2026-08-16 — Zoom added as item `009`: use the ScrollViewer's native zoom (`ZoomFactor` / `ChangeView`) driven by a slider + % box + fit button; zoom-around-cursor; verify pointer hit-testing at non-100% zoom.
- 2026-08-16 — Editable canvas added as item `010`: drag tables (connectors follow), entity inspector over the `Model.Data` POCOs, and delete-a-connector → remaining paths regenerate as simple non-crossing routes by default. Principle: a route is always *derived* from current state, never a frozen artifact. Depends on 008 + 009.

## Results

- **Completed:** (item numbers)
- **Deferred:** (item numbers + why)
- **Notes:** (anything the next agent needs to know)
