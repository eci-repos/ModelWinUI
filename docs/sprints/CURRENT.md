# Current Sprint

**No sprint is currently in execution.**

The last sprint (2026-08-19, graph node objects — backlog item `028`) was promoted to `docs/sprints/archive/sprint-2026-08-19-graph-node-objects.md`. `028` is part 2 of the **object-oriented drawing series**: every drawable on the XAML canvas is now a first-class **node object** — a portable `IGraphNode` surface (kind, identity, the live canonical object it renders, a hover summary, edit verbs) in ModelGraphLibrary that hover, click-select, and the inspector all read. Recognition is `GlObject.Node`; the host consumes `obj.Node` and never type-checks raw objects. Node summaries delegate to `HoverSummary` (the 022 discipline).

**The object-oriented drawing series is in progress: `027` and `028` are done, `029` (editable entities — rename/add/remove entities + columns, edit FK targets/cardinality/roles/metadata from the inspector; the 028 edit verbs get wired to real model operations) remains.** When the next sprint is defined, copy `docs/sprints/_TEMPLATE.md` here and start executing.
