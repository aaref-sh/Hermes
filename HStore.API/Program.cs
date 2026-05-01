using HStore.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace HStore.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = CreateHostBuilder(args).Build();
        
        // Seed data to the database for testing purposes, uncomment if needed
        using (var serviceScope = builder.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetService<HStoreDbContext>();
            context.Database.Migrate();

            //var userManager = serviceScope.ServiceProvider.GetService<UserManager<User>>();
            //var roleManager = serviceScope.ServiceProvider.GetService<RoleManager<Role>>();
            //var seeder = new DataSeeder(context!, userManager, roleManager);
            //await seeder.SeedAsync();
        }

        await builder.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
}