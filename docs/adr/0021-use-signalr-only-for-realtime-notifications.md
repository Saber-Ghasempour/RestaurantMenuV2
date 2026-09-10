# ADR 0021: Use SignalR only for realtime notifications

- Status: Accepted
- Date: 2026-09-10

## Context

V1 placed orders and changed operational state through a SignalR Hub. That made
a transient connection an authority for business commands and bypassed the HTTP
idempotency, authorization, validation, and concurrency boundaries now owned by
Ordering. V2 still needs low-latency guest and staff updates, but broker and
WebSocket delivery are at least once and connections can disappear at any time.

## Decision

Notifications is a stateless module with Application, Infrastructure, and
Presentation projects. It deliberately has no Domain aggregate, database schema,
or migration. Its Application layer consumes the committed
`OrderPlacedV1` and `OrderStatusChangedV1` contracts through B4's Inbox and calls
an `IRealtimeNotifier` port. Infrastructure owns the durable RabbitMQ consumer
and queue; Presentation implements the port with typed SignalR hub contexts.

The hubs expose subscription methods only. They never create an Order, perform
a transition, or write business state. Guests connect at
`/api/public/order-notifications`, where the existing secure DiningSession cookie
path applies, and may join only an Order returned by the capability-scoped guest
query. Staff connect at `/hubs/staff-order-notifications`; the handshake requires
`orders.read`, and every Branch join rechecks the authenticated subject's active
Branch role. Guest Order and staff Branch group namespaces are separate.

`OrderPlacedV1` notifies only its staff Branch. `OrderStatusChangedV1` notifies
that Branch and the exact guest Order group. The payload contains event ID,
stable event name, Order ID, status, aggregate version, and occurrence time.
Clients deduplicate by event ID and ignore an older aggregate version. SignalR
is a hint, not a source of truth: after initial connect or reconnect, clients
rejoin authorized groups and use the returned/current HTTP Order or queue
projection to recover anything missed while disconnected.

RabbitMQ delivery uses the transactional Inbox consumer identity. Completed
duplicates become no-ops; invalid envelopes are rejected to a durable dead-letter
queue. A crash after SignalR send but before Inbox commit can still produce a
duplicate, which is why event identity and client tolerance remain required.

## Consequences

- Existing HTTP command and query contracts remain authoritative and unchanged.
- A forged Order or Branch identifier cannot join another guest session or
  Branch group; errors do not reveal whether the target exists.
- Reconnect does not promise replay through SignalR. The database-backed HTTP
  projection closes delivery gaps.
- Notification scale can evolve independently without moving Order invariants
  or tenant authorization into a Hub.
- The shared Ordering Inbox currently provides deduplication. If Notifications
  becomes a separately deployed service, it must own an equivalent local Inbox.

## Alternatives considered

- SignalR command methods were rejected because connection retries are a poor
  business idempotency boundary and would duplicate Ordering authority.
- One public Branch group was rejected because it exposes unrelated guest and
  staff activity.
- Trusting client-supplied group names was rejected because identifiers are not
  authorization.
- Persisting per-connection delivery was deferred because current requirements
  need recoverable hints, not a user-visible durable notification inbox.
- Server-side replay on reconnect was deferred in favor of existing authoritative
  Order and queue queries.
