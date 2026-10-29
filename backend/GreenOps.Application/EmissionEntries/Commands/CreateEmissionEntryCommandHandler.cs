using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using MediatR;

namespace GreenOps.Application.EmissionEntries.Commands;

public class CreateEmissionEntryCommandHandler : IRequestHandler<CreateEmissionEntryCommand, Result<Guid>>
{
    private readonly IEmissionEntryService _emissionEntryService;

    public CreateEmissionEntryCommandHandler(IEmissionEntryService emissionEntryService)
    {
        _emissionEntryService = emissionEntryService;
    }

    public async Task<Result<Guid>> Handle(CreateEmissionEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = new EmissionEntry
        {
            FacilityId = request.FacilityId,
            CategoryId = request.CategoryId,
            Value = request.Value,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            EvidenceUrl = request.EvidenceUrl,
            CreatedByUserId = request.CreatedByUserId
        };
        return await _emissionEntryService.CreateAsync(entry, request.CompanyScope, cancellationToken);
    }
}
