# ADR 0005: Use cache-aside for restaurant details

- Status: Accepted
- Date: 2026-09-07

## Context

Restaurant detail reads are safe to reuse and are a suitable first place to
demonstrate distributed caching. PostgreSQL remains the source of truth, while
Redis is an optimization that can be temporarily unavailable. Restaurant names
can change and restaurants can be soft-deleted, so cached values must not live
indefinitely.

## Decision

Use the cache-aside pattern for `GetRestaurantQuery` behind the
application-owned `IRestaurantCache` port. On a cache miss, read the projection
from PostgreSQL and cache only a successful response. Do not cache not-found
results.

Use versioned, namespaced keys and a configurable five-minute absolute TTL.
After a successful database commit, update and delete handlers remove the
restaurant entry. Invalidation never happens before commit.

Treat Redis connection failures and invalid payloads as cache misses. Cache
set and remove failures are logged but do not fail an otherwise successful
database operation. Readiness still reports Redis as unhealthy so the degraded
dependency remains visible operationally.

Configure the shared Redis client with connection-abort disabled, a fail-fast
backlog policy, short configurable connection and operation timeouts, and a
bounded reconnect policy. Cache degradation must not turn a database fallback
into a long wait, and the client must recover after Redis becomes available.

## Consequences

### Positive

- Repeated detail reads avoid PostgreSQL and reduce response latency.
- Application code depends on a narrow cache port rather than Redis APIs.
- Redis outages do not make the source-of-truth database unavailable.
- TTL bounds the life of an entry if best-effort invalidation fails.
- No negative caching means a newly created resource is immediately visible.

### Negative

- Cache-aside does not prevent a cache stampede on a hot missing key.
- A failed invalidation can serve stale data until the TTL expires.
- Every successful mutation must remember the invalidation contract.
- Redis adds serialization, monitoring, security, and operating costs.

## Future considerations

Measure hit rate and database load before caching lists or Catalog responses.
If contention justifies it, add request coalescing or short-lived distributed
locking. Stronger consistency would require a different design, such as
versioned keys or event-driven invalidation with reliable publication.
