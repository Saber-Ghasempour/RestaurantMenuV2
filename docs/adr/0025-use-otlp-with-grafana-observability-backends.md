# ADR 0025: Use OTLP with Grafana observability backends

- Status: Accepted
- Date: 2026-09-10

## Context

The API already emitted structured console logs and OpenTelemetry HTTP, runtime,
HTTP-client, Npgsql, and messaging measurements. The Collector only wrote debug
output, however, and an Outbox publish lost the originating HTTP trace context.
Operators had no durable local search, dashboards, alerts, explicit SLOs, or
failure runbooks. Telemetry must also avoid turning guest capabilities, identity
data, or arbitrary identifiers into logs and high-cardinality metric labels.

## Decision

OTLP remains the only application export protocol. The local reference stack
routes logs to Loki, traces to Tempo, and metrics through the Collector's
Prometheus exporter into Prometheus; Grafana provisions all three data sources
and the RestaurantMenu overview dashboard. A production deployment may replace
these components with a managed OTLP-compatible backend without changing module
instrumentation. The local stack is not exposed as a production security model:
production backends require TLS, authentication, tenant isolation, and encrypted
storage at their ingress boundary.

Authenticated HTTP mutations emit a structured operational audit event with a
route template, method, outcome, correlation ID, trace ID, and a 64-bit
SHA-256-derived actor pseudonym. Membership lifecycle audit remains a durable
Restaurants-owned database record. Audit and application logs never contain
request or response bodies, query strings, authorization/cookie headers, raw
subjects, email addresses, public menu codes, dining-session tokens, database
credentials, or exception details from expected client failures. W3C trace
context is allowed in Outbox metadata and RabbitMQ headers; baggage is not
propagated. Trace context is operational metadata, never authorization data.

Metric attributes use only route templates and fixed method, outcome, event,
consumer, dependency, and source vocabularies. Message, aggregate, Restaurant,
Branch, user, code, and token identifiers are forbidden as metric labels.
Unknown event and consumer names collapse to `other`. A database-backed monitor
publishes identifier-free oldest-Outbox-age and Outbox/Inbox dead-letter gauges.

The local development retention windows are seven days for logs and traces and
15 days for metrics. Production targets are 30 days for ordinary logs/traces and
13 months for aggregated metrics. Security/management audit events target one
year in access-controlled storage; module-owned database audit retention remains
subject to its business policy. Legal or incident holds override deletion.

The initial rolling 30-day SLOs are 99.9% API availability for non-health
requests, p95 server latency below 500 ms, 99% of Outbox messages published
within 60 seconds, and zero unacknowledged dead letters. Alerts page on sustained
5xx errors, unhealthy PostgreSQL/Redis/RabbitMQ dependencies, old Outbox work,
or dead letters; latency creates a ticket unless availability is also affected.
The response procedures are in `docs/runbooks/observability.md`.

## Consequences

- One trace can be followed from HTTP through the transactionally stored Outbox,
  a publish span, RabbitMQ headers, and a consumer span after process boundaries.
- The dashboard covers API traffic, business mutations, database/cache/broker
  readiness, Outbox age, dead letters, logs, and trace drill-down.
- The Outbox schema gains nullable, length-bounded trace fields so old records
  and producers remain compatible.
- Audit actor hashes reduce direct identifier exposure but remain pseudonymous
  personal data; authorized business audit queries still use module-owned records.
- Local persistence consumes additional disk and must not be mistaken for a
  backed-up, authenticated production service.

## Alternatives rejected

- Backend-specific application SDKs were rejected because they couple every
  module to the current storage vendor.
- Raw user, Restaurant, Branch, Order, or message IDs as metric labels were
  rejected because cardinality grows with business data.
- Logging complete HTTP or broker payloads was rejected because payloads can
  contain personal data or bearer capabilities.
- Propagating W3C baggage was rejected because callers can place arbitrary or
  sensitive values in baggage.
- Keeping only Collector debug output was rejected because it cannot support
  search, retention, SLO evaluation, or incident response.
