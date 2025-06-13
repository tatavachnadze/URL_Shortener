
using Cassandra;
using FluentValidation;
using URLShortener.Core.Models;
using URLShortener.Core.Services;
using URLShortener.Core.Validators;
using URLShortener.Infrastructure.Data;
using URLShortener.Infrastructure.HostedServices;
using URLShortener.Infrastructure.Services;

namespace URLShortener.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddLogging();

            builder.Services.AddScoped<IValidator<CreateUrlRequest>, CreateUrlRequestValidator>();
            builder.Services.AddScoped<IValidator<UpdateUrlRequest>, UpdateUrlRequestValidator>();

            builder.Services.AddSingleton<IBase62Encoder, Base62Encoder>();
            builder.Services.AddScoped<IUrlService, UrlService>();
            builder.Services.AddScoped<ICassandraService, CassandraService>();
          
            builder.Services.AddSingleton<Cassandra.ISession>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var logger = provider.GetRequiredService<ILogger<Program>>();

                var contactPoints = configuration.GetValue<string>("Cassandra:ContactPoints")?.Split(',')
                                   ?? new[] { "localhost" };
                var port = configuration.GetValue<int>("Cassandra:Port", 9042);
                var keyspace = configuration.GetValue<string>("Cassandra:Keyspace", "urlshortener");

                logger.LogInformation("Connecting to Cassandra at {ContactPoints}:{Port}", string.Join(",", contactPoints), port);

                var cluster = Cluster.Builder()
                    .AddContactPoints(contactPoints)
                    .WithPort(port)
                    .WithReconnectionPolicy(new ExponentialReconnectionPolicy(1000, 10000))
                    .WithRetryPolicy(new DefaultRetryPolicy())
                    .Build();

                var session = cluster.Connect();

                session.Execute($@"
        CREATE KEYSPACE IF NOT EXISTS {keyspace}
        WITH REPLICATION = {{
            'class': 'SimpleStrategy',
            'replication_factor': 1
        }}");
                session.Execute($"USE {keyspace}");

                session.Execute(@"
        CREATE TABLE IF NOT EXISTS urls (
            short_code TEXT PRIMARY KEY,
            original_url TEXT,
            created_at TIMESTAMP,
            expires_at TIMESTAMP,
            is_active BOOLEAN,
            custom_alias BOOLEAN
        )");

                session.Execute(@"
        CREATE TABLE IF NOT EXISTS url_counters (
            short_code TEXT PRIMARY KEY,
            click_count COUNTER
        )");

                session.Execute(@"
        CREATE TABLE IF NOT EXISTS url_analytics (
            short_code TEXT,
            click_date DATE,
            click_timestamp TIMESTAMP,
            user_agent TEXT,
            ip_address TEXT,
            PRIMARY KEY (short_code, click_date, click_timestamp)
        ) WITH CLUSTERING ORDER BY (click_date DESC, click_timestamp DESC)");

                logger.LogInformation("Connected to Cassandra and created tables");

                return session;
            });

            builder.Services.AddHostedService<ExpirationCleanupService>();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors();
            app.UseRouting();
            app.MapControllers();

            app.Run();
        }
    }
}
