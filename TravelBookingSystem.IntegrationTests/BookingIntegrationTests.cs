using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TravelBookingSystem.Common.Persistence;
using TravelBookingSystem.Features.Flight.Common;
using TravelBookingSystem.Features.Passenger.Common;

namespace TravelBookingSystem.IntegrationTests;

public class BookingIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("travelbooking_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .WithCleanUp(true)
        .Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .WithCleanUp(true)
        .Build();
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private IServiceScope? _scope;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync()
        );

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<TravelBookingDbContext>) ||
                        d.ServiceType == typeof(DbContextOptions<TravelBookingDbContextReadOnly>) ||
                        d.ServiceType == typeof(TravelBookingDbContext) ||
                        d.ServiceType == typeof(TravelBookingDbContextReadOnly) ||
                        d.ServiceType == typeof(IConnectionMultiplexer))
                        .ToList();

                    foreach (var descriptor in descriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<TravelBookingDbContext>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));

                    services.AddDbContext<TravelBookingDbContextReadOnly>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));

                    services.AddSingleton<IConnectionMultiplexer>(sp =>
                        ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
                });
            });

        _client = _factory.CreateClient();

        // Apply migrations
        _scope = _factory.Services.CreateScope();
        var dbContext = _scope.ServiceProvider.GetRequiredService<TravelBookingDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _scope?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
        
        await Task.WhenAll(
            _postgresContainer.StopAsync(),
            _redisContainer.StopAsync()
        );
    }
}
