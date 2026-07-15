using System;
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

    /// <summary>异步加载资源。供热更层加载装备视觉与战斗特效。</summary>
    public static void LoadAsync<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            onLoaded?.Invoke(null);
            return;
        }

        if (_provider != null)
        {
            _provider.LoadAsync(path, onLoaded);
            return;
        }

        var asset = Resources.Load<T>(path);
        onLoaded?.Invoke(asset);
    }

    /// <summary>异步实例化视觉 Prefab。缺失时返回 null，由调用者自行降级。</summary>
    public static void InstantiateAsync(string path, Transform parent, Action<GameObject> onLoaded)
    {
        if (string.IsNullOrEmpty(path))
        {
            onLoaded?.Invoke(null);
            return;
        }

        if (_provider != null)
        {
            _provider.InstantiateAsync(path, parent, onLoaded);
            return;
        }

        var prefab = Resources.Load<GameObject>(path);
        onLoaded?.Invoke(prefab ? UnityEngine.Object.Instantiate(prefab, parent) : null);
    }

    /// <summary>GameManager 等待 HotUpdateBootstrap 完成</summary>
    public static bool IsYooAssetReady => IsBootstrapDone;

    /// <summary>当前 Provider</summary>
    public static IResourceProvider Provider => _provider;
}
