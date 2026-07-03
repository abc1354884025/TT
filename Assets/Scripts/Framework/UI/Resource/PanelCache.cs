using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 面板缓存池。CacheOnClose=true 的面板关闭时回池复用。
/// </summary>
public class PanelCache
{
    private readonly Dictionary<string, Queue<UIPanel>> _cache = new Dictionary<string, Queue<UIPanel>>();
    private const int DefaultMax = 3;

    public UIPanel TryGet(string panelId)
    {
        return _cache.TryGetValue(panelId, out var q) && q.Count > 0 ? q.Dequeue() : null;
    }

    public void Return(UIPanel panel, int maxCache = DefaultMax)
    {
        if (!panel) return;
        var id = panel.PanelId;
        if (!_cache.TryGetValue(id, out var q)) { q = new Queue<UIPanel>(); _cache[id] = q; }
        if (q.Count >= maxCache) { Object.Destroy(panel.gameObject); return; }
        panel.gameObject.SetActive(false);
        q.Enqueue(panel);
    }

    public void ClearAll()
    {
        foreach (var kv in _cache)
            while (kv.Value.Count > 0) Object.Destroy(kv.Value.Dequeue()?.gameObject);
        _cache.Clear();
    }

    public int TotalCount
    {
        get { int c = 0; foreach (var kv in _cache) c += kv.Value.Count; return c; }
    }
}
