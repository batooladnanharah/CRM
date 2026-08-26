# ADR-0003 — PostgreSQL + EF Core Migrations

## Context

CRM-25 names PostgreSQL as the database and Entity Framework Core for database access and migrations. The Database Principles section requires schema to be managed through EF Core migrations, explicit relationships/foreign keys, required-field validation, appropriate constraints, and no unnecessary storage of sensitive information.

## Decision

**PostgreSQL** is the primary application database. **Entity Framework Core migrations** are the only mechanism by which schema changes are applied — no ad-hoc SQL against production. Relationships and foreign keys are defined explicitly in the EF Core model; required fields and constraints are enforced at the database level where appropriate.

## Consequences

- Developers need a local PostgreSQL instance to run and test the backend.
- Migrations are checked into source control under `Infrastructure/` and reviewed like code.
- Schema drift is prevented — any schema change must go through a migration, making the schema history auditable.
- Recovery from a failed or half-applied migration is via a corrective migration, not manual database edits.

## Status

Accepted
