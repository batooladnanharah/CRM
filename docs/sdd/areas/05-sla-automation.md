# Area 05 — SLA & Automation

## Requirement bullets (verbatim from CRM-24)
- Response and resolution targets
- Automatic assignment
- Escalation rules
- Alerts and notifications

## MVP classification
- P0 items: Response and resolution targets, Alerts and notifications
- P1 items: none
- P2 items: Automatic assignment, Escalation rules

## Jira epic
- Epic id: TBD (create in Jira)
- Parent: CRM-24

## In scope for MVP
"Basic SLA" is explicitly named P0 in CRM-24's MVP Scope Strategy. "Response and resolution targets" and "Alerts and notifications" inherit this P0 classification (A-05).

## Out of scope for MVP
"Automatic assignment" and "Escalation rules" are assumed to fall under "Advanced automation", explicitly named P2, pending resolution of OQ-11 (escalation rule mechanics are unspecified). SLA target values themselves are unspecified — see OQ-03.

## Assumptions
- A-05: "Response and resolution targets" and "Alerts and notifications" inherit P0 (Basic SLA); "Automatic assignment" and "Escalation rules" are assumed P2 (Advanced automation).

## Open questions
- OQ-03: SLA definition — actual response/resolution targets (minutes/hours) not specified.
- OQ-11: Escalation rules — manual, time-based, or both not specified.
