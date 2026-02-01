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

## Key conventions
- Controllers are thin and call MediatR only.
- Commands mutate; queries read-only.
- Handlers return `RequestResult<T>`.
- API base route is `/api`.

## Common commands
```bash
 dotnet build
 dotnet test
 dotnet run --project src/PerfectApiTemplate.Api
```
