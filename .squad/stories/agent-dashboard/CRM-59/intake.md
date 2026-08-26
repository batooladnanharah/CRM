> **Fetched from jira:** [CRM-59](https://batooladnanharah.atlassian.net/browse/CRM-59)  
> *Fetched 2026-08-25T13:01:26.270Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** DASH-005 — Quick Replies & Team Collaboration  
**Type:** Task  
**Status:** In Progress  
**Assignee:** Batool Harah

### Description

User Story

As a support agent, I want to use predefined quick replies and collaborate with other support users on tickets so that I can respond faster and coordinate support work efficiently.

Objective

Provide two simple productivity features:

	Quick replies for commonly used customer responses.

	Basic team collaboration through internal notes and mentions.

The implementation should reuse the existing conversation functionality from TKT-008.

Scope

This story covers:

Quick Replies

	View available quick replies

	Search quick replies

	Insert quick reply into message composer

	Create/edit/delete quick replies for authorized users

Team Collaboration

	Internal notes

	Mention another CRM user

	Display mentions

	Ticket history for collaboration actions

This story does not cover:

	Real-time chat between agents

	WebSockets

	Full team messaging

	Presence indicators

	Complex notification system

	AI-generated quick replies

	AI-generated responses

Part 1 — Quick Replies

UI

Inside the ticket message composer:

[ Reply ▼ ]

[ Type your message................................. ]

[ Quick Reply ▼ ]                    [Send]

When Quick Reply is selected:

Quick Replies

[ Search replies... ]

────────────────────────────────────

Greeting
Hello, thank you for contacting support.

Password Reset
Please follow the password reset instructions...

Closing
Thank you for contacting us. Please let us know...

Clicking a quick reply inserts its content into the composer.

It should not automatically send the message.

The agent must be able to edit the text before sending.

Quick Reply Model

Suggested:

QuickReply

Id
Title
Content
IsActive
CreatedByUserId
CreatedAt
UpdatedAt

Keep the model simple.

Do not implement categories, approval workflows, versions, or complex permissions unless required by the SDD.

Quick Reply Permissions

For the MVP:

	Admin: manage quick replies.

	Manager: manage quick replies.

	Agent: use quick replies.

	Agent: may create personal quick replies only if the SDD requires it.

Recommended simplest implementation:

Admin/Manager
    ↓
Create/Edit/Delete shared quick replies

Agent
    ↓
Use shared quick replies

Quick Reply API

List

GET /api/quick-replies

Optional search:

GET /api/quick-replies?search=password

Create

POST /api/quick-replies

Example:

{
  "title": "Password Reset",
  "content": "Please follow the password reset instructions..."
}

Update

{{PUT /api/quick-replies/

{id}}}

h3. Delete

{{DELETE /api/quick-replies/{id}
}}

The final API conventions should follow the existing backend architecture.

Quick Reply Validation

Title

Required.

Content

Required.

Maximum Length

Use reasonable configurable limits.

Backend validation is mandatory.

Quick Reply Behavior

When selected:

Quick Reply
     ↓
Insert Content
     ↓
Message Composer
     ↓
Agent edits if required
     ↓
Agent clicks Send

Do not send automatically.

Part 2 — Team Collaboration

The main collaboration mechanism for the MVP is internal notes.

This reuses the internal note functionality from TKT-008.

Example:

Internal Note

@Ahmed Hassan can you review this payment issue?

by Sara Ahmed

Mentions

An agent can mention another CRM user in an internal note.

Example:

@Ahmed Hassan

The mentioned user should be represented in the stored message in a way that can support future notifications.

Mention UI

When typing:

@Ah

the UI may display:

Ahmed Hassan
Ahmed Ali

The agent can select a user.

For the MVP, a simple searchable dropdown is sufficient.

Do not implement a complex mention editor.

Mention Data

Avoid relying only on plain text.

If practical, store mention information separately.

Example:

TicketMessage
    ↓
MessageMention
    ↓
User

Suggested model:

MessageMention

Id
MessageId
UserId
CreatedAt

However, if the SDD does not require notification functionality, a simpler implementation may store the mention information within the message metadata.

The AI must inspect the existing architecture before introducing a new table.

Internal Note Visibility

Internal collaboration must never be exposed to customers.

Example:

CRM Agent
    ↓
Internal Note
    ↓
Other CRM users

Customer
    X
    ↓
Cannot see internal note

The backend must enforce this.

Notifications

For the MVP, a full notification system is not required.

If an existing notification service exists, mentioning a user may create a notification.

Example:

Sara Ahmed mentioned you in Ticket #1001.

If there is no notification infrastructure, simply store/display the mention.

Do not build a complete notification system for this story.

API — Mentions

The existing message endpoint from TKT-008 can accept mention information.

Example:

{
  "type": "InternalNote",
  "content": "@Ahmed Hassan please review this.",
  "channel": "Web",
  "mentionedUserIds": [
    "user-id"
  ]
}

The backend must validate every mentioned user.

Authorization

The backend must verify:

	User can access the ticket.

	User can create internal notes.

	Mentioned users are valid CRM users.

	Mentioned users are allowed to participate in the ticket/team.

Do not trust user IDs submitted by the frontend.

Ticket History

Important collaboration actions may create history.

Example:

Internal Note Added

Sara Ahmed added an internal note.

24 Aug 2026 16:30

Reuse TKT-009.

Do not create another collaboration history system.

Quick Reply Management UI

Authorized users should have a simple management page.

Recommended:

Quick Replies

[ + New Quick Reply ]

┌───────────────────┬────────────────────────────┬─────────┐
│ Title             │ Content                    │ Actions │
├───────────────────┼────────────────────────────┼─────────┤
│ Greeting          │ Hello, thank you...       │ Edit    │
│ Password Reset    │ Please follow...          │ Edit    │
│ Closing           │ Thank you for contacting  │ Edit    │
└───────────────────┴────────────────────────────┴─────────┘

Keep management simple.

Loading State

For quick replies:

Loading quick replies...

For mentions:

Searching users...

For management actions:

Saving...

Empty State

No quick replies:

No quick replies available.

No mention results:

No users found.

Error Handling

Handle:

	Quick reply not found

	Invalid quick reply

	Unauthorized management

	Invalid mentioned user

	Unauthorized mentioned user

	Ticket access failure

	API failure

	Database failure

Display user-friendly messages.

Do not expose technical details.

Arabic / English

Quick reply content itself may be created in either language.

The UI must support:

	Arabic

	English

	RTL

	LTR

Do not automatically translate quick replies.

Responsive UI

The quick reply selector should work on:

	Desktop

	Tablet

	Mobile

On mobile, use a drawer/modal if the dropdown becomes too large.

Mentions should remain usable on small screens.

Testing

Backend/API Tests

Test:

	Authorized agent can retrieve quick replies.

	Unauthorized user cannot manage quick replies.

	Authorized admin/manager can create quick reply.

	Authorized admin/manager can update quick reply.

	Authorized admin/manager can delete quick reply.

	Invalid quick reply rejected.

	Internal note can contain mention.

	Mentioned user is validated.

	Unauthorized mentioned user rejected.

	Internal note is not exposed to customers.

	History is created where required.

Frontend Tests

Test:

	Quick reply selector opens.

	Quick replies display.

	Search works.

	Selecting a quick reply inserts text.

	Inserted text can be edited.

	Quick reply is not automatically sent.

	Internal note mode works.

	Mention dropdown works.

	User can select a mention.

	Loading state works.

	Empty state works.

	Error state works.

Manual Verification

	Open a ticket.

	Open Reply composer.

	Open Quick Replies.

	Search for a quick reply.

	Select it.

	Verify text is inserted.

	Modify the text.

	Send the response.

	Switch to Internal Note.

	Type @.

	Search for another agent.

	Select the agent.

	Add the internal note.

	Verify the note appears.

	Verify customer-facing APIs do not expose it.

	Login as Manager.

	Create a quick reply.

	Login as Agent.

	Verify the quick reply is available.

Edge Cases

Handle:

	No quick replies.

	Quick reply deleted after list loaded.

	Very long quick reply.

	Empty quick reply.

	User mentioned no longer exists.

	User mentioned is inactive.

	User does not have access to the ticket.

	Duplicate mention.

	API failure.

	Database failure.

	Mobile UI.

	Arabic RTL.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read TKT-008.

	Read TKT-009.

	Read DASH-001.

	Read DASH-003.

	Inspect the existing conversation/message composer.

	Reuse the existing Internal Note implementation.

	Reuse existing user search.

	Reuse existing authorization.

	Do not create another messaging system.

	Do not implement WebSockets.

	Do not implement a full notification system.

	Do not implement AI-generated replies.

	Do not implement automatic message sending.

	Keep quick replies simple.

	Add backend and frontend tests.

	Run relevant tests.

	Review internal-note privacy carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Agents can view available quick replies.

	Agents can search quick replies.

	Selecting a quick reply inserts its content into the composer.

	Quick reply is not automatically sent.

	Agent can edit inserted content.

	Authorized users can manage shared quick replies.

	Quick reply validation is implemented.

	Agents can create internal collaboration notes.

	Agents can mention another CRM user.

	Mentioned users are validated by the backend.

	Internal notes remain CRM-only.

	Customer-facing APIs do not expose internal collaboration.

	Existing conversation functionality is reused.

	Existing ticket history is reused.

	Loading states are implemented.

	Empty states are implemented.

	Error states are implemented.

	Arabic RTL is supported.

	English LTR is supported.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Quick reply model implemented.

	Quick reply API implemented.

	Quick reply UI implemented.

	Quick reply insertion implemented.

	Internal collaboration/mention support implemented.

	Authorization implemented.

	Internal-note privacy verified.

	Ticket history integration reused.

	Loading/empty/error states implemented.

	Arabic/English support implemented.

	Responsive UI implemented.

	Backend tests pass.

	Frontend tests pass.

	Manual verification completed.

	No real-time collaboration infrastructure introduced.

	No unnecessary notification system introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/agent-dashboard/CRM-59/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `agent-dashboard`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-59` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `In Progress`
- **Assignee:** `Batool Harah`
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
DASH-005 — Quick Replies & Team Collaboration
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support agent, I want to use predefined quick replies and collaborate with other support users on tickets so that I can respond faster and coordinate support work efficiently.

Objective

Provide two simple productivity features:

	Quick replies for commonly used customer responses.

	Basic team collaboration through internal notes and mentions.

The implementation should reuse the existing conversation functionality from TKT-008.

Scope

This story covers:

Quick Replies

	View available quick replies

	Search quick replies

	Insert quick reply into message composer

	Create/edit/delete quick replies for authorized users

Team Collaboration

	Internal notes

	Mention another CRM user

	Display mentions

	Ticket history for collaboration actions

This story does not cover:

	Real-time chat between agents

	WebSockets

	Full team messaging

	Presence indicators

	Complex notification system

	AI-generated quick replies

	AI-generated responses

Part 1 — Quick Replies

UI

Inside the ticket message composer:

[ Reply ▼ ]

[ Type your message................................. ]

[ Quick Reply ▼ ]                    [Send]

When Quick Reply is selected:

Quick Replies

[ Search replies... ]

────────────────────────────────────

Greeting
Hello, thank you for contacting support.

Password Reset
Please follow the password reset instructions...

Closing
Thank you for contacting us. Please let us know...

Clicking a quick reply inserts its content into the composer.

It should not automatically send the message.

The agent must be able to edit the text before sending.

Quick Reply Model

Suggested:

QuickReply

Id
Title
Content
IsActive
CreatedByUserId
CreatedAt
UpdatedAt

Keep the model simple.

Do not implement categories, approval workflows, versions, or complex permissions unless required by the SDD.

Quick Reply Permissions

For the MVP:

	Admin: manage quick replies.

	Manager: manage quick replies.

	Agent: use quick replies.

	Agent: may create personal quick replies only if the SDD requires it.

Recommended simplest implementation:

Admin/Manager
    ↓
Create/Edit/Delete shared quick replies

Agent
    ↓
Use shared quick replies

Quick Reply API

List

GET /api/quick-replies

Optional search:

GET /api/quick-replies?search=password

Create

POST /api/quick-replies

Example:

{
  "title": "Password Reset",
  "content": "Please follow the password reset instructions..."
}

Update

{ {PUT /api/quick-replies/

{id}}}

h3. Delete

{ {DELETE /api/quick-replies/{id}
}}

The final API conventions should follow the existing backend architecture.

Quick Reply Validation

Title

Required.

Content

Required.

Maximum Length

Use reasonable configurable limits.

Backend validation is mandatory.

Quick Reply Behavior

When selected:

Quick Reply
     ↓
Insert Content
     ↓
Message Composer
     ↓
Agent edits if required
     ↓
Agent clicks Send

Do not send automatically.

Part 2 — Team Collaboration

The main collaboration mechanism for the MVP is internal notes.

This reuses the internal note functionality from TKT-008.

Example:

Internal Note

@Ahmed Hassan can you review this payment issue?

by Sara Ahmed

Mentions

An agent can mention another CRM user in an internal note.

Example:

@Ahmed Hassan

The mentioned user should be represented in the stored message in a way that can support future notifications.

Mention UI

When typing:

@Ah

the UI may display:

Ahmed Hassan
Ahmed Ali

The agent can select a user.

For the MVP, a simple searchable dropdown is sufficient.

Do not implement a complex mention editor.

Mention Data

Avoid relying only on plain text.

If practical, store mention information separately.

Example:

TicketMessage
    ↓
MessageMention
    ↓
User

Suggested model:

MessageMention

Id
MessageId
UserId
CreatedAt

However, if the SDD does not require notification functionality, a simpler implementation may store the mention information within the message metadata.

The AI must inspect the existing architecture before introducing a new table.

Internal Note Visibility

Internal collaboration must never be exposed to customers.

Example:

CRM Agent
    ↓
Internal Note
    ↓
Other CRM users

Customer
    X
    ↓
Cannot see internal note

The backend must enforce this.

Notifications

For the MVP, a full notification system is not required.

If an existing notification service exists, mentioning a user may create a notification.

Example:

Sara Ahmed mentioned you in Ticket #1001.

If there is no notification infrastructure, simply store/display the mention.

Do not build a complete notification system for this story.

API — Mentions

The existing message endpoint from TKT-008 can accept mention information.

Example:

{
  "type": "InternalNote",
  "content": "@Ahmed Hassan please review this.",
  "channel": "Web",
  "mentionedUserIds": [
    "user-id"
  ]
}

The backend must validate every mentioned user.

Authorization

The backend must verify:

	User can access the ticket.

	User can create internal notes.

	Mentioned users are valid CRM users.

	Mentioned users are allowed to participate in the ticket/team.

Do not trust user IDs submitted by the frontend.

Ticket History

Important collaboration actions may create history.

Example:

Internal Note Added

Sara Ahmed added an internal note.

24 Aug 2026 16:30

Reuse TKT-009.

Do not create another collaboration history system.

Quick Reply Management UI

Authorized users should have a simple management page.

Recommended:

Quick Replies

[ + New Quick Reply ]

┌───────────────────┬────────────────────────────┬─────────┐
│ Title             │ Content                    │ Actions │
├───────────────────┼────────────────────────────┼─────────┤
│ Greeting          │ Hello, thank you...       │ Edit    │
│ Password Reset    │ Please follow...          │ Edit    │
│ Closing           │ Thank you for contacting  │ Edit    │
└───────────────────┴────────────────────────────┴─────────┘

Keep management simple.

Loading State

For quick replies:

Loading quick replies...

For mentions:

Searching users...

For management actions:

Saving...

Empty State

No quick replies:

No quick replies available.

No mention results:

No users found.

Error Handling

Handle:

	Quick reply not found

	Invalid quick reply

	Unauthorized management

	Invalid mentioned user

	Unauthorized mentioned user

	Ticket access failure

	API failure

	Database failure

Display user-friendly messages.

Do not expose technical details.

Arabic / English

Quick reply content itself may be created in either language.

The UI must support:

	Arabic

	English

	RTL

	LTR

Do not automatically translate quick replies.

Responsive UI

The quick reply selector should work on:

	Desktop

	Tablet

	Mobile

On mobile, use a drawer/modal if the dropdown becomes too large.

Mentions should remain usable on small screens.

Testing

Backend/API Tests

Test:

	Authorized agent can retrieve quick replies.

	Unauthorized user cannot manage quick replies.

	Authorized admin/manager can create quick reply.

	Authorized admin/manager can update quick reply.

	Authorized admin/manager can delete quick reply.

	Invalid quick reply rejected.

	Internal note can contain mention.

	Mentioned user is validated.

	Unauthorized mentioned user rejected.

	Internal note is not exposed to customers.

	History is created where required.

Frontend Tests

Test:

	Quick reply selector opens.

	Quick replies display.

	Search works.

	Selecting a quick reply inserts text.

	Inserted text can be edited.

	Quick reply is not automatically sent.

	Internal note mode works.

	Mention dropdown works.

	User can select a mention.

	Loading state works.

	Empty state works.

	Error state works.

Manual Verification

	Open a ticket.

	Open Reply composer.

	Open Quick Replies.

	Search for a quick reply.

	Select it.

	Verify text is inserted.

	Modify the text.

	Send the response.

	Switch to Internal Note.

	Type @.

	Search for another agent.

	Select the agent.

	Add the internal note.

	Verify the note appears.

	Verify customer-facing APIs do not expose it.

	Login as Manager.

	Create a quick reply.

	Login as Agent.

	Verify the quick reply is available.

Edge Cases

Handle:

	No quick replies.

	Quick reply deleted after list loaded.

	Very long quick reply.

	Empty quick reply.

	User mentioned no longer exists.

	User mentioned is inactive.

	User does not have access to the ticket.

	Duplicate mention.

	API failure.

	Database failure.

	Mobile UI.

	Arabic RTL.

AI Implementation Instructions

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-001 and AUTH-003.

	Read TKT-008.

	Read TKT-009.

	Read DASH-001.

	Read DASH-003.

	Inspect the existing conversation/message composer.

	Reuse the existing Internal Note implementation.

	Reuse existing user search.

	Reuse existing authorization.

	Do not create another messaging system.

	Do not implement WebSockets.

	Do not implement a full notification system.

	Do not implement AI-generated replies.

	Do not implement automatic message sending.

	Keep quick replies simple.

	Add backend and frontend tests.

	Run relevant tests.

	Review internal-note privacy carefully.

	Verify every acceptance criterion.

Acceptance Criteria

	Agents can view available quick replies.

	Agents can search quick replies.

	Selecting a quick reply inserts its content into the composer.

	Quick reply is not automatically sent.

	Agent can edit inserted content.

	Authorized users can manage shared quick replies.

	Quick reply validation is implemented.

	Agents can create internal collaboration notes.

	Agents can mention another CRM user.

	Mentioned users are validated by the backend.

	Internal notes remain CRM-only.

	Customer-facing APIs do not expose internal collaboration.

	Existing conversation functionality is reused.

	Existing ticket history is reused.

	Loading states are implemented.

	Empty states are implemented.

	Error states are implemented.

	Arabic RTL is supported.

	English LTR is supported.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	Quick reply model implemented.

	Quick reply API implemented.

	Quick reply UI implemented.

	Quick reply insertion implemented.

	Internal collaboration/mention support implemented.

	Authorization implemented.

	Internal-note privacy verified.

	Ticket history integration reused.

	Loading/empty/error states implemented.

	Arabic/English support implemented.

	Responsive UI implemented.

	Backend tests pass.

	Frontend tests pass.

	Manual verification completed.

	No real-time collaboration infrastructure introduced.

	No unnecessary notification system introduced.

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
