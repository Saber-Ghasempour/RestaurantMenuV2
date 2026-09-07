# ADR 0009: Enforce restaurant membership at the HTTP boundary

## Status

Accepted

## Context

Permission claims describe a caller's general capabilities, such as
`catalog.write`, but do not identify which restaurant the caller is allowed to
manage. Using permission claims alone would let a manager operate on another
tenant by changing the `restaurantId` route value.

Keycloak remains responsible for authentication and capability claims.
Application-owned membership must remain the source of truth for resource
ownership because access can be revoked independently of token expiration.

## Decision

Store `RestaurantMembership` records in the Restaurants module, keyed by
`(restaurant_id, subject)`. The subject is the stable OpenID Connect `sub`
claim and must not be an email address or display name.

Creating a restaurant also creates an `Owner` membership in the same database
transaction. Restaurant lists join against memberships so callers see only
their own restaurants.

Every management endpoint containing `restaurantId` requires both:

1. its existing permission policy; and
2. the `restaurant-access` resource policy.

The resource authorization handler reads the route identifier and checks the
membership table. Missing membership returns 403 without revealing whether the
requested restaurant exists. Anonymous requests still return 401. Public Menu
endpoints remain anonymous and do not use this management policy.

## Consequences

- Changing a route identifier cannot cross the tenant boundary.
- Membership revocation takes effect on the next request even if the access
  token remains valid.
- The permission claim and database membership have distinct, explicit roles.
- Each protected request currently performs a membership query. A short-lived
  cache may be introduced later, but revocation semantics must be defined first.
- Roles are stored for the future Owner/Manager/Staff capability model; this
  decision only uses membership existence for resource scope.
- Cross-module Catalog routes can be protected without adding a Catalog
  dependency on the Restaurants module because the check is composed in the
  API authorization layer.
- Existing deployments must backfill owner memberships before enabling this
  policy. V2 currently has no production data to migrate, so the generated
  migration intentionally creates the table without guessing historical owners.
