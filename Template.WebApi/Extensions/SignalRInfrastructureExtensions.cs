using Template.Common.BackgroundQueue;
using Template.WebApi.Hubs;
using Template.WebApi.SignalR;

namespace Template.WebApi.Extensions;

/// <summary>
/// WebApi SignalR 基礎設施註冊。
/// </summary>
public static class SignalRInfrastructureExtensions
{
    /// <summary>
    /// 註冊 SignalR Hub 與背景佇列推播 handler。
    /// </summary>
    public static IServiceCollection AddSignalRInfrastructure(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<IBackgroundJobHandler, QueuedSignalRMessageHandler>();

        return services;
    }

    /// <summary>
    /// 掛載 SignalR Hub endpoint。
    /// </summary>
    public static IEndpointRouteBuilder MapSignalRInfrastructure(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationHub>("/hubs/notifications");

        return endpoints;
    }
}
