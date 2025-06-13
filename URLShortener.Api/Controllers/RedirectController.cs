using Microsoft.AspNetCore.Mvc;
using URLShortener.Infrastructure.Services;

namespace URLShortener.Api.Controllers
{
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly IUrlService _urlService;
        private readonly ILogger<RedirectController> _logger;

        public RedirectController(IUrlService urlService, ILogger<RedirectController> logger)
        {
            _urlService = urlService;
            _logger = logger;
        }

        [HttpGet("/go/{shortCode}")]
        public async Task<IActionResult> RedirectToUrl(string shortCode)
        {
            try
            {
                var originalUrl = await _urlService.GetOriginalUrlAsync(shortCode);
                if (originalUrl == null)
                {
                    return NotFound("Short URL not found or has expired");
                }

                if (!Uri.IsWellFormedUriString(originalUrl, UriKind.Absolute))
                {
                    _logger.LogWarning("Invalid URL format for short code {ShortCode}: {OriginalUrl}", shortCode, originalUrl);
                    return BadRequest("Invalid URL format");
                }

                var userAgent = Request.Headers.UserAgent.ToString();
                var ipAddress = GetClientIpAddress();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _urlService.RecordClickAsync(shortCode, userAgent, ipAddress);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to record click analytics for {ShortCode}", shortCode);
                    }
                });

                return Redirect(originalUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing redirect for short code {ShortCode}", shortCode);
                return StatusCode(500, "Internal server error");
            }
        }

        private string GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }
            return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}