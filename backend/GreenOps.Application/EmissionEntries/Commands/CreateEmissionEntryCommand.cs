using System.Text.Json.Serialization;
using GreenOps.Application.Common;
using MediatR;

namespace GreenOps.Application.EmissionEntries.Commands;

public class CreateEmissionEntryCommand : IRequest<Result<Guid>>
{
    public Guid FacilityId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Value { get; set; }
    public DateTimeOffset? PeriodStart { get; set; }
    public DateTimeOffset? PeriodEnd { get; set; }
    public string? EvidenceUrl { get; set; }

    // Sunucu tarafında token'dan doldurulur; istemci gövdesinden okunmaz.
    [JsonIgnore] public Guid CreatedByUserId { get; set; }
    [JsonIgnore] public Guid? CompanyScope { get; set; }
}
