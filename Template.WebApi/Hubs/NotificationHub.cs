using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Template.WebApi.Hubs;

/// <summary>
/// 通用 SignalR 通知 Hub。
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    /// <summary>
    /// 加入指定群組。
    /// </summary>
    public Task JoinGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new HubException("GroupName is required.");

        return Groups.AddToGroupAsync(Context.ConnectionId, groupName.Trim());
    }

    /// <summary>
    /// 離開指定群組。
    /// </summary>
    public Task LeaveGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new HubException("GroupName is required.");

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName.Trim());
    }
}
