> **Fetched from jira:** [CRM-32](https://batooladnanharah.atlassian.net/browse/CRM-32)  
> *Fetched 2026-08-24T23:10:24.604Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** CUS-004 Edit Customer  
**Type:** Task  
**Status:** In Progress

### Description

User Story

As a support agent, I want to edit customer information so that customer records remain accurate and up to date.

Objective

Allow authorized CRM users to update an existing customer's profile and contact information.

Scope

This story covers:

	Edit customer UI

	Retrieve existing customer data

	Update customer API

	Validation

	Authorization

	PostgreSQL persistence

	Success/error handling

	Audit of customer changes where supported

This story does not cover:

	Creating customers

	Customer interaction history

	Customer notes

	Customer attachments

	Merging duplicate customers

Those are handled by other stories.

UI Requirements

Route

{{/customers/

{id}/edit}}

The edit screen may be implemented as a dedicated page, modal, or drawer, but it should follow the existing CRM UI patterns.

h3. Form

Display the existing customer information:

* Name

* Email

* Phone

* Status

* Branch

* Department

* Notes

Example:

Edit Customer

Name *
[ Ahmed Ali ]

Email
[ ahmed@example.com ]

Phone
[ +201000000000 ]

Status
[ Active ▼ ]

Branch
[ Cairo ▼ ]

Department
[ Customer Support ▼ ]

Notes
[................................]

              [Cancel] [Save Changes]

The fields must be populated using the current customer data before editing.

h2. Form Behavior

* Show a loading state while retrieving the customer.

* Display a not-found state when the customer does not exist.

* Validate modified values before submission.

* Disable Save while the update is being submitted.

* Prevent duplicate submissions.

* Allow the user to cancel without saving changes.

* Display a success message after a successful update.

h2. API

h3. Get Customer

{{GET /api/customers/{id}
}}

This endpoint may already exist from the customer profile implementation.

Update Customer

{{PUT /api/customers/

{id}
}}

Example request:

{
  "name": "Ahmed Ali",
  "email": "ahmed@example.com",
  "phone": "+201000000000",
  "status": "Active",
  "branchId": "branch-id",
  "departmentId": "department-id",
  "notes": "Updated customer notes"
}

The exact request model must follow the approved domain model.

Backend Requirements

The backend must:

	Require authentication.

	Verify the user has permission to edit customers.

	Validate the customer ID.

	Retrieve the customer.

	Return not-found if the customer does not exist.

	Validate the update request.

	Validate referenced branch/department where applicable.

	Apply the update.

	Persist changes through EF Core/PostgreSQL.

	Return the updated customer.

	Record the change for audit/history where required.

Validation

Frontend

Validate:

	Name is required.

	Email must have a valid format when provided.

	Other fields must follow the rules established in CUS-003.

Backend

Repeat authoritative validation.

Do not trust frontend validation.

Duplicate Handling

The update must respect the customer uniqueness rule established in CUS-003.

For example, if email is used as a unique customer identifier, changing a customer's email to an email already belonging to another customer must be rejected.

Do not implement a new duplicate-matching rule in this story.

Authorization

Use the authorization model established in AUTH-003.

At minimum:

	Admin: allowed

	Manager: allowed

	Agent: allowed according to CRM access rules

	Customer: cannot directly edit internal CRM customer records through this endpoint

Customer self-service profile editing, if required later, should be handled separately.

Error Handling

Handle:

	Customer not found

	Invalid customer ID

	Missing required fields

	Invalid email

	Duplicate customer data

	Invalid branch

	Invalid department

	Unauthorized request

	Database failure

	Unexpected server error

Do not expose internal exception details.

Audit

When customer information is changed, record the change according to the audit strategy.

At minimum, the audit record should identify:

	User performing the change

	Customer

	Action

	Timestamp

If practical for the MVP, record the changed fields.

Do not store unnecessary sensitive information.

Concurrency

The implementation should avoid unintentionally overwriting a newer customer update where practical.

For the MVP, a simple update strategy is acceptable unless the existing architecture already supports optimistic concurrency.

Do not introduce complex concurrency infrastructure solely for this story.

Testing

Backend/API Tests

Test:

	Authorized user can update customer.

	Customer not found.

	Invalid customer ID.

	Missing required name.

	Invalid email.

	Duplicate email/customer data.

	Invalid branch.

	Invalid department.

	Unauthorized update.

	Successful update is persisted.

	Database failure is handled.

Frontend Tests

Test:

	Existing customer data loads into the form.

	Loading state appears.

	Not-found state appears.

	Validation works.

	Save works.

	Save button prevents duplicate submission.

	API error is displayed.

	Successful update displays confirmation.

	Cancel returns without saving.

Manual Verification

	Open Customers.

	Open an existing customer.

	Select Edit.

	Change the customer's name/contact information.

	Save.

	Verify the updated information appears.

	Refresh the page.

	Verify the update persisted.

	Try invalid data.

	Try editing a customer that does not exist.

Edge Cases

Handle:

	Customer does not exist.

	Customer deleted before update.

	Empty name.

	Invalid email.

	Duplicate email.

	Very long input.

	No optional values.

	Multiple Save clicks.

	API failure.

	Database failure.

	User loses authorization before saving.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read CUS-001, CUS-002, and CUS-003.

	Reuse the existing Customer entity.

	Reuse the existing customer API conventions.

	Reuse the existing form and validation components.

	Do not create another Customer model/table.

	Do not introduce a new authorization mechanism.

	Respect the duplicate-customer rule established by CUS-003.

	Add/update the EF Core migration only if the existing model requires it.

	Add/update tests.

	Run relevant tests.

	Review the implementation against all acceptance criteria.

	Do not implement customer notes, attachments, or interaction history in this story.

Acceptance Criteria

	Authorized user can open the customer edit screen.

	Existing customer information is loaded.

	User can modify customer information.

	Required fields are validated.

	Email validation works.

	Duplicate customer rules are enforced.

	Customer not found is handled.

	Updated customer is persisted to PostgreSQL.

	Updated information is returned by the API.

	Success feedback is displayed.

	API errors are handled.

	Authorization is enforced.

	Important changes are auditable where required.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated code has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Edit customer UI implemented.

	Customer retrieval implemented/reused.

	Update API implemented.

	PostgreSQL persistence verified.

	Validation implemented.

	Authorization implemented.

	Duplicate handling implemented.

	Error handling implemented.

	Audit behavior implemented where applicable.

	Tests pass.

	Manual verification completed.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/customer-management/CRM-32/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `customer-management`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-32` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Progress`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
CUS-004 Edit Customer
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to edit customer information so that customer records remain accurate and up to date.

Objective

Allow authorized CRM users to update an existing customer's profile and contact information.

Scope

This story covers:

	Edit customer UI

	Retrieve existing customer data

	Update customer API

	Validation

	Authorization

	PostgreSQL persistence

	Success/error handling

	Audit of customer changes where supported

This story does not cover:

	Creating customers

	Customer interaction history

	Customer notes

	Customer attachments

	Merging duplicate customers

Those are handled by other stories.

UI Requirements

Route

{ {/customers/

{id}/edit}}

The edit screen may be implemented as a dedicated page, modal, or drawer, but it should follow the existing CRM UI patterns.

h3. Form

Display the existing customer information:

* Name

* Email

* Phone

* Status

* Branch

* Department

* Notes

Example:

Edit Customer

Name *
[ Ahmed Ali ]

Email
[ ahmed@example.com ]

Phone
[ +201000000000 ]

Status
[ Active ▼ ]

Branch
[ Cairo ▼ ]

Department
[ Customer Support ▼ ]

Notes
[................................]

              [Cancel] [Save Changes]

The fields must be populated using the current customer data before editing.

h2. Form Behavior

* Show a loading state while retrieving the customer.

* Display a not-found state when the customer does not exist.

* Validate modified values before submission.

* Disable Save while the update is being submitted.

* Prevent duplicate submissions.

* Allow the user to cancel without saving changes.

* Display a success message after a successful update.

h2. API

h3. Get Customer

{ {GET /api/customers/{id}
}}

This endpoint may already exist from the customer profile implementation.

Update Customer

{ {PUT /api/customers/

{id}
}}

Example request:

{
  "name": "Ahmed Ali",
  "email": "ahmed@example.com",
  "phone": "+201000000000",
  "status": "Active",
  "branchId": "branch-id",
  "departmentId": "department-id",
  "notes": "Updated customer notes"
}

The exact request model must follow the approved domain model.

Backend Requirements

The backend must:

	Require authentication.

	Verify the user has permission to edit customers.

	Validate the customer ID.

	Retrieve the customer.

	Return not-found if the customer does not exist.

	Validate the update request.

	Validate referenced branch/department where applicable.

	Apply the update.

	Persist changes through EF Core/PostgreSQL.

	Return the updated customer.

	Record the change for audit/history where required.

Validation

Frontend

Validate:

	Name is required.

	Email must have a valid format when provided.

	Other fields must follow the rules established in CUS-003.

Backend

Repeat authoritative validation.

Do not trust frontend validation.

Duplicate Handling

The update must respect the customer uniqueness rule established in CUS-003.

For example, if email is used as a unique customer identifier, changing a customer's email to an email already belonging to another customer must be rejected.

Do not implement a new duplicate-matching rule in this story.

Authorization

Use the authorization model established in AUTH-003.

At minimum:

	Admin: allowed

	Manager: allowed

	Agent: allowed according to CRM access rules

	Customer: cannot directly edit internal CRM customer records through this endpoint

Customer self-service profile editing, if required later, should be handled separately.

Error Handling

Handle:

	Customer not found

	Invalid customer ID

	Missing required fields

	Invalid email

	Duplicate customer data

	Invalid branch

	Invalid department

	Unauthorized request

	Database failure

	Unexpected server error

Do not expose internal exception details.

Audit

When customer information is changed, record the change according to the audit strategy.

At minimum, the audit record should identify:

	User performing the change

	Customer

	Action

	Timestamp

If practical for the MVP, record the changed fields.

Do not store unnecessary sensitive information.

Concurrency

The implementation should avoid unintentionally overwriting a newer customer update where practical.

For the MVP, a simple update strategy is acceptable unless the existing architecture already supports optimistic concurrency.

Do not introduce complex concurrency infrastructure solely for this story.

Testing

Backend/API Tests

Test:

	Authorized user can update customer.

	Customer not found.

	Invalid customer ID.

	Missing required name.

	Invalid email.

	Duplicate email/customer data.

	Invalid branch.

	Invalid department.

	Unauthorized update.

	Successful update is persisted.

	Database failure is handled.

Frontend Tests

Test:

	Existing customer data loads into the form.

	Loading state appears.

	Not-found state appears.

	Validation works.

	Save works.

	Save button prevents duplicate submission.

	API error is displayed.

	Successful update displays confirmation.

	Cancel returns without saving.

Manual Verification

	Open Customers.

	Open an existing customer.

	Select Edit.

	Change the customer's name/contact information.

	Save.

	Verify the updated information appears.

	Refresh the page.

	Verify the update persisted.

	Try invalid data.

	Try editing a customer that does not exist.

Edge Cases

Handle:

	Customer does not exist.

	Customer deleted before update.

	Empty name.

	Invalid email.

	Duplicate email.

	Very long input.

	No optional values.

	Multiple Save clicks.

	API failure.

	Database failure.

	User loses authorization before saving.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read CUS-001, CUS-002, and CUS-003.

	Reuse the existing Customer entity.

	Reuse the existing customer API conventions.

	Reuse the existing form and validation components.

	Do not create another Customer model/table.

	Do not introduce a new authorization mechanism.

	Respect the duplicate-customer rule established by CUS-003.

	Add/update the EF Core migration only if the existing model requires it.

	Add/update tests.

	Run relevant tests.

	Review the implementation against all acceptance criteria.

	Do not implement customer notes, attachments, or interaction history in this story.

Acceptance Criteria

	Authorized user can open the customer edit screen.

	Existing customer information is loaded.

	User can modify customer information.

	Required fields are validated.

	Email validation works.

	Duplicate customer rules are enforced.

	Customer not found is handled.

	Updated customer is persisted to PostgreSQL.

	Updated information is returned by the API.

	Success feedback is displayed.

	API errors are handled.

	Authorization is enforced.

	Important changes are auditable where required.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated code has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Edit customer UI implemented.

	Customer retrieval implemented/reused.

	Update API implemented.

	PostgreSQL persistence verified.

	Validation implemented.

	Authorization implemented.

	Duplicate handling implemented.

	Error handling implemented.

	Audit behavior implemented where applicable.

	Tests pass.

	Manual verification completed.

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
