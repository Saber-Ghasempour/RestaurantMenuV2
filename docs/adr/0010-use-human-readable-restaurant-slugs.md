# ADR 0010: Use human-readable restaurant slugs for public menu lookup

## Status

Accepted

## Context

The first Public Menu slice is addressable by restaurant identifier. That route
is useful for an internal contract, but a public menu link should be readable
and shareable. A slug must also remain independent from the future Branch, Table,
and opaque QR-code model.

Restaurant slugs are mutable management data, so changes need the same
optimistic-concurrency and tenant-membership protections as other restaurant
updates. Concurrent claims must be resolved by the database rather than by a
check-then-insert race. A deleted restaurant must not resolve publicly, and its
old slug should not silently become available for another restaurant while
links or printed material may still reference it.

## Decision

Add an optional `Restaurant.Slug` owned by the Restaurants module. Normalize
input by trimming and lowercasing it. Valid slugs contain 3 to 80 ASCII letters,
digits, or single hyphens, with no leading or trailing hyphen. A unique database
index named `ux_restaurants_slug` enforces uniqueness for non-null values.

Expose the protected management endpoint:

```http
PUT /api/restaurants/{restaurantId}/slug
```

The request includes the desired slug and `expectedVersion`. It requires the
`restaurants.write` permission and active restaurant membership. A successful
change increments the restaurant version, raises
`RestaurantSlugChangedDomainEvent`, and invalidates the restaurant-detail cache
after the database commit. Invalid input returns 400, stale versions return
409, and a concurrent duplicate claim returns
`Restaurants.SlugAlreadyExists` as 409.

Expose the anonymous public lookup endpoint:

```http
GET /api/public/restaurants/by-slug/{slug}/menu
```

The API composition root resolves the slug through a Restaurants application
port and then invokes Catalog's existing public-menu query. This preserves the
module boundary: Catalog does not reference Restaurants.Domain or its database.
The response is the same public bootstrap contract as the restaurant-ID route
and includes the current slug.

Soft-deleted restaurants do not resolve through slug lookup, but their slug
reservation remains in the database. The slug is therefore not reused by a
new restaurant.

## Consequences

### Positive

- Public menu URLs are readable and easier to share than raw identifiers.
- Database uniqueness handles concurrent slug claims safely.
- Management writes retain existing permission, membership, and concurrency
  guarantees.
- Public slug lookup reuses the existing Catalog projection without coupling
  Catalog to Restaurants internals.
- Slugs do not become an ordering authorization mechanism or replace the future
  opaque QR mapping.

### Negative

- Slug changes invalidate existing public links.
- Deleted slugs remain reserved, which consumes names over time.
- The lookup adds a Restaurants read before the Catalog public-menu read.
- A slug is not sufficient context for branches, tables, or ordering.

## Verification

Domain tests cover normalization, validation, no-op versioning, and the slug
change event. Functional tests cover public resolution, duplicate claims under
concurrency, cache invalidation, deletion and reservation behavior, stale
updates, and permission or membership failures.
