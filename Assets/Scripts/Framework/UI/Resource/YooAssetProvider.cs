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
        if (package == null || !package.PackageValid)
        { Debug.LogError($"[YooAssetProvider] 同步加载失败，Package 未就绪: {_packageName}"); return null; }

        var handle = package.LoadAssetSync<T>(path);
        if (handle.Status == EOperationStatus.Succeeded)
        {
            AddRef(path, handle);
            return handle.GetAssetObject<T>();
        }

        Debug.LogError($"[YooAssetProvider] 同步加载失败: {path}, {handle.Error}");
        return null;
    }

    public void LoadAsync<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
    {
        _runner.StartCoroutine(LoadAsyncRoutine<T>(path, onLoaded));
    }

    private IEnumerator LoadAsyncRoutine<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
    {
        var package = GetPackage();
        if (package == null || !package.PackageValid)
        { Debug.LogError($"[YooAssetProvider] 异步加载失败，Manifest 未激活: {path}"); onLoaded?.Invoke(null); yield break; }

        var handle = package.LoadAssetAsync<T>(path);
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

        if (!package.PackageValid)
        {
            Debug.LogError($"[YooAssetProvider] Manifest 未激活，请先 LoadPackageManifestAsync。path={path}");
            onLoaded?.Invoke(null);
            yield break;
        }

        var handle = package.LoadAssetAsync<GameObject>(path);
        yield return handle;

        if (handle.Status == EOperationStatus.Succeeded)
        {
            AddRef(path, handle);

            var options = new InstantiateOptions(true, parent, false);
            var instOp = handle.InstantiateAsync(options);
            yield return instOp;

            onLoaded?.Invoke(instOp.Result);
        }
        else
        {
            Debug.LogError($"[YooAssetProvider] 实例化失败: {path}, {handle.Error}");
            onLoaded?.Invoke(null);
        }
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
