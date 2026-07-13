using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// YooAsset Build 后自动拷贝内置清单到 StreamingAssets。
/// </summary>
public class YooAssetBuildHook : AssetPostprocessor
{
    [MenuItem("Tools/Copy YooAsset Builtin To StreamingAssets")]
    public static void CopyBuiltin()
    {
        var srcDir = Path.Combine(Application.dataPath, "../Bundles/WebGL/DefaultPackage");
        if (!Directory.Exists(srcDir))
        {
            Debug.LogError($"[YooAssetHook] 源目录不存在: {srcDir}。请先用 YooAsset 打 WebGL 包。");
            return;
        }

        // 找最新版本目录
        var dirs = Directory.GetDirectories(srcDir);
        string latest = "";
        foreach (var d in dirs)
            if (string.Compare(Path.GetFileName(d), latest) > 0)
                latest = Path.GetFileName(d);

        if (string.IsNullOrEmpty(latest))
        {
            Debug.LogError("[YooAssetHook] 没有版本目录");
            return;
        }

        var versionDir = Path.Combine(srcDir, latest);
        var dstDir = Path.Combine(Application.dataPath, "StreamingAssets/yoo/DefaultPackage");
        Directory.CreateDirectory(dstDir);

        // 拷贝 BuiltinCatalog
        foreach (var file in Directory.GetFiles(versionDir, "*.bytes"))
        {
            var dst = Path.Combine(dstDir, "BuiltinCatalog.bytes");
            File.Copy(file, dst, true);
            Debug.Log($"[YooAssetHook] {Path.GetFileName(file)} → {dst}");
        }

        // 拷贝 .hash
        foreach (var file in Directory.GetFiles(versionDir, "*.hash"))
        {
            var dst = Path.Combine(dstDir, "BuiltinCatalog.hash");
            File.Copy(file, dst, true);
        }

        // 拷贝 .version
        foreach (var file in Directory.GetFiles(versionDir, "*.version"))
        {
            var dst = Path.Combine(dstDir, "DefaultPackage.version");
            File.Copy(file, dst, true);
        }

        AssetDatabase.Refresh();
        Debug.Log($"[YooAssetHook] 完成！版本: {latest}");
    }
}
