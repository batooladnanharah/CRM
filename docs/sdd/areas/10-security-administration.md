# Area 10 — Security & Administration

## Requirement bullets (verbatim from CRM-24)
- Users and roles
- Permissions
- Audit logs
- System configuration

## MVP classification
- P0 items: Users and roles, Permissions, System configuration (plus Authentication, see below)
- P1 items: Audit logs
- P2 items: none

## Jira epic
- Epic id: TBD (create in Jira)
- Parent: CRM-24

## In scope for MVP
"Basic administration" is explicitly named P0 in CRM-24's MVP Scope Strategy. "Users and roles", "Permissions", and "System configuration" inherit this classification (A-10). "Authentication" is also explicitly named P0 in the MVP Scope Strategy but is not a bullet under any of the 12 requirement areas; it is assumed to belong to this epic (A-13).

## Out of scope for MVP
"Audit logs" retention period and immutability are unspecified; it is assumed P1 pending resolution — see OQ-14. Role list beyond "agent" is unspecified — see OQ-02.

## Assumptions
- A-10: "Users and roles", "Permissions", and "System configuration" inherit P0 (Basic administration); "Audit logs" is assumed P1 pending OQ-14.
- A-13: Authentication is treated as an implicit part of this epic.

## Open questions
- OQ-01: Authentication — identity provider, MFA, SSO not specified.
- OQ-02: Roles & permissions — which roles exist beyond "agent" not specified.
- OQ-14: Audit logs — retention period, immutability not specified.
