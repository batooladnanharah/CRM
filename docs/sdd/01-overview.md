# Overview

## Product vision

As a project developer, I want the CRM requirements and MVP scope to be clearly documented so that implementation can be performed consistently by the development team and AI agent. The objective is to document the functional requirements provided for the Customer Support CRM and define what will be implemented as a two-day MVP.

## Target users

CRM-24 does not explicitly enumerate user personas beyond references to "agent", "customer", and "project developer" implicit in the requirement areas (Agent Dashboard, Customer Portal). Role granularity is not specified — see [OQ-02](04-assumptions-open-questions.md#oq-02).

## Timebox

Two-day MVP. The two-day implementation should prioritize working end-to-end functionality over advanced integrations, per CRM-24's MVP Scope Strategy.

## Non-goals (for this story)

- Any code changes under `backend/CRM.Api/` or a future `frontend/` folder. The current repository contains only the default ASP.NET Core `CRM.Api` scaffold (`backend/CRM.Api/Program.cs`) and no frontend project yet; neither is created as part of this story.
- Creating the actual Jira epics via API automation — this SDD only records the mapping table; epic creation is a manual tracker task performed by the assignee.
- Deciding technology stack details (database, auth provider, AI provider, i18n library). Those decisions belong to per-epic design stories.

## Current backend state

The backend is the default `dotnet new web` template:

- `backend/CRM.Api/Program.cs` — minimal API scaffold with a sample `/weatherforecast` endpoint. No CRM domain code exists.
- `backend/CRM.Api/CRM.Api.csproj` — targets `net10.0` (Microsoft.NET.Sdk.Web). See [OQ-18](04-assumptions-open-questions.md#oq-18) for confirmation that this is the intended MVP runtime.

No frontend project exists yet. "Responsive UI" (P0) and "Arabic/English" (P0) will require a frontend project to be scaffolded by a later story; this story does not scaffold it.

For the technical architecture (technology stack, layering, module structure, AI abstraction), see [05-architecture.md](05-architecture.md).
