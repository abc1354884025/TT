using UnityEngine;

/// <summary>
/// 统一资源加载入口。UIManager 切换 Provider 后自动生效。
/// 所有 Widget 通过此类加载资源，不直接调 Resources.Load。
/// </summary>
public static class ResourceManager
{
    private static IResourceProvider _provider;

    /// <summary>由 UIManager.SetResourceProvider 调用</summary>
    public static void SetProvider(IResourceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>加载 Sprite（图标等）</summary>
    public static Sprite LoadSprite(string path)
    {
        if (_provider != null)
            return _provider.LoadSprite(path);

        // fallback
        Debug.LogWarning($"[ResourceManager] Provider 未设置，fallback Resources.Load: {path}");
        return Resources.Load<Sprite>(path);
    }

    /// <summary>YooAsset 是否已初始化完成（Provider 已设置且不是 ResourcesProvider）</summary>
    public static bool IsYooAssetReady => _provider != null;

    /// <summary>当前 Provider</summary>
    public static IResourceProvider Provider => _provider;
}
