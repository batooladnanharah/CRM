> **Fetched from jira:** [CRM-74](https://batooladnanharah.atlassian.net/browse/CRM-74)  
> *Fetched 2026-09-01T12:24:27.472Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** PORTAL-003 — Customer Ticket History & Details  
**Type:** Task  
**Status:** In Review

### Description

User Story

As a customer, I want to open my support ticket and see its conversation and history so that I can understand what has happened and respond to the support team.

Objective

Provide a customer-facing ticket details page where the customer can:

	View ticket information

	View customer-visible conversation messages

	Reply to the support team

	View ticket status

	View category

	View relevant timestamps

	View safe ticket history

	Add attachments where supported

	Navigate back to their tickets

The customer must never see internal agent notes or internal CRM information.

Scope

This story covers:

	Customer ticket details page

	Customer-visible conversation

	Customer replies

	Message composer

	Attachments

	Status display

	Safe ticket history

	Authorization

	Loading/error states

	Arabic/English

	Responsive UI

	Tests

This story does not cover:

	Internal agent notes

	Agent assignment

	AI-generated replies

	Communication-channel administration

	Internal audit logs

Those belong to other areas.

Ticket Details Page

Recommended route:

/portal/tickets/{ticketId}

Example:

┌──────────────────────────────────────────────┐
│ ← My Tickets                                │
│                                              │
│ Ticket #1008                                 │
│ Unable to login                              │
│                                              │
│ Status: In Progress                          │
│ Category: Account & Security                 │
│ Created: 24 Aug 2026                         │
├──────────────────────────────────────────────┤
│ Conversation                                 │
│                                              │
│ Customer                                     │
│ I cannot login to my account.                │
│ 10:30 AM                                     │
│                                              │
│ Support                                      │
│ Please try resetting your password again.    │
│ 11:15 AM                                     │
│                                              │
├──────────────────────────────────────────────┤
│ Reply                                        │
│ ┌──────────────────────────────────────────┐ │
│ │ Type your message...                     │ │
│ └──────────────────────────────────────────┘ │
│                                              │
│ [Attach]                       [Send Reply]  │
└──────────────────────────────────────────────┘

Ticket Header

Display only customer-safe information:

	Ticket number

	Subject

	Status

	Category

	Created date

	Last updated date

Optional:

	Priority if it is intentionally customer-visible

Do not expose internal SLA details.

Status

Reuse the existing ticket status model.

Example:

New
In Progress
Waiting for Customer
Resolved
Closed

Do not create separate customer statuses.

Conversation

Display customer-visible messages chronologically.

Example:

Customer
I cannot access my account.

24 Aug, 10:30

Support Agent
Please try the password reset process.

24 Aug, 11:15

Newest messages should appear at the bottom.

Internal Notes

Internal notes must never appear in the customer portal.

Example:

Agent internal note:
Escalated to engineering.

The customer must not see this.

The backend should filter internal messages.

Do not retrieve all messages and hide internal notes only in Vue.

Customer Reply

The customer should be able to reply while the ticket is open.

Example:

Reply

[ I tried resetting the password but still cannot login. ]

[Send Reply]

The message should be added to the existing ticket communication system.

Do not create a separate CustomerMessage domain.

API

Recommended:

{{GET /api/portal/tickets/

{ticketId}}}

Returns customer-safe ticket data.

Example:

{
  "id": "ticket-id",
  "ticketNumber": 1008,
  "subject": "Unable to login",
  "status": "In Progress",
  "category": {
    "id": "category-id",
    "name": "Account & Security"
  },
  "createdAt": "2026-08-24T10:30:00Z",
  "updatedAt": "2026-08-24T11:15:00Z",
  "messages": [
    {
      "id": "message-id",
      "senderType": "Customer",
      "content": "I cannot login to my account.",
      "createdAt": "2026-08-24T10:30:00Z"
    },
    {
      "id": "message-id-2",
      "senderType": "Agent",
      "content": "Please try resetting your password.",
      "createdAt": "2026-08-24T11:15:00Z"
    }
  ]
}

The actual DTO should follow the existing API conventions.

h2. Customer Reply API

Recommended:

{{POST /api/portal/tickets/{ticketId}
/messages}}

Request:

{
  "content": "I tried resetting the password but it still does not work."
}

The backend determines:

CustomerId
Sender
CreatedAt
Ticket ownership

Do not accept sender/customer identity from the frontend.

Reply Authorization

Before adding a message:

Authenticate
      ↓
Find ticket
      ↓
Verify customer owns ticket
      ↓
Verify ticket allows replies
      ↓
Create message

If the ticket belongs to another customer:

403 / Not Found

Use the project's existing authorization convention.

Closed Ticket

If a ticket is closed and the business rules prevent replies:

This ticket is closed.

[Create New Ticket]

Do not allow a customer to bypass ticket lifecycle rules.

If the existing ticket design allows reopening through a reply, follow the existing SDD instead.

Do not invent a new behavior.

Resolved Ticket

If the existing workflow allows customer replies to reopen a resolved ticket, use that existing rule.

Otherwise:

This ticket has been resolved.

[Create New Ticket]

The customer portal must follow the central ticket lifecycle rules.

Customer-Visible History

The customer may see safe events such as:

Ticket created
Agent replied
Status changed to In Progress
Agent replied

Do not expose:

	Internal routing

	Agent assignment changes

	Internal escalation

	Internal SLA calculations

	Private notes

For the 2-day MVP, the conversation itself is sufficient if a separate safe history model is not already available.

Do not build a complex audit-history UI unnecessarily.

Attachments

Customers may attach files to replies if supported by the existing communication system.

Example:

Reply

[Add Attachment]

screenshot.png
error.pdf

[Send Reply]

Reuse existing attachment storage and validation.

Do not create a second upload system.

Attachment Security

Validate server-side:

	File size

	File type

	Number of files

	Authorization

	Storage path

Do not trust the browser.

Do not expose internal storage paths.

Message Validation

Required:

Content

Must not be:

"     "

Apply the existing message length limits.

Send Button

While sending:

Sending...

Disable the button to prevent duplicate submissions.

Success

After sending:

Message sent.

The new message should appear in the conversation.

Prefer updating the local conversation from the API response rather than reloading the entire portal.

Error

Unable to send your message.

Please try again.

The message should remain in the composer so the customer does not lose their text.

Loading

Initial page:

Loading ticket...

Conversation:

Loading conversation...

Sending:

Sending...

Not Found

If the ticket does not exist or the customer has no access:

Ticket not found.

[Back to My Tickets]

Avoid revealing whether the ticket exists for another customer.

Customer Data Isolation

This is a critical security requirement.

Customer A must never be able to access:

Customer B
Ticket
Messages
Attachments

by modifying the ticket ID.

The backend must enforce ownership.

Direct URL Test

Example:

/portal/tickets/123

If ticket 123 belongs to another customer:

Customer A → request → ticket 123
                      ↓
              authorization check
                      ↓
                  rejected

Do not rely only on frontend route guards.

Ticket Status Changes

The customer should not be able to directly change ticket status.

Do not expose:

[Resolve]
[Close]
[Assign]

unless the existing requirements explicitly allow it.

Support agents/system workflows control ticket status.

Customer Reopening

If the existing ticket SDD defines customer reply as reopening a ticket, reuse that behavior.

Otherwise do not introduce reopening logic here.

API Response Security

Do not return the entire internal ticket object.

Use a dedicated customer DTO.

Avoid fields such as:

InternalNotes
AssignedAgentId
InternalSla
EscalationReason
InternalPriority

unless explicitly customer-visible.

Pagination

If conversations can become large, paginate messages.

For the 2-day MVP, if the expected message count is small, a reasonable limit may be sufficient.

Do not load thousands of messages into Vue.

If the project already has message pagination, reuse it.

Refresh

Provide a simple refresh mechanism if useful:

[Refresh]

But do not implement complex real-time messaging unless required.

Real-Time Updates

For the 2-day implementation:

Do not add WebSockets/SignalR just for this story unless the existing architecture already uses them.

Simple refresh/polling is sufficient if real-time behavior is not a requirement.

The important part is the end-to-end customer communication flow.

Notifications

If the project already has notifications, a new agent reply may trigger the existing notification mechanism.

Do not create a separate notification system.

AI Integration

Do not add new AI behavior here.

The customer already has:

AI-004 — AI Customer Chatbot

The customer ticket conversation should remain a normal communication channel.

AI-generated suggested replies belong to the agent experience.

Arabic / English

Support:

	English

	Arabic

	LTR

	RTL

Example:

المحادثة

العميل:
لا أستطيع تسجيل الدخول.

الدعم:
يرجى تجربة إعادة تعيين كلمة المرور.

Responsive Design

Desktop:

Ticket Details
Conversation
Reply Composer

Mobile:

Ticket Header
Conversation
Reply Composer

The reply composer should remain easy to access.

Testing

Backend Tests

Test:

	Customer can retrieve own ticket.

	Customer cannot retrieve another customer's ticket.

	Internal notes excluded.

	Internal fields excluded.

	Customer can add a message to own ticket.

	Customer cannot add a message to another customer's ticket.

	Empty message rejected.

	Message length validation.

	Closed-ticket reply behavior follows ticket rules.

	Attachment validation.

	Customer identity comes from authentication.

	Message sender cannot be spoofed.

	Ticket not found handled correctly.

Frontend Tests

Test:

	Ticket details render.

	Status renders.

	Category renders.

	Messages render.

	Internal notes never render.

	Reply composer works.

	Send button works.

	Loading state.

	Error state.

	Empty conversation state.

	Attachment UI.

	Closed-ticket behavior.

	Arabic RTL.

	Mobile layout.

Integration Tests

Test:

Customer
 ↓
Vue Ticket Details
 ↓
GET /api/portal/tickets/{id}
 ↓
.NET
 ↓
Authorization
 ↓
Customer-safe DTO
 ↓
Vue

And:

Customer
 ↓
Reply
 ↓
POST /api/portal/tickets/{id}/messages
 ↓
Authorization
 ↓
Existing Ticket Communication
 ↓
Database
 ↓
Response
 ↓
Vue

Manual Verification

	Login as Customer A.

	Open one of Customer A's tickets.

	Verify ticket details.

	Verify conversation.

	Verify internal notes are not visible.

	Reply to the ticket.

	Verify message appears.

	Verify the agent can see the message through the normal ticket system.

	Login as Customer B.

	Try to open Customer A's ticket directly.

	Verify access is denied.

	Try sending a message to Customer A's ticket.

	Verify it is rejected.

	Test closed ticket behavior.

	Test attachment.

	Test Arabic.

	Test mobile.

Edge Cases

Handle:

	Ticket not found.

	Ticket belongs to another customer.

	Empty message.

	Very long message.

	Closed ticket.

	Resolved ticket.

	Attachment too large.

	Unsupported attachment.

	Network failure.

	Database failure.

	Duplicate send.

	Customer session expired.

	Internal note.

	Missing category.

	Deleted attachment.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read TKT-008.

	Read TKT-009.

	Read PORTAL-002.

	Inspect the existing ticket communication implementation.

	Inspect the existing message model.

	Reuse the existing communication service.

	Reuse existing attachment handling.

	Reuse existing authorization.

	Do not create a second message system.

	Do not expose internal notes.

	Do not trust customer IDs from the frontend.

	Return customer-safe DTOs.

	Do not add SignalR/WebSockets unless already required.

	Add backend/frontend/integration tests.

	Run relevant tests.

	Review authorization and data exposure.

	Verify every acceptance criterion.

Acceptance Criteria

	Customer can open their ticket.

	Customer can see ticket number.

	Customer can see subject.

	Customer can see status.

	Customer can see category.

	Customer can see customer-visible messages.

	Internal notes are never shown.

	Customer can reply to an allowed ticket.

	Reply uses the existing communication system.

	Customer identity is determined server-side.

	Customer cannot spoof another sender.

	Customer cannot access another customer's ticket.

	Customer cannot change internal ticket properties.

	Attachments follow existing validation/security rules.

	Closed/resolved behavior follows existing ticket rules.

	Loading state is implemented.

	Error state is implemented.

	Not-found state is implemented.

	Duplicate sending is prevented.

	Arabic RTL is supported.

	English LTR is supported.

	Mobile responsive UI is implemented.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	Integration tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Customer ticket details page implemented.

	Customer-visible conversation implemented.

	Customer reply implemented.

	Attachment integration implemented.

	Internal note protection verified.

	Customer ownership verified.

	Existing communication system reused.

	Closed-ticket rules verified.

	Arabic/English verified.

	Responsive UI verified.

	Backend tests pass.

	Frontend tests pass.

	Integration tests pass.

	Manual end-to-end conversation verified.

	No duplicate messaging architecture introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/customer-portal/CRM-74/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `customer-portal`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-74` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
PORTAL-003 — Customer Ticket History & Details
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a customer, I want to open my support ticket and see its conversation and history so that I can understand what has happened and respond to the support team.

Objective

Provide a customer-facing ticket details page where the customer can:

	View ticket information

	View customer-visible conversation messages

	Reply to the support team

	View ticket status

	View category

	View relevant timestamps

	View safe ticket history

	Add attachments where supported

	Navigate back to their tickets

The customer must never see internal agent notes or internal CRM information.

Scope

This story covers:

	Customer ticket details page

	Customer-visible conversation

	Customer replies

	Message composer

	Attachments

	Status display

	Safe ticket history

	Authorization

	Loading/error states

	Arabic/English

	Responsive UI

	Tests

This story does not cover:

	Internal agent notes

	Agent assignment

	AI-generated replies

	Communication-channel administration

	Internal audit logs

Those belong to other areas.

Ticket Details Page

Recommended route:

/portal/tickets/{ticketId}

Example:

┌──────────────────────────────────────────────┐
│ ← My Tickets                                │
│                                              │
│ Ticket #1008                                 │
│ Unable to login                              │
│                                              │
│ Status: In Progress                          │
│ Category: Account & Security                 │
│ Created: 24 Aug 2026                         │
├──────────────────────────────────────────────┤
│ Conversation                                 │
│                                              │
│ Customer                                     │
│ I cannot login to my account.                │
│ 10:30 AM                                     │
│                                              │
│ Support                                      │
│ Please try resetting your password again.    │
│ 11:15 AM                                     │
│                                              │
├──────────────────────────────────────────────┤
│ Reply                                        │
│ ┌──────────────────────────────────────────┐ │
│ │ Type your message...                     │ │
│ └──────────────────────────────────────────┘ │
│                                              │
│ [Attach]                       [Send Reply]  │
└──────────────────────────────────────────────┘

Ticket Header

Display only customer-safe information:

	Ticket number

	Subject

	Status

	Category

	Created date

	Last updated date

Optional:

	Priority if it is intentionally customer-visible

Do not expose internal SLA details.

Status

Reuse the existing ticket status model.

Example:

New
In Progress
Waiting for Customer
Resolved
Closed

Do not create separate customer statuses.

Conversation

Display customer-visible messages chronologically.

Example:

Customer
I cannot access my account.

24 Aug, 10:30

Support Agent
Please try the password reset process.

24 Aug, 11:15

Newest messages should appear at the bottom.

Internal Notes

Internal notes must never appear in the customer portal.

Example:

Agent internal note:
Escalated to engineering.

The customer must not see this.

The backend should filter internal messages.

Do not retrieve all messages and hide internal notes only in Vue.

Customer Reply

The customer should be able to reply while the ticket is open.

Example:

Reply

[ I tried resetting the password but still cannot login. ]

[Send Reply]

The message should be added to the existing ticket communication system.

Do not create a separate CustomerMessage domain.

API

Recommended:

{ {GET /api/portal/tickets/

{ticketId}}}

Returns customer-safe ticket data.

Example:

{
  "id": "ticket-id",
  "ticketNumber": 1008,
  "subject": "Unable to login",
  "status": "In Progress",
  "category": {
    "id": "category-id",
    "name": "Account & Security"
  },
  "createdAt": "2026-08-24T10:30:00Z",
  "updatedAt": "2026-08-24T11:15:00Z",
  "messages": [
    {
      "id": "message-id",
      "senderType": "Customer",
      "content": "I cannot login to my account.",
      "createdAt": "2026-08-24T10:30:00Z"
    },
    {
      "id": "message-id-2",
      "senderType": "Agent",
      "content": "Please try resetting your password.",
      "createdAt": "2026-08-24T11:15:00Z"
    }
  ]
}

The actual DTO should follow the existing API conventions.

h2. Customer Reply API

Recommended:

{ {POST /api/portal/tickets/{ticketId}
/messages}}

Request:

{
  "content": "I tried resetting the password but it still does not work."
}

The backend determines:

CustomerId
Sender
CreatedAt
Ticket ownership

Do not accept sender/customer identity from the frontend.

Reply Authorization

Before adding a message:

Authenticate
      ↓
Find ticket
      ↓
Verify customer owns ticket
      ↓
Verify ticket allows replies
      ↓
Create message

If the ticket belongs to another customer:

403 / Not Found

Use the project's existing authorization convention.

Closed Ticket

If a ticket is closed and the business rules prevent replies:

This ticket is closed.

[Create New Ticket]

Do not allow a customer to bypass ticket lifecycle rules.

If the existing ticket design allows reopening through a reply, follow the existing SDD instead.

Do not invent a new behavior.

Resolved Ticket

If the existing workflow allows customer replies to reopen a resolved ticket, use that existing rule.

Otherwise:

This ticket has been resolved.

[Create New Ticket]

The customer portal must follow the central ticket lifecycle rules.

Customer-Visible History

The customer may see safe events such as:

Ticket created
Agent replied
Status changed to In Progress
Agent replied

Do not expose:

	Internal routing

	Agent assignment changes

	Internal escalation

	Internal SLA calculations

	Private notes

For the 2-day MVP, the conversation itself is sufficient if a separate safe history model is not already available.

Do not build a complex audit-history UI unnecessarily.

Attachments

Customers may attach files to replies if supported by the existing communication system.

Example:

Reply

[Add Attachment]

screenshot.png
error.pdf

[Send Reply]

Reuse existing attachment storage and validation.

Do not create a second upload system.

Attachment Security

Validate server-side:

	File size

	File type

	Number of files

	Authorization

	Storage path

Do not trust the browser.

Do not expose internal storage paths.

Message Validation

Required:

Content

Must not be:

"     "

Apply the existing message length limits.

Send Button

While sending:

Sending...

Disable the button to prevent duplicate submissions.

Success

After sending:

Message sent.

The new message should appear in the conversation.

Prefer updating the local conversation from the API response rather than reloading the entire portal.

Error

Unable to send your message.

Please try again.

The message should remain in the composer so the customer does not lose their text.

Loading

Initial page:

Loading ticket...

Conversation:

Loading conversation...

Sending:

Sending...

Not Found

If the ticket does not exist or the customer has no access:

Ticket not found.

[Back to My Tickets]

Avoid revealing whether the ticket exists for another customer.

Customer Data Isolation

This is a critical security requirement.

Customer A must never be able to access:

Customer B
Ticket
Messages
Attachments

by modifying the ticket ID.

The backend must enforce ownership.

Direct URL Test

Example:

/portal/tickets/123

If ticket 123 belongs to another customer:

Customer A → request → ticket 123
                      ↓
              authorization check
                      ↓
                  rejected

Do not rely only on frontend route guards.

Ticket Status Changes

The customer should not be able to directly change ticket status.

Do not expose:

[Resolve]
[Close]
[Assign]

unless the existing requirements explicitly allow it.

Support agents/system workflows control ticket status.

Customer Reopening

If the existing ticket SDD defines customer reply as reopening a ticket, reuse that behavior.

Otherwise do not introduce reopening logic here.

API Response Security

Do not return the entire internal ticket object.

Use a dedicated customer DTO.

Avoid fields such as:

InternalNotes
AssignedAgentId
InternalSla
EscalationReason
InternalPriority

unless explicitly customer-visible.

Pagination

If conversations can become large, paginate messages.

For the 2-day MVP, if the expected message count is small, a reasonable limit may be sufficient.

Do not load thousands of messages into Vue.

If the project already has message pagination, reuse it.

Refresh

Provide a simple refresh mechanism if useful:

[Refresh]

But do not implement complex real-time messaging unless required.

Real-Time Updates

For the 2-day implementation:

Do not add WebSockets/SignalR just for this story unless the existing architecture already uses them.

Simple refresh/polling is sufficient if real-time behavior is not a requirement.

The important part is the end-to-end customer communication flow.

Notifications

If the project already has notifications, a new agent reply may trigger the existing notification mechanism.

Do not create a separate notification system.

AI Integration

Do not add new AI behavior here.

The customer already has:

AI-004 — AI Customer Chatbot

The customer ticket conversation should remain a normal communication channel.

AI-generated suggested replies belong to the agent experience.

Arabic / English

Support:

	English

	Arabic

	LTR

	RTL

Example:

المحادثة

العميل:
لا أستطيع تسجيل الدخول.

الدعم:
يرجى تجربة إعادة تعيين كلمة المرور.

Responsive Design

Desktop:

Ticket Details
Conversation
Reply Composer

Mobile:

Ticket Header
Conversation
Reply Composer

The reply composer should remain easy to access.

Testing

Backend Tests

Test:

	Customer can retrieve own ticket.

	Customer cannot retrieve another customer's ticket.

	Internal notes excluded.

	Internal fields excluded.

	Customer can add a message to own ticket.

	Customer cannot add a message to another customer's ticket.

	Empty message rejected.

	Message length validation.

	Closed-ticket reply behavior follows ticket rules.

	Attachment validation.

	Customer identity comes from authentication.

	Message sender cannot be spoofed.

	Ticket not found handled correctly.

Frontend Tests

Test:

	Ticket details render.

	Status renders.

	Category renders.

	Messages render.

	Internal notes never render.

	Reply composer works.

	Send button works.

	Loading state.

	Error state.

	Empty conversation state.

	Attachment UI.

	Closed-ticket behavior.

	Arabic RTL.

	Mobile layout.

Integration Tests

Test:

Customer
 ↓
Vue Ticket Details
 ↓
GET /api/portal/tickets/{id}
 ↓
.NET
 ↓
Authorization
 ↓
Customer-safe DTO
 ↓
Vue

And:

Customer
 ↓
Reply
 ↓
POST /api/portal/tickets/{id}/messages
 ↓
Authorization
 ↓
Existing Ticket Communication
 ↓
Database
 ↓
Response
 ↓
Vue

Manual Verification

	Login as Customer A.

	Open one of Customer A's tickets.

	Verify ticket details.

	Verify conversation.

	Verify internal notes are not visible.

	Reply to the ticket.

	Verify message appears.

	Verify the agent can see the message through the normal ticket system.

	Login as Customer B.

	Try to open Customer A's ticket directly.

	Verify access is denied.

	Try sending a message to Customer A's ticket.

	Verify it is rejected.

	Test closed ticket behavior.

	Test attachment.

	Test Arabic.

	Test mobile.

Edge Cases

Handle:

	Ticket not found.

	Ticket belongs to another customer.

	Empty message.

	Very long message.

	Closed ticket.

	Resolved ticket.

	Attachment too large.

	Unsupported attachment.

	Network failure.

	Database failure.

	Duplicate send.

	Customer session expired.

	Internal note.

	Missing category.

	Deleted attachment.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read TKT-008.

	Read TKT-009.

	Read PORTAL-002.

	Inspect the existing ticket communication implementation.

	Inspect the existing message model.

	Reuse the existing communication service.

	Reuse existing attachment handling.

	Reuse existing authorization.

	Do not create a second message system.

	Do not expose internal notes.

	Do not trust customer IDs from the frontend.

	Return customer-safe DTOs.

	Do not add SignalR/WebSockets unless already required.

	Add backend/frontend/integration tests.

	Run relevant tests.

	Review authorization and data exposure.

	Verify every acceptance criterion.

Acceptance Criteria

	Customer can open their ticket.

	Customer can see ticket number.

	Customer can see subject.

	Customer can see status.

	Customer can see category.

	Customer can see customer-visible messages.

	Internal notes are never shown.

	Customer can reply to an allowed ticket.

	Reply uses the existing communication system.

	Customer identity is determined server-side.

	Customer cannot spoof another sender.

	Customer cannot access another customer's ticket.

	Customer cannot change internal ticket properties.

	Attachments follow existing validation/security rules.

	Closed/resolved behavior follows existing ticket rules.

	Loading state is implemented.

	Error state is implemented.

	Not-found state is implemented.

	Duplicate sending is prevented.

	Arabic RTL is supported.

	English LTR is supported.

	Mobile responsive UI is implemented.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	Integration tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Customer ticket details page implemented.

	Customer-visible conversation implemented.

	Customer reply implemented.

	Attachment integration implemented.

	Internal note protection verified.

	Customer ownership verified.

	Existing communication system reused.

	Closed-ticket rules verified.

	Arabic/English verified.

	Responsive UI verified.

	Backend tests pass.

	Frontend tests pass.

	Integration tests pass.

	Manual end-to-end conversation verified.

	No duplicate messaging architecture introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.
```

---

## Acceptance criteria

*(Checklist, bullets, Gherkin, etc. Prefilled for Azure DevOps when the work item has acceptance criteria.)*

```

```

---

## Attachments

Place files in `attachments/` next to this `intake.md`, then list them here so the planner knows what to open.

| File (relative to this folder) | What it is |
| ------------------------------ | ---------- |
| *(e.g. `attachments/flow.png`)* | *(e.g. UX flow)* |

*(Add rows per file. If none, write "None.")*

---

## Dependencies

- **Blocked by / related ids:** (tracker ids only; optional short note)
- **Depends on code areas or other stories:**

## Extra notes (optional)

- Anything not captured above (e.g. chat context) — keep short.

## Technical hints (optional)

- APIs, screens, services already discussed. Repos/roots: `.`. Primary language: `typescript`.

## Out of scope

- What this story explicitly does **not** cover:
