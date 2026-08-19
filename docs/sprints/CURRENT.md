# Current Sprint

**No sprint is currently in execution.**

The last sprint (2026-08-19, editable entities — backlog item `029`) was promoted to `docs/sprints/archive/sprint-2026-08-19-editable-entities.md`. `029` is part 3 of the **object-oriented drawing series**: the 028 edit verbs are wired to real model operations. Pure `ModelConsole.Editing.ModelEdits` operations (cascade renames, invariant-preserving key flags) mutate the live canonical model; the inspector builds its edit surface gated by the 028 `NodeVerbs`; the host re-renders (partial re-route where localized) and resolution issues surface via `FkEdgeExtractor` (R8) — never a crash or a dangling connector.

**The object-oriented drawing series (027–029) is complete.** When the next sprint is defined, copy `docs/sprints/_TEMPLATE.md` here and start executing.
