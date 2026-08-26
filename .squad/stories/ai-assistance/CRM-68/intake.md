> **Fetched from jira:** [CRM-68](https://batooladnanharah.atlassian.net/browse/CRM-68)  
> *Fetched 2026-08-26T15:55:28.927Z. Edit the sections below as needed; the planner reads this file verbatim.*


## Source — work item (from tracker)

**Title:** AI-001 — AI Service Integration & Configuration  
**Type:** Task  
**Status:** To Do

### Description

User Story

As a support manager, I want the CRM to have a configurable AI service so that AI-powered features can be enabled when an AI provider is available without changing the core CRM architecture.

Objective

Create a provider-independent AI integration layer.

The CRM must work normally when no AI provider is configured.

The implementation should support:

	AI service abstraction

	Provider configuration

	Development/mock provider

	AI request/response models

	Error handling

	Timeout handling

	Feature availability

	Secure API key management

	Logging

	Testing

Important Assessment Requirement

There is currently no AI API available.

Therefore:

Do not make the CRM dependent on an external AI provider.

The application must work in this state:

AI Provider Configured?
        │
        ├── Yes → Real AI Provider
        │
        └── No → Development AI Provider

The development provider should return deterministic test responses.

Do not fake these responses as real AI-generated content.

Clearly label them as development/mock responses.

Architecture

Recommended:

Vue
 ↓
.NET API
 ↓
AI Application Service
 ↓
IAiService
 ↓
┌─────────────────────────┐
│ Real AI Provider        │
│ Development AI Provider │
└─────────────────────────┘

The Ticket domain must not directly depend on:

	OpenAI SDK

	Azure OpenAI SDK

	Gemini SDK

	Anthropic SDK

	Any specific AI vendor

Interface

Create a provider abstraction such as:

IAiService

Conceptual operation:

GenerateAsync(request)

The exact interface should follow the project's existing application/service architecture.

AI Request

A generic request model may contain:

AiRequest

Feature
SystemInstruction
UserInput
Context

Example:

Feature:
TicketSummary

Context:
Ticket information
Customer information
Conversation messages

Do not send the entire database record to the AI.

Only provide the minimum required context.

AI Response

Use a structured response:

AiResponse

Success
Content
Provider
Model
ErrorCode

The exact implementation should follow the project's SDD.

Supported Features

The AI service should recognize:

TicketSummary
TicketCategorization
SuggestedReply
SuggestedSolution
Chatbot

These features will be implemented in later stories.

Do not implement all AI logic in this story.

Provider Configuration

Example backend configuration:

AI:
  Enabled: false
  Provider: Development
  Model: Development
  ApiKey: ...

The exact configuration structure should follow the project's conventions.

Environment Variables

If a real provider is configured:

AI_API_KEY
AI_MODEL

The actual variable names should follow the project configuration standards.

Never commit the API key.

Security

The AI API key must only exist on the backend.

Never expose:

AI_API_KEY

to Vue.

Do not put it in:

	Vue environment variables that are bundled into the frontend

	localStorage

	sessionStorage

	API responses

	Git

Development Provider

Implement:

DevelopmentAiService

Example behavior:

Feature:
TicketSummary

Response:
"Development summary: Customer is unable to login."

This allows:

	Frontend development

	API testing

	End-to-end testing

	Demonstration without external credentials

Do not claim that the response came from an actual AI model.

AI Availability

The API should expose whether AI is available.

Example:

GET /api/ai/status

Response:

{
  "enabled": false,
  "provider": "Development",
  "available": true
}

If no provider is configured:

{
  "enabled": false,
  "provider": null,
  "available": false
}

The exact response should follow the project's API conventions.

UI

The CRM should communicate AI availability clearly.

If AI is available:

AI Assistant
● Available

If AI is unavailable:

AI Assistant
○ Not configured

Do not show broken AI buttons that always fail.

AI Feature States

Each AI feature should support:

Available
Loading
Unavailable
Error
Success

Example:

AI Summary

[Generate Summary]

If unavailable:

AI Summary

AI assistance is not configured.

Contact an administrator to enable it.

Error Handling

AI failures must not break CRM operations.

Example:

Ticket
   ↓
AI Summary
   ↓
AI provider fails
   ↓
Ticket still works normally

Display:

AI summary could not be generated.

Please try again later.

Do not expose:

	Provider stack traces

	API keys

	Internal request details

	Raw provider errors

Timeout

AI calls should have a reasonable timeout.

If the provider does not respond:

AI request timed out.

The CRM request should not hang indefinitely.

Cancellation

Where practical, support cancellation when the user navigates away or the request is no longer needed.

Do not introduce complex streaming infrastructure.

Retry

The UI may allow:

[Try Again]

Do not implement unlimited automatic retries.

A small number of controlled retries may be used by the backend if the provider supports it.

Logging

Log enough information to debug AI failures.

Example:

AI request failed
Feature: TicketSummary
Provider: Development
Duration: 120ms
Error: Timeout

Never log:

	API keys

	Passwords

	Sensitive credentials

Be careful with customer conversation content.

Do not log full ticket conversations unnecessarily.

AI Context

Each AI feature must explicitly define what context it receives.

Example for ticket summary:

Ticket:
Subject
Description
Status
Priority
Conversation messages

Do not automatically send:

	Passwords

	Authentication tokens

	Internal credentials

	Unrelated customer data

Prompt Management

Do not hard-code large prompts throughout controllers.

Keep AI instructions in a dedicated application/service layer or configuration/resource structure.

Example:

AiPromptTemplates

TicketSummary
SuggestedReply
Categorization
SuggestedSolution

The exact organization should follow the SDD.

AI Output Validation

Do not blindly trust AI output.

For structured features such as categorization:

AI Output
   ↓
Validate
   ↓
Allowed Category?
   ↓
Save

The backend remains authoritative.

AI suggestions must not bypass business validation.

AI Suggestions

AI-generated values should initially be treated as suggestions.

Example:

Suggested Category:
Technical Support

[Apply] [Ignore]

Do not automatically change important ticket properties without explicit business rules.

Cost Control

For the MVP:

	Keep prompts small.

	Send only necessary context.

	Avoid repeated automatic calls.

	Trigger AI actions explicitly.

Do not implement complex token/cost tracking unless required.

Privacy

Customer conversations may contain sensitive information.

The AI integration must send only the data required for the requested feature.

Document the provider/data-handling assumption in the SDD.

If a real external AI provider is added later, the organization must verify that its data-processing policy is acceptable.

API

AI features should be exposed through backend endpoints.

Example:

{{POST /api/ai/tickets/

{ticketId}
/summary}}

The backend:

	Authenticates the user.

	Authorizes ticket access.

	Loads required context.

	Calls IAiService.

	Validates the response.

	Returns the result.

The frontend must not call the external AI provider directly.

Authorization

AI operations require the same ticket permissions as the underlying ticket.

Example:

Agent can access ticket
       ↓
Agent can request AI summary

If a user cannot access the ticket, they cannot use AI to retrieve its contents.

Testing

Backend Tests

Test:

	Development provider works.

	Provider selection works.

	AI disabled behavior works.

	AI unavailable behavior works.

	AI timeout handled.

	AI provider error handled.

	API key is not exposed.

	Unauthorized ticket access rejected.

	AI response validation works.

	CRM continues working when AI fails.

Frontend Tests

Test:

	AI available state.

	AI unavailable state.

	Generate button.

	Loading state.

	Success state.

	Error state.

	Retry state.

	AI output display.

Integration Tests

Test:

Vue
 ↓
.NET API
 ↓
IAiService
 ↓
DevelopmentAiService
 ↓
Response
 ↓
Vue

Manual Verification

	Start the application without an AI API key.

	Verify CRM starts normally.

	Open a ticket.

	Verify AI feature shows unavailable or development mode.

	Trigger an AI feature.

	Verify development response.

	Configure a fake/invalid provider.

	Verify provider failure is handled.

	Verify ticket functionality still works.

	Verify no AI credentials are exposed in browser developer tools.

	Verify AI API calls happen through .NET.

Edge Cases

Handle:

	AI disabled.

	No provider configured.

	Invalid provider configuration.

	Invalid API key.

	Provider timeout.

	Provider unavailable.

	Malformed AI response.

	Empty AI response.

	Unauthorized ticket.

	Very large ticket conversation.

	Sensitive customer data.

	Database failure.

	AI service exception.

AI Implementation Instructions

This story itself is part of the AI architecture, so the implementing agent must be particularly careful.

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Inspect the existing service architecture.

	Inspect existing configuration patterns.

	Inspect existing error handling.

	Inspect existing logging.

	Create an IAiService abstraction.

	Implement a development provider.

	Keep provider-specific code isolated.

	Keep API keys server-side.

	Do not add an external AI SDK unless an actual provider is available.

	Do not make the CRM dependent on AI.

	Add tests before connecting a real provider.

	Verify failure behavior.

	Review all AI-generated code manually.

	Verify every acceptance criterion.

Acceptance Criteria

	AI service abstraction exists.

	AI provider is isolated from the core CRM domain.

	Development/mock provider exists.

	CRM works without an external AI API.

	AI availability can be determined.

	AI configuration is stored server-side.

	API keys are never exposed to Vue.

	AI requests are made through the .NET backend.

	AI failures do not break normal CRM operations.

	AI timeout is handled.

	AI output can be validated.

	Ticket authorization is enforced before AI access.

	AI feature state is visible in the UI.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	Integration tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	IAiService implemented.

	Development provider implemented.

	Provider configuration implemented.

	AI status endpoint implemented.

	Error/timeout handling implemented.

	Secure configuration implemented.

	Authorization implemented.

	Vue AI state implemented.

	Tests pass.

	Manual no-provider scenario verified.

	AI credentials verified to remain server-side.

	No unnecessary AI SDK introduced.

	AI-generated code reviewed.

	Acceptance criteria verified.

### Attachments

None.

---
# Story intake

Fill this template for each story you want planned. Keep it copy-paste-friendly: the planner reads **this file and the files in `attachments/`**, nothing else.

- Folder: `.squad/stories/ai-assistance/CRM-68/intake.md`
- Binaries (screenshots, PDFs, exports): put them in `attachments/` next to this file and list them below.
- Do **not** rely on external links (tracker URLs, wiki, chat) — the planner cannot open them. Paste the content you want considered.

This is **not** an implementation prompt. It is the input to the plan-generation meta-prompt bundled with squad-kit (`generate-plan.md` in the installed package).

---

## Feature

- **Feature name (display):**
- **Feature slug (folder under `plans/`):** `ai-assistance`

## Tracker (metadata only)

- **Tracker type:** `jira`
- **Work item id:** `CRM-68` *(used in filenames and plan tables; fill manually if empty)*
- **Work item type:** `Task`
- **Status:** `To Do`
- **Assignee:** ``
- **Labels:** ``

External tracker links are **not** followed by the planner. Keep the id for naming and traceability only.

---

## Title

*(Paste the work item title verbatim. Prefilled when `squad new-story` fetched from a tracker.)*

```
AI-001 — AI Service Integration & Configuration
```

---

## Description

*(Paste the full work item description. Prefilled when fetched from a tracker.)*

```
User Story

As a support manager, I want the CRM to have a configurable AI service so that AI-powered features can be enabled when an AI provider is available without changing the core CRM architecture.

Objective

Create a provider-independent AI integration layer.

The CRM must work normally when no AI provider is configured.

The implementation should support:

	AI service abstraction

	Provider configuration

	Development/mock provider

	AI request/response models

	Error handling

	Timeout handling

	Feature availability

	Secure API key management

	Logging

	Testing

Important Assessment Requirement

There is currently no AI API available.

Therefore:

Do not make the CRM dependent on an external AI provider.

The application must work in this state:

AI Provider Configured?
        │
        ├── Yes → Real AI Provider
        │
        └── No → Development AI Provider

The development provider should return deterministic test responses.

Do not fake these responses as real AI-generated content.

Clearly label them as development/mock responses.

Architecture

Recommended:

Vue
 ↓
.NET API
 ↓
AI Application Service
 ↓
IAiService
 ↓
┌─────────────────────────┐
│ Real AI Provider        │
│ Development AI Provider │
└─────────────────────────┘

The Ticket domain must not directly depend on:

	OpenAI SDK

	Azure OpenAI SDK

	Gemini SDK

	Anthropic SDK

	Any specific AI vendor

Interface

Create a provider abstraction such as:

IAiService

Conceptual operation:

GenerateAsync(request)

The exact interface should follow the project's existing application/service architecture.

AI Request

A generic request model may contain:

AiRequest

Feature
SystemInstruction
UserInput
Context

Example:

Feature:
TicketSummary

Context:
Ticket information
Customer information
Conversation messages

Do not send the entire database record to the AI.

Only provide the minimum required context.

AI Response

Use a structured response:

AiResponse

Success
Content
Provider
Model
ErrorCode

The exact implementation should follow the project's SDD.

Supported Features

The AI service should recognize:

TicketSummary
TicketCategorization
SuggestedReply
SuggestedSolution
Chatbot

These features will be implemented in later stories.

Do not implement all AI logic in this story.

Provider Configuration

Example backend configuration:

AI:
  Enabled: false
  Provider: Development
  Model: Development
  ApiKey: ...

The exact configuration structure should follow the project's conventions.

Environment Variables

If a real provider is configured:

AI_API_KEY
AI_MODEL

The actual variable names should follow the project configuration standards.

Never commit the API key.

Security

The AI API key must only exist on the backend.

Never expose:

AI_API_KEY

to Vue.

Do not put it in:

	Vue environment variables that are bundled into the frontend

	localStorage

	sessionStorage

	API responses

	Git

Development Provider

Implement:

DevelopmentAiService

Example behavior:

Feature:
TicketSummary

Response:
"Development summary: Customer is unable to login."

This allows:

	Frontend development

	API testing

	End-to-end testing

	Demonstration without external credentials

Do not claim that the response came from an actual AI model.

AI Availability

The API should expose whether AI is available.

Example:

GET /api/ai/status

Response:

{
  "enabled": false,
  "provider": "Development",
  "available": true
}

If no provider is configured:

{
  "enabled": false,
  "provider": null,
  "available": false
}

The exact response should follow the project's API conventions.

UI

The CRM should communicate AI availability clearly.

If AI is available:

AI Assistant
● Available

If AI is unavailable:

AI Assistant
○ Not configured

Do not show broken AI buttons that always fail.

AI Feature States

Each AI feature should support:

Available
Loading
Unavailable
Error
Success

Example:

AI Summary

[Generate Summary]

If unavailable:

AI Summary

AI assistance is not configured.

Contact an administrator to enable it.

Error Handling

AI failures must not break CRM operations.

Example:

Ticket
   ↓
AI Summary
   ↓
AI provider fails
   ↓
Ticket still works normally

Display:

AI summary could not be generated.

Please try again later.

Do not expose:

	Provider stack traces

	API keys

	Internal request details

	Raw provider errors

Timeout

AI calls should have a reasonable timeout.

If the provider does not respond:

AI request timed out.

The CRM request should not hang indefinitely.

Cancellation

Where practical, support cancellation when the user navigates away or the request is no longer needed.

Do not introduce complex streaming infrastructure.

Retry

The UI may allow:

[Try Again]

Do not implement unlimited automatic retries.

A small number of controlled retries may be used by the backend if the provider supports it.

Logging

Log enough information to debug AI failures.

Example:

AI request failed
Feature: TicketSummary
Provider: Development
Duration: 120ms
Error: Timeout

Never log:

	API keys

	Passwords

	Sensitive credentials

Be careful with customer conversation content.

Do not log full ticket conversations unnecessarily.

AI Context

Each AI feature must explicitly define what context it receives.

Example for ticket summary:

Ticket:
Subject
Description
Status
Priority
Conversation messages

Do not automatically send:

	Passwords

	Authentication tokens

	Internal credentials

	Unrelated customer data

Prompt Management

Do not hard-code large prompts throughout controllers.

Keep AI instructions in a dedicated application/service layer or configuration/resource structure.

Example:

AiPromptTemplates

TicketSummary
SuggestedReply
Categorization
SuggestedSolution

The exact organization should follow the SDD.

AI Output Validation

Do not blindly trust AI output.

For structured features such as categorization:

AI Output
   ↓
Validate
   ↓
Allowed Category?
   ↓
Save

The backend remains authoritative.

AI suggestions must not bypass business validation.

AI Suggestions

AI-generated values should initially be treated as suggestions.

Example:

Suggested Category:
Technical Support

[Apply] [Ignore]

Do not automatically change important ticket properties without explicit business rules.

Cost Control

For the MVP:

	Keep prompts small.

	Send only necessary context.

	Avoid repeated automatic calls.

	Trigger AI actions explicitly.

Do not implement complex token/cost tracking unless required.

Privacy

Customer conversations may contain sensitive information.

The AI integration must send only the data required for the requested feature.

Document the provider/data-handling assumption in the SDD.

If a real external AI provider is added later, the organization must verify that its data-processing policy is acceptable.

API

AI features should be exposed through backend endpoints.

Example:

{ {POST /api/ai/tickets/

{ticketId}
/summary}}

The backend:

	Authenticates the user.

	Authorizes ticket access.

	Loads required context.

	Calls IAiService.

	Validates the response.

	Returns the result.

The frontend must not call the external AI provider directly.

Authorization

AI operations require the same ticket permissions as the underlying ticket.

Example:

Agent can access ticket
       ↓
Agent can request AI summary

If a user cannot access the ticket, they cannot use AI to retrieve its contents.

Testing

Backend Tests

Test:

	Development provider works.

	Provider selection works.

	AI disabled behavior works.

	AI unavailable behavior works.

	AI timeout handled.

	AI provider error handled.

	API key is not exposed.

	Unauthorized ticket access rejected.

	AI response validation works.

	CRM continues working when AI fails.

Frontend Tests

Test:

	AI available state.

	AI unavailable state.

	Generate button.

	Loading state.

	Success state.

	Error state.

	Retry state.

	AI output display.

Integration Tests

Test:

Vue
 ↓
.NET API
 ↓
IAiService
 ↓
DevelopmentAiService
 ↓
Response
 ↓
Vue

Manual Verification

	Start the application without an AI API key.

	Verify CRM starts normally.

	Open a ticket.

	Verify AI feature shows unavailable or development mode.

	Trigger an AI feature.

	Verify development response.

	Configure a fake/invalid provider.

	Verify provider failure is handled.

	Verify ticket functionality still works.

	Verify no AI credentials are exposed in browser developer tools.

	Verify AI API calls happen through .NET.

Edge Cases

Handle:

	AI disabled.

	No provider configured.

	Invalid provider configuration.

	Invalid API key.

	Provider timeout.

	Provider unavailable.

	Malformed AI response.

	Empty AI response.

	Unauthorized ticket.

	Very large ticket conversation.

	Sensitive customer data.

	Database failure.

	AI service exception.

AI Implementation Instructions

This story itself is part of the AI architecture, so the implementing agent must be particularly careful.

Before implementation:

	Read SDD-001, SDD-002, and SDD-003.

	Read AUTH-003.

	Inspect the existing service architecture.

	Inspect existing configuration patterns.

	Inspect existing error handling.

	Inspect existing logging.

	Create an IAiService abstraction.

	Implement a development provider.

	Keep provider-specific code isolated.

	Keep API keys server-side.

	Do not add an external AI SDK unless an actual provider is available.

	Do not make the CRM dependent on AI.

	Add tests before connecting a real provider.

	Verify failure behavior.

	Review all AI-generated code manually.

	Verify every acceptance criterion.

Acceptance Criteria

	AI service abstraction exists.

	AI provider is isolated from the core CRM domain.

	Development/mock provider exists.

	CRM works without an external AI API.

	AI availability can be determined.

	AI configuration is stored server-side.

	API keys are never exposed to Vue.

	AI requests are made through the .NET backend.

	AI failures do not break normal CRM operations.

	AI timeout is handled.

	AI output can be validated.

	Ticket authorization is enforced before AI access.

	AI feature state is visible in the UI.

	Relevant backend tests pass.

	Relevant frontend tests pass.

	Integration tests pass.

	AI-generated implementation has been reviewed.

	Implementation follows the SDD.

Definition of Done

	IAiService implemented.

	Development provider implemented.

	Provider configuration implemented.

	AI status endpoint implemented.

	Error/timeout handling implemented.

	Secure configuration implemented.

	Authorization implemented.

	Vue AI state implemented.

	Tests pass.

	Manual no-provider scenario verified.

	AI credentials verified to remain server-side.

	No unnecessary AI SDK introduced.

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
