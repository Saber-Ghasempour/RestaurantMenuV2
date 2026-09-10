# ADR 0019: Enforce order transitions with Branch-scoped staff authority

- Status: Accepted
- Date: 2026-09-10

## Context

An authenticated restaurant member is not automatically authorized to operate
every Branch or perform every order action. Order updates can also race across
cashier, kitchen, and waiter clients. The workflow needs one authoritative state
machine, an immutable audit trail, and queue reads that cannot cross tenant or
Branch boundaries. Guests remain capability holders rather than staff principals.

## Decision

Ordering owns the dine-in state machine: `Placed -> Accepted -> Preparing ->
Ready -> Served -> Completed`, with `Rejected` and `Cancelled` terminal states.
Only explicit aggregate methods can change status. Rejection and staff
cancellation require a bounded reason. Every successful change increments the
Order version, appends an `OrderStatusHistory` row, and raises an in-process
`OrderStatusChanged` domain event. The history records its from/to states,
Guest/Staff/System actor type, optional staff subject, reason, and UTC time.

Restaurants owns `BranchMembership`. Its composite identity is Branch plus
identity-provider subject, while tenant-safe foreign keys require both the exact
Restaurant/Branch pair and an existing Restaurant membership. Memberships have
Manager, Cashier, Kitchen, or Waiter roles and can be suspended. The API
composition root exposes only the active role through an Ordering-owned port.

Staff endpoints require all three checks: the specific `orders.*` permission,
Restaurant resource membership, and an active Branch membership with the role
allowed for the current state/action. Every mutation also supplies
`ExpectedVersion`; EF Core's concurrency token is the final race barrier and a
stale update returns 409. Reads and writes always include Restaurant and Branch
in their predicates. Kitchen, Cashier, and Waiter queues are separate queries
with fixed status sets rather than a client-controlled filter.

Guests can read only an Order owned by their resolved DiningSession. They may
cancel their own `Placed` Order for five minutes after creation; the capability,
session ownership, expected version, and reason are all required. A public order
number or Order ID is never authority.

## Consequences

- Workflow rules are testable without HTTP or persistence and terminal states
  cannot be reopened.
- Permission claims alone cannot grant Branch access, and a Branch role alone
  cannot grant an API capability.
- Status history and the Order version commit atomically in the Ordering schema.
- Queue indexes support exact tenant/Branch/status reads; timeline indexes keep
  history deterministic.
- B4 can translate committed domain events to integration events through the
  Outbox without moving state mutation into messaging or SignalR.

## Alternatives considered

- A single generic `SetStatus` endpoint was rejected because it makes illegal
  transitions and role mapping easy to bypass.
- Job titles encoded only in token claims were rejected because Branch
  assignment and suspension are mutable application data.
- One role-conditioned queue endpoint was rejected because its filters are less
  explicit and harder to optimize and authorize.
- Last-write-wins updates were rejected because concurrent staff actions would
  silently lose audit facts.
