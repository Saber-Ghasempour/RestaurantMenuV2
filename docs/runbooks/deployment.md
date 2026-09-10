# RestaurantMenu deployment runbook

## Release contract

The Delivery workflow publishes the API to GHCR with both the release name and
commit SHA, but every deployment uses the immutable `sha256` digest. The image
must pass the high/critical vulnerability gate and receive signed GitHub build
provenance and an SPDX SBOM attestation before staging can start.

Create protected GitHub `staging` and `production` environments. Require an
approval for production and restrict which release tags can deploy. Each
environment provides:

- secret `KUBE_CONFIG`: base64-encoded, least-privilege cluster credentials;
- variable `KUBE_NAMESPACE`: an existing namespace;
- variable `BASE_URL`: the externally routed API origin;
- variable `BACKUP_REFERENCE`: the verified restore point captured immediately
  before the release;
- variable `ROLLBACK_IMAGE`: a digest-pinned known-good image used only for the
  first managed rollout.

The `restaurant-menu-runtime` Kubernetes Secret is provisioned by the platform
secret store, not GitHub Actions or this repository. It contains connection
strings, JWT/Keycloak configuration, RabbitMQ, Redis, object-storage, and OTLP
credentials. Rotate it in the source secret store and verify a controlled pod
rollout; never print, commit, or place secret values in deployment metadata.

## Normal rollout

1. Confirm CI is green and create an annotated semantic version tag.
2. Confirm the vulnerability scan, SBOM, and provenance attestations succeed.
3. Confirm the workflow's server-side manifest validation and rollback metadata
   artifact succeed. Missing previous-image, migration, or backup data blocks
   the migration.
4. The one-shot migration job runs with `--migrate`; API pods never migrate on
   startup. A failed migration stops the rollout.
5. Kubernetes performs a rolling deployment with zero unavailable replicas.
   Both rollout status and liveness/readiness smoke tests must pass in staging.
6. Review staging telemetry, then grant the protected production approval. The
   exact staging digest follows the same migration, rollout, and smoke gates.

## Failed rollout and application rollback

Stop promotion when migration, rollout, readiness, or smoke testing fails.
Download the environment's `rollback-metadata.json` artifact and verify its
environment, target digest, previous digest, migration release, and backup
reference against the workflow run.

If schema changes are backward compatible, restore the previous digest:

```powershell
kubectl -n <namespace> set image deployment/restaurant-menu-api api=<previousImage>
kubectl -n <namespace> rollout status deployment/restaurant-menu-api --timeout=5m
./deploy/scripts/smoke-test.ps1 -BaseUri https://api.example.com
```

If the previous application is not compatible with the migrated schema, do not
force an application rollback. Keep traffic on the safe version or maintenance
route and roll forward with a corrective migration and image.

## Database rollback and failed migration

EF migration `Down` methods are development aids, not the default production
rollback. Prefer expand/contract changes so the previous application remains
compatible during the deployment window. For a failed migration, preserve job
logs and database evidence, stop API promotion, and decide between an audited
corrective roll forward and the restore procedure. A database restore is an
incident action requiring the backup owner and incident commander approval.

## Rehearsal

At least quarterly in a disposable staging namespace, deploy an intentionally
unhealthy image or configuration, verify rollout/smoke failure prevents
production promotion, and restore the prior digest from rollback metadata.
Record timings, alerts, evidence links, and follow-up actions in the drill log.
