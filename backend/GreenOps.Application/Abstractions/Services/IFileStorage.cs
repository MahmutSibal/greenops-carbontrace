using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public enum FileCategory
{
    Document,
    Avatar
}

public interface IFileStorage
{
    /// <summary>Uzantı ve boyutu doğrular, dosyayı rastgele bir adla kaydeder ve göreli yolunu döner.</summary>
    Task<Result<string>> SaveAsync(FileCategory category, string originalFileName, long length, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Göreli yolu fiziksel yola çevirir; depolama kökü dışına çıkan yollar için null döner.</summary>
    string? ResolvePath(string storedPath);

    void Delete(string storedPath);
}
