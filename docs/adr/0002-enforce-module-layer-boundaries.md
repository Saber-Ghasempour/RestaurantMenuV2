# ADR 0002: Enforce module layer boundaries

- Status: Accepted
- Date: 2026-09-06

## Context

The Restaurants module uses Domain, Application, Infrastructure, and
Presentation layers. Without automated enforcement, dependencies can gradually
point in the wrong direction and couple business rules to frameworks.

## Decision

Each layer is implemented as a separate project.

Allowed dependency directions are:

- Domain may depend only on SharedKernel.
- Application may depend on Domain and SharedKernel.
- Infrastructure may depend on Application and Domain.
- Presentation may depend on Application.
- API acts as the composition root.
- SharedKernel must not depend on application modules.

ArchUnitNET tests enforce the forbidden dependency directions.

## Consequences

### Positive

- Domain logic remains independent of frameworks and persistence.
- Dependency violations fail during automated tests.
- Modules can be extracted more easily in the future.

### Negative

- The solution contains more projects.
- Public contracts between layers must be designed explicitly.
- Some dependency rules exist both in project references and architecture tests.