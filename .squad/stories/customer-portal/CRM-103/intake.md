> **Fetched from jira:** [CRM-103](https://batooladnanharah.atlassian.net/browse/CRM-103)  
> *Fetched 2026-08-25T20:20:27.407Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** Customer Portal — Submit & Track Tickets  
**Type:** CRM  
**Status:** To Do  
**Assignee:** Batool Harah

### Description

User Story

As a customer, I want to access a customer portal where I can submit support tickets and track my existing requests so that I can manage my support requests without accessing the internal CRM.

Objective

Provide a simple customer-facing portal that allows authenticated customers to:

	View their portal dashboard.

	Submit a new support ticket.

	View their own submitted tickets.

	Track the current status of their tickets.

	Open a ticket and view its history/conversation.

This story consolidates the core functionality from:

	PORTAL-001 — Customer Portal Dashboard

	PORTAL-002 — Submit & Track Support Tickets

	PORTAL-003 — Customer Ticket History

These original stories remain as business requirements for traceability. This story is the single implementation unit.

Scope

Customer Dashboard

Provide a customer-facing dashboard showing basic information such as:

	Welcome message

	Number of open tickets

	Number of pending tickets

	Recent tickets

	Action to create a new ticket

Example:

Customer Portal

Welcome, Ahmed

My Tickets
────────────────────────

Open        2
Pending     1
Resolved    4

Recent Tickets

#1001  Unable to login       Open
#1005  Payment issue         Pending

[ Submit New Ticket ]
[ View My Tickets ]

Submit Ticket

Customers can create a support ticket.

Minimum fields:

	Subject

	Description

	Category if the existing ticket model already supports it

	Priority if the existing customer workflow allows customers to select it

Do not introduce new ticket fields unless required by the existing SDD/domain model.

After successful submission, show confirmation and the created ticket.

Example:

Ticket submitted successfully.

Ticket #1006

[ View Ticket ]

My Tickets

Customers can view only their own tickets.

Display at minimum:

	Ticket number

	Subject

	Status

	Priority where available

	Created date

	Updated date

Provide basic search/filtering only if the existing ticket functionality can be reused easily.

Do not create a second ticket-management system.

Ticket Details / History

Customers can open one of their tickets and view:

	Ticket information

	Current status

	Conversation/history available to the customer

Reuse the existing ticket details and conversation functionality where possible.

Customers must not see internal CRM information such as:

	Internal notes

	Administrative information

	Other customers' tickets

Security

The backend must determine the current customer from the authenticated identity.

Do not trust a customer ID supplied by the frontend to determine which tickets can be viewed.

Correct:

Authenticated Customer
        ↓
Backend
        ↓
Current Customer ID
        ↓
Customer's Tickets

A customer must never be able to access another customer's ticket by changing an ID in the URL or API request.

Authorization

Reuse the existing authentication and authorization system.

Customer users may:

	Access the customer portal.

	Create their own tickets.

	View their own tickets.

	View their own ticket history/conversation.

Customers must not:

	View another customer's tickets.

	Modify another customer's tickets.

	Access internal CRM administration.

	View internal notes.

Backend authorization is authoritative.

API

Reuse existing ticket APIs where possible.

Before creating new endpoints, inspect the existing ticket implementation.

Possible endpoints if existing APIs do not support the portal:

GET  /api/customer/tickets
GET  /api/customer/tickets/{id}
POST /api/customer/tickets

The backend should determine the current customer from authentication.

Do not create duplicate Ticket entities.

Loading States

Implement appropriate loading states for:

	Dashboard

	Ticket list

	Ticket details

	Ticket submission

Example:

Loading your tickets...

Error Handling

Handle:

	Unauthorized customer

	Ticket not found

	Ticket belonging to another customer

	Invalid ticket data

	API failure

	Network failure

Do not expose technical exception details.

Example:

Unable to load your tickets.

[Try Again]

Empty States

If the customer has no tickets:

You don't have any support tickets yet.

[Submit a Ticket]

Success States

After creating a ticket:

Your ticket has been submitted successfully.

Ticket #1006

After loading tickets, display the customer's tickets normally.

Responsive UI

The portal should work on:

	Desktop

	Tablet

	Mobile

Reuse the existing responsive layout/components.

Arabic / English

Reuse the existing project's:

	English translations

	Arabic translations

	LTR/RTL support

Do not create a separate localization system.

Not in Scope

Do not implement:

	Customer feedback/satisfaction

	Advanced customer analytics

	Customer-to-customer visibility

	Customer administration

	Advanced ticket filtering

	Real-time notifications

	New chat/communication infrastructure

	New ticket management system

	AI functionality

PORTAL-004 — Customer Feedback & Satisfaction is deferred.

Reuse

Reuse existing:

	Authentication

	Customer identity

	Ticket entity

	Ticket creation

	Ticket list

	Ticket details

	Ticket conversation/history

	API client

	i18n

	UI components

	Status/priority components

Do not duplicate existing CRM functionality.

Acceptance Criteria

	Customer can access the customer portal.

	Customer dashboard is displayed.

	Customer can submit a support ticket.

	Submitted ticket is persisted.

	Customer can view their own tickets.

	Customer can open a ticket.

	Customer can view the ticket status.

	Customer can view available ticket history/conversation.

	Customer cannot access another customer's ticket.

	Backend enforces customer ownership.

	Empty state is implemented.

	Loading states are implemented.

	API errors are handled.

	Arabic and English are supported using the existing i18n system.

	Portal is responsive.

	Existing ticket functionality is reused where possible.

	No duplicate Ticket entity is introduced.

	PORTAL-004 is not implemented in this story.

Implementation Instructions

Before implementation:

	Inspect the existing authentication/customer model.

	Inspect the existing ticket creation implementation.

	Inspect the existing ticket list implementation.

	Inspect the existing ticket details implementation.

	Inspect the existing ticket conversation/history implementation.

	Reuse existing APIs where possible.

	Ensure backend customer ownership is enforced.

	Reuse existing i18n and responsive UI.

	Do not implement PORTAL-004.

	Do not create duplicate ticket functionality.

	Keep the implementation focused on the customer portal's core workflow.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/customer-portal/CRM-103/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `customer-portal`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-103` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `CRM`
- **Status:** `To Do`
- **Assignee:** `Batool Harah`
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
Customer Portal — Submit & Track Tickets
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a customer, I want to access a customer portal where I can submit support tickets and track my existing requests so that I can manage my support requests without accessing the internal CRM.

Objective

Provide a simple customer-facing portal that allows authenticated customers to:

	View their portal dashboard.

	Submit a new support ticket.

	View their own submitted tickets.

	Track the current status of their tickets.

	Open a ticket and view its history/conversation.

This story consolidates the core functionality from:

	PORTAL-001 — Customer Portal Dashboard

	PORTAL-002 — Submit & Track Support Tickets

	PORTAL-003 — Customer Ticket History

These original stories remain as business requirements for traceability. This story is the single implementation unit.

Scope

Customer Dashboard

Provide a customer-facing dashboard showing basic information such as:

	Welcome message

	Number of open tickets

	Number of pending tickets

	Recent tickets

	Action to create a new ticket

Example:

Customer Portal

Welcome, Ahmed

My Tickets
────────────────────────

Open        2
Pending     1
Resolved    4

Recent Tickets

#1001  Unable to login       Open
#1005  Payment issue         Pending

[ Submit New Ticket ]
[ View My Tickets ]

Submit Ticket

Customers can create a support ticket.

Minimum fields:

	Subject

	Description

	Category if the existing ticket model already supports it

	Priority if the existing customer workflow allows customers to select it

Do not introduce new ticket fields unless required by the existing SDD/domain model.

After successful submission, show confirmation and the created ticket.

Example:

Ticket submitted successfully.

Ticket #1006

[ View Ticket ]

My Tickets

Customers can view only their own tickets.

Display at minimum:

	Ticket number

	Subject

	Status

	Priority where available

	Created date

	Updated date

Provide basic search/filtering only if the existing ticket functionality can be reused easily.

Do not create a second ticket-management system.

Ticket Details / History

Customers can open one of their tickets and view:

	Ticket information

	Current status

	Conversation/history available to the customer

Reuse the existing ticket details and conversation functionality where possible.

Customers must not see internal CRM information such as:

	Internal notes

	Administrative information

	Other customers' tickets

Security

The backend must determine the current customer from the authenticated identity.

Do not trust a customer ID supplied by the frontend to determine which tickets can be viewed.

Correct:

Authenticated Customer
        ↓
Backend
        ↓
Current Customer ID
        ↓
Customer's Tickets

A customer must never be able to access another customer's ticket by changing an ID in the URL or API request.

Authorization

Reuse the existing authentication and authorization system.

Customer users may:

	Access the customer portal.

	Create their own tickets.

	View their own tickets.

	View their own ticket history/conversation.

Customers must not:

	View another customer's tickets.

	Modify another customer's tickets.

	Access internal CRM administration.

	View internal notes.

Backend authorization is authoritative.

API

Reuse existing ticket APIs where possible.

Before creating new endpoints, inspect the existing ticket implementation.

Possible endpoints if existing APIs do not support the portal:

GET  /api/customer/tickets
GET  /api/customer/tickets/{id}
POST /api/customer/tickets

The backend should determine the current customer from authentication.

Do not create duplicate Ticket entities.

Loading States

Implement appropriate loading states for:

	Dashboard

	Ticket list

	Ticket details

	Ticket submission

Example:

Loading your tickets...

Error Handling

Handle:

	Unauthorized customer

	Ticket not found

	Ticket belonging to another customer

	Invalid ticket data

	API failure

	Network failure

Do not expose technical exception details.

Example:

Unable to load your tickets.

[Try Again]

Empty States

If the customer has no tickets:

You don't have any support tickets yet.

[Submit a Ticket]

Success States

After creating a ticket:

Your ticket has been submitted successfully.

Ticket #1006

After loading tickets, display the customer's tickets normally.

Responsive UI

The portal should work on:

	Desktop

	Tablet

	Mobile

Reuse the existing responsive layout/components.

Arabic / English

Reuse the existing project's:

	English translations

	Arabic translations

	LTR/RTL support

Do not create a separate localization system.

Not in Scope

Do not implement:

	Customer feedback/satisfaction

	Advanced customer analytics

	Customer-to-customer visibility

	Customer administration

	Advanced ticket filtering

	Real-time notifications

	New chat/communication infrastructure

	New ticket management system

	AI functionality

PORTAL-004 — Customer Feedback & Satisfaction is deferred.

Reuse

Reuse existing:

	Authentication

	Customer identity

	Ticket entity

	Ticket creation

	Ticket list

	Ticket details

	Ticket conversation/history

	API client

	i18n

	UI components

	Status/priority components

Do not duplicate existing CRM functionality.

Acceptance Criteria

	Customer can access the customer portal.

	Customer dashboard is displayed.

	Customer can submit a support ticket.

	Submitted ticket is persisted.

	Customer can view their own tickets.

	Customer can open a ticket.

	Customer can view the ticket status.

	Customer can view available ticket history/conversation.

	Customer cannot access another customer's ticket.

	Backend enforces customer ownership.

	Empty state is implemented.

	Loading states are implemented.

	API errors are handled.

	Arabic and English are supported using the existing i18n system.

	Portal is responsive.

	Existing ticket functionality is reused where possible.

	No duplicate Ticket entity is introduced.

	PORTAL-004 is not implemented in this story.

Implementation Instructions

Before implementation:

	Inspect the existing authentication/customer model.

	Inspect the existing ticket creation implementation.

	Inspect the existing ticket list implementation.

	Inspect the existing ticket details implementation.

	Inspect the existing ticket conversation/history implementation.

	Reuse existing APIs where possible.

	Ensure backend customer ownership is enforced.

	Reuse existing i18n and responsive UI.

	Do not implement PORTAL-004.

	Do not create duplicate ticket functionality.

	Keep the implementation focused on the customer portal's core workflow.
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
