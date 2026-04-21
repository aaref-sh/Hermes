using HStore.API.Utilities;
using HStore.Application.Interfaces;
using HStore.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HStore.Infrastructure.Data.Context;
using HStore.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace HStore.API.Tests.Integration;

public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
        });
        
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContextOptions
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbContextOptions<HStoreDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add an in-memory database for testing
            services.AddDbContext<HStoreDbContext>(options =>
            {
                options.UseInMemoryDatabase("HStoreTestDb");
                options.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
            
            // Seed data
            services.AddScoped(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                services.AddSettings(configuration);
                var context = sp.GetRequiredService<HStoreDbContext>();
                return new DataSeeder(context);
            });
        });
        
        

        builder.UseEnvironment("Testing");
    }
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Seed database on host startup
        using var scope = host.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        seeder.SeedAsync().Wait();

        return host;
    }
}
