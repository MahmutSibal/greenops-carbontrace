using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly IBillingPlanRepository _planRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly AppDbContext _dbContext;

    public BillingService(
        IBillingPlanRepository planRepository,
        IInvoiceRepository invoiceRepository,
        ICompanyRepository companyRepository,
        AppDbContext dbContext)
    {
        _planRepository = planRepository;
        _invoiceRepository = invoiceRepository;
        _companyRepository = companyRepository;
        _dbContext = dbContext;
    }

    public async Task<Result<BillingPlanDto>> GetCurrentPlanAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies
            .Include(c => c.BillingPlan)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

        if (company is null)
            return Result<BillingPlanDto>.Failure("Şirket bulunamadı.");

        // Önce BillingPlan'a bak
        if (company.BillingPlan is not null)
        {
            return Result<BillingPlanDto>.Success(new BillingPlanDto
            {
                Id = company.BillingPlan.Id,
                Name = company.BillingPlan.Name,
                MonthlyPrice = company.BillingPlan.MonthlyPrice,
                MaxFacilities = company.BillingPlan.MaxFacilities,
                MaxUsersCount = company.BillingPlan.MaxUsersCount,
                HasApiAccess = company.BillingPlan.HasApiAccess,
                HasIotIntegration = company.BillingPlan.HasIotIntegration,
                HasAiPrediction = company.BillingPlan.HasAiPrediction
            });
        }

        // BillingPlan yoksa aktif lisansa bak
        var license = await _dbContext.Licenses
            .FirstOrDefaultAsync(l => l.CompanyId == companyId && l.IsActive && l.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);

        if (license is not null)
        {
            return Result<BillingPlanDto>.Success(new BillingPlanDto
            {
                Name = license.PlanName,
                MonthlyPrice = 0,
                MaxFacilities = license.MaxFacilities,
                MaxUsersCount = license.MaxUsers,
                HasApiAccess = license.PlanName != "Free",
                HasIotIntegration = license.PlanName == "Enterprise",
                HasAiPrediction = license.PlanName == "Enterprise" || license.PlanName == "Professional"
            });
        }

        // Hiçbiri yoksa Free
        return Result<BillingPlanDto>.Success(new BillingPlanDto
        {
            Name = "Free",
            MonthlyPrice = 0,
            MaxFacilities = 1,
            MaxUsersCount = 3,
            HasApiAccess = false,
            HasIotIntegration = false,
            HasAiPrediction = false
        });
    }

    public async Task<Result> UpgradePlanAsync(Guid companyId, UpgradePlanRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company is null)
            return Result.Failure("Şirket bulunamadı.");

        var plan = await _planRepository.GetByIdAsync(request.NewPlanId, cancellationToken);
        if (plan is null)
            return Result.Failure("Plan bulunamadı.");

        company.BillingPlanId = plan.Id;
        _companyRepository.Update(company);
        await _companyRepository.SaveChangesAsync(cancellationToken);

        // Fatura oluştur
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Amount = plan.MonthlyPrice,
            Currency = "TRY",
            Status = "Pending", // Ödeme entegrasyonu yok; ödeme alınmadan "Paid" yazılmaz.
            InvoiceDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _invoiceRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<InvoiceDto>>> ListInvoicesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.ListByCompanyAsync(companyId, cancellationToken);
        var dtos = invoices.Select(i => new InvoiceDto
        {
            Id = i.Id,
            Amount = i.Amount,
            Currency = i.Currency,
            Status = i.Status,
            InvoiceDate = i.InvoiceDate,
            PdfUrl = i.PdfUrl
        }).ToList();

        return Result<IReadOnlyList<InvoiceDto>>.Success(dtos);
    }
}
