using FluentValidation;

namespace GreenOps.Application.EmissionEntries.Commands;

public class CreateEmissionEntryCommandValidator : AbstractValidator<CreateEmissionEntryCommand>
{
    public CreateEmissionEntryCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty().WithMessage("Tesis seçilmelidir.");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Kategori seçilmelidir.");
        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Değer 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(EmissionLimits.MaxValue).WithMessage("Değer 1.000.000'u geçemez.");
        RuleFor(x => x.EvidenceUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.EvidenceUrl));
        RuleFor(x => x.PeriodEnd)
            .GreaterThanOrEqualTo(x => x.PeriodStart!.Value)
            .When(x => x.PeriodStart.HasValue && x.PeriodEnd.HasValue)
            .WithMessage("Dönem bitişi, dönem başlangıcından önce olamaz.");
        RuleFor(x => x.PeriodStart)
            .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow)
            .When(x => x.PeriodStart.HasValue)
            .WithMessage("Dönem başlangıcı gelecekte olamaz.");
    }
}

public static class EmissionLimits
{
    public const decimal MaxValue = 1_000_000m;
}
