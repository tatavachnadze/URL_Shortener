namespace URLShortener.Application.Dtos;

public class CreateUrlRequest
{
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string? CustomAlias { get; set; }

    public CreateUrlRequest() { }
    public CreateUrlRequest(string originalUrl)
    {
        OriginalUrl = originalUrl;
    }
    public CreateUrlRequest(string originalUrl, DateTime? expiresAt, string? customAlias)
    {
        OriginalUrl = originalUrl;
        ExpiresAt = expiresAt;
        CustomAlias = customAlias;
    }

    public bool HasCustomAlias => !string.IsNullOrWhiteSpace(CustomAlias);
    public bool HasExpiration => ExpiresAt.HasValue;
    public bool IsExpirationValid => !ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow;
    public bool IsValid => !string.IsNullOrWhiteSpace(OriginalUrl);
}

