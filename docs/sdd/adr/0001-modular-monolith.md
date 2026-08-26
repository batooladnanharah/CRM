# ADR-0001 — Modular Monolith

## Context

The Customer Support CRM must be built as a two-day MVP. CRM-25 requires "a simple, maintainable architecture suitable for the two-day MVP implementation" and explicitly states the implementation must avoid unnecessary distributed architecture, with constraints against microservices, CQRS (unless explicitly required), an event bus, container orchestration, additional databases, and search platforms.

## Decision

Adopt a **modular monolith**: a single deployable ASP.NET Core backend process, organized internally into feature modules (`Customers/`, `Tickets/`, `Dashboard/`, `SLA/`, `KnowledgeBase/`, `AI/`, `Reports/`, `Administration/`) under `Application/`, backed by a single PostgreSQL database via Entity Framework Core. A single Vue 3 frontend application talks to this backend over REST.

## Consequences

- Shared deployment unit — one backend process to build, test, deploy, and monitor.
- Module boundaries are enforced in-process (via folder/namespace organization and code review), not by network calls.
- Easier refactor path if a module ever needs to split out — the module boundary already exists in code, so extraction (should it ever be needed) is a later, separate decision, not a day-one requirement.
- No distributed-systems concerns (network partitions, distributed transactions, service discovery) during the MVP.

## Status

Accepted
