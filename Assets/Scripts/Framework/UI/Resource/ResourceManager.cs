using UnityEngine;

/// <summary>
/// 统一资源加载入口。UIManager 切换 Provider 后自动生效。
/// 所有 Widget 通过此类加载资源，不直接调 Resources.Load。
/// </summary>
public static class ResourceManager
{
    private static IResourceProvider _provider;
    /// <summary>Bootstrap 完成标志（GameManager 在等这个）</summary>
    public static bool IsBootstrapDone;

    public static void SetProvider(IResourceProvider provider)
    {
        _provider = provider;
    }

    public static Sprite LoadSprite(string path)
    {
        if (_provider != null)
            return _provider.LoadSprite(path);
        Debug.LogWarning($"[ResourceManager] Provider 未设置");
        return Resources.Load<Sprite>(path);
    }

    /// <summary>GameManager 等待 HotUpdateBootstrap 完成</summary>
    public static bool IsYooAssetReady => IsBootstrapDone;

    /// <summary>当前 Provider</summary>
    public static IResourceProvider Provider => _provider;
}
