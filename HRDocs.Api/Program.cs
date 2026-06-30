using System.Text;
using HRDocs.Api.Hubs;
using HRDocs.Api.Services;
using HRDocs.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ?? builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("Set ConnectionStrings:PostgreSQL or DATABASE_URL.");
if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    connectionString = DatabaseUrl.Normalize(connectionString);

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(o => o.AddPolicy("Client", p => p.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:5000", "https://localhost:5001"]).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"], IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), ClockSkew = TimeSpan.FromMinutes(1) };
    o.Events = new JwtBearerEvents { OnMessageReceived = c => { var token = c.Request.Query["access_token"]; if (!string.IsNullOrEmpty(token) && c.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications")) c.Token = token; return Task.CompletedTask; } };
});
builder.Services.AddAuthorization();
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", at = DateTime.UtcNow }));

if (app.Configuration.GetValue("Database:ApplyMigrations", true))
{
    using var scope = app.Services.CreateScope();
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), app.Configuration["SeedAdmin:Email"] ?? "admin@hrdocs.local", Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD") ?? app.Configuration["SeedAdmin:Password"] ?? "ChangeMe123!");
}
app.Run();

static class DatabaseUrl
{
    public static string Normalize(string value)
    {
        var uri = new Uri(value); var user = uri.UserInfo.Split(':', 2);
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={Uri.UnescapeDataString(user[0])};Password={Uri.UnescapeDataString(user.ElementAtOrDefault(1) ?? "")};SSL Mode=Require;Trust Server Certificate=true";
    }
}
