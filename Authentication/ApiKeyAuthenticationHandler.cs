using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using AiGateway.Data;
using AiGateway.Dto.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiGateway.Authentication;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    AppDbContext dbContext) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string AdministratorRole = "Administrator";
    public const string StandardRole = "Standard";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
        {
            return AuthenticateResult.Fail($"Missing '{HeaderName}' header.");
        }

        var providedKey = headerValue.ToString();

        var masterKey = configuration["Auth:MasterApiKey"]
            ?? throw new InvalidOperationException("Configuration value 'Auth:MasterApiKey' is missing.");

        if (FixedTimeEquals(providedKey, masterKey))
        {
            return AuthenticateResult.Success(CreateTicket("master", "Master Key", AdministratorRole));
        }

        var keyHash = ApiKeyHasher.Hash(providedKey);
        var apiKey = await dbContext.ApiKeys.SingleOrDefaultAsync(key => key.KeyHash == keyHash);

        if (apiKey is null || !apiKey.IsActive || apiKey.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return AuthenticateResult.Fail("Invalid, inactive or expired API key.");
        }

        apiKey.LastUsedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        return AuthenticateResult.Success(CreateTicket(apiKey.Id.ToString(), apiKey.Name, StandardRole));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        WriteErrorAsync(StatusCodes.Status401Unauthorized, "Unauthorized", $"A valid '{HeaderName}' header is required.");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        WriteErrorAsync(StatusCodes.Status403Forbidden, "Forbidden", "The provided API key does not have access to this resource.");

    private async Task WriteErrorAsync(int status, string title, string detail)
    {
        Response.StatusCode = status;
        await Response.WriteAsJsonAsync(new ErrorResponseDto
        {
            Status = status,
            Title = title,
            Detail = detail,
            TraceId = Context.TraceIdentifier
        });
    }

    private AuthenticationTicket CreateTicket(string subject, string name, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        return new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
    }

    // The master key has fixed length in practice, but CryptographicOperations.FixedTimeEquals still
    // short-circuits on a length mismatch — an acceptable, standard trade-off for comparing a bearer secret.
    private static bool FixedTimeEquals(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
}
