using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.EmissionEntries;
using GreenOps.Infrastructure.Ai;
using GreenOps.Infrastructure.Persistence;
using GreenOps.Infrastructure.Repositories;
using GreenOps.Infrastructure.Services;
using GreenOps.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GreenOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Persistence ──────────────────────────────────────────
        // ICurrentUserAccessor sunum katmanında (HttpContext üzerinden) kaydedilir.
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // ── Yapay zeka (yerel Ollama) ─────────────────────────────
        services.AddMemoryCache();
        services.AddSingleton(new OllamaClient(OllamaOptions.From(configuration)));
        services.AddScoped<IAiService, OllamaAiService>();

        // ── Repositories ─────────────────────────────────────────
        services.AddScoped<IConversionFactorRepository, ConversionFactorRepository>();
        services.AddScoped<IEmissionEntryRepository, EmissionEntryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ILicenseRepository, LicenseRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IFacilityRepository, FacilityRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IEmissionCategoryRepository, EmissionCategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ISupplierEmissionRepository, SupplierEmissionRepository>();
        services.AddScoped<ITargetRepository, TargetRepository>();
        services.AddScoped<ICarbonCreditRepository, CarbonCreditRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAlertSettingRepository, AlertSettingRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IBillingPlanRepository, BillingPlanRepository>();
        services.AddScoped<IScopeRepository, ScopeRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IIotDeviceRepository, IotDeviceRepository>();
        services.AddScoped<IIotMeterReadingRepository, IotMeterReadingRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        // ── Services ─────────────────────────────────────────────
        services.AddScoped<IEmissionEntryService, EmissionEntryService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ITargetService, TargetService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IExportCarbonPassportService, ExportCarbonPassportService>();

        return services;
    }
}
