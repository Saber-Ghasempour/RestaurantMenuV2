# ADR 0024: Version API contracts and bound HTTP concurrency and retries

- Status: Accepted
- Date: 2026-09-10

## Context

The API already exposes aggregate versions in representations and request bodies,
persists order idempotency keys, and rate limits public capabilities. Those
behaviors were locally correct but not expressed consistently through HTTP.
Identity provisioning also used an unconfigured `HttpClient`, so a dependency
stall could outlive the request and transient failures had no explicit policy.

## Decision

The current compatible contract is published as OpenAPI document `v1` with
semantic version `1.0`. Routes remain under `/api`; a new route prefix is added
only for an incompatible contract rather than duplicating every route now.

Versioned JSON representations emit a strong numeric `ETag`. Clients may send a
single strong numeric `If-Match` on a versioned mutation. During the v1
compatibility window the existing `expectedVersion` field remains valid without
the header; when both are present they must agree. A stale conditional request
returns HTTP 412, while a legacy stale body-only request retains HTTP 409.

Order placement remains the only demonstrated HTTP idempotency boundary. API
middleware validates and forwards `Idempotency-Key`; Ordering retains the durable
SHA-256 request fingerprint and database uniqueness barrier. No generic response
cache is introduced for unrelated commands.

All requests have a configurable 15-second default timeout and receive the
request cancellation token. Dining-session usage is partitioned by a SHA-256
digest of the capability rather than its raw secret or only the caller IP.

The Keycloak administration adapter uses the named `keycloak-admin` client with
a ten-second timeout and a bounded three-attempt pipeline. Only GET, HEAD, and
OPTIONS retry transport failures, 408, 429, and 5xx responses. Token acquisition
and identity creation are POST operations and never retry automatically; caller
cancellation is never converted into a retry.

## Consequences

- HTTP caches and clients can use standard validators without exposing internal
  persistence details.
- Existing v1 clients can migrate from body versions to `If-Match` incrementally.
- A timed-out request is cancellation, not proof that an operation did not
  commit; idempotent operations must still reuse their key.
- Credential-derived rate-limit keys are stable but never contain the capability.
- Keycloak recovery improves for safe reads without duplicating user creation.
- The response middleware inspects bounded API JSON representations to discover
  top-level versions; streaming endpoints remain outside `/api`.

## Alternatives rejected

- Require `If-Match` immediately: this would break every current v1 client.
- Retry every Keycloak call: replaying POST identity creation is unsafe even if
  the provider often detects duplicates.
- Add generic idempotency storage for every POST: no other command currently has
  a demonstrated retry contract or stable response-replay requirement.
- Put raw dining-session tokens in rate-limit partition keys: diagnostics could
  then disclose bearer capabilities.
