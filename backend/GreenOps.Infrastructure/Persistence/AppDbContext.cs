using GreenOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<EmissionCategory> EmissionCategories => Set<EmissionCategory>();
    public DbSet<ConversionFactor> ConversionFactors => Set<ConversionFactor>();
    public DbSet<EmissionEntry> EmissionEntries => Set<EmissionEntry>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Yeni DbSet'ler
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierEmission> SupplierEmissions => Set<SupplierEmission>();
    public DbSet<Target> Targets => Set<Target>();
    public DbSet<CarbonCredit> CarbonCredits => Set<CarbonCredit>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AlertSetting> AlertSettings => Set<AlertSetting>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<BillingPlan> BillingPlans => Set<BillingPlan>();
    public DbSet<Scope> Scopes => Set<Scope>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<IotDevice> IotDevices => Set<IotDevice>();
    public DbSet<IotMeterReading> IotMeterReadings => Set<IotMeterReading>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.TaxNo).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.Industry).HasMaxLength(100);
            entity.Property(x => x.AnnualRevenue).HasColumnType("decimal(18,2)");
            // Kayıtta otomatik açılan şirketlerin vergi no'su boş gelir; onlar hariç tutulur.
            entity.HasIndex(x => x.TaxNo).IsUnique().HasFilter("[TaxNo] <> ''");
        });

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.Department).HasMaxLength(200);
            entity.Property(x => x.SquareMeters).HasColumnType("decimal(18,2)");
            // Pasife alınan tesisler aynı adla yeniden açılabilsin diye sadece aktifler tekil.
            entity.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique().HasFilter("[IsActive] = 1");
            entity.HasOne(x => x.Company)
                .WithMany(c => c.Facilities)
                .HasForeignKey(x => x.CompanyId);
        });

        modelBuilder.Entity<EmissionCategory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Unit).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<ConversionFactor>(entity =>
        {
            entity.HasKey(x => x.Id);
            // Emisyon katsayıları 4-6 ondalık basamaklıdır; varsayılan decimal(18,2) yuvarlıyordu.
            entity.Property(x => x.Factor).HasColumnType("decimal(18,6)");
            entity.Property(x => x.Source).HasMaxLength(500);
            entity.Property(x => x.SourceUrl).HasMaxLength(500);
            entity.HasIndex(x => new { x.CategoryId, x.Year }).IsUnique();
            entity.HasOne(x => x.Category)
                .WithMany(c => c.ConversionFactors)
                .HasForeignKey(x => x.CategoryId);
        });

        modelBuilder.Entity<EmissionEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Value).HasColumnType("decimal(18,4)");
            entity.Property(x => x.CalculationResult).HasColumnType("decimal(18,4)");
            entity.Property(x => x.FactorValue).HasColumnType("decimal(18,6)");
            entity.Property(x => x.Unit).HasMaxLength(20);
            entity.Property(x => x.EvidenceUrl).HasMaxLength(500);
            entity.Property(x => x.RejectionReason).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId);
            entity.HasOne(x => x.Facility)
                .WithMany(f => f.EmissionEntries)
                .HasForeignKey(x => x.FacilityId);
            entity.HasOne(x => x.Category)
                .WithMany(c => c.EmissionEntries)
                .HasForeignKey(x => x.CategoryId);
            entity.HasOne(x => x.ConversionFactor)
                .WithMany()
                .HasForeignKey(x => x.ConversionFactorId)
                .IsRequired(false);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId);
            entity.HasOne(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .IsRequired(false);
            entity.HasOne(x => x.RejectedByUser)
                .WithMany()
                .HasForeignKey(x => x.RejectedByUserId)
                .IsRequired(false);
            // Dashboard, rapor ve trend sorguları bu kombinasyonla filtreler.
            entity.HasIndex(x => new { x.CompanyId, x.Status, x.IsDeleted, x.CreatedAt });
            entity.HasIndex(x => new { x.FacilityId, x.Status, x.IsDeleted, x.CreatedAt });
        });

        SeedEmissionCategories(modelBuilder);

        // AppUser
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PasswordHash).IsRequired().HasMaxLength(200);
            entity.Property(x => x.PhoneNumber).HasMaxLength(30);
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.TwoFactorSecret).HasMaxLength(100);
            entity.Property(x => x.PasswordResetToken).HasMaxLength(200);
            entity.HasIndex(x => x.PasswordResetToken).HasFilter("[PasswordResetToken] IS NOT NULL");
            entity.HasOne(x => x.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(x => x.CompanyId);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Token).IsRequired().HasMaxLength(500);
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId);
        });

        // License
        modelBuilder.Entity<License>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LicenseKey).IsRequired().HasMaxLength(200);
            entity.HasIndex(x => x.LicenseKey).IsUnique();
            entity.Property(x => x.MacAddress).HasMaxLength(100);
            entity.Property(x => x.IpAddress).HasMaxLength(50);
            entity.Property(x => x.PlanName).HasMaxLength(100);
            entity.HasOne(x => x.Company)
                .WithMany(c => c.Licenses)
                .HasForeignKey(x => x.CompanyId);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).IsRequired().HasMaxLength(100);
            entity.Property(x => x.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.EntityId).HasMaxLength(100);
            entity.Property(x => x.UserName).HasMaxLength(200);
            entity.HasIndex(x => new { x.CompanyId, x.CreatedAt });
        });

        // Supplier
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.ContactEmail).HasMaxLength(200);
            entity.Property(x => x.ContactPhone).HasMaxLength(30);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.HasOne(x => x.Company)
                .WithMany(c => c.Suppliers)
                .HasForeignKey(x => x.CompanyId);
        });

        // SupplierEmission
        modelBuilder.Entity<SupplierEmission>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Value).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasOne(x => x.Supplier)
                .WithMany(s => s.Emissions)
                .HasForeignKey(x => x.SupplierId);
        });

        // Target
        modelBuilder.Entity<Target>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BaselineEmissions).HasColumnType("decimal(18,4)");
            entity.Property(x => x.ReductionPercent).HasColumnType("decimal(5,2)");
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasOne(x => x.Company)
                .WithMany(c => c.Targets)
                .HasForeignKey(x => x.CompanyId);
        });

        // CarbonCredit
        modelBuilder.Entity<CarbonCredit>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Credits).HasColumnType("decimal(18,4)");
            entity.Property(x => x.CertificateUrl).HasMaxLength(500);
            entity.Property(x => x.Provider).HasMaxLength(200);
            entity.HasOne(x => x.Company)
                .WithMany(c => c.CarbonCredits)
                .HasForeignKey(x => x.CompanyId);
        });

        // Document
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).IsRequired().HasMaxLength(300);
            entity.Property(x => x.FileUrl).IsRequired().HasMaxLength(500);
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.HasOne(x => x.Company)
                .WithMany(c => c.Documents)
                .HasForeignKey(x => x.CompanyId);
            entity.HasOne(x => x.EmissionEntry)
                .WithMany(e => e.Documents)
                .HasForeignKey(x => x.EmissionEntryId)
                .IsRequired(false);
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Message).IsRequired().HasMaxLength(1000);
            entity.HasIndex(x => new { x.UserId, x.IsRead });
            entity.HasOne(x => x.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(x => x.UserId);
        });

        // AlertSetting
        modelBuilder.Entity<AlertSetting>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AlertType).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Threshold).HasColumnType("decimal(18,4)");
            entity.HasIndex(x => new { x.CompanyId, x.AlertType }).IsUnique();
            entity.HasOne(x => x.Company)
                .WithMany(c => c.AlertSettings)
                .HasForeignKey(x => x.CompanyId);
        });

        // Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        // Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        // RolePermission
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasOne(x => x.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(x => x.RoleId);
            entity.HasOne(x => x.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(x => x.PermissionId);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Currency).HasMaxLength(10);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.Property(x => x.PdfUrl).HasMaxLength(500);
            entity.HasOne(x => x.Company)
                .WithMany(c => c.Invoices)
                .HasForeignKey(x => x.CompanyId);
        });

        // BillingPlan
        modelBuilder.Entity<BillingPlan>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.MonthlyPrice).HasColumnType("decimal(18,2)");
        });

        // Scope
        modelBuilder.Entity<Scope>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        // ApiKey
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Key).IsRequired().HasMaxLength(200);
            entity.HasIndex(x => x.Key).IsUnique();
            entity.HasOne(x => x.Company)
                .WithMany(c => c.ApiKeys)
                .HasForeignKey(x => x.CompanyId);
        });

        // IotDevice
        modelBuilder.Entity<IotDevice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.SerialNumber).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.SerialNumber).IsUnique();
            entity.Property(x => x.DeviceType).HasMaxLength(100);
            entity.HasOne(x => x.Facility)
                .WithMany(f => f.IotDevices)
                .HasForeignKey(x => x.FacilityId);
        });

        // IotMeterReading
        modelBuilder.Entity<IotMeterReading>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Value).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Unit).HasMaxLength(20);
            entity.HasOne(x => x.IotDevice)
                .WithMany(d => d.Readings)
                .HasForeignKey(x => x.IotDeviceId);
        });

        // Report
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReportType).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.Property(x => x.FileUrl).HasMaxLength(500);
            entity.HasOne(x => x.Company)
                .WithMany(c => c.Reports)
                .HasForeignKey(x => x.CompanyId);
        });

        // AppUser -> CustomRole ilişkisi
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasOne(x => x.CustomRole)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .IsRequired(false);
        });

        // Company -> BillingPlan ilişkisi
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasOne(x => x.BillingPlan)
                .WithMany()
                .HasForeignKey(x => x.BillingPlanId)
                .IsRequired(false);
        });

        // EmissionCategory -> Scope ilişkisi
        modelBuilder.Entity<EmissionCategory>(entity =>
        {
            entity.HasOne(x => x.Scope)
                .WithMany(s => s.Categories)
                .HasForeignKey(x => x.ScopeId)
                .IsRequired(false);
        });

        // Emisyon ve finans verisi yasal olarak saklanmalı: üst kayıt silinince alt kayıtlar
        // zincirleme silinmesin. Sadece oturum/bildirim/eşleme gibi türetilmiş veriler cascade kalır.
        var cascadeAllowed = new[] { typeof(RefreshToken), typeof(Notification), typeof(RolePermission) };
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            if (fk.DeleteBehavior == DeleteBehavior.Cascade && !cascadeAllowed.Contains(fk.DeclaringEntityType.ClrType))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static void SeedEmissionCategories(ModelBuilder modelBuilder)
    {
        // Scope seed
        var scope1Id = Guid.Parse("A0000001-0000-0000-0000-000000000001");
        var scope2Id = Guid.Parse("A0000001-0000-0000-0000-000000000002");
        var scope3Id = Guid.Parse("A0000001-0000-0000-0000-000000000003");

        modelBuilder.Entity<Scope>().HasData(
            new Scope { Id = scope1Id, Number = 1, Name = "Scope 1", Description = "Doğrudan emisyonlar" },
            new Scope { Id = scope2Id, Number = 2, Name = "Scope 2", Description = "Dolaylı enerji emisyonları" },
            new Scope { Id = scope3Id, Number = 3, Name = "Scope 3", Description = "Diğer dolaylı emisyonlar" }
        );

        var electricityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var naturalGasId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var dieselId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        modelBuilder.Entity<EmissionCategory>().HasData(
            new EmissionCategory { Id = electricityId, Name = "Electricity", Unit = "kWh", ScopeId = scope2Id },
            new EmissionCategory { Id = naturalGasId, Name = "Natural Gas", Unit = "m3", ScopeId = scope1Id },
            new EmissionCategory { Id = dieselId, Name = "Diesel", Unit = "L", ScopeId = scope1Id }
        );

        const string placeholderSource = "Geliştirme yer tutucusu — resmi bir kaynağa dayanmaz (bkz. KAYNAKLAR.md bölüm 5.2).";

        modelBuilder.Entity<ConversionFactor>().HasData(
            // İlk sürümün kaynaksız elektrik değeri. Bu değerle hesaplanmış eski kayıtların denetim izi
            // kopmasın diye silinmez; IsActive=false olduğu için yeni hesaplamalarda kullanılmaz.
            new ConversionFactor
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                CategoryId = electricityId,
                Year = 2026,
                Factor = 0.5m,
                Source = placeholderSource + " ETKB 2023 resmi değeriyle değiştirildi.",
                IsVerified = false,
                IsActive = false
            },
            // Resmi kaynak: T.C. Enerji ve Tabii Kaynaklar Bakanlığı, Türkiye Elektrik Üretimi ve Elektrik
            // Tüketim Noktası Emisyon Faktörleri Bilgi Formu (ETKB-EVÇED-FRM-042 Rev.01), hesaplama dönemi 2023,
            // hesaplama yayım tarihi 26.12.2025 — "Dağıtım Hattından Bağlı Tüketim Noktası Emisyon Faktörü"
            // 0,469 tCO2-eşd./MWh = 0,469 kgCO2e/kWh.
            new ConversionFactor
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                CategoryId = electricityId,
                Year = 2023,
                Factor = 0.469m,
                Source = "T.C. Enerji ve Tabii Kaynaklar Bakanlığı, Türkiye Elektrik Üretimi ve Elektrik Tüketim Noktası Emisyon Faktörleri Bilgi Formu, ETKB-EVÇED-FRM-042 Rev.01, hesaplama dönemi 2023 (yayım 26.12.2025): Dağıtım hattından bağlı tüketim noktası, 0,469 tCO2-eşd./MWh.",
                SourceUrl = "https://enerji.gov.tr/Media/Dizin/EVCED/tr/%C3%87evreVe%C4%B0klim/%C4%B0klimDe%C4%9Fi%C5%9Fikli%C4%9Fi/EmisyonFaktorleri/2023_Turkiye_Elektrik_UretimiveElektrik_Tuketim_Noktasi_Emisyon_Faktorleri.pdf",
                IsVerified = true,
                IsActive = true
            },
            // İlk sürümün kaynaksız doğalgaz/motorin değerleri: elektrikteki gibi denetim izi için saklanır, pasif.
            new ConversionFactor
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                CategoryId = naturalGasId,
                Year = 2026,
                Factor = 2.0m,
                Source = placeholderSource + " IPCC 2006 + Türkiye NID 2026 ısıl değeriyle değiştirildi.",
                IsVerified = false,
                IsActive = false
            },
            new ConversionFactor
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                CategoryId = dieselId,
                Year = 2026,
                Factor = 2.7m,
                Source = placeholderSource + " IPCC 2006 + Türkiye NID 2026 ısıl değeriyle değiştirildi.",
                IsVerified = false,
                IsActive = false
            },
            // Yakıt yakma (Kapsam 1): IPCC 2006 Cilt 2 Bölüm 2 Tablo 2.3 (imalat sanayi) emisyon faktörleri
            // × Türkiye Ulusal Envanter Belgesi (NID 2026, 1990-2024) Ek "NCV of Fuels" 2024 ısıl değeri × AR5 GWP
            // (CH4 28, N2O 265; NID'nin kullandığı set). Hesap ve sayfa numaraları: KAYNAKLAR.md bölüm 5.
            // Doğalgaz: 34,54 TJ/10^6 m3 × (56.100 + 1×28 + 0,1×265) kg/TJ = 1,939576 kgCO2e/m3.
            new ConversionFactor
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                CategoryId = naturalGasId,
                Year = 2024,
                Factor = 1.939576m,
                Source = "Emisyon faktörü: 2006 IPCC Guidelines, Cilt 2, Bölüm 2, Tablo 2.3 (CO2 56.100, CH4 1, N2O 0,1 kg/TJ). Isıl değer: Türkiye Ulusal Sera Gazı Envanter Belgesi (NID 2026, 1990-2024), Ek \"NCV of Fuels\", 2024: 34,54 TJ/10^6 m3. GWP: IPCC AR5 (CH4 28, N2O 265).",
                SourceUrl = "https://unfccc.int/sites/default/files/resource/TUR_NID_2026.pdf",
                IsVerified = true,
                IsActive = true
            },
            // Motorin: 43,33 TJ/kt × 0,830 kg/L × (74.100 + 3×28 + 0,6×265) kg/TJ = 2,673664 kgCO2e/L.
            new ConversionFactor
            {
                Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                CategoryId = dieselId,
                Year = 2024,
                Factor = 2.673664m,
                Source = "Emisyon faktörü: 2006 IPCC Guidelines, Cilt 2, Bölüm 2, Tablo 2.3 (CO2 74.100, CH4 3, N2O 0,6 kg/TJ). Isıl değer: Türkiye NID 2026, Ek \"NCV of Fuels\", 2024: motorin 43,33 TJ/kt. Yoğunluk: Enerji Kaynaklarının ve Enerjinin Kullanımında Verimliliğin Artırılmasına Dair Yönetmelik EK-2 (RG 27.10.2011/28097): 0,830 kg/L. GWP: IPCC AR5.",
                SourceUrl = "https://unfccc.int/sites/default/files/resource/TUR_NID_2026.pdf",
                IsVerified = true,
                IsActive = true
            }
        );

        // Seed tarihleri sabit: UtcNow her migration'da gereksiz UpdateData üretiyordu.
        var seedDate = new DateTimeOffset(2026, 2, 26, 0, 0, 0, TimeSpan.Zero);

        // BillingPlan seed
        modelBuilder.Entity<BillingPlan>().HasData(
            new BillingPlan { Id = Guid.Parse("B0000001-0000-0000-0000-000000000001"), Name = "Free", MonthlyPrice = 0, MaxFacilities = 1, MaxUsersCount = 3, HasApiAccess = false, HasIotIntegration = false, HasAiPrediction = false, CreatedAt = seedDate },
            new BillingPlan { Id = Guid.Parse("B0000001-0000-0000-0000-000000000002"), Name = "Starter", MonthlyPrice = 499, MaxFacilities = 5, MaxUsersCount = 10, HasApiAccess = true, HasIotIntegration = false, HasAiPrediction = false, CreatedAt = seedDate },
            new BillingPlan { Id = Guid.Parse("B0000001-0000-0000-0000-000000000003"), Name = "Professional", MonthlyPrice = 1499, MaxFacilities = 20, MaxUsersCount = 50, HasApiAccess = true, HasIotIntegration = true, HasAiPrediction = true, CreatedAt = seedDate },
            new BillingPlan { Id = Guid.Parse("B0000001-0000-0000-0000-000000000004"), Name = "Enterprise", MonthlyPrice = 4999, MaxFacilities = 100, MaxUsersCount = 500, HasApiAccess = true, HasIotIntegration = true, HasAiPrediction = true, CreatedAt = seedDate }
        );

        // Varsayılan roller
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Guid.Parse("C0000001-0000-0000-0000-000000000001"), Name = "SuperAdmin", Description = "Tüm yetkiler", IsSystem = true, CreatedAt = seedDate },
            new Role { Id = Guid.Parse("C0000001-0000-0000-0000-000000000002"), Name = "CompanyAdmin", Description = "Şirket yöneticisi", IsSystem = true, CreatedAt = seedDate },
            new Role { Id = Guid.Parse("C0000001-0000-0000-0000-000000000003"), Name = "Manager", Description = "Tesis yöneticisi", IsSystem = true, CreatedAt = seedDate },
            new Role { Id = Guid.Parse("C0000001-0000-0000-0000-000000000004"), Name = "Personnel", Description = "Personel", IsSystem = true, CreatedAt = seedDate }
        );
    }
}
