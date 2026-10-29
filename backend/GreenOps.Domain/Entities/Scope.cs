namespace GreenOps.Domain.Entities;

public class Scope
{
    public Guid Id { get; set; }
    public int Number { get; set; }             // 1, 2, 3
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<EmissionCategory> Categories { get; set; } = new List<EmissionCategory>();
}
