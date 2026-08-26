# ADR-0002 — Frontend: Vue 3 + TypeScript

## Context

CRM-25's Technology Stack section names Vue 3, TypeScript, Vue Router, Pinia (where shared state is required), and i18n for Arabic/English as the frontend stack. The repository also contains two `react-learning-journey/` trees (at the repo root and under `backend/CRM.Api/`), which could be mistaken for the intended CRM frontend.

## Decision

The CRM frontend is **Vue 3 + TypeScript**, using **Vue Router** for routing, **Pinia** for shared application state, and **vue-i18n** for Arabic/English localization.

The `react-learning-journey/` folder in the repo root and the one under `backend/CRM.Api/` are **learning material only, not the CRM frontend**. They do not contradict this decision and are not built upon by the CRM application.

## Consequences

- All CRM UI implementation stories target Vue 3 components, Vue Router routes, and Pinia stores under `src/modules/...` as described in [`05-architecture.md`](../05-architecture.md#frontend-structure).
- No React code from `react-learning-journey/` is imported into or reused by the CRM frontend.
- Contributors unfamiliar with the repo must be told explicitly (in this ADR and in `05-architecture.md`) that the React folders are sandboxes, to avoid confusion about the chosen framework.

## Status

Accepted
