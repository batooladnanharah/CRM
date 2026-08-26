# Area 03 — Communication Channels

## Requirement bullets (verbatim from CRM-24)
- Email
- WhatsApp
- Live chat
- SMS
- Web forms

## MVP classification
- P0 items: none
- P1 items: Email, WhatsApp, Live chat, SMS, Web forms
- P2 items: none (live provider integration for WhatsApp/SMS is tracked separately as P2 under Integrations — see Area 11)

## Jira epic
- Epic id: TBD (create in Jira)
- Parent: CRM-24

## In scope for MVP
CRM-24's MVP Scope Strategy names "Communication channel representation" as P1 ("basic working implementation"). All five channels inherit this P1 classification (A-14) — representation/UI and data model, not necessarily live external provider integration.

## Out of scope for MVP
Live/real WhatsApp and SMS provider integration is explicitly P2 ("Real WhatsApp/SMS provider integration" — see Area 11, Integrations). CRM-24 lists these channels as full features under "Communication Channels" while classifying only "representation" as P1, which is a direct contradiction — recorded as OQ-09. Per the plan's edge-case guidance, the MVP strategy classification (P1) is authoritative until OQ-09 is resolved.

## Assumptions
- A-14: All five channels inherit the P1 classification of "Communication channel representation".

## Open questions
- OQ-09: Communication channels P0 vs P1 — CRM-24 lists channels as full P0-adjacent features under "Communication Channels" but the MVP strategy classifies only "Communication channel representation" as P1. Clarify whether Email is P0 or all channels are P1.
