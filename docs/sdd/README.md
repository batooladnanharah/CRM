# Software Design Document (SDD) — Customer Support CRM

As a project developer, this SDD documents the CRM requirements and MVP scope so that implementation can be performed consistently by the development team and AI agent.

## Source of truth statement

This SDD is the source of truth for implementation. No implementer or AI agent may introduce business rules that are not documented here. Unclear requirements are captured in [`04-assumptions-open-questions.md`](04-assumptions-open-questions.md) and must be resolved before the corresponding epic is implemented.

## Inputs

- Tracker intake: [`.squad/stories/sdd/CRM-24/intake.md`](../../.squad/stories/sdd/CRM-24/intake.md).
- Attachments: No attachments provided in CRM-24 intake as of 2026-08-24.

## Top-level documents

| Document | Purpose |
|---|---|
| [01-overview.md](01-overview.md) | Product vision, target users, timebox, non-goals |
| [02-mvp-scope.md](02-mvp-scope.md) | MVP scope table with P0/P1/P2 classification |
| [03-epic-map.md](03-epic-map.md) | Requirement area → Jira epic id mapping |
| [04-assumptions-open-questions.md](04-assumptions-open-questions.md) | Assumptions and open questions log |
| [05-architecture.md](05-architecture.md) | Technical architecture & decisions |

## Architecture Decision Records

| Document | Purpose |
|---|---|
| [adr/0001-modular-monolith.md](adr/0001-modular-monolith.md) | Modular monolith over microservices |
| [adr/0002-frontend-vue3.md](adr/0002-frontend-vue3.md) | Vue 3 + TypeScript as the CRM frontend |
| [adr/0003-postgres-efcore.md](adr/0003-postgres-efcore.md) | PostgreSQL + EF Core migrations |
| [adr/0004-ai-service-abstraction.md](adr/0004-ai-service-abstraction.md) | `IAiService` abstraction with `DemoAiService` |

## Requirement areas

| # | Area | File |
|---|---|---|
| 1 | Customer Management | [areas/01-customer-management.md](areas/01-customer-management.md) |
| 2 | Ticket Management | [areas/02-ticket-management.md](areas/02-ticket-management.md) |
| 3 | Communication Channels | [areas/03-communication-channels.md](areas/03-communication-channels.md) |
| 4 | Agent Dashboard | [areas/04-agent-dashboard.md](areas/04-agent-dashboard.md) |
| 5 | SLA & Automation | [areas/05-sla-automation.md](areas/05-sla-automation.md) |
| 6 | Knowledge Base | [areas/06-knowledge-base.md](areas/06-knowledge-base.md) |
| 7 | AI Features | [areas/07-ai-features.md](areas/07-ai-features.md) |
| 8 | Customer Portal | [areas/08-customer-portal.md](areas/08-customer-portal.md) |
| 9 | Reports & Management | [areas/09-reports-management.md](areas/09-reports-management.md) |
| 10 | Security & Administration | [areas/10-security-administration.md](areas/10-security-administration.md) |
| 11 | Integrations | [areas/11-integrations.md](areas/11-integrations.md) |
| 12 | Platform | [areas/12-platform.md](areas/12-platform.md) |
