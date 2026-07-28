using AiGateway.Authentication;
using AiGateway.Data;
using AiGateway.Dto.Errors;
using AiGateway.Middleware;
using AiGateway.Service.Health;
using AiGateway.Service.Health.Interfaces;
using AiGateway.Service.KnowledgeBase;
using AiGateway.Service.KnowledgeBase.Interfaces;
using AiGateway.Service.Keys;
using AiGateway.Service.Keys.Interfaces;
using AiGateway.Service.Logs;
using AiGateway.Service.Logs.Interfaces;
using AiGateway.Service.Ollama;
using AiGateway.Service.Ollama.Interfaces;
using AiGateway.Service.Speaches;
using AiGateway.Service.Speaches.Interfaces;
using AiGateway.Service.TextExtraction;
using AiGateway.Service.TextExtraction.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Serilog;

// Bootstrap logger so failures during startup (before configuration/DI is available) still reach the console.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // All databases for this app live under App_dbs/ at the project root.
    var dbDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_dbs");
    Directory.CreateDirectory(dbDirectory);
    var dbPath = Path.Combine(dbDirectory, "AiGateway.db");
    var logDbPath = Path.Combine(dbDirectory, "AiGatewayLogs.db");
    // Kept separate from AiGateway.db so heavy vector read/write traffic (knowledge base
    // embeddings) can't slow down API-key/knowledge-metadata queries against the main database.
    // Attached under the "vectors" schema on every connection by SqliteVecConnectionInterceptor.
    var vectorDbPath = Path.Combine(dbDirectory, "AiGatewayVectors.db");

    // Original knowledge base uploads (as opposed to their extracted text/embeddings, which live in
    // the databases above) are stored as plain files under App_files/<GroupId>/<DocumentId>, next to
    // App_dbs/ at the project root — kept off the database entirely so large uploads don't bloat it.
    var filesRootPath = Path.Combine(builder.Environment.ContentRootPath, "App_files");
    Directory.CreateDirectory(filesRootPath);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        // rollOver: false — a single, never-rotated log database file, consistent with the rest of App_dbs/.
        .WriteTo.SQLite(logDbPath, storeTimestampInUtc: true, rollOver: false));

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Route the [ApiController] automatic model-validation response through the same
    // ErrorResponseDto shape that GlobalExceptionHandler uses for every other error.
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            var response = new ErrorResponseDto
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred",
                TraceId = context.HttpContext.TraceIdentifier,
                Errors = errors
            };

            return new BadRequestObjectResult(response);
        };
    });
    builder.Services.AddSwaggerGen(options =>
    {
        // Use the full type name (not just the class name) as the schema id, since different
        // services' DTO folders can otherwise collide on identical class names (e.g. a
        // "ListModelsResponseDto" in both Dto/Ollama and Dto/Speaches). The Schemas section is
        // hidden in the UI anyway (see DefaultModelsExpandDepth below), so this is invisible to users.
        options.CustomSchemaIds(type => type.FullName);

        // Adds Swagger UI's "Authorize" button so protected endpoints can be tried out with an X-Api-Key value.
        options.AddSecurityDefinition(ApiKeyAuthenticationHandler.SchemeName, new OpenApiSecurityScheme
        {
            Name = ApiKeyAuthenticationHandler.HeaderName,
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "API-nøgle sendt via X-Api-Key-headeren."
        });
        options.AddSecurityRequirement(document =>
        {
            var requirement = new OpenApiSecurityRequirement();
            var schemeReference = new OpenApiSecuritySchemeReference(ApiKeyAuthenticationHandler.SchemeName, document);
            requirement.Add(schemeReference, []);
            return requirement;
        });
    });

    // Fail fast if the master API key isn't configured, mirroring the Ollama/Speaches BaseUrl checks below.
    _ = builder.Configuration["Auth:MasterApiKey"]
        ?? throw new InvalidOperationException("Configuration value 'Auth:MasterApiKey' is missing.");

    // Fail fast if the Knowledge Base configuration section isn't fully populated.
    foreach (var key in new[]
             {
                 "EmbeddingModel", "EmbeddingDimensions", "ChunkSizeCharacters", "ChunkOverlapCharacters", "MaxUploadSizeBytes"
             })
    {
        _ = builder.Configuration[$"KnowledgeBase:{key}"]
            ?? throw new InvalidOperationException($"Configuration value 'KnowledgeBase:{key}' is missing.");
    }

    builder.Services.Configure<KnowledgeBaseOptions>(builder.Configuration.GetSection("KnowledgeBase"));

    builder.Services.AddDbContext<AppDbContext>(options => options
        .UseSqlite($"Data Source={dbPath}")
        .AddInterceptors(new SqliteVecConnectionInterceptor(vectorDbPath)));

    builder.Services
        .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, options => { });
    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<IHealthService, HealthService>();
    builder.Services.AddScoped<IKeyService, KeyService>();
    builder.Services.AddSingleton<ITextExtractionService, TextExtractionService>();
    // Reuses filesRootPath (the same path the download/delete paths below resolve documents against)
    // so the upload and download/delete code paths can never point at different folders — same
    // rationale as LogService reusing logDbPath below.
    builder.Services.AddScoped<IKnowledgeBaseService>(serviceProvider => new KnowledgeBaseService(
        serviceProvider.GetRequiredService<AppDbContext>(),
        serviceProvider.GetRequiredService<IOllamaService>(),
        serviceProvider.GetRequiredService<ITextExtractionService>(),
        serviceProvider.GetRequiredService<IOptions<KnowledgeBaseOptions>>(),
        filesRootPath,
        serviceProvider.GetRequiredService<ILogger<KnowledgeBaseService>>()));
    // Reuses logDbPath (the same file Serilog's SQLite sink writes to, see UseSerilog above) so the
    // two can never point at different databases.
    builder.Services.AddSingleton<ILogService>(_ => new LogService($"Data Source={logDbPath}"));
    builder.Services.AddHttpClient<IOllamaService, OllamaService>(client =>
    {
        var baseUrl = builder.Configuration["Ollama:BaseUrl"]
            ?? throw new InvalidOperationException("Configuration value 'Ollama:BaseUrl' is missing.");
        client.BaseAddress = new Uri(baseUrl);
        // Default HttpClient.Timeout (100s) is too short for chat/embed generation on slower
        // models or cold model loads, causing RagChat and Ollama/Chat to fail with a spurious
        // 504 (TaskCanceledException, see GlobalExceptionHandler).
        client.Timeout = TimeSpan.FromMinutes(5);
    });
    builder.Services.AddHttpClient<ISpeachesService, SpeachesService>(client =>
    {
        var baseUrl = builder.Configuration["Speaches:BaseUrl"]
            ?? throw new InvalidOperationException("Configuration value 'Speaches:BaseUrl' is missing.");
        client.BaseAddress = new Uri(baseUrl);
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    // Configure the HTTP request pipeline.
    // Swagger is intentionally available in every environment (not just Development) so it can be
    // used against deployed instances too — the environment badge below exists specifically to make
    // that safe by keeping it obvious at a glance which environment you're pointed at.
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Collapse tag groups (Health, Ollama, ...) down to their headers for a quick overview of all endpoints.
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);

        // Hide the "Schemas" section — response shapes are already documented per-endpoint via ProducesResponseType.
        options.DefaultModelsExpandDepth(-1);

        // Discreet environment badge, placed under the "AiGateway" title in Swagger UI's info
        // section, so it's obvious at a glance which environment Swagger is pointed at. Colors are
        // keyed off ASPNETCORE_ENVIRONMENT; anything other than Development/Production (e.g. a
        // future Staging) falls back to a neutral color rather than defaulting to either dev or
        // prod's color. Swagger UI renders the info section asynchronously (after fetching the
        // OpenAPI doc), so the script polls briefly for the title instead of assuming it's present
        // at 'load'.
        var environmentName = app.Environment.EnvironmentName;
        var badgeColor = environmentName switch
        {
            "Production" => "#2e7d32",
            "Development" => "#e65100",
            _ => "#546e7a"
        };
        options.HeadContent = $@"
            <style>
                #env-badge {{
                    display: inline-block;
                    margin: 4px 0 12px;
                    padding: 3px 10px;
                    border-radius: 12px;
                    font-family: sans-serif;
                    font-size: 11px;
                    font-weight: 600;
                    color: #fff;
                    background-color: {badgeColor};
                    letter-spacing: 0.02em;
                }}
            </style>
            <script>
                window.addEventListener('load', function () {{
                    var attempts = 0;
                    var interval = setInterval(function () {{
                        attempts++;
                        var titleGroup = document.querySelector('.swagger-ui .info hgroup.main');
                        if (titleGroup) {{
                            clearInterval(interval);
                            var badge = document.createElement('div');
                            badge.id = 'env-badge';
                            badge.textContent = '{environmentName}';
                            titleGroup.insertAdjacentElement('afterend', badge);
                        }} else if (attempts > 50) {{
                            clearInterval(interval);
                        }}
                    }}, 100);
                }});
            </script>";
    });

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
