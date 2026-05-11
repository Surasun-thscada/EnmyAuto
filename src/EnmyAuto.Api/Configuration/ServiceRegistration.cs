using System.Text;
using EnmyAuto.Api.Hubs;
using EnmyAuto.Api.Services.Ai;
using EnmyAuto.Api.Services.Auth;
using EnmyAuto.Api.Services.Media;
using EnmyAuto.Api.Services.TikTok;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnmyAuto.Api.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddAiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<GeminiOptions>()
                .Bind(configuration.GetSection(GeminiOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddHttpClient(nameof(AiStoryboardService), client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
        });

        services.AddScoped<IAiStoryboardService, AiStoryboardService>();

        return services;
    }

    public static IServiceCollection AddMediaServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FfmpegOptions>()
                .Bind(configuration.GetSection(FfmpegOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        // Generous timeout — large media files can take time to download.
        services.AddHttpClient(nameof(VideoRenderService), client =>
            client.Timeout = TimeSpan.FromMinutes(5));

        services.AddScoped<IVideoRenderService, VideoRenderService>();

        return services;
    }

    public static IServiceCollection AddHangfireJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Hangfire requires 'DefaultConnection'.");

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(connectionString)));

        // One dedicated worker thread for video rendering to avoid CPU contention.
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
            options.Queues = ["render", "default"];
        });

        return services;
    }

    public static IServiceCollection AddRealTime(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = false; // set true only in Development
            options.ClientTimeoutInterval  = TimeSpan.FromSeconds(60);
            options.KeepAliveInterval      = TimeSpan.FromSeconds(20);
        });

        return services;
    }

    public static IServiceCollection AddAuthServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        var jwtSettings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtSettings.Issuer,
                ValidAudience            = jwtSettings.Audience,
                IssuerSigningKey         = jwtSettings.GetSigningKey(),
                ClockSkew                = TimeSpan.Zero, // no grace period on expiry
            };

            // Allow SignalR and TikTok OAuth redirect to read token from query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token))
                    {
                        var path = ctx.HttpContext.Request.Path;
                        if (path.StartsWithSegments("/hubs") ||
                            path.StartsWithSegments("/api/tiktok/auth/connect"))
                        {
                            ctx.Token = token;
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    public static IServiceCollection AddTikTokServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<TikTokOptions>()
                .Bind(configuration.GetSection(TikTokOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddHttpClient(nameof(TikTokAuthService), client =>
            client.Timeout = TimeSpan.FromSeconds(30));

        services.AddHttpClient(nameof(TikTokPostService), client =>
            client.Timeout = TimeSpan.FromMinutes(10));

        services.AddScoped<ITikTokAuthService, TikTokAuthService>();
        services.AddScoped<ITikTokPostService, TikTokPostService>();

        return services;
    }

    /// <summary>Maps SignalR and Hangfire dashboard endpoints.</summary>
    public static WebApplication MapEnmyEndpoints(this WebApplication app)
    {
        app.MapHub<RenderHub>(RenderHub.Endpoint);
        app.UseHangfireDashboard("/jobs");
        return app;
    }
}
