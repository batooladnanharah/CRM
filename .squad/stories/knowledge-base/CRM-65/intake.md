> **Fetched from jira:** [CRM-65](https://batooladnanharah.atlassian.net/browse/CRM-65)  
> *Fetched 2026-08-28T15:33:14.638Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** KB-002 — Categories & Article Management  
**Type:** Task  
**Status:** In Review

### Description

User Story

As a support manager, I want to organize knowledge base articles into categories so that agents and customers can find related information more easily.

Objective

Provide simple category management and connect knowledge base articles to categories.

The MVP should support:

	Create categories

	Edit categories

	Activate/deactivate categories

	Assign articles to categories

	Filter articles by category

	Display categories in the customer knowledge base

Scope

This story covers:

	Knowledge base category model

	Category CRUD

	Category status

	Article-category relationship

	Category filtering

	Category management UI

	Customer category navigation

	Validation

	Authorization

	Tests

This story does not cover:

	Nested category trees

	Unlimited hierarchy

	AI categorization

	Advanced taxonomy

	Automatic category generation

For the 2-day implementation, use flat categories.

Category Model

Suggested model:

KnowledgeBaseCategory

Id
Name
Description
IsActive
CreatedAt
UpdatedAt

Example:

Account & Security
Payments
Orders
Technical Support
Getting Started

Flat Categories

Categories should not have parent/child relationships for the MVP.

Do not implement:

Technical Support
    ├── Login
    │   ├── Password
    │   └── MFA
    └── Connectivity

Instead:

Login
Password
MFA
Connectivity

This keeps the implementation simple and makes search/filtering easier.

Category Management UI

Recommended route:

/knowledge-base/categories

Knowledge Base Categories

[ + New Category ]

┌──────────────────────┬──────────┬─────────┐
│ Category             │ Status   │ Actions │
├──────────────────────┼──────────┼─────────┤
│ Account & Security   │ Active   │ Edit    │
│ Payments             │ Active   │ Edit    │
│ Technical Support    │ Active   │ Edit    │
│ Legacy               │ Inactive │ Edit    │
└──────────────────────┴──────────┴─────────┘

Create Category

Create Category

Name *
[ Account & Security................. ]

Description
[ Account and security related help. ]

Status
[ Active ]

[Cancel] [Save]

Keep the form simple.

Edit Category

The same form should be reused for create and edit.

Do not create separate components unnecessarily.

Recommended Vue structure:

KnowledgeBaseCategoryList.vue
KnowledgeBaseCategoryForm.vue

Or follow the project's existing component conventions.

Category Status

Categories support:

Active
Inactive

Inactive categories should not be available for new article assignments.

Existing articles should not automatically be deleted when a category becomes inactive.

Category Assignment

An article should reference one category.

Example:

Article:
How to Reset Your Password

Category:
Account & Security

The article creation/edit form should provide:

Category *

[ Account & Security ▼ ]

Only active categories should be available for new article assignments.

Existing Articles

When a category is deactivated:

Category:
Payments → Inactive

Existing articles:
Remain associated

The article itself remains available.

The AI should not automatically move articles to another category.

Category Deletion

For the MVP, prefer deactivation rather than deletion.

Reason:

Articles may reference the category.

Recommended UI:

[Deactivate]

instead of:

[Delete]

If the project already has soft deletion conventions, follow them.

API

List Categories

GET /api/knowledge-base/categories

Example:

{
  "items": [
    {
      "id": "category-id",
      "name": "Account & Security",
      "description": "Account and security help.",
      "isActive": true
    }
  ]
}

Get Category

{{GET /api/knowledge-base/categories/

{id}}}

h3. Create Category

POST /api/knowledge-base/categories

Example:

{
  "name": "Account & Security",
  "description": "Account and security help."
}

The backend should default the category to active unless the existing API convention requires otherwise.

h3. Update Category

{{PUT /api/knowledge-base/categories/{id}
}}

Activate/Deactivate

Use:

{{PATCH /api/knowledge-base/categories/

{id}/status}}

or the project's existing API convention.

h2. Article Filtering

The article management API should support category filtering.

Example:

{{GET /api/knowledge-base/articles?categoryId={id}
}}

The UI can provide:

Category

[ All Categories ▼ ]

Example:

[ All Categories ▼ ]

Showing:
Account & Security

3 articles

Filtering must happen server-side.

Do not load every article into Vue and filter them in memory.

Customer Knowledge Base

Customers should be able to browse categories.

Example:

Help Center

What can we help you with?

┌──────────────────────┐
│ Account & Security   │
│ 12 articles          │
└──────────────────────┘

┌──────────────────────┐
│ Payments             │
│ 8 articles           │
└──────────────────────┘

┌──────────────────────┐
│ Technical Support    │
│ 15 articles          │
└──────────────────────┘

Only active categories with published articles should be displayed to customers.

Category Article Count

The customer UI may display:

Account & Security
12 articles

The count should be calculated by the backend.

Do not fetch all articles simply to calculate counts in Vue.

Customer Category Page

Recommended route:

/knowledge-base/category/{id}

Example:

Account & Security

How to Reset Your Password
How to Change Your Email
How to Enable MFA

Only published articles should appear.

Article Management

The existing KB-001 article form should use categories.

When editing an article:

Category:
[ Account & Security ▼ ]

If the current category is inactive, the article should still display the existing category.

The manager should be able to move it to an active category.

Validation

Category Name

Required.

Must not be whitespace-only.

Maximum length should be enforced.

Duplicate Names

Prevent duplicate category names where appropriate.

Recommended uniqueness:

Name

If case-insensitive uniqueness is supported by the application/database architecture, use it.

Example:

Payments
payments

should not become two categories.

Authorization

Recommended:

Admin
Manager
Knowledge Base Editor
    ↓
Create/Edit/Activate categories

Agent
    ↓
View categories

Customer
    ↓
View active categories

Use AUTH-003.

Security

The backend must enforce category permissions.

Do not rely on Vue to hide:

Edit
Deactivate
Create

buttons.

Unauthorized API requests must be rejected.

API Response

When returning an article, include category information where useful.

Example:

{
  "id": "article-id",
  "title": "How to Reset Your Password",
  "category": {
    "id": "category-id",
    "name": "Account & Security"
  },
  "status": "Published"
}

Avoid unnecessary repeated database queries.

Use appropriate EF Core projection/includes.

Database

Suggested relationship:

KnowledgeBaseCategory
        │
        │ 1
        │
        │ *
        ↓
KnowledgeBaseArticle

Article:

CategoryId → KnowledgeBaseCategory.Id

Add:

	Foreign key

	Index on CategoryId

	Unique category name constraint where appropriate

Category With No Articles

The category may exist without articles.

Customer view:

Getting Started

No articles are available yet.

Alternatively, hide empty categories from the customer portal.

Recommended:

Hide empty categories from customers.

Management users should still see them.

Loading State

Example:

Loading categories...

Article filtering:

Loading articles...

Empty State

Management:

No categories created yet.

[Create Category]

Customer:

No help categories are available.

Error Handling

Category loading:

Unable to load categories.

[Retry]

Save:

Unable to save category.

Please try again.

Do not expose database errors.

Arabic / English

System UI must support:

	English

	Arabic

	LTR

	RTL

Category names are content and should not automatically be translated.

The administrator can create Arabic or English categories as needed.

Responsive UI

Category management and customer category pages must work on:

	Desktop

	Tablet

	Mobile

Customer category cards can stack vertically on smaller screens.

Testing

Backend/API Tests

Test:

	Authorized user can create category.

	Unauthorized user cannot create category.

	Category validation works.

	Duplicate category rejected.

	Category can be edited.

	Category can be activated/deactivated.

	Inactive category cannot be assigned to a new article.

	Existing article remains associated with inactive category.

	Article category filtering works.

	Customer only sees active categories.

	Customer only sees categories containing published articles.

	Category article count is correct.

	Pagination/filtering works.

Frontend Tests

Test:

	Category list renders.

	Create category works.

	Edit category works.

	Activate/deactivate works.

	Article category selector works.

	Category filtering works.

	Customer category page works.

	Empty state works.

	Loading state works.

	Error state works.

	RTL works.

Manual Verification

	Login as Manager.

	Open Knowledge Base Categories.

	Create Account & Security.

	Create Payments.

	Create an article under Account & Security.

	Verify category appears on the article.

	Filter articles by category.

	Deactivate Payments.

	Verify it cannot be selected for a new article.

	Verify existing articles remain intact.

	Open Customer Portal.

	Verify active categories appear.

	Verify empty categories are hidden.

	Open a category.

	Verify only published articles appear.

	Test Arabic/RTL.

Edge Cases

Handle:

	Empty category name.

	Duplicate category.

	Category not found.

	Inactive category.

	Category with existing articles.

	Category with zero articles.

	Article referencing inactive category.

	Deleted/inconsistent category reference.

	Unauthorized user.

	Database failure.

	Concurrent update.

	Arabic category name.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read KB-001.

	Inspect existing CRUD patterns.

	Inspect existing pagination/filter components.

	Reuse existing authorization.

	Reuse existing form components.

	Keep categories flat.

	Do not implement nested categories.

	Do not implement AI categorization.

	Do not delete categories unnecessarily.

	Prefer deactivate/activate behavior.

	Ensure inactive categories cannot be newly assigned.

	Ensure draft articles remain protected.

	Add backend and frontend tests.

	Run relevant tests.

	Review database relationships and indexes.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized users can create categories.

	Authorized users can edit categories.

	Categories can be activated/deactivated.

	Category names are validated.

	Duplicate categories are prevented.

	Articles can be assigned to categories.

	Inactive categories cannot be assigned to new articles.

	Existing articles remain associated with their category.

	Articles can be filtered by category.

	Customer can browse active categories.

	Customer only sees categories with published articles.

	Category article counts are available where displayed.

	Category permissions are enforced server-side.

	Arabic RTL is supported.

	English LTR is supported.

	Responsive UI is implemented.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Knowledge base category model implemented.

	Category CRUD implemented.

	Category status implemented.

	Article/category relationship implemented.

	Article filtering implemented.

	Customer category navigation implemented.

	Authorization implemented.

	PostgreSQL relationship verified.

	Indexes/constraints implemented.

	Backend tests pass.

	Frontend tests pass.

	Manual verification completed.

	No nested taxonomy introduced.

	No AI categorization introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/knowledge-base/CRM-65/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `knowledge-base`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-65` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
KB-002 — Categories & Article Management
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support manager, I want to organize knowledge base articles into categories so that agents and customers can find related information more easily.

Objective

Provide simple category management and connect knowledge base articles to categories.

The MVP should support:

	Create categories

	Edit categories

	Activate/deactivate categories

	Assign articles to categories

	Filter articles by category

	Display categories in the customer knowledge base

Scope

This story covers:

	Knowledge base category model

	Category CRUD

	Category status

	Article-category relationship

	Category filtering

	Category management UI

	Customer category navigation

	Validation

	Authorization

	Tests

This story does not cover:

	Nested category trees

	Unlimited hierarchy

	AI categorization

	Advanced taxonomy

	Automatic category generation

For the 2-day implementation, use flat categories.

Category Model

Suggested model:

KnowledgeBaseCategory

Id
Name
Description
IsActive
CreatedAt
UpdatedAt

Example:

Account & Security
Payments
Orders
Technical Support
Getting Started

Flat Categories

Categories should not have parent/child relationships for the MVP.

Do not implement:

Technical Support
    ├── Login
    │   ├── Password
    │   └── MFA
    └── Connectivity

Instead:

Login
Password
MFA
Connectivity

This keeps the implementation simple and makes search/filtering easier.

Category Management UI

Recommended route:

/knowledge-base/categories

Knowledge Base Categories

[ + New Category ]

┌──────────────────────┬──────────┬─────────┐
│ Category             │ Status   │ Actions │
├──────────────────────┼──────────┼─────────┤
│ Account & Security   │ Active   │ Edit    │
│ Payments             │ Active   │ Edit    │
│ Technical Support    │ Active   │ Edit    │
│ Legacy               │ Inactive │ Edit    │
└──────────────────────┴──────────┴─────────┘

Create Category

Create Category

Name *
[ Account & Security................. ]

Description
[ Account and security related help. ]

Status
[ Active ]

[Cancel] [Save]

Keep the form simple.

Edit Category

The same form should be reused for create and edit.

Do not create separate components unnecessarily.

Recommended Vue structure:

KnowledgeBaseCategoryList.vue
KnowledgeBaseCategoryForm.vue

Or follow the project's existing component conventions.

Category Status

Categories support:

Active
Inactive

Inactive categories should not be available for new article assignments.

Existing articles should not automatically be deleted when a category becomes inactive.

Category Assignment

An article should reference one category.

Example:

Article:
How to Reset Your Password

Category:
Account & Security

The article creation/edit form should provide:

Category *

[ Account & Security ▼ ]

Only active categories should be available for new article assignments.

Existing Articles

When a category is deactivated:

Category:
Payments → Inactive

Existing articles:
Remain associated

The article itself remains available.

The AI should not automatically move articles to another category.

Category Deletion

For the MVP, prefer deactivation rather than deletion.

Reason:

Articles may reference the category.

Recommended UI:

[Deactivate]

instead of:

[Delete]

If the project already has soft deletion conventions, follow them.

API

List Categories

GET /api/knowledge-base/categories

Example:

{
  "items": [
    {
      "id": "category-id",
      "name": "Account & Security",
      "description": "Account and security help.",
      "isActive": true
    }
  ]
}

Get Category

{ {GET /api/knowledge-base/categories/

{id}}}

h3. Create Category

POST /api/knowledge-base/categories

Example:

{
  "name": "Account & Security",
  "description": "Account and security help."
}

The backend should default the category to active unless the existing API convention requires otherwise.

h3. Update Category

{ {PUT /api/knowledge-base/categories/{id}
}}

Activate/Deactivate

Use:

{ {PATCH /api/knowledge-base/categories/

{id}/status}}

or the project's existing API convention.

h2. Article Filtering

The article management API should support category filtering.

Example:

{ {GET /api/knowledge-base/articles?categoryId={id}
}}

The UI can provide:

Category

[ All Categories ▼ ]

Example:

[ All Categories ▼ ]

Showing:
Account & Security

3 articles

Filtering must happen server-side.

Do not load every article into Vue and filter them in memory.

Customer Knowledge Base

Customers should be able to browse categories.

Example:

Help Center

What can we help you with?

┌──────────────────────┐
│ Account & Security   │
│ 12 articles          │
└──────────────────────┘

┌──────────────────────┐
│ Payments             │
│ 8 articles           │
└──────────────────────┘

┌──────────────────────┐
│ Technical Support    │
│ 15 articles          │
└──────────────────────┘

Only active categories with published articles should be displayed to customers.

Category Article Count

The customer UI may display:

Account & Security
12 articles

The count should be calculated by the backend.

Do not fetch all articles simply to calculate counts in Vue.

Customer Category Page

Recommended route:

/knowledge-base/category/{id}

Example:

Account & Security

How to Reset Your Password
How to Change Your Email
How to Enable MFA

Only published articles should appear.

Article Management

The existing KB-001 article form should use categories.

When editing an article:

Category:
[ Account & Security ▼ ]

If the current category is inactive, the article should still display the existing category.

The manager should be able to move it to an active category.

Validation

Category Name

Required.

Must not be whitespace-only.

Maximum length should be enforced.

Duplicate Names

Prevent duplicate category names where appropriate.

Recommended uniqueness:

Name

If case-insensitive uniqueness is supported by the application/database architecture, use it.

Example:

Payments
payments

should not become two categories.

Authorization

Recommended:

Admin
Manager
Knowledge Base Editor
    ↓
Create/Edit/Activate categories

Agent
    ↓
View categories

Customer
    ↓
View active categories

Use AUTH-003.

Security

The backend must enforce category permissions.

Do not rely on Vue to hide:

Edit
Deactivate
Create

buttons.

Unauthorized API requests must be rejected.

API Response

When returning an article, include category information where useful.

Example:

{
  "id": "article-id",
  "title": "How to Reset Your Password",
  "category": {
    "id": "category-id",
    "name": "Account & Security"
  },
  "status": "Published"
}

Avoid unnecessary repeated database queries.

Use appropriate EF Core projection/includes.

Database

Suggested relationship:

KnowledgeBaseCategory
        │
        │ 1
        │
        │ *
        ↓
KnowledgeBaseArticle

Article:

CategoryId → KnowledgeBaseCategory.Id

Add:

	Foreign key

	Index on CategoryId

	Unique category name constraint where appropriate

Category With No Articles

The category may exist without articles.

Customer view:

Getting Started

No articles are available yet.

Alternatively, hide empty categories from the customer portal.

Recommended:

Hide empty categories from customers.

Management users should still see them.

Loading State

Example:

Loading categories...

Article filtering:

Loading articles...

Empty State

Management:

No categories created yet.

[Create Category]

Customer:

No help categories are available.

Error Handling

Category loading:

Unable to load categories.

[Retry]

Save:

Unable to save category.

Please try again.

Do not expose database errors.

Arabic / English

System UI must support:

	English

	Arabic

	LTR

	RTL

Category names are content and should not automatically be translated.

The administrator can create Arabic or English categories as needed.

Responsive UI

Category management and customer category pages must work on:

	Desktop

	Tablet

	Mobile

Customer category cards can stack vertically on smaller screens.

Testing

Backend/API Tests

Test:

	Authorized user can create category.

	Unauthorized user cannot create category.

	Category validation works.

	Duplicate category rejected.

	Category can be edited.

	Category can be activated/deactivated.

	Inactive category cannot be assigned to a new article.

	Existing article remains associated with inactive category.

	Article category filtering works.

	Customer only sees active categories.

	Customer only sees categories containing published articles.

	Category article count is correct.

	Pagination/filtering works.

Frontend Tests

Test:

	Category list renders.

	Create category works.

	Edit category works.

	Activate/deactivate works.

	Article category selector works.

	Category filtering works.

	Customer category page works.

	Empty state works.

	Loading state works.

	Error state works.

	RTL works.

Manual Verification

	Login as Manager.

	Open Knowledge Base Categories.

	Create Account & Security.

	Create Payments.

	Create an article under Account & Security.

	Verify category appears on the article.

	Filter articles by category.

	Deactivate Payments.

	Verify it cannot be selected for a new article.

	Verify existing articles remain intact.

	Open Customer Portal.

	Verify active categories appear.

	Verify empty categories are hidden.

	Open a category.

	Verify only published articles appear.

	Test Arabic/RTL.

Edge Cases

Handle:

	Empty category name.

	Duplicate category.

	Category not found.

	Inactive category.

	Category with existing articles.

	Category with zero articles.

	Article referencing inactive category.

	Deleted/inconsistent category reference.

	Unauthorized user.

	Database failure.

	Concurrent update.

	Arabic category name.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read KB-001.

	Inspect existing CRUD patterns.

	Inspect existing pagination/filter components.

	Reuse existing authorization.

	Reuse existing form components.

	Keep categories flat.

	Do not implement nested categories.

	Do not implement AI categorization.

	Do not delete categories unnecessarily.

	Prefer deactivate/activate behavior.

	Ensure inactive categories cannot be newly assigned.

	Ensure draft articles remain protected.

	Add backend and frontend tests.

	Run relevant tests.

	Review database relationships and indexes.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized users can create categories.

	Authorized users can edit categories.

	Categories can be activated/deactivated.

	Category names are validated.

	Duplicate categories are prevented.

	Articles can be assigned to categories.

	Inactive categories cannot be assigned to new articles.

	Existing articles remain associated with their category.

	Articles can be filtered by category.

	Customer can browse active categories.

	Customer only sees categories with published articles.

	Category article counts are available where displayed.

	Category permissions are enforced server-side.

	Arabic RTL is supported.

	English LTR is supported.

	Responsive UI is implemented.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Knowledge base category model implemented.

	Category CRUD implemented.

	Category status implemented.

	Article/category relationship implemented.

	Article filtering implemented.

	Customer category navigation implemented.

	Authorization implemented.

	PostgreSQL relationship verified.

	Indexes/constraints implemented.

	Backend tests pass.

	Frontend tests pass.

	Manual verification completed.

	No nested taxonomy introduced.

	No AI categorization introduced.

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
