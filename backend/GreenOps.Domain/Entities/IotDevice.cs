namespace GreenOps.Domain.Entities;

public class IotDevice
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;      // ElectricMeter, GasMeter, WaterMeter
    public string SerialNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Facility? Facility { get; set; }
    public ICollection<IotMeterReading> Readings { get; set; } = new List<IotMeterReading>();
}
