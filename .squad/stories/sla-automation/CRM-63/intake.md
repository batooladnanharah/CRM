> **Fetched from jira:** [CRM-63](https://batooladnanharah.atlassian.net/browse/CRM-63)  
> *Fetched 2026-08-27T16:13:22.936Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** SLA-004 — Escalation Rules & Notifications  
**Type:** Task  
**Status:** In Progress

### Description

User Story

As a support manager, I want tickets approaching or exceeding their SLA to trigger escalation actions so that critical support issues receive attention before or after an SLA breach.

Objective

Create simple SLA escalation rules that can trigger actions when a ticket becomes:

	At Risk

	Breached

The MVP should support:

	Escalation configuration

	Assignment to manager/team

	In-app notification

	Escalation history

Scope

This story covers:

	Escalation rules

	SLA-based triggers

	Notification model

	In-app notifications

	Escalation history

	Basic management UI

	Background processing integration

	Authorization

	Tests

This story does not cover:

	Email notifications

	SMS notifications

	WhatsApp notifications

	Push notifications

	Complex workflow engine

	Multi-level escalation chains

	Custom scripting

	AI-based escalation

Those can be future enhancements.

Recommended MVP

Use two escalation triggers:

AtRisk
Breached

Example:

AtRisk
   ↓
Notify assigned agent

Breached
   ↓
Notify assigned agent
   ↓
Notify manager

Keep the rules deterministic.

Escalation Rule

Suggested model:

EscalationRule

Id
Name
Trigger
IsActive
NotifyAgent
NotifyManager
CreatedAt
UpdatedAt

Trigger values:

AtRisk
Breached

Do not create a generic workflow engine.

Example Rules

Rule:
SLA At Risk

Trigger:
AtRisk

Action:
Notify assigned agent

Rule:
SLA Breached

Trigger:
Breached

Action:
Notify assigned agent
Notify manager

UI

Management page:

SLA Escalation Rules

[ + Create Rule ]

┌─────────────────────┬───────────┬──────────────────────┬─────────┐
│ Rule                │ Trigger   │ Actions              │ Status  │
├─────────────────────┼───────────┼──────────────────────┼─────────┤
│ SLA At Risk         │ At Risk   │ Agent notification   │ Active  │
│ SLA Breached        │ Breached  │ Agent + Manager      │ Active  │
└─────────────────────┴───────────┴──────────────────────┴─────────┘

Create Rule

Create Escalation Rule

Name *
[ SLA Breached ]

Trigger *
[ Breached ▼ ]

Actions

[x] Notify assigned agent
[x] Notify manager

Status
[x] Active

[Cancel] [Save]

Keep this simple.

Notification

Create a basic in-app notification model:

Notification

Id
UserId
Type
Title
Message
IsRead
CreatedAt

Example:

Title:
SLA Breached

Message:
Ticket #1001 has exceeded its resolution SLA.

Notification UI

Add a notification icon to the main application layout.

Example:

┌────────────────────────────────────────────┐
│ CRM                         🔔 3    Sara ▼ │
└────────────────────────────────────────────┘

Clicking it:

Notifications

● Ticket #1001 has breached its SLA.
  2 minutes ago

● Ticket #1008 is approaching its SLA.
  10 minutes ago

○ Ticket #0998 was assigned to you.
  1 hour ago

[Mark all as read]

Notification Behavior

When an escalation occurs:

SLA Status Changes
       ↓
Evaluate Rules
       ↓
Create Notification
       ↓
Notification appears in UI

Do not require real-time delivery.

The UI may refresh notifications periodically or when the user opens the notification menu.

Duplicate Notifications

The system must avoid repeatedly notifying users for the same escalation event.

Example:

Ticket becomes AtRisk
      ↓
Notify agent

Background worker runs again
      ↓
Do NOT notify agent again

Track escalation execution.

Suggested model:

EscalationEvent

Id
TicketId
RuleId
Trigger
ExecutedAt

Add a uniqueness constraint where appropriate.

Background Processing

Reuse the background processing from SLA-002.

Do not create another background worker.

Preferred flow:

SLA Background Worker
        ↓
Detect AtRisk/Breached
        ↓
Evaluate Escalation Rules
        ↓
Create Escalation Event
        ↓
Create Notifications

This keeps SLA processing centralized.

At-Risk Escalation

When a ticket changes to:

AtRisk

and an active AtRisk rule exists:

Notify assigned agent

Example:

Ticket #1001

SLA:
Resolution — At Risk

Notification:
"Ticket #1001 is approaching its resolution SLA."

Breach Escalation

When a ticket changes to:

Breached

and an active Breached rule exists:

Notify assigned agent
Notify manager

Example:

Ticket #1001

SLA:
Resolution — Breached

Notifications:

Agent:
"Ticket #1001 has exceeded its resolution SLA."

Manager:
"Ticket #1001 assigned to Sara Ahmed has breached its SLA."

Manager Resolution

The manager should be determined using the existing organizational structure.

Preferred order:

Ticket Department
      ↓
Department Manager

If the project does not yet have department managers, use a configured manager/admin role.

Do not build a complex management hierarchy.

Assignment Interaction

If a ticket is unassigned:

SLA Breached
    ↓
No assigned agent
    ↓
Notify manager

Do not fail the escalation.

Resolved Tickets

Once an SLA objective is completed, no new escalation should be generated for that objective.

Example:

Resolution = Completed

Background worker
      ↓
No resolution breach escalation

API

List Rules

GET /api/sla/escalation-rules

Create Rule

POST /api/sla/escalation-rules

Example:

{
  "name": "SLA Breached",
  "trigger": "Breached",
  "notifyAgent": true,
  "notifyManager": true,
  "isActive": true
}

Update

{{PUT /api/sla/escalation-rules/

{id}}}

h3. Activate/Deactivate

Follow existing API conventions.

h2. Notifications API

h3. Get Notifications

GET /api/notifications

Example:

{
  "items": [
    {
      "id": "notification-id",
      "title": "SLA Breached",
      "message": "Ticket #1001 has exceeded its resolution SLA.",
      "isRead": false,
      "createdAt": "2026-08-24T12:00:00Z"
    }
  ],
  "unreadCount": 1
}

h3. Mark Read

{{PATCH /api/notifications/{id}
/read}}

Mark All Read

Optional:

PATCH /api/notifications/read-all

Authorization

Only authorized management users can:

	Create escalation rules

	Edit escalation rules

	Activate/deactivate rules

Recommended:

Admin
Manager
    ↓
Manage escalation rules

Agent
    ↓
Receive notifications

Agents should not modify escalation configuration.

Security

A user must only be able to retrieve their own notifications.

Example:

GET /api/notifications

must use the authenticated user identity.

Do not allow:

GET /api/notifications?userId=another-user

to expose another user's notifications.

Notification Privacy

Notifications may contain ticket information.

Only users authorized to see the related ticket should receive the notification.

The backend must validate this.

Ticket UI

Display escalation information where useful.

Example:

SLA

Resolution
⚠ Breached

Escalated
✓ Agent notified
✓ Manager notified

Do not overload the ticket UI.

Ticket History

Record escalation events.

Example:

SLA Escalation

Ticket exceeded its resolution SLA.

Agent notified.
Manager notified.

24 Aug 2026 18:02

Reuse TKT-009.

Error Handling

If notification creation fails:

	Do not change the SLA status incorrectly.

	Log the failure.

	Retry where appropriate.

	Keep ticket operations working.

Do not expose technical details to the user.

Example:

Notification could not be delivered.

For the MVP, in-app notification creation is local database work and should be reliable.

Testing

Backend/API Tests

Test:

	Authorized manager can create escalation rule.

	Agent cannot create escalation rule.

	Rule validation works.

	AtRisk rule triggers.

	Breached rule triggers.

	Agent notification created.

	Manager notification created.

	Unassigned ticket escalates to manager.

	Duplicate escalation is prevented.

	Inactive rule does not trigger.

	Completed SLA does not trigger escalation.

	User only sees own notifications.

	Mark notification as read works.

Background Worker Tests

Test:

	AtRisk triggers rule evaluation.

	Breached triggers rule evaluation.

	Existing escalation event is not repeated.

	Multiple rules behave correctly.

	Inactive rules are ignored.

	Worker handles database errors.

	Notification failure does not break SLA processing.

Frontend Tests

Test:

	Escalation rule list.

	Create rule.

	Edit rule.

	Activate/deactivate rule.

	Notification icon.

	Unread count.

	Notification list.

	Mark as read.

	Empty notification state.

	Loading state.

	Error state.

Manual Verification

	Create an AtRisk escalation rule.

	Create a ticket with an SLA.

	Move/simulate ticket to AtRisk.

	Verify assigned agent receives notification.

	Verify escalation history.

	Create a Breached rule.

	Move/simulate ticket to Breached.

	Verify agent notification.

	Verify manager notification.

	Run the background worker again.

	Verify duplicate notifications are not created.

	Mark notification as read.

	Verify unread count decreases.

	Deactivate the rule.

	Verify it no longer triggers.

Edge Cases

Handle:

	No escalation rules.

	Inactive rule.

	No assigned agent.

	No manager.

	Duplicate background processing.

	Multiple rules for same trigger.

	Notification already exists.

	User disabled.

	Ticket already resolved.

	SLA already completed.

	Database failure.

	Notification creation failure.

	Unauthorized management user.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read TKT-009.

	Read SLA-001.

	Read the edited SLA-002.

	Inspect the existing background worker.

	Reuse the SLA worker.

	Do not create another SLA processing system.

	Implement only AtRisk and Breached triggers.

	Implement simple in-app notifications.

	Prevent duplicate escalation events.

	Reuse existing ticket authorization.

	Do not implement email/SMS/WhatsApp notifications.

	Do not implement a workflow engine.

	Do not implement real-time notifications.

	Add backend, worker, and frontend tests.

	Run relevant tests.

	Review authorization and duplicate-event handling.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized managers can create escalation rules.

	Authorized managers can edit escalation rules.

	Rules can be activated/deactivated.

	AtRisk trigger is supported.

	Breached trigger is supported.

	Assigned agent can be notified.

	Manager can be notified.

	Unassigned tickets can escalate to management.

	Duplicate escalation notifications are prevented.

	Escalation events are recorded.

	Notifications are stored.

	Users can view their notifications.

	Users can mark notifications as read.

	Users cannot view another user's notifications.

	Escalation integrates with the SLA background worker.

	Completed SLA objectives do not trigger new escalation.

	Ticket history records escalation.

	Relevant backend tests pass.

	Background worker tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Escalation rule model implemented.

	Escalation rule CRUD implemented.

	Notification model implemented.

	Notification API implemented.

	Notification UI implemented.

	SLA worker integration implemented.

	Duplicate escalation protection implemented.

	Ticket history integration implemented.

	Authorization implemented.

	Backend tests pass.

	Worker tests pass.

	Frontend tests pass.

	Manual escalation flow verified.

	No workflow engine introduced.

	No external notification provider introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/sla-automation/CRM-63/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `sla-automation`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-63` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Progress`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
SLA-004 — Escalation Rules & Notifications
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support manager, I want tickets approaching or exceeding their SLA to trigger escalation actions so that critical support issues receive attention before or after an SLA breach.

Objective

Create simple SLA escalation rules that can trigger actions when a ticket becomes:

	At Risk

	Breached

The MVP should support:

	Escalation configuration

	Assignment to manager/team

	In-app notification

	Escalation history

Scope

This story covers:

	Escalation rules

	SLA-based triggers

	Notification model

	In-app notifications

	Escalation history

	Basic management UI

	Background processing integration

	Authorization

	Tests

This story does not cover:

	Email notifications

	SMS notifications

	WhatsApp notifications

	Push notifications

	Complex workflow engine

	Multi-level escalation chains

	Custom scripting

	AI-based escalation

Those can be future enhancements.

Recommended MVP

Use two escalation triggers:

AtRisk
Breached

Example:

AtRisk
   ↓
Notify assigned agent

Breached
   ↓
Notify assigned agent
   ↓
Notify manager

Keep the rules deterministic.

Escalation Rule

Suggested model:

EscalationRule

Id
Name
Trigger
IsActive
NotifyAgent
NotifyManager
CreatedAt
UpdatedAt

Trigger values:

AtRisk
Breached

Do not create a generic workflow engine.

Example Rules

Rule:
SLA At Risk

Trigger:
AtRisk

Action:
Notify assigned agent

Rule:
SLA Breached

Trigger:
Breached

Action:
Notify assigned agent
Notify manager

UI

Management page:

SLA Escalation Rules

[ + Create Rule ]

┌─────────────────────┬───────────┬──────────────────────┬─────────┐
│ Rule                │ Trigger   │ Actions              │ Status  │
├─────────────────────┼───────────┼──────────────────────┼─────────┤
│ SLA At Risk         │ At Risk   │ Agent notification   │ Active  │
│ SLA Breached        │ Breached  │ Agent + Manager      │ Active  │
└─────────────────────┴───────────┴──────────────────────┴─────────┘

Create Rule

Create Escalation Rule

Name *
[ SLA Breached ]

Trigger *
[ Breached ▼ ]

Actions

[x] Notify assigned agent
[x] Notify manager

Status
[x] Active

[Cancel] [Save]

Keep this simple.

Notification

Create a basic in-app notification model:

Notification

Id
UserId
Type
Title
Message
IsRead
CreatedAt

Example:

Title:
SLA Breached

Message:
Ticket #1001 has exceeded its resolution SLA.

Notification UI

Add a notification icon to the main application layout.

Example:

┌────────────────────────────────────────────┐
│ CRM                         🔔 3    Sara ▼ │
└────────────────────────────────────────────┘

Clicking it:

Notifications

● Ticket #1001 has breached its SLA.
  2 minutes ago

● Ticket #1008 is approaching its SLA.
  10 minutes ago

○ Ticket #0998 was assigned to you.
  1 hour ago

[Mark all as read]

Notification Behavior

When an escalation occurs:

SLA Status Changes
       ↓
Evaluate Rules
       ↓
Create Notification
       ↓
Notification appears in UI

Do not require real-time delivery.

The UI may refresh notifications periodically or when the user opens the notification menu.

Duplicate Notifications

The system must avoid repeatedly notifying users for the same escalation event.

Example:

Ticket becomes AtRisk
      ↓
Notify agent

Background worker runs again
      ↓
Do NOT notify agent again

Track escalation execution.

Suggested model:

EscalationEvent

Id
TicketId
RuleId
Trigger
ExecutedAt

Add a uniqueness constraint where appropriate.

Background Processing

Reuse the background processing from SLA-002.

Do not create another background worker.

Preferred flow:

SLA Background Worker
        ↓
Detect AtRisk/Breached
        ↓
Evaluate Escalation Rules
        ↓
Create Escalation Event
        ↓
Create Notifications

This keeps SLA processing centralized.

At-Risk Escalation

When a ticket changes to:

AtRisk

and an active AtRisk rule exists:

Notify assigned agent

Example:

Ticket #1001

SLA:
Resolution — At Risk

Notification:
"Ticket #1001 is approaching its resolution SLA."

Breach Escalation

When a ticket changes to:

Breached

and an active Breached rule exists:

Notify assigned agent
Notify manager

Example:

Ticket #1001

SLA:
Resolution — Breached

Notifications:

Agent:
"Ticket #1001 has exceeded its resolution SLA."

Manager:
"Ticket #1001 assigned to Sara Ahmed has breached its SLA."

Manager Resolution

The manager should be determined using the existing organizational structure.

Preferred order:

Ticket Department
      ↓
Department Manager

If the project does not yet have department managers, use a configured manager/admin role.

Do not build a complex management hierarchy.

Assignment Interaction

If a ticket is unassigned:

SLA Breached
    ↓
No assigned agent
    ↓
Notify manager

Do not fail the escalation.

Resolved Tickets

Once an SLA objective is completed, no new escalation should be generated for that objective.

Example:

Resolution = Completed

Background worker
      ↓
No resolution breach escalation

API

List Rules

GET /api/sla/escalation-rules

Create Rule

POST /api/sla/escalation-rules

Example:

{
  "name": "SLA Breached",
  "trigger": "Breached",
  "notifyAgent": true,
  "notifyManager": true,
  "isActive": true
}

Update

{ {PUT /api/sla/escalation-rules/

{id}}}

h3. Activate/Deactivate

Follow existing API conventions.

h2. Notifications API

h3. Get Notifications

GET /api/notifications

Example:

{
  "items": [
    {
      "id": "notification-id",
      "title": "SLA Breached",
      "message": "Ticket #1001 has exceeded its resolution SLA.",
      "isRead": false,
      "createdAt": "2026-08-24T12:00:00Z"
    }
  ],
  "unreadCount": 1
}

h3. Mark Read

{ {PATCH /api/notifications/{id}
/read}}

Mark All Read

Optional:

PATCH /api/notifications/read-all

Authorization

Only authorized management users can:

	Create escalation rules

	Edit escalation rules

	Activate/deactivate rules

Recommended:

Admin
Manager
    ↓
Manage escalation rules

Agent
    ↓
Receive notifications

Agents should not modify escalation configuration.

Security

A user must only be able to retrieve their own notifications.

Example:

GET /api/notifications

must use the authenticated user identity.

Do not allow:

GET /api/notifications?userId=another-user

to expose another user's notifications.

Notification Privacy

Notifications may contain ticket information.

Only users authorized to see the related ticket should receive the notification.

The backend must validate this.

Ticket UI

Display escalation information where useful.

Example:

SLA

Resolution
⚠ Breached

Escalated
✓ Agent notified
✓ Manager notified

Do not overload the ticket UI.

Ticket History

Record escalation events.

Example:

SLA Escalation

Ticket exceeded its resolution SLA.

Agent notified.
Manager notified.

24 Aug 2026 18:02

Reuse TKT-009.

Error Handling

If notification creation fails:

	Do not change the SLA status incorrectly.

	Log the failure.

	Retry where appropriate.

	Keep ticket operations working.

Do not expose technical details to the user.

Example:

Notification could not be delivered.

For the MVP, in-app notification creation is local database work and should be reliable.

Testing

Backend/API Tests

Test:

	Authorized manager can create escalation rule.

	Agent cannot create escalation rule.

	Rule validation works.

	AtRisk rule triggers.

	Breached rule triggers.

	Agent notification created.

	Manager notification created.

	Unassigned ticket escalates to manager.

	Duplicate escalation is prevented.

	Inactive rule does not trigger.

	Completed SLA does not trigger escalation.

	User only sees own notifications.

	Mark notification as read works.

Background Worker Tests

Test:

	AtRisk triggers rule evaluation.

	Breached triggers rule evaluation.

	Existing escalation event is not repeated.

	Multiple rules behave correctly.

	Inactive rules are ignored.

	Worker handles database errors.

	Notification failure does not break SLA processing.

Frontend Tests

Test:

	Escalation rule list.

	Create rule.

	Edit rule.

	Activate/deactivate rule.

	Notification icon.

	Unread count.

	Notification list.

	Mark as read.

	Empty notification state.

	Loading state.

	Error state.

Manual Verification

	Create an AtRisk escalation rule.

	Create a ticket with an SLA.

	Move/simulate ticket to AtRisk.

	Verify assigned agent receives notification.

	Verify escalation history.

	Create a Breached rule.

	Move/simulate ticket to Breached.

	Verify agent notification.

	Verify manager notification.

	Run the background worker again.

	Verify duplicate notifications are not created.

	Mark notification as read.

	Verify unread count decreases.

	Deactivate the rule.

	Verify it no longer triggers.

Edge Cases

Handle:

	No escalation rules.

	Inactive rule.

	No assigned agent.

	No manager.

	Duplicate background processing.

	Multiple rules for same trigger.

	Notification already exists.

	User disabled.

	Ticket already resolved.

	SLA already completed.

	Database failure.

	Notification creation failure.

	Unauthorized management user.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read TKT-009.

	Read SLA-001.

	Read the edited SLA-002.

	Inspect the existing background worker.

	Reuse the SLA worker.

	Do not create another SLA processing system.

	Implement only AtRisk and Breached triggers.

	Implement simple in-app notifications.

	Prevent duplicate escalation events.

	Reuse existing ticket authorization.

	Do not implement email/SMS/WhatsApp notifications.

	Do not implement a workflow engine.

	Do not implement real-time notifications.

	Add backend, worker, and frontend tests.

	Run relevant tests.

	Review authorization and duplicate-event handling.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized managers can create escalation rules.

	Authorized managers can edit escalation rules.

	Rules can be activated/deactivated.

	AtRisk trigger is supported.

	Breached trigger is supported.

	Assigned agent can be notified.

	Manager can be notified.

	Unassigned tickets can escalate to management.

	Duplicate escalation notifications are prevented.

	Escalation events are recorded.

	Notifications are stored.

	Users can view their notifications.

	Users can mark notifications as read.

	Users cannot view another user's notifications.

	Escalation integrates with the SLA background worker.

	Completed SLA objectives do not trigger new escalation.

	Ticket history records escalation.

	Relevant backend tests pass.

	Background worker tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Escalation rule model implemented.

	Escalation rule CRUD implemented.

	Notification model implemented.

	Notification API implemented.

	Notification UI implemented.

	SLA worker integration implemented.

	Duplicate escalation protection implemented.

	Ticket history integration implemented.

	Authorization implemented.

	Backend tests pass.

	Worker tests pass.

	Frontend tests pass.

	Manual escalation flow verified.

	No workflow engine introduced.

	No external notification provider introduced.

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
