> **Fetched from jira:** [CRM-80](https://batooladnanharah.atlassian.net/browse/CRM-80)  
> *Fetched 2026-08-26T17:36:50.339Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** ADMIN-001 — User Management  
**Type:** Task  
**Status:** In Review

### Description

User Story

As an administrator, I want to manage CRM users so that authorized employees can access the system according to their responsibilities.

Description

Implement administration functionality for managing CRM users.

The administrator should be able to:

	View users

	Create users

	Update user information

	Activate/deactivate users

	Assign roles to users

	Search/filter users

	Prevent unauthorized users from accessing the system

User management must integrate with the existing authentication system.

Acceptance Criteria

	Admin can view users.

	Admin can create a user.

	Admin can update a user.

	Admin can activate/deactivate a user.

	Admin can assign a role.

	Unauthorized users cannot manage users.

	User changes are recorded in the audit log.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/security-administration/CRM-80/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `security-administration`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-80` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
ADMIN-001 — User Management
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As an administrator, I want to manage CRM users so that authorized employees can access the system according to their responsibilities.

Description

Implement administration functionality for managing CRM users.

The administrator should be able to:

	View users

	Create users

	Update user information

	Activate/deactivate users

	Assign roles to users

	Search/filter users

	Prevent unauthorized users from accessing the system

User management must integrate with the existing authentication system.

Acceptance Criteria

	Admin can view users.

	Admin can create a user.

	Admin can update a user.

	Admin can activate/deactivate a user.

	Admin can assign a role.

	Unauthorized users cannot manage users.

	User changes are recorded in the audit log.
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
