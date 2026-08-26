# API Inventory (CRM-84)

Verification pass over the existing `backend/CRM.Api` endpoint surface. Routes, policy
names, and DTOs below are copied verbatim from the code — see the file column for the
source of truth. This inventory is a snapshot; when a route changes, update this file
in the same PR.

## Authentication

- Scheme: the default ASP.NET Core JWT bearer scheme (`JwtBearerDefaults.AuthenticationScheme`,
  i.e. `"Bearer"`), registered in `backend/CRM.Api/Program.cs`.
- Issuer/audience: `Jwt:Issuer` (default `"crm-api"`) / `Jwt:Audience` (default `"crm-web"`)
  from configuration; the signing key is `Jwt:Key` (required — the app fails to start
  without it).
- **Obtain a token:** `POST /api/auth/login`, anonymous.
  - Request body — `LoginRequest` (`backend/CRM.Api/Auth/AuthContracts.cs`):
    ```json
    { "email": "agent@crm.local", "password": "..." }
    ```
  - Success (200) — `LoginResponse`:
    ```json
    {
      "user": { "id": "guid", "name": "string", "email": "string", "roles": ["agent"] },
      "token": "eyJ..."
    }
    ```
  - Failure (401) — a single generic message for every failure reason (unknown email,
    wrong password, inactive account) — see "Error format" below.
  - Every attempt, success or failure, is written to the audit log (`user.login.succeeded` /
    `user.login.failed`); the 401 response body never reveals which check failed, but the
    admin-only audit trail records the specific reason internally.
- **Use the token:** send `Authorization: Bearer <token>` on every subsequent request.
- Other auth routes: `POST /api/auth/logout` (any authenticated user, 204, stateless —
  no server-side revocation), `GET /api/auth/me` (any authenticated user, returns
  `AuthUserDto` built from the token's claims).

## Roles

From `backend/CRM.Api/Auth/Roles.cs` (exact constants, lowercase):

| Role | Constant value |
|---|---|
| Admin | `"admin"` |
| Agent | `"agent"` |
| Customer | `"customer"` |

A `User.Roles` is a list — most seeded accounts hold exactly one role, but nothing
prevents holding more than one.

## Authorization policies

Registered in `Program.cs`:

| Policy name | Requirement |
|---|---|
| `AdminOnly` | `Roles.Admin` |
| `AgentOrAdmin` | `Roles.Admin` **or** `Roles.Agent` |
| `CustomerPortal` | `Roles.Customer` |

A bare `.RequireAuthorization()` (no policy name) means "any authenticated user,
regardless of role."

## Endpoint inventory

One row per `Map{Get,Post,Put,Delete}` call, grouped by the file that registers it.
**Audited?** means the handler calls `IAuditLogger.WriteAsync(...)`.

### `backend/CRM.Api/Customers/CustomerEndpoints.cs` — prefix `/api/customers`

| Route | Method | Auth | Audited? |
|---|---|---|---|
| `/` | GET | any authenticated user | — |
| `/` | POST | `AgentOrAdmin` | `customer.created` |
| `/{id:guid}` | GET | any authenticated user | — |
| `/{id:guid}` | PUT | `AgentOrAdmin` | `customer.updated` |
| `/{id:guid}/interactions` | GET | any authenticated user | — |

### `backend/CRM.Api/Customers/CustomerAttachmentEndpoints.cs` — prefix `/api/customers/{customerId:guid}/attachments`

Group-level policy: `AgentOrAdmin` (all four routes).

| Route | Method | Audited? |
|---|---|---|
| `/` | GET | — |
| `/` | POST | `customer.attachment.added` |
| `/{attachmentId:guid}/download` | GET | — |
| `/{attachmentId:guid}` | DELETE | `customer.attachment.removed` |

### `backend/CRM.Api/Customers/CustomerNoteEndpoints.cs` — prefix `/api/customers/{customerId:guid}/notes`

Group-level policy: `AgentOrAdmin` (all four routes). Note authors and admins may
modify a note; other agents cannot (`CanModify` check inside the handlers).

| Route | Method | Audited? |
|---|---|---|
| `/` | GET | — |
| `/` | POST | `customer.note.added` |
| `/{noteId:guid}` | PUT | `customer.note.updated` |
| `/{noteId:guid}` | DELETE | `customer.note.removed` |

### `backend/CRM.Api/Tickets/TicketEndpoints.cs` — prefix `/api/tickets`

| Route | Method | Auth | Audited? |
|---|---|---|---|
| `/` | GET | `AgentOrAdmin` | — |
| `/` | POST | `AgentOrAdmin` | `ticket.created` |
| `/{id:guid}` | GET | `AgentOrAdmin` | — |
| `/{id:guid}/assignment` | PUT | `AgentOrAdmin` | `ticket.assigned` |
| `/{id:guid}/status` | PUT | `AgentOrAdmin` | `ticket.status.changed` |
| `/{id:guid}/priority` | PUT | `AgentOrAdmin` | `ticket.priority.changed` |
| `/{id:guid}/history` | GET | `AgentOrAdmin` | — |
| `/eligible-agents` | GET | `AgentOrAdmin` | — |
| `/{id:guid}/escalate` | POST | **`AdminOnly`** (stricter than the rest of the group) | `ticket.escalated` |

### `backend/CRM.Api/Tickets/TicketAttachmentEndpoints.cs` — prefix `/api/tickets/{ticketId:guid}/attachments`

Group-level policy: `AgentOrAdmin`.

| Route | Method | Audited? |
|---|---|---|
| `/` | GET | — |
| `/` | POST | `ticket.attachment.added` |
| `/{attachmentId:guid}/download` | GET | — |
| `/{attachmentId:guid}` | DELETE | `ticket.attachment.removed` |

### `backend/CRM.Api/Tickets/TicketMessageEndpoints.cs` — prefix `/api/tickets/{ticketId:guid}/messages`

Group-level policy: `AgentOrAdmin` (staff-only — internal notes and agent-authored
public replies; the customer role never posts messages directly).

| Route | Method | Audited? |
|---|---|---|
| `/` | GET | — |
| `/` | POST | `ticket.message.added` |

### `backend/CRM.Api/KnowledgeBase/KnowledgeBaseEndpoints.cs` — prefix `/api/knowledge-base/articles`

| Route | Method | Auth | Audited? |
|---|---|---|---|
| `/` | GET | `AgentOrAdmin` | — |
| `/search` | GET | `AgentOrAdmin` | — |
| `/by-slug/{slug}` | GET | `AgentOrAdmin` | — |
| `/{id:guid}` | GET | `AgentOrAdmin` | — |
| `/` | POST | `AdminOnly` | — |
| `/{id:guid}` | PUT | `AdminOnly` | — |
| `/{id:guid}` | DELETE | `AdminOnly` | — |

Not customer/ticket data (out of Task 2's audit-gap scope for this story).

### `backend/CRM.Api/CommunicationChannels/CommunicationChannelEndpoints.cs` — prefix `/api/channels`

| Route | Method | Auth | Audited? |
|---|---|---|---|
| `/` | GET | `AgentOrAdmin` | — |
| `/` | POST | `AdminOnly` | — |
| `/{id:guid}` | GET | `AgentOrAdmin` | — |
| `/{id:guid}` | PUT | `AdminOnly` | — |
| `/{id:guid}` | DELETE | `AdminOnly` | — |
| `/{id:guid}/emails` | GET | `AgentOrAdmin` | — |
| `/{id:guid}/emails/ingest` | POST | `AgentOrAdmin` | — |

Not customer/ticket data (out of Task 2's audit-gap scope for this story). The
`/ingest` route is an internal test/seeding endpoint — no real SMTP/IMAP integration
exists (correctly out of scope; see `docs/sdd/areas/11-integrations.md`).

### `backend/CRM.Api/QuickReplies/QuickReplyEndpoints.cs` — prefix `/api/quick-replies`

| Route | Method | Auth | Audited? |
|---|---|---|---|
| `/` | GET | `AgentOrAdmin` | — |
| `/` | POST | `AdminOnly` | — |
| `/{id:guid}` | PUT | `AdminOnly` | — |
| `/{id:guid}` | DELETE | `AdminOnly` | — |

Not customer/ticket data (out of Task 2's audit-gap scope for this story).

### `backend/CRM.Api/Sla/SlaPolicyEndpoints.cs` — prefixes `/api/sla/policies` and `/api/sla`

Group-level policy: `AdminOnly` (both groups).

| Route | Method | Audited? |
|---|---|---|
| `/api/sla/policies` | GET | — |
| `/api/sla/policies/{id:guid}` | GET | — |
| `/api/sla/policies` | POST | — |
| `/api/sla/policies/{id:guid}` | PUT | — |
| `/api/sla/policies/{id:guid}` | DELETE | — |
| `/api/sla/evaluate-now` | POST | — |

Not customer/ticket data directly (out of Task 2's audit-gap scope for this story).

### `backend/CRM.Api/Security/SecurityAdminEndpoints.cs` — prefix `/api/admin`

Group-level policy: `AdminOnly`. This is the one module that already wrote audit
entries before this story.

| Route | Method | Audited? |
|---|---|---|
| `/users` | GET | — |
| `/users/{id:guid}` | GET | — |
| `/users/{id:guid}/role` | PUT | `user.role.assigned` |
| `/users/{id:guid}/disable` | POST | `user.disabled` |
| `/users/{id:guid}/enable` | POST | `user.enabled` |
| `/audit-log` | GET | — |

Also: any 403 on a path starting with `/api/admin` is itself audited as
`security.access.denied` by a pipeline middleware in `Program.cs` (registered between
`UseAuthentication` and `UseAuthorization`).

### `backend/CRM.Api/Reports/ReportsEndpoints.cs` — prefix `/api/reports`

Group-level policy: `AdminOnly`.

| Route | Method | Audited? |
|---|---|---|
| `/summary` | GET | — |

### `backend/CRM.Api/CustomerPortal/CustomerPortalEndpoints.cs` — prefix `/api/customer`

Group-level policy: `CustomerPortal`. Every handler resolves the caller's own
`customerId` server-side via `ICurrentCustomerAccessor`; if it's `null` (a
mis-provisioned customer account), the handler returns `403 Forbidden`. Ticket detail
lookups return `404` — not `403` — for another customer's ticket, so URL tampering
cannot distinguish "not yours" from "doesn't exist."

| Route | Method | Audited? |
|---|---|---|
| `/dashboard` | GET | — |
| `/tickets` | GET | — |
| `/tickets/{id:guid}` | GET | — |
| `/tickets` | POST | `ticket.created` |

## New audit actions added by this story

`backend/CRM.Api/Security/AuditActions.cs` gained these constants (Task 2), each
wired into the corresponding mutating handler above — no existing constant was
renamed, and no endpoint's behavior changed:

```
customer.created, customer.updated,
customer.note.added, customer.note.updated, customer.note.removed,
customer.attachment.added, customer.attachment.removed,
ticket.created, ticket.assigned, ticket.status.changed, ticket.priority.changed,
ticket.escalated, ticket.message.added,
ticket.attachment.added, ticket.attachment.removed
```

## Error format

This codebase **does not use** `Results.Problem` / `Results.ValidationProblem` /
`ProblemDetails` anywhere (verified: zero matches across `backend/CRM.Api/`). Every
error response is a single-field DTO:

```csharp
// backend/CRM.Api/Auth/AuthContracts.cs
public sealed record ErrorResponse(string Message);
```

Example wire shape (`400`, `409`, `401`, or `500` — the field is always `message`):

```json
{ "message": "Full name is required." }
```

Status-code helper usage is not fully consistent across the codebase (both produce
the identical wire shape above):
- Most `400`s: `Results.BadRequest(new ErrorResponse("..."))`.
- Some `409`s: `Results.Conflict(new ErrorResponse("..."))` (KnowledgeBase, SecurityAdmin).
- Other `409`s and all `401`/`500`s: `Results.Json(new ErrorResponse("..."), statusCode: StatusCodes.Status4xx/5xx)`,
  because there's no built-in `Results.Unauthorized(body)` / `Results.InternalServerError(body)`
  overload that accepts a typed body in this SDK version.
- `404` and `204` are returned bare (`Results.NotFound()` / `Results.NoContent()`),
  with no `ErrorResponse` body.

## OpenAPI

- `builder.Services.AddOpenApi()` is called unconditionally in `Program.cs`.
- `app.MapOpenApi()` is called when `app.Environment.IsDevelopment()` **or**
  `app.Environment.IsEnvironment("Testing")` — the latter was added by this story
  (Task 3) so the integration tests in `ExternalApiIntegrationTests.cs` can assert
  the document is served; it remains off in Production.
- Served (no custom route was passed to `MapOpenApi()`) at the ASP.NET Core default:
  **`GET /openapi/v1.json`**.
- Every endpoint listed above now carries `.WithName(...)` and `.WithTags(...)`
  (Task 3); the `Customers` and `Tickets` modules — the plan's own designated
  "representative read/write modules" — additionally carry `.Produces<T>(...)` for
  their success/error response types. The remaining ten modules were not annotated
  with `.Produces<T>()` in this pass; extending that is a reasonable, contained
  follow-up rather than something this hardening story needed to complete in full.
- `.ProducesValidationProblem()` was deliberately **not** added anywhere: this
  codebase never returns an RFC 7807 validation-problem body (see "Error format"
  above), so that annotation would misrepresent the actual response shape.
