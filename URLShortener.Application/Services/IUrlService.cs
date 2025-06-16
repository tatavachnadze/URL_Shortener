using URLShortener.Application.Dtos;

namespace URLShortener.Application.Services;

public interface IUrlService
{
    Task<UrlResponse?> CreateUrlAsync(CreateUrlRequest request, string baseUrl);
    Task<UrlDetailsResponse?> GetUrlDetailsAsync(string shortCode, string baseUrl);
    Task<string?> GetOriginalUrlAsync(string shortCode);
    Task<bool> UpdateUrlAsync(string shortCode, UpdateUrlRequest request);
    Task<bool> DeleteUrlAsync(string shortCode);
    Task<bool> RecordClickAsync(string shortCode, string userAgent, string ipAddress);
}
