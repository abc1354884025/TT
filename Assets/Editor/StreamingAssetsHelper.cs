using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 从 YooAsset Build 输出中自动拷贝内置清单到 StreamingAssets。
/// 菜单：Tools → Sync BuiltinCatalog to StreamingAssets
/// </summary>
public class StreamingAssetsHelper
{
    [MenuItem("Tools/Sync BuiltinCatalog to StreamingAssets")]
    public static void Sync()
    {
        var srcDir = Path.Combine(Application.dataPath, "../Bundles/WebGL/DefaultPackage");
        if (!Directory.Exists(srcDir))
        {
            Debug.LogError($"[Sync] 源目录不存在: {srcDir}。请先用 YooAsset 打 WebGL 包。");
            return;
        }

        // 找最新版本
        var dirs = Directory.GetDirectories(srcDir);
        if (dirs.Length == 0) { Debug.LogError("[Sync] 没有版本目录"); return; }
        System.Array.Sort(dirs);
        var versionDir = dirs[dirs.Length - 1];

        var dstDir = Path.Combine(Application.dataPath, "StreamingAssets/yoo/DefaultPackage");
        Directory.CreateDirectory(dstDir);

        // catalog .bytes → BuiltinCatalog.bytes
        foreach (var f in Directory.GetFiles(versionDir, "*.bytes"))
        {
            var dst = Path.Combine(dstDir, "BuiltinCatalog.bytes");
            File.Copy(f, dst, true);
            Debug.Log($"[Sync] {Path.GetFileName(f)} → BuiltinCatalog.bytes");
        }
        foreach (var f in Directory.GetFiles(versionDir, "*.hash"))
        {
            var dst = Path.Combine(dstDir, "BuiltinCatalog.hash");
            File.Copy(f, dst, true);
        }
        foreach (var f in Directory.GetFiles(versionDir, "*.version"))
        {
            var dst = Path.Combine(dstDir, "DefaultPackage.version");
            File.Copy(f, dst, true);
        }

        // .bundle 文件也拷进去（内置资源）
        foreach (var f in Directory.GetFiles(versionDir, "*.bundle"))
        {
            var dst = Path.Combine(dstDir, Path.GetFileName(f));
            File.Copy(f, dst, true);
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Sync] 完成！从 {Path.GetFileName(versionDir)} 同步了 {Directory.GetFiles(versionDir).Length} 个文件到 StreamingAssets");
    }
}
