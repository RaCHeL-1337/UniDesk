using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UniDesk.Web.Data;

namespace UniDesk.IntegrationTests;

public class TestAuthOptions : AuthenticationSchemeOptions
{
    public string[] Roles { get; set; } = { AppRoles.Admin };
    public string UserId { get; set; } = "integration-test-user";
    public string Email { get; set; } = "integration-test@unidesk.local";
}

public class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
{
    public const string AuthenticationScheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<TestAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Options.UserId),
            new Claim(ClaimTypes.Name, Options.Email)
        };

        claims.AddRange(Options.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
