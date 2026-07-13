using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/// <summary>
/// 基于 YooAsset 的资源加载器。所有 UI 面板、Widget、Sprite 统一走 YooAsset。
/// 带引用计数，Release 归零时释放 AssetHandle。
///
/// 前提：调用前已完成 YooAssets.Initialize() 和 Package.InitializePackageAsync()。
/// </summary>
public class YooAssetProvider : IResourceProvider
{
    private readonly MonoBehaviour _runner;
    private readonly string _packageName;
    private readonly Dictionary<string, HandleRef> _handles = new Dictionary<string, HandleRef>();

    public YooAssetProvider(MonoBehaviour runner, string packageName = "DefaultPackage")
    {
        _runner = runner;
        _packageName = packageName;
    }

    private ResourcePackage GetPackage()
    {
        return YooAssets.TryGetPackage(_packageName, out var pkg) ? pkg : null;
    }

    #region IResourceProvider

    public T Load<T>(string path) where T : UnityEngine.Object
    {
        var package = GetPackage();
        if (package == null) return null;

        try
        {
            var handle = package.LoadAssetSync<T>(path);
            if (handle.Status == EOperationStatus.Succeeded)
            {
                AddRef(path, handle);
                return handle.GetAssetObject<T>();
            }
            Debug.LogError($"[YooAssetProvider] 同步加载失败: {path}, {handle.Error}");
        }
        catch (Exception e) { Debug.LogError($"[YooAssetProvider] 同步加载异常: {path}, {e.Message}"); }
        return null;
    }

    public void LoadAsync<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
    {
        _runner.StartCoroutine(LoadAsyncRoutine<T>(path, onLoaded));
    }

    private IEnumerator LoadAsyncRoutine<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
    {
        var package = GetPackage();
        if (package == null) { onLoaded?.Invoke(null); yield break; }

        AssetHandle handle;
        try { handle = package.LoadAssetAsync<T>(path); }
        catch (Exception e) { Debug.LogError($"[YooAssetProvider] 异步加载异常: {path}, {e.Message}"); onLoaded?.Invoke(null); yield break; }

        yield return handle;

        if (handle.Status == EOperationStatus.Succeeded)
        {
            AddRef(path, handle);
            onLoaded?.Invoke(handle.GetAssetObject<T>());
        }
        else
        {
            Debug.LogError($"[YooAssetProvider] 异步加载失败: {path}, {handle.Error}");
            onLoaded?.Invoke(null);
        }
    }

    public void InstantiateAsync(string path, Transform parent, Action<GameObject> onLoaded)
    {
        _runner.StartCoroutine(InstantiateRoutine(path, parent, onLoaded));
    }

    private IEnumerator InstantiateRoutine(string path, Transform parent, Action<GameObject> onLoaded)
    {
        var package = GetPackage();
        if (package == null) { onLoaded?.Invoke(null); yield break; }

        var handle = LoadWithFallback(package, path);
        if (handle == null) { onLoaded?.Invoke(null); yield break; }
        yield return handle;

        if (handle.Status == EOperationStatus.Succeeded)
        {
            AddRef(path, handle);
            var instOp = handle.InstantiateAsync(new InstantiateOptions(true, parent, false));
            yield return instOp;
            onLoaded?.Invoke(instOp.Result);
        }
        else
        {
            Debug.LogError($"[YooAssetProvider] 实例化失败: {path}, {handle.Error}");
            onLoaded?.Invoke(null);
        }
    }

    /// <summary>尝试多种地址格式加载，兼容 AddressByFileName</summary>
    private AssetHandle LoadWithFallback(ResourcePackage package, string path)
    {
        // 1. 原路径
        var h = package.LoadAssetAsync<GameObject>(path);
        if (h.Status != EOperationStatus.Failed) return h;

        // 2. 只要文件名（AddressByFileName 模式）
        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        if (fileName != path && !string.IsNullOrEmpty(fileName))
        {
            h = package.LoadAssetAsync<GameObject>(fileName);
            if (h.Status != EOperationStatus.Failed) return h;
        }

        // 3. 文件名.扩展名
        var fileWithExt = System.IO.Path.GetFileName(path);
        if (fileWithExt != fileName && !string.IsNullOrEmpty(fileWithExt))
        {
            h = package.LoadAssetAsync<GameObject>(fileWithExt);
            if (h.Status != EOperationStatus.Failed) return h;
        }

        Debug.LogError($"[YooAssetProvider] 所有地址格式均失败: {path}");
        return h;
    }

    public Sprite LoadSprite(string path)
    {
        return Load<Sprite>(path);
    }

    public void Release(string path)
    {
        if (!_handles.TryGetValue(path, out var hr)) return;
        hr.RefCount--;
        if (hr.RefCount <= 0)
        {
            hr.Handle.Release();
            _handles.Remove(path);
        }
    }

    public void DestroyInstance(GameObject instance)
    {
        if (instance)
            UnityEngine.Object.Destroy(instance);
    }

    #endregion

    private void AddRef(string path, AssetHandle handle)
    {
        if (!_handles.ContainsKey(path))
            _handles[path] = new HandleRef { Handle = handle };
        _handles[path].RefCount++;
    }

    private class HandleRef
    {
        public AssetHandle Handle;
        public int RefCount;
    }
}
