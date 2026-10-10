using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image 扩展：异步加载并设置 Sprite，自动处理对象销毁和重复调用。
///
/// 用法：_icon.SetSpriteAsync("Icons/sword_01");
/// </summary>
public static class UIImageExtensions
{
    private static readonly Dictionary<int, string> _pending = new Dictionary<int, string>();

    /// <summary>
    /// 异步加载 Sprite 并设置到 Image，自动处理：
    /// - Image 销毁后忽略回调
    /// - 多次调用只生效最后一次
    /// </summary>
    public static void SetSpriteAsync(this Image image, string path)
    {
        if (!image || string.IsNullOrEmpty(path)) return;

        var id = image.GetInstanceID();
        var expectedPath = path;
        _pending[id] = expectedPath;

        ResourceManager.LoadAsync<Sprite>(path, sprite =>
        {
            if (!image) { _pending.Remove(id); return; }
            if (!_pending.TryGetValue(id, out var current) || current != expectedPath) return;
            _pending.Remove(id);
            image.sprite = sprite;
        });
    }
}
