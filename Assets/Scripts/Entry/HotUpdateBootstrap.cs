using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;

/// <summary>
/// HybridCLR 热更启动器。挂载在启动场景的 GameObject 上。
///
/// 流程：
///   1. 从 CDN 下载热更 DLL 字节码
///   2. Assembly.Load 加载 DLL → 注入 UIManager
///   3. 从 CDN 下载 UI AssetBundle
///   4. 切换 UIManager 到 AssetBundleProvider
///   5. 打开首个面板（从热更 DLL 中的类型）
///
/// 兼容非热更模式：CDN 不可用时 fallback 到本地 Resources。
/// </summary>
public class HotUpdateBootstrap : MonoBehaviour
{
    [Header("开发设置")]
    [Tooltip("开发模式：跳过 CDN 下载，直接用本地 Resources 和已加载的程序集")]
    [SerializeField] private bool _devMode = true;

    [Header("CDN 配置")]
    [Tooltip("CDN 基础 URL")]
    [SerializeField] private string _cdnBaseUrl = "http://your-cdn.com/game/";

    [Tooltip("版本清单文件名（如 version.json）。应用启动时先拉此文件获取最新版本，避免版本号写死在包里")]
    [SerializeField] private string _versionManifest = "version.json";

    [Tooltip("热更 DLL 文件名列表（不含路径），如 HotUpdate.dll")]
    [SerializeField] private string[] _hotUpdateDlls = new[] { "HotUpdate.dll" };

    [Tooltip("UI AB 包列表（不含 URL 前缀），如 ui_panels")]
    [SerializeField] private string[] _uiBundles = new[] { "ui_panels" };

    [Tooltip("兜底版本号：版本清单拉不到时使用")]
    [SerializeField] private string _fallbackVersion = "1.0.0";

    [Header("启动设置")]
    [Tooltip("热更完成后自动打开的面板")]
    [SerializeField] private string _startPanel;

    [Tooltip("CDN 下载失败时是否退回本地 Resources 模式")]
    [SerializeField] private bool _fallbackToResources = true;

    [Tooltip("超时秒数（0 = 不限）")]
    [SerializeField] private float _timeoutSeconds = 30f;

    #region 启动

    private void Start()
    {
        StartCoroutine(Bootstrap());
    }

    private IEnumerator Bootstrap()
    {
        Debug.Log($"[HotUpdate] ===== 开始热更流程 =====");
        var startTime = Time.realtimeSinceStartup;

        // --- 阶段 1: 初始化 AOT 框架 ---
        Debug.Log("[HotUpdate] 阶段 1/4: 初始化 AOT 框架...");
        var ui = UIManager.Instance;
        yield return null; // 等一帧确保 Canvas 创建

        Assembly hotUpdateAss = null;
        string resolvedVersion = _fallbackVersion;

        if (_devMode)
        {
            // --- 开发模式：跳过 CDN 下载，直接使用本地 ---
            Debug.Log("[HotUpdate] 开发模式：跳过 CDN 下载，使用本地 Resources + 已加载程序集");
        }
        else
        {
            // --- 拉取版本清单，获取最新版本号 ---
            Debug.Log("[HotUpdate] 拉取版本清单...");
            string manifestUrl = $"{_cdnBaseUrl}{_versionManifest}";
            string manifestJson = null;
            yield return DownloadString(manifestUrl, s => manifestJson = s);

            if (!string.IsNullOrEmpty(manifestJson))
            {
                try
                {
                    var manifest = JsonUtility.FromJson<VersionManifest>(manifestJson);
                    if (manifest != null && !string.IsNullOrEmpty(manifest.version))
                    {
                        resolvedVersion = manifest.version;
                        Debug.Log($"[HotUpdate] 最新版本: {resolvedVersion}");
                    }
                }
                catch { Debug.LogWarning("[HotUpdate] 版本清单解析失败，使用兜底版本"); }
            }
            else
            {
                Debug.LogWarning("[HotUpdate] 版本清单拉取失败，使用兜底版本");
            }

            // --- 阶段 2: 下载并加载热更 DLL ---
            Debug.Log("[HotUpdate] 阶段 2/4: 下载热更 DLL...");

            foreach (var dllName in _hotUpdateDlls)
            {
                var url = $"{_cdnBaseUrl}{resolvedVersion}/{dllName}";
                byte[] dllBytes = null;
                yield return DownloadBytes(url, bytes => dllBytes = bytes);

                if (dllBytes != null && dllBytes.Length > 0)
                {
                    try
                    {
                        // HybridCLR: 加载热更 DLL
                        // 如果用 HybridCLR 的 RuntimeApi，替换下面这行：
                        // hotUpdateAss = RuntimeApi.LoadAssembly(dllBytes);
                        hotUpdateAss = Assembly.Load(dllBytes);
                        Debug.Log($"[HotUpdate] DLL 加载成功: {dllName} ({dllBytes.Length / 1024} KB)");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[HotUpdate] DLL 加载失败: {dllName}, {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[HotUpdate] DLL 下载失败: {url}");
                }
            }

            if (hotUpdateAss != null)
            {
                ui.SetHotUpdateAssembly(hotUpdateAss);
            }
            else if (!_fallbackToResources)
            {
                Debug.LogError("[HotUpdate] 热更 DLL 全部加载失败且不允许退回！");
                yield break;
            }
            else
            {
                Debug.LogWarning("[HotUpdate] 热更 DLL 不可用，退回本地 Resources 模式");
            }

            // --- 阶段 3: YooAsset 由 YooAssetBootstrap 独立处理 ---
            Debug.Log("[HotUpdate] 阶段 3/4: 跳过(YooAssetBootstrap 处理)");
        }

        // --- 阶段 4: 注入热更 Assembly ---
        Debug.Log("[HotUpdate] 阶段 4/4: 注入热更 Assembly...");
        if (hotUpdateAss != null)
            ui.SetHotUpdateAssembly(hotUpdateAss);

        var elapsed = Time.realtimeSinceStartup - startTime;
        Debug.Log($"[HotUpdate] ===== 框架就绪，耗时 {elapsed:F1}s =====");
    }

    #endregion

    #region 辅助

    /// <summary>下载文本数据（用于版本清单等）</summary>
    private IEnumerator DownloadString(string url, Action<string> onComplete)
    {
        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.CeilToInt(_timeoutSeconds > 0 ? _timeoutSeconds : 10);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            onComplete?.Invoke(req.downloadHandler.text);
        else
        {
            Debug.LogWarning($"[HotUpdate] 下载失败: {url}, {req.error}");
            onComplete?.Invoke(null);
        }
    }

    /// <summary>下载字节数据</summary>
    private IEnumerator DownloadBytes(string url, Action<byte[]> onComplete)
    {
        using var req = UnityEngine.Networking.UnityWebRequest.Get(url);
        req.timeout = Mathf.CeilToInt(_timeoutSeconds > 0 ? _timeoutSeconds : 30);

        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            onComplete?.Invoke(req.downloadHandler.data);
        }
        else
        {
            Debug.LogWarning($"[HotUpdate] 下载失败: {url}, {req.error}");
            onComplete?.Invoke(null);
        }
    }

    /// <summary>同步初始化（不使用协程，适合简单场景）</summary>
    public void InitSync()
    {
        var ui = UIManager.Instance;
        if (!string.IsNullOrEmpty(_startPanel))
            ui.Open(_startPanel);
    }

    #endregion
}

/// <summary>传给首个面板的启动数据</summary>
public class StartPanelData
{
    public string Version;
    public bool IsHotUpdated;
}

/// <summary>CDN 版本清单。version.json 的格式：
/// { "version": "2.0.0", "note": "修复了xxx" }</summary>
[Serializable]
public class VersionManifest
{
    public string version;
    public string note;
}
