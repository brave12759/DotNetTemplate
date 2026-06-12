namespace Template.Common.SignalR;

/// <summary>
/// SignalR 訊息推播目標類型。
/// </summary>
public enum SignalRTargetType
{
    /// <summary>
    /// 所有已連線 client。
    /// </summary>
    All = 1,

    /// <summary>
    /// 指定群組。
    /// </summary>
    Group = 2,

    /// <summary>
    /// 指定使用者。
    /// </summary>
    User = 3,

    /// <summary>
    /// 指定連線。
    /// </summary>
    Connection = 4
}
