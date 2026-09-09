# ADR 0018: Own order snapshots and HTTP idempotency in Ordering

- Status: Accepted
- Date: 2026-09-09

## Context

Placing a dine-in order crosses three ownership boundaries. Ordering owns the
transaction, Catalog owns current item/variant names, availability and Money,
and Restaurants owns the table display data. A guest controls the JSON request
and may retry it after a timeout. Trusting tenant, table, names, or prices from
that request would permit scope and price tampering; blindly repeating the
write could create duplicate orders.

## Decision

Ordering owns `Order`, `OrderLine`, and `IdempotencyRecord` in the `ordering`
schema. A place-order request contains only item ID, optional variant ID,
quantity, and notes. The short-lived DiningSession capability supplies the
restaurant, Branch, table, and session IDs on every command.

The API composition root implements narrow Ordering-owned snapshot ports. The
Catalog adapter returns a line only when the restaurant, Branch category
publication, category publication, item publication, item availability,
variant ownership, and variant availability all match. A missing variant ID
selects the current default. The Restaurants adapter returns only an active
table in the exact session scope. Ordering copies trusted names, selected
variant, price, currency, and table display name into immutable order rows and
calculates every line total and order total on the server.

`POST /api/public/orders` requires `Idempotency-Key`. Its scope is the resolved
DiningSession ID. Ordering hashes a canonical client payload with SHA-256. The
order, lines, and idempotency record are saved by one EF Core unit of work. A
unique `(scope, idempotency_key)` constraint is the concurrency barrier. The
same key and hash returns the original order with HTTP 201; changed-payload
reuse returns 409. A concurrent loser gets 409 and can retry after the winner
commits. Records expire after 24 hours and future retention maintenance may
remove them.

The public order number is display data, not authority. Reads and mutations
continue to require the DiningSession capability or future staff authority.

## Consequences

- Historical receipts remain stable when Catalog names or prices change.
- Client prices, tenant IDs, Branch IDs, and table IDs cannot affect an order.
- Cross-module reads remain composition-root adapters with no cross-schema
  foreign keys.
- Snapshot reads and the Ordering commit are deliberately not a distributed
  transaction; accepted snapshot values become the order fact.
- `OrderPlaced` is a domain event. B4 will add the transactional Outbox/Inbox
  and external integration-event contract.

## Alternatives considered

- Client-carried price and table data was rejected as untrusted and stale.
- Cross-schema foreign keys were rejected because snapshots must outlive source
  catalog data and module schemas remain independently owned.
- A global idempotency key was rejected because unrelated guests could collide.
- Outbox was left to B4, where the roadmap introduces reliable publication and
  a broker as one coherent slice.
