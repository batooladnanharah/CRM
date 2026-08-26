> **Fetched from jira:** [CRM-31](https://batooladnanharah.atlassian.net/browse/CRM-31)  
> *Fetched 2026-08-24T21:49:10.389Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** CUS-003 Create Customer  
**Type:** Task  
**Status:** To Do

### Description

User Story

As a support agent, I want to create a customer so that I can register the customer and associate support tickets with their profile.

Objective

Provide a customer creation form in Vue and a corresponding .NET API that validates and persists customer information in PostgreSQL.

Scope

This story covers:

	Create customer UI

	Customer validation

	Create customer API

	PostgreSQL persistence

	Duplicate customer handling

	Authorization

	Success/error handling

	Audit/history entry where supported by the existing implementation

This story does not cover editing an existing customer. Editing is handled by CUS-004.

UI Requirements

Route

/customers/new

Page

Display:

	Page title: Create Customer

	Customer name

	Email

	Phone

	Status

	Branch

	Department

	Notes

	Save button

	Cancel button

The exact fields should be adjusted to the final domain model if a field has not been approved by the SDD.

Example

Create Customer

Customer Information

Name *
[........................................]

Email
[........................................]

Phone
[........................................]

Status
[ Active ▼ ]

Branch
[ Select branch ▼ ]

Department
[ Select department ▼ ]

Notes
[........................................]
[........................................]

              [Cancel] [Create Customer]

Form Behavior

	Required fields must be clearly indicated.

	Validation errors should appear close to the relevant field.

	Save should be disabled while the request is being submitted.

	Duplicate submissions must be prevented.

	Cancel returns to the customer list.

	Successful creation returns the user to the customer list or customer profile.

API

Create Customer

POST /api/customers

Example request:

{
  "name": "Ahmed Ali",
  "email": "ahmed@example.com",
  "phone": "+201000000000",
  "status": "Active",
  "branchId": "branch-id",
  "departmentId": "department-id",
  "notes": "VIP customer"
}

The exact request model must match the final domain model.

Example response:

{
  "id": "customer-id",
  "name": "Ahmed Ali",
  "email": "ahmed@example.com",
  "phone": "+201000000000",
  "status": "Active"
}

Backend Requirements

The backend must:

	Require authentication.

	Verify the user has permission to create customers.

	Validate the request.

	Validate referenced branch/department when provided.

	Check the configured duplicate-customer rule.

	Create the customer.

	Persist the customer using EF Core/PostgreSQL.

	Return the created customer.

	Return appropriate errors when creation fails.

Duplicate Customer Rule

The system must prevent duplicate customers according to a clearly defined rule.

For the MVP, use a simple configurable/approved uniqueness rule, such as email when an email is provided.

Do not invent a complex customer-matching algorithm.

If the business rule for customer uniqueness is not yet defined, document it as an assumption in the SDD before implementation.

Database Requirements

The Customer entity should contain the fields required by the approved domain model.

At minimum, the implementation should support:

	Id

	Name

	Email

	Phone

	Status

	BranchId where applicable

	DepartmentId where applicable

	Notes where applicable

	CreatedAt

	UpdatedAt

Use appropriate foreign keys for branch and department relationships if those entities are already implemented.

Use EF Core migrations to create/update the PostgreSQL schema.

Validation

Frontend

Validate:

	Name is required.

	Email format is valid when provided.

	Phone format should be validated according to the selected MVP rule when provided.

	Branch/department values must be valid selections when required.

Backend

Repeat authoritative validation.

Do not rely on frontend validation for security or data integrity.

Authorization

Use the authorization model from AUTH-003.

At minimum:

	Admin: allowed

	Manager: allowed

	Agent: allowed

	Customer: not allowed to create internal CRM customer records through this endpoint

The customer portal has its own ticket submission flow.

Error Handling

Handle:

	Missing required fields

	Invalid email

	Invalid phone

	Duplicate customer

	Invalid branch

	Invalid department

	Unauthorized request

	Database failure

	Unexpected API failure

Errors returned to the frontend must be user-friendly.

Technical database/exception details must not be exposed.

Audit

Record customer creation in the audit mechanism defined by the SDD where applicable.

The audit record should identify:

	User

	Action

	Customer

	Timestamp

Do not store unnecessary sensitive information in the audit log.

Testing

Backend/API Tests

Test:

	Valid customer creation.

	Missing name.

	Invalid email.

	Invalid phone when validation is enabled.

	Duplicate customer.

	Invalid branch.

	Invalid department.

	Unauthorized request.

	Database failure handling.

	Successful customer is persisted.

Frontend Tests

Test:

	Form renders.

	Required validation.

	Invalid email validation.

	Successful submission.

	Duplicate/customer API error.

	API error.

	Loading/submitting state.

	Cancel action.

	Successful navigation after creation.

Manual Verification

	Login as an Agent.

	Open Customers.

	Select Add Customer.

	Enter valid information.

	Create customer.

	Verify customer appears in the list.

	Open customer profile.

	Test invalid form input.

	Test duplicate customer.

	Test unauthorized role if applicable.

Edge Cases

Handle:

	Empty name

	Whitespace-only name

	Invalid email

	Empty optional fields

	Duplicate email

	Invalid branch

	Invalid department

	Very long text

	Multiple rapid Save clicks

	API timeout

	Database failure

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read CUS-001 and CUS-002.

	Reuse the existing Customer entity and API conventions.

	Reuse the existing application layout and form components.

	Do not create a second Customer entity.

	Do not introduce a new validation framework unless required.

	Use EF Core and PostgreSQL.

	Add/update database migration.

	Add backend and frontend tests.

	Run all relevant tests.

	Review the implementation against every acceptance criterion.

	Do not implement customer editing in this story.

Acceptance Criteria

	Authorized user can open /customers/new.

	Customer creation form is displayed.

	Required fields are validated.

	Email validation works when email is provided.

	Backend repeats authoritative validation.

	Duplicate customer handling is implemented according to the approved rule.

	Customer is persisted to PostgreSQL.

	API returns the created customer.

	Successful creation displays a success state.

	User can navigate to the customer list/profile after creation.

	API errors are handled.

	Unauthorized users cannot create customers.

	Customer creation is auditable where required.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated code has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Vue creation form implemented.

	Create customer API implemented.

	PostgreSQL persistence implemented.

	Validation implemented.

	Duplicate handling implemented.

	Authorization implemented.

	Error handling implemented.

	Audit behavior implemented where applicable.

	Tests implemented and passing.

	Manual verification completed.

	Database migration verified.

	AI-generated implementation reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/customer-management/CRM-31/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `customer-management`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-31` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `To Do`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
CUS-003 Create Customer
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to create a customer so that I can register the customer and associate support tickets with their profile.

Objective

Provide a customer creation form in Vue and a corresponding .NET API that validates and persists customer information in PostgreSQL.

Scope

This story covers:

	Create customer UI

	Customer validation

	Create customer API

	PostgreSQL persistence

	Duplicate customer handling

	Authorization

	Success/error handling

	Audit/history entry where supported by the existing implementation

This story does not cover editing an existing customer. Editing is handled by CUS-004.

UI Requirements

Route

/customers/new

Page

Display:

	Page title: Create Customer

	Customer name

	Email

	Phone

	Status

	Branch

	Department

	Notes

	Save button

	Cancel button

The exact fields should be adjusted to the final domain model if a field has not been approved by the SDD.

Example

Create Customer

Customer Information

Name *
[........................................]

Email
[........................................]

Phone
[........................................]

Status
[ Active ▼ ]

Branch
[ Select branch ▼ ]

Department
[ Select department ▼ ]

Notes
[........................................]
[........................................]

              [Cancel] [Create Customer]

Form Behavior

	Required fields must be clearly indicated.

	Validation errors should appear close to the relevant field.

	Save should be disabled while the request is being submitted.

	Duplicate submissions must be prevented.

	Cancel returns to the customer list.

	Successful creation returns the user to the customer list or customer profile.

API

Create Customer

POST /api/customers

Example request:

{
  "name": "Ahmed Ali",
  "email": "ahmed@example.com",
  "phone": "+201000000000",
  "status": "Active",
  "branchId": "branch-id",
  "departmentId": "department-id",
  "notes": "VIP customer"
}

The exact request model must match the final domain model.

Example response:

{
  "id": "customer-id",
  "name": "Ahmed Ali",
  "email": "ahmed@example.com",
  "phone": "+201000000000",
  "status": "Active"
}

Backend Requirements

The backend must:

	Require authentication.

	Verify the user has permission to create customers.

	Validate the request.

	Validate referenced branch/department when provided.

	Check the configured duplicate-customer rule.

	Create the customer.

	Persist the customer using EF Core/PostgreSQL.

	Return the created customer.

	Return appropriate errors when creation fails.

Duplicate Customer Rule

The system must prevent duplicate customers according to a clearly defined rule.

For the MVP, use a simple configurable/approved uniqueness rule, such as email when an email is provided.

Do not invent a complex customer-matching algorithm.

If the business rule for customer uniqueness is not yet defined, document it as an assumption in the SDD before implementation.

Database Requirements

The Customer entity should contain the fields required by the approved domain model.

At minimum, the implementation should support:

	Id

	Name

	Email

	Phone

	Status

	BranchId where applicable

	DepartmentId where applicable

	Notes where applicable

	CreatedAt

	UpdatedAt

Use appropriate foreign keys for branch and department relationships if those entities are already implemented.

Use EF Core migrations to create/update the PostgreSQL schema.

Validation

Frontend

Validate:

	Name is required.

	Email format is valid when provided.

	Phone format should be validated according to the selected MVP rule when provided.

	Branch/department values must be valid selections when required.

Backend

Repeat authoritative validation.

Do not rely on frontend validation for security or data integrity.

Authorization

Use the authorization model from AUTH-003.

At minimum:

	Admin: allowed

	Manager: allowed

	Agent: allowed

	Customer: not allowed to create internal CRM customer records through this endpoint

The customer portal has its own ticket submission flow.

Error Handling

Handle:

	Missing required fields

	Invalid email

	Invalid phone

	Duplicate customer

	Invalid branch

	Invalid department

	Unauthorized request

	Database failure

	Unexpected API failure

Errors returned to the frontend must be user-friendly.

Technical database/exception details must not be exposed.

Audit

Record customer creation in the audit mechanism defined by the SDD where applicable.

The audit record should identify:

	User

	Action

	Customer

	Timestamp

Do not store unnecessary sensitive information in the audit log.

Testing

Backend/API Tests

Test:

	Valid customer creation.

	Missing name.

	Invalid email.

	Invalid phone when validation is enabled.

	Duplicate customer.

	Invalid branch.

	Invalid department.

	Unauthorized request.

	Database failure handling.

	Successful customer is persisted.

Frontend Tests

Test:

	Form renders.

	Required validation.

	Invalid email validation.

	Successful submission.

	Duplicate/customer API error.

	API error.

	Loading/submitting state.

	Cancel action.

	Successful navigation after creation.

Manual Verification

	Login as an Agent.

	Open Customers.

	Select Add Customer.

	Enter valid information.

	Create customer.

	Verify customer appears in the list.

	Open customer profile.

	Test invalid form input.

	Test duplicate customer.

	Test unauthorized role if applicable.

Edge Cases

Handle:

	Empty name

	Whitespace-only name

	Invalid email

	Empty optional fields

	Duplicate email

	Invalid branch

	Invalid department

	Very long text

	Multiple rapid Save clicks

	API timeout

	Database failure

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read CUS-001 and CUS-002.

	Reuse the existing Customer entity and API conventions.

	Reuse the existing application layout and form components.

	Do not create a second Customer entity.

	Do not introduce a new validation framework unless required.

	Use EF Core and PostgreSQL.

	Add/update database migration.

	Add backend and frontend tests.

	Run all relevant tests.

	Review the implementation against every acceptance criterion.

	Do not implement customer editing in this story.

Acceptance Criteria

	Authorized user can open /customers/new.

	Customer creation form is displayed.

	Required fields are validated.

	Email validation works when email is provided.

	Backend repeats authoritative validation.

	Duplicate customer handling is implemented according to the approved rule.

	Customer is persisted to PostgreSQL.

	API returns the created customer.

	Successful creation displays a success state.

	User can navigate to the customer list/profile after creation.

	API errors are handled.

	Unauthorized users cannot create customers.

	Customer creation is auditable where required.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated code has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Vue creation form implemented.

	Create customer API implemented.

	PostgreSQL persistence implemented.

	Validation implemented.

	Duplicate handling implemented.

	Authorization implemented.

	Error handling implemented.

	Audit behavior implemented where applicable.

	Tests implemented and passing.

	Manual verification completed.

	Database migration verified.

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
