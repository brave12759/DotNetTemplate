namespace Template.BusinessRule.MenuTreeService.Models;

/// <summary>
/// 選單樹節點輸出模型。
/// </summary>
public class MenuTreeDto
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string MenuCode { get; set; } = string.Empty;

    public string MenuName { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsEnable { get; set; }

    public DateTime CreatedTime { get; set; }

    public string CreatedId { get; set; } = string.Empty;

    public DateTime UpdatedTime { get; set; }

    public string UpdatedId { get; set; } = string.Empty;

    public List<MenuTreeDto> Children { get; set; } = [];
}
