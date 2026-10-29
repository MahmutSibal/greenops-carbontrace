using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using GreenOps.Application;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure;
using GreenOps.Infrastructure.Persistence;
using GreenOps.Infrastructure.Security;
using GreenOps.WebApi.Controllers;
using GreenOps.WebApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── JWT anahtarı: yoksa, kısaysa veya örnek değerse uygulama başlamaz ──
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32 || jwtKey.StartsWith("CHANGE_THIS"))
    throw new InvalidOperationException(
        "Jwt:Key en az 32 bayt olmalı ve örnek değer kullanılmamalı. " +
        "Geliştirmede appsettings.Development.json, üretimde ortam değişkeni (Jwt__Key) veya secret store kullanın.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            // Token süresi dolmadan önce pasife alınan, rolü veya şirketi değişen kullanıcı hemen dışarıda kalır.
            OnTokenValidated = async context =>
            {
                var principal = context.Principal!;
                if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                {
                    context.Fail("Geçersiz kullanıcı.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.IsActive, u.Role, u.CompanyId })
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                var claimCompany = principal.FindFirstValue("companyId");
                if (user is null
                    || !user.IsActive
                    || user.Role.ToString() != principal.FindFirstValue(ClaimTypes.Role)
                    || (user.CompanyId?.ToString() ?? "") != (claimCompany ?? ""))
                {
                    context.Fail("Oturum artık geçerli değil.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// ── Rate limiting: kaba kuvvet ve webhook selini sınırla ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Auth, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy(RateLimitPolicies.Webhook, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Request.Headers[IotMetersController.ApiKeyHeader].ToString() is { Length: > 0 } key ? key : "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    // Yerel LLM çağrıları pahalı: kullanıcı başına dakikada 8, aynı anda en fazla 2 kuyrukta.
    options.AddPolicy(RateLimitPolicies.Ai, http => RateLimitPartition.GetFixedWindowLimiter(
        http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(1), QueueLimit = 2 }));
});

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Sadece native mobil istemci (Flutter) var; tarayıcı istemcisi olmadığı için CORS açılmaz.

var app = builder.Build();

ApplyMigrations(app);
SeedSuperAdmin(app);

// Hata ayrıntıları istemciye sızmaz; geliştirmede geliştirici sayfası gösterilir.
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

static void SeedSuperAdmin(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
    var config = app.Configuration;

    var adminEmail = config["Seed:AdminEmail"] ?? "admin@greenops.com";
    if (dbContext.Users.Any(u => u.Role == UserRole.SuperAdmin))
        return;

    // Sabit varsayılan şifre yok: yapılandırılmamışsa tek seferlik rastgele şifre üretilip log'a yazılır.
    var password = config["Seed:AdminPassword"];
    if (string.IsNullOrWhiteSpace(password))
    {
        password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12)) + "1a";
        logger.LogWarning("SuperAdmin oluşturuldu: {Email} / {Password} — ilk girişte değiştirin.", adminEmail, password);
    }
    else if (PasswordPolicy.Validate(password) is { } error)
    {
        throw new InvalidOperationException($"Seed:AdminPassword geçersiz: {error}");
    }

    dbContext.Users.Add(new AppUser
    {
        Id = Guid.NewGuid(),
        FullName = "Super Admin",
        Email = adminEmail,
        PasswordHash = PasswordHasher.Hash(password),
        Role = UserRole.SuperAdmin,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow
    });
    dbContext.SaveChanges();
}
