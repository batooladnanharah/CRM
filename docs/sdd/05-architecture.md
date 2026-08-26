# Technical Architecture & Decisions

## Purpose

This document is the technical-architecture companion to the SDD. It is a hard constraint for the AI development agent and human developers: implementation must follow the technology stack, layering, and rules documented here unless a new story explicitly revises them.

## Scope & non-goals

**In scope:** technology stack, layering, folder layout, REST conventions, database principles, AI abstraction, i18n/responsive strategy.

**Non-goals:** choosing endpoints per feature, choosing an external AI provider, infrastructure/deployment. Those belong to later stories.

## Technology stack

### Frontend

- Vue 3
- TypeScript
- Vue Router
- Pinia where shared application state is required
- i18n for Arabic and English
- Responsive web UI

### Backend / Data

- ASP.NET Core / .NET
- REST API
- Entity Framework Core
- Built-in authentication and authorization mechanisms: **JWT Bearer** (see [Authentication & authorization](#authentication--authorization); resolves [OQ-01](04-assumptions-open-questions.md#oq-01))
- PostgreSQL as the primary database
- Entity Framework Core used for database access and migrations

## High-level architecture

Use a simple modular monolith architecture.

```
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
```

The implementation must avoid unnecessary distributed architecture. This is a **modular monolith**: a single deployable backend process and a single frontend application, organized internally by feature module. Distributed patterns (microservices, message buses, separate data stores per module) are explicitly rejected for the MVP.

## Frontend structure

Organize the frontend by feature/module.

```
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
```

- `modules/` — one folder per feature area (auth, customers, tickets, dashboard, knowledge-base, ai, portal, reports, administration). Each module owns its views, module-local components, and module-local logic.
- `components/` — reusable UI components shared across modules.
- `layouts/` — top-level page shells (e.g. authenticated layout, portal layout).
- `router/` — Vue Router route definitions and route guards.
- `services/` — HTTP clients / API access wrappers used by modules.
- `stores/` — Pinia stores for shared application state.
- `composables/` — shared composition-API logic (`use*` functions) not tied to a single module.
- `types/` — shared TypeScript types/interfaces.
- `i18n/` — locale message catalogs and i18n configuration.

Reusable UI components should be placed in shared component areas (`components/`) rather than duplicated across modules.

### Internationalisation (i18n)

- Message catalogs live under `src/i18n/{en,ar}/…`, one namespace per module where practical.
- `dir="rtl"` / `dir="ltr"` is switched at the layout root based on the active locale (Arabic = RTL, English = LTR).
- Dates, numbers, and currency are formatted using locale-aware formatting, not hard-coded formats.
- No user-visible string may be hard-coded in a component; all user-visible text goes through the i18n catalog.

### Responsive UI

The app must render usably on desktop and tablet at MVP; mobile is best-effort (see [A-01](04-assumptions-open-questions.md#a-01) — responsive web only, no native mobile app).

## Backend structure

Organize backend code by feature/module.

```
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
```

- `API/` — controllers, DI composition, request/response DTOs at the edge.
- `Application/` — feature modules (`Customers/`, `Tickets/`, `Dashboard/`, `SLA/`, `KnowledgeBase/`, `AI/`, `Reports/`, `Administration/`). Each module owns its services, validators, and DTOs.
- `Domain/` — entities, value objects, domain rules; no EF/ASP.NET dependencies.
- `Infrastructure/` — EF Core `DbContext`, migrations, external-provider adapters (email/WhatsApp/SMS/ERP/AI stubs), authentication providers.
- `Tests/` — unit + integration test projects.

The exact folder structure may be adjusted during implementation if the change improves maintainability without changing the architecture.

The current `backend/CRM.Api/` project (see `backend/CRM.Api/CRM.Api.csproj`, `backend/CRM.Api/Program.cs`) is a minimal .NET 10 API and has not yet adopted this modular layout; the layout above is the **target**, to be applied by a subsequent implementation story. This document does not require the reshape to happen immediately, but it forbids drift from the target.

## API conventions

Use REST-style endpoints.

```
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
```

The exact endpoint list will be defined by individual implementation stories.

Additional conventions:

- Lowercase, plural resource names in path segments (e.g. `/api/customers`, not `/api/Customer`).
- JSON request and response bodies.
- Timestamps are ISO-8601, in UTC.
- Error responses use a consistent problem shape: RFC 7807 `application/problem+json`.
- Pagination via `?page=&pageSize=` query parameters.
- `PUT` is used for full replace; `PATCH` is reserved for partial updates if introduced later.
- API versioning is deferred until it is needed.

## Database principles

- PostgreSQL is the primary application database.
- Database schema must be managed through Entity Framework Core migrations.
- Relationships and foreign keys must be explicitly defined.
- Required fields must be validated.
- Database constraints should be used where appropriate.
- Sensitive information must not be stored unnecessarily.
- Seed data may be used to support the MVP demonstration.

Migrations are the only way schema changes are applied; no ad-hoc SQL in production.

## AI architecture

AI functionality must be isolated behind an application service abstraction.

```
IAiService
   |
   ├── DemoAiService
   |
   └── ExternalAiService
```

The MVP must support `DemoAiService` because no external AI API is currently available.

Required AI capabilities (as operations the interface must cover; concrete method signatures are picked in the AI implementation story):

- Ticket summary
- Suggested reply
- Automatic categorization
- Suggested solution
- AI chatbot

Rules:

- `DemoAiService` is the **default** MVP implementation and must run without any external API.
- Provider selection is via configuration (e.g. `Ai:Provider = Demo | External`).
- AI suggestions must not silently modify important CRM data. Where appropriate, the agent/user must explicitly approve an AI suggestion before applying it, and the approval must be recorded (which action, by whom, when).
- AI errors/timeouts must not fail the containing CRM operation (customer/ticket flows continue without the suggestion).

## External integrations

The MVP must not depend on external integrations being available. Email, WhatsApp, SMS, ERP, and external systems live behind interfaces/configuration in `Infrastructure/`, with configuration-driven provider selection. The MVP ships with no-op / demo implementations for all of them.

Actual provider integration is P2 unless an available provider/API is explicitly configured. The MVP must be runnable and demonstrable with every external integration disabled.

## Authentication & authorization

The MVP uses ASP.NET Core's built-in authentication and authorization mechanisms. The concrete scheme is **JWT Bearer authentication**: the CRM.Api issues a signed JWT from `POST /api/auth/login` after verifying an email + password pair against a local `Users` table (password hashed with `PasswordHasher<User>`), and the SPA sends that token as an `Authorization: Bearer <token>` header on subsequent requests. ASP.NET Core's `AddAuthentication(...).AddJwtBearer(...)` / `AddAuthorization()` pipeline validates the token on protected endpoints (e.g. `GET /api/auth/me`).

This resolves [OQ-01](04-assumptions-open-questions.md#oq-01) in `04-assumptions-open-questions.md`: no external identity provider, MFA, or SSO is used for the MVP. Rationale — a self-issued JWT keeps authentication inside the modular monolith (no new infrastructure or third-party dependency, consistent with the [architectural constraints](#architectural-constraints-hard-rules) below) and is sufficient for a stateless REST API consumed by a single Vue SPA within the two-day timebox. Because the frontend/backend contract is just "bearer JWT in, bearer JWT out," swapping the issuer for an external identity provider later does not require changing how the token is consumed.

The signing key is sourced from configuration (`Jwt:Key`), never committed to source; Development uses `dotnet user-secrets`.

Rule: no third-party auth SaaS unless a later story explicitly approves it.

## Authorization

This resolves [OQ-02](04-assumptions-open-questions.md#oq-02): the approved MVP role vocabulary is **`admin`, `agent`, `customer`** (lowercase string values, matching the seed/test data already in place since CRM-27). A user **may hold more than one role at once** — roles are a collection, not a single primary role.

This is not a new data model: `backend/CRM.Api/Auth/User.cs`'s `Roles: List<string>` (mapped to a Postgres `text[]` column in `AuthDbContext`), the `ClaimTypes.Role` claims already emitted per-role by `JwtTokenService`, and the `Roles` list already returned by `AuthUserDto` from both `POST /api/auth/login` and `GET /api/auth/me` are the authorization surface. Role-based authorization (`[Authorize(Roles = "...")]` / named `AuthorizationPolicy`s on minimal-API routes, and role-aware route guards / UI gating on the frontend) is layered on top of this existing shape — it does not replace it with a singular `Role` field, and it does not require a new EF migration.

Fine-grained per-record permissions, a permissions-editing UI, and multi-tenant role scoping remain out of scope for the MVP (tracked as later work under `docs/sdd/areas/10-security-administration.md`).

## Architectural constraints (hard rules)

These constraints are hard. The AI development agent and human developers must treat any deviation as a blocker and raise a new story before proceeding.

- Do not introduce microservices.
- Do not introduce CQRS unless explicitly required.
- Do not introduce an event bus.
- Do not introduce Kubernetes or container orchestration as part of the MVP.
- Do not introduce additional databases without an approved requirement.
- Do not introduce Elasticsearch or another search platform for the MVP.
- Do not add dependencies unless they solve a documented requirement.
- Keep the implementation simple enough to complete and test within the two-day timeframe.

## Two-day MVP fit

- Single process, single deployable backend — no distributed infrastructure to stand up or debug.
- Single database (PostgreSQL) — no cross-database consistency concerns.
- No infrastructure beyond the app and database — no Kubernetes, no message bus, no search platform.
- Demo AI (`DemoAiService`) — AI capabilities are demonstrable without waiting on an external provider.
- Mocked/no-op external channels — Email, WhatsApp, SMS, ERP integrations do not block the demo.

## React learning sandboxes are not the CRM frontend

Two `react-learning-journey/` trees exist in this repository (at the repo root and under `backend/CRM.Api/`). These are learning artefacts only. **Vue 3 + TypeScript is the CRM frontend.** The presence of these React sandboxes does not contradict or change this decision — see [ADR-0002](adr/0002-frontend-vue3.md).

## Change log

| Date | Change | Author |
|---|---|---|
| 2026-08-24 | Initial architecture document (CRM-25) | batool.adnan@azm.com.sa |
| 2026-08-24 | Added §Authorization, resolving OQ-02 (role vocabulary: `admin`/`agent`/`customer`, multi-role collection) ahead of CRM-29 | batool.adnan@azm.com.sa |
