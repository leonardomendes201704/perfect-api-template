# Perfect API Template

Production-ready .NET 9 API starter using Clean Architecture + CQRS (MediatR), FluentValidation, EF Core (SQLite), Serilog, Swagger, and ProblemDetails.

## How to run
```bash
# from repo root
 dotnet build
 dotnet run --project src/PerfectApiTemplate.Api
```

The API will be available with Swagger at `/swagger` and health at `/health`.

## Databases (SQLite)
This template uses two databases:
- MainDb: application data (`app.db`)
- LogsDb: request/error/transaction logs (`logs.db`)

### Migrations
```bash
# install EF tools if needed
 dotnet tool install --global dotnet-ef

# add migration (MainDb)
 dotnet ef migrations add InitialCreate --project src/PerfectApiTemplate.Infrastructure --startup-project src/PerfectApiTemplate.Api --output-dir Persistence/Migrations/Main

# add migration (LogsDb)
 dotnet ef migrations add AddAuditLoggingTables --project src/PerfectApiTemplate.Infrastructure --startup-project src/PerfectApiTemplate.Api --context LogsDbContext --output-dir Persistence/Migrations/Logs

# apply migration (MainDb)
 dotnet ef database update --project src/PerfectApiTemplate.Infrastructure --startup-project src/PerfectApiTemplate.Api

# apply migration (LogsDb)
 dotnet ef database update --project src/PerfectApiTemplate.Infrastructure --startup-project src/PerfectApiTemplate.Api --context LogsDbContext
```

## Add a new feature
1. Create a feature folder in `src/PerfectApiTemplate.Application/Features/<FeatureName>`.
2. Add `Commands` and `Queries` with MediatR handlers and validators.
3. Add API controller endpoints that call ONLY MediatR.
4. Add mappings in Infrastructure if new entities are introduced.
5. Add unit + integration tests.

## Outbox and notifications
This template includes a minimal Outbox implementation:
- `OutboxMessage` entity stored in the database.
- `OutboxProcessor` background service reads pending messages and publishes via `INotificationPublisher`.
- Default publisher is `NoOpNotificationPublisher` (logs only). Replace with a real publisher.

To enqueue a message from Application, inject `IOutboxEnqueuer` and call `EnqueueAsync(type, payload)`.

Configuration:
```json
"Outbox": { "BatchSize": 20, "PollingSeconds": 5 }
```

## Logging/auditing (separate LogsDb)
This template writes observability data to a separate database (`LogsDb`):
- `RequestLogs`: request/response metadata + sanitized/truncated bodies.
- `ErrorLogs`: unhandled exceptions with request context.
- `TransactionLogs`: EF Core inserts/updates/deletes with before/after snapshots (masked).

Behavior:
- Sensitive headers and JSON keys/paths are masked.
- Bodies are captured only for safe content types (json/text) and truncated to a max size.
- Sampling applies to normal requests; 500+ responses always logged.
- Background queues persist logs; drops are recorded in Serilog.
- Retention worker deletes old rows based on configured days.

Exclusions:
- Default excluded paths: `/swagger`, `/health`.
- Default excluded content types: `multipart/form-data`, `application/octet-stream`.

Enable/disable:
- `Logging:Requests:Enabled`
- `Logging:Errors:Enabled`
- `Logging:Transactions:Enabled`

## Email (SMTP + IMAP/POP3)
Email sending and inbox reading are supported with background processors:
- `EmailOutboxProcessor` sends queued emails via SMTP.
- `EmailInboxProcessor` reads new messages via IMAP/POP3 and stores them for tracking.

Endpoints:
- `POST /api/emails` to enqueue a message.
- `GET /api/emails` to list sent/queued messages.
- `GET /api/emails/{id}` to read a message status.
- `GET /api/emails/inbox` to list inbox messages.
- `POST /api/emails/inbox/sync` to trigger a manual inbox sync.

## Key conventions
- Controllers are thin and call MediatR only.
- Commands mutate; queries read-only.
- Handlers return `RequestResult<T>`.
- API base route is `/api`.

## Pagination guidance
Prefer **keyset/seek pagination** for large datasets to avoid performance issues with LIMIT/OFFSET.

Example (recommended):
```
GET /api/customers?orderBy=CreatedAtUtc&orderDir=desc&pageSize=50&cursor=2026-02-01T10:00:00Z|b3f1c1e6-...
```

Notes:
- Cursor should include the last item’s ordered fields (e.g., `CreatedAtUtc|Id`) to keep ordering stable.
- Only use OFFSET pagination when you truly need page numbers; otherwise, default to keyset.

## ENVs
Keep this section in sync with `appsettings.json` and `appsettings.Development.json`.

```
ConnectionStrings:MainDb             -> SQLite connection string (e.g., Data Source=app.db)
ConnectionStrings:LogsDb             -> Logs SQLite connection string (e.g., Data Source=logs.db)
Jwt:Issuer                           -> JWT issuer
Jwt:Audience                         -> JWT audience
Jwt:SigningKey                       -> JWT signing key
Outbox:BatchSize                     -> Outbox processor batch size
Outbox:PollingSeconds                -> Outbox polling interval in seconds
Logging:Requests:Enabled              -> Enable request logging
Logging:Requests:SamplingPercent      -> Request sampling percentage
Logging:Requests:MaxBodyBytes         -> Max request/response body bytes to capture
Logging:Requests:ExcludedPaths        -> Paths excluded from request logging
Logging:Requests:ExcludedContentTypes -> Content types excluded from body capture
Logging:Errors:Enabled                -> Enable error logging
Logging:Errors:MaxBodyBytes           -> Max request body bytes for error logging
Logging:Transactions:Enabled          -> Enable transaction logging
Logging:Transactions:ExcludedEntities -> Entities excluded from transaction logging
Logging:Transactions:ExcludedProperties -> Properties excluded from snapshots
Logging:Mask:Enabled                  -> Enable masking
Logging:Mask:HeaderDenyList           -> Headers always masked
Logging:Mask:HeaderAllowList          -> Headers allowlist (optional)
Logging:Mask:JsonKeys                 -> JSON keys to mask
Logging:Mask:JsonPaths                -> JSON paths to mask
Logging:Retention:Enabled             -> Enable retention cleanup
Logging:Retention:RunIntervalMinutes  -> Retention run interval
Logging:RetentionDays:Requests        -> Request log retention days
Logging:RetentionDays:Errors          -> Error log retention days
Logging:RetentionDays:Transactions    -> Transaction log retention days
Logging:Enrichment:TenantClaim        -> Claim used for tenant id
Logging:Enrichment:UserIdClaim        -> Claim used for user id
Logging:Enrichment:TenantHeader       -> Header used for tenant id
Logging:Queue:Capacity                -> In-memory log queue capacity
Logging:RequestIdHeader               -> Request id header name
Email:Smtp:Host                       -> SMTP host
Email:Smtp:Port                       -> SMTP port
Email:Smtp:UseSsl                     -> SMTP SSL toggle
Email:Smtp:Username                   -> SMTP username
Email:Smtp:Password                   -> SMTP password
Email:Smtp:DefaultFrom                -> Default sender address
Email:Inbox:Protocol                  -> Imap or Pop3
Email:Inbox:Host                      -> IMAP/POP3 host
Email:Inbox:Port                      -> IMAP/POP3 port
Email:Inbox:UseSsl                    -> IMAP/POP3 SSL toggle
Email:Inbox:Username                  -> IMAP/POP3 username
Email:Inbox:Password                  -> IMAP/POP3 password
Email:Inbox:MaxMessagesPerPoll        -> Inbox max messages per polling cycle
Email:Processing:OutboxPollingSeconds -> Email outbox polling interval in seconds
Email:Processing:InboxPollingSeconds  -> Email inbox polling interval in seconds
Email:Processing:BatchSize            -> Email batch size
Email:Processing:MaxAttempts          -> Email send max attempts
ExternalAuth:Providers:Google:UserInfoUrl    -> Google user info endpoint
ExternalAuth:Providers:Facebook:UserInfoUrl  -> Facebook user info endpoint
ExternalAuth:Providers:LinkedIn:UserInfoUrl  -> LinkedIn user info endpoint
ExternalAuth:Providers:Microsoft:UserInfoUrl -> Microsoft Graph user info endpoint
AdminUser:Email                    -> Admin seed email
AdminUser:Password                 -> Admin seed password
AdminUser:FullName                 -> Admin display name
```

## Use this repo as a template
This repository is marked as a GitHub template. To create a new API:
1. Click the **Use this template** button on GitHub.
2. Create a new repository from the template.
3. Clone the new repo and start coding.

## Common commands
```bash
 dotnet build
 dotnet test
 dotnet run --project src/PerfectApiTemplate.Api
```

