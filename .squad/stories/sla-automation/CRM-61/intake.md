> **Fetched from jira:** [CRM-61](https://batooladnanharah.atlassian.net/browse/CRM-61)  
> *Fetched 2026-08-30T21:51:44.640Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** SLA-002 — SLA Tracking & Breach Detection  
**Type:** Task  
**Status:** In Review

### Description

User Story

As a support agent, I want the CRM to track response and resolution deadlines for my tickets so that I can identify tickets that are approaching or exceeding their SLA targets.

As a support manager, I want SLA breaches to be detected automatically so that support performance can be monitored and corrective action can be taken.

Objective

Apply the SLA policy from SLA-001 to tickets and calculate:

	First response deadline

	Resolution deadline

	Current SLA status

	Response breach

	Resolution breach

The system should automatically update SLA status based on ticket activity and time.

Scope

This story covers:

	Applying SLA policy to tickets

	Response deadline

	Resolution deadline

	SLA status

	First response detection

	Resolution detection

	SLA breach detection

	Remaining time

	Background processing

	Ticket UI indicators

	Dashboard integration

	API

	Tests

This story does not cover:

	Automatic assignment

	Escalation actions

	Email/SMS notifications

	Complex business calendars

	Holiday calendars

	Pausing/resuming SLA

	Multiple SLA policies per ticket

Those can be implemented later if required.

SLA Lifecycle

Recommended flow:

Ticket Created
      ↓
Select Active SLA Policy
      ↓
Determine Priority
      ↓
Calculate Targets
      ↓
Set Response Deadline
      ↓
Set Resolution Deadline
      ↓
Track Ticket Activity
      ↓
SLA Status

Ticket SLA Data

The ticket should have access to its SLA information.

Recommended model:

TicketSla

Id
TicketId
SlaPolicyId
FirstResponseDueAt
ResolutionDueAt
FirstRespondedAt
ResolvedAt
ResponseStatus
ResolutionStatus
CreatedAt
UpdatedAt

The exact model should follow the existing architecture.

Why Snapshot the SLA

When an SLA policy changes later, an existing ticket should not unexpectedly change its deadline.

Example:

Ticket #1001 created

SLA:
Response = 30 minutes
Resolution = 8 hours

       ↓

Manager changes policy

Response = 15 minutes
Resolution = 4 hours

       ↓

Ticket #1001
Still uses its original SLA targets

The AI should implement SLA assignment as a snapshot or equivalent mechanism.

Applying the SLA

When a ticket is created:

Ticket Priority
       ↓
Active SLA Policy
       ↓
Priority Target
       ↓
Ticket SLA

Example:

Created:
10:00

Priority:
High

First Response:
30 minutes

Resolution:
8 hours

Response Due:
10:30

Resolution Due:
18:00

First Response

A ticket is considered responded when an authorized agent sends the first customer-visible response.

Important:

An internal note must not count as a first response.

Example:

Customer message
       ↓
Internal Note
       ↓
Internal Note
       ↓
Agent Reply
       ↓
First Response

The Agent Reply is the first response.

Resolution

A ticket is considered resolved when it reaches the existing resolved/closed state according to the Ticket Management rules.

Do not create another ticket status system.

Reuse the existing ticket status.

SLA Status

For MVP, use:

OnTrack
AtRisk
Breached
Completed

On Track

Deadline is sufficiently far away.

At Risk

Deadline is approaching.

Breached

Deadline has passed.

Completed

The relevant SLA objective has been completed.

At-Risk Threshold

Use a simple configurable threshold.

Recommended MVP:

AtRisk when remaining time <= 20%

Example:

SLA target = 100 minutes

Remaining:
80 minutes → On Track
20 minutes → At Risk
0 minutes → Breached

The exact threshold should be configurable if the SDD requires it.

Do not build a complex SLA prediction algorithm.

Response vs Resolution Status

Track them separately.

Example:

Response:
Completed

Resolution:
AtRisk

This is important because an agent may have responded but still have a long-running unresolved ticket.

SLA Status API

The ticket API should return SLA information.

Example:

{
  "sla": {
    "response": {
      "status": "Completed",
      "dueAt": "2026-08-24T10:30:00Z",
      "respondedAt": "2026-08-24T10:20:00Z"
    },
    "resolution": {
      "status": "AtRisk",
      "dueAt": "2026-08-24T18:00:00Z",
      "resolvedAt": null
    }
  }
}

Reuse the existing ticket API where possible.

Do not create multiple redundant SLA endpoints.

Ticket UI

Display SLA information in the ticket.

Example:

SLA

Response
✓ Completed

Resolution
⚠ At Risk
2h 15m remaining

Breached:

Resolution
⚠ Breached

Do not rely only on colors.

Use:

	Status text

	Icon

	Accessible label

Ticket List

The ticket list should display a compact SLA indicator.

Example:

#1001 | Unable to login | High | Open | ⚠ At Risk

Reuse existing ticket list components.

Do not create another ticket list.

Agent Dashboard

DASH-001 should use the SLA data.

Example:

SLA At Risk
     3

The dashboard count should be based on backend data.

Do not calculate the count from all tickets in Vue.

Background Processing

SLA status must not depend on the user opening the ticket.

Example:

Ticket deadline passes
       ↓
Background process
       ↓
SLA becomes Breached

Use the simplest reliable background mechanism already supported by the .NET application.

Possible approach:

BackgroundService

The exact implementation should follow the SDD.

Do not introduce Hangfire/Quartz/etc. unless the project already uses it or the SDD requires it.

Processing Frequency

For the MVP, checking SLA status every few minutes is sufficient.

Do not implement second-by-second timers.

Example:

Every 1–5 minutes
       ↓
Find active SLA records
       ↓
Update status

The UI can calculate/display remaining time using the stored deadline.

Database Processing

Only process relevant active SLA records.

Do not retrieve every ticket into memory.

Preferred:

PostgreSQL
    ↓
Active SLA records
    ↓
Background worker
    ↓
Update status

Time Handling

Store all timestamps in UTC.

Examples:

CreatedAt
FirstResponseDueAt
ResolutionDueAt
FirstRespondedAt
ResolvedAt

The frontend converts times to the user's locale.

Do not calculate SLA deadlines using browser local time.

Business Hours

For the 2-day MVP, SLA targets use elapsed time.

Example:

8 hours
=
8 elapsed hours

Do not implement:

	Business hours

	Weekends

	Public holidays

	Working calendars

unless explicitly required by the SDD.

Document this as a future enhancement.

Pausing SLA

SLA pause/resume is out of scope for this story.

Do not add complex pause states.

SLA Recalculation

SLA should not be recalculated unnecessarily.

For example:

Ticket Priority changes
       ↓
Should SLA change?

The behavior must be defined by the SDD.

For the MVP, recommend:

	SLA target is determined when the ticket is created.

	Changing priority does not silently rewrite the original SLA.

If priority changes need SLA recalculation, implement it explicitly and record the change.

API

No new endpoint is required if existing ticket APIs can return SLA information.

If needed:

{{GET /api/tickets/

{id}
/sla}}

Example:

{
  "responseStatus": "Completed",
  "responseDueAt": "2026-08-24T10:30:00Z",
  "resolutionStatus": "AtRisk",
  "resolutionDueAt": "2026-08-24T18:00:00Z"
}

Prefer embedding this into the existing ticket response if practical.

Authorization

Use AUTH-003.

Users should only see SLA information for tickets they are authorized to access.

Managers may have access to broader SLA reporting later.

Error Handling

If SLA calculation fails:

	Do not prevent normal ticket operations.

	Log the error.

	Retry background processing.

	Keep the ticket usable.

If SLA data is unavailable in the UI:

SLA information unavailable

Do not show incorrect SLA values.

Testing

Backend/API Tests

Test:

	SLA assigned when ticket created.

	Correct priority target selected.

	Response deadline calculated.

	Resolution deadline calculated.

	First agent response marks response completed.

	Internal note does not complete response SLA.

	Resolution marks resolution completed.

	Response breach detected.

	Resolution breach detected.

	At-risk status calculated.

	Completed status calculated.

	Existing ticket SLA remains stable after policy changes.

	Unauthorized SLA access rejected.

Background Worker Tests

Test:

	Active SLA records are processed.

	At-risk records updated.

	Breached records updated.

	Completed records are ignored.

	Worker handles database errors.

	Worker does not process unlimited records at once.

Frontend Tests

Test:

	SLA displayed on ticket.

	At-risk state displayed.

	Breached state displayed.

	Completed state displayed.

	Remaining time displayed.

	Dashboard SLA count works.

	Loading state works.

	Missing SLA state works.

Manual Verification

	Create an SLA policy.

	Create a High-priority ticket.

	Verify SLA targets are applied.

	Verify response deadline.

	Verify resolution deadline.

	Add an internal note.

	Verify response SLA remains active.

	Send an agent reply.

	Verify response SLA becomes Completed.

	Move ticket toward resolution.

	Verify resolution status.

	Simulate/adjust time to test At Risk.

	Simulate/adjust time to test Breached.

	Verify dashboard count.

	Change SLA policy.

	Verify existing ticket does not unexpectedly change.

Edge Cases

Handle:

	No active SLA policy.

	Missing SLA target for priority.

	Ticket created with unknown priority.

	Ticket created without SLA.

	First response after deadline.

	Resolution after deadline.

	Internal note before response.

	Multiple agent responses.

	Ticket resolved before response.

	SLA policy deactivated.

	Policy changed after ticket creation.

	Database failure.

	Background worker failure.

	Time zone differences.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read TKT-003.

	Read TKT-006.

	Read TKT-009.

	Read SLA-001.

	review 22-story-crm-100.md ,23-story-crm-101.md ,36-story-crm-63.md ,40-story-crm-60.md  plan and reuse the exiting code and enhance  it

	Inspect existing ticket status behavior.

	Inspect existing TicketMessage behavior.

	Reuse the existing priority model.

	Reuse existing ticket services.

	Implement SLA calculation server-side.

	Store timestamps in UTC.

	Use a simple .NET background worker if background processing is not already available.

	Do not introduce a complex job framework unnecessarily.

	Do not implement business hours.

	Do not implement SLA pause/resume.

	Do not implement escalation notifications yet.

	Add backend, worker, and frontend tests.

	Run relevant tests.

	Review time calculations and edge cases carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	New tickets receive an applicable SLA.

	Response target is calculated.

	Resolution target is calculated.

	SLA is associated with the ticket.

	SLA target is stable for an existing ticket.

	First customer-visible agent response completes response SLA.

	Internal notes do not complete response SLA.

	Ticket resolution completes resolution SLA.

	At-risk state is detected.

	Breached state is detected.

	Completed state is detected.

	SLA status is available through the API.

	SLA status is visible in the ticket UI.

	Dashboard can display SLA-at-risk information.

	Background processing updates SLA status.

	Timestamps are stored in UTC.

	Authorization is enforced.

	Relevant backend tests pass.

	Background processing tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Ticket SLA model implemented.

	SLA assignment implemented.

	Response tracking implemented.

	Resolution tracking implemented.

	At-risk detection implemented.

	Breach detection implemented.

	Background processing implemented.

	Ticket UI indicators implemented.

	Dashboard integration implemented.

	Authorization verified.

	UTC time handling verified.

	Backend tests pass.

	Worker tests pass.

	Frontend tests pass.

	Manual SLA lifecycle verified.

	No unnecessary scheduling framework introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/sla-automation/CRM-61/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `sla-automation`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-61` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
SLA-002 — SLA Tracking & Breach Detection
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want the CRM to track response and resolution deadlines for my tickets so that I can identify tickets that are approaching or exceeding their SLA targets.

As a support manager, I want SLA breaches to be detected automatically so that support performance can be monitored and corrective action can be taken.

Objective

Apply the SLA policy from SLA-001 to tickets and calculate:

	First response deadline

	Resolution deadline

	Current SLA status

	Response breach

	Resolution breach

The system should automatically update SLA status based on ticket activity and time.

Scope

This story covers:

	Applying SLA policy to tickets

	Response deadline

	Resolution deadline

	SLA status

	First response detection

	Resolution detection

	SLA breach detection

	Remaining time

	Background processing

	Ticket UI indicators

	Dashboard integration

	API

	Tests

This story does not cover:

	Automatic assignment

	Escalation actions

	Email/SMS notifications

	Complex business calendars

	Holiday calendars

	Pausing/resuming SLA

	Multiple SLA policies per ticket

Those can be implemented later if required.

SLA Lifecycle

Recommended flow:

Ticket Created
      ↓
Select Active SLA Policy
      ↓
Determine Priority
      ↓
Calculate Targets
      ↓
Set Response Deadline
      ↓
Set Resolution Deadline
      ↓
Track Ticket Activity
      ↓
SLA Status

Ticket SLA Data

The ticket should have access to its SLA information.

Recommended model:

TicketSla

Id
TicketId
SlaPolicyId
FirstResponseDueAt
ResolutionDueAt
FirstRespondedAt
ResolvedAt
ResponseStatus
ResolutionStatus
CreatedAt
UpdatedAt

The exact model should follow the existing architecture.

Why Snapshot the SLA

When an SLA policy changes later, an existing ticket should not unexpectedly change its deadline.

Example:

Ticket #1001 created

SLA:
Response = 30 minutes
Resolution = 8 hours

       ↓

Manager changes policy

Response = 15 minutes
Resolution = 4 hours

       ↓

Ticket #1001
Still uses its original SLA targets

The AI should implement SLA assignment as a snapshot or equivalent mechanism.

Applying the SLA

When a ticket is created:

Ticket Priority
       ↓
Active SLA Policy
       ↓
Priority Target
       ↓
Ticket SLA

Example:

Created:
10:00

Priority:
High

First Response:
30 minutes

Resolution:
8 hours

Response Due:
10:30

Resolution Due:
18:00

First Response

A ticket is considered responded when an authorized agent sends the first customer-visible response.

Important:

An internal note must not count as a first response.

Example:

Customer message
       ↓
Internal Note
       ↓
Internal Note
       ↓
Agent Reply
       ↓
First Response

The Agent Reply is the first response.

Resolution

A ticket is considered resolved when it reaches the existing resolved/closed state according to the Ticket Management rules.

Do not create another ticket status system.

Reuse the existing ticket status.

SLA Status

For MVP, use:

OnTrack
AtRisk
Breached
Completed

On Track

Deadline is sufficiently far away.

At Risk

Deadline is approaching.

Breached

Deadline has passed.

Completed

The relevant SLA objective has been completed.

At-Risk Threshold

Use a simple configurable threshold.

Recommended MVP:

AtRisk when remaining time <= 20%

Example:

SLA target = 100 minutes

Remaining:
80 minutes → On Track
20 minutes → At Risk
0 minutes → Breached

The exact threshold should be configurable if the SDD requires it.

Do not build a complex SLA prediction algorithm.

Response vs Resolution Status

Track them separately.

Example:

Response:
Completed

Resolution:
AtRisk

This is important because an agent may have responded but still have a long-running unresolved ticket.

SLA Status API

The ticket API should return SLA information.

Example:

{
  "sla": {
    "response": {
      "status": "Completed",
      "dueAt": "2026-08-24T10:30:00Z",
      "respondedAt": "2026-08-24T10:20:00Z"
    },
    "resolution": {
      "status": "AtRisk",
      "dueAt": "2026-08-24T18:00:00Z",
      "resolvedAt": null
    }
  }
}

Reuse the existing ticket API where possible.

Do not create multiple redundant SLA endpoints.

Ticket UI

Display SLA information in the ticket.

Example:

SLA

Response
✓ Completed

Resolution
⚠ At Risk
2h 15m remaining

Breached:

Resolution
⚠ Breached

Do not rely only on colors.

Use:

	Status text

	Icon

	Accessible label

Ticket List

The ticket list should display a compact SLA indicator.

Example:

#1001 | Unable to login | High | Open | ⚠ At Risk

Reuse existing ticket list components.

Do not create another ticket list.

Agent Dashboard

DASH-001 should use the SLA data.

Example:

SLA At Risk
     3

The dashboard count should be based on backend data.

Do not calculate the count from all tickets in Vue.

Background Processing

SLA status must not depend on the user opening the ticket.

Example:

Ticket deadline passes
       ↓
Background process
       ↓
SLA becomes Breached

Use the simplest reliable background mechanism already supported by the .NET application.

Possible approach:

BackgroundService

The exact implementation should follow the SDD.

Do not introduce Hangfire/Quartz/etc. unless the project already uses it or the SDD requires it.

Processing Frequency

For the MVP, checking SLA status every few minutes is sufficient.

Do not implement second-by-second timers.

Example:

Every 1–5 minutes
       ↓
Find active SLA records
       ↓
Update status

The UI can calculate/display remaining time using the stored deadline.

Database Processing

Only process relevant active SLA records.

Do not retrieve every ticket into memory.

Preferred:

PostgreSQL
    ↓
Active SLA records
    ↓
Background worker
    ↓
Update status

Time Handling

Store all timestamps in UTC.

Examples:

CreatedAt
FirstResponseDueAt
ResolutionDueAt
FirstRespondedAt
ResolvedAt

The frontend converts times to the user's locale.

Do not calculate SLA deadlines using browser local time.

Business Hours

For the 2-day MVP, SLA targets use elapsed time.

Example:

8 hours
=
8 elapsed hours

Do not implement:

	Business hours

	Weekends

	Public holidays

	Working calendars

unless explicitly required by the SDD.

Document this as a future enhancement.

Pausing SLA

SLA pause/resume is out of scope for this story.

Do not add complex pause states.

SLA Recalculation

SLA should not be recalculated unnecessarily.

For example:

Ticket Priority changes
       ↓
Should SLA change?

The behavior must be defined by the SDD.

For the MVP, recommend:

	SLA target is determined when the ticket is created.

	Changing priority does not silently rewrite the original SLA.

If priority changes need SLA recalculation, implement it explicitly and record the change.

API

No new endpoint is required if existing ticket APIs can return SLA information.

If needed:

{ {GET /api/tickets/

{id}
/sla}}

Example:

{
  "responseStatus": "Completed",
  "responseDueAt": "2026-08-24T10:30:00Z",
  "resolutionStatus": "AtRisk",
  "resolutionDueAt": "2026-08-24T18:00:00Z"
}

Prefer embedding this into the existing ticket response if practical.

Authorization

Use AUTH-003.

Users should only see SLA information for tickets they are authorized to access.

Managers may have access to broader SLA reporting later.

Error Handling

If SLA calculation fails:

	Do not prevent normal ticket operations.

	Log the error.

	Retry background processing.

	Keep the ticket usable.

If SLA data is unavailable in the UI:

SLA information unavailable

Do not show incorrect SLA values.

Testing

Backend/API Tests

Test:

	SLA assigned when ticket created.

	Correct priority target selected.

	Response deadline calculated.

	Resolution deadline calculated.

	First agent response marks response completed.

	Internal note does not complete response SLA.

	Resolution marks resolution completed.

	Response breach detected.

	Resolution breach detected.

	At-risk status calculated.

	Completed status calculated.

	Existing ticket SLA remains stable after policy changes.

	Unauthorized SLA access rejected.

Background Worker Tests

Test:

	Active SLA records are processed.

	At-risk records updated.

	Breached records updated.

	Completed records are ignored.

	Worker handles database errors.

	Worker does not process unlimited records at once.

Frontend Tests

Test:

	SLA displayed on ticket.

	At-risk state displayed.

	Breached state displayed.

	Completed state displayed.

	Remaining time displayed.

	Dashboard SLA count works.

	Loading state works.

	Missing SLA state works.

Manual Verification

	Create an SLA policy.

	Create a High-priority ticket.

	Verify SLA targets are applied.

	Verify response deadline.

	Verify resolution deadline.

	Add an internal note.

	Verify response SLA remains active.

	Send an agent reply.

	Verify response SLA becomes Completed.

	Move ticket toward resolution.

	Verify resolution status.

	Simulate/adjust time to test At Risk.

	Simulate/adjust time to test Breached.

	Verify dashboard count.

	Change SLA policy.

	Verify existing ticket does not unexpectedly change.

Edge Cases

Handle:

	No active SLA policy.

	Missing SLA target for priority.

	Ticket created with unknown priority.

	Ticket created without SLA.

	First response after deadline.

	Resolution after deadline.

	Internal note before response.

	Multiple agent responses.

	Ticket resolved before response.

	SLA policy deactivated.

	Policy changed after ticket creation.

	Database failure.

	Background worker failure.

	Time zone differences.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read TKT-003.

	Read TKT-006.

	Read TKT-009.

	Read SLA-001.

	review 22-story-crm-100.md ,23-story-crm-101.md ,36-story-crm-63.md ,40-story-crm-60.md  plan and reuse the exiting code and enhance  it

	Inspect existing ticket status behavior.

	Inspect existing TicketMessage behavior.

	Reuse the existing priority model.

	Reuse existing ticket services.

	Implement SLA calculation server-side.

	Store timestamps in UTC.

	Use a simple .NET background worker if background processing is not already available.

	Do not introduce a complex job framework unnecessarily.

	Do not implement business hours.

	Do not implement SLA pause/resume.

	Do not implement escalation notifications yet.

	Add backend, worker, and frontend tests.

	Run relevant tests.

	Review time calculations and edge cases carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	New tickets receive an applicable SLA.

	Response target is calculated.

	Resolution target is calculated.

	SLA is associated with the ticket.

	SLA target is stable for an existing ticket.

	First customer-visible agent response completes response SLA.

	Internal notes do not complete response SLA.

	Ticket resolution completes resolution SLA.

	At-risk state is detected.

	Breached state is detected.

	Completed state is detected.

	SLA status is available through the API.

	SLA status is visible in the ticket UI.

	Dashboard can display SLA-at-risk information.

	Background processing updates SLA status.

	Timestamps are stored in UTC.

	Authorization is enforced.

	Relevant backend tests pass.

	Background processing tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Ticket SLA model implemented.

	SLA assignment implemented.

	Response tracking implemented.

	Resolution tracking implemented.

	At-risk detection implemented.

	Breach detection implemented.

	Background processing implemented.

	Ticket UI indicators implemented.

	Dashboard integration implemented.

	Authorization verified.

	UTC time handling verified.

	Backend tests pass.

	Worker tests pass.

	Frontend tests pass.

	Manual SLA lifecycle verified.

	No unnecessary scheduling framework introduced.

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
