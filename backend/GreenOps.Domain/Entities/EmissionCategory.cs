namespace GreenOps.Domain.Entities;

public class EmissionCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public Guid? ScopeId { get; set; }

    public Scope? Scope { get; set; }
    public ICollection<ConversionFactor> ConversionFactors { get; set; } = new List<ConversionFactor>();
    public ICollection<EmissionEntry> EmissionEntries { get; set; } = new List<EmissionEntry>();
}
