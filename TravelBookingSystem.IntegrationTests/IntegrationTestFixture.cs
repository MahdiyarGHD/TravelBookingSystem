using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TravelBookingSystem.Common.Persistence;

namespace TravelBookingSystem.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime, IDisposable
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

    public HttpClient Client => _client ?? throw new InvalidOperationException("Fixture not initialized");

    public IServiceProvider Services => _factory?.Services ?? throw new InvalidOperationException("Factory not initialized");

    public IServiceScope CreateScope() => Services.CreateScope();

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
                        services.Remove(descriptor);

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
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TravelBookingDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();

        await Task.WhenAll(
            _postgresContainer.StopAsync(),
            _redisContainer.StopAsync()
        );
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
        _postgresContainer.DisposeAsync().AsTask().Wait();
        _redisContainer.DisposeAsync().AsTask().Wait();
    }
}
