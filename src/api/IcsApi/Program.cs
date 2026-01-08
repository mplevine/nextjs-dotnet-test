using System.Security.Claims;
using IcsApi.Models;
using IcsApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

builder.Services.AddSingleton<ICaseStore, InMemoryCaseStore>();
builder.Services.AddSingleton<IAuditStore, InMemoryAuditStore>();

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

    context.RequestServices.GetRequiredService<IAuditStore>().Add(evt);
});

app.MapControllers();

app.Run();
