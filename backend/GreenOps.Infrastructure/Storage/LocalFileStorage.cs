using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using Microsoft.Extensions.Configuration;
using System.Text;

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
            var buffer = new byte[81920];
            long written = 0;
            var prefixLength = await content.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, 4096)), cancellationToken);
            if (!HasExpectedSignature(ext, buffer.AsSpan(0, prefixLength)))
            {
                fs.Close();
                File.Delete(fullPath);
                return Result<string>.Failure("Dosya içeriği uzantısıyla eşleşmiyor.");
            }

            await fs.WriteAsync(buffer.AsMemory(0, prefixLength), cancellationToken);
            written += prefixLength;

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

    private static bool HasExpectedSignature(string extension, ReadOnlySpan<byte> prefix)
    {
        return extension switch
        {
            ".png" => prefix.Length >= 8 && prefix[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => prefix.Length >= 3 && prefix[..3].SequenceEqual(new byte[] { 255, 216, 255 }),
            ".webp" => prefix.Length >= 12
                       && prefix[..4].SequenceEqual(Encoding.ASCII.GetBytes("RIFF"))
                       && prefix[8..12].SequenceEqual(Encoding.ASCII.GetBytes("WEBP")),
            ".pdf" => prefix.Length >= 5 && Encoding.ASCII.GetString(prefix[..5]) == "%PDF-",
            ".xls" => prefix.Length >= 8 && prefix[..8].SequenceEqual(new byte[] { 208, 207, 17, 224, 161, 177, 26, 225 }),
            ".xlsx" => prefix.Length >= 2 && prefix[..2].SequenceEqual(new byte[] { 80, 75 }),
            ".csv" => IsText(prefix),
            _ => false
        };
    }

    private static bool IsText(ReadOnlySpan<byte> prefix)
    {
        foreach (var value in prefix)
            if (value == 0) return false;
        return true;
    }
}
