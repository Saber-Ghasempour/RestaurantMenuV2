# ADR 0001: Start with a modular monolith

- Status: Accepted
- Date: 2026-09-06

## Context

Restaurant Menu contains several business capabilities, including identity,
restaurant management, catalog management, ordering, feedback, and
notifications.

Starting directly with independently deployed microservices would introduce
networking, messaging, data-consistency, deployment, and observability
complexity before the domain boundaries are proven.

## Decision

The system will initially be implemented as a modular monolith.

Each module will:

- Own its domain model and application use cases.
- Expose an explicit public contract.
- Hide its internal implementation.
- Avoid direct access to another module's database objects.
- Communicate through contracts and domain or integration events.

Architecture tests will enforce module boundaries.

Modules with a demonstrated need for independent deployment or scaling can
later be extracted into microservices.

## Consequences

### Positive

- Faster initial development.
- Easier local debugging and testing.
- Transactional consistency within the monolith.
- Clear migration path toward microservices.

### Negative

- Modules share one deployment unit initially.
- Architectural boundaries must be actively enforced.
- Independent scaling is unavailable until a module is extracted.