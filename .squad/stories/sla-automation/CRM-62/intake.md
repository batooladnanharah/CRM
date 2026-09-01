> **Fetched from jira:** [CRM-62](https://batooladnanharah.atlassian.net/browse/CRM-62)  
> *Fetched 2026-08-30T22:26:11.174Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** SLA-003 — Automatic Ticket Assignment  
**Type:** Task  
**Status:** In Review

### Description

User Story

As a support manager, I want new tickets to be automatically assigned to an appropriate support agent so that tickets are handled quickly without requiring manual assignment for every request.

Objective

Implement simple rule-based automatic ticket assignment.

When a new ticket is created, the system should optionally select an available agent based on:

	Department

	Agent availability

	Current workload

The assignment must happen on the backend.

Scope

This story covers:

	Automatic assignment configuration

	Agent availability

	Basic workload calculation

	Department matching

	Assignment during ticket creation

	Assignment API/service

	Manual assignment fallback

	Validation

	Authorization

	Tests

This story does not cover:

	AI-based assignment

	Skill-based routing

	Complex routing rules

	Predictive workload

	Geographic routing

	Round-robin scheduling engine

	External workforce systems

Recommended MVP Rule

Use this simple priority:

1. Match ticket department
2. Find active agents
3. Prefer available agents
4. Select the agent with the fewest active tickets
5. If no agent is available, leave ticket unassigned

This is enough to demonstrate automatic assignment without building a complicated rules engine.

Assignment Flow

Create Ticket
      ↓
Determine Department
      ↓
Automatic Assignment Enabled?
      ↓
Find Eligible Agents
      ↓
Filter Active/Available
      ↓
Calculate Workload
      ↓
Select Lowest Workload
      ↓
Assign Ticket

If no eligible agent exists:

Create Ticket
      ↓
No available agent
      ↓
Ticket remains Unassigned
      ↓
Manager can manually assign

Agent Eligibility

An agent should be eligible when:

	User is active.

	User has Agent role/permission.

	User belongs to the ticket department.

	User is available.

	User has permission to handle the ticket.

Do not rely on frontend-provided agent IDs.

Agent Availability

For the MVP, availability can be a simple field:

Available
Unavailable

Example:

User
Name: Sara Ahmed
Role: Agent
Department: Support
Availability: Available

Do not implement:

	Calendar schedules

	Shift management

	Vacation management

	Presence detection

	WebSocket presence

Agent Workload

Workload can be calculated using active assigned tickets.

Example:

Agent A → 3 active tickets
Agent B → 8 active tickets
Agent C → 5 active tickets

Selected:
Agent A

Active tickets should exclude statuses such as:

	Resolved

	Closed

Use the existing ticket status model.

Do not create another ticket-status definition.

Workload Query

Prefer database-level counting.

Example concept:

SELECT Agent,
       COUNT(ActiveTickets)
FROM Tickets
WHERE AssignedAgent = Agent
GROUP BY Agent

Do not retrieve every ticket into .NET and calculate workload in memory.

Tie Breaking

If multiple agents have the same workload:

Agent A → 3
Agent B → 3
Agent C → 5

Use a deterministic tie-breaker.

Recommended:

or a stable user ID/order.

Do not introduce randomness.

Department Matching

Tickets may belong to a department.

Example:

Ticket:
Department = Technical Support

Eligible:
Technical Support Agents

Do not assign a Technical Support ticket to a Sales agent unless explicitly allowed by the SDD.

If the project does not yet have departments implemented, automatic department matching may be prepared for the existing department model.

Do not create a complex department system inside this story.

Assignment Configuration

Recommended simple administration setting:

Automatic Assignment

[✓] Enable automatic assignment

Optional:

Assignment Strategy

[ Lowest Workload ▼ ]

For the 2-day MVP, only Lowest Workload is required.

Do not create a configurable rules engine.

Ticket Creation

When a ticket is created:

Auto Assignment Enabled
       ↓
Find Agent
       ↓
Assign

The assignment should happen within the backend ticket creation workflow.

Do not make the Vue application call:

Create Ticket
      ↓
Assign Ticket

as two unrelated operations if the assignment is supposed to be automatic.

Manual Assignment

If automatic assignment fails or is disabled, authorized users can manually assign the ticket.

Reuse the existing assignment functionality from TKT-005.

Do not create another assignment UI.

API

The existing ticket creation endpoint should trigger automatic assignment.

Example:

POST /api/tickets

Request:

{
  "subject": "Unable to login",
  "description": "I cannot login.",
  "priority": "High",
  "departmentId": "department-id"
}

The backend determines the assigned agent.

Response:

{
  "id": "ticket-id",
  "ticketNumber": 1001,
  "assignedAgent": {
    "id": "agent-id",
    "name": "Sara Ahmed"
  }
}

Do not allow the public/client request to override automatic assignment unless the caller has the appropriate permission.

Assignment Service

Create/reuse a backend service such as:

ITicketAssignmentService

Conceptual responsibility:

FindEligibleAgent(ticket)
Assign(ticket, agent)

Keep assignment logic outside the Vue application.

The exact service structure should follow the SDD.

Configuration

The automatic-assignment setting should be stored in the existing system configuration mechanism if one exists.

Do not create a configuration table unnecessarily.

Possible configuration:

AutomaticAssignmentEnabled = true

Assignment History

When a ticket is automatically assigned, record the assignment in ticket history.

Example:

Ticket Automatically Assigned

Ticket #1001 assigned to Sara Ahmed.

Reason:
Lowest active workload

This is useful for debugging and demonstrates ownership/traceability.

Reuse TKT-009.

Reassignment

Automatic assignment should occur when the ticket is initially created.

Do not automatically move existing tickets between agents every time workload changes.

Example:

Ticket assigned to Sara

Sara workload changes

→ Do NOT automatically move the ticket

Reassignment can be performed manually.

This avoids surprising agents and unnecessary complexity.

Failure Handling

If no agent is available:

Ticket created successfully.

Assignment:
Unassigned

The ticket must still be created.

Do not fail ticket creation simply because no agent is available.

If the assignment service encounters an unexpected database/system error, follow the project's transaction/error strategy.

Do not silently hide assignment failures.

Authorization

Automatic assignment is a backend business process.

Manual assignment requires appropriate permission.

Recommended:

Agent
  → Can view assigned tickets

Manager/Admin
  → Can manually assign/reassign

Follow the existing authorization model.

Security

Never allow an unprivileged client to force:

assignedAgentId = another-user

The backend must validate assignment permissions.

UI

The agent does not need a separate automatic-assignment screen.

On the ticket details page, display:

Assigned To

Sara Ahmed

✓ Automatically assigned

If manually assigned:

Assigned To

Ahmed Hassan

Assigned by:
Manager

The exact display can reuse the existing assignment component.

Dashboard

The assigned ticket should automatically appear in the agent's dashboard.

Reuse:

DASH-001

and:

DASH-002

No additional dashboard implementation is required.

Testing

Backend/API Tests

Test:

	Automatic assignment enabled.

	Automatic assignment disabled.

	Eligible agent selected.

	Department filtering works.

	Unavailable agents excluded.

	Inactive agents excluded.

	Non-agent users excluded.

	Lowest workload selected.

	Tie-breaking works.

	No available agent leaves ticket unassigned.

	Assignment occurs during ticket creation.

	Manual assignment still works.

	Unauthorized assignment rejected.

	Assignment history created.

	Closed/resolved tickets excluded from workload.

Frontend Tests

Test:

	Assigned agent displayed.

	Unassigned state displayed.

	Automatic assignment indicator displayed where applicable.

	Existing manual assignment UI continues to work.

Manual Verification

	Create several agents.

	Set them as available.

	Give Agent A 2 active tickets.

	Give Agent B 5 active tickets.

	Create a new ticket.

	Verify Agent A receives it.

	Make Agent A unavailable.

	Create another ticket.

	Verify Agent B receives it.

	Make all agents unavailable.

	Create another ticket.

	Verify it remains unassigned.

	Login as Manager.

	Manually assign the ticket.

	Verify assignment history.

Edge Cases

Handle:

	No agents.

	No agents in department.

	All agents unavailable.

	Inactive agents.

	Agent with many tickets.

	Equal workload.

	Ticket without department.

	Automatic assignment disabled.

	Database failure.

	Assignment failure.

	Agent becomes unavailable during assignment.

	Manual reassignment.

	Unauthorized assignment.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read TKT-003.

	Read TKT-005.

	Read TKT-009.

	Read DASH-001.

	Inspect the existing User/Role model.

	Inspect the Department model if already implemented.

	Inspect existing ticket assignment logic.

	Reuse existing ticket statuses.

	Implement assignment on the backend.

	Use database-level workload counting.

	Keep the assignment algorithm deterministic.

	Do not create a rules engine.

	Do not implement AI assignment.

	Do not automatically reassign existing tickets.

	Reuse existing manual assignment UI.

	Add backend and frontend tests.

	Run relevant tests.

	Review authorization carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Automatic assignment can be enabled/disabled.

	New tickets can be automatically assigned.

	Only eligible agents are considered.

	Inactive agents are excluded.

	Unavailable agents are excluded.

	Department matching is respected where departments exist.

	Lowest active workload is preferred.

	Tie-breaking is deterministic.

	Tickets remain unassigned when no agent is available.

	Ticket creation does not fail solely because no agent is available.

	Assignment occurs server-side.

	Clients cannot force unauthorized assignment.

	Assignment is recorded in ticket history.

	Existing manual assignment remains available.

	Assigned tickets appear in the agent dashboard.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Automatic assignment service implemented.

	Assignment configuration implemented.

	Workload calculation implemented.

	Department/availability filtering implemented.

	Ticket creation integration implemented.

	Assignment history implemented.

	Manual assignment remains functional.

	Authorization verified.

	PostgreSQL queries optimized appropriately.

	Backend tests pass.

	Frontend tests pass.

	Manual assignment scenarios verified.

	No complex routing engine introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/sla-automation/CRM-62/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `sla-automation`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-62` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
SLA-003 — Automatic Ticket Assignment
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support manager, I want new tickets to be automatically assigned to an appropriate support agent so that tickets are handled quickly without requiring manual assignment for every request.

Objective

Implement simple rule-based automatic ticket assignment.

When a new ticket is created, the system should optionally select an available agent based on:

	Department

	Agent availability

	Current workload

The assignment must happen on the backend.

Scope

This story covers:

	Automatic assignment configuration

	Agent availability

	Basic workload calculation

	Department matching

	Assignment during ticket creation

	Assignment API/service

	Manual assignment fallback

	Validation

	Authorization

	Tests

This story does not cover:

	AI-based assignment

	Skill-based routing

	Complex routing rules

	Predictive workload

	Geographic routing

	Round-robin scheduling engine

	External workforce systems

Recommended MVP Rule

Use this simple priority:

1. Match ticket department
2. Find active agents
3. Prefer available agents
4. Select the agent with the fewest active tickets
5. If no agent is available, leave ticket unassigned

This is enough to demonstrate automatic assignment without building a complicated rules engine.

Assignment Flow

Create Ticket
      ↓
Determine Department
      ↓
Automatic Assignment Enabled?
      ↓
Find Eligible Agents
      ↓
Filter Active/Available
      ↓
Calculate Workload
      ↓
Select Lowest Workload
      ↓
Assign Ticket

If no eligible agent exists:

Create Ticket
      ↓
No available agent
      ↓
Ticket remains Unassigned
      ↓
Manager can manually assign

Agent Eligibility

An agent should be eligible when:

	User is active.

	User has Agent role/permission.

	User belongs to the ticket department.

	User is available.

	User has permission to handle the ticket.

Do not rely on frontend-provided agent IDs.

Agent Availability

For the MVP, availability can be a simple field:

Available
Unavailable

Example:

User
Name: Sara Ahmed
Role: Agent
Department: Support
Availability: Available

Do not implement:

	Calendar schedules

	Shift management

	Vacation management

	Presence detection

	WebSocket presence

Agent Workload

Workload can be calculated using active assigned tickets.

Example:

Agent A → 3 active tickets
Agent B → 8 active tickets
Agent C → 5 active tickets

Selected:
Agent A

Active tickets should exclude statuses such as:

	Resolved

	Closed

Use the existing ticket status model.

Do not create another ticket-status definition.

Workload Query

Prefer database-level counting.

Example concept:

SELECT Agent,
       COUNT(ActiveTickets)
FROM Tickets
WHERE AssignedAgent = Agent
GROUP BY Agent

Do not retrieve every ticket into .NET and calculate workload in memory.

Tie Breaking

If multiple agents have the same workload:

Agent A → 3
Agent B → 3
Agent C → 5

Use a deterministic tie-breaker.

Recommended:

or a stable user ID/order.

Do not introduce randomness.

Department Matching

Tickets may belong to a department.

Example:

Ticket:
Department = Technical Support

Eligible:
Technical Support Agents

Do not assign a Technical Support ticket to a Sales agent unless explicitly allowed by the SDD.

If the project does not yet have departments implemented, automatic department matching may be prepared for the existing department model.

Do not create a complex department system inside this story.

Assignment Configuration

Recommended simple administration setting:

Automatic Assignment

[✓] Enable automatic assignment

Optional:

Assignment Strategy

[ Lowest Workload ▼ ]

For the 2-day MVP, only Lowest Workload is required.

Do not create a configurable rules engine.

Ticket Creation

When a ticket is created:

Auto Assignment Enabled
       ↓
Find Agent
       ↓
Assign

The assignment should happen within the backend ticket creation workflow.

Do not make the Vue application call:

Create Ticket
      ↓
Assign Ticket

as two unrelated operations if the assignment is supposed to be automatic.

Manual Assignment

If automatic assignment fails or is disabled, authorized users can manually assign the ticket.

Reuse the existing assignment functionality from TKT-005.

Do not create another assignment UI.

API

The existing ticket creation endpoint should trigger automatic assignment.

Example:

POST /api/tickets

Request:

{
  "subject": "Unable to login",
  "description": "I cannot login.",
  "priority": "High",
  "departmentId": "department-id"
}

The backend determines the assigned agent.

Response:

{
  "id": "ticket-id",
  "ticketNumber": 1001,
  "assignedAgent": {
    "id": "agent-id",
    "name": "Sara Ahmed"
  }
}

Do not allow the public/client request to override automatic assignment unless the caller has the appropriate permission.

Assignment Service

Create/reuse a backend service such as:

ITicketAssignmentService

Conceptual responsibility:

FindEligibleAgent(ticket)
Assign(ticket, agent)

Keep assignment logic outside the Vue application.

The exact service structure should follow the SDD.

Configuration

The automatic-assignment setting should be stored in the existing system configuration mechanism if one exists.

Do not create a configuration table unnecessarily.

Possible configuration:

AutomaticAssignmentEnabled = true

Assignment History

When a ticket is automatically assigned, record the assignment in ticket history.

Example:

Ticket Automatically Assigned

Ticket #1001 assigned to Sara Ahmed.

Reason:
Lowest active workload

This is useful for debugging and demonstrates ownership/traceability.

Reuse TKT-009.

Reassignment

Automatic assignment should occur when the ticket is initially created.

Do not automatically move existing tickets between agents every time workload changes.

Example:

Ticket assigned to Sara

Sara workload changes

→ Do NOT automatically move the ticket

Reassignment can be performed manually.

This avoids surprising agents and unnecessary complexity.

Failure Handling

If no agent is available:

Ticket created successfully.

Assignment:
Unassigned

The ticket must still be created.

Do not fail ticket creation simply because no agent is available.

If the assignment service encounters an unexpected database/system error, follow the project's transaction/error strategy.

Do not silently hide assignment failures.

Authorization

Automatic assignment is a backend business process.

Manual assignment requires appropriate permission.

Recommended:

Agent
  → Can view assigned tickets

Manager/Admin
  → Can manually assign/reassign

Follow the existing authorization model.

Security

Never allow an unprivileged client to force:

assignedAgentId = another-user

The backend must validate assignment permissions.

UI

The agent does not need a separate automatic-assignment screen.

On the ticket details page, display:

Assigned To

Sara Ahmed

✓ Automatically assigned

If manually assigned:

Assigned To

Ahmed Hassan

Assigned by:
Manager

The exact display can reuse the existing assignment component.

Dashboard

The assigned ticket should automatically appear in the agent's dashboard.

Reuse:

DASH-001

and:

DASH-002

No additional dashboard implementation is required.

Testing

Backend/API Tests

Test:

	Automatic assignment enabled.

	Automatic assignment disabled.

	Eligible agent selected.

	Department filtering works.

	Unavailable agents excluded.

	Inactive agents excluded.

	Non-agent users excluded.

	Lowest workload selected.

	Tie-breaking works.

	No available agent leaves ticket unassigned.

	Assignment occurs during ticket creation.

	Manual assignment still works.

	Unauthorized assignment rejected.

	Assignment history created.

	Closed/resolved tickets excluded from workload.

Frontend Tests

Test:

	Assigned agent displayed.

	Unassigned state displayed.

	Automatic assignment indicator displayed where applicable.

	Existing manual assignment UI continues to work.

Manual Verification

	Create several agents.

	Set them as available.

	Give Agent A 2 active tickets.

	Give Agent B 5 active tickets.

	Create a new ticket.

	Verify Agent A receives it.

	Make Agent A unavailable.

	Create another ticket.

	Verify Agent B receives it.

	Make all agents unavailable.

	Create another ticket.

	Verify it remains unassigned.

	Login as Manager.

	Manually assign the ticket.

	Verify assignment history.

Edge Cases

Handle:

	No agents.

	No agents in department.

	All agents unavailable.

	Inactive agents.

	Agent with many tickets.

	Equal workload.

	Ticket without department.

	Automatic assignment disabled.

	Database failure.

	Assignment failure.

	Agent becomes unavailable during assignment.

	Manual reassignment.

	Unauthorized assignment.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read TKT-003.

	Read TKT-005.

	Read TKT-009.

	Read DASH-001.

	Inspect the existing User/Role model.

	Inspect the Department model if already implemented.

	Inspect existing ticket assignment logic.

	Reuse existing ticket statuses.

	Implement assignment on the backend.

	Use database-level workload counting.

	Keep the assignment algorithm deterministic.

	Do not create a rules engine.

	Do not implement AI assignment.

	Do not automatically reassign existing tickets.

	Reuse existing manual assignment UI.

	Add backend and frontend tests.

	Run relevant tests.

	Review authorization carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Automatic assignment can be enabled/disabled.

	New tickets can be automatically assigned.

	Only eligible agents are considered.

	Inactive agents are excluded.

	Unavailable agents are excluded.

	Department matching is respected where departments exist.

	Lowest active workload is preferred.

	Tie-breaking is deterministic.

	Tickets remain unassigned when no agent is available.

	Ticket creation does not fail solely because no agent is available.

	Assignment occurs server-side.

	Clients cannot force unauthorized assignment.

	Assignment is recorded in ticket history.

	Existing manual assignment remains available.

	Assigned tickets appear in the agent dashboard.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Automatic assignment service implemented.

	Assignment configuration implemented.

	Workload calculation implemented.

	Department/availability filtering implemented.

	Ticket creation integration implemented.

	Assignment history implemented.

	Manual assignment remains functional.

	Authorization verified.

	PostgreSQL queries optimized appropriately.

	Backend tests pass.

	Frontend tests pass.

	Manual assignment scenarios verified.

	No complex routing engine introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.
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
