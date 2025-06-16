namespace URLShortener.Application.Dtos;
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
        public UrlResponse() { }
        public UrlResponse(
            string shortCode,
            string originalUrl,
            string shortUrl,
            DateTime createdAt,
            DateTime? expiresAt,
            long clickCount,
            bool isActive,
            bool isExpired,
            bool isPermanent)
        {
            ShortCode = shortCode;
            OriginalUrl = originalUrl;
            ShortUrl = shortUrl;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
            ClickCount = clickCount;
            IsActive = isActive;
            IsExpired = isExpired;
            IsPermanent = isPermanent;
        }
        public string Status => IsActive ? (IsExpired ? "Expired" : "Active") : "Inactive";
    }

