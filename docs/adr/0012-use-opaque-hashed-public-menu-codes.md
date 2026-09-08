# ADR 0012: Use opaque hashed public menu codes

## Status

Accepted

## Context

Printed table QR codes need a stable public route, but sequential identifiers
would expose tenant structure and a permanent table credential would be unsafe
for future ordering. Printed codes also need revocation and rotation without
renumbering a DiningTable. Menu viewing and dine-in ordering have different
authority requirements.

## Decision

Model `DiningTable` and `PublicMenuCode` as separate aggregate roots inside the
Restaurants module. DiningTable has immutable Restaurant and Branch ownership,
a positive Branch-unique number, optional display name/capacity, active state,
and optimistic version.

Use a cryptographically random 256-bit base64url code at the canonical anonymous
route `/m/{code}`. Persist only its lowercase SHA-256 hash. Return the raw code
once from create or rotation; management lists never return the raw value or
hash. Generation retries known hash collisions, while the database unique index
is the final race-safe guard.

`MenuOnly` may target a Restaurant or Branch and never a DiningTable.
`DineInOrdering` requires a Branch and active DiningTable. Resolution requires
an active, unexpired code and active referenced Branch/table. It returns scope
only; a future short-lived DiningSession will provide ordering authority.

Rotation replaces the hash, optionally replaces expiry, reactivates the code,
and increments its concurrency version. Revocation is idempotent and versioned.
Composite foreign keys prove that referenced Branches and DiningTables belong to
the same Restaurant. Resolution is anonymously accessible but has a named,
IP-partitioned fixed-window rate-limit policy.

The request logger replaces every `/m/{code}` value with the route template.
ASP.NET Core tracing excludes this secret-bearing route so raw codes do not enter
trace URL attributes. Problem Details use one generic invalid/revoked/expired
response and never echo the supplied code.

## Consequences

### Positive

- A database disclosure does not reveal usable printed QR codes.
- Codes can be revoked or rotated independently of table identity.
- Purpose rules prevent menu-only codes from accidentally granting ordering scope.
- Tenant-safe foreign keys and scoped handlers prevent cross-Restaurant references.
- Expiry, active parents, concurrency, and rate limiting are enforced explicitly.

### Negative

- Lost raw codes cannot be recovered; managers must rotate them.
- Hash lookup adds a small computation before the indexed database query.
- Secret-bearing resolver requests are omitted from automatic ASP.NET traces;
  aggregate metrics and redacted request-completion logs remain available.
- A future DiningSession slice must still exchange a dine-in code for a bounded
  capability before ordering can be enabled.

## Verification

Domain tests cover number/capacity validation, normalization, purpose scope,
expiry, rotation, and revocation. Application tests cover inactive Branches and
collision retry with hash-only persistence. PostgreSQL tests cover uniqueness,
resolution state, and concurrent rotation. Functional tests cover one-time raw
code exposure, anonymous resolution, revocation, tenant isolation, safe Problem
Details/log paths, and rate-limit metadata.
