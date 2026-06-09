using System.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniDesk.Web.Data;
using UniDesk.Web.Options;

namespace UniDesk.IntegrationTests;

public sealed class UniDeskWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly DirectoryInfo DataProtectionKeyDirectory =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "UniDesk.IntegrationTests.DataProtectionKeys"));

    private readonly string _environmentName;
    private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;
    private readonly string _testEmail;
    private readonly bool _useTestAuthentication;
    private readonly string _testUserId;
    private readonly string[] _testRoles;
    private SqliteConnection? _connection;

    public UniDeskWebApplicationFactory(
        bool useTestAuthentication = true,
        string[]? testRoles = null,
        string environmentName = "Development",
        string testUserId = "integration-test-user",
        string testEmail = "integration-test@unidesk.local",
        IReadOnlyDictionary<string, string?>? configurationOverrides = null)
    {
        _environmentName = environmentName;
        _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
        _testEmail = testEmail;
        _useTestAuthentication = useTestAuthentication;
        _testUserId = testUserId;
        _testRoles = testRoles ?? new[] { AppRoles.Admin };
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(_configurationOverrides);
        });

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<UniDeskDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<UniDeskDbContext>(options => options.UseSqlite(_connection));
            services.AddDataProtection()
                .PersistKeysToFileSystem(DataProtectionKeyDirectory)
                .SetApplicationName("UniDesk.IntegrationTests");
            services.Configure<SeedDataOptions>(options => options.Tickets.Clear());

            if (_useTestAuthentication)
            {
                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                        options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                    })
                    .AddScheme<TestAuthOptions, TestAuthHandler>(
                        TestAuthHandler.AuthenticationScheme,
                        options =>
                        {
                            options.Email = _testEmail;
                            options.Roles = _testRoles;
                            options.UserId = _testUserId;
                        });
            }

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
            db.Database.Migrate();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
