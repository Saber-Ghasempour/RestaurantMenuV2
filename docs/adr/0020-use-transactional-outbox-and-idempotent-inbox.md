# ADR 0020: Use a transactional Outbox and idempotent Inbox

- Status: Accepted
- Date: 2026-09-10

## Context

Order placement and state transitions now produce facts needed by realtime and
future analytics consumers. Publishing directly from an HTTP handler can lose a
fact after the database commits, or publish a fact whose database transaction
later rolls back. Brokers can also redeliver, disconnect, delay, or reorder
messages. Consumer retries must not apply a business effect more than once.

## Decision

Ordering translates `OrderPlaced` and `OrderStatusChanged` domain events into
the immutable `OrderPlacedV1` and `OrderStatusChangedV1` integration contracts.
The aggregate mutation and its Outbox row are persisted by the same
`OrderingDbContext.SaveChangesAsync` call and database transaction. Each
envelope has a UUIDv7 message identity, stable event name, schema version,
aggregate identity and version, occurrence time, and JSON payload containing
the Restaurant and Branch scope. Contract versions are additive; an
incompatible payload requires a new version and parallel consumer support.

A background worker claims bounded batches with PostgreSQL `FOR UPDATE SKIP
LOCKED`, a renewable lock lease, and deterministic occurrence/aggregate-version
ordering. It publishes persistent messages to a durable RabbitMQ topic exchange
with publisher confirms. Failures use bounded exponential backoff. After the
configured maximum attempts, the row is marked dead-lettered for operational
inspection rather than deleted. Processed rows remain an audit and replay
source until a later retention policy is approved.

Consumers execute through the Inbox processor. A PostgreSQL transaction-scoped
advisory lock serializes the `(messageId, consumer)` identity. The Inbox receipt
and consumer effect commit in one transaction; a completed identity is a
no-op on redelivery. Failed effects roll back before their attempt is recorded,
and repeated poison delivery is dead-lettered after the configured bound.

The resulting guarantee is at-least-once transport with exactly-once database
effect per named consumer. It is not global exactly-once delivery. Ordering is
guaranteed only by aggregate version when a consumer enforces it; consumers
must still tolerate delayed and out-of-order messages. RabbitMQ is the default
broker because the current workload needs durable routing and acknowledgements,
not Kafka's partitioned replay and operating model.

## Consequences

- A committed Order fact survives broker downtime and is retried independently
  of the originating HTTP request.
- Duplicate delivery cannot repeat a consumer's transactional database effect.
- Poison messages remain visible with bounded error text, attempt count, and
  dead-letter timestamp; telemetry counts publish/process/failure/duplicate and
  newly dead-lettered outcomes.
- RabbitMQ joins PostgreSQL and Redis in readiness checks and local Compose.
- Guest-session tokens and other secrets never enter event payloads, headers,
  logs, or persistence; Restaurant and Branch identifiers remain explicit.
- B5 can consume committed facts for notification without making SignalR an
  authority or changing the existing public Ordering API.

## Alternatives considered

- Publishing inside request handlers was rejected because database and broker
  commits cannot be atomic and transient broker failure would affect API
  availability.
- A distributed transaction was rejected because RabbitMQ and PostgreSQL do
  not provide a useful portable atomic boundary for this design.
- Broker deduplication alone was rejected because consumer effects and receipt
  persistence still need one local transaction.
- Kafka was deferred because current throughput and replay requirements do not
  justify its partition and operational complexity.
- Deleting successful Outbox/Inbox rows immediately was rejected because it
  weakens diagnosis and controlled replay; retention is a separate policy.
