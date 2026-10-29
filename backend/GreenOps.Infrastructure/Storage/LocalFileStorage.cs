using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using Microsoft.Extensions.Configuration;

namespace GreenOps.Infrastructure.Storage;

/// <summary>
/// Dosyaları web kökü dışında saklar; istemciye yol değil, yetki kontrollü indirme ucu verilir.
/// Kök dizin "Storage:Root" ile değiştirilebilir (varsayılan: çalışma dizini/uploads).
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private static readonly Dictionary<FileCategory, (string Folder, long MaxBytes, string[] Extensions)> Rules = new()
    {
        [FileCategory.Document] = ("documents", 10 * 1024 * 1024, new[] { ".pdf", ".png", ".jpg", ".jpeg", ".csv", ".xlsx", ".xls" }),
        [FileCategory.Avatar] = ("avatars", 2 * 1024 * 1024, new[] { ".png", ".jpg", ".jpeg", ".webp" }),
    };

    private readonly string _root;

    public LocalFileStorage(IConfiguration configuration)
    {
        var configured = configuration["Storage:Root"];
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), "uploads")
            : configured);
    }

    public async Task<Result<string>> SaveAsync(FileCategory category, string originalFileName, long length, Stream content, CancellationToken cancellationToken = default)
    {
        var rule = Rules[category];
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();

        if (!rule.Extensions.Contains(ext))
            return Result<string>.Failure($"İzin verilmeyen dosya türü. Desteklenenler: {string.Join(", ", rule.Extensions)}");
        if (length <= 0)
            return Result<string>.Failure("Dosya boş.");
        if (length > rule.MaxBytes)
            return Result<string>.Failure($"Dosya en fazla {rule.MaxBytes / (1024 * 1024)} MB olabilir.");

        var folder = Path.Combine(_root, rule.Folder);
        Directory.CreateDirectory(folder);

        var storedPath = $"{rule.Folder}/{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_root, storedPath);

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew))
        {
            // Bildirilen uzunluktan fazlasını yazma (sahte Content-Length'e karşı).
            var buffer = new byte[81920];
            long written = 0;
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                written += read;
                if (written > rule.MaxBytes)
                {
                    fs.Close();
                    File.Delete(fullPath);
                    return Result<string>.Failure("Dosya boyut sınırını aşıyor.");
                }
                await fs.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }

        return Result<string>.Success(storedPath);
    }

    public string? ResolvePath(string storedPath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, storedPath));
        return fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath)
            ? fullPath
            : null;
    }

    public void Delete(string storedPath)
    {
        var fullPath = ResolvePath(storedPath);
        if (fullPath is not null)
            File.Delete(fullPath);
    }
}
