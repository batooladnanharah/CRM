> **Fetched from jira:** [CRM-81](https://batooladnanharah.atlassian.net/browse/CRM-81)  
> *Fetched 2026-08-26T18:13:48.812Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** ADMIN-002 — Roles & Permissions  
**Type:** Task  
**Status:** Done

### Description

User Story

As an administrator, I want to manage roles and permissions so that users can only access functionality appropriate to their responsibilities.

Description

Implement role-based access control for the CRM.

The system should support:

	Roles

	Permissions

	Assigning roles to users

	Checking permissions before protected operations

	Protecting management functionality

Examples of roles may include:

Admin
Manager
Agent
Customer

Use the roles defined by the project's requirements/design rather than creating unnecessary roles.

Authorization must be enforced on the backend, not only in the Vue frontend.

Acceptance Criteria

	Roles exist.

	Permissions exist.

	Users can be assigned roles.

	Protected API endpoints verify permissions.

	Unauthorized operations are rejected.

	Frontend hides unavailable functionality.

	Backend remains the authoritative authorization layer.

	Permission changes are audited.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/security-administration/CRM-81/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `security-administration`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-81` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `Done`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
ADMIN-002 — Roles & Permissions
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As an administrator, I want to manage roles and permissions so that users can only access functionality appropriate to their responsibilities.

Description

Implement role-based access control for the CRM.

The system should support:

	Roles

	Permissions

	Assigning roles to users

	Checking permissions before protected operations

	Protecting management functionality

Examples of roles may include:

Admin
Manager
Agent
Customer

Use the roles defined by the project's requirements/design rather than creating unnecessary roles.

Authorization must be enforced on the backend, not only in the Vue frontend.

Acceptance Criteria

	Roles exist.

	Permissions exist.

	Users can be assigned roles.

	Protected API endpoints verify permissions.

	Unauthorized operations are rejected.

	Frontend hides unavailable functionality.

	Backend remains the authoritative authorization layer.

	Permission changes are audited.
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
