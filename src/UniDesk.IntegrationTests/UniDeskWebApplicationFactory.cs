using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniDesk.Web.Data;

namespace UniDesk.IntegrationTests;

public sealed class UniDeskWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool _useTestAuthentication;
    private readonly string[] _testRoles;
    private SqliteConnection? _connection;

    public UniDeskWebApplicationFactory(bool useTestAuthentication = true, string[]? testRoles = null)
    {
        _useTestAuthentication = useTestAuthentication;
        _testRoles = testRoles ?? new[] { AppRoles.Admin };
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
                        options => options.Roles = _testRoles);
            }

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
            db.Database.EnsureCreated();
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
