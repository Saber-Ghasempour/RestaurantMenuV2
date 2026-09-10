# ADR 0023: Separate identity provisioning from membership authority

- Status: Accepted
- Date: 2026-09-10

## Context

Restaurant owners need to invite staff by email and control their Restaurant
and Branch access. Keycloak authenticates people, but an identity-provider role
or a long-lived access-token claim cannot safely represent revocation-sensitive
tenant membership. Invitation links are bearer capabilities and therefore also
need expiry, replay protection, and storage that does not disclose the raw
secret after issuance.

## Decision

Keycloak owns authentication and the stable OIDC `sub` identifier. The
Restaurants module owns invitation state and all authorization-changing
membership data: Restaurant, subject, role, status, version, and Branch scope.
Every protected resource operation checks the current application-owned
membership even when the JWT is otherwise valid.

Invitation acceptance reaches identity management only through
`IIdentityProvisioner`. Its Keycloak adapter uses a dedicated confidential
client to find or create an identity, then stores the returned stable subject
in the membership. Email is an invitation address, not an authorization key.
If the dedicated client is not fully configured, provisioning fails closed.
The existing API client credential is not promoted into an administrative
credential.

The dedicated provisioning client is granted only Keycloak's
`realm-management/manage-users` role and its secret is supplied through runtime
configuration. Because that built-in role is broader than this application's
single operation, the credential must also be isolated by secret management,
network policy, rotation, and audit. It is not included in the checked-in
development realm.

Invitation tokens contain 256 random bits. The raw URL-safe token is returned
once; only its SHA-256 hash is persisted. Invitations expire after two days,
are single-use, may be revoked, and are protected by a partial unique index for
one pending invitation per normalized Restaurant/email pair.

Owners may grant any Restaurant role. Managers may invite or manage Staff but
cannot create Managers or Owners, change an existing privileged member, or
grant Branch Manager authority. Staff cannot manage memberships. The final
active Owner cannot be demoted, suspended, or revoked. Membership and
invitation changes use optimistic concurrency and append a tenant-scoped audit
entry in the same database transaction. Audit records contain identifiers and
action metadata, never raw invitation tokens, client secrets, or credentials.

## Consequences

- Revoking a membership takes effect independently of JWT expiry.
- Identity-provider availability is required only when accepting an invitation,
  not for ordinary authorization checks.
- Tenant isolation remains enforceable and queryable inside the application
  database.
- Operators must provision and rotate a separate least-privilege Keycloak
  client before invitation acceptance can create identities in production.
- Keycloak identity creation and the application database transaction cannot be
  atomic. A retry safely resolves an already-created identity by normalized
  email before completing membership acceptance.

## Alternatives rejected

- Keycloak realm/client roles as tenant membership: revocation can remain stale
  in issued tokens and per-Restaurant/Branch scope becomes identity-provider
  policy state.
- Store raw invitation tokens: a database disclosure would immediately expose
  every pending invitation capability.
- Reuse the API machine client: this unnecessarily escalates a credential that
  was deliberately read-only.
- Automatically grant broad realm administration in the development import:
  convenient defaults are not worth normalizing an unsafe production shape.
