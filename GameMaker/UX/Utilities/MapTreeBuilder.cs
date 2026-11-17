using System.Collections.ObjectModel;
using GameLibrary.Models.GameMaps;
using GameMaker.UX.Models.MainPage;

namespace GameMaker.UX.Utilities;

/// <summary>
/// Builds the editor's map tree from the flat list of maps (each map points at its parent folder).
/// </summary>
public static class MapTreeBuilder
{
    #region Methods

    // fills nodes (every map by guid) and roots (the top of the tree), sorted by id
    public static void Build(IEnumerable<GameMap> maps, Dictionary<Guid, MapTreeNode> nodes, ObservableCollection<MapTreeNode> roots)
    {
        nodes.Clear();
        roots.Clear();
        foreach (var map in maps.OrderBy(x => x.Id)) nodes[map.Guid] = new MapTreeNode(map);

        foreach (var node in nodes.Values)
        {
            if (HasValidParent(node.Map, nodes) && nodes.TryGetValue(node.Map.ParentGuid!.Value, out var parent)) parent.Children.Add(node);
            else roots.Add(node);
        }
    }

    // the tree can only select a node whose parents are open
    public static void ExpandParents(MapTreeNode? node, Dictionary<Guid, MapTreeNode> nodes)
    {
        for (var parent = node?.Map.ParentGuid; parent is { } guid && nodes.TryGetValue(guid, out var parentNode); parent = parentNode.Map.ParentGuid)
        {
            parentNode.IsExpanded = true;
        }
    }

    // every map inside this one, at any depth
    public static IEnumerable<Guid> Descendants(IEnumerable<GameMap> maps, Guid guid)
    {
        var all = maps as IReadOnlyCollection<GameMap> ?? [.. maps];
        foreach (var child in all.Where(x => x.ParentGuid == guid))
        {
            yield return child.Guid;
            foreach (var descendant in Descendants(all, child.Guid)) yield return descendant;
        }
    }

    // a parent that no longer exists or would create a loop puts the map at the root
    private static bool HasValidParent(GameMap map, Dictionary<Guid, MapTreeNode> nodes)
    {
        var visited = new HashSet<Guid> { map.Guid };
        for (var parent = map.ParentGuid; parent is { } guid; parent = nodes[guid].Map.ParentGuid)
        {
            if (!nodes.ContainsKey(guid) || !visited.Add(guid)) return false;
        }

        return map.ParentGuid is not null;
    }

    #endregion
}
