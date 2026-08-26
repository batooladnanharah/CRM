> **Fetched from jira:** [CRM-36](https://batooladnanharah.atlassian.net/browse/CRM-36)  
> *Fetched 2026-08-25T00:26:07.416Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** CUS-008 Attachments  
**Type:** Task  
**Status:** In Progress

### Description

User Story

As a support agent, I want to upload and view files attached to a customer so that relevant documents and supporting information are available from the customer profile.

Objective

Allow authorized CRM users to attach files to a customer and view/download existing attachments.

Scope

This MVP story covers:

	Upload customer attachment

	View attachment list

	Download attachment

	Delete attachment where authorized

	File validation

	Authorization

	Basic metadata

	Error handling

This story does not cover:

	Advanced document management

	File versioning

	External cloud storage

	Virus scanning services

	Document preview for every file type

Those can be added later if required.

UI Requirements

Add an Attachments section/tab to the customer profile.

Example:

Customer Profile

[Overview] [Tickets] [Interactions] [Notes] [Attachments]

Attachments                              [+ Upload File]

────────────────────────────────────────────────────────

📄 customer-contract.pdf
   PDF · 2.4 MB
   Uploaded by Ahmed · Today 10:30

   [Download] [Delete]

────────────────────────────────────────────────────────

📷 customer-photo.jpg
   JPG · 1.2 MB
   Uploaded by Sara · Yesterday

   [Download] [Delete]

Upload UI

Provide an upload form/modal:

Upload Attachment

[ Select File ]

Maximum file size: configured application limit

Selected:
customer-contract.pdf

              [Cancel] [Upload]

The UI should show:

	Selected file name

	File size

	File type

	Upload progress where practical

	Success state

	Error state

Prevent duplicate upload submissions.

File Metadata

Store:

	Attachment ID

	Customer ID

	Original file name

	Stored file name/identifier

	Content type

	File size

	Uploaded by

	Created date

Do not store unnecessary metadata.

API

List Attachments

{{GET /api/customers/

{id}/attachments}}

h3. Upload Attachment

{{POST /api/customers/{id}
/attachments}}

Use multipart/form-data.

Download Attachment

{{GET /api/customers/

{id}/attachments/{attachmentId}/download}}

h3. Delete Attachment

{{DELETE /api/customers/{id}
/attachments/

{attachmentId}
}}

The exact endpoint conventions may be adjusted to match the existing API architecture.

Backend Requirements

The backend must:

	Require authentication.

	Validate the customer ID.

	Verify customer access.

	Validate the uploaded file.

	Validate file size.

	Validate allowed file types.

	Store the file safely.

	Store attachment metadata in PostgreSQL.

	Associate the attachment with the customer.

	Associate the attachment with the uploading user.

	Return attachment metadata.

	Prevent unauthorized downloads/deletions.

File Storage

For the MVP, use a simple configurable file-storage abstraction.

Example:

IFileStorage
    |
    └── LocalFileStorage

The database should store metadata and a storage reference rather than unnecessarily storing large binary files directly in PostgreSQL.

The storage implementation must be configurable so that cloud/object storage can be introduced later without changing the customer domain model.

Do not introduce external cloud storage unless already available.

File Validation

The backend must validate:

	File is present.

	File size is within the configured maximum.

	File type is allowed.

	File name is safe.

	Storage path cannot be controlled directly by the user.

Do not trust only the file extension supplied by the client.

The exact allowed file types and maximum size should be configurable.

For the MVP, reasonable document/image types may be supported, but the final allowed list should be documented as an application configuration decision.

Security

	Never use the original filename directly as the physical storage filename.

	Generate a safe unique storage identifier.

	Prevent path traversal.

	Do not expose internal storage paths.

	Require authorization for downloads.

	Require authorization for deletion.

	Do not expose attachments belonging to another customer.

	Do not commit uploaded files to Git.

	Do not expose server filesystem paths to the frontend.

Authorization

Use AUTH-003.

At minimum:

	Admin: allowed

	Manager: allowed

	Agent: allowed according to customer access

	Customer: restricted to attachments they are explicitly permitted to access through the customer portal

Internal CRM attachments must not automatically become customer-visible.

Database Model

A simple model is sufficient:

CustomerAttachment

Id
CustomerId
UploadedByUserId
OriginalFileName
StorageKey
ContentType
FileSize
CreatedAt

Use foreign keys to Customer and User.

Do not store the file binary in PostgreSQL unless the architecture explicitly chooses this approach.

Error Handling

Handle:

	No file uploaded

	File too large

	Unsupported file type

	Customer not found

	Attachment not found

	Unauthorized access

	Upload failure

	Download failure

	Storage failure

	Database failure

Display user-friendly messages.

Do not expose filesystem or infrastructure details.

Loading States

Support:

	Loading attachments

	Uploading

	Downloading where practical

	Deleting

	Refreshing after successful upload

Empty State

No attachments yet.

Upload a document or file related to this customer.

[Upload File]

Testing

Backend/API Tests

Test:

	Authorized user can list attachments.

	Authorized user can upload an allowed file.

	Missing file is rejected.

	Oversized file is rejected.

	Unsupported file type is rejected.

	Customer not found is handled.

	Attachment not found is handled.

	Unauthorized download is rejected.

	Unauthorized delete is rejected.

	Uploaded metadata is persisted.

	Storage failure is handled.

	Database failure is handled.

Frontend Tests

Test:

	Attachment list renders.

	Empty state renders.

	Upload dialog works.

	File selection works.

	Validation errors display.

	Upload success refreshes the list.

	Upload failure displays an error.

	Download action works.

	Delete confirmation works.

	Delete refreshes the list.

Manual Verification

	Open a customer profile.

	Open Attachments.

	Upload a valid file.

	Verify it appears in the list.

	Download it.

	Verify the downloaded file is correct.

	Delete the attachment.

	Verify it disappears.

	Try an unsupported file.

	Try a file exceeding the configured limit.

	Verify unauthorized users cannot access attachments.

Edge Cases

Handle:

	Empty upload

	Unsupported file type

	Large file

	Duplicate filenames

	Special characters in filenames

	Very long filenames

	Customer does not exist

	Attachment does not exist

	Storage unavailable

	Database unavailable

	User loses authorization

	Upload interrupted

	Multiple upload clicks

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read CUS-005 and existing customer profile implementation.

	Reuse existing authentication and authorization.

	Reuse existing UI components.

	Use a file-storage abstraction.

	Do not introduce cloud storage unless already configured.

	Do not store uploaded files in Git.

	Do not expose filesystem paths.

	Do not create another Customer entity.

	Add backend and frontend tests.

	Run relevant tests.

	Review file-upload security carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized users can view customer attachments.

	Authorized users can upload attachments.

	File size is validated.

	File type is validated.

	Attachment metadata is persisted.

	Attachment is associated with the correct customer.

	Uploading user is recorded.

	Authorized users can download attachments.

	Unauthorized users cannot download restricted attachments.

	Authorized users can delete attachments according to the permission model.

	Storage paths are not exposed.

	Original filenames are not used directly as storage filenames.

	Loading/empty/error states are implemented.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated code has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Attachment UI implemented.

	Upload API implemented.

	Download API implemented.

	Delete API implemented.

	File storage abstraction implemented.

	PostgreSQL metadata persistence implemented.

	File validation implemented.

	Authorization implemented.

	Upload security reviewed.

	Tests pass.

	Manual verification completed.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/customer-management/CRM-36/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `customer-management`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-36` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Progress`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
CUS-008 Attachments
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to upload and view files attached to a customer so that relevant documents and supporting information are available from the customer profile.

Objective

Allow authorized CRM users to attach files to a customer and view/download existing attachments.

Scope

This MVP story covers:

	Upload customer attachment

	View attachment list

	Download attachment

	Delete attachment where authorized

	File validation

	Authorization

	Basic metadata

	Error handling

This story does not cover:

	Advanced document management

	File versioning

	External cloud storage

	Virus scanning services

	Document preview for every file type

Those can be added later if required.

UI Requirements

Add an Attachments section/tab to the customer profile.

Example:

Customer Profile

[Overview] [Tickets] [Interactions] [Notes] [Attachments]

Attachments                              [+ Upload File]

────────────────────────────────────────────────────────

📄 customer-contract.pdf
   PDF · 2.4 MB
   Uploaded by Ahmed · Today 10:30

   [Download] [Delete]

────────────────────────────────────────────────────────

📷 customer-photo.jpg
   JPG · 1.2 MB
   Uploaded by Sara · Yesterday

   [Download] [Delete]

Upload UI

Provide an upload form/modal:

Upload Attachment

[ Select File ]

Maximum file size: configured application limit

Selected:
customer-contract.pdf

              [Cancel] [Upload]

The UI should show:

	Selected file name

	File size

	File type

	Upload progress where practical

	Success state

	Error state

Prevent duplicate upload submissions.

File Metadata

Store:

	Attachment ID

	Customer ID

	Original file name

	Stored file name/identifier

	Content type

	File size

	Uploaded by

	Created date

Do not store unnecessary metadata.

API

List Attachments

{ {GET /api/customers/

{id}/attachments}}

h3. Upload Attachment

{ {POST /api/customers/{id}
/attachments}}

Use multipart/form-data.

Download Attachment

{ {GET /api/customers/

{id}/attachments/{attachmentId}/download}}

h3. Delete Attachment

{ {DELETE /api/customers/{id}
/attachments/

{attachmentId}
}}

The exact endpoint conventions may be adjusted to match the existing API architecture.

Backend Requirements

The backend must:

	Require authentication.

	Validate the customer ID.

	Verify customer access.

	Validate the uploaded file.

	Validate file size.

	Validate allowed file types.

	Store the file safely.

	Store attachment metadata in PostgreSQL.

	Associate the attachment with the customer.

	Associate the attachment with the uploading user.

	Return attachment metadata.

	Prevent unauthorized downloads/deletions.

File Storage

For the MVP, use a simple configurable file-storage abstraction.

Example:

IFileStorage
    |
    └── LocalFileStorage

The database should store metadata and a storage reference rather than unnecessarily storing large binary files directly in PostgreSQL.

The storage implementation must be configurable so that cloud/object storage can be introduced later without changing the customer domain model.

Do not introduce external cloud storage unless already available.

File Validation

The backend must validate:

	File is present.

	File size is within the configured maximum.

	File type is allowed.

	File name is safe.

	Storage path cannot be controlled directly by the user.

Do not trust only the file extension supplied by the client.

The exact allowed file types and maximum size should be configurable.

For the MVP, reasonable document/image types may be supported, but the final allowed list should be documented as an application configuration decision.

Security

	Never use the original filename directly as the physical storage filename.

	Generate a safe unique storage identifier.

	Prevent path traversal.

	Do not expose internal storage paths.

	Require authorization for downloads.

	Require authorization for deletion.

	Do not expose attachments belonging to another customer.

	Do not commit uploaded files to Git.

	Do not expose server filesystem paths to the frontend.

Authorization

Use AUTH-003.

At minimum:

	Admin: allowed

	Manager: allowed

	Agent: allowed according to customer access

	Customer: restricted to attachments they are explicitly permitted to access through the customer portal

Internal CRM attachments must not automatically become customer-visible.

Database Model

A simple model is sufficient:

CustomerAttachment

Id
CustomerId
UploadedByUserId
OriginalFileName
StorageKey
ContentType
FileSize
CreatedAt

Use foreign keys to Customer and User.

Do not store the file binary in PostgreSQL unless the architecture explicitly chooses this approach.

Error Handling

Handle:

	No file uploaded

	File too large

	Unsupported file type

	Customer not found

	Attachment not found

	Unauthorized access

	Upload failure

	Download failure

	Storage failure

	Database failure

Display user-friendly messages.

Do not expose filesystem or infrastructure details.

Loading States

Support:

	Loading attachments

	Uploading

	Downloading where practical

	Deleting

	Refreshing after successful upload

Empty State

No attachments yet.

Upload a document or file related to this customer.

[Upload File]

Testing

Backend/API Tests

Test:

	Authorized user can list attachments.

	Authorized user can upload an allowed file.

	Missing file is rejected.

	Oversized file is rejected.

	Unsupported file type is rejected.

	Customer not found is handled.

	Attachment not found is handled.

	Unauthorized download is rejected.

	Unauthorized delete is rejected.

	Uploaded metadata is persisted.

	Storage failure is handled.

	Database failure is handled.

Frontend Tests

Test:

	Attachment list renders.

	Empty state renders.

	Upload dialog works.

	File selection works.

	Validation errors display.

	Upload success refreshes the list.

	Upload failure displays an error.

	Download action works.

	Delete confirmation works.

	Delete refreshes the list.

Manual Verification

	Open a customer profile.

	Open Attachments.

	Upload a valid file.

	Verify it appears in the list.

	Download it.

	Verify the downloaded file is correct.

	Delete the attachment.

	Verify it disappears.

	Try an unsupported file.

	Try a file exceeding the configured limit.

	Verify unauthorized users cannot access attachments.

Edge Cases

Handle:

	Empty upload

	Unsupported file type

	Large file

	Duplicate filenames

	Special characters in filenames

	Very long filenames

	Customer does not exist

	Attachment does not exist

	Storage unavailable

	Database unavailable

	User loses authorization

	Upload interrupted

	Multiple upload clicks

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read CUS-005 and existing customer profile implementation.

	Reuse existing authentication and authorization.

	Reuse existing UI components.

	Use a file-storage abstraction.

	Do not introduce cloud storage unless already configured.

	Do not store uploaded files in Git.

	Do not expose filesystem paths.

	Do not create another Customer entity.

	Add backend and frontend tests.

	Run relevant tests.

	Review file-upload security carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized users can view customer attachments.

	Authorized users can upload attachments.

	File size is validated.

	File type is validated.

	Attachment metadata is persisted.

	Attachment is associated with the correct customer.

	Uploading user is recorded.

	Authorized users can download attachments.

	Unauthorized users cannot download restricted attachments.

	Authorized users can delete attachments according to the permission model.

	Storage paths are not exposed.

	Original filenames are not used directly as storage filenames.

	Loading/empty/error states are implemented.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated code has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Attachment UI implemented.

	Upload API implemented.

	Download API implemented.

	Delete API implemented.

	File storage abstraction implemented.

	PostgreSQL metadata persistence implemented.

	File validation implemented.

	Authorization implemented.

	Upload security reviewed.

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
