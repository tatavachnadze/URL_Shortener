-- Create keyspace
CREATE KEYSPACE IF NOT EXISTS urlshortener
WITH REPLICATION = {
    'class': 'SimpleStrategy',
    'replication_factor': 1
};

USE urlshortener;

-- Primary URLs table (without counter)
CREATE TABLE IF NOT EXISTS urls (
    short_code TEXT PRIMARY KEY,
    original_url TEXT,
    created_at TIMESTAMP,
    expires_at TIMESTAMP,
    is_active BOOLEAN,
    custom_alias BOOLEAN
);

-- Separate counter table
CREATE TABLE IF NOT EXISTS url_counters (
    short_code TEXT PRIMARY KEY,
    click_count COUNTER
);

-- Analytics table for click tracking
CREATE TABLE IF NOT EXISTS url_analytics (
    short_code TEXT,
    click_date DATE,
    click_timestamp TIMESTAMP,
    user_agent TEXT,
    ip_address TEXT,
    PRIMARY KEY (short_code, click_date, click_timestamp)
) WITH CLUSTERING ORDER BY (click_date DESC, click_timestamp DESC);

-- Index for querying by expiration
CREATE INDEX IF NOT EXISTS idx_urls_expires_at ON urls (expires_at);
CREATE INDEX IF NOT EXISTS idx_urls_is_active ON urls (is_active);