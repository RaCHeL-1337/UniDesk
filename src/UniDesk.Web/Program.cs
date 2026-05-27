using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using UniDesk.Web.DTOs;
using UniDesk.Web.Middleware;
using UniDesk.Web.Models;
using UniDesk.Web.Services;
using Microsoft.EntityFrameworkCore;
using UniDesk.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "UniDesk API",
        Version = "v1"
    });
});
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<UniDeskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine, LogLevel.Information));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<UniDeskDbContext>()
    .AddSignInManager()
    .AddApiEndpoints()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".AspNetCore.Identity.Application";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<ITicketService>(provider => provider.GetRequiredService<TicketService>());

var app = builder.Build();

await IdentitySeedData.EnsureSeedUserAsync(app.Services, app.Configuration);

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        return Task.CompletedTask;
    });

    await next(context);
});

app.UseMiddleware<EntityNotFoundExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapIdentityApi<ApplicationUser>()
    .AllowAnonymous();

var ticketsV2 = app.MapGroup("/api/v2/tickets")
    .WithTags("Tickets v2")
    .WithOpenApi()
    .RequireAuthorization();

ticketsV2.MapGet("/", GetTicketsV2);
ticketsV2.MapPost("/", CreateTicketV2);
ticketsV2.MapPut("/{id:int}", UpdateTicketV2);
ticketsV2.MapDelete("/{id:int}", DeleteTicketV2)
    .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin));

app.Run();

static IResult GetTicketsV2(ITicketService ticketService)
{
    return Results.Ok(ticketService.GetAllForApi());
}

static IResult CreateTicketV2(CreateTicketRequest request, ITicketService ticketService)
{
    var created = ticketService.Create(request);
    return Results.Created($"/api/v2/tickets/{created.Id}", created);
}

static IResult UpdateTicketV2(int id, CreateTicketRequest request, ITicketService ticketService)
{
    ticketService.Update(id, request);
    return Results.NoContent();
}

static IResult DeleteTicketV2(int id, ITicketService ticketService)
{
    ticketService.Delete(id);
    return Results.NoContent();
}

public partial class Program { }
