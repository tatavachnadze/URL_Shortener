using Microsoft.Extensions.Logging;
using URLShortener.Application.Dtos;
using URLShortener.Application.Extensions;
using URLShortener.Application.Services;
using URLShortener.Domain.Models;

namespace URLShortener.Infrastructure.Services
{
    
    public class UrlService : IUrlService
    {
        private readonly ICassandraService _cassandraService;
        private readonly IBase62Encoder _encoder;
        private readonly ILogger<UrlService> _logger;

        public UrlService(ICassandraService cassandraService, IBase62Encoder encoder, ILogger<UrlService> logger)
        {
            _cassandraService = cassandraService;
            _encoder = encoder;
            _logger = logger;
        }

        public async Task<UrlResponse?> CreateUrlAsync(CreateUrlRequest request, string baseUrl)
        {
            if (!request.IsValid)
            {
                _logger.LogWarning("Invalid create URL request: OriginalUrl is empty");
                return null;
            }

            string shortCode;

            if (request.HasCustomAlias)
            {
                shortCode = request.CustomAlias!;
                if (await _cassandraService.ExistsAsync(shortCode))
                {
                    _logger.LogWarning("Custom alias {Alias} already exists", shortCode);
                    return null;
                }
            }
            else
            {
                shortCode = _encoder.GenerateShortCode();
                while (await _cassandraService.ExistsAsync(shortCode))
                {
                    shortCode = _encoder.GenerateShortCode();
                }
            }

            // Use business constructor for new URL creation
            var url = new Url(shortCode, request.OriginalUrl, request.ExpiresAt, request.HasCustomAlias);

            var success = await _cassandraService.CreateUrlAsync(url);
            if (!success)
            {
                _logger.LogError("Failed to create URL in database for short code {ShortCode}", shortCode);
                return null;
            }

            _logger.LogInformation("Successfully created URL with short code {ShortCode}", shortCode);

            // Use mapping extension for response creation
            return url.ToResponse(baseUrl);
        }

        public async Task<UrlDetailsResponse?> GetUrlDetailsAsync(string shortCode, string baseUrl)
        {
            var url = await _cassandraService.GetUrlAsync(shortCode);
            if (url == null)
            {
                _logger.LogWarning("URL not found for short code {ShortCode}", shortCode);
                return null;
            }

            var analytics = await _cassandraService.GetAnalyticsAsync(shortCode);

            _logger.LogInformation("Retrieved URL details for {ShortCode} with {AnalyticsCount} analytics records",
                shortCode, analytics.Count);

            // Use mapping extension for detailed response creation
            return url.ToDetailsResponse(baseUrl, analytics);
        }

        public async Task<string?> GetOriginalUrlAsync(string shortCode)
        {
            _logger.LogInformation("Looking up original URL for: {ShortCode}", shortCode);

            var url = await _cassandraService.GetUrlAsync(shortCode);

            if (url == null)
            {
                _logger.LogWarning("URL not found in database: {ShortCode}", shortCode);
                return null;
            }

            // Use domain model business logic method
            if (!url.CanBeAccessed())
            {
                _logger.LogWarning("URL cannot be accessed: {ShortCode} - Active: {IsActive}, Expired: {IsExpired}",
                    shortCode, url.IsActive, url.IsExpired);
                return null;
            }

            _logger.LogInformation("Successfully retrieved original URL for {ShortCode}", shortCode);
            return url.OriginalUrl;
        }

        public async Task<bool> UpdateUrlAsync(string shortCode, UpdateUrlRequest request)
        {
            if (!request.HasAnyUpdates)
            {
                _logger.LogWarning("Update request for {ShortCode} contains no updates", shortCode);
                return false;
            }

            var success = await _cassandraService.UpdateUrlAsync(shortCode, request);

            if (success)
            {
                _logger.LogInformation("Successfully updated URL {ShortCode}", shortCode);
            }
            else
            {
                _logger.LogWarning("Failed to update URL {ShortCode}", shortCode);
            }

            return success;
        }

        public async Task<bool> DeleteUrlAsync(string shortCode)
        {
            var success = await _cassandraService.DeleteUrlAsync(shortCode);

            if (success)
            {
                _logger.LogInformation("Successfully deleted URL {ShortCode}", shortCode);
            }
            else
            {
                _logger.LogWarning("Failed to delete URL {ShortCode}", shortCode);
            }

            return success;
        }

        public async Task<bool> RecordClickAsync(string shortCode, string userAgent, string ipAddress)
        {
            try
            {
                // Increment click counter
                await _cassandraService.IncrementClickCountAsync(shortCode);

                // Use business constructor for analytics creation
                var analytics = new UrlAnalytics(shortCode, userAgent, ipAddress);

                // Store analytics data
                await _cassandraService.AddAnalyticsAsync(analytics);

                _logger.LogDebug("Successfully recorded click for {ShortCode} from IP {IpAddress}",
                    shortCode, ipAddress);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record click for {ShortCode}", shortCode);
                return false;
            }
        }
    }
}
