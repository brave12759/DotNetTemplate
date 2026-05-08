using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Template.Common.Settings;
using Template.Common.Models;
using Template.BusinessRule.Extensions;
using Template.WebApi.Authentication;
using Template.Common.Services;
using Template.WebApi.Converters;
using Template.WebApi.Filters;
using Template.WebApi.Services;

// 捕捉應用程式啟動階段的錯誤
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 日誌設定
    builder.Services.Configure<LogSettings>(
        builder.Configuration.GetSection(LogSettings.SectionName));

    var logSettings = builder.Configuration
        .GetSection(LogSettings.SectionName)
        .Get<LogSettings>()
        ?? throw new InvalidOperationException($"設定區段 '{LogSettings.SectionName}' 不存在或無效。");

    var minimumLevel = Enum.TryParse<LogEventLevel>(logSettings.MinimumLevel, ignoreCase: true, out var lvl)
        ? lvl
        : LogEventLevel.Warning;

    var logPath = Path.IsPathRooted(logSettings.LogDirectory)
        ? logSettings.LogDirectory
        : Path.Combine(AppContext.BaseDirectory, logSettings.LogDirectory);

    // Serilog 設定
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

    // Add services to the container.

    // API 設定
    builder.Services.Configure<ApiSettings>(
        builder.Configuration.GetSection(ApiSettings.SectionName));

    var apiSettings = builder.Configuration
        .GetSection(ApiSettings.SectionName)
        .Get<ApiSettings>()
        ?? throw new InvalidOperationException($"設定區段 '{ApiSettings.SectionName}' 不存在或無效。");

    // HTTPS 設定
    builder.Services.Configure<HttpsSettings>(
        builder.Configuration.GetSection(HttpsSettings.SectionName));

    var httpsSettings = builder.Configuration
        .GetSection(HttpsSettings.SectionName)
        .Get<HttpsSettings>()
        ?? new HttpsSettings();

    builder.Services.AddSingleton(httpsSettings);

    if (!builder.Environment.IsDevelopment() && httpsSettings.EnforceHttps && string.IsNullOrWhiteSpace(httpsSettings.CertificatePath))
        Log.Warning("正式環境啟用 HTTPS 但未設定 HttpsSettings.CertificatePath，請確認是否由反向代理終止 TLS。");

    // 憑證設定：若有設定 PFX 路徑，則使用指定憑證。
    // 未設定時，Development 可使用 dev certificate；正式環境建議透過環境變數提供。
    builder.WebHost.ConfigureKestrel(options =>
    {
        if (string.IsNullOrWhiteSpace(httpsSettings.CertificatePath))
            return;

        if (!File.Exists(httpsSettings.CertificatePath))
            throw new InvalidOperationException($"HTTPS 憑證檔不存在: {httpsSettings.CertificatePath}");

        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            httpsSettings.CertificatePath,
            httpsSettings.CertificatePassword);

        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.ServerCertificate = certificate;
        });
    });

    // 時區設定
    builder.Services.Configure<TimeZoneSettings>(
        builder.Configuration.GetSection(TimeZoneSettings.SectionName));

    var timeZoneSettings = builder.Configuration
        .GetSection(TimeZoneSettings.SectionName)
        .Get<TimeZoneSettings>()
        ?? throw new InvalidOperationException($"設定區段 '{TimeZoneSettings.SectionName}' 不存在或無效。");

    var appTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneSettings.TimeZoneId);
    builder.Services.AddSingleton(appTimeZone);

    // 資料庫設定
    builder.Services.Configure<DatabaseSettings>(
        builder.Configuration.GetSection(DatabaseSettings.SectionName));

    var databaseSettings = builder.Configuration
        .GetSection(DatabaseSettings.SectionName)
        .Get<DatabaseSettings>()
        ?? throw new InvalidOperationException($"設定區段 '{DatabaseSettings.SectionName}' 不存在或無效。");

    builder.Services.AddSingleton(databaseSettings);

    // 背景工作佇列設定
    builder.Services.Configure<BackgroundQueueSettings>(
        builder.Configuration.GetSection(BackgroundQueueSettings.SectionName));

    var backgroundQueueSettings = builder.Configuration
        .GetSection(BackgroundQueueSettings.SectionName)
        .Get<BackgroundQueueSettings>()
        ?? new BackgroundQueueSettings();

    if (backgroundQueueSettings.DefaultPollingIntervalSeconds <= 0)
        throw new InvalidOperationException("BackgroundQueueSettings.DefaultPollingIntervalSeconds 必須大於 0。");

    if (backgroundQueueSettings.DefaultLockTimeoutSeconds <= 0)
        throw new InvalidOperationException("BackgroundQueueSettings.DefaultLockTimeoutSeconds 必須大於 0。");

    if (backgroundQueueSettings.DefaultMaxRetryCount < 0)
        throw new InvalidOperationException("BackgroundQueueSettings.DefaultMaxRetryCount 不可小於 0。");

    if (backgroundQueueSettings.ShutdownTimeoutSeconds <= 0)
        throw new InvalidOperationException("BackgroundQueueSettings.ShutdownTimeoutSeconds 必須大於 0。");

    // BusinessRule 服務（由 BusinessRule 統一註冊 DbContext / Token 撤銷 / 背景佇列 / 各業務服務）
    builder.Services.AddBusinessRuleServices(databaseSettings, backgroundQueueSettings);

    // HTTPS / 反向代理設定：支援多層 LB 或 API Gateway 的 X-Forwarded-* 標頭。
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = httpsSettings.RedirectStatusCode;
    });

    builder.Services.AddHsts(options =>
    {
        options.IncludeSubDomains = httpsSettings.HstsIncludeSubDomains;
        options.Preload = httpsSettings.HstsPreload;
        options.MaxAge = TimeSpan.FromDays(httpsSettings.HstsMaxAgeDays);
    });

    // JWT 設定
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection(JwtSettings.SectionName));

    var jwtSettings = builder.Configuration
        .GetSection(JwtSettings.SectionName)
        .Get<JwtSettings>()
        ?? throw new InvalidOperationException($"設定區段 '{JwtSettings.SectionName}' 不存在或無效。");

    builder.Services.AddSingleton(jwtSettings);

    if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        throw new InvalidOperationException("JwtSettings.SecretKey 未設定。");

    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

    // Development 環境：自動以假用戶通過驗證，免登入即可呼叫所有 API。
    // 若請求帶有 Authorization: Bearer Token，則仍走 JWT 驗證流程。
    // Production 環境：必須提供有效 JWT Token。
    if (builder.Environment.IsDevelopment())
    {
        var devUser = builder.Configuration
            .GetSection(DevBypassUserSettings.SectionName)
            .Get<DevBypassUserSettings>() ?? new DevBypassUserSettings();
        builder.Services.AddSingleton(devUser);
    }

    var authBuilder = builder.Services
        .AddAuthentication(options =>
        {
            if (builder.Environment.IsDevelopment())
            {
                options.DefaultAuthenticateScheme = DevBypassAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme    = DevBypassAuthenticationHandler.SchemeName;
            }
            else
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            }
        });

    if (builder.Environment.IsDevelopment())
        authBuilder.AddScheme<DevBypassAuthenticationOptions, DevBypassAuthenticationHandler>(
            DevBypassAuthenticationHandler.SchemeName, _ => { });

    authBuilder.AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtSettings.Issuer,
                ValidAudience            = jwtSettings.Audience,
                IssuerSigningKey         = signingKey,
                ClockSkew                = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var revocationService = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();
                    var tokenId = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

                    if (!string.IsNullOrWhiteSpace(tokenId) && revocationService.IsRevoked(tokenId))
                        context.Fail("Token 已登出或撤銷。");

                    return Task.CompletedTask;
                }
            };
        });

    // Hash 設定
    builder.Services.Configure<HashSettings>(
        builder.Configuration.GetSection(HashSettings.SectionName));

    var hashSettings = builder.Configuration
        .GetSection(HashSettings.SectionName)
        .Get<HashSettings>()
        ?? throw new InvalidOperationException($"設定區段 '{HashSettings.SectionName}' 不存在或無效。");

    if (hashSettings.Iterations < 10000)
        throw new InvalidOperationException("HashSettings.Iterations 必須大於等於 10000。");

    builder.Services.AddSingleton(hashSettings);

    // 加解密金鑰設定
    builder.Services.Configure<CryptographyKeySettings>(
        builder.Configuration.GetSection(CryptographyKeySettings.SectionName));

    var cryptographyKeySettings = builder.Configuration
        .GetSection(CryptographyKeySettings.SectionName)
        .Get<CryptographyKeySettings>()
        ?? throw new InvalidOperationException($"設定區段 '{CryptographyKeySettings.SectionName}' 不存在或無效。");

    builder.Services.AddSingleton(cryptographyKeySettings);

    builder.Services.AddHttpContextAccessor();

    // Token 撤銷策略警告（LogConnectionString 為空時降級至 In-Memory）
    if (string.IsNullOrWhiteSpace(databaseSettings.LogConnectionString))
        Log.Warning("DatabaseSettings.LogConnectionString 未設定，Token 撤銷將使用 In-Memory（不支援多節點共用）。");

    // CurrentUserService：從 JWT Claims 解析當前登入者資訊，供邏輯層注入使用
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    // JwtService：產生 JWT Token（含 UserId / Email / MobilePhone / DeptId / IP 等 Claims）
    // Auth：POST /Auth/Login（登入）、POST /Auth/Logout（登出撤銷）
    builder.Services.AddScoped<IJwtService, JwtService>();

    builder.Services.AddAuthorization();

    // Health Check：K8s liveness/readiness probe、負載均衡器心跳偵測
    // GET /health → 200 Healthy / 503 Unhealthy（不需驗證，基礎設施直接呼叫）
    builder.Services.AddHealthChecks()
        .AddBusinessRuleHealthChecks(databaseSettings);

    // CORS 設定
    builder.Services.Configure<CorsSettings>(
        builder.Configuration.GetSection(CorsSettings.SectionName));

    var corsSettings = builder.Configuration
        .GetSection(CorsSettings.SectionName)
        .Get<CorsSettings>() ?? new CorsSettings();

    builder.Services.AddSingleton(corsSettings);

    if (corsSettings.AllowCredentials && corsSettings.AllowAnyOrigin)
        Log.Warning("CorsSettings: AllowCredentials=true 與 AllowAnyOrigin=true 不相容，AllowCredentials 將被忽略。");

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
                // 未設定任何來源 → 不允許任何跨域請求（安全預設值）
                policy.WithOrigins();
            }

            policy.AllowAnyMethod().AllowAnyHeader();
        });
    });

    builder.Services.AddControllers(options =>
    {
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

        var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

        // 加入 Bearer Token 輸入框
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "請輸入 JWT Token（不需加 Bearer 前綴）"
        });

        // 全域套用 Bearer 驗證
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", apiSettings.Name);
            c.DocExpansion(DocExpansion.None);
        });
    }

    // 先處理 Forwarded Headers，避免 HTTPS 重新導向與來源 IP 判斷錯誤。
    app.UseForwardedHeaders();

    // 將 HTTP 請求記錄路由至 Serilog ILogger
    app.UseSerilogRequestLogging();

    // Middleware 層全域例外處理：覆蓋整條管線（含 MVC 之外的錯誤）。
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var exception = feature?.Error;
            var mvcJsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>();

            var request = context.Request;
            var userId = context.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
            var tokenId = context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;

            logger.LogError(
                exception,
                "Unhandled exception by middleware. TraceId={TraceId}, Method={Method}, Path={Path}, QueryString={QueryString}, UserId={UserId}, TokenId={TokenId}, Ip={Ip}",
                context.TraceIdentifier,
                request.Method,
                request.Path.Value,
                request.QueryString.Value,
                userId,
                tokenId,
                context.Connection.RemoteIpAddress?.ToString());

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";

            var payload = ResponseMessage<object>.Fail(500, "系統發生未預期錯誤，請稍後再試。");
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, mvcJsonOptions.Value.JsonSerializerOptions));
        });
    });

    if (httpsSettings.EnforceHttps && httpsSettings.HstsEnabled && !app.Environment.IsDevelopment())
        app.UseHsts();

    if (httpsSettings.EnforceHttps)
        app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseCors(DefaultCorsPolicy);

    app.UseAuthentication();
    app.UseAuthorization();

    // Health Check 端點（公開，不需 JWT）
    app.MapHealthChecks("/health").AllowAnonymous();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "應用程式啟動失敗");
}
finally
{
    await Log.CloseAndFlushAsync();
}
