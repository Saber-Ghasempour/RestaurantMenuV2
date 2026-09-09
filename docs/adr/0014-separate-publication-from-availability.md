# ADR 0014: Separate Catalog publication from availability

## Status

Accepted

## Context

V1 used overlapping `IsExist` and `IsAvailable` flags. Treating either flag as
deletion made it impossible to distinguish draft or withdrawn content from an
item that remains on the menu but is temporarily sold out. A4 also adds
category publication, richer item metadata, and fields required by existing
public-menu clients, so the compatibility behavior must be explicit.

## Decision

Categories and MenuItems have an explicit base `IsPublished` state. New content
starts unpublished. The A4 migration backfills pre-existing categories and
items as published so an upgrade does not silently empty an existing public
menu.

Public Restaurant menus require the Category and MenuItem to be published.
Public Branch menus additionally require the existing Branch category
publication. A child Category cannot be published while its parent is
unpublished, and a Category with published children cannot be unpublished.

`IsAvailable` is independent of publication. A published but unavailable item
remains in the public response with `isAvailable: false`; clients disable
ordering and may display a sold-out state. Unpublished or soft-deleted content
is absent. Availability therefore represents temporary orderability, not
visibility or lifecycle.

Management uses focused content, metadata, publication, and availability
commands with optimistic versions instead of one over-posting request. Public
DTO changes are additive: Restaurant defaults, Category description, item
metadata, `isFeatured`, and `isAvailable` are added without renaming or removing
existing fields. The Branch public-menu Redis namespace advances to
`public-menu:v2` so old payloads are not served under the expanded contract.

Changed published Catalog content invalidates all known Branch menu entries for
the Restaurant only after the database commit. Restaurant-default changes use
an API-composed invalidation port so Restaurants does not reference Catalog.
Redis failures retain the existing fail-open behavior.

## Alternatives considered

- Hiding unavailable items would preserve the old projection but would make a
  temporary sold-out state indistinguishable from withdrawn content.
- Reusing soft deletion for publication would discard draft and reversible
  editorial workflows and mix audit lifecycle with guest visibility.
- Returning breaking replacement DTOs would force coordinated client rollout
  without a demonstrated need; additive fields preserve compatible readers.
- Publishing all newly created content would preserve old behavior but make
  accidental guest exposure the default.

## Consequences

- Staff can prepare drafts without exposing them and can mark sold-out items
  without removing them from the menu.
- Public clients must honor `isAvailable` before enabling an order action.
- Existing data stays visible after migration, while new data requires an
  explicit publication command.
- Cache invalidation now spans every Branch publication for a Restaurant; this
  is bounded by the number of configured Branches and can later move to
  committed integration events.
- The public contract grows additively and its Redis representation has a new
  namespace version.

## Verification

Domain tests cover defaults, metadata limits and normalization, nullable
calories, tag rules, idempotency, and publication transitions. Application
tests cover scoping, commit-before-invalidate behavior, and parent/child
publication policy. PostgreSQL tests cover metadata projection and constraints.
Functional tests cover focused endpoints, authorization, draft exclusion,
unavailable-item visibility, public defaults, and real Redis invalidation.
