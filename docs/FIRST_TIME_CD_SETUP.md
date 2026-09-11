# First-time continuous delivery setup

This guide configures the external services expected by
`.github/workflows/delivery.yml`. Complete it when a Kubernetes hosting provider
has been selected. The workflow deploys workloads; it does not create the
cluster, databases, dependencies, DNS, TLS, backups, or secret store.

## Release flow

```text
Pull request -> CI -> merge -> version tag
-> build and publish image to GHCR
-> vulnerability scan
-> SPDX SBOM and signed attestations
-> staging migration
-> staging rollout and health smoke tests
-> protected production approval
-> production migration
-> production rollout and health smoke tests
```

Any failed gate stops promotion.

## 1. Commit C4 through a pull request

Create a feature branch before committing the current C4 work:

```powershell
git switch -c feat/c4-continuous-delivery

git add `
  .github/workflows/ci.yml `
  .github/workflows/delivery.yml `
  src/RestaurantMenu.Api/Program.cs `
  src/RestaurantMenu.Api/Infrastructure/DatabaseMigrationExtensions.cs `
  deploy/kubernetes `
  deploy/scripts `
  docs/adr/0026-use-digest-promoted-kubernetes-delivery.md `
  docs/runbooks `
  docs/FIRST_TIME_CD_SETUP.md `
  tests/deployment

git commit -m "feat(delivery): add hardened staging and production pipeline"
git push -u origin feat/c4-continuous-delivery
```

Open a pull request into `main`, confirm CI passes, review the diff, and merge
it. `TECHNICAL_GUIDE.md` and `V1_TO_V2_IMPLEMENTATION_BLUEPRINT.md` are private,
ignored working documents and are not part of the commit.

## 2. Prepare the hosting infrastructure

The current deployment target is Kubernetes. Provide at least:

- a Kubernetes cluster;
- separate staging and production namespaces;
- externally reachable HTTPS routes for both environments;
- PostgreSQL with encrypted backups and point-in-time recovery;
- Redis and RabbitMQ;
- S3-compatible object storage;
- Keycloak;
- an OTLP-compatible telemetry destination; and
- a runtime Secret named `restaurant-menu-runtime` in each namespace.

Prefer separate production infrastructure or accounts. Namespace separation
alone is useful but is not a complete production security or failure boundary.

The repository supplies a `ClusterIP` Service, not an Ingress, DNS record, or
TLS certificate. Add provider-specific routing before setting `BASE_URL`.

## 3. Create namespaces

After configuring `kubectl` for the selected cluster:

```powershell
kubectl create namespace restaurant-menu-staging
kubectl create namespace restaurant-menu-production
```

## 4. Provision runtime configuration

The `restaurant-menu-runtime` Secret should provide these settings as needed:

- `ConnectionStrings__Restaurants`
- `ConnectionStrings__Catalog`
- `ConnectionStrings__Media`
- `ConnectionStrings__Ordering`
- `ConnectionStrings__Feedback`
- `ConnectionStrings__Payments`
- `ConnectionStrings__Redis`
- `ConnectionStrings__RabbitMq`
- `Authentication__MetadataAddress`
- `Authentication__ValidIssuer`
- `Authentication__Audience`
- `Authentication__RequireHttpsMetadata=true`
- `KeycloakAdmin__BaseUrl`
- `KeycloakAdmin__Realm`
- `KeycloakAdmin__ClientId`
- `KeycloakAdmin__ClientSecret`
- `ObjectStorage__BucketName`
- `ObjectStorage__ServiceUrl`
- `ObjectStorage__PublicServiceUrl`
- `ObjectStorage__AccessKey`
- `ObjectStorage__SecretKey`
- `Stripe__SecretKey`
- `Stripe__WebhookSecret`
- `OTEL_EXPORTER_OTLP_ENDPOINT`
- `OTEL_EXPORTER_OTLP_PROTOCOL`

For an initial staging exercise, create the Secret from an ignored local file:

```powershell
kubectl -n restaurant-menu-staging create secret generic restaurant-menu-runtime `
  --from-env-file=<path-to-staging-secrets.env>
```

Never commit that file or put values directly in shell history. Production must
use the selected platform secret manager and encryption at rest. The deployment
workflow reads the existing Secret but does not create or print it.

Before enabling guest payments, also complete
`docs/runbooks/stripe-connect.md`. The Stripe signing secret belongs to the
webhook endpoint for the matching environment; staging and production must not
share test/live credentials.

## 5. Create protected GitHub environments

In the GitHub repository, open **Settings -> Environments** and create
`staging` and `production`. Configure required reviewers and protected release
tag rules for production where the repository plan supports them.

Add these settings to both environments:

| Type     | Name               | Purpose                                      |
| -------- | ------------------ | -------------------------------------------- |
| Secret   | `KUBE_CONFIG`      | Base64 least-privilege deployment kubeconfig |
| Variable | `KUBE_NAMESPACE`   | Existing Kubernetes namespace                |
| Variable | `BASE_URL`         | Externally routed HTTPS API origin            |
| Variable | `BACKUP_REFERENCE` | Verified pre-release recovery-point ID        |
| Variable | `ROLLBACK_IMAGE`   | Digest-pinned first-rollout fallback image    |

Use a dedicated deployment service account. Do not upload a cluster-admin
kubeconfig. Its namespace-scoped role needs only the Deployment, Service,
PodDisruptionBudget, Job, rollout observation, pod status, and migration-log
operations exercised by the workflow.

## 6. Bootstrap the first deployment

There is no previous application image before the first deployment, while the
workflow deliberately rejects missing rollback metadata.

1. Run the Delivery workflow manually after it exists on `main`.
2. Let the publish job build, scan, publish, and attest the image.
3. The staging job is expected to stop if neither a deployment nor
   `ROLLBACK_IMAGE` exists.
4. Copy the published immutable reference from the workflow or GHCR. It has the
   form `ghcr.io/<owner>/restaurant-menu@sha256:<digest>`.
5. Store that reference as `ROLLBACK_IMAGE` in both GitHub environments.
6. Create and verify a real database recovery point, then store its provider ID
   as `BACKUP_REFERENCE`.
7. Rerun the failed workflow.

For this bootstrap only, the fallback cannot restore an older application.
Failure recovery means stopping the new workload and restoring the verified
database recovery point when necessary. Later releases automatically record the
actually deployed previous digest.

## 7. Create a versioned release

After the delivery change is merged:

```powershell
git switch main
git pull --ff-only
git tag -a v2.0.0 -m "RestaurantMenu V2 C4 delivery milestone"
git push origin v2.0.0
```

The tag starts the Delivery workflow. GitHub's workflow token publishes to
GHCR. The deployment always uses the immutable image digest rather than either
mutable tag.

## 8. Observe and approve the release

In **Actions -> Delivery**, verify in order:

1. image publication, vulnerability scan, SBOM, and attestations succeed;
2. staging server-side manifest validation succeeds;
3. `staging-rollback-metadata` is uploaded;
4. the staging migration Job completes;
5. the staging rollout and `/health/live` plus `/health/ready` smoke tests pass;
6. staging behavior and telemetry are healthy;
7. an authorized reviewer approves production;
8. the same migration, rollout, and smoke gates pass in production; and
9. `production-rollback-metadata` is downloaded or retained with the release.

Do not bypass a high/critical vulnerability, migration failure, unhealthy
probe, or missing recovery reference merely to complete a release.

## 9. Rehearse recovery

After the initial deployment, run the failed-rollout rehearsal in
`docs/runbooks/deployment.md` and the isolated point-in-time restore drill in
`docs/runbooks/backup-and-restore.md`. Record the achieved RPO/RTO, recovery
point, checksum verification, smoke result, approvers, and evidence links.

Do not mark C4 operationally verified until the live registry/attestation,
cluster validation, protected promotion, failed-rollout rehearsal, and isolated
restore drill have all succeeded.
