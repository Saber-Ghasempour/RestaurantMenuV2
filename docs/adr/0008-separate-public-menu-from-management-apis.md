# ADR 0008: Separate the public menu from management APIs

- Status: Accepted
- Date: 2026-09-07

## Context

Restaurant guests arrive through a QR code and must be able to view the public
menu without creating an account. Management reads contain operational fields
such as concurrency versions and are governed by staff permissions. Making all
existing GET endpoints anonymous would mix two audiences, expose fields that
guests do not need, and make later authorization changes risky.

The branch, table, and durable QR-code concepts have not yet been modeled in
V2. Introducing a temporary branch or QR schema before their invariants are
defined would create migrations that are immediately replaced.

## Decision

Create a dedicated anonymous Public Menu query and HTTP contract. The first
slice uses `GET /api/public/restaurants/{restaurantId}/menu` as a temporary
lookup route and returns one bootstrap representation containing the public
Restaurant name, ordered active categories, and ordered available active menu
items. It excludes aggregate versions, deletion state, timestamps, and other
management metadata.

Catalog owns the public menu projection and executes two no-tracking database
queries: one for categories and one for available items. Application composes
that projection with a small public Restaurant profile port. The API composition
root implements the port through the Restaurants application contract, so
Catalog does not reference the Restaurants module.

Keep all existing management endpoints protected. After Branch and Table are
modeled, add a server-owned opaque public-code mapping and expose
`GET /api/public/menu-context/{publicCode}`. The printed QR will target the
stable `/q/{publicCode}` frontend route. The restaurant-ID route can then be
retained as a shareable restaurant-level menu or deprecated through API
versioning; it must not be used as table/order authorization.

## Consequences

### Positive

- Guests can view a menu anonymously while management APIs remain protected.
- The transport exposes only fields intended for the public audience.
- One HTTP response avoids the legacy frontend's branch/category/product
  request waterfall.
- Query code avoids aggregate materialization and N+1 item queries.
- Existing module dependency boundaries remain intact.

### Negative

- The bootstrap response can become large and will need measurement,
  compression, ETags, and cache policy as menus grow.
- Restaurant and Catalog data are read separately and do not form one database
  snapshot across module boundaries.
- The temporary identifier route does not yet resolve branch or table context.

## Verification

Application tests cover composition, empty menus, and missing restaurants.
Functional tests exercise the anonymous HTTP pipeline against PostgreSQL and
verify that unavailable and soft-deleted data is excluded while management
reads still return 401 to an anonymous caller.
