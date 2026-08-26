# External API Consumer Guide

This guide is for an external application that needs to authenticate against the CRM
API and call it end-to-end (read + write). It documents the plumbing that already
exists in `backend/CRM.Api` — see `docs/sdd/05-architecture.md` for the overall system
architecture and `docs/sdd/areas/11-integrations.md` for why deeper external
integrations (ERP, live email/SMS/WhatsApp providers) are explicitly out of scope for
the MVP. This story (CRM-84) added **no new endpoints** — everything below already
existed; see `docs/integration/api-inventory.md` for the full endpoint-by-endpoint
inventory this guide is based on.

## 1. Overview

- All business endpoints require a JWT bearer token (`Authorization: Bearer <token>`),
  obtained by logging in with a CRM user account.
- Roles are `admin`, `agent`, `customer` — see the authorization matrix below for what
  each can do.
- There is no API-key or client-credentials flow; an external integration authenticates
  as a real CRM user (typically a dedicated service account with the `agent` or `admin`
  role, provisioned like any other user).
- Related reading: `docs/sdd/05-architecture.md` (system architecture),
  `docs/sdd/areas/11-integrations.md` (scope of external integrations for the MVP).

## 2. Authentication

**`POST /api/auth/login`** — anonymous.

Request:
```json
{ "email": "agent@crm.local", "password": "your-password" }
```

Success (`200 OK`):
```json
{
  "user": { "id": "3fa85f64-...", "name": "Demo Agent", "email": "agent@crm.local", "roles": ["agent"] },
  "token": "eyJhbGciOi..."
}
```

Failure (`401 Unauthorized`) — a single generic message covers unknown email, wrong
password, and a disabled/inactive account (deliberately not distinguished, to avoid
leaking account existence):
```json
{ "message": "Invalid email or password." }
```

Attach the token to every subsequent request:
```
Authorization: Bearer eyJhbGciOi...
```

```bash
TOKEN=$(curl -s -X POST https://your-crm-host/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"agent@crm.local","password":"your-password"}' \
  | jq -r '.token')
```

Tokens are stateless JWTs; there is no server-side revocation (`POST /api/auth/logout`
exists for client-side parity but does not invalidate the token — see
`backend/CRM.Api/Program.cs`). Treat the token's expiry (`Jwt:ExpiryMinutes`,
default 60 minutes) as the only expiration mechanism, and re-authenticate when it lapses.

## 3. Representative read flow — Customers

**`GET /api/customers`** — any authenticated user. Supports `search`, `company`,
`sortBy` (`fullName` | `email` | `company` | `createdAtUtc`), `sortDir` (`asc` | `desc`),
`page`, `pageSize`.

```bash
curl -s https://your-crm-host/api/customers?search=acme \
  -H "Authorization: Bearer $TOKEN"
```

`200 OK`:
```json
{
  "items": [
    { "id": "...", "fullName": "Alice Johnson", "email": "alice@example.com", "phone": null, "company": "Acme Corp", "createdAtUtc": "2026-01-01T00:00:00Z" }
  ],
  "page": 1, "pageSize": 25, "totalCount": 1
}
```

**`GET /api/customers/{id}`** — any authenticated user.

```bash
curl -s https://your-crm-host/api/customers/3fa85f64-... \
  -H "Authorization: Bearer $TOKEN"
```

Responses: `200 OK` with the same customer shape as above; `404 Not Found` if the id
doesn't exist; `401 Unauthorized` with no/invalid token.

## 4. Representative write flow — Tickets

**`POST /api/tickets`** — requires `AgentOrAdmin` (an `agent` or `admin` account; a
`customer`-role token gets `403 Forbidden` here — customers create tickets through the
separate `POST /api/customer/tickets` self-service endpoint instead, which always
binds the ticket to the caller's own linked customer record).

```bash
curl -s -X POST https://your-crm-host/api/tickets \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
        "customerId": "3fa85f64-...",
        "title": "Cannot access account",
        "description": "Customer reports a login failure since this morning.",
        "priority": "High"
      }'
```

`priority` is optional (`Low` | `Normal` | `High` | `Urgent`; defaults to `Normal`).

Responses:
- `201 Created` — full `TicketResponse` body, including the computed SLA snapshot.
- `400 Bad Request` — validation failure, e.g. `{ "message": "Title is required." }`.
- `401 Unauthorized` — missing/invalid token.
- `403 Forbidden` — authenticated but not `agent`/`admin`.

Follow-up read: **`GET /api/tickets/{id}`** (same auth requirement) returns the
created ticket, including its assignee, status, priority, and SLA due dates.

## 5. Error format

There is no RFC 7807 `application/problem+json` response anywhere in this API —
every error is the same single-field shape, regardless of status code:

```csharp
public sealed record ErrorResponse(string Message);
```

```json
{ "message": "Full name is required." }
```

`404 Not Found` and `204 No Content` responses have no body at all. See
`docs/integration/api-inventory.md` → "Error format" for the exact status-code helper
used per case.

## 6. Authorization matrix

Roles are defined in `backend/CRM.Api/Auth/Roles.cs`: `admin`, `agent`, `customer`.

| Capability | admin | agent | customer |
|---|:---:|:---:|:---:|
| Read customers/tickets, list eligible agents | ✅ | ✅ | ❌ |
| Create/update customers, create/manage tickets | ✅ | ✅ | ❌ (self-service portal only) |
| Escalate a ticket | ✅ | ❌ | ❌ |
| Manage quick replies, communication channels, SLA policies, knowledge base articles | ✅ | read-only | ❌ |
| Security admin (`/api/admin/*`) — manage user roles, view audit log | ✅ | ❌ | ❌ |
| Reports summary | ✅ | ❌ | ❌ |
| Customer self-service portal (`/api/customer/*`) — own dashboard, own tickets | ❌ | ❌ | ✅ (own data only) |

The full per-route breakdown lives in `docs/integration/api-inventory.md`.

## 7. Auditing

Security-relevant actions are persisted to an append-only `AuditLog` table
(`backend/CRM.Api/Security/AuditLog.cs`), viewable by an admin via
`GET /api/admin/audit-log` (filters: `actorId`, `targetId`, `action`, `from`, `to`).
Recorded actions (see `AuditActions.cs`):

```
user.login.succeeded, user.login.failed,
user.role.assigned, user.disabled, user.enabled,
security.access.denied,
customer.created, customer.updated,
customer.note.added, customer.note.updated, customer.note.removed,
customer.attachment.added, customer.attachment.removed,
ticket.created, ticket.assigned, ticket.status.changed, ticket.priority.changed,
ticket.escalated, ticket.message.added,
ticket.attachment.added, ticket.attachment.removed
```

Every ticket/customer-mutating endpoint an external client can reach writes one of
these entries, including the two write paths this guide demonstrates
(`POST /api/tickets`, `POST /api/customer/tickets` → `ticket.created`).

## 8. OpenAPI discovery

The generated OpenAPI 3 document is served at:

```
GET /openapi/v1.json
```

available in Development and Testing environments (off in Production by default —
see `docs/integration/api-inventory.md` → "OpenAPI"). Import that URL directly into
Postman (Import → Link) or Insomnia (Import → From URL) to generate a full request
collection with schemas.

## 9. Explicitly out of scope

Per the CRM-84 intake and `docs/sdd/areas/11-integrations.md`:

- ERP, SMS, WhatsApp, or live Email provider integration (only a demo/manual email
  ingestion endpoint exists today — see `docs/sdd/areas/11-integrations.md`).
- An API gateway or new integration middleware.
- A new authentication architecture (API keys, OAuth client-credentials, etc.).
- Webhooks / push notifications to external systems.
- New business endpoints of any kind — this story only inventoried, audited, and
  documented what already existed.
