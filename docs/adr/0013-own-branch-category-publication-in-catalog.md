# ADR 0013: Own Branch category publication in Catalog

## Status

Accepted

## Context

A Branch belongs to Restaurants, while a MenuCategory belongs to Catalog. The
system needs an explicit decision about which categories each Branch exposes
without copying category data into Restaurants or coupling Catalog directly to
Restaurants.Domain. The public QR menu is read frequently and must not expose
unpublished content, stale management metadata, or raw public codes.

## Decision

Catalog owns `BranchCategoryPublication` because publication decides how Catalog
content is exposed. Each row stores `restaurant_id`, `branch_id`, `category_id`,
published state, an optional non-negative display-order override, creation time,
and an optimistic version. `(branch_id, category_id)` is the primary key. A
composite Catalog foreign key proves that the Category belongs to the same
Restaurant; a narrow API-composed Branch port proves Branch ownership without a
Catalog-to-Restaurants project or database foreign key.

`SetBranchCategoryPublications` is a complete-set replacement: every active
Category must appear exactly once. Repeating an equivalent set performs no
write. A child may be published only when its parent is published. This keeps a
public hierarchy navigable and makes omission an error rather than an accidental
unpublish operation.

Management uses the nested route
`/api/restaurants/{restaurantId}/branches/{branchId}/category-publications` with
Catalog permission plus Restaurant membership. The management read left-joins
all active Categories so never-configured entries appear as unpublished.

The anonymous `GET /api/public/menu-codes/{code}/menu` endpoint first resolves
the opaque code in Restaurants. Branch-scoped codes invoke Catalog's public
Branch projection; Restaurant-scoped codes retain the Restaurant menu fallback.
The Branch projection includes only published, active Categories and available,
active items. Category ordering uses the Branch override or base order, followed
by name and identifier for deterministic ties.

Successful Branch menus use cache-aside with the contract-versioned key
`public-menu:v1:restaurants:{restaurantId}:branches:{branchId}` and an absolute
TTL. A changed publication set invalidates only after the database commit;
equivalent sets do not churn the cache. Redis failures remain fail-open. Code
menu paths are redacted in request logs and excluded from automatic HTTP traces.

## Alternatives considered

- Restaurants-owned publication would make Restaurants responsible for Catalog
  visibility rules and duplicate Catalog identifiers and policy.
- A direct Catalog foreign key to `restaurants.branches` would improve database
  enforcement but couple module schemas and complicate later extraction.
- Sparse "published rows only" state would make explicit unpublished management
  state and complete-set concurrency harder to inspect.
- Publishing a child while hiding its parent would require an additional public
  flattening contract and produce ambiguous navigation.

## Consequences

- Catalog remains the single owner of menu exposure rules.
- Tenant checks exist at both the cross-module Branch port and the composite
  Category foreign key.
- Complete-set requests are unambiguous and idempotent but grow with category
  count; a patch contract may be added if measurements justify it.
- Public reads avoid N+1 queries and cache successful composed responses.
- Until integration events/outbox exist, Branch/profile and other Catalog
  changes can remain stale for the bounded TTL unless their handlers explicitly
  invalidate the key. Publication changes are invalidated immediately.

## Verification

Domain and application tests cover validation, complete-set idempotency,
parent/child policy, cache hits, and commit-before-invalidate ordering.
PostgreSQL tests cover composite ownership and deterministic published
projection. Functional tests cover anonymous code menus, unpublished exclusion,
real Redis invalidation, and cross-tenant Branch/Category rejection.
