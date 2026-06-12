namespace Template.Common.Enums;

/// <summary>
/// 日誌寫入器種類。
/// </summary>
public enum LogWriterTypeEnum
{
    /// <summary>
    /// 使用者操作日誌，包含使用者資料維護、密碼異動與登入登出。
    /// </summary>
    UserOperation = 1,

    /// <summary>
    /// 佇列執行日誌。
    /// </summary>
    Queue = 2,

    /// <summary>
    /// SSO 串接日誌。
    /// </summary>
    Sso = 3
}
