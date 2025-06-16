namespace URLShortener.Application.Dtos;
public class UrlAnalyticsDto
    {
        public string ShortCode { get; set; } = string.Empty;
        public DateTime ClickDate { get; set; }
        public DateTime ClickTimestamp { get; set; }
        public string UserAgent { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string BrowserName { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public bool IsMobile { get; set; }
        public UrlAnalyticsDto() { }
        public UrlAnalyticsDto(
            string shortCode,
            DateTime clickDate,
            DateTime clickTimestamp,
            string userAgent,
            string ipAddress,
            string browserName,
            string operatingSystem,
            bool isMobile)
        {
            ShortCode = shortCode;
            ClickDate = clickDate;
            ClickTimestamp = clickTimestamp;
            UserAgent = userAgent;
            IpAddress = ipAddress;
            BrowserName = browserName;
            OperatingSystem = operatingSystem;
            IsMobile = isMobile;
        }
    }

