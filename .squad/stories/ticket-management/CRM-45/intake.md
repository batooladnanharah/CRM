> **Fetched from jira:** [CRM-45](https://batooladnanharah.atlassian.net/browse/CRM-45)  
> *Fetched 2026-09-01T11:56:50.452Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** TKT-008 Ticket Conversation  
**Type:** Task  
**Status:** In Review

### Description

User Story

As a support agent, I want to view and send messages within a ticket so that I can communicate with the customer and keep the conversation associated with the support request.

Objective

Provide a simple ticket conversation area where customer and agent messages are displayed chronologically and authorized agents can send replies.

The conversation should be designed so that future Email, WhatsApp, SMS, and Live Chat integrations can use the same message model without implementing those integrations now.

Scope

This story covers:

	Conversation UI

	Message list

	Customer messages

	Agent messages

	Internal notes

	Send message

	Message validation

	Message author

	Timestamp

	Channel

	Authorization

	Loading/empty/error states

	Ticket history integration

	Tests

This story does not cover:

	Real Email integration

	WhatsApp integration

	SMS integration

	Live Chat integration

	AI suggested replies

	AI chatbot

	External messaging providers

Those will be implemented separately.

UI Requirements

The conversation should be displayed on the Ticket Details page from TKT-004.

Recommended layout:

Conversation

─────────────────────────────────────────────────────────────

Ahmed Ali
Customer · Web
10:30 AM

I cannot login to my account.

─────────────────────────────────────────────────────────────

Sara Ahmed
Agent · Web
10:35 AM

Please try resetting your password.

─────────────────────────────────────────────────────────────

Ahmed Ali
Customer · Web
10:40 AM

I tried that but it still doesn't work.

─────────────────────────────────────────────────────────────

[ Reply ▼ ]

[ Type your message...                         ]

[Attach File]                         [Send]

The exact layout should follow the existing CRM design system.

Message Types

The MVP should support:

Customer Message
Agent Message
Internal Note

Customer Message

A message originating from the customer.

Agent Message

A response written by a support agent.

Internal Note

An internal CRM note related to the ticket.

Internal notes must be visually distinct and must never be treated as customer-visible messages.

Example:

Internal Note
────────────────────
Customer has already attempted password reset.

Message Channel

Each message should contain a channel.

Initial supported values:

	Web

	Email

	WhatsApp

	SMS

	LiveChat

For this story, actual external integrations are not required.

The channel is primarily stored so the same conversation model can support future integrations.

Message UI

Each message should display:

	Author

	Author type/role

	Message content

	Timestamp

	Channel

Example:

Sara Ahmed
Agent · Web
24 Aug 2026 10:35

Please try resetting your password.

Conversation Ordering

Messages must be displayed chronologically.

Recommended:

	Oldest message first.

	Newest message last.

When opening a ticket, the UI should normally scroll to the most recent message.

Do not load the entire conversation into memory if pagination is required.

Empty State

If there are no messages:

No messages yet.

Start the conversation by sending a reply.

Message Composer

The composer should provide:

[ Reply ▼ ]

[ Write your message................................ ]

                              [Send]

The message type selector should support:

	Reply

	Internal Note

Only authorized users should be able to create internal notes.

Internal Note Warning

When Internal Note is selected, clearly indicate that the message is internal.

Example:

Internal note

This message will only be visible to CRM users.

This is important to prevent an agent from accidentally sending internal information to the customer.

Message Validation

Frontend

Validate:

	Message is required.

	Whitespace-only messages are rejected.

	Maximum message length is enforced.

	Send is disabled while submitting.

Backend

Repeat validation.

The backend must be authoritative.

API

Get Messages

{{GET /api/tickets/

{id}/messages}}

Recommended query parameters:

?page=1
&pageSize=50

Example response:

{
  "items": [
    {
      "id": "message-id",
      "ticketId": "ticket-id",
      "type": "CustomerMessage",
      "content": "I cannot login.",
      "channel": "Web",
      "author": {
        "id": "customer-id",
        "name": "Ahmed Ali"
      },
      "createdAt": "2026-08-24T10:30:00Z"
    },
    {
      "id": "message-id-2",
      "ticketId": "ticket-id",
      "type": "AgentMessage",
      "content": "Please try resetting your password.",
      "channel": "Web",
      "author": {
        "id": "agent-id",
        "name": "Sara Ahmed"
      },
      "createdAt": "2026-08-24T10:35:00Z"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 2
}

h3. Send Message

{{POST /api/tickets/{id}
/messages}}

Example:

{
  "content": "Please try resetting your password.",
  "type": "AgentMessage",
  "channel": "Web"
}

Backend Requirements

The backend must:

	Require authentication.

	Verify the user can access the ticket.

	Verify the user has permission to send the requested message type.

	Validate the ticket ID.

	Validate message content.

	Validate message type.

	Validate channel.

	Create the message.

	Associate the message with the ticket.

	Associate the message with the authenticated user.

	Persist the message through EF Core/PostgreSQL.

	Record the creation timestamp.

	Create a ticket history event where appropriate.

	Return the created message.

Message Data Model

A simple model is sufficient:

TicketMessage

Id
TicketId
AuthorId
Type
Content
Channel
CreatedAt
UpdatedAt

The author may represent either:

	Customer

	CRM User/Agent

The final model must follow the approved domain design.

Do not create separate tables for every communication channel.

Internal Notes

Internal notes must be protected at the backend level.

A customer-facing endpoint must never return internal notes.

For example:

CRM conversation
    ├── Customer Message
    ├── Agent Message
    └── Internal Note

Customer Portal
    ├── Customer Message
    └── Agent Message

The exact customer portal implementation belongs to E10.

Authorization

Use AUTH-003.

At minimum:

	Admin: allowed.

	Manager: allowed.

	Agent: allowed according to ticket access.

	Customer: may send customer messages only through the customer portal.

	Customer: must never create internal notes.

The backend must enforce these rules.

Channel Handling

For this MVP, the agent may send messages using the Web channel.

Other channel values should be supported by the data model but do not require external integration.

Example:

Web
Email
WhatsApp
SMS
LiveChat

Do not create fake external integrations.

Do not claim an Email/WhatsApp/SMS message was actually delivered unless a real integration exists.

Ticket Status Integration

Sending an agent message does not automatically require a status change in this story.

Do not silently implement:

Send message
      ↓
Ticket automatically becomes In Progress

unless the SDD explicitly requires it.

Keep status behavior controlled by TKT-006.

Ticket History

A successful message may create a history event such as:

Message Added

Agent reply added by Sara Ahmed.
24 Aug 2026 10:35

However, do not duplicate the message content inside history if the history system already references the message.

The implementation should reuse the existing ticket history architecture.

Loading State

When loading messages:

Loading conversation...

When sending:

[ Sending... ]

The Send button must be disabled while the request is processing.

Optimistic UI

Optimistic message rendering is optional.

For the MVP, prefer the safer approach:

Send
 ↓
API request
 ↓
Success
 ↓
Add message to conversation

Do not implement complicated optimistic synchronization unless the existing frontend architecture already supports it.

Error Handling

Handle:

	Ticket not found

	Unauthorized access

	Invalid message

	Invalid message type

	Invalid channel

	API failure

	Database failure

	Network failure

Example:

Unable to send message.

Please try again.

The typed message should remain available in the composer when practical so the agent does not lose their work.

Testing

Backend/API Tests

Test:

	Authorized agent can retrieve messages.

	Authorized agent can send a message.

	Customer message can be represented correctly.

	Agent message can be created.

	Internal note can be created by authorized CRM user.

	Customer cannot create internal notes.

	Unauthorized user cannot access conversation.

	Empty message is rejected.

	Whitespace-only message is rejected.

	Invalid message type is rejected.

	Invalid channel is rejected.

	Message is associated with correct ticket.

	Author is recorded.

	Timestamp is recorded.

	Internal notes are excluded from customer-facing responses.

	History is recorded where required.

	Database failure is handled.

Frontend Tests

Test:

	Conversation renders.

	Messages display chronologically.

	Customer message displays correctly.

	Agent message displays correctly.

	Internal note is visually distinct.

	Composer renders.

	Reply/Internal Note selector works.

	Validation works.

	Send works.

	Loading state works.

	Empty state works.

	Error state works.

	Message remains available after failed send.

	New message appears after successful send.

Manual Verification

	Open a ticket.

	Open Conversation.

	Verify existing messages.

	Verify chronological ordering.

	Send an agent reply.

	Verify the reply appears.

	Refresh the page.

	Verify the message persisted.

	Create an internal note.

	Verify it is visually different.

	Verify the internal note is not exposed through customer-facing functionality.

	Try an empty message.

	Verify validation.

	Simulate API failure.

	Verify the message is not lost.

Edge Cases

Handle:

	Ticket does not exist.

	Ticket has no messages.

	Very long message.

	Whitespace-only message.

	Invalid message type.

	Invalid channel.

	User loses authorization.

	Network failure during send.

	Database failure.

	Duplicate send click.

	Multiple users sending messages.

	Internal note accidentally selected.

	Customer attempts to access internal note.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read TKT-004.

	Read TKT-006.

	Read TKT-009 if history is already implemented.

	Inspect the existing ticket details page.

	Reuse existing CRM message/chat components if available.

	Reuse the existing authentication and authorization.

	Do not create separate message tables for Email, WhatsApp, SMS, and Live Chat.

	Use a channel field so future integrations can be added.

	Do not implement external messaging integrations.

	Do not implement AI suggested replies.

	Do not implement an AI chatbot.

	Do not expose internal notes to customers.

	Do not introduce WebSockets unless already available and necessary.

	A normal API request/refresh approach is acceptable for the MVP.

	Add backend and frontend tests.

	Run relevant tests.

	Review the authorization/privacy behavior carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized users can view ticket messages.

	Messages are displayed chronologically.

	Customer messages are displayed.

	Agent messages are displayed.

	Internal notes are supported.

	Internal notes are visually distinct.

	Authorized agents can send replies.

	Authorized users can create internal notes.

	Customers cannot create internal notes.

	Message content is validated.

	Message author is recorded.

	Message timestamp is recorded.

	Message channel is recorded.

	Messages are persisted in PostgreSQL.

	Ticket relationship is persisted.

	Internal notes are protected from customer-facing APIs.

	Loading state is implemented.

	Empty state is implemented.

	Error state is implemented.

	Duplicate message submission is prevented.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Conversation UI implemented.

	Message API implemented.

	PostgreSQL message persistence implemented.

	Customer/agent message support implemented.

	Internal note support implemented.

	Channel field implemented.

	Authorization implemented.

	Internal note privacy verified.

	Loading/empty/error states implemented.

	Ticket history integration implemented/reused.

	Backend tests pass.

	Frontend tests pass.

	Manual conversation flow verified.

	No external messaging integration introduced.

	No unnecessary real-time infrastructure introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/ticket-management/CRM-45/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `ticket-management`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-45` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
TKT-008 Ticket Conversation
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to view and send messages within a ticket so that I can communicate with the customer and keep the conversation associated with the support request.

Objective

Provide a simple ticket conversation area where customer and agent messages are displayed chronologically and authorized agents can send replies.

The conversation should be designed so that future Email, WhatsApp, SMS, and Live Chat integrations can use the same message model without implementing those integrations now.

Scope

This story covers:

	Conversation UI

	Message list

	Customer messages

	Agent messages

	Internal notes

	Send message

	Message validation

	Message author

	Timestamp

	Channel

	Authorization

	Loading/empty/error states

	Ticket history integration

	Tests

This story does not cover:

	Real Email integration

	WhatsApp integration

	SMS integration

	Live Chat integration

	AI suggested replies

	AI chatbot

	External messaging providers

Those will be implemented separately.

UI Requirements

The conversation should be displayed on the Ticket Details page from TKT-004.

Recommended layout:

Conversation

─────────────────────────────────────────────────────────────

Ahmed Ali
Customer · Web
10:30 AM

I cannot login to my account.

─────────────────────────────────────────────────────────────

Sara Ahmed
Agent · Web
10:35 AM

Please try resetting your password.

─────────────────────────────────────────────────────────────

Ahmed Ali
Customer · Web
10:40 AM

I tried that but it still doesn't work.

─────────────────────────────────────────────────────────────

[ Reply ▼ ]

[ Type your message...                         ]

[Attach File]                         [Send]

The exact layout should follow the existing CRM design system.

Message Types

The MVP should support:

Customer Message
Agent Message
Internal Note

Customer Message

A message originating from the customer.

Agent Message

A response written by a support agent.

Internal Note

An internal CRM note related to the ticket.

Internal notes must be visually distinct and must never be treated as customer-visible messages.

Example:

Internal Note
────────────────────
Customer has already attempted password reset.

Message Channel

Each message should contain a channel.

Initial supported values:

	Web

	Email

	WhatsApp

	SMS

	LiveChat

For this story, actual external integrations are not required.

The channel is primarily stored so the same conversation model can support future integrations.

Message UI

Each message should display:

	Author

	Author type/role

	Message content

	Timestamp

	Channel

Example:

Sara Ahmed
Agent · Web
24 Aug 2026 10:35

Please try resetting your password.

Conversation Ordering

Messages must be displayed chronologically.

Recommended:

	Oldest message first.

	Newest message last.

When opening a ticket, the UI should normally scroll to the most recent message.

Do not load the entire conversation into memory if pagination is required.

Empty State

If there are no messages:

No messages yet.

Start the conversation by sending a reply.

Message Composer

The composer should provide:

[ Reply ▼ ]

[ Write your message................................ ]

                              [Send]

The message type selector should support:

	Reply

	Internal Note

Only authorized users should be able to create internal notes.

Internal Note Warning

When Internal Note is selected, clearly indicate that the message is internal.

Example:

Internal note

This message will only be visible to CRM users.

This is important to prevent an agent from accidentally sending internal information to the customer.

Message Validation

Frontend

Validate:

	Message is required.

	Whitespace-only messages are rejected.

	Maximum message length is enforced.

	Send is disabled while submitting.

Backend

Repeat validation.

The backend must be authoritative.

API

Get Messages

{ {GET /api/tickets/

{id}/messages}}

Recommended query parameters:

?page=1
&pageSize=50

Example response:

{
  "items": [
    {
      "id": "message-id",
      "ticketId": "ticket-id",
      "type": "CustomerMessage",
      "content": "I cannot login.",
      "channel": "Web",
      "author": {
        "id": "customer-id",
        "name": "Ahmed Ali"
      },
      "createdAt": "2026-08-24T10:30:00Z"
    },
    {
      "id": "message-id-2",
      "ticketId": "ticket-id",
      "type": "AgentMessage",
      "content": "Please try resetting your password.",
      "channel": "Web",
      "author": {
        "id": "agent-id",
        "name": "Sara Ahmed"
      },
      "createdAt": "2026-08-24T10:35:00Z"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 2
}

h3. Send Message

{ {POST /api/tickets/{id}
/messages}}

Example:

{
  "content": "Please try resetting your password.",
  "type": "AgentMessage",
  "channel": "Web"
}

Backend Requirements

The backend must:

	Require authentication.

	Verify the user can access the ticket.

	Verify the user has permission to send the requested message type.

	Validate the ticket ID.

	Validate message content.

	Validate message type.

	Validate channel.

	Create the message.

	Associate the message with the ticket.

	Associate the message with the authenticated user.

	Persist the message through EF Core/PostgreSQL.

	Record the creation timestamp.

	Create a ticket history event where appropriate.

	Return the created message.

Message Data Model

A simple model is sufficient:

TicketMessage

Id
TicketId
AuthorId
Type
Content
Channel
CreatedAt
UpdatedAt

The author may represent either:

	Customer

	CRM User/Agent

The final model must follow the approved domain design.

Do not create separate tables for every communication channel.

Internal Notes

Internal notes must be protected at the backend level.

A customer-facing endpoint must never return internal notes.

For example:

CRM conversation
    ├── Customer Message
    ├── Agent Message
    └── Internal Note

Customer Portal
    ├── Customer Message
    └── Agent Message

The exact customer portal implementation belongs to E10.

Authorization

Use AUTH-003.

At minimum:

	Admin: allowed.

	Manager: allowed.

	Agent: allowed according to ticket access.

	Customer: may send customer messages only through the customer portal.

	Customer: must never create internal notes.

The backend must enforce these rules.

Channel Handling

For this MVP, the agent may send messages using the Web channel.

Other channel values should be supported by the data model but do not require external integration.

Example:

Web
Email
WhatsApp
SMS
LiveChat

Do not create fake external integrations.

Do not claim an Email/WhatsApp/SMS message was actually delivered unless a real integration exists.

Ticket Status Integration

Sending an agent message does not automatically require a status change in this story.

Do not silently implement:

Send message
      ↓
Ticket automatically becomes In Progress

unless the SDD explicitly requires it.

Keep status behavior controlled by TKT-006.

Ticket History

A successful message may create a history event such as:

Message Added

Agent reply added by Sara Ahmed.
24 Aug 2026 10:35

However, do not duplicate the message content inside history if the history system already references the message.

The implementation should reuse the existing ticket history architecture.

Loading State

When loading messages:

Loading conversation...

When sending:

[ Sending... ]

The Send button must be disabled while the request is processing.

Optimistic UI

Optimistic message rendering is optional.

For the MVP, prefer the safer approach:

Send
 ↓
API request
 ↓
Success
 ↓
Add message to conversation

Do not implement complicated optimistic synchronization unless the existing frontend architecture already supports it.

Error Handling

Handle:

	Ticket not found

	Unauthorized access

	Invalid message

	Invalid message type

	Invalid channel

	API failure

	Database failure

	Network failure

Example:

Unable to send message.

Please try again.

The typed message should remain available in the composer when practical so the agent does not lose their work.

Testing

Backend/API Tests

Test:

	Authorized agent can retrieve messages.

	Authorized agent can send a message.

	Customer message can be represented correctly.

	Agent message can be created.

	Internal note can be created by authorized CRM user.

	Customer cannot create internal notes.

	Unauthorized user cannot access conversation.

	Empty message is rejected.

	Whitespace-only message is rejected.

	Invalid message type is rejected.

	Invalid channel is rejected.

	Message is associated with correct ticket.

	Author is recorded.

	Timestamp is recorded.

	Internal notes are excluded from customer-facing responses.

	History is recorded where required.

	Database failure is handled.

Frontend Tests

Test:

	Conversation renders.

	Messages display chronologically.

	Customer message displays correctly.

	Agent message displays correctly.

	Internal note is visually distinct.

	Composer renders.

	Reply/Internal Note selector works.

	Validation works.

	Send works.

	Loading state works.

	Empty state works.

	Error state works.

	Message remains available after failed send.

	New message appears after successful send.

Manual Verification

	Open a ticket.

	Open Conversation.

	Verify existing messages.

	Verify chronological ordering.

	Send an agent reply.

	Verify the reply appears.

	Refresh the page.

	Verify the message persisted.

	Create an internal note.

	Verify it is visually different.

	Verify the internal note is not exposed through customer-facing functionality.

	Try an empty message.

	Verify validation.

	Simulate API failure.

	Verify the message is not lost.

Edge Cases

Handle:

	Ticket does not exist.

	Ticket has no messages.

	Very long message.

	Whitespace-only message.

	Invalid message type.

	Invalid channel.

	User loses authorization.

	Network failure during send.

	Database failure.

	Duplicate send click.

	Multiple users sending messages.

	Internal note accidentally selected.

	Customer attempts to access internal note.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read TKT-004.

	Read TKT-006.

	Read TKT-009 if history is already implemented.

	Inspect the existing ticket details page.

	Reuse existing CRM message/chat components if available.

	Reuse the existing authentication and authorization.

	Do not create separate message tables for Email, WhatsApp, SMS, and Live Chat.

	Use a channel field so future integrations can be added.

	Do not implement external messaging integrations.

	Do not implement AI suggested replies.

	Do not implement an AI chatbot.

	Do not expose internal notes to customers.

	Do not introduce WebSockets unless already available and necessary.

	A normal API request/refresh approach is acceptable for the MVP.

	Add backend and frontend tests.

	Run relevant tests.

	Review the authorization/privacy behavior carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Authorized users can view ticket messages.

	Messages are displayed chronologically.

	Customer messages are displayed.

	Agent messages are displayed.

	Internal notes are supported.

	Internal notes are visually distinct.

	Authorized agents can send replies.

	Authorized users can create internal notes.

	Customers cannot create internal notes.

	Message content is validated.

	Message author is recorded.

	Message timestamp is recorded.

	Message channel is recorded.

	Messages are persisted in PostgreSQL.

	Ticket relationship is persisted.

	Internal notes are protected from customer-facing APIs.

	Loading state is implemented.

	Empty state is implemented.

	Error state is implemented.

	Duplicate message submission is prevented.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Conversation UI implemented.

	Message API implemented.

	PostgreSQL message persistence implemented.

	Customer/agent message support implemented.

	Internal note support implemented.

	Channel field implemented.

	Authorization implemented.

	Internal note privacy verified.

	Loading/empty/error states implemented.

	Ticket history integration implemented/reused.

	Backend tests pass.

	Frontend tests pass.

	Manual conversation flow verified.

	No external messaging integration introduced.

	No unnecessary real-time infrastructure introduced.

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
