using URLShortener.Application.Dtos;
using URLShortener.Domain.Models;

namespace URLShortener.Application.Services;
    public interface ICassandraService
    {
        Task<Url?> GetUrlAsync(string shortCode);
        Task<bool> CreateUrlAsync(Url url);
        Task<bool> UpdateUrlAsync(string shortCode, UpdateUrlRequest request);
        Task<bool> DeleteUrlAsync(string shortCode);
        Task<bool> IncrementClickCountAsync(string shortCode);
        Task<bool> AddAnalyticsAsync(UrlAnalytics analytics);
        Task<List<UrlAnalytics>> GetAnalyticsAsync(string shortCode, int limit = 10);
        Task<bool> ExistsAsync(string shortCode);
        Task<List<Url>> GetExpiredUrlsAsync();
        Task<bool> DeactivateUrlAsync(string shortCode);
    }

