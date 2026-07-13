using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// YooAsset 资源包上传工具。构建完成后一键同步到火山引擎 TOS。
/// 菜单：Tools → YooAsset Uploader
///
/// TOS 上传依赖 tosutil（火山引擎官方 CLI）:
///   tosutil config -e http://tos-cn-xxx.volces.com -ak <AK> -sk <SK>
/// </summary>
public class YooAssetUploader : EditorWindow
{
    private string _bundleSourcePath = "";
    private string _packageName = "DefaultPackage";

    // TOS
    private string _tosEndpoint = "tos-cn-shanghai.volces.com";
    private string _tosBucket = "";
    private string _tosAccessKey = "";
    private string _tosSecretKey = "";
    private string _tosPrefix = "game";
    private string _tosUtilPath = "";

    // 自动检测到的版本目录
    private string _detectedVersionDir = "";
    private string _detectedVersion = "";

    // 状态
    private bool _isWorking;
    private string _statusText = "";
    private readonly List<string> _logs = new List<string>();

    private void Log(string msg)
    {
        _logs.Add(msg);
        Debug.Log(msg);
    }

    [MenuItem("Tools/YooAsset Uploader")]
    public static void ShowWindow()
    {
        var window = GetWindow<YooAssetUploader>("YooAsset Uploader");
        window.minSize = new Vector2(480, 400);
        window.Show();
    }

    private void OnEnable()
    {
        _bundleSourcePath = EditorPrefs.GetString("Yau_Source",
            Path.Combine(Application.dataPath, "../Bundles"));
        _packageName = EditorPrefs.GetString("Yau_Pkg", "DefaultPackage");
        _tosEndpoint = EditorPrefs.GetString("Yau_Ep", "tos-cn-beijing.volces.com");
        _tosBucket = EditorPrefs.GetString("Yau_Bucket", "");
        _tosAccessKey = EditorPrefs.GetString("Yau_Ak", "");
        _tosSecretKey = EditorPrefs.GetString("Yau_Sk", "");
        _tosPrefix = EditorPrefs.GetString("Yau_Prefix", "game");
        _tosUtilPath = EditorPrefs.GetString("Yau_TosUtil",
            Path.Combine(Application.dataPath, "../Tools/tosutil.exe"));
    }

    private void OnDisable()
    {
        EditorPrefs.SetString("Yau_Source", _bundleSourcePath);
        EditorPrefs.SetString("Yau_Pkg", _packageName);
        EditorPrefs.SetString("Yau_Ep", _tosEndpoint);
        EditorPrefs.SetString("Yau_Bucket", _tosBucket);
        EditorPrefs.SetString("Yau_Ak", _tosAccessKey);
        EditorPrefs.SetString("Yau_Sk", _tosSecretKey);
        EditorPrefs.SetString("Yau_Prefix", _tosPrefix);
        EditorPrefs.SetString("Yau_TosUtil", _tosUtilPath);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("YooAsset 资源包上传", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        // 源路径
        _packageName = EditorGUILayout.TextField("包名", _packageName);
        _bundleSourcePath = EditorGUILayout.TextField("Bundles 根目录", _bundleSourcePath);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("自动填充", GUILayout.Width(80)))
            _bundleSourcePath = Path.Combine(Application.dataPath, "../Bundles");
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            var p = EditorUtility.OpenFolderPanel("Bundles 根目录", _bundleSourcePath, "");
            if (!string.IsNullOrEmpty(p)) _bundleSourcePath = p;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (!string.IsNullOrEmpty(_detectedVersion))
        {
            EditorGUILayout.LabelField($"  版本: {_detectedVersion}",
                new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.green } });
            EditorGUILayout.LabelField($"  目录: {_detectedVersionDir}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(6);

        // TOS 配置
        EditorGUILayout.LabelField("火山引擎 TOS", EditorStyles.boldLabel);
        _tosEndpoint = EditorGUILayout.TextField("Endpoint", _tosEndpoint);
        _tosBucket = EditorGUILayout.TextField("Bucket", _tosBucket);
        _tosPrefix = EditorGUILayout.TextField("对象前缀", _tosPrefix);

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("密钥", EditorStyles.miniLabel);
        _tosAccessKey = EditorGUILayout.TextField("AccessKey", _tosAccessKey);
        _tosSecretKey = EditorGUILayout.PasswordField("SecretKey", _tosSecretKey);

        EditorGUILayout.Space(2);
        _tosUtilPath = EditorGUILayout.TextField("tosutil 路径", _tosUtilPath);

        // 配置 tosutil 按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("配置 tosutil", GUILayout.Width(120)))
            ConfigTosUtil();
        EditorGUILayout.LabelField(
            "等效: tosutil config -e=http://... -re=... -i=<AK> -k=<SK>",
            EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("构建流程", EditorStyles.boldLabel);

        GUI.enabled = !_isWorking;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1. 编译热更 DLL", GUILayout.Height(28)))
        {
            EditorApplication.ExecuteMenuItem("HybridCLR/Generate/All");
            Log("✓ HybridCLR Generate/All 已执行");
        }
        if (GUILayout.Button("2. 扫描版本目录", GUILayout.Height(28)))
            DetectVersion();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        GUI.enabled = !_isWorking && !string.IsNullOrEmpty(_detectedVersion);
        if (GUILayout.Button("3. 同步到 TOS", GUILayout.Height(36)))
        {
            if (string.IsNullOrEmpty(_tosBucket))
            {
                Log("❌ 请先填写 Bucket 名称");
            }
            else
            {
                TosSync();
            }
        }
        GUI.enabled = true;

        if (_isWorking)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(_statusText, EditorStyles.boldLabel);
        }

        if (_logs.Count > 0)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
            foreach (var l in _logs)
                EditorGUILayout.LabelField(l, EditorStyles.wordWrappedLabel);
        }
    }

    #region 版本检测

    /// <summary>
    /// 扫描 YooAsset 构建输出，自动找到版本目录。
    /// 结构: {Bundles}/{Platform}/{Package}/{versionDir}/
    /// 版本目录包含 .bundle 文件和 .version 文件。
    /// </summary>
    private void DetectVersion()
    {
        _detectedVersion = "";
        _detectedVersionDir = "";
        _logs.Clear();

        Log($"扫描: {_bundleSourcePath}");

        try
        {
            if (string.IsNullOrEmpty(_bundleSourcePath) || !Directory.Exists(_bundleSourcePath))
            {
                Log($"❌ Bundles 目录不存在: {_bundleSourcePath}");
                Log("请先通过 YooAsset → AssetBundle Builder 构建资源包");
                Repaint();
                return;
            }

            // 遍历 Bundles/{任意平台}/{任意包名}/ 找包含 .bundle 的版本目录
            foreach (var platformDir in SafeGetDirectories(_bundleSourcePath))
            {
                foreach (var pkgDir in SafeGetDirectories(platformDir))
                {
                    if (Path.GetFileName(pkgDir) != _packageName && !string.IsNullOrEmpty(_packageName))
                        continue;

                    string bestVersionDir = null;
                    string bestVersion = "";
                    DateTime bestTime = DateTime.MinValue;

                    foreach (var verDir in SafeGetDirectories(pkgDir))
                    {
                        if (Path.GetFileName(verDir) == "OutputCache") continue;
                        string fullPath = verDir;
                        if (!Directory.Exists(fullPath)) continue;

                        try
                        {
                            var hasBundles = Directory.GetFiles(fullPath, "*.bundle",
                                SearchOption.TopDirectoryOnly).Length > 0;
                            if (!hasBundles) continue;

                            var writeTime = Directory.GetLastWriteTimeUtc(fullPath);
                            if (writeTime > bestTime)
                            {
                                bestTime = writeTime;
                                bestVersionDir = fullPath;

                                // 读 .version 文件
                                var vf = Directory.GetFiles(fullPath, "*.version",
                                    SearchOption.TopDirectoryOnly).FirstOrDefault();
                                bestVersion = vf != null
                                    ? File.ReadAllText(vf).Trim()
                                    : Path.GetFileName(verDir);
                            }
                        }
                        catch { /* 跳过无法访问的目录 */ }
                    }

                    if (bestVersionDir != null)
                    {
                        _detectedVersionDir = bestVersionDir;
                        _detectedVersion = bestVersion;

                        string platformName = Path.GetFileName(Path.GetDirectoryName(
                            Path.GetDirectoryName(bestVersionDir)));
                        string pkgName = Path.GetFileName(Path.GetDirectoryName(bestVersionDir));

                        string[] allBundles;
                        long totalSize;
                        try
                        {
                            allBundles = Directory.GetFiles(bestVersionDir, "*.bundle",
                                SearchOption.AllDirectories);
                            totalSize = allBundles.Sum(f => new FileInfo(f).Length);
                        }
                        catch { allBundles = new string[0]; totalSize = 0; }

                        Log($"✓ 平台: {platformName}");
                        Log($"✓ 包名: {pkgName}");
                        Log($"✓ 版本: {bestVersion}");
                        Log($"✓ bundle: {allBundles.Length} 个, 共 {FormatSize(totalSize)}");

                        Repaint();
                        return;
                    }
                }
            }

            Log("❌ 未找到构建产物");
            Log("请先通过 YooAsset → AssetBundle Builder 构建资源包");
        }
        catch (Exception e)
        {
            Log($"❌ 扫描异常: {e.GetType().Name}: {e.Message}");
            Debug.LogException(e);
        }

        Repaint();
    }

    /// <summary>安全获取子目录名，避免异常导致崩溃</summary>
    private static List<string> SafeGetDirectories(string path)
    {
        try { return Directory.GetDirectories(path).ToList(); }
        catch { return new List<string>(); }
    }

    #endregion

    private void ConfigTosUtil()
    {
        if (string.IsNullOrEmpty(_tosAccessKey) || string.IsNullOrEmpty(_tosSecretKey))
        {
            Log("❌ 请先填写 AccessKey 和 SecretKey");
            return;
        }

        // tosutil 的 flag: -e=endpoint, -re=region, -i=ak, -k=sk
        // endpoint 里的 region 提取：tos-cn-shanghai → cn-shanghai
        string endpoint = _tosEndpoint;
        string region = _tosEndpoint.Replace("tos-", "");
        string args = $"config -e=http://{endpoint} -re={region} -i={_tosAccessKey} -k={_tosSecretKey}";

        Log($"$ tosutil {args}");

        try
        {
            var psi = new ProcessStartInfo(_tosUtilPath, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            var proc = Process.Start(psi);
            proc.WaitForExit(10000);

            if (proc.ExitCode == 0)
                Log("✓ tosutil 配置成功");
            else
            {
                Log($"tosutil 退出码: {proc.ExitCode}");
                Log(proc.StandardOutput.ReadToEnd());
                Log(proc.StandardError.ReadToEnd());
            }
        }
        catch (Exception e)
        {
            Log($"❌ 启动失败: {e.Message}");
        }
    }

    #region TOS 同步

    private void TosSync()
    {
        _isWorking = true;
        _statusText = "同步 BuiltinCatalog...";
        _logs.Clear();

        // 1. 复制热更 DLL 到 StreamingAssets（YooAsset 用）
        HotUpdateDllCopier.CopyDlls();

        // 2. 同步 BuiltinCatalog 到 StreamingAssets
        SyncBuiltinCatalog();

        // 3. 上传 HotUpdate.dll 到 CDN（和 YooAsset 包同一目录）
        UploadHotUpdateDll();

        _statusText = "同步到 TOS...";

        var sourceDir = new DirectoryInfo(_detectedVersionDir);
        var files = sourceDir.GetFiles("*", SearchOption.AllDirectories)
            .Where(f => !f.Name.EndsWith(".meta")
                     && f.Name != "buildlogtep.json"
                     && f.Name.EndsWith(".report") == false)
            .ToList();

        Log($"源: {sourceDir.FullName}");
        Log($"目标: tos://{_tosBucket}/{_tosPrefix}/{_detectedVersion}/");
        Log($"文件: {files.Count} 个, " +
                  $"共 {FormatSize(files.Sum(f => f.Length))}");
        Log("--- tosutil 输出 ---");

        try
        {
            // 构建 tosutil sync 命令
            string tosPath = $"tos://{_tosBucket}/{_tosPrefix}/";

            var args = $"cp \"{sourceDir.FullName}\" \"{tosPath}\" -r " +
                       $"--exclude \"*.meta\" --exclude \"*.report\" --exclude \"buildlogtep.json\"";

            Log($"$ tosutil {args}");

            var psi = new ProcessStartInfo(_tosUtilPath, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };

            var proc = Process.Start(psi);
            proc.WaitForExit(300000);

            // 同步读取输出（避免 BeginOutputReadLine + WaitForExit 丢消息）
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(stdout))
                foreach (var line in stdout.Split('\n'))
                    if (!string.IsNullOrWhiteSpace(line))
                        Log($"  {line.TrimEnd('\r')}");
            if (!string.IsNullOrEmpty(stderr))
                foreach (var line in stderr.Split('\n'))
                    if (!string.IsNullOrWhiteSpace(line))
                        Log($"  [err] {line.TrimEnd('\r')}");

            if (proc.ExitCode == 0)
            {
                // 上传 version.json（CDN 版本清单，和你的 bootstrap 对应）
                string tmpPath = Path.Combine(Path.GetTempPath(), $"yoo_version_{_detectedVersion}.json");
                File.WriteAllText(tmpPath, $"{{\"version\":\"{_detectedVersion}\"}}");

                string manifestArgs = $"cp \"{tmpPath}\" " +
                    $"\"tos://{_tosBucket}/{_tosPrefix}/version.json\"";

                var proc2 = Process.Start(new ProcessStartInfo(_tosUtilPath, manifestArgs)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                });
                proc2.WaitForExit(30000);
                File.Delete(tmpPath);

                Log(proc2.ExitCode == 0 ? "  ✓ version.json" : "  ✗ version.json 上传失败");
                _statusText = "同步完成 ✓";
            }
            else
            {
                Log($"tosutil 退出码: {proc.ExitCode}");
                _statusText = "同步失败 ✗";
            }
        }
        catch (Exception e)
        {
            Log($"❌ 启动失败: {e.Message}");
            Log($"请确认 tosutil 存在于: {_tosUtilPath}");
            _statusText = "错误";
        }

        _isWorking = false;
        AssetDatabase.Refresh();
    }

    #endregion

    #region 内置清单同步

    private void SyncBuiltinCatalog()
    {
        var dstDir = Path.Combine(Application.dataPath, "StreamingAssets/yoo/DefaultPackage");
        Directory.CreateDirectory(dstDir);

        var srcDir = _detectedVersionDir;
        if (string.IsNullOrEmpty(srcDir) || !Directory.Exists(srcDir))
        {
            Log("⚠ 未检测到 YooAsset 构建产物，跳过 BuiltinCatalog 同步");
            return;
        }

        foreach (var f in Directory.GetFiles(srcDir, "*.bytes"))
        {
            var dst = Path.Combine(dstDir, "BuiltinCatalog.bytes");
            File.Copy(f, dst, true);
            Log($"  BuiltinCatalog.bytes ← {Path.GetFileName(f)}");
        }
        foreach (var f in Directory.GetFiles(srcDir, "*.hash"))
        {
            var dst = Path.Combine(dstDir, "BuiltinCatalog.hash");
            File.Copy(f, dst, true);
        }
        foreach (var f in Directory.GetFiles(srcDir, "*.version"))
        {
            var dst = Path.Combine(dstDir, "DefaultPackage.version");
            File.Copy(f, dst, true);
        }
        foreach (var f in Directory.GetFiles(srcDir, "*.bundle"))
        {
            var dst = Path.Combine(dstDir, Path.GetFileName(f));
            File.Copy(f, dst, true);
        }

        AssetDatabase.Refresh();
        Log("✓ BuiltinCatalog 已同步到 StreamingAssets");
    }

    #endregion

    private void UploadHotUpdateDll()
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var dllPath = Path.Combine(projectRoot, "HybridCLRData/HotUpdateDlls/WebGL/HotUpdate.dll");
        if (!File.Exists(dllPath))
        {
            Log("⚠ HotUpdate.dll 不存在，跳过上传（先编译热更 DLL）");
            return;
        }

        // 拷到 YooAsset 构建目录，让它被 YooAsset 管理
        var dllDst = Path.Combine(_detectedVersionDir, "HotUpdate.dll");
        File.Copy(dllPath, dllDst, true);
        Log($"✓ HotUpdate.dll → YooAsset 构建目录");

        string tosPath = $"tos://{_tosBucket}/{_tosPrefix}/{_detectedVersion}/HotUpdate.dll";
        var args = $"cp \"{dllDst}\" \"{tosPath}\"";

        try
        {
            var psi = new ProcessStartInfo(_tosUtilPath, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            var proc = Process.Start(psi);
            proc.WaitForExit(30000);
            Log(proc.ExitCode == 0 ? "✓ HotUpdate.dll 已上传" : $"✗ HotUpdate.dll 上传失败");
        }
        catch (Exception e)
        {
            Log($"❌ HotUpdate.dll 上传异常: {e.Message}");
        }
    }

    #region 辅助

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
        return $"{bytes / (1024f * 1024f):F1} MB";
    }

    #endregion
}
