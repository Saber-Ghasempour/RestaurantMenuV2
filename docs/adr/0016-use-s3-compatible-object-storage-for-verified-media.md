# ADR 0016: Use S3-compatible object storage for verified media

- Status: Accepted
- Date: 2026-09-09

## Context

Restaurant, Category, and MenuItem images must not be stored in PostgreSQL or
identified by environment-specific URLs. Uploads are untrusted, tenant-owned
content: client-provided extensions, MIME types, sizes, checksums, and paths are
not verified metadata. Deleting an object while another module references it
would break public menus.

## Decision

Create a Media module that owns `media.media_assets` and the
Pending/Ready/Rejected/Deleted lifecycle. It generates an opaque tenant-prefixed
storage key and issues a 15-minute signed PUT URL. Completion streams the object
through a 10 MiB bound, detects PNG/JPEG/WebP from file signatures, extracts
dimensions, computes SHA-256, and grants Ready only when the verified values
match the declaration.

Use an `IObjectStorage` application port with an AWS S3 adapter and MinIO in
local Compose and integration tests. Persist storage keys, never public or
signed URLs. Require AWS Signature Version 4. Management reads receive
five-minute signed GET URLs. Public menu
contracts expose stable API media URLs; the anonymous resolver proves the Ready
asset is referenced by published content before redirecting to object storage.
Internal object access and browser-visible signing endpoints are configured
separately so Compose does not leak its private `minio` hostname to clients.

Restaurants and Catalog own their logical references. Cross-module readiness
and deletion checks are narrow ports composed in the API. There are deliberately
no cross-schema foreign keys. Active SHA-256 values are unique within a tenant;
the same checksum in another tenant is allowed. A cleanup worker rejects and
removes Pending objects older than 24 hours.

## Consequences

- Existing public and management contracts remain compatible through appended
  nullable fields.
- Object bytes bypass the API upload path, while completion verifies the actual
  bounded content.
- Deletion is reference-safe across branding, Category images, and MenuItem images.
- Database and object-store changes are not one transaction. Logical deletion
  commits before idempotent removal; command retries and the cleanup worker retry
  failed object removal.
- MinIO is a development/test dependency, not a production mandate.
- Production malware scanning must run before Ready. Extension checks do not
  substitute for that deployment capability.

## Alternatives considered

- PostgreSQL blobs: rejected because object delivery and growth would burden the
  transactional database and its backups.
- Persisted public URLs: rejected because host/CDN changes become data migrations
  and signed URLs expire.
- Trust client MIME or extensions: rejected because that permits content-type
  confusion and bypasses the integrity boundary.
- Polymorphic attachment rows: rejected because nullable owner columns weaken
  ownership. Each module stores its own explicit reference shape.
