namespace GreenOps.Domain.Entities;

public class ConversionFactor
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    /// <summary>Katsayının ait olduğu hesaplama yılı (kaynak belgedeki dönem).</summary>
    public int Year { get; set; }
    /// <summary>kgCO2e / kategori birimi.</summary>
    public decimal Factor { get; set; }
    /// <summary>Kaynak belge (kurum, belge no, tablo/satır). Kaynaksız değerler bunu açıkça belirtir.</summary>
    public string? Source { get; set; }
    public string? SourceUrl { get; set; }
    /// <summary>Resmi kaynağa dayanıyor mu? Kaynaksız yer tutucular false.</summary>
    public bool IsVerified { get; set; }
    /// <summary>false ise yeni hesaplamalarda kullanılmaz; eski kayıtların denetim izi için silinmez.</summary>
    public bool IsActive { get; set; } = true;

    public EmissionCategory? Category { get; set; }
}
