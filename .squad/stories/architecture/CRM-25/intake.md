> **Fetched from jira:** [CRM-25](https://batooladnanharah.atlassian.net/browse/CRM-25)  
> *Fetched 2026-08-24T18:37:55.300Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** SDD-002 — Technical Architecture & Decisions  
**Type:** Task  
**Status:** To Do  
**Assignee:** Batool Harah

### Description

User Story

As a development team, I want the technical architecture and technology decisions documented so that the application can be implemented consistently by developers and the AI development agent.

Objective

Define a simple, maintainable architecture suitable for the two-day MVP implementation.

Technology Stack

Frontend

	Vue 3

	TypeScript

	Vue Router

	Pinia where shared application state is required

	i18n for Arabic and English

	Responsive web UI

Backend

	http://ASP.NET  Core / .NET

	REST API

	Entity Framework Core

	Built-in authentication and authorization mechanisms appropriate to the selected implementation

Database

	PostgreSQL

	Entity Framework Core used for database access and migrations

Architecture

Use a simple modular monolith architecture.

Vue 3 Frontend
      |
      | HTTP / REST
      v
ASP.NET Core API
      |
      v
Application / Business Logic
      |
      v
Entity Framework Core
      |
      v
PostgreSQL

The implementation must avoid unnecessary distributed architecture.

Frontend Structure

Organize the frontend by feature/module.

Example:

src/
├── modules/
│   ├── auth/
│   ├── customers/
│   ├── tickets/
│   ├── dashboard/
│   ├── knowledge-base/
│   ├── ai/
│   ├── portal/
│   ├── reports/
│   └── administration/
│
├── components/
├── layouts/
├── router/
├── services/
├── stores/
├── composables/
├── types/
└── i18n/

Reusable UI components should be placed in shared component areas rather than duplicated across modules.

Backend Structure

Organize backend code by feature/module.

Example:

src/
├── API/
├── Application/
│   ├── Customers/
│   ├── Tickets/
│   ├── Dashboard/
│   ├── SLA/
│   ├── KnowledgeBase/
│   ├── AI/
│   ├── Reports/
│   └── Administration/
│
├── Domain/
├── Infrastructure/
└── Tests/

The exact folder structure may be adjusted during implementation if the change improves maintainability without changing the architecture.

API Conventions

Use REST-style endpoints.

Examples:

GET    /api/customers
GET    /api/customers/{id}
POST   /api/customers
PUT    /api/customers/{id}

GET    /api/tickets
GET    /api/tickets/{id}
POST   /api/tickets
PUT    /api/tickets/{id}

POST   /api/tickets/{id}/assign
POST   /api/tickets/{id}/status
POST   /api/tickets/{id}/messages

The exact endpoint list will be defined by individual implementation stories.

Database Principles

	PostgreSQL is the primary application database.

	Database schema must be managed through Entity Framework Core migrations.

	Relationships and foreign keys must be explicitly defined.

	Required fields must be validated.

	Database constraints should be used where appropriate.

	Sensitive information must not be stored unnecessarily.

	Seed data may be used to support the MVP demonstration.

AI Architecture

AI functionality must be isolated behind an application service abstraction.

Example:

IAiService
   |
   ├── DemoAiService
   |
   └── ExternalAiService

The MVP must support DemoAiService because no external AI API is currently available.

Required AI capabilities:

	Ticket summary

	Suggested reply

	Automatic categorization

	Suggested solution

	AI chatbot

AI suggestions must not silently modify important CRM data.

Where appropriate, the agent/user must explicitly approve an AI suggestion before applying it.

External Integrations

The MVP must not depend on external integrations being available.

Email, WhatsApp, SMS, ERP and external systems should be represented through clear interfaces/configuration where required.

Actual provider integration is P2 unless an available provider/API is explicitly configured.

Architectural Constraints

	Do not introduce microservices.

	Do not introduce CQRS unless explicitly required.

	Do not introduce an event bus.

	Do not introduce Kubernetes or container orchestration as part of the MVP.

	Do not introduce additional databases without an approved requirement.

	Do not introduce Elasticsearch or another search platform for the MVP.

	Do not add dependencies unless they solve a documented requirement.

	Keep the implementation simple enough to complete and test within the two-day timeframe.

Acceptance Criteria

	Technology stack is documented.

	Frontend architecture is documented.

	Backend architecture is documented.

	Database technology and ORM are documented.

	API approach is documented.

	AI service abstraction is documented.

	Demo AI behavior is supported without an external AI API.

	Arabic/English support is considered in the architecture.

	Responsive UI is considered in the architecture.

	Architecture does not require unnecessary infrastructure.

	AI agent can use this document as an implementation constraint.

Definition of Done

	Architecture decision is documented.

	Technology decisions are documented.

	AI integration strategy is documented.

	Major architectural constraints are documented.

	Architecture has been reviewed against the two-day MVP scope.

	No unresolved technology decision blocks implementation.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/architecture/CRM-25/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `architecture`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-25` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `To Do`
- **Assignee:** `Batool Harah`
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
SDD-002 — Technical Architecture & Decisions
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a development team, I want the technical architecture and technology decisions documented so that the application can be implemented consistently by developers and the AI development agent.

Objective

Define a simple, maintainable architecture suitable for the two-day MVP implementation.

Technology Stack

Frontend

	Vue 3

	TypeScript

	Vue Router

	Pinia where shared application state is required

	i18n for Arabic and English

	Responsive web UI

Backend

	http://ASP.NET  Core / .NET

	REST API

	Entity Framework Core

	Built-in authentication and authorization mechanisms appropriate to the selected implementation

Database

	PostgreSQL

	Entity Framework Core used for database access and migrations

Architecture

Use a simple modular monolith architecture.

Vue 3 Frontend
      |
      | HTTP / REST
      v
ASP.NET Core API
      |
      v
Application / Business Logic
      |
      v
Entity Framework Core
      |
      v
PostgreSQL

The implementation must avoid unnecessary distributed architecture.

Frontend Structure

Organize the frontend by feature/module.

Example:

src/
├── modules/
│   ├── auth/
│   ├── customers/
│   ├── tickets/
│   ├── dashboard/
│   ├── knowledge-base/
│   ├── ai/
│   ├── portal/
│   ├── reports/
│   └── administration/
│
├── components/
├── layouts/
├── router/
├── services/
├── stores/
├── composables/
├── types/
└── i18n/

Reusable UI components should be placed in shared component areas rather than duplicated across modules.

Backend Structure

Organize backend code by feature/module.

Example:

src/
├── API/
├── Application/
│   ├── Customers/
│   ├── Tickets/
│   ├── Dashboard/
│   ├── SLA/
│   ├── KnowledgeBase/
│   ├── AI/
│   ├── Reports/
│   └── Administration/
│
├── Domain/
├── Infrastructure/
└── Tests/

The exact folder structure may be adjusted during implementation if the change improves maintainability without changing the architecture.

API Conventions

Use REST-style endpoints.

Examples:

GET    /api/customers
GET    /api/customers/{id}
POST   /api/customers
PUT    /api/customers/{id}

GET    /api/tickets
GET    /api/tickets/{id}
POST   /api/tickets
PUT    /api/tickets/{id}

POST   /api/tickets/{id}/assign
POST   /api/tickets/{id}/status
POST   /api/tickets/{id}/messages

The exact endpoint list will be defined by individual implementation stories.

Database Principles

	PostgreSQL is the primary application database.

	Database schema must be managed through Entity Framework Core migrations.

	Relationships and foreign keys must be explicitly defined.

	Required fields must be validated.

	Database constraints should be used where appropriate.

	Sensitive information must not be stored unnecessarily.

	Seed data may be used to support the MVP demonstration.

AI Architecture

AI functionality must be isolated behind an application service abstraction.

Example:

IAiService
   |
   ├── DemoAiService
   |
   └── ExternalAiService

The MVP must support DemoAiService because no external AI API is currently available.

Required AI capabilities:

	Ticket summary

	Suggested reply

	Automatic categorization

	Suggested solution

	AI chatbot

AI suggestions must not silently modify important CRM data.

Where appropriate, the agent/user must explicitly approve an AI suggestion before applying it.

External Integrations

The MVP must not depend on external integrations being available.

Email, WhatsApp, SMS, ERP and external systems should be represented through clear interfaces/configuration where required.

Actual provider integration is P2 unless an available provider/API is explicitly configured.

Architectural Constraints

	Do not introduce microservices.

	Do not introduce CQRS unless explicitly required.

	Do not introduce an event bus.

	Do not introduce Kubernetes or container orchestration as part of the MVP.

	Do not introduce additional databases without an approved requirement.

	Do not introduce Elasticsearch or another search platform for the MVP.

	Do not add dependencies unless they solve a documented requirement.

	Keep the implementation simple enough to complete and test within the two-day timeframe.

Acceptance Criteria

	Technology stack is documented.

	Frontend architecture is documented.

	Backend architecture is documented.

	Database technology and ORM are documented.

	API approach is documented.

	AI service abstraction is documented.

	Demo AI behavior is supported without an external AI API.

	Arabic/English support is considered in the architecture.

	Responsive UI is considered in the architecture.

	Architecture does not require unnecessary infrastructure.

	AI agent can use this document as an implementation constraint.

Definition of Done

	Architecture decision is documented.

	Technology decisions are documented.

	AI integration strategy is documented.

	Major architectural constraints are documented.

	Architecture has been reviewed against the two-day MVP scope.

	No unresolved technology decision blocks implementation.
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
