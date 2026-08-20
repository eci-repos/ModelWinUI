# 039 — Collapsed group boxes: UML package nodes as first-class drawables

## Summary

Visibility filtering (038) gets a 2000-table model down to *one subsystem* — but showing a large group still yields a tangle, and that is the actual case that makes big models impractical. The industry answer (and the one that composes with 037's tags) is the **collapsed group box**: when a group is collapsed, it renders as a **single box** — a rounded rectangle with the group's name, stereotype, and table count — and its *external* FK edges are aggregated to the box boundary (one connector per external target), internal edges hidden. Each group has three states: **collapsed (box) / expanded (members) / hidden (nothing)**. **UML seam:** the collapsed box is, by construction, a **UML package node** in a package diagram — name compartment, nesting for overlapping tag sets, stereotype `<<package>>` — so it doubles as the package-diagram view (040) with no separate drawing.

## Goals

- [ ] A `GroupBox` primitive in **both** renderers (XAML `Gl*` + Skia) — the collapsed group as a UML package node: name compartment, table count, per-group tint (036 palette), rounded to match the tables.
- [ ] Pure external-edge aggregation in `Model.Graph`: for a collapsed group, compute its member set (union of tagged tables) and its **external edges** (edges with exactly one endpoint in the group), aggregated one connector per external target.
- [ ] Three states per group — collapsed / expanded / hidden — driven from the same view state as 038 (explorer toggles + a collapse affordance on the box).
- [ ] Collapsed groups participate in layout as a single rect; expanded groups flow their members inside. Routing runs over box-level edges when collapsed (the 2000-table win).
- [ ] **UML:** the box follows UML package-diagram notation (name compartment, nesting, count) so the same primitive renders the package view in 040.

## Scope

**In scope:**
- Model.Graph: `GroupBoxAggregation` (pure, deterministic) + tests.
- Renderers: `GroupBox` primitive in XAML and Skia (parity, 003), layout integration, interaction (click to expand, collapse back).
- Editor: state wiring with 038's groups list; per-group tint source.
- Docs/WORKLOG.

**Out of scope:**
- Full UML notation toggle / PlantUML export (040).
- Tag editing (037), visibility rule (038).
- Saved named views.
- Dependency-closure ("show table + reachable within N hops") — noted as a future item.

## Approach / Notes

- **`GroupBoxAggregation.Build(tables, edges, group)` → box model:** member tables = union of tables tagged with the group's tag; external edges = edges with exactly one endpoint in the group (internal edges are hidden when collapsed); the box's boundary connectors are deduplicated **per external target** (one connector per target table or target group, labeled with a count when several FKs share a target — the "aggregate, don't spray" rule). Pure and unit-tested; feeds layout + routing the same as a table.
- **Overlapping tags (entity in N groups) — the UML nuance:** UML gives a class one *owning* package and *references* from others. Match that: an entity renders inside its **primary** group (its first tag); in the other groups it appears only as an external reference connector stub on that group's box. Document this rule in the aggregation doc comment — it is the one place the "entity in many groups" requirement needs an explicit policy.
- **Renderer primitive (both stacks):** XAML — a `Gl*`-based box (rounded rect + name TextBlock + count + tint border/fill); Skia — a `RectangleHalf`-style box with `GlText`. Both hit-testable (click expands) and draggable like a table. Tint comes from the shared palette (036) — a per-group pastel distinct from the model banner colors.
- **Layout:** v1 keeps it simple — collapsed groups are laid out by the existing engine as blocks; an expanded group is a container its members flow inside (a nested layout pass). Flag the nested-pass as the main complexity; if it grows, split the container layout into its own sub-step. Routing: when collapsed, only box-level edges route (the A* cost drops with the edge count — the actual scaling lever for 2000 tables).
- **Interaction:** click a box → expand (show members, internal edges re-route); an expand/collapse toggle on the box and in the explorer; the explorer drives hide via 038. State shared: 038's group toggle + 039's collapse flag are both view-side.
- **Parity:** every change lands in both renderers in one commit (003 discipline); the 012 invariant (no connector crosses a table) extends to "no connector crosses a collapsed box" — aggregated external edges must route around boxes like tables.
- **Tests:** `GroupBoxAggregationTests` (member union, external edges, dedup per target, overlap primary-group rule), routing sanity on a collapsed box (012).

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; `dotnet test tests/ModelConsole.Tests` → all pass.
- [ ] Collapsing a group in the 50-table sample renders one box with a per-target external connector set; expanding restores the members and internal edges.
- [ ] Both renderers draw the box identically (tint, name, count, border); box-level edges route without crossing the box.
- [ ] An entity tagged into two groups renders in its primary group and as a reference stub in the other.
- [ ] Manual run: collapse/expand/hide a group from the explorer and from the box itself; pan/zoom/drag still work on boxes; untagged models unaffected.
- [ ] `docs/WORKLOG.md` updated (and `CLAUDE.md`: `GroupBoxAggregation` in the pure-modules list, the group-box primitive in both stacks).

## Status

- **State:** Planned
- **Sprint:** not yet scheduled
- **Completed:** —
