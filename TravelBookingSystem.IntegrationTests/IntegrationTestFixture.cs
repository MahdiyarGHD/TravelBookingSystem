using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TravelBookingSystem.Common.Persistence;

namespace TravelBookingSystem.IntegrationTests;

public sealed class IntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
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

    public T GetDbContext<T>() where T : DbContext
    {
        var scope = Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
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
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync()
        );
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StopAsync(),
            _redisContainer.StopAsync()
        );
    }
}