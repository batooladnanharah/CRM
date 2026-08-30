> **Fetched from jira:** [CRM-60](https://batooladnanharah.atlassian.net/browse/CRM-60)  
> *Fetched 2026-08-30T21:28:42.905Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** SLA-001 — SLA Policies & Targets  
**Type:** Task  
**Status:** In Review

### Description

User Story

As a support manager, I want to define SLA policies with response and resolution targets so that support tickets can be measured against agreed service levels.

Objective

Create a simple SLA policy system that defines:

	First response target

	Resolution target

	Priority applicability

	Active/inactive status

The SLA policy will be used by later stories for SLA tracking, breach detection, and escalation.

Scope

This story covers:

	SLA policy model

	SLA target configuration

	Priority-based targets

	Create/update/list policies

	Activate/deactivate policy

	Validation

	API

	Basic administration UI

	Authorization

	Tests

This story does not implement:

	SLA timers

	Breach detection

	Escalation

	Notifications

	Automatic assignment

Those are handled by later stories.

SLA Policy

A policy should contain:

SlaPolicy

Id
Name
Description
IsActive
CreatedAt
UpdatedAt

SLA targets should define the response and resolution time.

A simple MVP structure can be:

SlaPolicyTarget

Id
SlaPolicyId
Priority
FirstResponseMinutes
ResolutionMinutes

This allows different targets for different ticket priorities.

Example

Policy: Standard Support

Priority       First Response       Resolution
------------------------------------------------
Critical       15 minutes           4 hours
High           30 minutes           8 hours
Medium         2 hours              24 hours
Low            4 hours              48 hours

UI Requirements

Recommended administration page:

SLA Policies

[ + Create Policy ]

┌──────────────────────┬──────────┬─────────────┬──────────┐
│ Policy               │ Status   │ Targets     │ Actions  │
├──────────────────────┼──────────┼─────────────┼──────────┤
│ Standard Support     │ Active   │ 4 priorities│ Edit     │
│ VIP Support          │ Active   │ 4 priorities│ Edit     │
│ Legacy Support       │ Inactive │ 4 priorities│ Edit     │
└──────────────────────┴──────────┴─────────────┴──────────┘

Create/Edit Form

Create SLA Policy

Name *
[ Standard Support ]

Description
[ General support SLA policy........ ]

Priority Targets

              Response       Resolution

Critical      [15 min]       [4 hours]
High          [30 min]       [8 hours]
Medium        [2 hours]       [24 hours]
Low           [4 hours]       [48 hours]

Status
[ Active ]

[Cancel] [Save]

Priority

Use the existing ticket priority model.

Do not create another priority system.

Expected priorities:

Critical
High
Medium
Low

If the existing project uses a different ordering or naming, follow the existing domain model.

Target Units

Store targets in a consistent unit.

Recommended:

Minutes

Example:

FirstResponseMinutes = 30
ResolutionMinutes = 480

The UI may display:

30 minutes
8 hours

Do not store human-readable strings such as:

"8 hours"

as the actual SLA value.

Validation

Policy Name

Required.

Must not be whitespace-only.

Maximum length should be enforced.

First Response

Must be:

> 0

Resolution

Must be:

> 0

Logical Validation

Resolution target should not normally be shorter than the first response target.

Reject invalid configurations unless the SDD explicitly allows them.

Active Policies

An SLA policy may be:

Active
Inactive

Inactive policies should not be selected for new tickets.

Existing tickets using an SLA policy should not unexpectedly change because an administrator deactivated the policy.

The AI should inspect the SDD and existing data model before deciding whether tickets store a policy reference or snapshot target values.

Policy Selection

For the MVP, a ticket may use the active default SLA policy.

Example:

New Ticket
    ↓
Active Default SLA Policy
    ↓
Priority
    ↓
SLA Targets

Do not implement complex policy matching based on:

	Customer segment

	Branch

	Department

	Product

	Channel

unless required by the SDD.

Default Policy

The system should support identifying one active default policy.

Only one policy should be the default at a time.

If the project does not require a default field, the backend may select the appropriate active policy according to a documented rule.

Do not implement multiple conflicting default policies.

API

List Policies

GET /api/sla/policies

Example:

{
  "items": [
    {
      "id": "policy-id",
      "name": "Standard Support",
      "description": "General support SLA policy",
      "isActive": true,
      "isDefault": true
    }
  ]
}

Get Policy

{{GET /api/sla/policies/

{id}}}

h3. Create

POST /api/sla/policies

Example:

{
  "name": "Standard Support",
  "description": "General support SLA policy",
  "isActive": true,
  "isDefault": true,
  "targets": [
    {
      "priority": "Critical",
      "firstResponseMinutes": 15,
      "resolutionMinutes": 240
    },
    {
      "priority": "High",
      "firstResponseMinutes": 30,
      "resolutionMinutes": 480
    },
    {
      "priority": "Medium",
      "firstResponseMinutes": 120,
      "resolutionMinutes": 1440
    },
    {
      "priority": "Low",
      "firstResponseMinutes": 240,
      "resolutionMinutes": 2880
    }
  ]
}

h3. Update

{{PUT /api/sla/policies/{id}
}}

Activate/Deactivate

Either:

{{PATCH /api/sla/policies/

{id}
/status}}

or follow the project's existing API conventions.

Authorization

Only authorized management users should manage SLA policies.

Recommended:

Admin
Manager
   ↓
Manage SLA policies

Agent
   ↓
View SLA information

Agents should not be able to modify SLA policy configuration.

Use AUTH-003.

Backend Requirements

The backend must:

	Authenticate the user.

	Authorize policy management.

	Validate policy input.

	Validate priority targets.

	Prevent duplicate/conflicting default policies.

	Persist policies in PostgreSQL.

	Persist targets.

	Return appropriate API responses.

	Handle database failures.

	Prevent invalid SLA configurations.

Database

Use PostgreSQL with EF Core.

Suggested:

SlaPolicy
---------
Id
Name
Description
IsActive
IsDefault
CreatedAt
UpdatedAt

SlaPolicyTarget
---------------
Id
SlaPolicyId
Priority
FirstResponseMinutes
ResolutionMinutes

Add appropriate:

	Primary keys

	Foreign keys

	Unique constraints

	Indexes

A policy should not contain duplicate targets for the same priority.

Ticket Integration

Do not implement SLA timing in this story.

However, the model should be usable by SLA-002.

Later flow:

Ticket Created
      ↓
Select SLA Policy
      ↓
Select Target Based On Priority
      ↓
Track Response/Resolution

Do not implement the timers yet.

UI Error Handling

Create policy:

Unable to create SLA policy.

Please check the entered values.

Duplicate default:

Another default SLA policy already exists.

Do not display technical database errors.

Loading States

Use the shared loading/skeleton components.

Examples:

Loading SLA policies...

Saving policy...

Empty State

If no policies exist:

No SLA policies configured.

Create your first SLA policy.

Button:

+ Create Policy

Arabic / English

The UI must support:

	English

	Arabic

	LTR

	RTL

Policy names/descriptions are user-entered data and should not be automatically translated.

System labels should use i18n.

Testing

Backend/API Tests

Test:

	Authorized manager can create policy.

	Agent cannot create policy.

	Policy validation works.

	First response must be positive.

	Resolution must be positive.

	Duplicate priority target rejected.

	Default policy behavior works.

	Active/inactive works.

	Policy persists.

	Targets persist.

	Update works.

	Database failure is handled.

Frontend Tests

Test:

	Policy list renders.

	Create form renders.

	Validation works.

	Priority target fields work.

	Create works.

	Edit works.

	Activate/deactivate works.

	Empty state works.

	Loading state works.

	Error state works.

	Arabic RTL works.

Manual Verification

	Login as Manager.

	Open SLA Policies.

	Create Standard Support.

	Add targets for all priorities.

	Save.

	Verify policy appears.

	Edit policy.

	Change target.

	Save.

	Deactivate policy.

	Verify status changes.

	Attempt invalid target.

	Verify validation.

	Login as Agent.

	Verify agent cannot modify policies.

Edge Cases

Handle:

	Empty policy name.

	Duplicate policy name if prohibited.

	Duplicate priority target.

	Zero target.

	Negative target.

	Extremely large target.

	Resolution shorter than response.

	Multiple default policies.

	Deactivating default policy.

	No active policy.

	Database failure.

	Unauthorized user.

	Arabic RTL.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read the existing Ticket Priority model.

	Read TKT-003.

	Read TKT-007.

	review 22-story-crm-100.md ,23-story-crm-101.md ,36-story-crm-63.md plan and reuse the exiting code and enhance  it

	Inspect existing configuration/administration patterns.

	Reuse existing authorization.

	Reuse existing priority values.

	Use PostgreSQL and EF Core.

	Do not implement SLA timers yet.

	Do not implement escalation yet.

	Do not implement notifications yet.

	Do not create complex policy matching.

	Keep the model simple.

	Add backend and frontend tests.

	Run relevant tests.

	Review business validation carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized users can create SLA policies.

	Authorized users can edit SLA policies.

	Authorized users can activate/deactivate policies.

	SLA policy has response targets.

	SLA policy has resolution targets.

	Targets can be configured by ticket priority.

	Existing ticket priorities are reused.

	Invalid targets are rejected.

	Duplicate priority targets are rejected.

	Default policy behavior is enforced.

	Policies persist in PostgreSQL.

	Agents cannot modify SLA policies without permission.

	Loading state is implemented.

	Empty state is implemented.

	Error handling is implemented.

	Arabic RTL is supported.

	English LTR is supported.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	SLA policy model implemented.

	SLA target model implemented.

	CRUD API implemented.

	Management UI implemented.

	Authorization implemented.

	Validation implemented.

	PostgreSQL persistence verified.

	Default-policy rules implemented.

	Backend tests pass.

	Frontend tests pass.

	Manual verification completed.

	No SLA timing/escalation complexity added prematurely.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/sla-automation/CRM-60/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `sla-automation`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-60` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
SLA-001 — SLA Policies & Targets
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support manager, I want to define SLA policies with response and resolution targets so that support tickets can be measured against agreed service levels.

Objective

Create a simple SLA policy system that defines:

	First response target

	Resolution target

	Priority applicability

	Active/inactive status

The SLA policy will be used by later stories for SLA tracking, breach detection, and escalation.

Scope

This story covers:

	SLA policy model

	SLA target configuration

	Priority-based targets

	Create/update/list policies

	Activate/deactivate policy

	Validation

	API

	Basic administration UI

	Authorization

	Tests

This story does not implement:

	SLA timers

	Breach detection

	Escalation

	Notifications

	Automatic assignment

Those are handled by later stories.

SLA Policy

A policy should contain:

SlaPolicy

Id
Name
Description
IsActive
CreatedAt
UpdatedAt

SLA targets should define the response and resolution time.

A simple MVP structure can be:

SlaPolicyTarget

Id
SlaPolicyId
Priority
FirstResponseMinutes
ResolutionMinutes

This allows different targets for different ticket priorities.

Example

Policy: Standard Support

Priority       First Response       Resolution
------------------------------------------------
Critical       15 minutes           4 hours
High           30 minutes           8 hours
Medium         2 hours              24 hours
Low            4 hours              48 hours

UI Requirements

Recommended administration page:

SLA Policies

[ + Create Policy ]

┌──────────────────────┬──────────┬─────────────┬──────────┐
│ Policy               │ Status   │ Targets     │ Actions  │
├──────────────────────┼──────────┼─────────────┼──────────┤
│ Standard Support     │ Active   │ 4 priorities│ Edit     │
│ VIP Support          │ Active   │ 4 priorities│ Edit     │
│ Legacy Support       │ Inactive │ 4 priorities│ Edit     │
└──────────────────────┴──────────┴─────────────┴──────────┘

Create/Edit Form

Create SLA Policy

Name *
[ Standard Support ]

Description
[ General support SLA policy........ ]

Priority Targets

              Response       Resolution

Critical      [15 min]       [4 hours]
High          [30 min]       [8 hours]
Medium        [2 hours]       [24 hours]
Low           [4 hours]       [48 hours]

Status
[ Active ]

[Cancel] [Save]

Priority

Use the existing ticket priority model.

Do not create another priority system.

Expected priorities:

Critical
High
Medium
Low

If the existing project uses a different ordering or naming, follow the existing domain model.

Target Units

Store targets in a consistent unit.

Recommended:

Minutes

Example:

FirstResponseMinutes = 30
ResolutionMinutes = 480

The UI may display:

30 minutes
8 hours

Do not store human-readable strings such as:

"8 hours"

as the actual SLA value.

Validation

Policy Name

Required.

Must not be whitespace-only.

Maximum length should be enforced.

First Response

Must be:

> 0

Resolution

Must be:

> 0

Logical Validation

Resolution target should not normally be shorter than the first response target.

Reject invalid configurations unless the SDD explicitly allows them.

Active Policies

An SLA policy may be:

Active
Inactive

Inactive policies should not be selected for new tickets.

Existing tickets using an SLA policy should not unexpectedly change because an administrator deactivated the policy.

The AI should inspect the SDD and existing data model before deciding whether tickets store a policy reference or snapshot target values.

Policy Selection

For the MVP, a ticket may use the active default SLA policy.

Example:

New Ticket
    ↓
Active Default SLA Policy
    ↓
Priority
    ↓
SLA Targets

Do not implement complex policy matching based on:

	Customer segment

	Branch

	Department

	Product

	Channel

unless required by the SDD.

Default Policy

The system should support identifying one active default policy.

Only one policy should be the default at a time.

If the project does not require a default field, the backend may select the appropriate active policy according to a documented rule.

Do not implement multiple conflicting default policies.

API

List Policies

GET /api/sla/policies

Example:

{
  "items": [
    {
      "id": "policy-id",
      "name": "Standard Support",
      "description": "General support SLA policy",
      "isActive": true,
      "isDefault": true
    }
  ]
}

Get Policy

{ {GET /api/sla/policies/

{id}}}

h3. Create

POST /api/sla/policies

Example:

{
  "name": "Standard Support",
  "description": "General support SLA policy",
  "isActive": true,
  "isDefault": true,
  "targets": [
    {
      "priority": "Critical",
      "firstResponseMinutes": 15,
      "resolutionMinutes": 240
    },
    {
      "priority": "High",
      "firstResponseMinutes": 30,
      "resolutionMinutes": 480
    },
    {
      "priority": "Medium",
      "firstResponseMinutes": 120,
      "resolutionMinutes": 1440
    },
    {
      "priority": "Low",
      "firstResponseMinutes": 240,
      "resolutionMinutes": 2880
    }
  ]
}

h3. Update

{ {PUT /api/sla/policies/{id}
}}

Activate/Deactivate

Either:

{ {PATCH /api/sla/policies/

{id}
/status}}

or follow the project's existing API conventions.

Authorization

Only authorized management users should manage SLA policies.

Recommended:

Admin
Manager
   ↓
Manage SLA policies

Agent
   ↓
View SLA information

Agents should not be able to modify SLA policy configuration.

Use AUTH-003.

Backend Requirements

The backend must:

	Authenticate the user.

	Authorize policy management.

	Validate policy input.

	Validate priority targets.

	Prevent duplicate/conflicting default policies.

	Persist policies in PostgreSQL.

	Persist targets.

	Return appropriate API responses.

	Handle database failures.

	Prevent invalid SLA configurations.

Database

Use PostgreSQL with EF Core.

Suggested:

SlaPolicy
---------
Id
Name
Description
IsActive
IsDefault
CreatedAt
UpdatedAt

SlaPolicyTarget
---------------
Id
SlaPolicyId
Priority
FirstResponseMinutes
ResolutionMinutes

Add appropriate:

	Primary keys

	Foreign keys

	Unique constraints

	Indexes

A policy should not contain duplicate targets for the same priority.

Ticket Integration

Do not implement SLA timing in this story.

However, the model should be usable by SLA-002.

Later flow:

Ticket Created
      ↓
Select SLA Policy
      ↓
Select Target Based On Priority
      ↓
Track Response/Resolution

Do not implement the timers yet.

UI Error Handling

Create policy:

Unable to create SLA policy.

Please check the entered values.

Duplicate default:

Another default SLA policy already exists.

Do not display technical database errors.

Loading States

Use the shared loading/skeleton components.

Examples:

Loading SLA policies...

Saving policy...

Empty State

If no policies exist:

No SLA policies configured.

Create your first SLA policy.

Button:

+ Create Policy

Arabic / English

The UI must support:

	English

	Arabic

	LTR

	RTL

Policy names/descriptions are user-entered data and should not be automatically translated.

System labels should use i18n.

Testing

Backend/API Tests

Test:

	Authorized manager can create policy.

	Agent cannot create policy.

	Policy validation works.

	First response must be positive.

	Resolution must be positive.

	Duplicate priority target rejected.

	Default policy behavior works.

	Active/inactive works.

	Policy persists.

	Targets persist.

	Update works.

	Database failure is handled.

Frontend Tests

Test:

	Policy list renders.

	Create form renders.

	Validation works.

	Priority target fields work.

	Create works.

	Edit works.

	Activate/deactivate works.

	Empty state works.

	Loading state works.

	Error state works.

	Arabic RTL works.

Manual Verification

	Login as Manager.

	Open SLA Policies.

	Create Standard Support.

	Add targets for all priorities.

	Save.

	Verify policy appears.

	Edit policy.

	Change target.

	Save.

	Deactivate policy.

	Verify status changes.

	Attempt invalid target.

	Verify validation.

	Login as Agent.

	Verify agent cannot modify policies.

Edge Cases

Handle:

	Empty policy name.

	Duplicate policy name if prohibited.

	Duplicate priority target.

	Zero target.

	Negative target.

	Extremely large target.

	Resolution shorter than response.

	Multiple default policies.

	Deactivating default policy.

	No active policy.

	Database failure.

	Unauthorized user.

	Arabic RTL.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read the existing Ticket Priority model.

	Read TKT-003.

	Read TKT-007.

	review 22-story-crm-100.md ,23-story-crm-101.md ,36-story-crm-63.md plan and reuse the exiting code and enhance  it

	Inspect existing configuration/administration patterns.

	Reuse existing authorization.

	Reuse existing priority values.

	Use PostgreSQL and EF Core.

	Do not implement SLA timers yet.

	Do not implement escalation yet.

	Do not implement notifications yet.

	Do not create complex policy matching.

	Keep the model simple.

	Add backend and frontend tests.

	Run relevant tests.

	Review business validation carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized users can create SLA policies.

	Authorized users can edit SLA policies.

	Authorized users can activate/deactivate policies.

	SLA policy has response targets.

	SLA policy has resolution targets.

	Targets can be configured by ticket priority.

	Existing ticket priorities are reused.

	Invalid targets are rejected.

	Duplicate priority targets are rejected.

	Default policy behavior is enforced.

	Policies persist in PostgreSQL.

	Agents cannot modify SLA policies without permission.

	Loading state is implemented.

	Empty state is implemented.

	Error handling is implemented.

	Arabic RTL is supported.

	English LTR is supported.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	SLA policy model implemented.

	SLA target model implemented.

	CRUD API implemented.

	Management UI implemented.

	Authorization implemented.

	Validation implemented.

	PostgreSQL persistence verified.

	Default-policy rules implemented.

	Backend tests pass.

	Frontend tests pass.

	Manual verification completed.

	No SLA timing/escalation complexity added prematurely.

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
