using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HybridCLR 编译后将热更 DLL 复制到 StreamingAssets，改后缀为 .bytes 方便 YooAsset 打包。
/// 菜单：Tools → Copy HotUpdate DLLs for YooAsset
/// </summary>
public class HotUpdateDllCopier : EditorWindow
{
    private const string SourceDir = "HybridCLRData/HotUpdateDlls/WebGL";
    private const string TargetDir = "Assets/Res/HotDll";
    private const string LegacyTargetDir = "Assets/StreamingAssets/HotUpdateDlls";

    public static void CopyDlls()
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var srcDir = Path.Combine(projectRoot, SourceDir);
        var dstDir = TargetDir;

        // 旧路径会被 Unity 原样收进 StreamingAssets，与 YooAsset Bundle 重复。
        // 在构建前清理它，只保留 Assets/Res/HotDll/HotUpdate.bytes 由 YooAsset 收集。
        RemoveLegacyStreamingAsset();

        if (!Directory.Exists(srcDir))
        {
            Debug.LogError($"[DllCopier] 源目录不存在: {srcDir}。请先执行 HybridCLR/Generate/All 编译热更 DLL。");
            return;
        }

        // 只需要热更业务 DLL，不需要引擎/框架 DLL（这些随包走 AOT）
        var hotUpdateDlls = new[] { "HotUpdate.dll" };
        int copied = 0;

        foreach (var dllName in hotUpdateDlls)
        {
            var srcPath = Path.Combine(srcDir, dllName);
            if (!File.Exists(srcPath))
            {
                Debug.LogWarning($"[DllCopier] 跳过（不存在）: {dllName}");
                continue;
            }

            var dstPath = Path.Combine(dstDir, dllName.Replace(".dll", ".bytes"));
            Directory.CreateDirectory(dstDir);
            File.Copy(srcPath, dstPath, true);
            copied++;
            Debug.Log($"[DllCopier] {dllName} → {dstPath}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"[DllCopier] 完成，复制了 {copied} 个 DLL → {dstDir}");

        if (copied > 0)
        {
            Debug.Log($"[DllCopier] HotUpdate.bytes 已就绪");
        }
    }

    public static void RemoveDlls()
    {
        foreach (var dllName in new[] { "HotUpdate.bytes" })
        {
            var path = Path.Combine(TargetDir, dllName);
            if (File.Exists(path))
                File.Delete(path);

        }

        AssetDatabase.Refresh();
        Debug.Log("[DllCopier] 已移除首包中的热更 DLL；运行时将从 YooAsset 网络资源加载。");
    }

    private static void RemoveLegacyStreamingAsset()
    {
        var legacyPath = Path.Combine(LegacyTargetDir, "HotUpdate.bytes");
        if (File.Exists(legacyPath))
            File.Delete(legacyPath);

        var legacyMetaPath = legacyPath + ".meta";
        if (File.Exists(legacyMetaPath))
            File.Delete(legacyMetaPath);
    }
}
