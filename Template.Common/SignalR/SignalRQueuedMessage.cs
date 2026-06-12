namespace Template.Common.SignalR;

/// <summary>
/// 儲存在背景佇列中的 SignalR 推播訊息。
/// </summary>
public sealed class SignalRQueuedMessage
{
    /// <summary>
    /// 推播目標類型。
    /// </summary>
    public SignalRTargetType TargetType { get; set; }

    /// <summary>
    /// Group/User/Connection 的目標識別值。All 不需要填寫。
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Client 端接收的方法名稱。
    /// </summary>
    public string Method { get; set; } = SignalRClientMethods.Notification;

    /// <summary>
    /// 推播資料。
    /// </summary>
    public object? Payload { get; set; }
}
