# CRM — Code Review & Enhancement Suggestions

_Reviewed: 2026-08-27_

## Stack Summary
- **Frontend:** Vue 3.5 (Composition API), Pinia, vue-router 5, vue-i18n 11 (EN/AR), TypeScript, Vite 8, Vitest.
- **Backend:** ASP.NET Core minimal APIs, EF Core + PostgreSQL, JWT bearer auth, per-module DbContexts.
- **Modules:** Tickets, Customers, Customer Portal, Knowledge Base, Quick Replies, Communication Channels, Reports, SLA, Security/Audit, Auth (RBAC), AI assistance.

Good foundations already in place: modular structure, RBAC with policy tests, i18n, per-endpoint xUnit tests, Vitest specs, confirm-dialog composable, SLA automation service.

---

## 1. Finish Incomplete Features
- **SMTP email sending is unimplemented** — `backend/CRM.Api/Email/SmtpEmailService.cs:9-12` throws `NotImplementedException`. Outbound email (ticket notifications, customer replies) currently can't work in production.
- **AI ticket category suggestion** is stubbed/blocked — `backend/CRM.Api/Ai/AiApplicationService.cs:44`. Worth unblocking since the AI plumbing (`IAiService`, provider abstraction) already exists.
- **Customer interaction timeline can't deep-link to tickets** — `CustomerInteractionTimeline.vue:71`. Small fix, high UX value (agents jump from a customer's history straight into the related ticket).

## 2. Notifications & Real-Time Updates
- No toast/notification center beyond a static `AppAlert.vue`. Add a global toast system for async success/error feedback (ticket assigned, SLA breach, message received).
- No sign of WebSockets/SignalR — ticket updates, new customer messages, and SLA breaches likely require manual refresh. A SignalR hub (backend already on ASP.NET Core) + Pinia store subscription would enable live ticket boards and unread-message badges.

## 3. API Contract Safety
- Frontend `src/api/*.ts` clients are hand-written against the backend, while OpenAPI is enabled server-side. Generate a typed client (e.g. `openapi-typescript` or NSwag) from the OpenAPI spec to eliminate drift risk between backend contracts and frontend types.

## 4. Search, Filtering & Pagination
- Verify ticket/customer list views support server-side pagination, filtering (status, priority, assignee, date range) and full-text search — these are the highest-leverage UX wins for any CRM at scale. If only client-side filtering over a full fetch exists today, that will not scale past a few hundred records.
- Consider saved views/filters per agent (e.g. "My open tickets", "Unassigned, high priority").

## 5. Dashboard & Reporting
- A `dashboard` module and `reports` module exist — worth checking whether they cover: SLA compliance trends, agent workload/performance, customer satisfaction (CSAT), first-response time, ticket volume by channel. If not, these are standard CRM reporting features worth adding.
- Export to CSV/Excel for reports if not already present.

## 6. Customer Portal Enhancements
- Self-service knowledge base search from the portal (tie into the existing Knowledge Base module).
- Ticket status tracking / satisfaction survey after ticket resolution (CSAT).
- File attachment support in customer portal replies, if not already there.

## 7. AI Assistance Expansion
- Beyond ticket summary/availability badge already present: auto-suggested replies drawing from Quick Replies + Knowledge Base, sentiment detection on incoming messages to help prioritize, and the already-planned but blocked category suggestion.

## 8. Accessibility & UX Polish
- No explicit dark mode toggle found — worth adding given a custom `components/ui` design system already exists (low incremental cost, real user-facing win).
- Audit custom UI components (`AppButton`, `AppDialog`, `AppTable`) for keyboard navigation and ARIA attributes if not already covered by tests.

## 9. Security & Audit
- `SecurityAdminEndpoints` and `AuditLogger` exist — confirm audit trail covers permission changes, login attempts (including failures), and data exports, which are common compliance requirements.
- Consider rate limiting / lockout policy on auth endpoints if not already implemented (`Auth/JwtTokenService.cs`).

## 10. Roadmap Source Already in Repo
- `.squad/plans/*` and `.squad/stories/*` contain existing planning docs for agent-dashboard, ai-assistance, customer-portal, sla-automation, and security-administration. Worth reviewing these directly — they may already describe planned features not yet built, avoiding duplicate design work.

---

## Suggested Priority Order
1. Fix SMTP email service (blocks core notification flows).
2. Add global toast/notification system.
3. Confirm/add server-side pagination + filtering on ticket & customer lists.
4. Real-time updates via SignalR for tickets/messages.
5. Generate typed API client from OpenAPI.
6. Dark mode + accessibility pass.
7. Expand AI assistance (unblock category suggestion, add suggested replies).
8. CSAT survey + reporting exports.
