> **Fetched from jira:** [CRM-106](https://batooladnanharah.atlassian.net/browse/CRM-106)  
> *Fetched 2026-08-26T22:41:26.481Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** DASH-006- Global Toast Notification System  
**Type:** CRM  
**Status:** To Do  
**Assignee:** Batool Harah

### Description

User Story

As a CRM user,

I want to receive clear, immediate toast notifications when an action succeeds, fails, or requires attention,

so that I can understand the result of my actions without needing to manually check another page or refresh the application.

Description

Implement a reusable global toast/notification system for the CRM frontend.

The current application has AppAlert.vue, but the code review indicates that there is no dedicated global toast/notification mechanism for asynchronous actions such as ticket assignment, SLA events, or received messages.

The implementation should introduce a centralized notification mechanism that can be triggered from any feature without creating separate notification implementations for each page.

The solution should reuse the existing Vue 3, Composition API, Pinia, and UI component structure and should require minimal changes to existing features.

Functional Requirements

1. Global Toast Component

Create a reusable global toast component that can display short-lived notifications.

The toast should support at least:

	Success

	Error

	Warning

	Information

Each notification should contain:

	Message

	Notification type

	Optional title

	Optional duration

	Close action

2. Global Notification State

Create a centralized notification mechanism, preferably using the existing Pinia architecture.

The application should be able to trigger a notification from any module.

Example:

Ticket Assignment
        ↓
Action succeeds
        ↓
Notification Store
        ↓
Global Toast
        ↓
"Ticket assigned successfully"

3. Automatic Dismissal

Toast notifications should automatically disappear after a configurable duration.

The user must also be able to close a toast manually.

Errors should remain visible long enough for the user to understand them and should provide a manual close option.

4. Multiple Notifications

The system should support multiple notifications without replacing an existing notification unexpectedly.

Notifications should be displayed in a consistent stack/order.

5. Async Operation Feedback

Use the global notification system for asynchronous operations where user feedback is currently missing.

Examples include:

	Ticket created successfully

	Ticket updated successfully

	Ticket assigned successfully

	Customer updated successfully

	Knowledge-base article created/updated

	SLA configuration updated

	User created successfully

	Permission update completed

	AI operation completed

	API operation failed

Only relevant existing flows should be updated as part of this story.

6. API Error Feedback

When an API request fails, the frontend should be able to display an appropriate error toast instead of silently failing or requiring the user to infer what happened.

The existing API/client error-handling approach should be reused where possible.

7. Internationalization

Toast messages must support the existing English and Arabic localization system.

Messages should not be hard-coded directly inside reusable components.

Example:

en:
"Ticket assigned successfully."

ar:
"تم تعيين التذكرة بنجاح."

8. Accessibility

The toast system should be accessible to keyboard and screen-reader users.

Notifications should use appropriate ARIA semantics and should not prevent normal interaction with the application.

Technical Requirements

The implementation should reuse the existing frontend architecture:

	Vue 3 Composition API

	Pinia

	Existing UI components/design system

	Existing AppAlert.vue where appropriate

	Existing vue-i18n localization

	Existing API error-handling conventions

Do not introduce a new notification library unless the existing project architecture requires it.

Prefer implementing a small internal notification service/store over adding an unnecessary external dependency.

Suggested Structure

The exact structure should follow the existing project conventions, but a possible implementation is:

src/
├── components/
│   └── ui/
│       └── AppToast.vue
│
├── stores/
│   └── notification.ts
│
├── composables/
│   └── useToast.ts
│
└── App.vue

For example, feature code should be able to use:

useToast().success(...)
useToast().error(...)

or the equivalent pattern already used by the project.

The exact API should follow the existing code style.

Acceptance Criteria

AC1 — Display Success Toast

Given a user performs a successful operation

When the operation completes successfully

Then a success toast is displayed.

AC2 — Display Error Toast

Given an operation fails

When the backend/API returns an error

Then an error toast is displayed with an understandable message.

AC3 — Toast Auto-Dismissal

Given a toast is displayed

When its configured duration expires

Then the toast is automatically removed.

AC4 — Manual Close

Given a toast is displayed

When the user selects the close action

Then the toast is immediately removed.

AC5 — Multiple Toasts

Given multiple operations generate notifications

When multiple notifications are triggered

Then all active notifications are displayed without one incorrectly replacing another.

AC6 — Global Availability

Given a feature/module needs to display a notification

When it invokes the notification mechanism

Then the notification is displayed by the global toast component regardless of the current page.

AC7 — Localization

Given the application language is English or Arabic

When a toast is displayed

Then the notification text is displayed using the selected language.

AC8 — Accessibility

Given a toast is displayed

When the user navigates using keyboard or assistive technology

Then the notification remains accessible and provides an appropriate way to dismiss it.

AC9 — Existing Functionality

Given the existing AppAlert.vue and notification/error-handling functionality

When the new global toast system is implemented

Then existing functionality should continue working unless intentionally replaced by the new centralized mechanism.

AC10 — Minimal Changes

The implementation must reuse the existing frontend architecture and should not require unnecessary changes to unrelated modules.

Testing Requirements

Add tests covering:

	Success notification

	Error notification

	Warning notification

	Information notification

	Automatic dismissal

	Manual dismissal

	Multiple active notifications

	Store/composable behavior

	English localization

	Arabic localization

	Accessibility behavior where supported by the existing test setup

Existing Vitest conventions should be followed.

Out of Scope

This story does not include:

	Building a persistent notification center

	Push notifications

	Email notifications

	SMS notifications

	WhatsApp notifications

	WebSocket/SignalR real-time infrastructure

	Redesigning the existing notification architecture

	Adding unrelated UI redesigns

Those should be separate stories if required.

Implementation Constraint

Reuse the existing code and architecture wherever possible.

The goal is to introduce a centralized, reusable toast mechanism with minimal changes to existing modules rather than rewriting existing error-handling or UI components.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/agent-dashboard/CRM-106/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `agent-dashboard`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-106` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `CRM`
- **Status:** `To Do`
- **Assignee:** `Batool Harah`
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
DASH-006- Global Toast Notification System
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a CRM user,

I want to receive clear, immediate toast notifications when an action succeeds, fails, or requires attention,

so that I can understand the result of my actions without needing to manually check another page or refresh the application.

Description

Implement a reusable global toast/notification system for the CRM frontend.

The current application has AppAlert.vue, but the code review indicates that there is no dedicated global toast/notification mechanism for asynchronous actions such as ticket assignment, SLA events, or received messages.

The implementation should introduce a centralized notification mechanism that can be triggered from any feature without creating separate notification implementations for each page.

The solution should reuse the existing Vue 3, Composition API, Pinia, and UI component structure and should require minimal changes to existing features.

Functional Requirements

1. Global Toast Component

Create a reusable global toast component that can display short-lived notifications.

The toast should support at least:

	Success

	Error

	Warning

	Information

Each notification should contain:

	Message

	Notification type

	Optional title

	Optional duration

	Close action

2. Global Notification State

Create a centralized notification mechanism, preferably using the existing Pinia architecture.

The application should be able to trigger a notification from any module.

Example:

Ticket Assignment
        ↓
Action succeeds
        ↓
Notification Store
        ↓
Global Toast
        ↓
"Ticket assigned successfully"

3. Automatic Dismissal

Toast notifications should automatically disappear after a configurable duration.

The user must also be able to close a toast manually.

Errors should remain visible long enough for the user to understand them and should provide a manual close option.

4. Multiple Notifications

The system should support multiple notifications without replacing an existing notification unexpectedly.

Notifications should be displayed in a consistent stack/order.

5. Async Operation Feedback

Use the global notification system for asynchronous operations where user feedback is currently missing.

Examples include:

	Ticket created successfully

	Ticket updated successfully

	Ticket assigned successfully

	Customer updated successfully

	Knowledge-base article created/updated

	SLA configuration updated

	User created successfully

	Permission update completed

	AI operation completed

	API operation failed

Only relevant existing flows should be updated as part of this story.

6. API Error Feedback

When an API request fails, the frontend should be able to display an appropriate error toast instead of silently failing or requiring the user to infer what happened.

The existing API/client error-handling approach should be reused where possible.

7. Internationalization

Toast messages must support the existing English and Arabic localization system.

Messages should not be hard-coded directly inside reusable components.

Example:

en:
"Ticket assigned successfully."

ar:
"تم تعيين التذكرة بنجاح."

8. Accessibility

The toast system should be accessible to keyboard and screen-reader users.

Notifications should use appropriate ARIA semantics and should not prevent normal interaction with the application.

Technical Requirements

The implementation should reuse the existing frontend architecture:

	Vue 3 Composition API

	Pinia

	Existing UI components/design system

	Existing AppAlert.vue where appropriate

	Existing vue-i18n localization

	Existing API error-handling conventions

Do not introduce a new notification library unless the existing project architecture requires it.

Prefer implementing a small internal notification service/store over adding an unnecessary external dependency.

Suggested Structure

The exact structure should follow the existing project conventions, but a possible implementation is:

src/
├── components/
│   └── ui/
│       └── AppToast.vue
│
├── stores/
│   └── notification.ts
│
├── composables/
│   └── useToast.ts
│
└── App.vue

For example, feature code should be able to use:

useToast().success(...)
useToast().error(...)

or the equivalent pattern already used by the project.

The exact API should follow the existing code style.

Acceptance Criteria

AC1 — Display Success Toast

Given a user performs a successful operation

When the operation completes successfully

Then a success toast is displayed.

AC2 — Display Error Toast

Given an operation fails

When the backend/API returns an error

Then an error toast is displayed with an understandable message.

AC3 — Toast Auto-Dismissal

Given a toast is displayed

When its configured duration expires

Then the toast is automatically removed.

AC4 — Manual Close

Given a toast is displayed

When the user selects the close action

Then the toast is immediately removed.

AC5 — Multiple Toasts

Given multiple operations generate notifications

When multiple notifications are triggered

Then all active notifications are displayed without one incorrectly replacing another.

AC6 — Global Availability

Given a feature/module needs to display a notification

When it invokes the notification mechanism

Then the notification is displayed by the global toast component regardless of the current page.

AC7 — Localization

Given the application language is English or Arabic

When a toast is displayed

Then the notification text is displayed using the selected language.

AC8 — Accessibility

Given a toast is displayed

When the user navigates using keyboard or assistive technology

Then the notification remains accessible and provides an appropriate way to dismiss it.

AC9 — Existing Functionality

Given the existing AppAlert.vue and notification/error-handling functionality

When the new global toast system is implemented

Then existing functionality should continue working unless intentionally replaced by the new centralized mechanism.

AC10 — Minimal Changes

The implementation must reuse the existing frontend architecture and should not require unnecessary changes to unrelated modules.

Testing Requirements

Add tests covering:

	Success notification

	Error notification

	Warning notification

	Information notification

	Automatic dismissal

	Manual dismissal

	Multiple active notifications

	Store/composable behavior

	English localization

	Arabic localization

	Accessibility behavior where supported by the existing test setup

Existing Vitest conventions should be followed.

Out of Scope

This story does not include:

	Building a persistent notification center

	Push notifications

	Email notifications

	SMS notifications

	WhatsApp notifications

	WebSocket/SignalR real-time infrastructure

	Redesigning the existing notification architecture

	Adding unrelated UI redesigns

Those should be separate stories if required.

Implementation Constraint

Reuse the existing code and architecture wherever possible.

The goal is to introduce a centralized, reusable toast mechanism with minimal changes to existing modules rather than rewriting existing error-handling or UI components.
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
