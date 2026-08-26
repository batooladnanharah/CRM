> **Fetched from jira:** [CRM-33](https://batooladnanharah.atlassian.net/browse/CRM-33)  
> *Fetched 2026-08-24T23:23:44.047Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** CUS-005 Customer Profile  
**Type:** Task  
**Status:** To Do

### Description

User Story

As a support agent, I want to view a customer's complete profile so that I can understand the customer and their support history from one place.

Objective

Provide a customer profile screen that displays the customer's contact information and the CRM information currently available for that customer.

The profile should provide a foundation for later customer features such as tickets, interaction history, notes, and attachments.

UI Requirements

Route

{{/customers/

{id}}}

h3. Profile Layout

Recommended layout:

Customer Profile

┌─────────────────────────────────────────────────────────────┐
│ Ahmed Ali                                      [Edit]       │
│ ahmed@example.com                                           │
│ +201000000000                                                │
│ Status: Active                                               │
└─────────────────────────────────────────────────────────────┘

[Overview] [Tickets] [Interactions] [Notes] [Attachments]

Overview
─────────────────────────────────────────────────────────────

Contact Information
Email:      ahmed@example.com
Phone:      +201000000000

Organization
Branch:     Cairo
Department: Customer Support

Customer Since
24 Aug 2026

The exact tabs can be introduced as the corresponding stories are implemented.

h2. Information Displayed

At minimum display:

* Customer name

* Email

* Phone

* Status

* Branch where available

* Department where available

* Created date

* Updated date

* Ticket count where available

Do not display fields that are not part of the approved Customer model.

h2. Actions

Authorized CRM users should be able to:

* Edit customer

* Return to customer list

* View customer tickets where implemented

The Edit action should navigate to the functionality from CUS-004.

h2. API

h3. Get Customer

{{GET /api/customers/{id}
}}

Example response:

{
  "id": "customer-id",
  "name": "Ahmed Ali",
  "email": "ahmed@example.com",
  "phone": "+201000000000",
  "status": "Active",
  "branchId": "branch-id",
  "departmentId": "department-id",
  "createdAt": "2026-08-24T10:00:00Z",
  "updatedAt": "2026-08-24T10:30:00Z",
  "ticketCount": 5
}

The exact response must follow the final Customer domain model.

Backend Requirements

The backend must:

	Require authentication.

	Validate the customer identifier.

	Retrieve the customer from PostgreSQL.

	Include only information the current user is authorized to see.

	Return a not-found response when the customer does not exist.

	Return the customer information in a consistent API response.

	Handle database failures safely.

Database Requirements

Use the existing Customer entity created by CUS-003.

Do not create another customer table.

The profile may retrieve related information such as ticket count using appropriate EF Core queries.

Avoid unnecessary database queries.

Authorization

Use the authorization rules from AUTH-003.

At minimum:

	Admin: allowed.

	Manager: allowed.

	Agent: allowed according to CRM access rules.

	Customer: restricted to their own customer/portal context.

Customer data must not be exposed to unauthorized users.

Loading State

Display a loading/skeleton state while the customer profile is being retrieved.

Do not display an empty profile while data is still loading.

Empty/Not Found State

If the customer does not exist:

Customer not found.

The requested customer could not be found.

[Back to Customers]

Error State

If the API fails:

Unable to load customer information.

[Retry]

Do not display technical exception details.

Validation

Validate the customer identifier before attempting to retrieve the customer where appropriate.

The backend remains responsible for authoritative validation.

Testing

Backend/API Tests

Test:

	Authorized user can retrieve a customer.

	Valid customer ID returns customer information.

	Invalid customer ID is handled.

	Customer does not exist.

	Unauthorized request is rejected.

	User cannot access restricted customer data.

	Database failure is handled safely.

Frontend Tests

Test:

	Profile page renders.

	Customer information is displayed.

	Loading state appears.

	Not-found state appears.

	API error appears.

	Retry works.

	Edit action navigates correctly.

	Back action returns to customer list.

Manual Verification

	Login as an authorized agent.

	Open Customers.

	Select a customer.

	Verify profile information.

	Select Edit.

	Return to customer list.

	Open an invalid customer URL.

	Verify the not-found state.

	Simulate/API failure and verify the error state.

Edge Cases

Handle:

	Customer does not exist.

	Invalid customer ID.

	Missing optional email.

	Missing optional phone.

	Missing branch.

	Missing department.

	Customer with no tickets.

	Customer with many tickets.

	API failure.

	Unauthorized access.

	Expired authentication.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read CUS-001 through CUS-004.

	Reuse the existing Customer entity and customer API.

	Reuse the existing CRM layout and UI components.

	Do not create another Customer entity or table.

	Do not implement interaction history, notes, or attachments in this story.

	Those capabilities will be added through their dedicated stories.

	Reuse the existing authorization implementation.

	Add tests.

	Run relevant tests.

	Review the implementation against all acceptance criteria.

	Do not introduce unnecessary dependencies.

Acceptance Criteria

	Authorized user can open {{/customers/
{id}
}}.

	Customer profile information is displayed.

	Customer data is retrieved from the backend.

	Customer data comes from PostgreSQL through the existing data layer.

	Loading state is displayed.

	Not-found state is displayed when appropriate.

	API errors are handled.

	Retry is available after a load failure.

	Edit action navigates to customer editing.

	Authorization is enforced.

	Unauthorized users cannot access restricted customer information.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated code has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Customer profile route implemented.

	Profile UI implemented.

	Customer profile API implemented or reused.

	Authorization verified.

	Loading/error/not-found states implemented.

	Tests implemented and passing.

	Manual verification completed.

	No duplicate customer model introduced.

	AI-generated implementation reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/customer-management/CRM-33/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `customer-management`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-33` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `To Do`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
CUS-005 Customer Profile
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to view a customer's complete profile so that I can understand the customer and their support history from one place.

Objective

Provide a customer profile screen that displays the customer's contact information and the CRM information currently available for that customer.

The profile should provide a foundation for later customer features such as tickets, interaction history, notes, and attachments.

UI Requirements

Route

{ {/customers/

{id}}}

h3. Profile Layout

Recommended layout:

Customer Profile

┌─────────────────────────────────────────────────────────────┐
│ Ahmed Ali                                      [Edit]       │
│ ahmed@example.com                                           │
│ +201000000000                                                │
│ Status: Active                                               │
└─────────────────────────────────────────────────────────────┘

[Overview] [Tickets] [Interactions] [Notes] [Attachments]

Overview
─────────────────────────────────────────────────────────────

Contact Information
Email:      ahmed@example.com
Phone:      +201000000000

Organization
Branch:     Cairo
Department: Customer Support

Customer Since
24 Aug 2026

The exact tabs can be introduced as the corresponding stories are implemented.

h2. Information Displayed

At minimum display:

* Customer name

* Email

* Phone

* Status

* Branch where available

* Department where available

* Created date

* Updated date

* Ticket count where available

Do not display fields that are not part of the approved Customer model.

h2. Actions

Authorized CRM users should be able to:

* Edit customer

* Return to customer list

* View customer tickets where implemented

The Edit action should navigate to the functionality from CUS-004.

h2. API

h3. Get Customer

{ {GET /api/customers/{id}
}}

Example response:

{
  "id": "customer-id",
  "name": "Ahmed Ali",
  "email": "ahmed@example.com",
  "phone": "+201000000000",
  "status": "Active",
  "branchId": "branch-id",
  "departmentId": "department-id",
  "createdAt": "2026-08-24T10:00:00Z",
  "updatedAt": "2026-08-24T10:30:00Z",
  "ticketCount": 5
}

The exact response must follow the final Customer domain model.

Backend Requirements

The backend must:

	Require authentication.

	Validate the customer identifier.

	Retrieve the customer from PostgreSQL.

	Include only information the current user is authorized to see.

	Return a not-found response when the customer does not exist.

	Return the customer information in a consistent API response.

	Handle database failures safely.

Database Requirements

Use the existing Customer entity created by CUS-003.

Do not create another customer table.

The profile may retrieve related information such as ticket count using appropriate EF Core queries.

Avoid unnecessary database queries.

Authorization

Use the authorization rules from AUTH-003.

At minimum:

	Admin: allowed.

	Manager: allowed.

	Agent: allowed according to CRM access rules.

	Customer: restricted to their own customer/portal context.

Customer data must not be exposed to unauthorized users.

Loading State

Display a loading/skeleton state while the customer profile is being retrieved.

Do not display an empty profile while data is still loading.

Empty/Not Found State

If the customer does not exist:

Customer not found.

The requested customer could not be found.

[Back to Customers]

Error State

If the API fails:

Unable to load customer information.

[Retry]

Do not display technical exception details.

Validation

Validate the customer identifier before attempting to retrieve the customer where appropriate.

The backend remains responsible for authoritative validation.

Testing

Backend/API Tests

Test:

	Authorized user can retrieve a customer.

	Valid customer ID returns customer information.

	Invalid customer ID is handled.

	Customer does not exist.

	Unauthorized request is rejected.

	User cannot access restricted customer data.

	Database failure is handled safely.

Frontend Tests

Test:

	Profile page renders.

	Customer information is displayed.

	Loading state appears.

	Not-found state appears.

	API error appears.

	Retry works.

	Edit action navigates correctly.

	Back action returns to customer list.

Manual Verification

	Login as an authorized agent.

	Open Customers.

	Select a customer.

	Verify profile information.

	Select Edit.

	Return to customer list.

	Open an invalid customer URL.

	Verify the not-found state.

	Simulate/API failure and verify the error state.

Edge Cases

Handle:

	Customer does not exist.

	Invalid customer ID.

	Missing optional email.

	Missing optional phone.

	Missing branch.

	Missing department.

	Customer with no tickets.

	Customer with many tickets.

	API failure.

	Unauthorized access.

	Expired authentication.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read CUS-001 through CUS-004.

	Reuse the existing Customer entity and customer API.

	Reuse the existing CRM layout and UI components.

	Do not create another Customer entity or table.

	Do not implement interaction history, notes, or attachments in this story.

	Those capabilities will be added through their dedicated stories.

	Reuse the existing authorization implementation.

	Add tests.

	Run relevant tests.

	Review the implementation against all acceptance criteria.

	Do not introduce unnecessary dependencies.

Acceptance Criteria

	Authorized user can open { {/customers/
{id}
}}.

	Customer profile information is displayed.

	Customer data is retrieved from the backend.

	Customer data comes from PostgreSQL through the existing data layer.

	Loading state is displayed.

	Not-found state is displayed when appropriate.

	API errors are handled.

	Retry is available after a load failure.

	Edit action navigates to customer editing.

	Authorization is enforced.

	Unauthorized users cannot access restricted customer information.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated code has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Customer profile route implemented.

	Profile UI implemented.

	Customer profile API implemented or reused.

	Authorization verified.

	Loading/error/not-found states implemented.

	Tests implemented and passing.

	Manual verification completed.

	No duplicate customer model introduced.

	AI-generated implementation reviewed.

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
