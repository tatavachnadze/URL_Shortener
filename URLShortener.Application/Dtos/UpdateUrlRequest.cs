namespace URLShortener.Application.Dtos;

    public class UpdateUrlRequest
    {
        public string? OriginalUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UpdateUrlRequest() { }
        public UpdateUrlRequest(string originalUrl)
        {
            OriginalUrl = originalUrl;
        }
        public UpdateUrlRequest(DateTime expiresAt)
        {
            ExpiresAt = expiresAt;
        }

        public UpdateUrlRequest(string? originalUrl, DateTime? expiresAt)
        {
            OriginalUrl = originalUrl;
            ExpiresAt = expiresAt;
        }

        public bool HasOriginalUrlUpdate => !string.IsNullOrWhiteSpace(OriginalUrl);
        public bool HasExpirationUpdate => ExpiresAt.HasValue;
        public bool IsExpirationValid => !ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow;
        public bool HasAnyUpdates => HasOriginalUrlUpdate || HasExpirationUpdate;
    }

