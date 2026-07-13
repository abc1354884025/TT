using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;

/// <summary>
/// YooAsset 初始化器。在 GameManager 之前执行，确保所有 UI 资源走 YooAsset 加载。
///
/// 挂载到 MainScene 的 GameObject 上，放在 GameManager 之前（Script Execution Order
/// 或同一 GameObject 上排在前面）。
/// </summary>
public class YooAssetBootstrap : MonoBehaviour
{
    [Header("基础配置")]
    [SerializeField] private string _packageName = "DefaultPackage";
    [SerializeField] private EPlayMode _playMode = EPlayMode.EditorSimulateMode;

    [Header("CDN 配置（HostPlayMode / WebPlayMode 时使用）")]
    [SerializeField] private string _cdnBaseUrl = "http://your-cdn.com/game/";
    [SerializeField] private string _fallbackVersion = "1.0.0";
    [SerializeField] private string _versionManifest = "version.json";

    [Header("开发设置")]
    [Tooltip("编辑器下用 EditorSimulateMode（直接加载，不走 AB）")]
    [SerializeField] private bool _editorSimulate = true;

    private string _resolvedVersion;

    public bool IsDone { get; private set; }
    public string PackageVersion => _resolvedVersion;

    private void Start()
    {
        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        Debug.Log($"[YooAsset] ===== 开始初始化 =====");

        // 1. 初始化 YooAsset 引擎
        YooAssets.Initialize();
        Debug.Log("[YooAsset] 引擎初始化完成");

        // 2. 获取或创建资源包
        ResourcePackage package;
        if (!YooAssets.TryGetPackage(_packageName, out package))
        {
            package = YooAssets.CreatePackage(_packageName);
            Debug.Log($"[YooAsset] 创建 Package: {_packageName}");
        }

        // 3. 确定 PlayMode 并初始化
        EPlayMode actualMode = ResolvePlayMode();
        _resolvedVersion = _fallbackVersion;

        var initOp = InitializePackage(package, actualMode);
        yield return initOp;

        if (initOp.Status != EOperationStatus.Succeeded)
        {
            Debug.LogError($"[YooAsset] Package 初始化失败: {initOp.Error}，退回 Resources");
            yield break;
        }
        Debug.Log($"[YooAsset] Package 初始化完成, 模式: {actualMode}");

        // 4. 版本和清单（CDN 模式）
        if (actualMode == EPlayMode.HostPlayMode || actualMode == EPlayMode.WebPlayMode)
        {
            yield return FetchVersionManifest();

            var versionOp = package.RequestPackageVersionAsync();
            yield return versionOp;

            if (versionOp.Status == EOperationStatus.Succeeded)
            {
                _resolvedVersion = versionOp.PackageVersion;
                Debug.Log($"[YooAsset] CDN 最新版本: {_resolvedVersion}");
            }

            var manifestOptions = new LoadPackageManifestOptions(_resolvedVersion, timeout: 60);
            var manifestOp = package.LoadPackageManifestAsync(manifestOptions);
            yield return manifestOp;

            if (manifestOp.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError($"[YooAsset] 清单加载失败: {manifestOp.Error}");
                yield break;
            }
            Debug.Log("[YooAsset] 资源清单加载完成");
        }

        // 5. 创建 Provider 并注入 UIManager
        var provider = new YooAssetProvider(this, _packageName);
        UIManager.Instance.SetResourceProvider(provider);
        Debug.Log("[YooAsset] YooAssetProvider 已注入 UIManager");

        IsDone = true;
        Debug.Log($"[YooAsset] ===== 初始化完成, 版本: {_resolvedVersion} =====");
    }

    private EPlayMode ResolvePlayMode()
    {
#if UNITY_EDITOR
        if (_editorSimulate)
            return EPlayMode.EditorSimulateMode;
#endif
        return _playMode;
    }

    private InitializePackageOperation InitializePackage(ResourcePackage package, EPlayMode mode)
    {
        switch (mode)
        {
            case EPlayMode.EditorSimulateMode:
            {
                var options = new EditorSimulateModeOptions();
                options.EditorFileSystemParameters =
                    FileSystemParameters.CreateDefaultEditorFileSystemParameters(_packageName);
                return package.InitializePackageAsync(options);
            }

            case EPlayMode.OfflinePlayMode:
            {
                var options = new OfflinePlayModeOptions();
                options.BuiltinFileSystemParameters =
                    FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
                return package.InitializePackageAsync(options);
            }

            case EPlayMode.HostPlayMode:
            {
                var options = new HostPlayModeOptions();
                options.BuiltinFileSystemParameters =
                    FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
                options.CacheFileSystemParameters =
                    FileSystemParameters.CreateDefaultSandboxFileSystemParameters(
                        new CdnRemoteService(_cdnBaseUrl, _resolvedVersion));
                return package.InitializePackageAsync(options);
            }

            case EPlayMode.WebPlayMode:
            {
                var options = new WebPlayModeOptions();
                options.WebServerFileSystemParameters =
                    FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                options.WebNetworkFileSystemParameters =
                    FileSystemParameters.CreateDefaultWebNetworkFileSystemParameters(
                        new CdnRemoteService(_cdnBaseUrl, _resolvedVersion));
                return package.InitializePackageAsync(options);
            }

            default:
                Debug.LogError($"[YooAsset] 不支持的 PlayMode: {mode}");
                return null;
        }
    }

    private IEnumerator FetchVersionManifest()
    {
        string url = $"{_cdnBaseUrl}{_versionManifest}";
        using var req = UnityWebRequest.Get(url);
        req.timeout = 10;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var manifest = JsonUtility.FromJson<VersionManifest>(req.downloadHandler.text);
                if (manifest != null && !string.IsNullOrEmpty(manifest.version))
                {
                    _resolvedVersion = manifest.version;
                    Debug.Log($"[YooAsset] 版本清单: {_resolvedVersion}");
                }
            }
            catch { Debug.LogWarning("[YooAsset] 版本清单解析失败"); }
        }
    }
}

/// <summary>
/// CDN 远程服务实现。YooAsset v3 使用 IRemoteService 接口。
/// </summary>
public class CdnRemoteService : IRemoteService
{
    private readonly string _baseUrl;
    private readonly string _version;

    public CdnRemoteService(string baseUrl, string version)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _version = version;
    }

    public IReadOnlyList<string> GetRemoteUrls(string fileName)
    {
        return new[] { $"{_baseUrl}/{_version}/{fileName}" };
    }
}
