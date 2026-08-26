> **Fetched from jira:** [CRM-50](https://batooladnanharah.atlassian.net/browse/CRM-50)  
> *Fetched 2026-08-26T14:53:57.122Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** COM-002 — Email Communication  
**Type:** Task  
**Status:** In Review  
**Assignee:** Batool Harah

### Description

User Story

As a support agent, I want to send and receive customer communication through email so that email-based support requests can be handled as part of the CRM ticket conversation.

Objective

Enable the CRM to represent email communication as part of an existing ticket conversation.

The implementation should provide a clean abstraction for email delivery so that a real provider can be connected without changing the Ticket or Conversation domain.

Scope

This story covers:

	Email channel support

	Email message model integration

	Email sending abstraction

	Email API

	Email configuration

	Email message status

	Ticket association

	Basic email UI

	Validation

	Error handling

	Tests

This story does not require a production email provider for the 2-day assessment.

Actual provider integration may be implemented through:

	SMTP

	SendGrid

	Microsoft Graph

	Amazon SES

	Another approved provider

The provider should be selected through configuration.

Important MVP Decision

For the assessment:

Do not spend significant implementation time configuring an external email provider.

The application should demonstrate:

Agent
  ↓
CRM
  ↓
Email Service Abstraction
  ↓
Provider

The provider can initially be:

DevelopmentEmailProvider

which records/logs the outgoing email instead of sending it externally.

If a real provider is already available, the real implementation may be used.

Architecture

Do not put email-sending logic directly inside:

	Vue components

	Ticket entity

	Ticket controller

Use a backend abstraction.

Example:

IEmailService
      │
      ├── SmtpEmailService
      ├── SendGridEmailService
      └── DevelopmentEmailService

The Ticket application service should depend on the abstraction.

Email Message

Email communication should use the existing TicketMessage model from TKT-008 / COM-001.

Example:

TicketMessage

Type = AgentMessage
Channel = Email
Content = "Please try resetting your password."

Do not create a separate EmailTicket or EmailConversation entity.

Email Metadata

If required by the integration, email-specific metadata may be stored separately.

Example:

EmailMessageMetadata

Id
MessageId
From
To
Cc
Bcc
Subject
ProviderMessageId
DeliveryStatus
CreatedAt

Do not add email-specific fields to the core Ticket entity.

The AI should first inspect the existing message/domain architecture before creating this entity.

UI Requirements

The ticket conversation should allow the agent to select Email as the communication channel.

Example:

Reply Via

[ Web ▼ ]

Web
Email

When Email is selected:

To:
Ahmed Ali <ahmed@example.com>

Subject:
Re: Unable to login

Message:
[ Please try resetting your password................ ]

                           [Send Email]

The customer email should be populated from the customer profile where available.

Subject

For an email reply, the subject should normally be derived from the ticket subject.

Example:

Ticket Subject:
Unable to login

Email Subject:
Re: Unable to login

Do not require the agent to manually enter the subject for every reply.

Recipient

The default recipient should come from the ticket's customer contact information.

Example:

Customer:
Ahmed Ali

Email:
ahmed@example.com

The backend must validate the recipient.

Do not trust a recipient supplied by the browser if the system can determine the customer email from the ticket.

Sending Email

Recommended flow:

Agent
 ↓
Select Email
 ↓
Compose message
 ↓
POST /api/tickets/{id}/messages
 ↓
Create TicketMessage
 ↓
Email Service
 ↓
Provider

The exact flow may vary according to the existing architecture.

API

Reuse the existing ticket message endpoint where practical.

Example:

{{POST /api/tickets/

{id}
/messages}}

Request:

{
  "type": "AgentMessage",
  "channel": "Email",
  "content": "Please try resetting your password."
}

The backend should derive:

	Recipient

	Customer

	Ticket

	Sender

from trusted server-side data.

If the API requires email-specific information, use a dedicated request DTO rather than changing the core Ticket entity.

Email Sending Service

Create/use:

IEmailService

Example conceptual method:

SendAsync(
    recipient,
    subject,
    body
)

The exact interface should follow the project's architecture.

Development Provider

For the assessment, a development implementation is acceptable.

Example:

DevelopmentEmailService

Input:
To
Subject
Body

Output:
Logged/stored email

This allows the evaluator to verify the complete application flow without requiring external credentials.

Clearly document that this is a development implementation.

Configuration

Email configuration should be stored in backend configuration.

Example:

Email:
  Provider
  FromAddress
  FromName

If using a real SMTP provider:

SMTP:
  Host
  Port
  Username
  Password

Secrets must come from secure configuration/environment variables.

Never commit secrets to Git.

Delivery Status

The MVP may support:

Pending
Sent
Failed

Example:

Email sent

or:

Email failed to send.

Do not implement a complex email delivery tracking system.

Failure Behavior

If email delivery fails:

	Do not pretend the email was delivered.

	Show an error to the agent.

	Preserve the message content where practical.

	Log the technical failure server-side.

	Do not expose provider credentials or stack traces.

Example:

Unable to send email.

Please try again.

Database Consistency

The system must avoid incorrectly recording an email as successfully sent when delivery failed.

For the MVP, a simple approach is acceptable:

Create/send email
      ↓
Provider result
      ↓
Update message delivery status

Do not introduce distributed transactions.

Incoming Email

Receiving email is significantly more complex than sending email because it requires:

	Mailbox access

	Webhooks/polling

	Message parsing

	Thread matching

	Security validation

For the 2-day assessment, incoming email does not need to be implemented unless a provider is already available.

The architecture should leave room for:

Incoming Email
      ↓
Email Adapter
      ↓
Find Ticket
      ↓
TicketMessage
      ↓
Channel = Email

Ticket Matching

If incoming email is implemented later, matching should use a reliable identifier such as:

Ticket reference

Example:

Re: [Ticket #1001] Unable to login

Do not attempt fuzzy matching based only on customer name.

Security

Never expose:

	SMTP password

	API keys

	Provider credentials

to the Vue application.

Email sending must happen server-side.

Validate recipient addresses server-side.

Authorization

Only authorized CRM users may send emails through the CRM.

Use AUTH-003.

Customers should not use the internal agent email endpoint.

Customer communication through the Customer Portal is handled separately.

History

A successful email message should appear in:

	Ticket conversation

	Ticket history where applicable

Example:

Email Sent

Agent sent an email to Ahmed Ali.

by Sara Ahmed

Reuse TKT-009.

Do not create a separate email history system.

Attachments

Email attachments are out of scope for this story unless the existing attachment architecture already supports them cleanly.

Do not spend assessment time building email attachment processing.

Testing

Backend/API Tests

Test:

	Authorized agent can send email.

	Unauthorized user cannot send email.

	Customer email is retrieved correctly.

	Invalid customer email is rejected.

	Empty message rejected.

	Email service is called.

	Ticket message is created with Channel = Email.

	Successful email produces Sent status.

	Email provider failure produces Failed status.

	Email credentials are not exposed.

	Ticket history is created where applicable.

Frontend Tests

Test:

	Email channel can be selected.

	Customer email is displayed.

	Subject is populated.

	Message can be entered.

	Send Email works.

	Loading state works.

	Failure message works.

	Message is not lost after failure.

Manual Verification

	Open a ticket.

	Select Email.

	Verify customer email.

	Verify subject.

	Enter a response.

	Click Send Email.

	Verify the backend email service is called.

	Verify TicketMessage is created.

	Verify channel is Email.

	Verify history.

	Simulate provider failure.

	Verify the UI reports failure.

Edge Cases

Handle:

	Customer has no email.

	Invalid email address.

	Empty message.

	Very long message.

	Provider unavailable.

	Provider timeout.

	Provider returns failure.

	Unauthorized user.

	Ticket not found.

	Customer not found.

	Expired authentication.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read TKT-008.

	Read TKT-009.

	Read COM-001.

	Inspect existing customer contact information.

	Inspect existing TicketMessage implementation.

	Reuse the existing conversation UI.

	Create/use an IEmailService abstraction.

	Keep provider-specific code outside the Ticket domain.

	Use a development email provider if no real provider is available.

	Do not add real email credentials to source control.

	Do not expose email credentials to Vue.

	Do not implement incoming email unless a provider is already available.

	Do not implement email attachments unless already supported.

	Add backend and frontend tests.

	Run relevant tests.

	Review security and failure handling.

	Verify every acceptance criterion.

Acceptance Criteria

	Agent can select Email as a communication channel.

	Customer email is displayed.

	Email subject is generated from the ticket.

	Agent can compose an email.

	Email is sent through a backend service abstraction.

	TicketMessage.Channel is Email.

	Email sending occurs server-side.

	Email credentials are never exposed to Vue.

	Development email provider works without external credentials.

	Real provider can be configured later.

	Email delivery failure is handled.

	Email status is represented where implemented.

	Email appears in ticket conversation.

	Ticket history is updated where applicable.

	Unauthorized users cannot send email.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Email channel implemented.

	Email UI implemented.

	Email service abstraction implemented.

	Development provider implemented if no real provider exists.

	TicketMessage integration implemented.

	Authorization implemented.

	Error handling implemented.

	PostgreSQL persistence verified.

	Tests pass.

	Manual send flow verified.

	Provider credentials secured.

	No unnecessary external integration introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/communication-channel/CRM-50/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `communication-channel`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-50` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Review`
- **Assignee:** `Batool Harah`
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
COM-002 — Email Communication
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to send and receive customer communication through email so that email-based support requests can be handled as part of the CRM ticket conversation.

Objective

Enable the CRM to represent email communication as part of an existing ticket conversation.

The implementation should provide a clean abstraction for email delivery so that a real provider can be connected without changing the Ticket or Conversation domain.

Scope

This story covers:

	Email channel support

	Email message model integration

	Email sending abstraction

	Email API

	Email configuration

	Email message status

	Ticket association

	Basic email UI

	Validation

	Error handling

	Tests

This story does not require a production email provider for the 2-day assessment.

Actual provider integration may be implemented through:

	SMTP

	SendGrid

	Microsoft Graph

	Amazon SES

	Another approved provider

The provider should be selected through configuration.

Important MVP Decision

For the assessment:

Do not spend significant implementation time configuring an external email provider.

The application should demonstrate:

Agent
  ↓
CRM
  ↓
Email Service Abstraction
  ↓
Provider

The provider can initially be:

DevelopmentEmailProvider

which records/logs the outgoing email instead of sending it externally.

If a real provider is already available, the real implementation may be used.

Architecture

Do not put email-sending logic directly inside:

	Vue components

	Ticket entity

	Ticket controller

Use a backend abstraction.

Example:

IEmailService
      │
      ├── SmtpEmailService
      ├── SendGridEmailService
      └── DevelopmentEmailService

The Ticket application service should depend on the abstraction.

Email Message

Email communication should use the existing TicketMessage model from TKT-008 / COM-001.

Example:

TicketMessage

Type = AgentMessage
Channel = Email
Content = "Please try resetting your password."

Do not create a separate EmailTicket or EmailConversation entity.

Email Metadata

If required by the integration, email-specific metadata may be stored separately.

Example:

EmailMessageMetadata

Id
MessageId
From
To
Cc
Bcc
Subject
ProviderMessageId
DeliveryStatus
CreatedAt

Do not add email-specific fields to the core Ticket entity.

The AI should first inspect the existing message/domain architecture before creating this entity.

UI Requirements

The ticket conversation should allow the agent to select Email as the communication channel.

Example:

Reply Via

[ Web ▼ ]

Web
Email

When Email is selected:

To:
Ahmed Ali <ahmed@example.com>

Subject:
Re: Unable to login

Message:
[ Please try resetting your password................ ]

                           [Send Email]

The customer email should be populated from the customer profile where available.

Subject

For an email reply, the subject should normally be derived from the ticket subject.

Example:

Ticket Subject:
Unable to login

Email Subject:
Re: Unable to login

Do not require the agent to manually enter the subject for every reply.

Recipient

The default recipient should come from the ticket's customer contact information.

Example:

Customer:
Ahmed Ali

Email:
ahmed@example.com

The backend must validate the recipient.

Do not trust a recipient supplied by the browser if the system can determine the customer email from the ticket.

Sending Email

Recommended flow:

Agent
 ↓
Select Email
 ↓
Compose message
 ↓
POST /api/tickets/{id}/messages
 ↓
Create TicketMessage
 ↓
Email Service
 ↓
Provider

The exact flow may vary according to the existing architecture.

API

Reuse the existing ticket message endpoint where practical.

Example:

{ {POST /api/tickets/

{id}
/messages}}

Request:

{
  "type": "AgentMessage",
  "channel": "Email",
  "content": "Please try resetting your password."
}

The backend should derive:

	Recipient

	Customer

	Ticket

	Sender

from trusted server-side data.

If the API requires email-specific information, use a dedicated request DTO rather than changing the core Ticket entity.

Email Sending Service

Create/use:

IEmailService

Example conceptual method:

SendAsync(
    recipient,
    subject,
    body
)

The exact interface should follow the project's architecture.

Development Provider

For the assessment, a development implementation is acceptable.

Example:

DevelopmentEmailService

Input:
To
Subject
Body

Output:
Logged/stored email

This allows the evaluator to verify the complete application flow without requiring external credentials.

Clearly document that this is a development implementation.

Configuration

Email configuration should be stored in backend configuration.

Example:

Email:
  Provider
  FromAddress
  FromName

If using a real SMTP provider:

SMTP:
  Host
  Port
  Username
  Password

Secrets must come from secure configuration/environment variables.

Never commit secrets to Git.

Delivery Status

The MVP may support:

Pending
Sent
Failed

Example:

Email sent

or:

Email failed to send.

Do not implement a complex email delivery tracking system.

Failure Behavior

If email delivery fails:

	Do not pretend the email was delivered.

	Show an error to the agent.

	Preserve the message content where practical.

	Log the technical failure server-side.

	Do not expose provider credentials or stack traces.

Example:

Unable to send email.

Please try again.

Database Consistency

The system must avoid incorrectly recording an email as successfully sent when delivery failed.

For the MVP, a simple approach is acceptable:

Create/send email
      ↓
Provider result
      ↓
Update message delivery status

Do not introduce distributed transactions.

Incoming Email

Receiving email is significantly more complex than sending email because it requires:

	Mailbox access

	Webhooks/polling

	Message parsing

	Thread matching

	Security validation

For the 2-day assessment, incoming email does not need to be implemented unless a provider is already available.

The architecture should leave room for:

Incoming Email
      ↓
Email Adapter
      ↓
Find Ticket
      ↓
TicketMessage
      ↓
Channel = Email

Ticket Matching

If incoming email is implemented later, matching should use a reliable identifier such as:

Ticket reference

Example:

Re: [Ticket #1001] Unable to login

Do not attempt fuzzy matching based only on customer name.

Security

Never expose:

	SMTP password

	API keys

	Provider credentials

to the Vue application.

Email sending must happen server-side.

Validate recipient addresses server-side.

Authorization

Only authorized CRM users may send emails through the CRM.

Use AUTH-003.

Customers should not use the internal agent email endpoint.

Customer communication through the Customer Portal is handled separately.

History

A successful email message should appear in:

	Ticket conversation

	Ticket history where applicable

Example:

Email Sent

Agent sent an email to Ahmed Ali.

by Sara Ahmed

Reuse TKT-009.

Do not create a separate email history system.

Attachments

Email attachments are out of scope for this story unless the existing attachment architecture already supports them cleanly.

Do not spend assessment time building email attachment processing.

Testing

Backend/API Tests

Test:

	Authorized agent can send email.

	Unauthorized user cannot send email.

	Customer email is retrieved correctly.

	Invalid customer email is rejected.

	Empty message rejected.

	Email service is called.

	Ticket message is created with Channel = Email.

	Successful email produces Sent status.

	Email provider failure produces Failed status.

	Email credentials are not exposed.

	Ticket history is created where applicable.

Frontend Tests

Test:

	Email channel can be selected.

	Customer email is displayed.

	Subject is populated.

	Message can be entered.

	Send Email works.

	Loading state works.

	Failure message works.

	Message is not lost after failure.

Manual Verification

	Open a ticket.

	Select Email.

	Verify customer email.

	Verify subject.

	Enter a response.

	Click Send Email.

	Verify the backend email service is called.

	Verify TicketMessage is created.

	Verify channel is Email.

	Verify history.

	Simulate provider failure.

	Verify the UI reports failure.

Edge Cases

Handle:

	Customer has no email.

	Invalid email address.

	Empty message.

	Very long message.

	Provider unavailable.

	Provider timeout.

	Provider returns failure.

	Unauthorized user.

	Ticket not found.

	Customer not found.

	Expired authentication.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Read TKT-008.

	Read TKT-009.

	Read COM-001.

	Inspect existing customer contact information.

	Inspect existing TicketMessage implementation.

	Reuse the existing conversation UI.

	Create/use an IEmailService abstraction.

	Keep provider-specific code outside the Ticket domain.

	Use a development email provider if no real provider is available.

	Do not add real email credentials to source control.

	Do not expose email credentials to Vue.

	Do not implement incoming email unless a provider is already available.

	Do not implement email attachments unless already supported.

	Add backend and frontend tests.

	Run relevant tests.

	Review security and failure handling.

	Verify every acceptance criterion.

Acceptance Criteria

	Agent can select Email as a communication channel.

	Customer email is displayed.

	Email subject is generated from the ticket.

	Agent can compose an email.

	Email is sent through a backend service abstraction.

	TicketMessage.Channel is Email.

	Email sending occurs server-side.

	Email credentials are never exposed to Vue.

	Development email provider works without external credentials.

	Real provider can be configured later.

	Email delivery failure is handled.

	Email status is represented where implemented.

	Email appears in ticket conversation.

	Ticket history is updated where applicable.

	Unauthorized users cannot send email.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Email channel implemented.

	Email UI implemented.

	Email service abstraction implemented.

	Development provider implemented if no real provider exists.

	TicketMessage integration implemented.

	Authorization implemented.

	Error handling implemented.

	PostgreSQL persistence verified.

	Tests pass.

	Manual send flow verified.

	Provider credentials secured.

	No unnecessary external integration introduced.

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
