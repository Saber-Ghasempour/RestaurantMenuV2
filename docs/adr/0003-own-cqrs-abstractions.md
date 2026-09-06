# ADR 0003: Own the CQRS abstractions

- Status: Accepted
- Date: 2026-09-06

## Context

The application requires commands, queries, handlers, validation, logging,
transactions, and other pipeline behaviors.

MediatR is a popular mediator implementation, but current versions introduce
licensing considerations. CQRS itself should not depend on a specific mediator
library.

## Decision

The application owns small framework-independent abstractions for commands,
queries, handlers, and units of work.

Feature handlers depend only on these abstractions. Request dispatching and
dependency injection are infrastructure concerns and can be implemented or
replaced independently.

## Consequences

### Positive

- Application code is independent of a mediator vendor.
- CQRS concepts remain explicit.
- Licensing and package changes do not affect use cases.
- Unit tests can invoke handlers directly.

### Negative

- Dispatching and pipeline behavior infrastructure must be implemented.
- The team owns and maintains these abstractions.
- Advanced mediator features are not available automatically.