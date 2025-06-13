using Cassandra;
using Microsoft.Extensions.Logging;
using URLShortener.Core.Models;

namespace URLShortener.Infrastructure.Data;

public interface ICassandraService
{
    Task<Url?> GetUrlAsync(string shortCode);
    Task<bool> CreateUrlAsync(Url url);
    Task<bool> UpdateUrlAsync(string shortCode, UpdateUrlRequest request);
    Task<bool> DeleteUrlAsync(string shortCode);
    Task<bool> IncrementClickCountAsync(string shortCode);
    Task<bool> AddAnalyticsAsync(UrlAnalytics analytics);
    Task<List<UrlAnalytics>> GetAnalyticsAsync(string shortCode, int limit = 10);
    Task<bool> ExistsAsync(string shortCode);
    Task<List<Url>> GetExpiredUrlsAsync();
    Task<bool> DeactivateUrlAsync(string shortCode);
}

public class CassandraService : ICassandraService
{
    private readonly ISession _session;
    private readonly ILogger<CassandraService> _logger;
    private PreparedStatement? _getUrlStatement;
    private PreparedStatement? _getClickCountStatement;
    private PreparedStatement? _insertUrlStatement;
    private PreparedStatement? _updateUrlStatement;
    private PreparedStatement? _deleteUrlStatement;
    private PreparedStatement? _incrementClickStatement;
    private PreparedStatement? _insertAnalyticsStatement;
    private PreparedStatement? _getAnalyticsStatement;
    private PreparedStatement? _existsStatement;
    private PreparedStatement? _getExpiredStatement;
    private PreparedStatement? _deactivateStatement;

    public CassandraService(ISession session, ILogger<CassandraService> logger)
    {
        _session = session;
        _logger = logger;
        PrepareStatements();
    }

    private void PrepareStatements()
    {
        _getUrlStatement = _session.Prepare(
            "SELECT short_code, original_url, created_at, expires_at, is_active, custom_alias FROM urls WHERE short_code = ?");

        _insertUrlStatement = _session.Prepare(
            "INSERT INTO urls (short_code, original_url, created_at, expires_at, is_active, custom_alias) VALUES (?, ?, ?, ?, ?, ?)");

        _updateUrlStatement = _session.Prepare(
            "UPDATE urls SET original_url = ?, expires_at = ? WHERE short_code = ?");

        _deleteUrlStatement = _session.Prepare(
            "DELETE FROM urls WHERE short_code = ?");

        _existsStatement = _session.Prepare(
            "SELECT short_code FROM urls WHERE short_code = ?");

        _getExpiredStatement = _session.Prepare(
            "SELECT short_code, original_url, created_at, expires_at, is_active, custom_alias FROM urls WHERE expires_at <= ? AND is_active = true ALLOW FILTERING");

        _deactivateStatement = _session.Prepare(
            "UPDATE urls SET is_active = false WHERE short_code = ?");

        _getClickCountStatement = _session.Prepare(
            "SELECT click_count FROM url_counters WHERE short_code = ?");

        _incrementClickStatement = _session.Prepare(
            "UPDATE url_counters SET click_count = click_count + 1 WHERE short_code = ?");

        _insertAnalyticsStatement = _session.Prepare(
            "INSERT INTO url_analytics (short_code, click_date, click_timestamp, user_agent, ip_address) VALUES (?, ?, ?, ?, ?)");

        _getAnalyticsStatement = _session.Prepare(
            "SELECT short_code, click_date, click_timestamp, user_agent, ip_address FROM url_analytics WHERE short_code = ? LIMIT ?");
    }

    public async Task<Url?> GetUrlAsync(string shortCode)
    {
        var row = await _session.ExecuteAsync(_getUrlStatement!.Bind(shortCode));
        var firstRow = row.FirstOrDefault();
        if (firstRow == null) return null;        
        var clickCount = 0L;
        try
        {
            var counterRow = await _session.ExecuteAsync(_getClickCountStatement!.Bind(shortCode));
            var counterFirstRow = counterRow.FirstOrDefault();
            if (counterFirstRow != null)
            {
                clickCount = counterFirstRow.GetValue<long>("click_count");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get click count for {ShortCode}", shortCode);
        }

        return new Url
        {
            ShortCode = firstRow.GetValue<string>("short_code"),
            OriginalUrl = firstRow.GetValue<string>("original_url"),
            CreatedAt = firstRow.GetValue<DateTime>("created_at"),
            ExpiresAt = firstRow.GetValue<DateTime?>("expires_at"),
            ClickCount = clickCount,
            IsActive = firstRow.GetValue<bool>("is_active"),
            CustomAlias = firstRow.GetValue<bool>("custom_alias")
        };
    }

    public async Task<bool> CreateUrlAsync(Url url)
    {
        try
        {
            await _session.ExecuteAsync(_insertUrlStatement!.Bind(
                url.ShortCode,
                url.OriginalUrl,
                url.CreatedAt,
                url.ExpiresAt,
                url.IsActive,
                url.CustomAlias));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create URL with short code {ShortCode}", url.ShortCode);
            return false;
        }
    }

    public async Task<bool> UpdateUrlAsync(string shortCode, UpdateUrlRequest request)
    {
        try
        {
            var existingUrl = await GetUrlAsync(shortCode);
            if (existingUrl == null) return false;

            await _session.ExecuteAsync(_updateUrlStatement!.Bind(
                request.OriginalUrl ?? existingUrl.OriginalUrl,
                request.ExpiresAt ?? existingUrl.ExpiresAt,
                shortCode));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update URL with short code {ShortCode}", shortCode);
            return false;
        }
    }

    public async Task<bool> DeleteUrlAsync(string shortCode)
    {
        try
        {
            await _session.ExecuteAsync(_deleteUrlStatement!.Bind(shortCode));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete URL with short code {ShortCode}", shortCode);
            return false;
        }
    }

    public async Task<bool> IncrementClickCountAsync(string shortCode)
    {
        try
        {
            await _session.ExecuteAsync(_incrementClickStatement!.Bind(shortCode));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment click count for {ShortCode}", shortCode);
            return false;
        }
    }

    public async Task<bool> AddAnalyticsAsync(UrlAnalytics analytics)
    {
        try
        {
            await _session.ExecuteAsync(_insertAnalyticsStatement!.Bind(
                analytics.ShortCode,
                analytics.ClickDate,
                analytics.ClickTimestamp,
                analytics.UserAgent,
                analytics.IpAddress));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add analytics for {ShortCode}", analytics.ShortCode);
            return false;
        }
    }

    public async Task<List<UrlAnalytics>> GetAnalyticsAsync(string shortCode, int limit = 10)
    {
        var rows = await _session.ExecuteAsync(_getAnalyticsStatement!.Bind(shortCode, limit));
        return rows.Select(row => new UrlAnalytics
        {
            ShortCode = row.GetValue<string>("short_code"),
            ClickDate = row.GetValue<DateTime>("click_date"),
            ClickTimestamp = row.GetValue<DateTime>("click_timestamp"),
            UserAgent = row.GetValue<string>("user_agent"),
            IpAddress = row.GetValue<string>("ip_address")
        }).ToList();
    }

    public async Task<bool> ExistsAsync(string shortCode)
    {
        var rows = await _session.ExecuteAsync(_existsStatement!.Bind(shortCode));
        return rows.Any();
    }

    public async Task<List<Url>> GetExpiredUrlsAsync()
    {
        var rows = await _session.ExecuteAsync(_getExpiredStatement!.Bind(DateTime.UtcNow));
        return rows.Select(row => new Url
        {
            ShortCode = row.GetValue<string>("short_code"),
            OriginalUrl = row.GetValue<string>("original_url"),
            CreatedAt = row.GetValue<DateTime>("created_at"),
            ExpiresAt = row.GetValue<DateTime?>("expires_at"),
            ClickCount = 0,
            IsActive = row.GetValue<bool>("is_active"),
            CustomAlias = row.GetValue<bool>("custom_alias")
        }).ToList();
    }

    public async Task<bool> DeactivateUrlAsync(string shortCode)
    {
        try
        {
            await _session.ExecuteAsync(_deactivateStatement!.Bind(shortCode));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate URL with short code {ShortCode}", shortCode);
            return false;
        }
    }
}