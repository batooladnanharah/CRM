> **Fetched from jira:** [CRM-66](https://batooladnanharah.atlassian.net/browse/CRM-66)  
> *Fetched 2026-08-28T16:13:45.260Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** KB-003 — Knowledge Base Search  
**Type:** Task  
**Status:** In Review

### Description

User Story

As a support agent, I want to search knowledge base articles so that I can quickly find relevant solutions while handling customer tickets.

As a customer, I want to search the knowledge base so that I can find answers without opening a support ticket.

Objective

Provide a simple knowledge base search that allows users to search published articles by:

	Article title

	Article content

	Category

The search should be available to both CRM users and customers, while respecting article visibility and authorization.

Scope

This story covers:

	Knowledge base search API

	Search input

	Title search

	Content search

	Category filtering

	Pagination

	Basic relevance ordering

	Agent search UI

	Customer search UI

	Empty states

	Loading/error states

	Arabic/English support

	Tests

This story does not cover:

	Elasticsearch

	OpenSearch

	Vector search

	Semantic search

	AI search

	Search analytics

	Typo correction

	Advanced ranking algorithms

MVP Search Approach

Use PostgreSQL/database search.

Recommended initial behavior:

Search Query
     ↓
PostgreSQL
     ↓
Title / Content / Category
     ↓
Filter Published Articles
     ↓
Order Results
     ↓
Pagination

Do not introduce an external search engine for this assessment.

Search Behavior

A query should match:

Title
Content
Category Name

Example:

Query:
password

Possible results:

How to Reset Your Password
Password Requirements
How to Change Your Password

Search Input

Customer UI:

Help Center

┌──────────────────────────────────────────┐
│ Search for help...                 🔍    │
└──────────────────────────────────────────┘

Agent UI:

Knowledge Base

[ Search articles........................ 🔍 ]

Category:
[ All Categories ▼ ]

Search Results

Example:

Search results for "password"

3 articles found

How to Reset Your Password
Account & Security

Reset your password using the Forgot Password option...

----------------------------

Password Requirements
Account & Security

Your password must contain...

----------------------------

How to Change Your Password
Account & Security

To change your password...

Result Information

Each result should display:

	Article title

	Category

	Short content excerpt

	Status where appropriate for CRM users

Customers should not see:

	Draft status

	Internal metadata

	Author management information

Search API

Recommended:

GET /api/knowledge-base/articles/search?q=password

Optional:

categoryId
page
pageSize

Example:

GET /api/knowledge-base/articles/search?q=password&categoryId=security&page=1&pageSize=10

Response

Example:

{
  "items": [
    {
      "id": "article-id",
      "title": "How to Reset Your Password",
      "category": {
        "id": "category-id",
        "name": "Account & Security"
      },
      "excerpt": "Reset your password using the Forgot Password option..."
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 3
}

Visibility Rules

Customer

Customer search must only return:

Published articles
+
Active categories

Draft articles must never appear.

CRM User

Authorized CRM users may search:

Published articles
Draft articles

if they have permission to manage/view drafts.

Follow the existing authorization model.

Category Filter

The search UI should support category filtering.

Example:

Search:
password

Category:
[ Account & Security ▼ ]

The backend performs the filter.

Do not fetch all articles and filter them in Vue.

Empty Search

If the search field is empty:

Recommended behavior:

Show popular/recent published articles

or simply:

Enter a search term to find help articles.

For the 2-day MVP, the second option is simpler.

No Results

Example:

No articles found.

Try a different search term.

Optional:

Can't find what you need?

[Contact Support]

The Contact Support button can link to the Customer Portal ticket form.

Search Query Validation

The backend should:

	Trim whitespace.

	Reject excessively long queries.

	Handle empty queries consistently.

	Prevent unsafe query construction.

Never concatenate raw user input into SQL.

Use EF Core parameterized queries or the project's existing data-access abstraction.

Minimum Query Length

Recommended:

Minimum:
2 characters

For example:

a

may return an empty/validation response.

This prevents unnecessary broad searches.

Maximum Query Length

Use a reasonable maximum.

Example:

200 characters

The exact limit should follow the application's validation conventions.

Search Matching

The MVP may use case-insensitive matching.

Example:

Password
password
PASSWORD

should produce equivalent results.

Use PostgreSQL capabilities appropriately.

Relevance Ordering

Use simple deterministic ordering.

Recommended priority:

1. Title match
2. Category match
3. Content match
4. Published date

Example:

Query: password

1. How to Reset Your Password
2. Password Requirements
3. Account Security Guide

Do not implement machine-learning relevance.

Excerpts

Search results should not display the entire article.

Example:

How to Reset Your Password

Reset your password using the Forgot Password option...

The backend may generate a simple excerpt from the content.

If excerpt generation becomes unnecessarily complex, the frontend may truncate a safely returned content preview.

Do not build highlighted search snippets for the MVP.

Pagination

Search results must support pagination.

Example:

1  2  3  Next →

The backend must perform pagination.

Do not return every result to Vue.

Sorting

Default:

Relevance

Optional:

Newest

Do not implement many sorting options.

Agent Workflow

The agent should be able to search knowledge base articles while viewing a ticket.

Recommended:

Ticket

Customer: Ahmed Ali
Issue: Unable to login

Knowledge Base
[ Search for solution........ ]

Results:
How to Reset Your Password
Password Requirements

Clicking an article opens it without losing the ticket context where practical.

Customer Workflow

Customer:

Customer Portal
      ↓
Help Center
      ↓
Search
      ↓
Search Results
      ↓
Article

If no solution is found:

Can't find an answer?

[Submit a Ticket]

Performance

For the MVP:

	Use database filtering.

	Use pagination.

	Select only required fields.

	Avoid loading entire article collections into memory.

Do not introduce:

	Elasticsearch

	OpenSearch

	Redis search

	Vector database

unless the existing SDD specifically requires them.

Database

Use PostgreSQL.

Useful indexes may include:

Status
CategoryId
PublishedAt

For larger datasets, PostgreSQL full-text search can be considered.

However, do not over-engineer this for the assessment.

The AI should inspect the expected data size and existing database conventions before selecting LIKE, ILIKE, or PostgreSQL full-text search.

Security

Search must respect authorization.

A customer must not discover:

	Draft article titles

	Draft article content

	Internal articles

	Inactive/private content

through search.

This is especially important because search endpoints can accidentally expose records hidden from normal lists.

API Authorization

The backend must determine the caller's visibility.

Do not accept:

includeDrafts=true

from a normal customer and trust it.

The server decides what the caller can search.

Arabic Search

The search should support Arabic text.

Example:

Query:
إعادة تعيين كلمة المرور

The backend should return matching Arabic articles.

Do not implement Arabic transliteration or advanced language processing.

English Search

Example:

Query:
password reset

should return English articles.

Loading State

Example:

Searching...

Avoid showing a loading spinner for every keystroke.

Recommended:

Search on:

	Enter

	Search button

Do not implement live search/autocomplete in the MVP.

Error State

Unable to search the knowledge base.

[Try Again]

Do not expose database errors.

Search Debouncing

Not required because search occurs on submit.

If the AI proposes automatic search-as-you-type, reject it unless the SDD specifically requires it.

Testing

Backend/API Tests

Test:

	Search by title.

	Search by content.

	Search by category.

	Case-insensitive search.

	Category filtering.

	Pagination.

	Empty query handling.

	Minimum query length.

	Maximum query length.

	Customer only receives published articles.

	Customer cannot discover drafts.

	CRM user can search according to permissions.

	Unauthorized content is excluded.

	Search result count is correct.

	Search does not return deleted/inactive content.

Frontend Tests

Test:

	Search input renders.

	Search button works.

	Enter triggers search.

	Results render.

	Category filter works.

	Pagination works.

	Empty state works.

	No-results state works.

	Loading state works.

	Error state works.

	Customer search works.

	Arabic search UI works.

Manual Verification

	Create several articles.

	Publish some.

	Leave one as Draft.

	Search for a common keyword.

	Verify correct results.

	Search by category.

	Verify filtering.

	Search with uppercase/lowercase.

	Verify equivalent results.

	Search for a non-existing term.

	Verify no-results state.

	Login as customer.

	Search the same term.

	Verify drafts are not returned.

	Test Arabic article/search.

	Test pagination.

Edge Cases

Handle:

	Empty query.

	Query containing only spaces.

	Very long query.

	Special characters.

	Arabic text.

	No results.

	Draft articles.

	Inactive categories.

	Deleted articles.

	Unauthorized user.

	Database failure.

	Large result set.

	Duplicate article matches.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read KB-001.

	Read KB-002.

	Inspect existing PostgreSQL/EF Core query patterns.

	Reuse existing pagination.

	Reuse existing category filtering.

	Reuse existing authorization.

	Implement search server-side.

	Do not filter the complete article collection in Vue.

	Do not introduce Elasticsearch.

	Do not introduce a vector database.

	Do not implement AI/semantic search.

	Do not implement autocomplete.

	Ensure draft articles cannot leak through search.

	Add backend and frontend tests.

	Run relevant tests.

	Review query performance and authorization.

	Verify every acceptance criterion.

Acceptance Criteria

	Users can search knowledge base articles.

	Search checks article titles.

	Search checks article content.

	Search supports category filtering.

	Search is case-insensitive where supported.

	Search results are paginated.

	Search returns a useful excerpt.

	Results have deterministic ordering.

	Customers only receive published articles.

	Customers cannot discover drafts through search.

	Inactive/private content is excluded.

	Arabic search is supported.

	English search is supported.

	Empty search is handled.

	No-results state is implemented.

	Loading state is implemented.

	Error state is implemented.

	Search is performed server-side.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Search API implemented.

	PostgreSQL search implemented.

	Category filtering implemented.

	Pagination implemented.

	Agent search UI implemented.

	Customer search UI implemented.

	Draft protection verified.

	Authorization verified.

	Arabic/English search verified.

	Backend tests pass.

	Frontend tests pass.

	Manual search flow verified.

	No external search engine introduced.

	No AI search introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/knowledge-base/CRM-66/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `knowledge-base`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-66` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
KB-003 — Knowledge Base Search
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to search knowledge base articles so that I can quickly find relevant solutions while handling customer tickets.

As a customer, I want to search the knowledge base so that I can find answers without opening a support ticket.

Objective

Provide a simple knowledge base search that allows users to search published articles by:

	Article title

	Article content

	Category

The search should be available to both CRM users and customers, while respecting article visibility and authorization.

Scope

This story covers:

	Knowledge base search API

	Search input

	Title search

	Content search

	Category filtering

	Pagination

	Basic relevance ordering

	Agent search UI

	Customer search UI

	Empty states

	Loading/error states

	Arabic/English support

	Tests

This story does not cover:

	Elasticsearch

	OpenSearch

	Vector search

	Semantic search

	AI search

	Search analytics

	Typo correction

	Advanced ranking algorithms

MVP Search Approach

Use PostgreSQL/database search.

Recommended initial behavior:

Search Query
     ↓
PostgreSQL
     ↓
Title / Content / Category
     ↓
Filter Published Articles
     ↓
Order Results
     ↓
Pagination

Do not introduce an external search engine for this assessment.

Search Behavior

A query should match:

Title
Content
Category Name

Example:

Query:
password

Possible results:

How to Reset Your Password
Password Requirements
How to Change Your Password

Search Input

Customer UI:

Help Center

┌──────────────────────────────────────────┐
│ Search for help...                 🔍    │
└──────────────────────────────────────────┘

Agent UI:

Knowledge Base

[ Search articles........................ 🔍 ]

Category:
[ All Categories ▼ ]

Search Results

Example:

Search results for "password"

3 articles found

How to Reset Your Password
Account & Security

Reset your password using the Forgot Password option...

----------------------------

Password Requirements
Account & Security

Your password must contain...

----------------------------

How to Change Your Password
Account & Security

To change your password...

Result Information

Each result should display:

	Article title

	Category

	Short content excerpt

	Status where appropriate for CRM users

Customers should not see:

	Draft status

	Internal metadata

	Author management information

Search API

Recommended:

GET /api/knowledge-base/articles/search?q=password

Optional:

categoryId
page
pageSize

Example:

GET /api/knowledge-base/articles/search?q=password&categoryId=security&page=1&pageSize=10

Response

Example:

{
  "items": [
    {
      "id": "article-id",
      "title": "How to Reset Your Password",
      "category": {
        "id": "category-id",
        "name": "Account & Security"
      },
      "excerpt": "Reset your password using the Forgot Password option..."
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 3
}

Visibility Rules

Customer

Customer search must only return:

Published articles
+
Active categories

Draft articles must never appear.

CRM User

Authorized CRM users may search:

Published articles
Draft articles

if they have permission to manage/view drafts.

Follow the existing authorization model.

Category Filter

The search UI should support category filtering.

Example:

Search:
password

Category:
[ Account & Security ▼ ]

The backend performs the filter.

Do not fetch all articles and filter them in Vue.

Empty Search

If the search field is empty:

Recommended behavior:

Show popular/recent published articles

or simply:

Enter a search term to find help articles.

For the 2-day MVP, the second option is simpler.

No Results

Example:

No articles found.

Try a different search term.

Optional:

Can't find what you need?

[Contact Support]

The Contact Support button can link to the Customer Portal ticket form.

Search Query Validation

The backend should:

	Trim whitespace.

	Reject excessively long queries.

	Handle empty queries consistently.

	Prevent unsafe query construction.

Never concatenate raw user input into SQL.

Use EF Core parameterized queries or the project's existing data-access abstraction.

Minimum Query Length

Recommended:

Minimum:
2 characters

For example:

a

may return an empty/validation response.

This prevents unnecessary broad searches.

Maximum Query Length

Use a reasonable maximum.

Example:

200 characters

The exact limit should follow the application's validation conventions.

Search Matching

The MVP may use case-insensitive matching.

Example:

Password
password
PASSWORD

should produce equivalent results.

Use PostgreSQL capabilities appropriately.

Relevance Ordering

Use simple deterministic ordering.

Recommended priority:

1. Title match
2. Category match
3. Content match
4. Published date

Example:

Query: password

1. How to Reset Your Password
2. Password Requirements
3. Account Security Guide

Do not implement machine-learning relevance.

Excerpts

Search results should not display the entire article.

Example:

How to Reset Your Password

Reset your password using the Forgot Password option...

The backend may generate a simple excerpt from the content.

If excerpt generation becomes unnecessarily complex, the frontend may truncate a safely returned content preview.

Do not build highlighted search snippets for the MVP.

Pagination

Search results must support pagination.

Example:

1  2  3  Next →

The backend must perform pagination.

Do not return every result to Vue.

Sorting

Default:

Relevance

Optional:

Newest

Do not implement many sorting options.

Agent Workflow

The agent should be able to search knowledge base articles while viewing a ticket.

Recommended:

Ticket

Customer: Ahmed Ali
Issue: Unable to login

Knowledge Base
[ Search for solution........ ]

Results:
How to Reset Your Password
Password Requirements

Clicking an article opens it without losing the ticket context where practical.

Customer Workflow

Customer:

Customer Portal
      ↓
Help Center
      ↓
Search
      ↓
Search Results
      ↓
Article

If no solution is found:

Can't find an answer?

[Submit a Ticket]

Performance

For the MVP:

	Use database filtering.

	Use pagination.

	Select only required fields.

	Avoid loading entire article collections into memory.

Do not introduce:

	Elasticsearch

	OpenSearch

	Redis search

	Vector database

unless the existing SDD specifically requires them.

Database

Use PostgreSQL.

Useful indexes may include:

Status
CategoryId
PublishedAt

For larger datasets, PostgreSQL full-text search can be considered.

However, do not over-engineer this for the assessment.

The AI should inspect the expected data size and existing database conventions before selecting LIKE, ILIKE, or PostgreSQL full-text search.

Security

Search must respect authorization.

A customer must not discover:

	Draft article titles

	Draft article content

	Internal articles

	Inactive/private content

through search.

This is especially important because search endpoints can accidentally expose records hidden from normal lists.

API Authorization

The backend must determine the caller's visibility.

Do not accept:

includeDrafts=true

from a normal customer and trust it.

The server decides what the caller can search.

Arabic Search

The search should support Arabic text.

Example:

Query:
إعادة تعيين كلمة المرور

The backend should return matching Arabic articles.

Do not implement Arabic transliteration or advanced language processing.

English Search

Example:

Query:
password reset

should return English articles.

Loading State

Example:

Searching...

Avoid showing a loading spinner for every keystroke.

Recommended:

Search on:

	Enter

	Search button

Do not implement live search/autocomplete in the MVP.

Error State

Unable to search the knowledge base.

[Try Again]

Do not expose database errors.

Search Debouncing

Not required because search occurs on submit.

If the AI proposes automatic search-as-you-type, reject it unless the SDD specifically requires it.

Testing

Backend/API Tests

Test:

	Search by title.

	Search by content.

	Search by category.

	Case-insensitive search.

	Category filtering.

	Pagination.

	Empty query handling.

	Minimum query length.

	Maximum query length.

	Customer only receives published articles.

	Customer cannot discover drafts.

	CRM user can search according to permissions.

	Unauthorized content is excluded.

	Search result count is correct.

	Search does not return deleted/inactive content.

Frontend Tests

Test:

	Search input renders.

	Search button works.

	Enter triggers search.

	Results render.

	Category filter works.

	Pagination works.

	Empty state works.

	No-results state works.

	Loading state works.

	Error state works.

	Customer search works.

	Arabic search UI works.

Manual Verification

	Create several articles.

	Publish some.

	Leave one as Draft.

	Search for a common keyword.

	Verify correct results.

	Search by category.

	Verify filtering.

	Search with uppercase/lowercase.

	Verify equivalent results.

	Search for a non-existing term.

	Verify no-results state.

	Login as customer.

	Search the same term.

	Verify drafts are not returned.

	Test Arabic article/search.

	Test pagination.

Edge Cases

Handle:

	Empty query.

	Query containing only spaces.

	Very long query.

	Special characters.

	Arabic text.

	No results.

	Draft articles.

	Inactive categories.

	Deleted articles.

	Unauthorized user.

	Database failure.

	Large result set.

	Duplicate article matches.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read KB-001.

	Read KB-002.

	Inspect existing PostgreSQL/EF Core query patterns.

	Reuse existing pagination.

	Reuse existing category filtering.

	Reuse existing authorization.

	Implement search server-side.

	Do not filter the complete article collection in Vue.

	Do not introduce Elasticsearch.

	Do not introduce a vector database.

	Do not implement AI/semantic search.

	Do not implement autocomplete.

	Ensure draft articles cannot leak through search.

	Add backend and frontend tests.

	Run relevant tests.

	Review query performance and authorization.

	Verify every acceptance criterion.

Acceptance Criteria

	Users can search knowledge base articles.

	Search checks article titles.

	Search checks article content.

	Search supports category filtering.

	Search is case-insensitive where supported.

	Search results are paginated.

	Search returns a useful excerpt.

	Results have deterministic ordering.

	Customers only receive published articles.

	Customers cannot discover drafts through search.

	Inactive/private content is excluded.

	Arabic search is supported.

	English search is supported.

	Empty search is handled.

	No-results state is implemented.

	Loading state is implemented.

	Error state is implemented.

	Search is performed server-side.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Search API implemented.

	PostgreSQL search implemented.

	Category filtering implemented.

	Pagination implemented.

	Agent search UI implemented.

	Customer search UI implemented.

	Draft protection verified.

	Authorization verified.

	Arabic/English search verified.

	Backend tests pass.

	Frontend tests pass.

	Manual search flow verified.

	No external search engine introduced.

	No AI search introduced.

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
