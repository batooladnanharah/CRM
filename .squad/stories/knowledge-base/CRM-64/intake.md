> **Fetched from jira:** [CRM-64](https://batooladnanharah.atlassian.net/browse/CRM-64)  
> *Fetched 2026-08-28T11:56:10.795Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** KB-001 — Knowledge Base Articles  
**Type:** Task  
**Status:** In Review

### Description

User Story

As a support agent, I want to create and manage knowledge base articles so that common customer questions and support solutions can be documented and reused.

As a customer, I want to read published knowledge base articles so that I can find answers without contacting support.

Objective

Create a simple knowledge base article system supporting:

	Article creation

	Article editing

	Article publishing

	Article unpublishing

	Article viewing

	Draft/published status

	Category association

	Author information

The system should provide a reusable content source for both agents and the Customer Portal.

Scope

This story covers:

	Knowledge base article model

	Article CRUD

	Draft/published status

	Article title

	Article content

	Category reference

	Author

	Published date

	Agent management UI

	Customer-facing article view

	Authorization

	Validation

	Tests

This story does not cover:

	Advanced search

	AI-generated articles

	Article versioning

	Article approval workflows

	Article comments

	Article ratings

	Article analytics

Those can be added later.

Article Model

Suggested model:

KnowledgeBaseArticle

Id
Title
Content
Status
CategoryId
AuthorId
PublishedAt
CreatedAt
UpdatedAt

Status:

Draft
Published

Do not introduce unnecessary statuses for the MVP.

Article Example

Title:
How to Reset Your Password

Category:
Account & Security

Content:

If you forgot your password, follow these steps:

1. Open the login page.
2. Select "Forgot Password".
3. Enter your email address.
4. Follow the instructions sent to your email.

Article UI

Management Page

Recommended route:

/knowledge-base/articles

Knowledge Base

[ + New Article ]

[ Search articles... ]

┌──────────────────────────────┬────────────┬─────────────┬─────────┐
│ Article                      │ Category   │ Status      │ Actions │
├──────────────────────────────┼────────────┼─────────────┼─────────┤
│ How to Reset Your Password   │ Security   │ Published   │ Edit    │
│ Payment Troubleshooting      │ Payments   │ Draft       │ Edit    │
│ Account Verification         │ Accounts   │ Published   │ Edit    │
└──────────────────────────────┴────────────┴─────────────┴─────────┘

Create Article

Recommended route:

/knowledge-base/articles/new

Form:

Create Article

Title *
[ How to Reset Your Password........ ]

Category *
[ Account & Security ▼ ]

Content *
[..................................]
[..................................]
[..................................]

Status
[ Draft ▼ ]

[Cancel] [Save]

For the MVP, a simple text/textarea editor is acceptable.

If the project already has a rich-text editor, reuse it.

Do not spend significant time building a custom rich-text editor.

Article Content

The content must support readable formatting.

Minimum requirement:

	Paragraphs

	Line breaks

Optional if existing editor supports it:

	Headings

	Lists

	Bold

	Links

Do not implement a custom document editor.

Publishing

An article can be:

Draft
Published

Draft articles:

	Visible to authorized CRM users.

	Not visible in the public Customer Portal.

Published articles:

	Visible to authorized CRM users.

	Visible to customers.

Publish Action

Management UI:

Draft

[Edit] [Publish]

After publishing:

Published

[Edit] [Unpublish]

Publishing should set:

PublishedAt

Unpublishing

When an article is unpublished:

Status = Draft

The public article should no longer be accessible.

The content should not be deleted.

Author

The author should be determined by the authenticated CRM user.

Do not allow the frontend to submit an arbitrary AuthorId.

Example:

Created by:
Sara Ahmed

Category

An article may belong to a category.

For this story, category management is handled by:

KB-002 — Categories & Article Management.

Use the existing category entity if already available.

Do not duplicate category logic.

API

List Articles

GET /api/knowledge-base/articles

For CRM users, this may return draft and published articles.

For public/customer access, only published articles should be returned.

Get Article

{{GET /api/knowledge-base/articles/

{id}}}

Authorization rules depend on whether the caller is:

* CRM user

* Customer

* Public user

Published articles may be publicly accessible if the product requirements allow it.

h3. Create

POST /api/knowledge-base/articles

Example:

{
  "title": "How to Reset Your Password",
  "content": "If you forgot your password...",
  "categoryId": "category-id",
  "status": "Draft"
}

The backend determines:

AuthorId = CurrentUser

h3. Update

{{PUT /api/knowledge-base/articles/{id}
}}

Publish

Recommended:

{{POST /api/knowledge-base/articles/

{id}/publish}}

h3. Unpublish

Recommended:

{{POST /api/knowledge-base/articles/{id}
/unpublish}}

Follow the project's existing API conventions if different.

Validation

Title

Required.

Must not be whitespace-only.

Reasonable maximum length should be enforced.

Content

Required.

Must not be whitespace-only.

Category

Required if the SDD defines category as mandatory.

The backend must validate that the category exists.

Authorization

Recommended:

Admin
Manager
Knowledge Base Editor
    ↓
Create/Edit/Publish articles

Agent
    ↓
Read published articles

You may simplify this based on the existing role/permission model.

Customers:

Read published articles only

Use AUTH-003.

Security

The backend must enforce article permissions.

Do not rely on Vue to hide:

Edit
Delete
Publish

buttons.

A user without permission must still be rejected by the API.

Customer Portal

Published articles should be available to the Customer Portal.

Example:

Help Center

Search for an answer...

Popular Articles

How to Reset Your Password
Payment Troubleshooting
How to Update Your Profile

Clicking an article:

How to Reset Your Password

If you forgot your password...

[Was this helpful?]

The feedback functionality belongs to the Customer Portal/reporting stories.

Do not implement it here unless already available.

Article URL

Use a stable identifier.

Example:

/knowledge-base/articles/{id}

A slug may be introduced later.

Do not add slug-generation complexity unless required.

Draft Protection

A customer must never be able to access a draft by guessing its ID.

Example:

GET /api/knowledge-base/articles/{draftId}

must return an appropriate response such as:

404 Not Found

or authorization denial according to the API convention.

Do not reveal that the draft exists.

Pagination

The article management list should support pagination.

Recommended:

page
pageSize

Do not load every article into the browser.

Loading State

Example:

Loading articles...

Create/edit:

Saving article...

Publish:

Publishing...

Empty State

If no articles exist:

No knowledge base articles yet.

[Create Article]

For the customer portal:

No help articles are available yet.

Error Handling

Example:

Unable to load knowledge base articles.

[Retry]

Create/update:

Unable to save the article.

Please try again.

Publish:

Unable to publish the article.

Please try again.

Do not expose database or stack-trace errors.

Arabic / English

The CRM UI must support:

	English

	Arabic

	LTR

	RTL

Article content may be written in Arabic or English.

For the MVP, do not create a translation table or multilingual article versioning system unless explicitly required.

The article itself can contain the language chosen by the author.

Responsive UI

The article list should work on:

	Desktop

	Tablet

	Mobile

Customer-facing article pages should also be mobile friendly.

Recommended mobile management view:

How to Reset Your Password
Security
Published

[Edit]

Database

Use PostgreSQL with EF Core.

Suggested:

KnowledgeBaseArticle
---------------------
Id
Title
Content
Status
CategoryId
AuthorId
PublishedAt
CreatedAt
UpdatedAt

Foreign keys:

CategoryId → KnowledgeBaseCategory
AuthorId   → User

Add indexes where useful.

For example:

Status
CategoryId
PublishedAt

Deletion

For the MVP, prefer soft deletion if the existing project already uses soft deletion.

If soft deletion does not exist, follow the project's established entity lifecycle.

Do not introduce a new deletion architecture only for knowledge base articles.

Ticket Integration

Ticket Management can later reference knowledge base articles.

Example future flow:

Ticket
  ↓
Suggested Article
  ↓
Agent sends solution

Do not implement article-to-ticket suggestions in this story.

That belongs to the AI features.

Testing

Backend/API Tests

Test:

	Authorized user can create article.

	Unauthorized user cannot create article.

	Article validation works.

	Article is persisted.

	Author comes from authenticated user.

	Category is validated.

	Article can be updated.

	Draft can be published.

	Published article can be unpublished.

	Customer can access published article.

	Customer cannot access draft.

	Pagination works.

	Unauthorized management operations are rejected.

Frontend Tests

Test:

	Article list renders.

	Create form renders.

	Validation works.

	Edit works.

	Publish works.

	Unpublish works.

	Loading state works.

	Empty state works.

	Error state works.

	Customer article page renders.

	Draft articles are not shown to customers.

Manual Verification

	Login as Manager.

	Open Knowledge Base.

	Create an article.

	Save as Draft.

	Verify it appears in CRM.

	Verify it does not appear in the Customer Portal.

	Publish it.

	Verify Published status.

	Open Customer Portal.

	Verify the article appears.

	Open the article.

	Unpublish it.

	Verify customers can no longer access it.

	Edit the article.

	Verify changes are saved.

	Test Arabic content.

	Test mobile layout.

Edge Cases

Handle:

	Empty title.

	Empty content.

	Invalid category.

	Deleted category.

	Draft article.

	Unpublished article.

	Unauthorized user.

	Article not found.

	Customer requesting draft.

	Large article content.

	Database failure.

	Concurrent article update.

	Arabic RTL content.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read KB-002 if already created.

	Inspect existing User/Role implementation.

	Inspect 24-story-crm-102.md user story and reuse

	Inspect existing CRUD patterns.

	Inspect Customer Portal structure.

	Reuse existing authorization.

	Reuse existing pagination components.

	Reuse existing form components.

	Reuse existing rich-text editor if available.

	Do not build a custom rich-text editor.

	Do not implement article versioning.

	Do not implement article approval workflows.

	Do not implement AI article generation.

	Do not implement article search yet.

	Ensure draft articles cannot leak to customers.

	Add backend and frontend tests.

	Run relevant tests.

	Review authorization carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized users can create knowledge base articles.

	Authorized users can edit articles.

	Articles support title and content.

	Articles can belong to a category.

	Author is determined server-side.

	Articles support Draft status.

	Articles support Published status.

	Articles can be published.

	Articles can be unpublished.

	Draft articles are not visible to customers.

	Published articles are visible to customers.

	Article permissions are enforced server-side.

	Article list supports pagination.

	Loading state is implemented.

	Empty state is implemented.

	Error handling is implemented.

	Arabic RTL is supported.

	English LTR is supported.

	Customer-facing article page is responsive.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Knowledge base article model implemented.

	CRUD APIs implemented.

	Article management UI implemented.

	Publishing workflow implemented.

	Customer-facing article view implemented.

	Authorization implemented.

	Draft protection verified.

	PostgreSQL persistence verified.

	Pagination implemented.

	Responsive UI implemented.

	Arabic/English support implemented.

	Backend tests pass.

	Frontend tests pass.

	Manual verification completed.

	No unnecessary article-management complexity introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/knowledge-base/CRM-64/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `knowledge-base`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-64` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
KB-001 — Knowledge Base Articles
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to create and manage knowledge base articles so that common customer questions and support solutions can be documented and reused.

As a customer, I want to read published knowledge base articles so that I can find answers without contacting support.

Objective

Create a simple knowledge base article system supporting:

	Article creation

	Article editing

	Article publishing

	Article unpublishing

	Article viewing

	Draft/published status

	Category association

	Author information

The system should provide a reusable content source for both agents and the Customer Portal.

Scope

This story covers:

	Knowledge base article model

	Article CRUD

	Draft/published status

	Article title

	Article content

	Category reference

	Author

	Published date

	Agent management UI

	Customer-facing article view

	Authorization

	Validation

	Tests

This story does not cover:

	Advanced search

	AI-generated articles

	Article versioning

	Article approval workflows

	Article comments

	Article ratings

	Article analytics

Those can be added later.

Article Model

Suggested model:

KnowledgeBaseArticle

Id
Title
Content
Status
CategoryId
AuthorId
PublishedAt
CreatedAt
UpdatedAt

Status:

Draft
Published

Do not introduce unnecessary statuses for the MVP.

Article Example

Title:
How to Reset Your Password

Category:
Account & Security

Content:

If you forgot your password, follow these steps:

1. Open the login page.
2. Select "Forgot Password".
3. Enter your email address.
4. Follow the instructions sent to your email.

Article UI

Management Page

Recommended route:

/knowledge-base/articles

Knowledge Base

[ + New Article ]

[ Search articles... ]

┌──────────────────────────────┬────────────┬─────────────┬─────────┐
│ Article                      │ Category   │ Status      │ Actions │
├──────────────────────────────┼────────────┼─────────────┼─────────┤
│ How to Reset Your Password   │ Security   │ Published   │ Edit    │
│ Payment Troubleshooting      │ Payments   │ Draft       │ Edit    │
│ Account Verification         │ Accounts   │ Published   │ Edit    │
└──────────────────────────────┴────────────┴─────────────┴─────────┘

Create Article

Recommended route:

/knowledge-base/articles/new

Form:

Create Article

Title *
[ How to Reset Your Password........ ]

Category *
[ Account & Security ▼ ]

Content *
[..................................]
[..................................]
[..................................]

Status
[ Draft ▼ ]

[Cancel] [Save]

For the MVP, a simple text/textarea editor is acceptable.

If the project already has a rich-text editor, reuse it.

Do not spend significant time building a custom rich-text editor.

Article Content

The content must support readable formatting.

Minimum requirement:

	Paragraphs

	Line breaks

Optional if existing editor supports it:

	Headings

	Lists

	Bold

	Links

Do not implement a custom document editor.

Publishing

An article can be:

Draft
Published

Draft articles:

	Visible to authorized CRM users.

	Not visible in the public Customer Portal.

Published articles:

	Visible to authorized CRM users.

	Visible to customers.

Publish Action

Management UI:

Draft

[Edit] [Publish]

After publishing:

Published

[Edit] [Unpublish]

Publishing should set:

PublishedAt

Unpublishing

When an article is unpublished:

Status = Draft

The public article should no longer be accessible.

The content should not be deleted.

Author

The author should be determined by the authenticated CRM user.

Do not allow the frontend to submit an arbitrary AuthorId.

Example:

Created by:
Sara Ahmed

Category

An article may belong to a category.

For this story, category management is handled by:

KB-002 — Categories & Article Management.

Use the existing category entity if already available.

Do not duplicate category logic.

API

List Articles

GET /api/knowledge-base/articles

For CRM users, this may return draft and published articles.

For public/customer access, only published articles should be returned.

Get Article

{ {GET /api/knowledge-base/articles/

{id}}}

Authorization rules depend on whether the caller is:

* CRM user

* Customer

* Public user

Published articles may be publicly accessible if the product requirements allow it.

h3. Create

POST /api/knowledge-base/articles

Example:

{
  "title": "How to Reset Your Password",
  "content": "If you forgot your password...",
  "categoryId": "category-id",
  "status": "Draft"
}

The backend determines:

AuthorId = CurrentUser

h3. Update

{ {PUT /api/knowledge-base/articles/{id}
}}

Publish

Recommended:

{ {POST /api/knowledge-base/articles/

{id}/publish}}

h3. Unpublish

Recommended:

{ {POST /api/knowledge-base/articles/{id}
/unpublish}}

Follow the project's existing API conventions if different.

Validation

Title

Required.

Must not be whitespace-only.

Reasonable maximum length should be enforced.

Content

Required.

Must not be whitespace-only.

Category

Required if the SDD defines category as mandatory.

The backend must validate that the category exists.

Authorization

Recommended:

Admin
Manager
Knowledge Base Editor
    ↓
Create/Edit/Publish articles

Agent
    ↓
Read published articles

You may simplify this based on the existing role/permission model.

Customers:

Read published articles only

Use AUTH-003.

Security

The backend must enforce article permissions.

Do not rely on Vue to hide:

Edit
Delete
Publish

buttons.

A user without permission must still be rejected by the API.

Customer Portal

Published articles should be available to the Customer Portal.

Example:

Help Center

Search for an answer...

Popular Articles

How to Reset Your Password
Payment Troubleshooting
How to Update Your Profile

Clicking an article:

How to Reset Your Password

If you forgot your password...

[Was this helpful?]

The feedback functionality belongs to the Customer Portal/reporting stories.

Do not implement it here unless already available.

Article URL

Use a stable identifier.

Example:

/knowledge-base/articles/{id}

A slug may be introduced later.

Do not add slug-generation complexity unless required.

Draft Protection

A customer must never be able to access a draft by guessing its ID.

Example:

GET /api/knowledge-base/articles/{draftId}

must return an appropriate response such as:

404 Not Found

or authorization denial according to the API convention.

Do not reveal that the draft exists.

Pagination

The article management list should support pagination.

Recommended:

page
pageSize

Do not load every article into the browser.

Loading State

Example:

Loading articles...

Create/edit:

Saving article...

Publish:

Publishing...

Empty State

If no articles exist:

No knowledge base articles yet.

[Create Article]

For the customer portal:

No help articles are available yet.

Error Handling

Example:

Unable to load knowledge base articles.

[Retry]

Create/update:

Unable to save the article.

Please try again.

Publish:

Unable to publish the article.

Please try again.

Do not expose database or stack-trace errors.

Arabic / English

The CRM UI must support:

	English

	Arabic

	LTR

	RTL

Article content may be written in Arabic or English.

For the MVP, do not create a translation table or multilingual article versioning system unless explicitly required.

The article itself can contain the language chosen by the author.

Responsive UI

The article list should work on:

	Desktop

	Tablet

	Mobile

Customer-facing article pages should also be mobile friendly.

Recommended mobile management view:

How to Reset Your Password
Security
Published

[Edit]

Database

Use PostgreSQL with EF Core.

Suggested:

KnowledgeBaseArticle
---------------------
Id
Title
Content
Status
CategoryId
AuthorId
PublishedAt
CreatedAt
UpdatedAt

Foreign keys:

CategoryId → KnowledgeBaseCategory
AuthorId   → User

Add indexes where useful.

For example:

Status
CategoryId
PublishedAt

Deletion

For the MVP, prefer soft deletion if the existing project already uses soft deletion.

If soft deletion does not exist, follow the project's established entity lifecycle.

Do not introduce a new deletion architecture only for knowledge base articles.

Ticket Integration

Ticket Management can later reference knowledge base articles.

Example future flow:

Ticket
  ↓
Suggested Article
  ↓
Agent sends solution

Do not implement article-to-ticket suggestions in this story.

That belongs to the AI features.

Testing

Backend/API Tests

Test:

	Authorized user can create article.

	Unauthorized user cannot create article.

	Article validation works.

	Article is persisted.

	Author comes from authenticated user.

	Category is validated.

	Article can be updated.

	Draft can be published.

	Published article can be unpublished.

	Customer can access published article.

	Customer cannot access draft.

	Pagination works.

	Unauthorized management operations are rejected.

Frontend Tests

Test:

	Article list renders.

	Create form renders.

	Validation works.

	Edit works.

	Publish works.

	Unpublish works.

	Loading state works.

	Empty state works.

	Error state works.

	Customer article page renders.

	Draft articles are not shown to customers.

Manual Verification

	Login as Manager.

	Open Knowledge Base.

	Create an article.

	Save as Draft.

	Verify it appears in CRM.

	Verify it does not appear in the Customer Portal.

	Publish it.

	Verify Published status.

	Open Customer Portal.

	Verify the article appears.

	Open the article.

	Unpublish it.

	Verify customers can no longer access it.

	Edit the article.

	Verify changes are saved.

	Test Arabic content.

	Test mobile layout.

Edge Cases

Handle:

	Empty title.

	Empty content.

	Invalid category.

	Deleted category.

	Draft article.

	Unpublished article.

	Unauthorized user.

	Article not found.

	Customer requesting draft.

	Large article content.

	Database failure.

	Concurrent article update.

	Arabic RTL content.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read KB-002 if already created.

	Inspect existing User/Role implementation.

	Inspect 24-story-crm-102.md user story and reuse

	Inspect existing CRUD patterns.

	Inspect Customer Portal structure.

	Reuse existing authorization.

	Reuse existing pagination components.

	Reuse existing form components.

	Reuse existing rich-text editor if available.

	Do not build a custom rich-text editor.

	Do not implement article versioning.

	Do not implement article approval workflows.

	Do not implement AI article generation.

	Do not implement article search yet.

	Ensure draft articles cannot leak to customers.

	Add backend and frontend tests.

	Run relevant tests.

	Review authorization carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized users can create knowledge base articles.

	Authorized users can edit articles.

	Articles support title and content.

	Articles can belong to a category.

	Author is determined server-side.

	Articles support Draft status.

	Articles support Published status.

	Articles can be published.

	Articles can be unpublished.

	Draft articles are not visible to customers.

	Published articles are visible to customers.

	Article permissions are enforced server-side.

	Article list supports pagination.

	Loading state is implemented.

	Empty state is implemented.

	Error handling is implemented.

	Arabic RTL is supported.

	English LTR is supported.

	Customer-facing article page is responsive.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Knowledge base article model implemented.

	CRUD APIs implemented.

	Article management UI implemented.

	Publishing workflow implemented.

	Customer-facing article view implemented.

	Authorization implemented.

	Draft protection verified.

	PostgreSQL persistence verified.

	Pagination implemented.

	Responsive UI implemented.

	Arabic/English support implemented.

	Backend tests pass.

	Frontend tests pass.

	Manual verification completed.

	No unnecessary article-management complexity introduced.

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
