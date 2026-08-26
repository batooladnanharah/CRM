# Area 11 — Integrations

## Requirement bullets (verbatim from CRM-24)
- APIs
- ERP
- Email, SMS and WhatsApp
- External systems

## MVP classification
- P0 items: none
- P1 items: none
- P2 items: APIs, ERP, Email, SMS and WhatsApp, External systems

## Jira epic
- Epic id: TBD (create in Jira)
- Parent: CRM-24

## In scope for MVP
None of this area's items are P0 or P1. CRM-24's MVP Scope Strategy explicitly names "External integrations", "ERP integration", and "Real WhatsApp/SMS provider integration" as P2 ("simplified/demo implementation"). "APIs" and "External systems" are assumed to fall under "External integrations" (A-11); "ERP" maps to "ERP integration"; "Email, SMS and WhatsApp" maps to "Real WhatsApp/SMS provider integration" (live provider integration, distinct from the P1 channel representation in Area 03).

## Out of scope for MVP
Full external integrations are out of scope; only simplified/demo implementations are expected. Per CRM-24: "P2 functionality must not block the core CRM workflow." Which specific APIs are stubbed vs live is unspecified — see OQ-15.

## Assumptions
- A-11: "APIs" and "External systems" inherit P2 under "External integrations"; "ERP" maps to "ERP integration" (P2); "Email, SMS and WhatsApp" maps to "Real WhatsApp/SMS provider integration" (P2).

## Open questions
- OQ-15: Integrations (P2) — which APIs are stubs vs live not specified.

## Existing API surface (CRM-84)

The core CRM API itself (`backend/CRM.Api`) is fully implemented and already
consumable by an authorized external client — this is distinct from the P2 items
above (ERP, live Email/SMS/WhatsApp providers), which remain out of scope. See:

- `docs/integration/api-inventory.md` — endpoint-by-endpoint inventory (route,
  auth policy, validation, audit status) verified against the code.
- `docs/integration/external-api-consumer-guide.md` — how an external application
  authenticates and calls a representative read + write flow, with the exact error
  format, authorization matrix, and OpenAPI discovery URL.
