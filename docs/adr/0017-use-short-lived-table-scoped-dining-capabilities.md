# ADR 0017: Use short-lived table-scoped dining capabilities

## Status

Accepted

## Context

Restaurant guests must be able to order after scanning a dine-in QR code without
creating an OIDC account. The durable printed public menu code is intentionally
anonymous and can be copied, rotated, or revoked, so it must not itself become
permanent order authority. Guest authority must also be impossible to move to a
different Restaurant, Branch, or table by changing client-supplied identifiers.

## Decision

Create an Ordering module that owns `DiningSession`. A session is a narrow guest
capability with immutable Restaurant, Branch, and DiningTable scope, creation
time, strict expiry, optional revocation, and optional last-seen time. It is not
customer identity and does not grant staff permissions.

`POST /api/public/menu-codes/{code}/sessions` exchanges only a currently valid
`DineInOrdering` code whose Branch and table are active. The Ordering application
depends on an `IPublicCodeResolver` port; the API composition root adapts the
existing Restaurants resolver without creating a module reference. Scope is
copied from that trusted result and never accepted from the guest.

Generate 256 random bits and encode them as an unpadded 43-character base64url
token. Persist only its lowercase SHA-256 hash under a unique index. The default
lifetime is two hours and configuration rejects non-positive lifetimes or values
over 24 hours. Expiry is exclusive: a token is invalid when `now >= expiresAtUtc`.
Database revocation invalidates it immediately and idempotently.

Return the raw token once in the `X-Dining-Session` response header and in the
`rm_dining_session` cookie. The cookie is `Secure`, `HttpOnly`, `SameSite=Strict`,
and limited to `/api/public`. The response body contains scope and expiry metadata
but no token, and responses are `Cache-Control: no-store`. Capability resolution
accepts either transport; if both are supplied they must match. Missing,
malformed, expired, revoked, unknown, or wrong-scope credentials all return the
same HTTP 401 Problem Details response.

Session creation and use have separate IP-partitioned rate-limit policies. Raw QR
codes remain redacted to route templates in request-completion logs and excluded
from automatic ASP.NET request tracing. Session tokens never appear in a URL or
structured application log.

A repeated scan may issue a new independent session because the printed QR is a
shared bootstrap credential, not a one-time secret. Revoking a public code stops
future exchanges; already-issued sessions remain bounded by their own expiry or
explicit session revocation. Future Order handlers must resolve the session on
every operation and compare its trusted scope with the target order/table.

## Consequences

### Positive

- Anonymous guests receive narrowly scoped authority without entering the staff
  OIDC authorization model.
- Database disclosure does not reveal usable session tokens.
- Client-controlled identifiers cannot widen the Restaurant, Branch, or table
  scope established by Restaurants.
- Cookie and explicit-header transports support browser and non-browser clients
  without placing credentials in URLs or bodies.
- The independent Ordering schema and port keep later service extraction viable.

### Negative

- A stolen bearer token remains usable until its short expiry or explicit
  revocation; TLS and careful client storage remain mandatory.
- `SameSite=Strict` requires the browser frontend and API to be deployed in a
  same-site topology or to use the explicit header transport.
- Public-code revocation does not fan out synchronously to existing sessions.
- Ordering adds another DbContext, migration history, readiness check, and module
  boundary before Order behavior exists.

## Verification

Domain and application tests cover immutable scope, purpose, expiry boundaries,
idempotent revocation, collision retry, repeated exchange, and wrong-table
resolution. PostgreSQL tests cover migration, hash uniqueness, hash-only storage,
expiry, and revocation. Functional tests cover active-parent revalidation,
header/cookie transport, mismatched credentials, response/storage secrecy,
generic failures, route redaction, and named rate limits. Architecture tests
enforce all four Ordering layer boundaries.
