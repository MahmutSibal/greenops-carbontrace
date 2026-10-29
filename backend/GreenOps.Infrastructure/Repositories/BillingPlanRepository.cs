using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;

namespace GreenOps.Infrastructure.Repositories;

public class BillingPlanRepository : RepositoryBase<BillingPlan>, IBillingPlanRepository
{
    public BillingPlanRepository(AppDbContext dbContext) : base(dbContext) { }
}
