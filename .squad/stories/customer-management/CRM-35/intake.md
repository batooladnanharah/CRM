> **Fetched from jira:** [CRM-35](https://batooladnanharah.atlassian.net/browse/CRM-35)  
> *Fetched 2026-08-25T00:03:32.522Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** CUS-007 Notes  
**Type:** Task  
**Status:** In Progress

### Description

User Story

As a support agent, I want to add and view notes on a customer so that important internal information can be recorded for future support interactions.

Objective

Allow authorized CRM users to create and view internal notes associated with a customer.

Notes are internal CRM information and must not automatically be exposed to the customer through the customer portal.

Scope

This story covers:

	View customer notes

	Add a note

	Edit a note created by the current user where appropriate

	Delete a note where permitted

	Note author

	Note timestamp

	Authorization

	Validation

	Error handling

This story does not cover customer attachments.

UI Requirements

Add a Notes section/tab to the customer profile.

Example:

Customer Profile

[Overview] [Tickets] [Interactions] [Notes] [Attachments]

Notes                                      [+ Add Note]

────────────────────────────────────────────────

Ahmed — Today 10:45
Customer prefers communication by email.

[Edit] [Delete]

────────────────────────────────────────────────

Sara — Yesterday 15:20
Customer reported a previous billing issue.

[Edit] [Delete]

Add Note

Provide a form/modal/drawer:

Add Customer Note

Note *
[........................................]
[........................................]
[........................................]

              [Cancel] [Save Note]

The existing CRM UI patterns should be reused.

Note Information

Each note should display:

	Note content

	Author

	Created date/time

	Updated date/time where applicable

Newest notes should appear first.

API

Get Notes

{{GET /api/customers/

{id}/notes}}

h3. Create Note

{{POST /api/customers/{id}
/notes}}

Example request:

{
  "content": "Customer prefers communication by email."
}

Update Note

{{PUT /api/customers/

{customerId}/notes/{noteId}}}

h3. Delete Note

{{DELETE /api/customers/{customerId}
/notes/

{noteId}
}}

The exact endpoint structure may be adjusted to match the existing API conventions.

Backend Requirements

The backend must:

	Require authentication.

	Validate the customer ID.

	Verify the user can access the customer.

	Validate note content.

	Create the note associated with the customer.

	Associate the note with the authenticated user.

	Persist the note through EF Core/PostgreSQL.

	Return the created/updated note.

	Prevent unauthorized note access or modification.

Data Model

A simple model is sufficient:

CustomerNote

Id
CustomerId
AuthorId
Content
CreatedAt
UpdatedAt

Use foreign keys to Customer and User.

Do not create unnecessary note metadata for the MVP.

Validation

Frontend

	Note content is required.

	Whitespace-only content is invalid.

	Note length should have a reasonable maximum.

	Save should be disabled while submitting.

Backend

Repeat authoritative validation.

The backend must reject invalid note content even if frontend validation is bypassed.

Authorization

Notes are internal CRM information.

At minimum:

	Admin: allowed.

	Manager: allowed.

	Agent: allowed according to customer access rules.

	Customer: cannot access internal customer notes.

For editing/deleting:

	Prefer allowing the note author to modify/delete their own note.

	Admin/Manager may modify/delete notes according to the application's permission model.

Keep the MVP permission model simple.

Privacy

Customer notes must not be returned through customer-facing APIs unless a future requirement explicitly makes them customer-visible.

Internal notes may contain sensitive operational information, so access must be protected.

Loading State

Display a loading state while notes are retrieved.

Empty State

If there are no notes:

No notes yet.

Add an internal note to keep important customer information.

Error Handling

Handle:

	Customer not found

	Note not found

	Invalid note

	Unauthorized access

	Database failure

	Network/API failure

Display user-friendly messages.

Do not expose internal exception details.

Audit

Important note operations should be auditable where required:

	Note created

	Note updated

	Note deleted

The audit record should identify:

	User

	Customer

	Action

	Timestamp

Do not duplicate the entire note content in audit logs unless required.

Testing

Backend/API Tests

Test:

	Authorized agent can create a note.

	Note is associated with the correct customer.

	Note author is the authenticated user.

	Empty note is rejected.

	Whitespace-only note is rejected.

	Unauthorized user cannot access notes.

	Customer cannot access internal notes.

	Author can update their note.

	Unauthorized user cannot update another user's note unless permitted.

	Note deletion authorization works.

	Customer not found is handled.

	Database failure is handled.

Frontend Tests

Test:

	Notes section renders.

	Notes are displayed.

	Add Note form opens.

	Required validation works.

	Note can be created.

	Success state appears.

	Error state appears.

	Edit works where permitted.

	Delete confirmation works.

	Empty state appears.

Manual Verification

	Open a customer.

	Open Notes.

	Add an internal note.

	Verify the note appears.

	Edit the note.

	Delete the note.

	Login as another role.

	Verify authorization behavior.

	Verify the note is not exposed through customer-facing functionality.

Edge Cases

Handle:

	Empty note

	Whitespace-only note

	Very long note

	Customer does not exist

	Note does not exist

	User loses authorization

	Duplicate submissions

	API failure

	Database failure

	Multiple notes created at the same time

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read CUS-005 and existing customer profile implementation.

	Reuse existing Customer and User entities.

	Reuse existing UI components and authorization.

	Do not expose internal notes through customer-facing APIs.

	Do not create a separate authentication mechanism.

	Do not introduce unnecessary dependencies.

	Add backend and frontend tests.

	Run relevant tests.

	Review authorization and privacy behavior carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized CRM users can view customer notes.

	Authorized users can create notes.

	Note is associated with the correct customer.

	Note author is recorded.

	Note creation time is recorded.

	Note content is validated.

	Authorized users can edit notes according to the permission model.

	Authorized users can delete notes according to the permission model.

	Unauthorized users cannot access internal notes.

	Customers cannot access internal CRM notes.

	Loading state is implemented.

	Empty state is implemented.

	Error handling is implemented.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated code has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Notes UI implemented.

	Notes API implemented.

	PostgreSQL persistence implemented.

	Authorization implemented.

	Validation implemented.

	Privacy rules verified.

	Create/edit/delete behavior implemented as required.

	Tests pass.

	Manual verification completed.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/customer-management/CRM-35/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `customer-management`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-35` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Progress`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
CUS-007 Notes
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to add and view notes on a customer so that important internal information can be recorded for future support interactions.

Objective

Allow authorized CRM users to create and view internal notes associated with a customer.

Notes are internal CRM information and must not automatically be exposed to the customer through the customer portal.

Scope

This story covers:

	View customer notes

	Add a note

	Edit a note created by the current user where appropriate

	Delete a note where permitted

	Note author

	Note timestamp

	Authorization

	Validation

	Error handling

This story does not cover customer attachments.

UI Requirements

Add a Notes section/tab to the customer profile.

Example:

Customer Profile

[Overview] [Tickets] [Interactions] [Notes] [Attachments]

Notes                                      [+ Add Note]

────────────────────────────────────────────────

Ahmed — Today 10:45
Customer prefers communication by email.

[Edit] [Delete]

────────────────────────────────────────────────

Sara — Yesterday 15:20
Customer reported a previous billing issue.

[Edit] [Delete]

Add Note

Provide a form/modal/drawer:

Add Customer Note

Note *
[........................................]
[........................................]
[........................................]

              [Cancel] [Save Note]

The existing CRM UI patterns should be reused.

Note Information

Each note should display:

	Note content

	Author

	Created date/time

	Updated date/time where applicable

Newest notes should appear first.

API

Get Notes

{ {GET /api/customers/

{id}/notes}}

h3. Create Note

{ {POST /api/customers/{id}
/notes}}

Example request:

{
  "content": "Customer prefers communication by email."
}

Update Note

{ {PUT /api/customers/

{customerId}/notes/{noteId}}}

h3. Delete Note

{ {DELETE /api/customers/{customerId}
/notes/

{noteId}
}}

The exact endpoint structure may be adjusted to match the existing API conventions.

Backend Requirements

The backend must:

	Require authentication.

	Validate the customer ID.

	Verify the user can access the customer.

	Validate note content.

	Create the note associated with the customer.

	Associate the note with the authenticated user.

	Persist the note through EF Core/PostgreSQL.

	Return the created/updated note.

	Prevent unauthorized note access or modification.

Data Model

A simple model is sufficient:

CustomerNote

Id
CustomerId
AuthorId
Content
CreatedAt
UpdatedAt

Use foreign keys to Customer and User.

Do not create unnecessary note metadata for the MVP.

Validation

Frontend

	Note content is required.

	Whitespace-only content is invalid.

	Note length should have a reasonable maximum.

	Save should be disabled while submitting.

Backend

Repeat authoritative validation.

The backend must reject invalid note content even if frontend validation is bypassed.

Authorization

Notes are internal CRM information.

At minimum:

	Admin: allowed.

	Manager: allowed.

	Agent: allowed according to customer access rules.

	Customer: cannot access internal customer notes.

For editing/deleting:

	Prefer allowing the note author to modify/delete their own note.

	Admin/Manager may modify/delete notes according to the application's permission model.

Keep the MVP permission model simple.

Privacy

Customer notes must not be returned through customer-facing APIs unless a future requirement explicitly makes them customer-visible.

Internal notes may contain sensitive operational information, so access must be protected.

Loading State

Display a loading state while notes are retrieved.

Empty State

If there are no notes:

No notes yet.

Add an internal note to keep important customer information.

Error Handling

Handle:

	Customer not found

	Note not found

	Invalid note

	Unauthorized access

	Database failure

	Network/API failure

Display user-friendly messages.

Do not expose internal exception details.

Audit

Important note operations should be auditable where required:

	Note created

	Note updated

	Note deleted

The audit record should identify:

	User

	Customer

	Action

	Timestamp

Do not duplicate the entire note content in audit logs unless required.

Testing

Backend/API Tests

Test:

	Authorized agent can create a note.

	Note is associated with the correct customer.

	Note author is the authenticated user.

	Empty note is rejected.

	Whitespace-only note is rejected.

	Unauthorized user cannot access notes.

	Customer cannot access internal notes.

	Author can update their note.

	Unauthorized user cannot update another user's note unless permitted.

	Note deletion authorization works.

	Customer not found is handled.

	Database failure is handled.

Frontend Tests

Test:

	Notes section renders.

	Notes are displayed.

	Add Note form opens.

	Required validation works.

	Note can be created.

	Success state appears.

	Error state appears.

	Edit works where permitted.

	Delete confirmation works.

	Empty state appears.

Manual Verification

	Open a customer.

	Open Notes.

	Add an internal note.

	Verify the note appears.

	Edit the note.

	Delete the note.

	Login as another role.

	Verify authorization behavior.

	Verify the note is not exposed through customer-facing functionality.

Edge Cases

Handle:

	Empty note

	Whitespace-only note

	Very long note

	Customer does not exist

	Note does not exist

	User loses authorization

	Duplicate submissions

	API failure

	Database failure

	Multiple notes created at the same time

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read CUS-005 and existing customer profile implementation.

	Reuse existing Customer and User entities.

	Reuse existing UI components and authorization.

	Do not expose internal notes through customer-facing APIs.

	Do not create a separate authentication mechanism.

	Do not introduce unnecessary dependencies.

	Add backend and frontend tests.

	Run relevant tests.

	Review authorization and privacy behavior carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized CRM users can view customer notes.

	Authorized users can create notes.

	Note is associated with the correct customer.

	Note author is recorded.

	Note creation time is recorded.

	Note content is validated.

	Authorized users can edit notes according to the permission model.

	Authorized users can delete notes according to the permission model.

	Unauthorized users cannot access internal notes.

	Customers cannot access internal CRM notes.

	Loading state is implemented.

	Empty state is implemented.

	Error handling is implemented.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated code has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Notes UI implemented.

	Notes API implemented.

	PostgreSQL persistence implemented.

	Authorization implemented.

	Validation implemented.

	Privacy rules verified.

	Create/edit/delete behavior implemented as required.

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
