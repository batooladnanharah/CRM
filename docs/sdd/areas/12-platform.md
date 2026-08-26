# Area 12 — Platform

> For the authoritative technology stack, module layout, and architectural constraints, see `../05-architecture.md`. This area file describes platform-level product concerns only.

## Requirement bullets (verbatim from CRM-24)
- Arabic and English
- Web and mobile friendly
- Multi-department
- Multi-branch
- Custom branding

## MVP classification
- P0 items: Arabic and English, Web and mobile friendly
- P1 items: Multi-department, Multi-branch
- P2 items: Custom branding

## Jira epic
- Epic id: TBD (create in Jira)
- Parent: CRM-24

## In scope for MVP
"Arabic/English support" and "Responsive UI" are explicitly named P0 in CRM-24's MVP Scope Strategy, mapping to "Arabic and English" and "Web and mobile friendly" respectively. "Web and mobile friendly" is scoped to responsive web only for MVP (A-01).

## Out of scope for MVP
"Multi-department" and "Multi-branch" are not explicitly classified in the MVP Scope Strategy; assumed P1 pending resolution of the multi-tenancy hierarchy/isolation question (A-12, OQ-06). "Custom branding" maps to "Advanced branding", explicitly named P2; per-tenant theming scope is unspecified — see OQ-16. RTL support scope and translation source for Arabic/English are unspecified — see OQ-07.

## Assumptions
- A-01: "Web and mobile friendly" is scoped to responsive web only for MVP (no native mobile app).
- A-12: "Multi-department" and "Multi-branch" are assumed P1 pending OQ-06.

## Open questions
- OQ-06: Multi-tenancy — hierarchy semantics, data isolation not specified.
- OQ-07: Arabic/English — RTL support scope, translation source, user- vs org-level not specified.
- OQ-16: Custom branding — per-tenant theming scope not specified.
- OQ-17: Mobile — native app or responsive web only; resolved by A-01.
- OQ-18: Backend runtime — confirm `net10.0` is the intended runtime for MVP.
