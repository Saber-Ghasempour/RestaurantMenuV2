# Stripe Connect operations runbook

## Production setup

1. Create or verify the Portugal Stripe platform account and complete its
   business verification.
2. Enable Connect with Express connected accounts. Review Stripe's platform
   profile, branding, support, dispute, and negative-balance settings.
3. Create restricted production credentials where Stripe supports the required
   operations. Store `Stripe__SecretKey` only in the runtime secret store.
4. Register `POST /api/webhooks/stripe` as a Connect webhook endpoint for
   events on connected accounts. Subscribe at minimum to
   `payment_intent.succeeded`, `refund.created`, and `refund.updated`; store its signing secret as
   `Stripe__WebhookSecret`.
5. Configure the web client with Stripe.js/Payment Element over HTTPS. Enable
   the desired automatic payment methods in Stripe. Register production domains
   for Apple Pay when Stripe requires it; verify Google Pay on supported devices.
6. Put `ConnectionStrings__Payments` in the runtime secret. It may address the
   shared PostgreSQL database, but the module owns only the `payments` schema.
7. Run the normal migration job, onboard a test restaurant, enable payments only
   after Stripe reports charges, payouts, and details ready, then perform a
   low-value payment and refund in staging.

## API flow

- An authorized restaurant owner or manager starts onboarding with country,
  three-letter settlement currency, and HTTPS refresh/return URLs.
- After the Stripe-hosted flow finishes, the restaurant calls the enable
  endpoint. Readiness is checked server-side with Stripe.
- A guest presents the DiningSession capability and order ID. Ordering supplies
  the trusted bill; the client may supply only a non-negative tip in minor units.
- The API creates a direct connected-account PaymentIntent and returns its
  client secret. Stripe.js completes the selected card or wallet method.
- An authorized restaurant refund is submitted through RestaurantMenu, which
  asks Stripe to refund the charge and reverse the application fee. Do not use
  an out-of-band Dashboard refund as the normal operational path.
- Only verified webhooks mark success or record refunds. Browser redirects are
  never payment proof.

## Local webhook testing

Use Stripe test-mode keys and the Stripe CLI; never commit them:

```powershell
$env:STRIPE_SECRET_KEY='<test secret key>'
$env:STRIPE_WEBHOOK_SECRET='<CLI webhook signing secret>'
stripe listen --forward-connect-to http://localhost:8080/api/webhooks/stripe
docker compose up --build
```

Use a Stripe test connected account and test payment methods. Wallet buttons
may be absent on unsupported browsers/devices even when the server contract is
correct.

## Reconciliation and incidents

- Reconcile gross amount, application fee, refunds, currency, connected account,
  and Stripe PaymentIntent/event IDs daily.
- Investigate signature failures, persistent webhook retries, duplicate-ID
  conflicts, and ledger/provider mismatches without logging payload secrets or
  client secrets.
- Replay missing provider events from Stripe only after verifying the target
  environment and event ownership. Idempotency makes an exact replay safe.
- Do not edit ledger rows to resolve a mismatch. Record a compensating event or
  implement an explicit repair workflow with audit evidence.
- Before entering a new country, confirm Stripe Connect availability, settlement
  currency, payment methods, cross-border restrictions, fees, tax registration,
  tipping rules, refunds, and consumer-law obligations with qualified advisers.
