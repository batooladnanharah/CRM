> **Fetched from jira:** [CRM-104](https://batooladnanharah.atlassian.net/browse/CRM-104)  
> *Fetched 2026-08-25T20:48:28.020Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** CRM Reports & Management Dashboard  
**Type:** CRM  
**Status:** In Progress  
**Assignee:** Batool Harah

### Description

User Story

As a manager or authorized support user, I want to view CRM reports and management dashboards so that I can monitor ticket volume, agent performance, SLA performance, and overall support operations.

Objective

Provide a simple reporting and management dashboard that consolidates the core reporting capabilities from:

	REPORT-001 — Dashboard & KPI Reporting

	REPORT-002 — Ticket & Agent Performance

	REPORT-003 — SLA & Resolution Analytics

This is one implementation story combining the three original business stories.

REPORT-004 — Report Export & Scheduling is deferred and is not part of this implementation.

Core Requirements

The dashboard should provide:

	Ticket volume and basic ticket KPIs.

	Ticket status distribution.

	Agent performance indicators.

	SLA performance indicators.

	Resolution metrics.

	Basic management-level summaries.

Use the existing ticket and SLA data already available in the system.

Example

Reports & Management

Total Tickets       Open Tickets       Resolved
     120                  32               88

Ticket Performance
Open          32
In Progress   18
Pending       14
Resolved      56

Agent Performance
Sara Ahmed       24 tickets
Ahmed Hassan     19 tickets
Mohamed Ali      17 tickets

SLA Performance
Within SLA       82%
At Risk          10%
Breached          8%

Resolution
Average Resolution Time: 4h 32m

Scope

Implement a basic working reporting dashboard, not an advanced analytics platform.

Reuse:

	Existing ticket data

	Existing agent data

	Existing SLA data

	Existing dashboard/UI components

	Existing authentication and authorization

	Existing API patterns

Do not create duplicate ticket, agent, or SLA models.

Not in Scope

Do not implement:

	Report export

	Scheduled reports

	Email report delivery

	Advanced analytics

	Custom report builder

	Complex charting infrastructure

	Predictive analytics

	AI-generated reports

These can be implemented later.

Traceability

REPORT-001 ─┐
REPORT-002 ─┼──→ CRM Reports & Management Dashboard
REPORT-003 ─┘

REPORT-004 → Deferred

Implementation unit: ONE story.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/report-management/CRM-104/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `report-management`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-104` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `CRM`
- **Status:** `In Progress`
- **Assignee:** `Batool Harah`
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
CRM Reports & Management Dashboard
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a manager or authorized support user, I want to view CRM reports and management dashboards so that I can monitor ticket volume, agent performance, SLA performance, and overall support operations.

Objective

Provide a simple reporting and management dashboard that consolidates the core reporting capabilities from:

	REPORT-001 — Dashboard & KPI Reporting

	REPORT-002 — Ticket & Agent Performance

	REPORT-003 — SLA & Resolution Analytics

This is one implementation story combining the three original business stories.

REPORT-004 — Report Export & Scheduling is deferred and is not part of this implementation.

Core Requirements

The dashboard should provide:

	Ticket volume and basic ticket KPIs.

	Ticket status distribution.

	Agent performance indicators.

	SLA performance indicators.

	Resolution metrics.

	Basic management-level summaries.

Use the existing ticket and SLA data already available in the system.

Example

Reports & Management

Total Tickets       Open Tickets       Resolved
     120                  32               88

Ticket Performance
Open          32
In Progress   18
Pending       14
Resolved      56

Agent Performance
Sara Ahmed       24 tickets
Ahmed Hassan     19 tickets
Mohamed Ali      17 tickets

SLA Performance
Within SLA       82%
At Risk          10%
Breached          8%

Resolution
Average Resolution Time: 4h 32m

Scope

Implement a basic working reporting dashboard, not an advanced analytics platform.

Reuse:

	Existing ticket data

	Existing agent data

	Existing SLA data

	Existing dashboard/UI components

	Existing authentication and authorization

	Existing API patterns

Do not create duplicate ticket, agent, or SLA models.

Not in Scope

Do not implement:

	Report export

	Scheduled reports

	Email report delivery

	Advanced analytics

	Custom report builder

	Complex charting infrastructure

	Predictive analytics

	AI-generated reports

These can be implemented later.

Traceability

REPORT-001 ─┐
REPORT-002 ─┼──→ CRM Reports & Management Dashboard
REPORT-003 ─┘

REPORT-004 → Deferred

Implementation unit: ONE story.
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
