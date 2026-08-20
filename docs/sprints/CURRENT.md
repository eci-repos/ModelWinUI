# Current Sprint

**No sprint is currently in execution.**

The last sprint (2026-08-20, tags on the model — backlog item `037`) was promoted to `docs/sprints/archive/sprint-2026-08-20-tags-on-the-model.md`. `037` added **`TableInfo.Tags`** — the first *persisted* per-entity annotation — a serialized `List<string>` (written only when present, so older model files stay byte-identical) that round-trips through both JSON formats and both schemas. Tags are edited in the inspector through the pure `ModelEdits.SetTableTags` (UML-identifier hygiene enforced; rejected names surface on the diagnostics log), shown in the explorer nodes (`[tag, tag]` suffix) and the hover/readout (`Tags: …` line), and captured from JSON by the interpreter (`TagsField` on the array and grouped profiles). Tags are the deliberate bridge to UML: they map to UML package/stereotype names losslessly (038–040 build on them).

**The backlog holds `038`–`040`** (visibility projection, collapsed group/package boxes, UML notation/export), unscheduled. Next work: whichever the user picks — `038` (visibility projection) builds directly on `037`'s tags.
