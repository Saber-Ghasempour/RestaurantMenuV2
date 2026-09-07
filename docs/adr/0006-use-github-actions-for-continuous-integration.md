# ADR 0006: Use GitHub Actions for continuous integration

- Status: Accepted
- Date: 2026-09-07

## Context

Local builds and tests are necessary but do not provide an independent,
repeatable quality signal for every change. The repository is hosted on GitHub,
the integration suites require Docker for Testcontainers, and the production
artifact is a Docker image. The pipeline must expose failures and coverage data
without gaining write access or pretending that image construction is a
deployment strategy.

## Decision

Use GitHub Actions on GitHub-hosted Ubuntu runners for pushes and pull requests
targeting `main`, plus manual dispatch. Use the SDK selected by `global.json` and
run a Release restore, build, and the complete test suite with `CI=true`.

Collect Cobertura data with Coverlet and merge it using the repository-local,
version-pinned ReportGenerator tool. Publish a Markdown run summary and retain
TRX and HTML/Cobertura reports for 14 days. Fail the pipeline when merged line
coverage falls below 80%.

After code quality succeeds, validate the Compose model and build the API image
from the production Dockerfile. Do not push or deploy it in this workflow.
Restrict the workflow token to `contents: read`, disable persisted checkout
credentials, cancel superseded runs on the same ref, and set job timeouts.

## Consequences

### Positive

- Every proposed change receives the same independent build and test signal.
- Testcontainers preserve realistic PostgreSQL and Redis verification in CI.
- Coverage regressions are visible in the run summary and enforced by a gate.
- Retained artifacts make failed-run diagnosis easier.
- Compose and Dockerfile drift is caught before delivery work begins.
- Least-privilege permissions reduce workflow-token exposure.

### Negative

- The full suite creates containers and is slower than unit tests alone.
- GitHub-hosted runner and artifact usage consume repository quota.
- A repository-wide percentage can hide poorly tested critical code, so review
  must still evaluate test quality and risk.
- Major-version action tags receive compatible upstream updates rather than
  being immutable commit pins.

## Future considerations

Add package vulnerability, secret, source, and container-image scanning. Before
continuous delivery, publish immutable images to a registry, use protected
environments and approvals, run migrations as a controlled job, verify rollout
health, and define rollback. Consider pinning third-party actions to full commit
SHAs and automating their updates when the maintenance model justifies it.
