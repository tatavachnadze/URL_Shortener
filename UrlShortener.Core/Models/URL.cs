namespace URLShortener.Domain.Models;

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
    public Url(string shortCode, string originalUrl, DateTime? expiresAt, bool isCustomAlias)
    {
        ShortCode = shortCode;
        OriginalUrl = originalUrl;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        ClickCount = 0;
        IsActive = true;
        CustomAlias = isCustomAlias;
    }
    public void IncrementClickCount()
    {
        ClickCount++;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void UpdateDetails(string? newOriginalUrl, DateTime? newExpiresAt)
    {
        if (!string.IsNullOrEmpty(newOriginalUrl))
            OriginalUrl = newOriginalUrl;

        if (newExpiresAt.HasValue)
            ExpiresAt = newExpiresAt;
    }

    public bool CanBeAccessed()
    {
        return IsActive && !IsExpired;
    }
}

    


