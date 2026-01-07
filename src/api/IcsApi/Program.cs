using System.Security.Claims;
using IcsApi.Models;
using IcsApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrAttorney", policy => policy.RequireRole("Admin", "Attorney"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevAdminUI", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<InMemoryCaseStore>();
builder.Services.AddSingleton<InMemoryAuditStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Ics:UsePathBase"))
{
    app.UsePathBase("/ics-api");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevAdminUI");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    await next();

    var user = context.User;
    var roles = user.FindAll("roles").Select(r => r.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    if (roles.Count == 0)
    {
        roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    var evt = new AuditEvent(
        TimestampUtc: DateTime.UtcNow,
        UserObjectId: user.FindFirstValue("oid"),
        Username: user.FindFirstValue("preferred_username") ?? user.Identity?.Name,
        Roles: roles,
        Method: context.Request.Method,
        Path: context.Request.Path.Value ?? string.Empty,
        StatusCode: context.Response.StatusCode,
        CorrelationId: context.TraceIdentifier
    );

    context.RequestServices.GetRequiredService<InMemoryAuditStore>().Add(evt);
});

var api = app.MapGroup(string.Empty);

api.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

api.MapGet("/me", (ClaimsPrincipal user) =>
{
    var roles = user.FindAll("roles").Select(r => r.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    if (roles.Count == 0)
    {
        roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    return Results.Ok(new
    {
        oid = user.FindFirstValue("oid"),
        username = user.FindFirstValue("preferred_username") ?? user.Identity?.Name,
        roles
    });
}).RequireAuthorization("AdminOrAttorney");

api.MapGet("/cases", (InMemoryCaseStore store) => Results.Ok(store.GetAll()))
    .RequireAuthorization("AdminOrAttorney");

api.MapGet("/cases/{id}", (string id, InMemoryCaseStore store) =>
{
    var item = store.Get(id);
    return item is null
        ? Results.NotFound(new ProblemDetails { Title = "Not found", Detail = $"Case '{id}' was not found." })
        : Results.Ok(item);
}).RequireAuthorization("AdminOrAttorney");

api.MapPost("/cases", ([FromBody] CaseItem request, InMemoryCaseStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Id))
    {
        return Results.BadRequest(new ProblemDetails { Title = "Validation", Detail = "Id is required." });
    }

    var now = DateTime.UtcNow;
    var created = store.Upsert(request with { CreatedUtc = request.CreatedUtc == default ? now : request.CreatedUtc });
    return Results.Created($"/cases/{created.Id}", created);
}).RequireAuthorization("AdminOnly");

api.MapDelete("/cases/{id}", (string id, InMemoryCaseStore store) =>
{
    var deleted = store.Delete(id);
    return deleted ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization("AdminOnly");

api.MapGet("/audit", (InMemoryAuditStore audit) => Results.Ok(audit.GetAll()))
    .RequireAuthorization("AdminOnly");

app.Run();
