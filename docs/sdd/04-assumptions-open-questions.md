# Assumptions & Open Questions

Every requirement in CRM-24 that is not fully specified is recorded here as either an **assumption** (a default the SDD adopts so work can proceed) or an **open question** (unresolved, blocking the related epic until answered). No implementer or AI agent may resolve these silently — see the source-of-truth statement in [README.md](README.md).

## Assumptions

### A-01
**Mobile support scope**
- Requirement area: Platform
- CRM-24 text: "Web and mobile friendly"
- Assumption / Question: Assume responsive web only for MVP (no native mobile app).
- Impact if wrong: A native app would require a separate platform-specific implementation not scoped here.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-02
**Customer Management bullets inherit area classification**
- Requirement area: Customer Management
- CRM-24 text: "Customer management" is P0; the child bullets (Customer profiles, Contact details, Interaction history, Notes and attachments) are not individually classified.
- Assumption / Question: All four bullets inherit the P0 classification of "Customer management".
- Impact if wrong: A sub-feature intended as P1/P2 (e.g. attachments) may be over-built in the 2-day MVP.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-03
**Ticket Management bullets inherit area classification**
- Requirement area: Ticket Management
- CRM-24 text: "Ticket management" is P0; child bullets are not individually classified.
- Assumption / Question: All five bullets (Create and track tickets, Categories and priorities, Assign tickets to agents, Status and escalation, Ticket history) inherit P0.
- Impact if wrong: Scope creep or under-scope within the 2-day timebox.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-04
**Agent Dashboard bullets inherit area classification**
- Requirement area: Agent Dashboard
- CRM-24 text: "Agent dashboard" is P0; child bullets are not individually classified.
- Assumption / Question: All five bullets (Assigned tickets, Customer information, Tasks and reminders, Quick replies, Team collaboration) inherit P0.
- Impact if wrong: "Team collaboration" in particular may need more/less effort than a P0 item implies.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-05
**SLA & Automation bullets split between Basic SLA (P0) and Advanced automation (P2)**
- Requirement area: SLA & Automation
- CRM-24 text: "Basic SLA" is P0; "Advanced automation" is P2. Child bullets are not individually mapped to either.
- Assumption / Question: "Response and resolution targets" and "Alerts and notifications" inherit P0 (Basic SLA). "Automatic assignment" and "Escalation rules" are assumed P2 (Advanced automation) pending resolution of [OQ-11](#oq-11).
- Impact if wrong: Automatic assignment/escalation may be expected as P0 working functionality, not a P2 demo.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-06
**Knowledge Base bullets inherit area classification**
- Requirement area: Knowledge Base
- CRM-24 text: "Knowledge Base" is P1; child bullets are not individually classified.
- Assumption / Question: All four bullets (FAQs, Help articles, Solutions and guides, Search) inherit P1.
- Impact if wrong: None significant — P1 scope is already "basic working implementation".
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-07
**AI Features bullets inherit area classification**
- Requirement area: AI Features
- CRM-24 text: "AI assistance" is P0; child bullets are not individually classified.
- Assumption / Question: All five bullets (Ticket summaries, Suggested replies, Automatic categorization, Suggested solutions, AI chatbot) inherit P0.
- Impact if wrong: Full AI chatbot as P0 in a 2-day MVP may be unrealistic; may need re-scoping per [OQ-04](#oq-04).
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-08
**Customer Portal bullets inherit area classification**
- Requirement area: Customer Portal
- CRM-24 text: "Customer Portal" is P1; child bullets are not individually classified.
- Assumption / Question: All five bullets (Submit tickets, Track requests, View history, Access FAQs, Submit feedback) inherit P1.
- Impact if wrong: None significant — P1 scope is already "basic working implementation".
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-09
**Reports & Management bullets inherit area classification**
- Requirement area: Reports & Management
- CRM-24 text: "Reports" is P1; "Advanced reporting" is P2. Child bullets are not individually mapped.
- Assumption / Question: "Ticket reports", "SLA performance", "Agent performance", "Customer satisfaction" inherit P1. "Management dashboards" is assumed P1 unless it overlaps with "Advanced reporting" (P2), which is unresolved — see [OQ-13](#oq-13).
- Impact if wrong: Management dashboards could be over- or under-built relative to the 2-day timebox.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-10
**Security & Administration bullets split between Basic administration (P0) and unclassified items**
- Requirement area: Security & Administration
- CRM-24 text: "Basic administration" is P0. Child bullets are not individually classified; "Audit logs" retention/immutability is unspecified ([OQ-14](#oq-14)).
- Assumption / Question: "Users and roles", "Permissions", and "System configuration" inherit P0 (Basic administration). "Audit logs" is assumed P1 (present but not core to the 2-day demo) pending [OQ-14](#oq-14).
- Impact if wrong: Audit logging could be a compliance requirement that must ship as P0.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-11
**Integrations bullets inherit P2 classification**
- Requirement area: Integrations
- CRM-24 text: "External integrations", "ERP integration", and "Real WhatsApp/SMS provider integration" are explicitly P2.
- Assumption / Question: "APIs" and "External systems" (not explicitly named) are assumed to fall under "External integrations" (P2). "ERP" maps to "ERP integration" (P2). "Email, SMS and WhatsApp" maps to "Real WhatsApp/SMS provider integration" (P2).
- Impact if wrong: None expected — all sub-items already trend P2 in CRM-24's strategy.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-12
**Multi-department and Multi-branch classification**
- Requirement area: Platform
- CRM-24 text: Platform lists "Multi-department" and "Multi-branch" but the MVP Scope Strategy does not name either explicitly under P0/P1/P2.
- Assumption / Question: Assumed P1 (present in data model but not fully isolated/enforced in the 2-day MVP) pending resolution of [OQ-06](#oq-06).
- Impact if wrong: Multi-tenant data isolation could be a P0 requirement affecting the core data model.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-13
**Authentication maps to Security & Administration**
- Requirement area: Security & Administration
- CRM-24 text: "Authentication" is listed as a standalone P0 item in the MVP Scope Strategy but is not a bullet under any of the 12 requirement areas.
- Assumption / Question: Authentication is treated as an implicit part of the Security & Administration epic (alongside Users and roles / Permissions), since it is a prerequisite for role-based access.
- Impact if wrong: Authentication may need to be tracked as its own epic rather than folded into Security & Administration.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-14
**Communication Channels bullets inherit P1 classification**
- Requirement area: Communication Channels
- CRM-24 text: "Communication channel representation" is P1; individual channels (Email, WhatsApp, Live chat, SMS, Web forms) are not separately classified.
- Assumption / Question: All five channels inherit P1 ("representation" only — UI/data model present, not necessarily live provider integration, which is P2 per "Real WhatsApp/SMS provider integration"). See [OQ-09](#oq-09) for the P0-vs-P1 ambiguity this creates.
- Impact if wrong: Email in particular may be expected as a P0 working channel, not a P1 representation.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### A-15
**MVP Customer List field set**
- Requirement area: Customer Management
- CRM-24 text: "Customer profiles", "Contact details" (see `docs/sdd/areas/01-customer-management.md`) — no concrete field names or schema are specified anywhere in the SDD.
- Assumption / Question: The SDD does not currently define a concrete customer schema. For the MVP Customer List (CRM-30), the assumed minimal field set is: **`Id`, `FullName`, `Email`, `Phone`, `Company`, `CreatedAtUtc`, `UpdatedAtUtc`**. This is an implementation assumption, not an SDD-derived requirement. "Interaction history" and "Notes and attachments" — both explicit P0 bullets under Customer Management — are **not** covered by this field set and are deferred to their own dedicated follow-up stories.
- Impact if wrong: Additional fields (e.g. tax id, segment, custom fields) may require a schema migration once real requirements are clarified.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

## Open Questions

### OQ-01
**Authentication**
- Requirement area: Security & Administration / Authentication
- CRM-24 text: "Authentication" listed as P0; no further detail.
- Assumption / Question: Which identity provider? Is MFA or SSO required for MVP?
- Resolution: **JWT Bearer** authentication, issued by the CRM.Api itself against a local `Users` table (email + `PasswordHasher<User>` password hash). No external identity provider, MFA, or SSO for the MVP. Rationale: CRM-25's MVP Scope Strategy requires "Basic SLA" / "Basic administration" — a self-issued JWT keeps the auth surface inside the modular monolith (no new infrastructure, no third-party SaaS dependency, consistent with the architectural constraints in [`05-architecture.md`](05-architecture.md#architectural-constraints-hard-rules)) and is sufficient for a stateless REST API consumed by a single Vue SPA within the two-day timebox. MFA/SSO are deferred; a real identity provider can replace this without changing the API contract (`POST /api/auth/login` → JWT), matching the "no third-party auth SaaS unless a later story explicitly approves it" rule.
- Impact if wrong: Wrong auth approach could block the P0 demo or require rework. Since the token contract (bearer JWT) is decoupled from the issuer, migrating to an external identity provider later only changes how the token is issued/validated, not how the frontend/backend consume it.
- Resolver: batool.adnan@azm.com.sa
- Status: Resolved (2026-08-24) — see [`05-architecture.md`](05-architecture.md#authentication--authorization) and CRM-27 (`.squad/plans/authentication/05-story-crm-27.md`).

### OQ-02
**Roles & permissions**
- Requirement area: Security & Administration
- CRM-24 text: "Users and roles", "Permissions" — not specified.
- Assumption / Question: Which roles exist beyond "agent"? Admin, manager, customer?
- Resolution: The approved role vocabulary for the MVP is **`admin`, `agent`, `customer`** (lowercase string values). A user **may hold more than one role** — the data model is a role collection (`User.Roles: List<string>`, one Postgres `text[]` column), not a single primary role. Rationale: CRM-24 already implies three cohorts touching the system — "agent" (Agent Dashboard, seeded and used since CRM-27), "customer" (Customer Portal), and an implicit "admin" cohort (Basic administration / System configuration, Security & Administration). CRM-27 built and shipped the `Roles` collection and lowercase `"agent"` seed/test data before this question was revisited; keeping that shape (collection, lowercase) avoids an unnecessary breaking schema change and follows the AI Implementation Constraint to "prefer the simplest implementation that satisfies the acceptance criteria." Fine-grained permissions beyond these three role names remain out of scope for the MVP (see `05-architecture.md` §Authorization).
- Impact if wrong: Permission model may need redesign mid-implementation.
- Resolver: batool.adnan@azm.com.sa
- Status: Resolved (2026-08-24) — see [`05-architecture.md`](05-architecture.md#authorization) and CRM-29 (`.squad/plans/authentication/07-story-crm-29.md`).

### OQ-03
**SLA definition**
- Requirement area: SLA & Automation
- CRM-24 text: "Response and resolution targets" — not specified.
- Assumption / Question: What are the actual response/resolution targets (minutes/hours)?
- Impact if wrong: SLA automation logic cannot be built without concrete thresholds.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-04
**AI provider**
- Requirement area: AI Features
- CRM-24 text: "AI assistance" — not specified.
- Assumption / Question: Which LLM/service backs summaries, suggested replies, chatbot?
- Impact if wrong: AI epic cannot start technical design without a chosen provider.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-05
**Data storage**
- Requirement area: Platform / cross-cutting
- CRM-24 text: Not specified.
- Assumption / Question: Which database?
- Impact if wrong: Data model design is blocked without a storage decision.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-06
**Multi-tenancy**
- Requirement area: Platform
- CRM-24 text: "Multi-department", "Multi-branch" — not specified.
- Assumption / Question: Hierarchy semantics and data isolation requirements?
- Impact if wrong: Data model may need a breaking change to add tenancy boundaries later.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-07
**Arabic/English support scope**
- Requirement area: Platform
- CRM-24 text: "Arabic/English support" is P0; no further detail.
- Assumption / Question: RTL support scope, translation source, whether user- or org-level?
- Impact if wrong: i18n architecture may need rework if scope is broader than assumed.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-08
**Attachments**
- Requirement area: Customer Management
- CRM-24 text: "Notes and attachments" — not specified.
- Assumption / Question: File size limits, virus scanning, storage backend?
- Impact if wrong: Attachment handling could introduce unbudgeted infrastructure work.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-09
**Communication channels P0 vs P1**
- Requirement area: Communication Channels
- CRM-24 text: Channels are listed as full features under "Communication Channels", but the MVP Scope Strategy classifies only "Communication channel representation" as P1.
- Assumption / Question: Is Email P0, or are all channels P1 (representation only)? Per the plan's edge-case guidance, the MVP strategy classification (P1) is authoritative until resolved.
- Impact if wrong: A P0 channel expectation would require rework of the 2-day MVP scope.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-10
**Ticket categories & priorities**
- Requirement area: Ticket Management
- CRM-24 text: "Categories and priorities" — not specified.
- Assumption / Question: Fixed enum or admin-configurable?
- Impact if wrong: Data model for categories/priorities may need to change from static to configurable.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-11
**Escalation rules**
- Requirement area: SLA & Automation
- CRM-24 text: "Escalation rules" — not specified.
- Assumption / Question: Manual, time-based, or both?
- Impact if wrong: Escalation feature (see [A-05](#a-05)) may be mis-scoped as P2 when it should be P0.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-12
**Customer Portal auth**
- Requirement area: Customer Portal
- CRM-24 text: Not specified.
- Assumption / Question: Separate from agent auth? Passwordless?
- Impact if wrong: Portal auth design may need rework once [OQ-01](#oq-01) is resolved.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-13
**Reporting**
- Requirement area: Reports & Management
- CRM-24 text: Not specified.
- Assumption / Question: Real-time or scheduled? Export formats?
- Impact if wrong: Reporting epic scope (see [A-09](#a-09)) may need adjustment.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-14
**Audit logs**
- Requirement area: Security & Administration
- CRM-24 text: "Audit logs" — not specified.
- Assumption / Question: Retention period, immutability?
- Impact if wrong: Audit logging may need to move from P1 (see [A-10](#a-10)) to P0 for compliance.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-15
**Integrations (P2)**
- Requirement area: Integrations
- CRM-24 text: Not specified.
- Assumption / Question: Which APIs are stubs vs live?
- Impact if wrong: Demo may over- or under-deliver on integration fidelity.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-16
**Custom branding**
- Requirement area: Platform
- CRM-24 text: "Advanced branding" is P2; not further specified.
- Assumption / Question: Per-tenant theming scope?
- Impact if wrong: Branding epic scope undefined until resolved.
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-17
**Mobile**
- Requirement area: Platform
- CRM-24 text: "Web and mobile friendly" — not specified beyond this phrase.
- Assumption / Question: Native app or responsive web only? Resolved by assumption [A-01](#a-01): responsive web only for MVP.
- Impact if wrong: See [A-01](#a-01).
- Resolver: batool.adnan@azm.com.sa
- Status: Open

### OQ-18
**Backend runtime**
- Requirement area: Platform / cross-cutting
- CRM-24 text: Not specified in CRM-24; inferred from repo — `backend/CRM.Api/CRM.Api.csproj` targets `net10.0`.
- Assumption / Question: Confirm `net10.0` is the intended runtime for MVP.
- Impact if wrong: A runtime change would require re-scaffolding the backend project.
- Resolver: batool.adnan@azm.com.sa
- Status: Open
