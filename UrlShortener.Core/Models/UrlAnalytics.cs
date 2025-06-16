namespace URLShortener.Domain.Models;
    public class UrlAnalytics
    {
        public string ShortCode { get; set; } = string.Empty;
        public DateTime ClickDate { get; set; }
        public DateTime ClickTimestamp { get; set; }
        public string UserAgent { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string BrowserName => ExtractBrowserName(UserAgent);
        public string OperatingSystem => ExtractOperatingSystem(UserAgent);
        public bool IsMobile => UserAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase);

        public UrlAnalytics() { }
        public UrlAnalytics(
            string shortCode,
            DateTime clickDate,
            DateTime clickTimestamp,
            string userAgent,
            string ipAddress)
        {
            ShortCode = shortCode;
            ClickDate = clickDate;
            ClickTimestamp = clickTimestamp;
            UserAgent = userAgent;
            IpAddress = ipAddress;
        }
        public UrlAnalytics(string shortCode, string userAgent, string ipAddress)
        {
            ShortCode = shortCode;
            ClickDate = DateTime.UtcNow.Date;
            ClickTimestamp = DateTime.UtcNow;
            UserAgent = userAgent;
            IpAddress = ipAddress;
        }
        private string ExtractBrowserName(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";

            if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase)) return "Chrome";
            if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase)) return "Firefox";
            if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase)) return "Safari";
            if (userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase)) return "Edge";

            return "Other";
        }

        private string ExtractOperatingSystem(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";

            if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase)) return "Windows";
            if (userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase)) return "macOS";
            if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "Linux";
            if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase)) return "Android";
            if (userAgent.Contains("iOS", StringComparison.OrdinalIgnoreCase)) return "iOS";

            return "Other";
        }
    }

