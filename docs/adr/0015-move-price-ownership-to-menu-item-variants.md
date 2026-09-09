# ADR 0015: Move price ownership to MenuItemVariant

- Status: Accepted
- Date: 2026-09-09

## Context

V1 allowed one product to have several titled prices, while early V2 stored one
`Money` value directly on `MenuItem`. Keeping both stores writable would make
ordering totals and public menus ambiguous. Existing V2 clients also consume
the scalar `priceAmount` and `currency` fields.

## Decision

`MenuItemVariant` is a separate Catalog aggregate and the sole persisted price
owner. Each active MenuItem has exactly one active default variant. Active
variant names are unique within an item, variants are deterministically ordered,
and all variants for an item use one currency. Money remains non-negative,
uppercase three-letter currency with at most two decimal places.

The first variant is created as `Default` with the MenuItem. The existing item
create and update requests remain compatible: their price fields create or
update that default variant rather than a MenuItem price column. Management and
public reads add ordered `variants`; their legacy scalar price fields are
read-only projections of the default variant.

A default cannot be deleted. Selecting another default demotes the current one
and promotes the target in one database transaction with version checks.
Variant availability is independent from MenuItem availability and is exposed
to guests. Variant mutations invalidate public Branch-menu cache entries after
success, and the expanded payload uses `public-menu:v3`.

The migration creates the variant table and constraints, copies every existing
MenuItem price to one active `Default` variant, verifies uniqueness structurally,
then drops `menu_items.price_amount` and `menu_items.price_currency`. Its down
path restores those columns from the active default before dropping variants.

## Consequences

- There is no dual writable price source.
- Existing clients can continue reading scalar prices and using item create or
  update while newer clients adopt explicit variant routes.
- Variant IDs and versions support future order-line snapshots and concurrency.
- Currency conversion is not implicit; changing currency requires all sibling
  variants to remain compatible.
- The partial unique indexes enforce at most one active default and one active
  variant name per item; commands prevent removal of the last default.

## Alternatives considered

- Keeping MenuItem price beside variants was rejected because the stores could
  diverge.
- Removing scalar response fields immediately was rejected as an unnecessary
  public-contract break.
- Modeling variants as mutable rows owned by MenuItem was rejected because each
  variant has independent lifecycle and optimistic concurrency.
