using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using URLShortener.Infrastructure.Data;

namespace URLShortener.Infrastructure.HostedServices
{
    public class ExpirationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpirationCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30);

        public ExpirationCleanupService(IServiceProvider serviceProvider, ILogger<ExpirationCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Expiration cleanup service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredUrls();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during URL cleanup");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Expiration cleanup service stopped");
        }

        private async Task CleanupExpiredUrls()
        {
            using var scope = _serviceProvider.CreateScope();
            var cassandraService = scope.ServiceProvider.GetRequiredService<ICassandraService>();

            try
            {
                var expiredUrls = await cassandraService.GetExpiredUrlsAsync();

                if (expiredUrls.Count == 0)
                {
                    _logger.LogDebug("No expired URLs found during cleanup");
                    return;
                }

                _logger.LogInformation("Found {Count} expired URLs to clean up", expiredUrls.Count);

                foreach (var url in expiredUrls)
                {
                    await cassandraService.DeactivateUrlAsync(url.ShortCode);
                    _logger.LogDebug("Deactivated expired URL: {ShortCode}", url.ShortCode);
                }

                _logger.LogInformation("Successfully cleaned up {Count} expired URLs", expiredUrls.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired URLs");
            }
        }
    }
}
