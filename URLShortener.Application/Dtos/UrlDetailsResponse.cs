namespace URLShortener.Application.Dtos;
public class UrlDetailsResponse : UrlResponse
    {
        public List<UrlAnalyticsDto> RecentClicks { get; set; } = new();

        public UrlDetailsResponse() { }
        public UrlDetailsResponse(UrlResponse baseResponse, List<UrlAnalyticsDto> recentClicks)
            : base(
                baseResponse.ShortCode,
                baseResponse.OriginalUrl,
                baseResponse.ShortUrl,
                baseResponse.CreatedAt,
                baseResponse.ExpiresAt,
                baseResponse.ClickCount,
                baseResponse.IsActive,
                baseResponse.IsExpired,
                baseResponse.IsPermanent)
        {
            RecentClicks = recentClicks;
        }
        public int TotalRecentClicks => RecentClicks.Count;
        public DateTime? LastClickTime => RecentClicks.FirstOrDefault()?.ClickTimestamp;
    }

