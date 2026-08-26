> **Fetched from jira:** [CRM-95](https://batooladnanharah.atlassian.net/browse/CRM-95)  
> *Fetched 2026-08-25T08:55:34.890Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** Ticket Management — Assignment, Status & Priority  
**Type:** CRM  
**Status:** In Progress  
**Assignee:** Batool Harah

### Description

User Story:

As an authorized support user, I want to assign tickets, change their status, and change their priority so that I can manage each ticket throughout its support lifecycle.

Scope:

	Assign ticket

	Reassign ticket

	Unassign ticket

	Select eligible support agent

	Validate agent

	Change ticket status

	Validate status transitions

	Change ticket priority

	Validate priority

	Authorization

	Ticket history for changes

	Loading states

	Success/error handling

	PostgreSQL persistence

	Reuse existing Ticket, User, authorization, and history models

	Reuse existing ticket details UI

Not in scope:

	Automatic assignment

	SLA automation

	AI priority suggestions

	AI status suggestions

	Workload balancing

	Workflow engine

Source business requirements:

This story consolidates:

	TKT-005 — Ticket Assignment

	TKT-006 — Ticket Status

	TKT-007 — Ticket Priority

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/ticket-management/CRM-95/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `ticket-management`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-95` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `CRM`
- **Status:** `In Progress`
- **Assignee:** `Batool Harah`
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Ticket Management — Assignment, Status & Priority
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story:

As an authorized support user, I want to assign tickets, change their status, and change their priority so that I can manage each ticket throughout its support lifecycle.

Scope:

	Assign ticket

	Reassign ticket

	Unassign ticket

	Select eligible support agent

	Validate agent

	Change ticket status

	Validate status transitions

	Change ticket priority

	Validate priority

	Authorization

	Ticket history for changes

	Loading states

	Success/error handling

	PostgreSQL persistence

	Reuse existing Ticket, User, authorization, and history models

	Reuse existing ticket details UI

Not in scope:

	Automatic assignment

	SLA automation

	AI priority suggestions

	AI status suggestions

	Workload balancing

	Workflow engine

Source business requirements:

This story consolidates:

	TKT-005 — Ticket Assignment

	TKT-006 — Ticket Status

	TKT-007 — Ticket Priority
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```

```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| *(e.g. `attachments/flow.png`)* | *(e.g. UX flow)* |

*(Add rows per file. If none, write "None.")*

---

## Dependencies

- **Blocked by / related ids:** (tracker ids only; optional short note)
- **Depends on code areas or other stories:**

## Extra notes (optional)

- Anything not captured above (e.g. chat context) — keep short.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.

## Out of scope

- What this story explicitly does **not** cover:
