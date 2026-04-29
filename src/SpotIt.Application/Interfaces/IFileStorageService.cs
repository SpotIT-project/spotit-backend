using Microsoft.AspNetCore.Http;

namespace SpotIt.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file, CancellationToken ct = default);
    void Delete(string relativeUrl);
}
