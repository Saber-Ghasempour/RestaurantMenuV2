# ADR 0011: Model Branches as aggregates inside the Restaurants module

## Status

Accepted

## Context

V1 stored Branches with a Restaurant identifier, name, phone, table count,
coordinates, and mandatory Province/City identifiers. Branch is required before
table-aware QR resolution, Branch-specific menu publication, staff scope,
Ordering, and Feedback can be implemented.

Branch has its own lifecycle and concurrency boundary, but it does not yet have
an independent deployment, scaling, data-ownership, or team-ownership need.
Creating a Branches microservice or even a separate module now would add
cross-module consistency and operational cost without evidence. V1 `TableCount`
also cannot represent table identity or code lifecycle, while mandatory regional
foreign keys would couple the new model to one geographic data source.

## Decision

Model `Branch` as a separate aggregate root inside the Restaurants module. A
Branch has immutable `RestaurantId`, validated and normalized public details,
optional slug, paired decimal coordinates, optional ISO country code and IANA
time-zone identifier, active state, optimistic version, and soft-delete state.

Store Branches in `restaurants.branches`. Enforce Restaurant ownership with a
restricting foreign key, scope every repository/read query by both Restaurant
and Branch identifiers, and enforce optional slug uniqueness with
`ux_branches_restaurant_id_slug`.

Expose nested management routes under:

```http
/api/restaurants/{restaurantId}/branches
```

Create, read, list, update, status, and delete operations require dedicated
`branches.read` or `branches.write` permission plus active Restaurant membership.
The query side uses direct no-tracking projections with bounded paging, search,
status filtering, and deterministic ordering.

Do not copy `TableCount`. A later DiningTable aggregate will represent real
tables. Store address fields as optional text and ISO country code; defer a
Province/City reference-data module until current product requirements justify it.
Media references are deferred to the Media slice.

## Consequences

### Positive

- Branch has independent invariants and optimistic concurrency without premature
  service boundaries.
- Nested routes, scoped queries, membership, and permissions protect tenant data.
- Decimal coordinates and paired validation avoid partial or imprecise locations.
- Branch-local slugs can be reused by different Restaurants but not duplicated
  within one Restaurant.
- The design provides a stable parent for DiningTable, public codes, menu
  publication, staff scope, and Orders.

### Negative

- Restaurants now owns two aggregate types and a larger schema.
- Flexible address text provides less normalization than Province/City reference
  tables until that optional module exists.
- Dedicated Branch permissions require identity-provider role configuration.
- Public Branch caching and public projections remain future work.

## Verification

Domain tests cover normalization, validation, atomic updates, no-op behavior,
status transitions, events, and idempotent deletion. Application tests cover
creation, missing Restaurant handling, and stale updates. PostgreSQL integration
tests cover materialization, Restaurant scope, soft-delete filtering, slug
uniqueness, search, status filtering, and deterministic reads. Functional tests
cover the complete lifecycle, validation, duplicate conflicts, permissions,
membership, and cross-Restaurant Branch identifiers. The complete suite contains
309 passing tests.
