namespace Template.BusinessRule.Extensions;

/// <summary>
/// 泛型樹狀結構組裝工具
/// </summary>
public static class TreeBuilder
{
    public static List<TNode> BuildTree<TNode, TKey>(
        IReadOnlyList<TNode> source,
        Func<TNode, TKey> keySelector,
        Func<TNode, TKey?> parentKeySelector,
        Func<TNode, TNode> cloneSelector,
        Func<TNode, List<TNode>> childrenSelector,
        Comparison<TNode> comparison)
        where TKey : struct
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(parentKeySelector);
        ArgumentNullException.ThrowIfNull(cloneSelector);
        ArgumentNullException.ThrowIfNull(childrenSelector);
        ArgumentNullException.ThrowIfNull(comparison);

        var lookup = source.ToDictionary(keySelector, cloneSelector);
        var roots = new List<TNode>();

        foreach (var node in lookup.Values)
        {
            var parentKey = parentKeySelector(node);
            if (parentKey.HasValue && lookup.TryGetValue(parentKey.Value, out var parent))
                childrenSelector(parent).Add(node);
            else
                roots.Add(node);
        }

        SortRecursive(roots, childrenSelector, comparison);
        return roots;
    }

    private static void SortRecursive<TNode>(
        List<TNode> nodes,
        Func<TNode, List<TNode>> childrenSelector,
        Comparison<TNode> comparison)
    {
        if (nodes.Count == 0)
            return;

        nodes.Sort(comparison);

        foreach (var node in nodes)
            SortRecursive(childrenSelector(node), childrenSelector, comparison);
    }
}
