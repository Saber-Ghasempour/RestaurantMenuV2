# RestaurantMenu observability runbook

## Access and first checks

The Compose reference stack exposes Grafana at `http://localhost:3000`,
Prometheus at `http://localhost:9090`, Tempo at `http://localhost:3200`, and Loki
at `http://localhost:3100`. Grafana credentials come from
`GRAFANA_ADMIN_USER`/`GRAFANA_ADMIN_PASSWORD`; the checked-in defaults are only
for local development.

Start or validate the stack from the repository root:

```powershell
docker compose config --quiet
docker compose up -d
docker compose ps
```

Open the provisioned **RestaurantMenu overview** dashboard. Begin with the alert
time range, then pivot from a log's `TraceId` to Tempo. Never paste tokens,
cookies, authorization headers, connection strings, payloads, or raw user data
into an incident ticket.

## API errors or latency

1. Confirm `/health/live` and `/health/ready`; a readiness failure routes to the
   dependency procedure below.
2. Break down HTTP rate, error rate, and p95 latency by bounded `http.route` and
   method labels. Do not add IDs to a dashboard query or metric.
3. Find error logs by trace or correlation ID and inspect the connected Npgsql,
   HttpClient, publisher, and consumer spans in Tempo.
4. Compare deployment/configuration time, database latency, runtime saturation,
   and request timeout counts. Roll back through the deployment procedure when a
   new release caused the regression.
5. Resolve the alert only after the relevant rate and latency windows recover.

## Dependency unhealthy

1. Identify the `dependency` label: one module database, `redis`, or `rabbitmq`.
2. Check `docker compose ps` and the dependency container logs without printing
   environment variables or connection strings.
3. For PostgreSQL, check reachability, connection saturation, locks, and storage.
   For Redis, check ping latency, memory, and evictions. For RabbitMQ, check node,
   connection, channel, queue, and disk alarms.
4. Restore the dependency before restarting the API. Readiness should recover
   automatically; avoid restart loops that destroy diagnostic context.

## Outbox backlog or dead letter

1. Compare `messaging_outbox_oldest_pending_age_seconds`, dead-letter source,
   RabbitMQ health, and publish failure logs.
2. Query only metadata needed for diagnosis. Treat payload and `last_error` as
   restricted because upstream exception text or business content may exist.
3. Restore RabbitMQ or fix the consumer before replay. Preserve message IDs and
   aggregate versions; never edit payloads in place.
4. Replay requires an approved operator procedure and remains manual until a
   separately authorized replay command exists. Do not clear dead-letter rows to
   make the alert green.
5. Confirm Outbox age returns below 60 seconds, consumer effects remain
   idempotent, and no new dead letters appear before resolving.

## Developer-owned failure drill

With no important local work using the Compose stack, create a normal order so
an Outbox message exists, then run:

```powershell
docker compose stop rabbitmq
# Create another order through the API and confirm readiness/broker and Outbox-age alerts.
docker compose start rabbitmq
# Confirm the same trace continues HTTP -> publish -> notification consumer.
```

Also submit a request whose URL contains a disposable public-menu code and a
dining-session token. Search Loki for each raw value; both searches must return
zero results. Confirm the audit event contains `AuditType`, `Method`, `Route`,
`Outcome`, `ActorHash`, `CorrelationId`, `TraceId`, `SpanId`, and no body or query.
