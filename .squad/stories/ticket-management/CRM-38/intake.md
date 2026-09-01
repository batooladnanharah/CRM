> **Fetched from jira:** [CRM-38](https://batooladnanharah.atlassian.net/browse/CRM-38)  
> *Fetched 2026-09-01T00:44:03.352Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** TKT-001 — Ticket List  
**Type:** Task  
**Status:** In Review  
**Assignee:** Batool Harah

### Description

User Story

As a support agent, I want to view a list of support tickets so that I can quickly identify, review, and manage customer requests.

Objective

Provide an authenticated CRM ticket list that displays the most important ticket information and supports pagination, basic navigation, and reusable loading, empty, and error states.

This story focuses on displaying tickets. Advanced search and filtering are handled by TKT-002.

Scope

This story covers:

	Ticket list page

	Ticket table

	Pagination

	Ticket navigation

	Customer information

	Ticket status

	Ticket priority

	Category

	Assigned agent

	SLA indicator where available

	Loading state

	Empty state

	Error state

	Authorization

This story does not cover:

	Ticket creation

	Ticket editing

	Advanced filtering

	Ticket conversation

	Ticket assignment

	Ticket escalation

Those are handled by separate stories.

UI Requirements

Route

/tickets

Page Layout

The page should contain:

	Page title

	Page description

	Search input placeholder for future/related search functionality

	Create Ticket action

	Ticket table

	Pagination

	Loading state

	Empty state

	Error state

Example:

Tickets

Manage and track customer support requests.

[ Search tickets... ]                         [+ Create Ticket]

┌─────────────────────────────────────────────────────────────────────┐
│ ID │ Subject │ Customer │ Category │ Priority │ Status │ Agent │ SLA │
├─────────────────────────────────────────────────────────────────────┤
│ #1 │ Login   │ Ahmed    │ Technical│ High     │ Open   │ Sara  │ 2h  │
│ #2 │ Billing │ Sara     │ Billing  │ Medium   │ Pending│ Ali   │ 5h  │
└─────────────────────────────────────────────────────────────────────┘

                         < 1 2 3 4 >

Ticket Table

Initial columns:

	Ticket number

	Subject

	Customer

	Category

	Priority

	Status

	Assigned agent

	SLA

	Created date

	Updated date

	Actions

The displayed columns may be adjusted if the final domain model does not yet provide a value.

Row Actions

At minimum:

	View ticket

The following actions are handled by other stories:

	Assign

	Change status

	Change priority

	Escalate

Status Display

Use clear visual status indicators.

Initial statuses:

	Open

	In Progress

	Pending

	Resolved

	Closed

These statuses are an MVP implementation assumption and may be refined if the business requirements later define different values.

Priority Display

Initial priorities:

	Low

	Medium

	High

	Critical

Use a consistent visual indicator/badge.

Do not hardcode styling in a way that prevents future theme customization.

Customer Display

Display the customer name.

Where useful, provide navigation to the customer profile:

{{/customers/

{id}}}

Do not expose customer information that the current user is not authorized to see.

h2. SLA Display

If SLA functionality is available, display a simple indicator such as:

2h 15m remaining

or:

Overdue

If SLA functionality has not yet been implemented, the UI should gracefully display an appropriate placeholder rather than blocking the ticket list.

SLA calculation is handled by E07 — SLA & Automation.

h2. API

h3. Get Tickets

GET /api/tickets

Initial query parameters:

?page=1
&pageSize=20

Search and filtering parameters will be added by TKT-002.

Example response:

{
  "items": [
    {
      "id": "ticket-id",
      "ticketNumber": 1001,
      "subject": "Unable to login",
      "customer": {
        "id": "customer-id",
        "name": "Ahmed Ali"
      },
      "category": {
        "id": "category-id",
        "name": "Technical"
      },
      "priority": "High",
      "status": "Open",
      "assignedAgent": {
        "id": "agent-id",
        "name": "Sara Ahmed"
      },
      "sla": {
        "status": "OnTrack",
        "remainingMinutes": 135
      },
      "createdAt": "2026-08-24T10:00:00Z",
      "updatedAt": "2026-08-24T10:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}

The final response model must follow the approved domain model.

h2. Backend Requirements

The backend must:

# Require authentication.

# Verify the user has permission to access tickets.

# Validate pagination parameters.

# Query tickets from PostgreSQL.

# Use Entity Framework Core.

# Apply pagination at the database level.

# Return only the fields required by the list.

# Include required customer information.

# Include category information where available.

# Include assignment information where available.

# Include SLA information where available.

# Avoid unnecessary database queries.

# Return a consistent API response.

h2. Database Requirements

Use the Ticket entity defined by the SDD.

The ticket list should use existing relationships to:

* Customer

* Category

* Assigned User/Agent

* SLA information where implemented

Do not create duplicate customer, user, or SLA entities.

The query should be optimized to retrieve only information required for the list.

h2. Pagination

The API must support:

* Page number

* Page size

* Total count

Example:

GET /api/tickets?page=2&pageSize=20

The backend must not load all tickets into application memory before pagination.

The frontend must display appropriate pagination controls.

h2. Loading State

While tickets are loading:

* Display a table skeleton or loading indicator.

* Prevent misleading empty-state messaging.

* Avoid unnecessary repeated requests.

h2. Empty State

If there are no tickets:

No tickets found.

Create a ticket to start managing customer requests.

[+ Create Ticket]

h2. Error State

If the API request fails:

Unable to load tickets.

Please try again.

[Retry]

Technical exception details must not be displayed to the user.

h2. Navigation

Clicking a ticket should navigate to:

{{/tickets/{id}
}}

Clicking the customer should navigate to:

{{/customers/

{id}
}}

The Create Ticket button should navigate to:

/tickets/new

The target pages may not yet be implemented when this story is completed. Navigation should use the application's routing conventions.

Authorization

Use the authorization model defined in AUTH-003.

At minimum:

	Admin: allowed

	Manager: allowed

	Agent: allowed according to ticket access rules

	Customer: must not access the internal agent ticket list

Customer access to tickets is handled by E10 — Customer Portal.

Backend authorization is authoritative.

Frontend visibility must not be treated as a security mechanism.

Validation

Backend validation must cover:

	Page number greater than zero.

	Page size greater than zero.

	Maximum page size enforced.

The exact maximum page size should be defined by the application configuration.

Frontend validation should prevent obviously invalid pagination requests.

Error Handling

Handle:

	Unauthenticated request

	Unauthorized request

	Invalid pagination

	Database failure

	Unexpected API error

	Network failure

Return appropriate HTTP responses.

Do not expose stack traces or database errors to the frontend.

Performance Requirements

For the MVP:

	Use database-side pagination.

	Select only required columns.

	Avoid N+1 queries.

	Avoid loading entire related entities unnecessarily.

	Do not introduce Elasticsearch or another search engine.

The implementation should remain simple and maintainable.

Testing

Backend/API Tests

Test:

	Authenticated user can retrieve tickets.

	Unauthenticated request is rejected.

	Unauthorized role is rejected.

	Valid pagination works.

	Invalid page number is handled.

	Invalid page size is handled.

	Maximum page size is enforced.

	Empty ticket list returns valid response.

	Customer information is returned correctly.

	Assigned agent information is returned where available.

	Database failure is handled safely.

Frontend Tests

Test:

	Ticket list page renders.

	Loading state appears.

	Tickets are displayed.

	Empty state appears.

	Error state appears.

	Retry works.

	Pagination works.

	Ticket navigation works.

	Customer navigation works.

	Create Ticket navigation works.

Manual Verification

	Login as an authorized agent.

	Open /tickets.

	Verify tickets load.

	Verify customer information.

	Verify status and priority.

	Verify assigned agent.

	Verify SLA indicator where available.

	Navigate to a ticket.

	Navigate to a customer.

	Change pages.

	Test an empty dataset.

	Test an API failure.

Edge Cases

Handle:

	Zero tickets.

	Large number of tickets.

	Ticket without assigned agent.

	Ticket without SLA.

	Ticket without category.

	Ticket with missing customer information.

	Invalid page number.

	Page beyond available results.

	Database/API failure.

	Expired authentication.

	Unauthorized user.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read CUS-001 and CUS-005.

	Inspect the existing repository before creating files.

	Reuse the existing CRM layout.

	Reuse existing table, pagination, badge, loading, empty, and error components.

	Reuse the existing authentication and authorization implementation.

	Reuse the existing Customer entity.

	Do not create a duplicate Customer model.

	Do not implement advanced ticket filtering in this story.

	Do not introduce a search engine.

	Use PostgreSQL and Entity Framework Core.

	Use database-side pagination.

	Add backend and frontend tests.

	Run relevant tests after implementation.

	Review the implementation against every acceptance criterion.

	Do not implement ticket creation, assignment, status changes, or escalation in this story.

Acceptance Criteria

	Authenticated authorized users can open /tickets.

	Ticket data is retrieved from the .NET API.

	Ticket data is retrieved from PostgreSQL through EF Core.

	Ticket number is displayed.

	Subject is displayed.

	Customer is displayed.

	Category is displayed where available.

	Priority is displayed.

	Status is displayed.

	Assigned agent is displayed where available.

	SLA information is displayed where available.

	Pagination works.

	Database-side pagination is used.

	Loading state is implemented.

	Empty state is implemented.

	Error state is implemented.

	Retry is available after an error.

	Ticket navigation works.

	Customer navigation works.

	Create Ticket navigation is available.

	Backend authorization is enforced.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Vue ticket list page implemented.

	Ticket list API implemented.

	PostgreSQL/EF Core query implemented.

	Pagination implemented.

	Customer information included.

	Assignment information included where available.

	Status and priority displayed.

	SLA displayed where available.

	Authorization implemented.

	Loading/empty/error states implemented.

	Tests implemented and passing.

	Manual verification completed.

	No unnecessary dependencies introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/ticket-management/CRM-38/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `ticket-management`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-38` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** `Batool Harah`
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
TKT-001 — Ticket List
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to view a list of support tickets so that I can quickly identify, review, and manage customer requests.

Objective

Provide an authenticated CRM ticket list that displays the most important ticket information and supports pagination, basic navigation, and reusable loading, empty, and error states.

This story focuses on displaying tickets. Advanced search and filtering are handled by TKT-002.

Scope

This story covers:

	Ticket list page

	Ticket table

	Pagination

	Ticket navigation

	Customer information

	Ticket status

	Ticket priority

	Category

	Assigned agent

	SLA indicator where available

	Loading state

	Empty state

	Error state

	Authorization

This story does not cover:

	Ticket creation

	Ticket editing

	Advanced filtering

	Ticket conversation

	Ticket assignment

	Ticket escalation

Those are handled by separate stories.

UI Requirements

Route

/tickets

Page Layout

The page should contain:

	Page title

	Page description

	Search input placeholder for future/related search functionality

	Create Ticket action

	Ticket table

	Pagination

	Loading state

	Empty state

	Error state

Example:

Tickets

Manage and track customer support requests.

[ Search tickets... ]                         [+ Create Ticket]

┌─────────────────────────────────────────────────────────────────────┐
│ ID │ Subject │ Customer │ Category │ Priority │ Status │ Agent │ SLA │
├─────────────────────────────────────────────────────────────────────┤
│ #1 │ Login   │ Ahmed    │ Technical│ High     │ Open   │ Sara  │ 2h  │
│ #2 │ Billing │ Sara     │ Billing  │ Medium   │ Pending│ Ali   │ 5h  │
└─────────────────────────────────────────────────────────────────────┘

                         < 1 2 3 4 >

Ticket Table

Initial columns:

	Ticket number

	Subject

	Customer

	Category

	Priority

	Status

	Assigned agent

	SLA

	Created date

	Updated date

	Actions

The displayed columns may be adjusted if the final domain model does not yet provide a value.

Row Actions

At minimum:

	View ticket

The following actions are handled by other stories:

	Assign

	Change status

	Change priority

	Escalate

Status Display

Use clear visual status indicators.

Initial statuses:

	Open

	In Progress

	Pending

	Resolved

	Closed

These statuses are an MVP implementation assumption and may be refined if the business requirements later define different values.

Priority Display

Initial priorities:

	Low

	Medium

	High

	Critical

Use a consistent visual indicator/badge.

Do not hardcode styling in a way that prevents future theme customization.

Customer Display

Display the customer name.

Where useful, provide navigation to the customer profile:

{ {/customers/

{id}}}

Do not expose customer information that the current user is not authorized to see.

h2. SLA Display

If SLA functionality is available, display a simple indicator such as:

2h 15m remaining

or:

Overdue

If SLA functionality has not yet been implemented, the UI should gracefully display an appropriate placeholder rather than blocking the ticket list.

SLA calculation is handled by E07 — SLA & Automation.

h2. API

h3. Get Tickets

GET /api/tickets

Initial query parameters:

?page=1
&pageSize=20

Search and filtering parameters will be added by TKT-002.

Example response:

{
  "items": [
    {
      "id": "ticket-id",
      "ticketNumber": 1001,
      "subject": "Unable to login",
      "customer": {
        "id": "customer-id",
        "name": "Ahmed Ali"
      },
      "category": {
        "id": "category-id",
        "name": "Technical"
      },
      "priority": "High",
      "status": "Open",
      "assignedAgent": {
        "id": "agent-id",
        "name": "Sara Ahmed"
      },
      "sla": {
        "status": "OnTrack",
        "remainingMinutes": 135
      },
      "createdAt": "2026-08-24T10:00:00Z",
      "updatedAt": "2026-08-24T10:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}

The final response model must follow the approved domain model.

h2. Backend Requirements

The backend must:

# Require authentication.

# Verify the user has permission to access tickets.

# Validate pagination parameters.

# Query tickets from PostgreSQL.

# Use Entity Framework Core.

# Apply pagination at the database level.

# Return only the fields required by the list.

# Include required customer information.

# Include category information where available.

# Include assignment information where available.

# Include SLA information where available.

# Avoid unnecessary database queries.

# Return a consistent API response.

h2. Database Requirements

Use the Ticket entity defined by the SDD.

The ticket list should use existing relationships to:

* Customer

* Category

* Assigned User/Agent

* SLA information where implemented

Do not create duplicate customer, user, or SLA entities.

The query should be optimized to retrieve only information required for the list.

h2. Pagination

The API must support:

* Page number

* Page size

* Total count

Example:

GET /api/tickets?page=2&pageSize=20

The backend must not load all tickets into application memory before pagination.

The frontend must display appropriate pagination controls.

h2. Loading State

While tickets are loading:

* Display a table skeleton or loading indicator.

* Prevent misleading empty-state messaging.

* Avoid unnecessary repeated requests.

h2. Empty State

If there are no tickets:

No tickets found.

Create a ticket to start managing customer requests.

[+ Create Ticket]

h2. Error State

If the API request fails:

Unable to load tickets.

Please try again.

[Retry]

Technical exception details must not be displayed to the user.

h2. Navigation

Clicking a ticket should navigate to:

{ {/tickets/{id}
}}

Clicking the customer should navigate to:

{ {/customers/

{id}
}}

The Create Ticket button should navigate to:

/tickets/new

The target pages may not yet be implemented when this story is completed. Navigation should use the application's routing conventions.

Authorization

Use the authorization model defined in AUTH-003.

At minimum:

	Admin: allowed

	Manager: allowed

	Agent: allowed according to ticket access rules

	Customer: must not access the internal agent ticket list

Customer access to tickets is handled by E10 — Customer Portal.

Backend authorization is authoritative.

Frontend visibility must not be treated as a security mechanism.

Validation

Backend validation must cover:

	Page number greater than zero.

	Page size greater than zero.

	Maximum page size enforced.

The exact maximum page size should be defined by the application configuration.

Frontend validation should prevent obviously invalid pagination requests.

Error Handling

Handle:

	Unauthenticated request

	Unauthorized request

	Invalid pagination

	Database failure

	Unexpected API error

	Network failure

Return appropriate HTTP responses.

Do not expose stack traces or database errors to the frontend.

Performance Requirements

For the MVP:

	Use database-side pagination.

	Select only required columns.

	Avoid N+1 queries.

	Avoid loading entire related entities unnecessarily.

	Do not introduce Elasticsearch or another search engine.

The implementation should remain simple and maintainable.

Testing

Backend/API Tests

Test:

	Authenticated user can retrieve tickets.

	Unauthenticated request is rejected.

	Unauthorized role is rejected.

	Valid pagination works.

	Invalid page number is handled.

	Invalid page size is handled.

	Maximum page size is enforced.

	Empty ticket list returns valid response.

	Customer information is returned correctly.

	Assigned agent information is returned where available.

	Database failure is handled safely.

Frontend Tests

Test:

	Ticket list page renders.

	Loading state appears.

	Tickets are displayed.

	Empty state appears.

	Error state appears.

	Retry works.

	Pagination works.

	Ticket navigation works.

	Customer navigation works.

	Create Ticket navigation works.

Manual Verification

	Login as an authorized agent.

	Open /tickets.

	Verify tickets load.

	Verify customer information.

	Verify status and priority.

	Verify assigned agent.

	Verify SLA indicator where available.

	Navigate to a ticket.

	Navigate to a customer.

	Change pages.

	Test an empty dataset.

	Test an API failure.

Edge Cases

Handle:

	Zero tickets.

	Large number of tickets.

	Ticket without assigned agent.

	Ticket without SLA.

	Ticket without category.

	Ticket with missing customer information.

	Invalid page number.

	Page beyond available results.

	Database/API failure.

	Expired authentication.

	Unauthorized user.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read CUS-001 and CUS-005.

	Inspect the existing repository before creating files.

	Reuse the existing CRM layout.

	Reuse existing table, pagination, badge, loading, empty, and error components.

	Reuse the existing authentication and authorization implementation.

	Reuse the existing Customer entity.

	Do not create a duplicate Customer model.

	Do not implement advanced ticket filtering in this story.

	Do not introduce a search engine.

	Use PostgreSQL and Entity Framework Core.

	Use database-side pagination.

	Add backend and frontend tests.

	Run relevant tests after implementation.

	Review the implementation against every acceptance criterion.

	Do not implement ticket creation, assignment, status changes, or escalation in this story.

Acceptance Criteria

	Authenticated authorized users can open /tickets.

	Ticket data is retrieved from the .NET API.

	Ticket data is retrieved from PostgreSQL through EF Core.

	Ticket number is displayed.

	Subject is displayed.

	Customer is displayed.

	Category is displayed where available.

	Priority is displayed.

	Status is displayed.

	Assigned agent is displayed where available.

	SLA information is displayed where available.

	Pagination works.

	Database-side pagination is used.

	Loading state is implemented.

	Empty state is implemented.

	Error state is implemented.

	Retry is available after an error.

	Ticket navigation works.

	Customer navigation works.

	Create Ticket navigation is available.

	Backend authorization is enforced.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Vue ticket list page implemented.

	Ticket list API implemented.

	PostgreSQL/EF Core query implemented.

	Pagination implemented.

	Customer information included.

	Assignment information included where available.

	Status and priority displayed.

	SLA displayed where available.

	Authorization implemented.

	Loading/empty/error states implemented.

	Tests implemented and passing.

	Manual verification completed.

	No unnecessary dependencies introduced.

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
