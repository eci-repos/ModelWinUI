# Docs

This folder is the project's working documentation. It is the single source of truth for **what work is planned**, **what work has been done**, and **what to do next**.

## Folder layout

```
docs/
├── README.md          ← this file: how the docs are organized
├── WORKLOG.md         ← running record of work done + next pending tasks (handoff doc)
├── backlog/           ← plans for scheduled work and sprint definitions
│   ├── README.md      ← backlog conventions + workflow
│   ├── _TEMPLATE.md   ← template for a new backlog item
│   └── archive/       ← completed/promoted backlog items are moved here
└── sprints/           ← the current sprint + a copy of each executed sprint
    ├── README.md      ← sprint conventions + workflow
    ├── CURRENT.md     ← the single current sprint (in execution)
    ├── _TEMPLATE.md   ← template for a sprint record
    └── archive/       ← promoted (executed) sprints, named sprint-YYYY-MM-DD-<name>.md
```

**One current sprint:** `docs/sprints/CURRENT.md` is the only current sprint. When it is promoted, move it to `sprints/archive/` with its dated name and start the next sprint in `CURRENT.md`.

## Workflow

1. **Plan** — scheduled work and sprint definitions are written as backlog items in `docs/backlog/`. Each item is one file, named `NNN-short-description.md`.
2. **Execute** — when a sprint runs, a copy of its definition is placed in `docs/sprints/` (named `sprint-YYYY-MM-DD-<name>.md`) and updated with what actually happened.
3. **Record** — every session updates `docs/WORKLOG.md`: what was done, what is pending, and anything the next agent needs to know.
4. **Promote** — when a backlog item is completed, move its file from `docs/backlog/` into `docs/backlog/archive/` and note the promotion in `WORKLOG.md`.

## Handoff

`docs/WORKLOG.md` is the handoff document. Any agent starting work here should read it first, then `Agents.md` at the repo root for the full instruction set.
