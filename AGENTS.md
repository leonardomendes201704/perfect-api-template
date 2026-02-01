# AGENTS.md - Perfect API Template

## Architecture overview
- Clean Architecture with four layers:
  - Api (presentation)
  - Application (use cases)
  - Domain (business model)
  - Infrastructure (EF Core, external integrations)
- Dependency rules (non-negotiable):
  - Api -> Application
  - Api -> Infrastructure (ONLY via DI/extension methods; no direct data access)
  - Application -> Domain
  - Infrastructure -> Application + Domain
  - Domain -> (no references)
- CQRS with MediatR:
  - Commands mutate state
  - Queries are read-only
- Controllers are thin: controllers call ONLY MediatR.
- Result pattern via `RequestResult<T>` for handlers and controller responses.

## Responsibilities (where code must live)
- Controllers: routing, auth attributes, request/response mapping ONLY.
- Handlers: orchestration only (call Services/Repositories), no complex business rules.
- Services (Application): business rules, validations that require data, orchestration logic.
- Domain: entities/value objects/domain rules that don’t depend on infrastructure.
- Infrastructure: EF Core DbContext/mappings/repositories, external clients, observability plumbing.

## Coding standards
- Naming:
  - PascalCase for types/methods/properties
  - camelCase for locals/parameters
  - async methods end with `Async` (except MediatR Handle)
- Folders:
  - Features live under `Application/Features/<FeatureName>/Commands` and `/Queries`
  - Each command/query has its own folder: `<ActionName>/`
- API base route: `/api`.
- Use async/await for I/O and MediatR handlers.
- Keep domain entities free of infrastructure concerns.

## Result/Error model rules
- Handlers return `RequestResult<T>`.
- Validation failures use `RequestResult<T>.ValidationFailure(...)`.
- Not-found uses `RequestResult<T>.Failure("<feature>.<entity>.not_found", "...")`.
- Error code format: `<feature>.<entity>.<reason>` (e.g., `customers.customer.not_found`).
- Controllers map `RequestResult<T>` to HTTP responses using `ResultExtensions`.
- Global exceptions must be converted to ProblemDetails and include `correlationId`.

## MediatR patterns
- Commands: `record : IRequest<RequestResult<T>>` with handler in the same folder.
- Queries: `record : IRequest<RequestResult<T>>` with handler in the same folder.
- Validators live alongside commands/queries in Application (FluentValidation).
- Pipeline behaviors:
  - ValidationBehavior is mandatory
  - (Optional) Logging/Performance behaviors if present must be consistent

## How to add a new feature (recipe)
When implementing a new endpoint:
1) Create Feature folder under `Application/Features/<FeatureName>/`.
2) Add DTOs (Input/Response) close to the command/query.
3) Add Command/Query + Handler + Validator.
4) Add/Update Service methods for business rules.
5) Wire endpoint in Api controller (thin) calling MediatR only.
6) Add/Update tests:
   - Unit tests for Application/Domain behavior
   - Integration tests for the endpoint
7) Ensure Swagger is updated (summary, responses, auth if needed).

## Testing rules
- Unit tests target Application/Domain logic (no infrastructure).
- Integration tests use WebApplicationFactory and hit real HTTP endpoints.
- Tests must compile and run without external dependencies.

## PR checklist
- Build succeeds (`dotnet build`).
- Tests pass (`dotnet test`).
- No controller has business logic or DB access.
- No EF Core usage in Application.
- New features follow CQRS folder structure and `RequestResult<T>` pattern.
- Error codes follow `<feature>.<entity>.<reason>`.
- Swagger docs updated if new endpoints were added/changed.
- Health endpoint still works (`/health`) and ProblemDetails include `correlationId`.
- HistoryLog updated (`HistoryLog.md`) for every code change.
- Commit and push messages include a clear title and a complete description of what was done.

## Change log rule (MANDATORY)
Every code change must append an entry to `HistoryLog.md` at solution root with:
- Date (YYYY-MM-DD)
- Short description of the change

## Git commit & push rule (MANDATORY)
Every commit and push must include:
- A clear title (concise summary).
- A complete description of what was done (details).

## Database & migrations rules (MANDATORY)
Whenever you deliver a NEW FEATURE or FIX that changes ANY of the following:
- Domain entity persisted by EF (new/removed/renamed properties)
- EF Core configuration (Persistence/Configurations)
- DbContext (DbSet, model configuration)
- Any change that impacts the database schema

You MUST also:
1) Create a new EF Core migration in Infrastructure
2) Apply the migration to the local database (update-database)
3) Ensure the solution still builds and tests pass

Commands (SQLite):
```bash
dotnet ef migrations add <MigrationName> \
  --project src/PerfectApiTemplate.Infrastructure \
  --startup-project src/PerfectApiTemplate.Api

dotnet ef database update \
  --project src/PerfectApiTemplate.Infrastructure \
  --startup-project src/PerfectApiTemplate.Api
