# Sprint 2026-08-19 — Optimal connector routes: crossing is a cost, not a ban

> Executed copy of the sprint. Definition: `docs/backlog/archive/033-connector-routing-optimal-paths.md`.

## Dates

- **Start:** 2026-08-19
- **End:** 2026-08-19

## Scope

Backlog items in this sprint:

- [x] `033` — Connector routing takes optimal paths: crossing is a cost, not a ban

## Execution Log

- 2026-08-19 — Sprint defined from backlog item 033 (a user report: "some of the connector path lines are extremely long and the optimal path is not calculated"). Measured the symptom on the 50-table public-safety model with a throwaway diagnostic (`RouteDiagnosticTests`): 2 of 74 routes exceeded 2× their anchor distance — worst `Employee → Person` at **9.6×** (8252 px vs 861 px straight), routed out to the routing-region bounds (`x=3624 / y=−24`). Routing the same edge alone gives the optimal 1212 px. Root cause: `SequentialRouter.RouteAll` feeds each routed polyline back as a thin "wall" and `OrthogonalRouter.Route` **banned** crossing a connector (retrying without walls only when the grid was *unreachable*, never when the detour was merely absurdly long); the first edges claim the short corridors and late short edges detour around the accumulated walls. Nearest-first ordering was measured and rejected (4 routes > 2× — it just moves the cascade).
- 2026-08-19 — Fix: crossing becomes a cost. `RouterOptions.CrossingTolerance` (default 1.5); `OrthogonalRouter.RouteBest` routes **with and without** the walls and returns the wall-ignoring route only when `len(without) × tolerance < len(with)`; new public `PolylineLength` helper. `SequentialRouter.RouteAll` and `ModelPanelControl`'s drag re-route path call `RouteBest` and feed the chosen route back as walls (self-consistent, deterministic). Table interiors are never crossed in either candidate.
- 2026-08-19 — Tests (+4, 180 total): `OptimalRoutingTests` — end-to-end `PublicSafetyRoutesStayNearOptimal` (ErdComposer: **0 routes > 2×**, max 1.98× on a genuinely diagonal Facility→Agency edge; `Employee → Person` at **1212 px**), `AbsurdDetourCrossesTheWall` (766 vs 350 px — tolerance 1.5 crosses), `CheapDetourKeepsTheWallAvoidingRoute` (tolerance 10 avoids), `CrossingRouteNeverCrossesATable` (012 invariant holds for the crossing route).
- 2026-08-19 — Verified: full solution `-p:Platform=x64` → **0 errors, 0 warnings**; `dotnet test` → **180/180 pass** (176 baseline + 4 new; the throwaway diagnostic was folded into `OptimalRoutingTests`).

## Results

- **Completed:** `033`
- **Deferred:** — (the backlog is again empty)
- **Notes:** Manual verification of the rendered connector paths (no stray long detours, drag re-route still clean) needs a human pass — CLI launch runs on the agent's non-interactive desktop. `RouterOptions.CrossingTolerance` (1.5) is a taste knob: lower → shorter lines but more connector crossings; higher → cleaner look, occasionally longer routes.
