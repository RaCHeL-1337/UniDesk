using UniDesk.Web.DTOs;
using UniDesk.Web.Middleware;
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

builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<ITicketService>(provider => provider.GetRequiredService<TicketService>());

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var ticketsV2 = app.MapGroup("/api/v2/tickets")
    .WithTags("Tickets v2")
    .WithOpenApi();

ticketsV2.MapGet("/", GetTicketsV2);
ticketsV2.MapPost("/", CreateTicketV2);
ticketsV2.MapPut("/{id:int}", UpdateTicketV2);
ticketsV2.MapDelete("/{id:int}", DeleteTicketV2);

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
