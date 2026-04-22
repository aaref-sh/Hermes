using EasyCaching.InMemory;
using EFCoreSecondLevelCacheInterceptor;
using FluentValidation;
using FluentValidation.AspNetCore;
using HStore.API.Filters;
using HStore.API.Utilities;
using HStore.Domain.Entities;
using HStore.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AutoValidationExtensions = SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions.ServiceCollectionExtensions;
using System.Text;
using Microsoft.OpenApi.Models;

namespace HStore.API;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 1. Configure Database
        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<HStoreDbContext>()
        .AddDefaultTokenProviders();

        services.AddControllers();
        // CORS configuration for development
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllCors", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
        services.AddDbContext<HStoreDbContext>((serviceProvider, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    sqlServerOptionsBuilder =>
                    {
                        sqlServerOptionsBuilder
                            .CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds)
                            .EnableRetryOnFailure()
                            .MigrationsAssembly(typeof(HStoreDbContext).Assembly.FullName);
                    })
                .AddInterceptors(serviceProvider.GetRequiredService<SecondLevelCacheInterceptor>()));

        // JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings");
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings["Secret"]!)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = true
            };
        });

        // 2. Configure Caching
        const string cacheProvider = "InMemory";
        services.AddEFSecondLevelCache(options =>
            options.UseEasyCachingCoreProvider(cacheProvider, isHybridCache: false).ConfigureLogging(true)
                .UseCacheKeyPrefix("EF_")
                .CacheAllQueries(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(30))
                .UseDbCallsIfCachingProviderIsDown(TimeSpan.FromMinutes(1))
        );

        services.AddEasyCaching(options =>
        {
            options.UseInMemory(config =>
            {
                config.DBConfig = new InMemoryCachingOptions
                {
                    ExpirationScanFrequency = 60,
                    SizeLimit = 100,
                    EnableReadDeepClone = false,
                    EnableWriteDeepClone = false,
                };
                config.MaxRdSecond = 120;
                config.EnableLogging = false;
                config.LockMs = 5000;
                config.SleepMs = 300;
            }, cacheProvider);
        });


        // 3. Configure Validators
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<Startup>();
        AutoValidationExtensions.AddFluentValidationAutoValidation(services, options =>
        {
            options.DisableBuiltInModelValidation = true;
            options.EnableBodyBindingSourceAutomaticValidation = true;
            options.EnableFormBindingSourceAutomaticValidation = true;
            options.EnableQueryBindingSourceAutomaticValidation = true;
            options.EnablePathBindingSourceAutomaticValidation = true;
            options.EnableCustomBindingSourceAutomaticValidation = true;
        });
        
        // 4. Configure AutoMapper
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // 5. Configure Filters
        services.AddMvc(options => { options.Filters.Add<ApiExceptionFilter>(); });

        // 6. Configure Settings
        services.AddSettings(configuration);

        // 7. Register Repositories
        services.AddRepositories();

        // 8. Register Services
        services.AddServices();

        // 9. Configure Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "HStore API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.ApplyMigrations();
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        app.UseCors("AllowAllCors");
        app.UseHttpsRedirection()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints => { endpoints.MapControllers(); })
            .UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        var error = new { message = contextFeature.Error.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));

                        // Logging
                        Console.WriteLine($"Error: {contextFeature.Error}");
                        Console.WriteLine($"Stack Trace: {contextFeature.Error.StackTrace}");
                    }
                });
            });
    }
}