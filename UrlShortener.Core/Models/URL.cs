namespace URLShortener.Core.Models;

public class Url
{
    public string ShortCode { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long ClickCount { get; set; }
    public bool IsActive { get; set; } = true;
    public bool CustomAlias { get; set; }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    public bool IsPermanent => !ExpiresAt.HasValue;

    public Url() {}

    public Url(
        string shortCode,
        string originalUrl,
        DateTime createdAt,
        DateTime? expiresAt,
        long clickCount,
        bool isActive,
        bool customAlias)
    {
        ShortCode = shortCode;
        OriginalUrl = originalUrl;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        ClickCount = clickCount;
        IsActive = isActive;
        CustomAlias = customAlias;
    }
}

public class UrlAnalytics
{
    public string ShortCode { get; set; } = string.Empty;
    public DateTime ClickDate { get; set; }
    public DateTime ClickTimestamp { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class CreateUrlRequest
{
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string? CustomAlias { get; set; }
}

public class UpdateUrlRequest
{
    public string? OriginalUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class UrlResponse
{
    public string ShortCode { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long ClickCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool IsPermanent { get; set; }
}

public class UrlDetailsResponse : UrlResponse
{
    public List<UrlAnalytics> RecentClicks { get; set; } = new();
}
