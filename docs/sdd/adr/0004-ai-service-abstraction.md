# ADR-0004 — AI Service Abstraction

## Context

CRM-25 requires AI functionality (ticket summary, suggested reply, automatic categorization, suggested solution, AI chatbot) to be isolated behind an application service abstraction, since no external AI API is currently available for the MVP. It also requires that AI suggestions must not silently modify important CRM data.

## Decision

Introduce an `IAiService` interface covering the five required AI capabilities. Ship `DemoAiService` as the default MVP implementation, requiring no external API. Reserve `ExternalAiService` as a future implementation for when a real provider is configured. Provider selection is via configuration (e.g. `Ai:Provider = Demo | External`). Any CRM data write triggered by an AI suggestion requires explicit user/agent approval, and that approval is recorded (action, actor, timestamp).

## Consequences

- Switching to an external AI provider later is a configuration change plus a new adapter implementing `IAiService`, not a re-architecture.
- All AI call sites depend on the `IAiService` abstraction, never on a concrete provider directly.
- AI calls have a uniform failure contract: errors/timeouts do not fail the containing CRM operation.
- AI-driven data changes always have an auditable approval trail, preventing silent mutation of CRM data.

## Status

Accepted
