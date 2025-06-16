
using Cassandra;
using FluentValidation;
using URLShortener.Application.Dtos;
using URLShortener.Application.HostedServices;
using URLShortener.Application.Services;
using URLShortener.Core.Services;
using URLShortener.Core.Validators;
using URLShortener.Infrastructure.Data;
using URLShortener.Infrastructure.Services;

namespace URLShortener.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure Cassandra
            builder.Services.AddSingleton<ICluster>(provider =>
            {
                return Cluster.Builder()
                    .AddContactPoint("localhost")
                    .WithPort(9042)
                    .Build();
            });

            builder.Services.AddSingleton<Cassandra.ISession>(provider =>
            {
                var cluster = provider.GetRequiredService<ICluster>();
                return cluster.Connect("urlshortener");
            });
            builder.Services.AddSingleton<IBase62Encoder>(provider =>
            {
                var configuration = provider.GetService<IConfiguration>();
                var datacenterId = configuration?.GetValue<long>("Snowflake:DatacenterId", 1) ?? 1;
                var workerId = configuration?.GetValue<long>("Snowflake:WorkerId", 1) ?? 1;

                return new Base62Encoder(datacenterId, workerId);
            });
            builder.Services.AddScoped<ICassandraService, CassandraService>();
            builder.Services.AddScoped<IUrlService, UrlService>();

            builder.Services.AddScoped<IValidator<CreateUrlRequest>, CreateUrlRequestValidator>();
            builder.Services.AddScoped<IValidator<UpdateUrlRequest>, UpdateUrlRequestValidator>();

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

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
