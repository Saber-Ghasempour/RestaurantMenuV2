# RestaurantMenu backup and restore runbook

## Policy

Production PostgreSQL must use provider-managed encrypted backups with
point-in-time recovery. Retain daily recovery points for 35 days and monthly
copies for 13 months in a separate account or immutable vault. Encrypt backups
in transit and at rest with a restricted key, deny application identities
delete access, and alert on backup age or failed copy/verification jobs.

The database owner records the provider recovery-point identifier as the
GitHub environment variable `BACKUP_REFERENCE` before each release. A delivery
with a missing reference fails before the migration job. The reference is
operational metadata, not a credential.

Nightly verification must check backup completion, object size, provider
checksum, encryption/key state, retention lock, and restore-point age. Never
consider an untested backup successful merely because a scheduled job exited
zero.

## Restore drill

Run a restore drill at least quarterly and before a material storage or schema
change:

1. Select a recovery point inside the target RPO and record its
   `BACKUP_REFERENCE`, checksum, source database, and UTC timestamp.
2. Restore into a new isolated database instance with no production traffic and
   outbound access restricted.
3. Use fresh least-privilege credentials and point a disposable API namespace at
   the restored instance. Do not overwrite the existing staging or production
   database.
4. Run all EF pending-model checks, `/health/live`, `/health/ready`, and bounded
   read-only business probes. Reconcile representative table counts and the
   latest expected audit/order timestamps without exporting personal data.
5. Record achieved RPO/RTO, checksum verification, schema versions, smoke-test
   result, operator, approver, and evidence links. Securely destroy the restored
   instance after evidence review.

## Incident restore

Declare an incident and freeze writes before replacing production data. Preserve
the damaged database and logs. The incident commander, database owner, and
security owner approve the exact recovery point and data-loss window. Restore
to a new instance, validate it as in the drill, switch credentials through the
secret store, roll the API, and run smoke tests. Keep the old instance isolated
until reconciliation and formal closure; then destroy it according to policy.

Schema reversal is a restore or roll forward decision. Do not run ad-hoc `Down`
migrations against production and do not reuse a backup from a different tenant
or environment.
