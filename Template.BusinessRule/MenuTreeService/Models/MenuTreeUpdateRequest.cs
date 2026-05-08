namespace Template.BusinessRule.MenuTreeService.Models;

/// <summary>
/// 選單樹更新請求。
/// </summary>
public class MenuTreeUpdateRequest
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string MenuCode { get; set; } = string.Empty;

    public string MenuName { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsEnable { get; set; } = true;
}
