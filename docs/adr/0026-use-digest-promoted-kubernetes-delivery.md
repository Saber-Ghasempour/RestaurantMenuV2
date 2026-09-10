# ADR 0026: Use digest-promoted Kubernetes delivery

- Status: Accepted
- Date: 2026-09-10

## Context

CI builds an API container but does not publish, verify, or deploy it. Production
delivery needs artifact identity, supply-chain evidence, controlled database
changes, environment approval, health gates, secret separation, and recoverable
rollback evidence. Startup migrations are unsafe with multiple API replicas.

## Decision

GitHub Actions publishes versioned images to GHCR and promotes one immutable
digest through protected staging and production environments. High or critical
known-fix image vulnerabilities fail delivery. Each digest receives signed
GitHub/Sigstore build provenance plus an SPDX SBOM attestation.

Kubernetes uses a zero-unavailable rolling Deployment with non-root,
read-only-root-filesystem containers and independent liveness/readiness probes.
A one-shot, zero-retry Job runs the same image in `--migrate` mode before each
rollout. The platform secret store pre-provisions runtime secrets; the workflow
receives only least-privilege cluster credentials from protected environment
secrets.

Before migration, delivery requires and retains the previous image digest,
target digest, release/migration identity, and a verified backup reference.
Database changes follow expand/contract compatibility. Roll back the application
only while it remains schema-compatible; otherwise roll forward. Destructive
database recovery uses an approved point-in-time restore to a new instance.

## Consequences

- Staging and production run the exact artifact that was scanned and attested.
- Failed manifests, migrations, rollouts, smoke tests, or missing rollback data
  stop promotion.
- Production approval and runtime-secret administration remain external
  environment controls that repository code cannot enforce by itself.
- Releases require a Kubernetes target, GHCR permissions, a configured secret
  store, and current backup evidence.
- A quarterly failed-rollout and restore drill is necessary to validate the
  operational plan rather than only its syntax.

## Alternatives rejected

- Mutable tags were rejected because they do not identify the reviewed bytes.
- API startup migrations were rejected because replica races couple schema
  changes to serving traffic.
- Automatic EF `Down` migration in production was rejected because data loss and
  compatibility cannot be inferred safely by a generic workflow.
- Committing Kubernetes Secret values was rejected because repository access is
  not the runtime secret boundary.
