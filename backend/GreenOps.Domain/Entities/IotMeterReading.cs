namespace GreenOps.Domain.Entities;

public class IotMeterReading
{
    public Guid Id { get; set; }
    public Guid IotDeviceId { get; set; }
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;         // kWh, m³ vb.
    public DateTimeOffset ReadingTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public IotDevice? IotDevice { get; set; }
}
