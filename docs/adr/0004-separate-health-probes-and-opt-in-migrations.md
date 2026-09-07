# ADR 0004: Separate health probes and keep startup migrations opt-in

- Status: Accepted
- Date: 2026-09-07

## Context

Container orchestration needs to distinguish a process that is alive from an
instance that is ready to serve database-backed traffic. The local Compose
stack also needs a reproducible way to initialize both module schemas, while a
production deployment may run multiple API replicas and requires controlled
database changes.

## Decision

Expose separate liveness and readiness endpoints. Liveness uses only an
in-process self check. Readiness checks connectivity for the Restaurants and
Catalog DbContexts. Health responses omit exception and connection details,
and high-frequency probes are excluded from request traces and completion
logs.

Support sequential startup migrations behind the
`Database:ApplyMigrations` configuration flag. Enable the flag in the
single-instance local Compose stack, but leave it disabled by default.
Production environments must apply migrations through a dedicated deployment
job before API replicas are rolled out.

## Consequences

### Positive

- Orchestrators can stop routing traffic without causing needless restart
  loops during a database outage.
- Local startup creates both module schemas without manual commands.
- Health responses and telemetry avoid leaking details or generating probe
  noise.
- Production migration ordering remains an explicit deployment concern.

### Negative

- Readiness performs two database round trips per probe.
- Local startup fails if a migration cannot be applied.
- Deployment automation must provide a separate production migration step.
