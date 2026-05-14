using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SpotIt.Application.Interfaces;

namespace SpotIt.Infrastructure.Services;

public class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private const string UploadFolder = "uploads";

    public async Task<string> SaveAsync(IFormFile file, CancellationToken ct = default)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsPath = Path.Combine(webRoot, UploadFolder);
        Directory.CreateDirectory(uploadsPath);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsPath, fileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);

        return $"/{UploadFolder}/{fileName}";
    }

    public void Delete(string relativeUrl)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, relativeUrl.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
