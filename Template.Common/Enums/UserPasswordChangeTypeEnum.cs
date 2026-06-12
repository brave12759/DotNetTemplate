namespace Template.Common.Enums;

/// <summary>
/// 使用者密碼異動類型。
/// </summary>
public enum UserPasswordChangeTypeEnum
{
    /// <summary>
    /// 建立使用者時設定初始密碼。
    /// </summary>
    Create = 1,

    /// <summary>
    /// 管理員重設密碼。
    /// </summary>
    Reset = 2,

    /// <summary>
    /// 使用者自行變更密碼。
    /// </summary>
    Change = 3
}
