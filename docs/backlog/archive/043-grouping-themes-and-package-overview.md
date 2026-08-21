# 043 — Grouping themes and the package overview for very large models

## Summary

On a model with hundreds or thousands of tables, a flat drawing is unreadable — nobody can make sense of it, so the model has to be *viewed through groups*. Today a group *is* a tag (037), and tags are authored per-entity in the inspector: you must already understand the model before you can group it, which is exactly backwards for discovery. This item makes grouping a first-class, **theme-driven** capability: groups can be **derived automatically** (schema, table kind, FK connectivity) as well as authored (tags), and the model is viewed through a **package overview** — collapse-all-by-theme turns each group into one UML package box with a member count and aggregated inter-group edges, so a 1,000-table model reads as N boxes and M dependency lines. This is the discovery + readability layer for large models; the same viewpoint is what 040 later renders as UML.

## Goals

- [ ] **Grouping-theme abstraction:** one pure `GroupingTheme` that maps a `TableInfo` → group name (null = no group). `EntityVisibility.Create` and the explorer's Groups section take a theme; tags become *one* theme, not the only one.
- [ ] **Schema theme:** `GroupOf(t) = t.SchemaName` — zero-authoring grouping for multi-schema models (the containerized form 023 already carries it).
- [ ] **Kind theme:** entity vs reference-code via `TableKindClassifier` — a coarse but always-available split.
- [ ] **Connectivity theme:** weakly-connected components of the FK graph (union-find over `FkEdgeExtractor` edges) — auto-suggested functional clusters with **no metadata at all**, the answer to "quickly identify the grouping opportunities" on a single-schema, untagged model.
- [ ] **Package overview view:** a "collapse all by theme" affordance → one UML package box per group (reuse 039's `GroupBox`/`GroupBoxAggregation`) with member count + aggregated inter-group edges; both renderers.
- [ ] **Group stats in the explorer:** each group row shows its table count (and FK count) so the model's shape is visible at a glance.
- [ ] **Group-first explorer:** a tree mode with the active theme's groups as top-level nodes and tables nested under them (the flat schema-root → table list stays available).
- [ ] **Zoom-to-group:** fit the drawing to a group's members.
- [ ] A **multi-schema sample model** exercising the feature (the current samples are single-schema, so the schema theme is invisible today).

## Scope

**In scope:**
- The theme abstraction + the three derived themes (schema, kind, connectivity) in Model.Graph (pure, unit-tested).
- `EntityVisibility.Create(tables, theme)` and the explorer Groups section driven by the active theme.
- Package overview: collapse-all-by-theme + group stats, both renderers (the 038/039 shared-state wiring already keeps them in parity).
- Group-first explorer + zoom-to-group.
- A multi-schema sample.
- Docs/WORKLOG.

**Out of scope:**
- UML notation/export — that is 040; this item's package view is the same viewpoint 040 will render, but 040 stays its own item.
- Persisting themes or auto-assigned groups to the model file — themes are **derived at view time**; tags remain the only persisted grouping (the 037 seam).
- Auto-layout of group boxes — the existing layout engine places boxes; group-level layout polish is a future item.
- Editing group membership from the package view — boxes are view-only; membership is edited via tags in the inspector.

## Approach / Notes

- **The machinery is already theme-agnostic.** `EntityVisibility`, `ModelProjection`, `GroupCollapseState`, and `GroupBoxAggregation` all operate on *group names* — they never read tags. The only tag-hardcoded spots are `EntityVisibility.Create(tables)` (derives the group universe from tags) and the explorer's Groups section. So this is a **theme seam**, not a rewrite: introduce the theme, feed it where the group universe is derived, and the projection/collapse/aggregation layers work unchanged.
- **Theme abstraction (pure, Model.Graph):** `GroupingTheme` with `GroupOf(TableInfo) → string?` and `Groups(IEnumerable<TableInfo>) → ordered group names` (deterministic ordering — the repo bans nondeterministic output). `EntityVisibility.Create(tables, theme)`; the explorer lists `theme.Groups(tables)`.
- **Schema theme:** `GroupOf(t) = t.SchemaName`. The containerized form (023) already populates `SchemaName` per table, so a multi-schema model groups with zero authoring. The `PublicSafety` sample is one schema, so a multi-schema sample is needed to make the theme visible.
- **Kind theme:** `GroupOf(t) = TableKindClassifier.Classify(t).ToString()` — entity vs reference-code. Coarse, but always available and a useful first split on any model.
- **Connectivity theme:** union-find over the `FkEdgeExtractor.Extract` edges → one component id per table; components with ≥ 2 tables become groups (singletons stay ungrouped). This is the discovery answer for the no-metadata case: one schema, no tags, and the model's natural functional clusters appear with no authoring. Cheap (near-linear), deterministic.
- **Package overview:** a "Collapse all" affordance (per theme) collapses every group through the existing `GroupCollapseState`; `GroupBoxAggregation.Build` already produces one box per group with member count and deduplicated per-external-target edges — the package diagram of a 1,000-table model. Both renderers already share the collapse state (039 wiring), so the overview is parity by construction.
- **Group stats:** the explorer's group rows gain a count readout (tables; FKs within the group) computed from the theme + `FkEdgeExtractor` — the "shape of the model" at a glance (one giant schema vs. many small ones).
- **Group-first explorer:** a tree mode where the active theme's groups are the top-level nodes and tables nest under them. With 1,000 tables the flat tree is itself unusable; this is the scalable shape. The existing schema-root → flat table list stays as the default.
- **Zoom-to-group:** `ModelPanelControl` fits to the union of a group's member bounds, reusing the `FitToWindow` math (viewport-px offsets, 5 px margin).
- **Sequencing:** (1) theme abstraction + schema/kind themes, (2) package overview + group stats, (3) connectivity auto-suggest, (4) group-first explorer + zoom-to-group. Each lands with tests; the item is one feature but the goals are independently shippable.
- **Relationships:** builds on 037 (tags as the authored theme), 038 (visibility projection — the viewpoint machinery), 039 (collapse + package boxes — the overview's building block). The package view is the same viewpoint 040 will render as UML; this item is the interactive discovery/readability layer, 040 is the notation/export layer.
- **Tests:** theme unit tests (schema/kind/connectivity — deterministic group assignment), `EntityVisibility.Create` over a theme, projection over a theme, package-overview aggregation (box count + external edges), explorer group stats. Manual: load a multi-schema model, group by schema, collapse all → one box per schema with aggregated edges; both renderers identical.

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; `dotnet test tests/ModelConsole.Tests` → all pass.
- [ ] A multi-schema model groups by schema with zero authoring; both renderers draw the identical grouped view.
- [ ] Collapse-all-by-theme shows one package box per group (member count + aggregated inter-group edges); expanding restores the full drawing.
- [ ] The connectivity theme proposes groups on a no-metadata model (one schema, no tags).
- [ ] The explorer shows group stats and a group-first tree; zoom-to-group fits the drawing to a group's members.
- [ ] Tags remain the only persisted grouping; themes are derived at view time and never touch the model file.
- [ ] `docs/WORKLOG.md` updated.

## Status

- **State:** Completed
- **Sprint:** sprint-2026-08-21-grouping-themes-and-package-overview
- **Completed:** 2026-08-21
