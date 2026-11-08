using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public enum FileCategory
{
    Document,
    Avatar
}

public interface IFileStorage
{
    /// <summary>Uzantı, içerik imzası ve boyutu doğrular; dosyayı rastgele adla kaydedip göreli yolu döner.</summary>
    Task<Result<string>> SaveAsync(FileCategory category, string originalFileName, long length, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Göreli yolu fiziksel yola çevirir; depolama kökü dışına çıkan yollar için null döner.</summary>
    string? ResolvePath(string storedPath);

    void Delete(string storedPath);
}
