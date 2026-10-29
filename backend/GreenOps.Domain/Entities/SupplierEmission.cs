namespace GreenOps.Domain.Entities;

public class SupplierEmission
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public int Year { get; set; }
    public decimal Value { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Supplier? Supplier { get; set; }
}
