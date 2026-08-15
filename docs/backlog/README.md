# Backlog

Plans for scheduled work and sprint definitions live here. A backlog item is a single file that describes one piece of planned work in enough detail that any agent can pick it up and execute it.

## Conventions

- One file per backlog item, named `NNN-short-description.md` (e.g. `001-ortho-connector-grips.md`). Use the next free number.
- Copy `_TEMPLATE.md` to start a new item.
- Items are **planned** work. Once a sprint is defined from one or more items, the sprint definition is also kept here until it is executed.
- The sprint currently in execution is tracked in `docs/sprints/CURRENT.md` (there is only ever one current sprint).
- When an item is **completed** (or otherwise resolved), move its file to `archive/` and record the promotion in `docs/WORKLOG.md`.

## Workflow

1. Create the item file from `_TEMPLATE.md`.
2. When the work is scheduled, reference the item from the sprint definition in `docs/sprints/`.
3. On completion, move the file to `archive/` and update `docs/WORKLOG.md`.

## Archive

`archive/` holds completed or superseded backlog items. It is a record of what was planned and delivered — do not delete archived items.
