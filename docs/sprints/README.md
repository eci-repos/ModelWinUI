# Sprints

A copy of each **executed** sprint is kept here. The sprint record starts from the sprint definition (planned in `docs/backlog/`) and is updated with what actually happened during execution.

## The one current sprint rule

There is **exactly one current sprint** at any time: `docs/sprints/CURRENT.md`.

- `CURRENT.md` holds the sprint currently in execution (or the most recently promoted sprint, until the next one starts).
- When a sprint is **promoted** (completed), move `CURRENT.md` to `docs/sprints/archive/sprint-YYYY-MM-DD-<name>.md` and start the next sprint in `CURRENT.md`.
- Never create a second current-sprint file. If you need to reference a past sprint, it lives in `archive/`.

## Conventions

- The current sprint is always `CURRENT.md`.
- Promoted (executed) sprints are archived as `sprint-YYYY-MM-DD-<name>.md` in `archive/`.
- Copy `_TEMPLATE.md` to start a sprint record.

## Workflow

1. Define the sprint (scope, items, dates) — the definition lives in `docs/backlog/`.
2. When the sprint starts, write it to `CURRENT.md`.
3. During/after execution, update `CURRENT.md`: what was done, what was deferred, results.
4. On completion, promote: move `CURRENT.md` to `archive/` with its dated name, promote completed backlog items to `docs/backlog/archive/`, and update `docs/WORKLOG.md`.
