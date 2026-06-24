using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.SwaggerUI;
using Template.BusinessRule.Extensions;
using Template.Common.BackgroundQueue;
using Template.Common.Models.Jwt;
using Template.Common.Models;
using Template.Common.Services;
using Template.Common.Settings;
using Template.BusinessRule.TokenRevocationService.Services;
using Template.WebApi.Authentication;
using Template.WebApi.Converters;
using Template.WebApi.Extensions;
using Template.WebApi.Filters;
using Template.WebApi.Routing;
using Template.WebApi.Services;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    LoadDotEnv(builder.Environment.ContentRootPath, builder.Environment.EnvironmentName);
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddCommandLine(args);

    builder.Services.Configure<LogSettings>(builder.Configuration.GetSection(LogSettings.SectionName));
    var logSettings = builder.Configuration.GetSection(LogSettings.SectionName).Get<LogSettings>()
        ?? throw new InvalidOperationException($"Missing configuration section: {LogSettings.SectionName}");

    var minimumLevel = Enum.TryParse<LogEventLevel>(logSettings.MinimumLevel, ignoreCase: true, out var lvl)
        ? lvl
        : LogEventLevel.Warning;

    var logPath = Path.IsPathRooted(logSettings.LogDirectory)
        ? logSettings.LogDirectory
        : Path.Combine(AppContext.BaseDirectory, logSettings.LogDirectory);

    builder.Host.UseSerilog((_, _, config) =>
        config
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(logPath, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: logSettings.FileSizeLimitMb * 1024L * 1024L,
                retainedFileCountLimit: logSettings.RetainedFileCountLimit,
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection(ApiSettings.SectionName));
    var apiSettings = builder.Configuration.GetSection(ApiSettings.SectionName).Get<ApiSettings>()
        ?? throw new InvalidOperationException($"Missing configuration section: {ApiSettings.SectionName}");

    builder.Services.Configure<TimeZoneSettings>(builder.Configuration.GetSection(TimeZoneSettings.SectionName));
    var timeZoneSettings = builder.Configuration.GetSection(TimeZoneSettings.SectionName).Get<TimeZoneSettings>()
        ?? throw new InvalidOperationException($"Missing configuration section: {TimeZoneSettings.SectionName}");
    var appTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneSettings.TimeZoneId);
    builder.Services.AddSingleton(appTimeZone);

    builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection(DatabaseSettings.SectionName));
    var databaseSettings = builder.Configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>()
        ?? throw new InvalidOperationException($"Missing configuration section: {DatabaseSettings.SectionName}");
    builder.Services.AddSingleton(databaseSettings);

    builder.Services.Configure<BackgroundQueueSettings>(builder.Configuration.GetSection(BackgroundQueueSettings.SectionName));
    var backgroundQueueSettings = builder.Configuration.GetSection(BackgroundQueueSettings.SectionName).Get<BackgroundQueueSettings>()
        ?? new BackgroundQueueSettings();
    ValidateBackgroundQueueSettings(backgroundQueueSettings);

    builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection(FileStorageSettings.SectionName));
    var fileStorageSettings = builder.Configuration.GetSection(FileStorageSettings.SectionName).Get<FileStorageSettings>()
        ?? new FileStorageSettings();
    ValidateFileStorageSettings(fileStorageSettings);
    builder.Services.AddSingleton(fileStorageSettings);

    builder.Services.AddBusinessRuleServices(databaseSettings, backgroundQueueSettings);

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    if (builder.Environment.IsDevelopment())
    {
        var devUser = builder.Configuration.GetSection(DevBypassUserSettings.SectionName).Get<DevBypassUserSettings>()
            ?? new DevBypassUserSettings();
        builder.Services.AddSingleton(devUser);
    }

    var authBuilder = builder.Services.AddAuthentication(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.DefaultAuthenticateScheme = DevBypassAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = DevBypassAuthenticationHandler.SchemeName;
        }
        else
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }
    });

    if (builder.Environment.IsDevelopment())
        authBuilder.AddScheme<DevBypassAuthenticationOptions, DevBypassAuthenticationHandler>(
            DevBypassAuthenticationHandler.SchemeName, _ => { });

    var jwtValidationSettings = GetRequiredJwtCoreSettings(builder.Configuration);

    authBuilder.AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            ValidIssuer = jwtValidationSettings.Issuer,
            ValidAudience = jwtValidationSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtValidationSettings.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var tokenRevocationService = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();
                var tokenId = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(tokenId))
                {
                    context.Fail("Invalid token: missing jti.");
                    return;
                }

                if (tokenRevocationService.IsRevoked(tokenId))
                {
                    context.Fail("Token revoked.");
                    return;
                }

                await Task.CompletedTask;
            }
        };
    });

    builder.Services.Configure<HashSettings>(builder.Configuration.GetSection(HashSettings.SectionName));
    var hashSettings = builder.Configuration.GetSection(HashSettings.SectionName).Get<HashSettings>()
        ?? throw new InvalidOperationException($"Missing configuration section: {HashSettings.SectionName}");
    if (hashSettings.Iterations < 10000)
        throw new InvalidOperationException("HashSettings.Iterations must be at least 10000.");
    builder.Services.AddSingleton(hashSettings);

    builder.Services.Configure<CryptographyKeySettings>(builder.Configuration.GetSection(CryptographyKeySettings.SectionName));
    var cryptographyKeySettings = builder.Configuration.GetSection(CryptographyKeySettings.SectionName).Get<CryptographyKeySettings>()
        ?? throw new InvalidOperationException($"Missing configuration section: {CryptographyKeySettings.SectionName}");
    cryptographyKeySettings.RsaPublicKeyPem = NormalizeEscapedNewLines(cryptographyKeySettings.RsaPublicKeyPem);
    cryptographyKeySettings.RsaPrivateKeyPem = NormalizeEscapedNewLines(cryptographyKeySettings.RsaPrivateKeyPem);
    builder.Services.AddSingleton(cryptographyKeySettings);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddAuthorization();
    builder.Services.AddSignalRInfrastructure();

    builder.Services.AddHealthChecks().AddBusinessRuleHealthChecks(databaseSettings);

    builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));
    var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
    builder.Services.AddSingleton(corsSettings);

    const string DefaultCorsPolicy = "DefaultCorsPolicy";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(DefaultCorsPolicy, policy =>
        {
            if (corsSettings.AllowAnyOrigin)
            {
                policy.AllowAnyOrigin();
            }
            else if (corsSettings.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(corsSettings.AllowedOrigins);
                if (corsSettings.AllowCredentials)
                    policy.AllowCredentials();
            }
            else
            {
                // No allowed origins configured: leave origins unset to deny cross-origin requests by default.
            }

            policy.AllowAnyMethod().AllowAnyHeader();
        });
    });

    builder.Services.AddControllers(options =>
    {
        options.Conventions.Add(new RestfulRouteConvention());
        options.Filters.Add<GlobalExceptionLogFilter>();
        options.Filters.Add<ResponseWrapperFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
        options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter(appTimeZone));
        options.JsonSerializerOptions.Converters.Add(new DateTimeOffsetJsonConverter(appTimeZone));
    });

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = apiSettings.Name, Version = "v1" });
        options.OperationFilter<ResponseMessageOperationFilter>();

        var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Use: Bearer {token}"
        });

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", apiSettings.Name);
            c.DocExpansion(DocExpansion.None);
        });
    }

    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging();

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var exception = feature?.Error;
            var mvcJsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>();
            var userId = context.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
            var tokenId = context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;

            logger.LogError(
                exception,
                "Unhandled exception. TraceId={TraceId}, Method={Method}, Path={Path}, QueryString={QueryString}, UserId={UserId}, TokenId={TokenId}, Ip={Ip}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.QueryString.Value,
                userId,
                tokenId,
                context.Connection.RemoteIpAddress?.ToString());

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            var payload = ResponseMessage<object>.Fail(500, "Internal server error.");
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, mvcJsonOptions.Value.JsonSerializerOptions));
        });
    });

    app.UseStaticFiles();
    app.UseCors(DefaultCorsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapSignalRInfrastructure();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static void ValidateBackgroundQueueSettings(BackgroundQueueSettings settings)
{
    if (settings.DefaultPollingIntervalSeconds <= 0)
        throw new InvalidOperationException("BackgroundQueueSettings.DefaultPollingIntervalSeconds must be greater than zero.");

    if (settings.DefaultLockTimeoutSeconds <= 0)
        throw new InvalidOperationException("BackgroundQueueSettings.DefaultLockTimeoutSeconds must be greater than zero.");

    if (settings.DefaultMaxRetryCount < 0)
        throw new InvalidOperationException("BackgroundQueueSettings.DefaultMaxRetryCount must be greater than or equal to zero.");

    if (settings.ShutdownTimeoutSeconds <= 0)
        throw new InvalidOperationException("BackgroundQueueSettings.ShutdownTimeoutSeconds must be greater than zero.");
}

static void ValidateFileStorageSettings(FileStorageSettings settings)
{
    if (!settings.Enabled)
        return;

    if (string.IsNullOrWhiteSpace(settings.Provider))
        throw new InvalidOperationException("FileStorageSettings.Provider is required when file storage is enabled.");

    if (!settings.EnableSingleUpload && !settings.EnableChunkUpload)
        throw new InvalidOperationException("At least one upload mode must be enabled (single or chunk).");

    if (!settings.EnableAdminScope && !settings.EnablePersonalScope)
        throw new InvalidOperationException("At least one file scope must be enabled (admin or personal).");

    if (settings.MaxFileSizeMb <= 0)
        throw new InvalidOperationException("FileStorageSettings.MaxFileSizeMb must be greater than zero.");

    if (settings.MaxSingleUploadSizeMb <= 0)
        throw new InvalidOperationException("FileStorageSettings.MaxSingleUploadSizeMb must be greater than zero.");

    if (settings.MaxSingleUploadSizeMb > settings.MaxFileSizeMb)
        throw new InvalidOperationException("FileStorageSettings.MaxSingleUploadSizeMb cannot exceed MaxFileSizeMb.");

    if (settings.MaxChunkSizeMb <= 0)
        throw new InvalidOperationException("FileStorageSettings.MaxChunkSizeMb must be greater than zero.");

    if (settings.MaxChunkCountPerFile <= 0)
        throw new InvalidOperationException("FileStorageSettings.MaxChunkCountPerFile must be greater than zero.");

    var maxChunkUploadMb = (long)settings.MaxChunkSizeMb * settings.MaxChunkCountPerFile;
    if (maxChunkUploadMb < settings.MaxFileSizeMb)
        throw new InvalidOperationException("Chunk capacity (MaxChunkSizeMb * MaxChunkCountPerFile) must be >= MaxFileSizeMb.");

    if (settings.ChunkSessionExpireMinutes <= 0)
        throw new InvalidOperationException("FileStorageSettings.ChunkSessionExpireMinutes must be greater than zero.");

    if (settings.DownloadUrlExpireSeconds <= 0)
        throw new InvalidOperationException("FileStorageSettings.DownloadUrlExpireSeconds must be greater than zero.");
}

static void LoadDotEnv(string contentRootPath, string environmentName)
{
    var paths = new[]
    {
        Path.Combine(contentRootPath, ".env"),
        Path.Combine(contentRootPath, $".env.{environmentName}")
    };

    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var path in paths)
    {
        if (!File.Exists(path))
            continue;

        foreach (var pair in ReadDotEnv(path))
            values[pair.Key] = pair.Value;
    }

    foreach (var pair in values)
    {
        if (Environment.GetEnvironmentVariable(pair.Key) is null)
            Environment.SetEnvironmentVariable(pair.Key, pair.Value);
    }
}

static Dictionary<string, string> ReadDotEnv(string path)
{
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
            continue;

        if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            line = line[7..].TrimStart();

        var equalsIndex = line.IndexOf('=');
        if (equalsIndex <= 0)
            continue;

        var key = line[..equalsIndex].Trim();
        var value = line[(equalsIndex + 1)..].Trim();
        if (key.Length == 0)
            continue;

        values[key] = UnquoteDotEnvValue(value);
    }

    return values;
}

static string UnquoteDotEnvValue(string value)
{
    if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
    {
        return value[1..^1]
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");
    }

    if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
        return value[1..^1];

    return value;
}

static string NormalizeEscapedNewLines(string value)
{
    return value.Replace("\\r\\n", "\n").Replace("\\n", "\n");
}

static JwtSettingDto GetRequiredJwtCoreSettings(IConfiguration configuration)
{
    var secretKey = configuration["JwtSettings:SecretKey"]?.Trim();
    var issuer = configuration["JwtSettings:Issuer"]?.Trim();
    var audience = configuration["JwtSettings:Audience"]?.Trim();

    if (string.IsNullOrWhiteSpace(secretKey))
        throw new InvalidOperationException("Missing required JWT setting: JwtSettings:SecretKey.");

    if (string.IsNullOrWhiteSpace(issuer))
        throw new InvalidOperationException("Missing required JWT setting: JwtSettings:Issuer.");

    if (string.IsNullOrWhiteSpace(audience))
        throw new InvalidOperationException("Missing required JWT setting: JwtSettings:Audience.");

    if (Encoding.UTF8.GetByteCount(secretKey) < 32)
        throw new InvalidOperationException("JwtSettings:SecretKey must be at least 32 bytes.");

    return new JwtSettingDto
    {
        SecretKey = secretKey,
        Issuer = issuer,
        Audience = audience
    };
}
