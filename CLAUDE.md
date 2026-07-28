# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A .NET 10 controller-based Web API (`AiGateway`). Single-project solution, no separate class library or test project yet.

## Commands

- Build: `dotnet build`
- Run (http profile, port 5207): `dotnet run --launch-profile http`
- Run (https profile, ports 7161/5207): `dotnet run --launch-profile https`
- Restore packages: `dotnet restore`
- Add an EF Core migration (after changing an entity in `Data/Entities/`): `dotnet ef migrations add <Name> -o Data/Migrations`

There is no test project yet — `dotnet test` has nothing to run.

## Docker

- The app is also runnable as a container: `Dockerfile` (multi-stage `sdk:10.0` build → `aspnet:10.0` runtime, listening on port 8080 internally via `ASPNETCORE_HTTP_PORTS`) and `docker-compose.yml` at the project root, which publishes that same port 8080 on the host by default (`HOST_PORT` in `.env` overrides it).
- Copy `.env.example` to `.env` and fill in real values before running `docker compose up --build` — in particular `AUTH_MASTER_API_KEY` (there's no safe default; the app fails fast at startup without `Auth:MasterApiKey`). `docker-compose.yml` maps the `.env` variables to ASP.NET Core config keys via the double-underscore convention (e.g. `AUTH_MASTER_API_KEY` → `Auth__MasterApiKey`).
- `App_dbs/` and `App_files/` are bind-mounted into the container at the same paths used by local `dotnet run`, so the SQLite databases and knowledge-base uploads persist on the host across container rebuilds — never baked into the image.
- `OLLAMA_BASE_URL`/`SPEACHES_BASE_URL` default to `http://host.docker.internal:11434`/`:8000` (reaching services running on the Docker host itself); `docker-compose.yml` adds the Linux-only `extra_hosts: host.docker.internal:host-gateway` entry so this also works outside Docker Desktop.
- `.dockerignore` excludes `bin/`, `obj/`, `App_dbs/`, `App_files/`, and `.env` from the build context — keep it in sync with `.gitignore` if either changes.

## Architecture

- `Program.cs` uses top-level statements to build the `WebApplication`, register services (`AddControllers`), and wire up the pipeline (no separate `Startup` class). It does not map any endpoints itself — `app.MapControllers()` routes to attribute-routed controllers.
- Endpoints live in `Controllers/` as one `[ApiController]` class per resource (e.g. `Controllers/HealthController.cs` for `GET /health`), following standard ASP.NET Core MVC controller conventions.
- Swagger is provided by `Swashbuckle.AspNetCore` (`AddSwaggerGen` / `UseSwaggerUI`), not the `Microsoft.AspNetCore.OpenApi` package the default `dotnet new webapi` template uses — that package was deliberately swapped out because it doesn't ship a Swagger UI page. Keep using Swashbuckle for any OpenAPI/Swagger changes.
- Swagger UI and the raw OpenAPI doc are wired up unconditionally (`UseSwagger()` / `UseSwaggerUI()` at `http://localhost:5207/swagger`) — deliberately available in every environment, not just Development, so it can be used against deployed instances too. To make that safe, `UseSwaggerUI`'s `HeadContent` injects a small fixed-position badge (bottom-right corner) showing `ASPNETCORE_ENVIRONMENT` — orange for `Development`, green for `Production`, neutral gray-blue for anything else (e.g. a future `Staging`) — so it's obvious at a glance which environment you're pointed at. Keep this badge logic in sync if the environment-gating ever changes.
- `AiGateway.http` is a manual REST Client scratch file for ad-hoc requests, including examples for `/health` and every `/keys` action — update it when adding/removing endpoints if you rely on it.
- `HealthController` calls `IHealthService.CheckSelf()` (see Service convention below) directly and maps the result to the DTOs, returning 503 when `IsHealthy` is false. The project deliberately does not use ASP.NET Core's built-in `Microsoft.Extensions.Diagnostics.HealthChecks` framework (`AddHealthChecks`/`IHealthCheck`/`HealthCheckService`) — that was tried and removed in favor of controllers calling services directly, consistent with the Service convention. Don't reintroduce it without checking with the user first. `HealthController` has no `[Authorize]` — it's intentionally reachable without an API key, for load balancers/uptime monitors.
- All SQLite databases live under `App_dbs/` at the project root (created automatically at startup via `Directory.CreateDirectory` in `Program.cs` if missing). Currently holds `AiGateway.db` (used by `Data/AppDbContext.cs` for API keys and knowledge-base metadata), `AiGatewayVectors.db` (the knowledge base's `vec_chunks` embeddings — a `sqlite-vec` virtual table, kept in its own file so heavy vector traffic can't slow down `AiGateway.db`; attached under the `vectors` schema on every connection by `Data/SqliteVecConnectionInterceptor.cs`, see `Service/KnowledgeBase/Docs/KnowledgeBaseService.md` for the full picture), and `AiGatewayLogs.db` (Serilog's log store, see Logging convention below).
- Original knowledge-base document uploads (as opposed to their extracted text/embeddings, which live in the databases above) are stored as plain files, not in a database, under `App_files/<GroupId>/<DocumentId>` at the project root (also created via `Directory.CreateDirectory` in `Program.cs`). The on-disk filename is the document's GUID, never the caller-supplied original filename, so there's no path-sanitization or collision concern — see `Service/KnowledgeBase/Docs/KnowledgeBaseService.md` for the full picture, including why cleanup on delete is deliberately best-effort (logged, not thrown) rather than failing the request.

## Logging convention

- Logging is `Serilog` (`Serilog.AspNetCore`), not the default `Microsoft.Extensions.Logging` console formatter — wired up in `Program.cs` via `builder.Host.UseSerilog(...)`, with a `CreateBootstrapLogger()` console-only logger active before the host is built so startup failures are still logged. `Log.CloseAndFlush()` runs in a `finally` block wrapping the whole top-level `Program.cs`.
- Two sinks are always active: `Console` (for local/dev tailing and container log collection) and `SQLite` (`Serilog.Sinks.SQLite`, writing to `App_dbs/AiGatewayLogs.db`, table `Logs`). `ILogger<T>` injected anywhere in the app routes through both automatically — controllers/services never need Serilog-specific APIs.
- The SQLite sink is called with `rollOver: false` deliberately — there is exactly one log database file, never rotated into `AiGatewayLogs1.db`/`AiGatewayLogs2.db` etc. Don't set `maxDatabaseSize` or flip `rollOver` to `true` without checking with the user first; if unbounded growth becomes a problem, prefer `retentionPeriod` (SQLite-side pruning of old rows) over rollover.
- `app.UseSerilogRequestLogging()` in `Program.cs` logs one line per HTTP request. It's suppressed at `Information` level in Development because `appsettings.Development.json`'s `Serilog:MinimumLevel:Override` sets `Microsoft.AspNetCore` to `Warning` (mirroring the old `Logging` config it replaced) — raise that override to see per-request lines locally.
- Log level configuration lives under a `Serilog` section in `appsettings.json`/`appsettings.Development.json` (`MinimumLevel:Default` / `MinimumLevel:Override`), read via `ReadFrom.Configuration(...)` — this replaced the old `Logging:LogLevel` section, which Serilog does not read.
- `Controllers/LogsController.cs` exposes `GET /Logs/Search` (administrator-only, like `KeysController`) for searching/filtering the log entries in `AiGatewayLogs.db` — query params: `Level`, `Search` (substring match against message/exception), `From`/`To` (timestamp range), `Page`/`PageSize`. Business logic lives in `Service/Logs/LogService.cs`, per the Service convention, but it deliberately reads the `Logs` table with raw `Microsoft.Data.Sqlite` calls instead of EF Core/`AppDbContext` — that table's schema (`id`, `Timestamp`, `Level`, `Exception`, `RenderedMessage`, `Properties`) is owned and created by `Serilog.Sinks.SQLite` itself, not by this app, so there is no EF entity or migration for it. `LogService`'s connection string is built once in `Program.cs` (the same `logDbPath` passed to `UseSerilog`) and injected via a DI factory, so the log-search path and the Serilog sink can never drift onto different files.

## Service convention

- Business/domain logic lives in `Service/<ServiceName>/` at the project root — one folder per service (e.g. `Service/Health/` for the health self-check logic), separate from the `Controllers/` and `Dto/` folders.
- Inside each service folder:
  - The service implementation class sits directly in the service's root folder (e.g. `Service/Health/HealthService.cs`).
  - `Interfaces/` holds the service's interface(s) (e.g. `Service/Health/Interfaces/IHealthService.cs`). Controllers, health checks, and other consumers depend on the interface, not the concrete class.
  - `Dtos/` holds DTOs used at the service's own boundary (inputs/outputs of its methods, e.g. `Service/Health/Dtos/SelfCheckResultDto.cs`) — distinct from the controller-facing DTOs in `Dto/<ControllerName>/`. Don't reuse controller DTOs inside a service or vice versa.
  - `Docs/` holds a short Markdown file per service (e.g. `Service/Health/Docs/HealthService.md`) describing its purpose, consumers, and how to extend it — written so an AI model can quickly understand the service without reading all the code.
- Namespace follows the folder: `AiGateway.Service.<ServiceName>`, `AiGateway.Service.<ServiceName>.Interfaces`, `AiGateway.Service.<ServiceName>.Dtos`.
- Services are registered in `Program.cs` against their interface (e.g. `AddSingleton<IHealthService, HealthService>()`); consumers (controllers, `IHealthCheck` implementations, other services) take the interface via constructor injection.

## DTO convention

- Every controller action that returns data must declare its response shape with a dedicated `*ResponseDto` class, referenced via `[ProducesResponseType(typeof(XResponseDto), StatusCodes.StatusXxx)]` on the action — never return anonymous objects or bare types.
- Every action that accepts input (route params beyond simple ids, query params, or a body) must accept a dedicated `*RequestDto` class rather than loose primitive parameters. Actions with no input (e.g. a parameterless health check) do not need a placeholder RequestDto.
- DTOs live under `Dto/<ControllerName>/` at the project root — one folder per controller, containing only that controller's request/response DTOs (e.g. `Dto/Health/HealthResponseDto.cs` for `HealthController`). Namespace follows the folder: `AiGateway.Dto.<ControllerName>`.
- Multiple actions on the same controller each get their own DTOs unless they genuinely share the exact same shape — don't force-fit unrelated actions onto one DTO.

## Authentication & API key management

- All authentication is a single custom `X-Api-Key` header, handled by `Authentication/ApiKeyAuthenticationHandler.cs` (scheme name `"ApiKey"`, registered as the default scheme in `Program.cs` via `AddAuthentication().AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>()`). This is deliberately **not** ASP.NET Core Identity (`Microsoft.AspNetCore.Identity`, `UserManager`, `PasswordHasher`, etc.) — that framework is built around username/password login, which this project has no use for. Don't reintroduce it without checking with the user first.
- There is exactly one administrator credential: the master key in configuration (`Auth:MasterApiKey` in `appsettings.json`). It is checked with a constant-time comparison (`CryptographicOperations.FixedTimeEquals`) directly against the request header — it has no row in the database, can't be created/edited/rotated/listed through the API, and must be changed by hand in `appsettings.json`. It is the only credential with the `Administrator` role claim; every other authenticated request gets the `Standard` role.
- All other keys are managed via `Controllers/KeysController.cs` (`/keys/*`, gated with `[Authorize(Roles = ApiKeyAuthenticationHandler.AdministratorRole)]` — i.e. master-key only). Business logic lives in `Service/Keys/KeyService.cs`, per the Service convention, storing rows in `Data/Entities/ApiKey.cs` via `Data/AppDbContext.cs` (SQLite, see `App_dbs/` above).
- Each key carries who it was issued to: `Name`, `ResponsibleName`, `ContactInfo`, plus an optional `ExpiresAt` and an `IsActive` flag (deactivate/activate instead of delete — keys are never hard-deleted, so history and the audit log survive).
- The key secret itself is a random 256-bit value prefixed `agw_` (recognizable like GitHub's `ghp_`/Stripe's `sk_`). Only a SHA-256 hash (`Data/ApiKeyHasher.cs`) is persisted — the plaintext is shown exactly once, in the response of `POST /keys` (create) or `POST /keys/{id}/rollover` (rotate), and can never be retrieved again. `ApiKeyHasher` is shared between `KeyService` (hashing on write) and `ApiKeyAuthenticationHandler` (hashing on lookup) specifically so the two can't drift apart.
- Every create/update/rollover/activate/deactivate is recorded in `Data/Entities/ApiKeyAuditLog.cs`, readable via `GET /keys/{id}/audit-log`. Note `KeyService.GetAuditLogAsync` sorts these client-side after materializing — SQLite's EF Core provider can't translate `ORDER BY` over a `DateTimeOffset` column.
- `OllamaController`/`SpeachesController` require any authenticated key (`[Authorize]`, no role restriction) since both `Standard` and `Administrator` keys may call the gateway proxy endpoints.
- Schema changes to `ApiKey`/`ApiKeyAuditLog` need a new EF Core migration: `dotnet ef migrations add <Name> -o Data/Migrations`. Migrations are applied automatically at startup (`Database.Migrate()` in `Program.cs`) — no manual `dotnet ef database update` step needed when running the app.

## Error handling convention

- All errors (unhandled exceptions, model-validation failures, downstream HTTP failures from `Service/Ollama` and `Service/Speaches`) are rendered as the single `AiGateway.Dto.Errors.ErrorResponseDto` shape (`Dto/Errors/ErrorResponseDto.cs`) — controllers and services never build their own error payloads.
- `Middleware/GlobalExceptionHandler.cs` implements ASP.NET Core's `IExceptionHandler`, registered via `AddExceptionHandler<GlobalExceptionHandler>()` + `AddProblemDetails()` (the latter is only there to satisfy `UseExceptionHandler()`'s startup check — the custom handler always handles the exception itself, so `ProblemDetailsService` is never actually invoked) and wired in with `app.UseExceptionHandler()` in `Program.cs`. `HttpRequestException` thrown by `OllamaService`/`SpeachesService` carries the upstream's real status code (see their `EnsureSuccessAsync` helpers), which the handler forwards as-is; connection-level failures fall back to 502, timeouts to 504, `Service.Keys.ApiKeyNotFoundException` to 404, anything else to 500.
- `[ApiController]`'s automatic model-validation `400` is routed through the same DTO via `ApiBehaviorOptions.InvalidModelStateResponseFactory` in `Program.cs`, rather than the framework's default `ValidationProblemDetails`.
- `401`/`403` are the one exception to "GlobalExceptionHandler renders every error" — they never throw as exceptions, so `Authentication/ApiKeyAuthenticationHandler.cs` builds the same `ErrorResponseDto` shape itself in `HandleChallengeAsync`/`HandleForbiddenAsync`, keeping the wire format identical either way.
- Controllers document error responses with `[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.StatusXxx)]` — `502`/`500` are applied at the controller class level on `OllamaController`/`SpeachesController` (every action proxies to a downstream service), alongside `401` (both now require `[Authorize]`); `400` on individual actions that bind a request DTO; `404` on the Speaches model-by-id actions and the `KeysController` id-based actions; `401`/`403`/`500` at the class level on `KeysController` (administrator-only). `HealthController` only declares `500`, since its `200`/`503` responses use `HealthResponseDto`, not the error DTO, and it requires no API key at all.
