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
            new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
            new Claim(ClaimTypes.Name, "integration-test@unidesk.local")
        };

        claims.AddRange(Options.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
