using URLShortener.Domain.Models;
using URLShortener.Application.Dtos;
using URLShortener.Domain.Models;

namespace URLShortener.Application.Extensions;

public static class MappingExtensions
{
    public static UrlResponse ToResponse(this Url url, string baseUrl)
    {
        return new UrlResponse(
            url.ShortCode,
            url.OriginalUrl,
            $"{baseUrl.TrimEnd('/')}/go/{url.ShortCode}",
            url.CreatedAt,
            url.ExpiresAt,
            url.ClickCount,
            url.IsActive,
            url.IsExpired,
            url.IsPermanent
        );
    }
    public static UrlAnalyticsDto ToDto(this UrlAnalytics analytics)
    {
        return new UrlAnalyticsDto(
            analytics.ShortCode,
            analytics.ClickDate,
            analytics.ClickTimestamp,
            analytics.UserAgent,
            analytics.IpAddress,
            analytics.BrowserName,
            analytics.OperatingSystem,
            analytics.IsMobile
        );
    }
    public static UrlDetailsResponse ToDetailsResponse(this Url url, string baseUrl, List<UrlAnalytics> analytics)
    {
        var baseResponse = url.ToResponse(baseUrl);
        var analyticsDto = analytics.Select(a => a.ToDto()).ToList();
        return new UrlDetailsResponse(baseResponse, analyticsDto);
    }
}