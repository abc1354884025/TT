using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 基于 Resources 的资源加载器。开发期/整包发布时使用。
/// 带引用计数——Release 归零时才真正卸载。
/// </summary>
public class ResourcesProvider : IResourceProvider
{
    private readonly Dictionary<string, RefCount> _refs = new Dictionary<string, RefCount>();
    private readonly MonoBehaviour _runner;

    public ResourcesProvider(MonoBehaviour runner) { _runner = runner; }

    public T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = Resources.Load<T>(path);
        if (asset != null) AddRef(path, asset);
        else Debug.LogError($"[ResourcesProvider] 加载失败: {path}");
        return asset;
    }

    public void LoadAsync<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
    {
        _runner.StartCoroutine(LoadRoutine(path, onLoaded));
    }

    private IEnumerator LoadRoutine<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
    {
        var req = Resources.LoadAsync<T>(path);
        yield return req;
        var asset = req.asset as T;
        if (asset != null) AddRef(path, asset);
        else Debug.LogError($"[ResourcesProvider] 异步加载失败: {path}");
        onLoaded?.Invoke(asset);
    }

    public void InstantiateAsync(string path, Transform parent, Action<GameObject> onLoaded)
    {
        _runner.StartCoroutine(InstantiateRoutine(path, parent, onLoaded));
    }

    private IEnumerator InstantiateRoutine(string path, Transform parent, Action<GameObject> onLoaded)
    {
        var req = Resources.LoadAsync<GameObject>(path);
        yield return req;
        var prefab = req.asset as GameObject;
        if (prefab == null) { Debug.LogError($"[ResourcesProvider] 实例化失败: {path}"); onLoaded?.Invoke(null); yield break; }
        AddRef(path, prefab);
        var instance = parent ? UnityEngine.Object.Instantiate(prefab, parent) : UnityEngine.Object.Instantiate(prefab);
        onLoaded?.Invoke(instance);
    }

    public void Release(string path)
    {
        if (!_refs.TryGetValue(path, out var rc)) return;
        rc.Count--;
        if (rc.Count <= 0)
        {
            if (rc.Asset != null) Resources.UnloadAsset(rc.Asset);
            _refs.Remove(path);
        }
    }

    public void DestroyInstance(GameObject instance)
    {
        if (instance) UnityEngine.Object.Destroy(instance);
    }

    private void AddRef(string path, UnityEngine.Object asset)
    {
        if (!_refs.ContainsKey(path)) _refs[path] = new RefCount();
        _refs[path].Count++;
        _refs[path].Asset = asset;
    }

    private class RefCount { public int Count; public UnityEngine.Object Asset; }
}
