using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using URLShortener.Core.Models;
using URLShortener.Core.Services;
using URLShortener.Infrastructure.Data;

namespace URLShortener.Infrastructure.Services
{
    public interface IUrlService
    {
        Task<UrlResponse?> CreateUrlAsync(CreateUrlRequest request, string baseUrl);
        Task<UrlDetailsResponse?> GetUrlDetailsAsync(string shortCode, string baseUrl);
        Task<string?> GetOriginalUrlAsync(string shortCode);
        Task<bool> UpdateUrlAsync(string shortCode, UpdateUrlRequest request);
        Task<bool> DeleteUrlAsync(string shortCode);
        Task<bool> RecordClickAsync(string shortCode, string userAgent, string ipAddress);
    }

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
            string shortCode;
            if (!string.IsNullOrEmpty(request.CustomAlias))
            {
                shortCode = request.CustomAlias;
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

            var url = new Url
            {
                ShortCode = shortCode,
                OriginalUrl = request.OriginalUrl,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                IsActive = true,
                CustomAlias = !string.IsNullOrEmpty(request.CustomAlias)
            };

            var success = await _cassandraService.CreateUrlAsync(url);
            if (!success)
            {
                _logger.LogError("Failed to create URL in database");
                return null;
            }

            return new UrlResponse
            {
                ShortCode = url.ShortCode,
                OriginalUrl = url.OriginalUrl,
                ShortUrl = $"{baseUrl.TrimEnd('/')}/{url.ShortCode}",
                CreatedAt = url.CreatedAt,
                ExpiresAt = url.ExpiresAt,
                ClickCount = 0,
                IsActive = url.IsActive,
                IsExpired = url.IsExpired,
                IsPermanent = url.IsPermanent
            };
        }

        public async Task<UrlDetailsResponse?> GetUrlDetailsAsync(string shortCode, string baseUrl)
        {
            var url = await _cassandraService.GetUrlAsync(shortCode);
            if (url == null) return null;

            var analytics = await _cassandraService.GetAnalyticsAsync(shortCode);

            return new UrlDetailsResponse
            {
                ShortCode = url.ShortCode,
                OriginalUrl = url.OriginalUrl,
                ShortUrl = $"{baseUrl.TrimEnd('/')}/{url.ShortCode}",
                CreatedAt = url.CreatedAt,
                ExpiresAt = url.ExpiresAt,
                ClickCount = url.ClickCount,
                IsActive = url.IsActive,
                IsExpired = url.IsExpired,
                IsPermanent = url.IsPermanent,
                RecentClicks = analytics
            };
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

            _logger.LogInformation("Found URL: Active={IsActive}, Expired={IsExpired}, OriginalUrl={OriginalUrl}",
                url.IsActive, url.IsExpired, url.OriginalUrl);

            if (!url.IsActive || url.IsExpired)
            {
                _logger.LogWarning("URL is inactive or expired: {ShortCode}", shortCode);
                return null;
            }

            return url.OriginalUrl;
        }

        public async Task<bool> UpdateUrlAsync(string shortCode, UpdateUrlRequest request)
        {
            return await _cassandraService.UpdateUrlAsync(shortCode, request);
        }

        public async Task<bool> DeleteUrlAsync(string shortCode)
        {
            return await _cassandraService.DeleteUrlAsync(shortCode);
        }

        public async Task<bool> RecordClickAsync(string shortCode, string userAgent, string ipAddress)
        {
            try
            {
                await _cassandraService.IncrementClickCountAsync(shortCode);
                var analytics = new UrlAnalytics
                {
                    ShortCode = shortCode,
                    ClickDate = DateTime.UtcNow.Date,
                    ClickTimestamp = DateTime.UtcNow,
                    UserAgent = userAgent,
                    IpAddress = ipAddress
                };

                await _cassandraService.AddAnalyticsAsync(analytics);
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
