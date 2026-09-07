# ADR 0007: Use Keycloak and permission-based authorization

- Status: Accepted
- Date: 2026-09-07

## Context

The API needs standards-based authentication for interactive users and service
clients without owning passwords, login flows, token issuance, or signing-key
rotation. Module endpoints also need authorization rules that express business
capabilities instead of depending on broad framework roles.

## Decision

Use Keycloak as the local OpenID Connect and OAuth 2.0 identity provider. Keep
the API as a resource server: it accepts access tokens through ASP.NET Core JWT
Bearer authentication and validates signature, issuer, audience, expiry, and
not-before time. Production metadata and issuer endpoints require HTTPS.

Represent API capabilities as a multi-valued `permission` claim. Define four
policies: `restaurants.read`, `restaurants.write`, `catalog.read`, and
`catalog.write`. Read endpoints require the matching read permission; mutation
endpoints require the matching write permission. A fallback policy protects any
endpoint that is not explicitly anonymous. Health probes and development
OpenAPI are anonymous by design.

Provide a local realm import with a public browser client that requires the
Authorization Code flow with PKCE, and a confidential service client for local
machine-to-machine verification. Secrets and demo passwords are supplied by
environment variables and real credentials must never be committed. Functional
tests replace only the authentication handler with a deterministic test scheme;
the authorization policies and endpoint metadata remain production code.

## Consequences

### Positive

- Passwords, sessions, federation, MFA, and signing keys remain identity-provider concerns.
- The API is stateless and authorization rules are explicit at each endpoint.
- Audience validation prevents a token issued for another resource from being accepted.
- Permission policies are easier to evolve and audit than controller-wide role checks.
- Tests cover unauthenticated, forbidden, and permitted requests without a network dependency.

### Negative

- Local development gains another container and a longer startup time.
- Permission changes do not affect an already-issued token until it expires or is refreshed.
- Browser SSO still requires a frontend/BFF and secure token-handling design.
- The development realm is convenient bootstrap data, not production identity configuration.

## Operational notes

The API retrieves discovery metadata through the internal Compose hostname but
validates the public issuer embedded in tokens. Keycloak is a startup dependency
in the local stack, but it is not included in API readiness: after signing keys
are cached, a temporary identity-provider outage should not evict otherwise
healthy API replicas that can still validate existing tokens.

Use a secrets manager, TLS, restricted redirect URIs, short-lived access tokens,
key rotation, administrative audit logs, brute-force protection, MFA, and
separate realms or identity deployments per environment before production.
