# Perfect API Template

Production-ready .NET 9 API starter using Clean Architecture + CQRS (MediatR), FluentValidation, EF Core (SQLite), Serilog, Swagger, and ProblemDetails.

## How to run
```bash
# from repo root
 dotnet build
 dotnet run --project src/PerfectApiTemplate.Api
```

The API will be available with Swagger at `/swagger` and health at `/health`.

## Database (SQLite)
Default connection string is `Data Source=app.db` in `appsettings.json`.

### Migrations
```bash
# install EF tools if needed
 dotnet tool install --global dotnet-ef

# add migration
 dotnet ef migrations add InitialCreate --project src/PerfectApiTemplate.Infrastructure --startup-project src/PerfectApiTemplate.Api

# apply migration
 dotnet ef database update --project src/PerfectApiTemplate.Infrastructure --startup-project src/PerfectApiTemplate.Api
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

## Key conventions
- Controllers are thin and call MediatR only.
- Commands mutate; queries read-only.
- Handlers return `RequestResult<T>`.
- API base route is `/api`.

## ENVs
Keep this section in sync with `appsettings.json` and `appsettings.Development.json`.

```
ConnectionStrings:DefaultConnection  -> SQLite connection string (e.g., Data Source=app.db)
Jwt:Issuer                           -> JWT issuer
Jwt:Audience                         -> JWT audience
Jwt:SigningKey                       -> JWT signing key
Outbox:BatchSize                     -> Outbox processor batch size
Outbox:PollingSeconds                -> Outbox polling interval in seconds
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

