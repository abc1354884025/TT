using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 基于 AssetBundle 的资源加载器。从 CDN 下载 AB 包后加载 Prefab，支持热更。
/// 兼容抖音 StarkSDK 的 TTAssetBundle 加载方式。
///
/// 用法：
///   1. 先用 DownloadBundle(path, url) 从 CDN 下载 AB
///   2. 再正常调用 Open 面板，InstantiateAsync 自动从缓存 AB 中加载
/// </summary>
public class AssetBundleProvider : IResourceProvider
{
    private readonly MonoBehaviour _runner;
    private readonly Dictionary<string, AssetBundle> _bundles = new Dictionary<string, AssetBundle>();
    private readonly Dictionary<string, RefCount> _refs = new Dictionary<string, RefCount>();

    /// <summary>AB 包路径映射：资源路径 → (AB 名, Asset 名)</summary>
    private readonly Dictionary<string, (string bundleName, string assetName)> _pathMap
        = new Dictionary<string, (string, string)>();

    public AssetBundleProvider(MonoBehaviour runner) { _runner = runner; }

    #region AB 下载

    /// <summary>
    /// 从远端下载 AssetBundle（协程）
    /// </summary>
    /// <param name="bundleName">AB 包名（如 "ui_panels"）</param>
    /// <param name="url">CDN 地址</param>
    /// <param name="onComplete">完成回调（success）</param>
    public void DownloadBundle(string bundleName, string url, Action<bool> onComplete = null)
    {
        _runner.StartCoroutine(DownloadRoutine(bundleName, url, onComplete));
    }

    private IEnumerator DownloadRoutine(string bundleName, string url, Action<bool> onComplete)
    {
        Debug.Log($"[AssetBundleProvider] 开始下载 AB: {bundleName} ← {url}");

        // 优先用 UnityWebRequest（WebGL 兼容）
        using var req = UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(url);
        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[AssetBundleProvider] 下载失败: {bundleName}, {req.error}");
            onComplete?.Invoke(false);
            yield break;
        }

        var ab = UnityEngine.Networking.DownloadHandlerAssetBundle.GetContent(req);
        if (ab == null)
        {
            Debug.LogError($"[AssetBundleProvider] AB 为空: {bundleName}");
            onComplete?.Invoke(false);
            yield break;
        }

        // 如果已有同名 AB，先卸载
        if (_bundles.TryGetValue(bundleName, out var oldAb))
            oldAb.Unload(false);

        _bundles[bundleName] = ab;
        Debug.Log($"[AssetBundleProvider] 下载完成: {bundleName}");
        onComplete?.Invoke(true);
    }

    /// <summary>
    /// 注册资源路径映射。将逻辑路径映射到 AB 中的 Asset。
    /// 例：RegisterPath("UI/Panels/ShopPanel", "ui_panels", "ShopPanel")
    /// </summary>
    public void RegisterPath(string resourcePath, string bundleName, string assetName)
    {
        _pathMap[resourcePath] = (bundleName, assetName);
    }

    /// <summary>检查 AB 是否已下载</summary>
    public bool IsBundleReady(string bundleName) => _bundles.ContainsKey(bundleName);

    #endregion

    #region IResourceProvider 实现

    public T Load<T>(string path) where T : UnityEngine.Object
    {
        if (!TryResolve(path, out var bundleName, out var assetName))
            return null;

        if (!_bundles.TryGetValue(bundleName, out var ab))
        {
            Debug.LogError($"[AssetBundleProvider] AB 未下载: {bundleName}");
            return null;
        }

        var asset = ab.LoadAsset<T>(assetName);
        if (asset != null) AddRef(path);
        else Debug.LogError($"[AssetBundleProvider] 加载失败: {assetName} in {bundleName}");
        return asset;
    }

    public void LoadAsync<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
    {
        _runner.StartCoroutine(LoadAssetRoutine(path, onLoaded));
    }

    private IEnumerator LoadAssetRoutine<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
    {
        if (!TryResolve(path, out var bundleName, out var assetName))
        { onLoaded?.Invoke(null); yield break; }

        if (!_bundles.TryGetValue(bundleName, out var ab))
        { Debug.LogError($"[AssetBundleProvider] AB 未下载: {bundleName}"); onLoaded?.Invoke(null); yield break; }

        var req = ab.LoadAssetAsync<T>(assetName);
        yield return req;
        var asset = req.asset as T;
        if (asset != null) AddRef(path);
        else Debug.LogError($"[AssetBundleProvider] 异步加载失败: {assetName} in {bundleName}");
        onLoaded?.Invoke(asset);
    }

    public void InstantiateAsync(string path, Transform parent, Action<GameObject> onLoaded)
    {
        _runner.StartCoroutine(InstantiateRoutine(path, parent, onLoaded));
    }

    private IEnumerator InstantiateRoutine(string path, Transform parent, Action<GameObject> onLoaded)
    {
        if (!TryResolve(path, out var bundleName, out var assetName))
        { onLoaded?.Invoke(null); yield break; }

        if (!_bundles.TryGetValue(bundleName, out var ab))
        { Debug.LogError($"[AssetBundleProvider] AB 未下载: {bundleName}"); onLoaded?.Invoke(null); yield break; }

        var req = ab.LoadAssetAsync<GameObject>(assetName);
        yield return req;
        var prefab = req.asset as GameObject;
        if (prefab == null) { Debug.LogError($"[AssetBundleProvider] 实例化失败: {assetName}"); onLoaded?.Invoke(null); yield break; }
        AddRef(path);
        var instance = parent ? UnityEngine.Object.Instantiate(prefab, parent) : UnityEngine.Object.Instantiate(prefab);
        onLoaded?.Invoke(instance);
    }

    public Sprite LoadSprite(string path)
    {
        return Load<Sprite>(path);
    }

    public void Release(string path)
    {
        if (!_refs.TryGetValue(path, out var rc)) return;
        if (--rc.Count > 0) return;
        _refs.Remove(path);
    }

    public void DestroyInstance(GameObject instance)
    {
        if (instance) UnityEngine.Object.Destroy(instance);
    }

    /// <summary>卸载所有 AB（切场景/大版本更新时调用）</summary>
    public void UnloadAll()
    {
        foreach (var ab in _bundles.Values) ab.Unload(false);
        _bundles.Clear();
        _refs.Clear();
    }

    #endregion

    #region 辅助

    private bool TryResolve(string path, out string bundleName, out string assetName)
    {
        if (_pathMap.TryGetValue(path, out var pair))
        {
            bundleName = pair.bundleName;
            assetName = pair.assetName;
            return true;
        }
        // fallback：将路径拆分为 AB 名 + Asset 名
        var parts = path.Split('/');
        if (parts.Length >= 2)
        {
            bundleName = parts[parts.Length - 2];
            assetName = parts[parts.Length - 1];
            return true;
        }
        Debug.LogError($"[AssetBundleProvider] 无法解析路径: {path}");
        bundleName = assetName = null;
        return false;
    }

    private void AddRef(string path)
    {
        if (!_refs.ContainsKey(path)) _refs[path] = new RefCount();
        _refs[path].Count++;
    }

    private class RefCount { public int Count; }

    #endregion
}
