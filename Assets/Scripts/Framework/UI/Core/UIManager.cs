using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 管理器——UI 框架核心。支持 HybridCLR 热更：
///   1. 先调用 SetHotUpdateAssembly 注入热更 DLL 的 Assembly
///   2. Open(panelId) 自动从热更 Assembly 反射创建 Panel 实例
///   3. 框架层（AOT）和业务层（热更）通过 UIPanel 基类解耦
///
/// 用法：
///   UIManager.Instance.SetHotUpdateAssembly(hotUpdateAss);
///   UIManager.Instance.Open("ShopPanel", shopData);
/// </summary>
public class UIManager : MonoSingleton<UIManager>
{
    #region 字段

    [SerializeField] private IResourceProvider _resourceProvider;
    private readonly PanelCache _panelCache = new PanelCache();
    private readonly Dictionary<UILayer, RectTransform> _layerRoots = new Dictionary<UILayer, RectTransform>();
    private readonly Dictionary<UILayer, Stack<UIPanel>> _layerStacks = new Dictionary<UILayer, Stack<UIPanel>>();
    private readonly Dictionary<string, string> _panelPaths = new Dictionary<string, string>();
    private readonly Dictionary<string, UIPanel> _openPanels = new Dictionary<string, UIPanel>();

    /// <summary>热更程序集（HybridCLR 加载的 DLL）</summary>
    private Assembly _hotUpdateAssembly;

    /// <summary>面板类型缓存：PanelId → Type，避免每次都反射查找</summary>
    private readonly Dictionary<string, Type> _panelTypeCache = new Dictionary<string, Type>();

    #endregion

    #region 初始化

    protected override void Awake()
    {
        base.Awake();
        if (_resourceProvider == null)
            _resourceProvider = new ResourcesProvider(this);

        EnsureLayerRoots();
    }

    private void EnsureLayerRoots()
    {
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            _layerStacks[layer] = new Stack<UIPanel>();
            if (!_layerRoots.ContainsKey(layer))
            {
                var existing = GameObject.Find($"Canvas_{layer}");
                if (existing != null)
                    _layerRoots[layer] = existing.GetComponent<RectTransform>();
                else
                {
                    var go = new GameObject($"Canvas_{layer}", typeof(Canvas), typeof(CanvasScaler));
                    go.transform.SetParent(transform);
                    var canvas = go.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = (int)layer;
                    _layerRoots[layer] = go.GetComponent<RectTransform>();
                }
            }
        }
    }

    #endregion

    #region 热更 Assembly 注入

    /// <summary>
    /// 注入热更程序集。HybridCLR 加载 DLL 后调用此方法。
    /// </summary>
    /// <param name="assembly">RuntimeApi.LoadAssembly 或 Assembly.Load 得到的程序集</param>
    public void SetHotUpdateAssembly(Assembly assembly)
    {
        _hotUpdateAssembly = assembly;
        _panelTypeCache.Clear();
        Debug.Log($"[UIManager] 热更 Assembly 已注入: {assembly.GetName().Name}");
    }

    /// <summary>热更 Assembly 是否已注入</summary>
    public bool IsHotUpdateReady => _hotUpdateAssembly != null;

    /// <summary>切换资源加载器（如从 Resources 切换到 AssetBundle）</summary>
    public void SetResourceProvider(IResourceProvider provider)
    {
        _resourceProvider = provider;
    }

    #endregion

    #region 面板注册

    /// <summary>注册面板资源路径。不注册默认用 "UI/Panels/{panelId}"</summary>
    public void RegisterPanel(string panelId, string resourcePath)
    {
        _panelPaths[panelId] = resourcePath;
    }

    private string GetPanelPath(string panelId)
    {
        return _panelPaths.TryGetValue(panelId, out var p) ? p : $"UI/Panels/{panelId}";
    }

    #endregion

    #region 类型查找（AOT + 热更）

    /// <summary>
    /// 根据 panelId 查找 UIPanel 的子类型。
    /// 优先从热更 Assembly 找，找不到再从 AOT Assembly-CSharp 找。
    /// </summary>
    private Type FindPanelType(string panelId)
    {
        // 缓存命中
        if (_panelTypeCache.TryGetValue(panelId, out var cached))
            return cached;

        Type found = null;

        // 1. 从热更 Assembly 查找（优先）
        if (_hotUpdateAssembly != null)
        {
            found = FindTypeInAssembly(_hotUpdateAssembly, panelId);
        }

        // 2. 从 Assembly-CSharp（AOT 框架层）查找
        if (found == null)
        {
            var aotAssembly = typeof(UIManager).Assembly;
            found = FindTypeInAssembly(aotAssembly, panelId);
        }

        // 3. 遍历所有已加载的 Assembly（兜底）
        if (found == null)
        {
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                found = FindTypeInAssembly(ass, panelId);
                if (found != null) break;
            }
        }

        if (found != null)
            _panelTypeCache[panelId] = found;
        else
            Debug.LogError($"[UIManager] 找不到面板类型: {panelId}（热更 Assembly: {_hotUpdateAssembly?.GetName().Name}）");

        return found;
    }

    private Type FindTypeInAssembly(Assembly assembly, string panelId)
    {
        // 按类名精确匹配
        var type = assembly.GetType(panelId);
        if (type != null && typeof(UIPanel).IsAssignableFrom(type))
            return type;

        // 按类名 + 命名空间模糊匹配
        foreach (var t in assembly.GetTypes())
        {
            if (t.Name == panelId && typeof(UIPanel).IsAssignableFrom(t))
                return t;
        }

        return null;
    }

    #endregion

    #region Open / Close

    /// <summary>
    /// 打开面板（泛型版，用于 AOT 层已知类型的面板）
    /// </summary>
    public void Open<T>(string panelId, object userData = null, Action<UIPanel> onReady = null) where T : UIPanel
    {
        StartCoroutine(OpenRoutine<T>(panelId, userData, onReady));
    }

    /// <summary>
    /// 打开面板。自动从热更 Assembly 查找类型并反射创建。
    /// 推荐用法：UIManager.Instance.Open("ShopPanel", data)
    /// </summary>
    public void Open(string panelId, object userData = null, Action<UIPanel> onReady = null)
    {
        var panelType = FindPanelType(panelId);
        if (panelType == null)
        {
            Debug.LogError($"[UIManager] 打开失败，找不到面板类型: {panelId}。请确认热更 DLL 已加载且包含此类。");
            onReady?.Invoke(null);
            return;
        }
        StartCoroutine(OpenRoutine(panelType, panelId, userData, onReady));
    }

    /// <summary>泛型版本：已知道类型</summary>
    private IEnumerator OpenRoutine<T>(string panelId, object userData, Action<UIPanel> onReady) where T : UIPanel
    {
        return OpenRoutine(typeof(T), panelId, userData, onReady);
    }

    /// <summary>核心加载流程</summary>
    private IEnumerator OpenRoutine(Type panelType, string panelId, object userData, Action<UIPanel> onReady)
    {
        // 1. 已打开 → 拉到栈顶
        if (_openPanels.TryGetValue(panelId, out var existing))
        {
            BringToTop(existing);
            existing.OnRefresh();
            onReady?.Invoke(existing);
            yield break;
        }

        // 2. 尝试从缓存获取
        UIPanel panel = _panelCache.TryGet(panelId);

        // 3. 缓存未命中 → 异步加载 Prefab
        if (panel == null)
        {
            var path = GetPanelPath(panelId);
            bool loaded = false;
            GameObject loadedGo = null;

            _resourceProvider.InstantiateAsync(path, _layerRoots[UILayer.Normal], go =>
            {
                loadedGo = go; loaded = true;
            });

            yield return new WaitUntil(() => loaded);

            if (loadedGo == null)
            {
                Debug.LogError($"[UIManager] 加载 Prefab 失败: {panelId}（路径: {path}）");
                onReady?.Invoke(null);
                yield break;
            }

            panel = loadedGo.GetComponent(panelType) as UIPanel;
            if (panel == null)
                panel = loadedGo.AddComponent(panelType) as UIPanel;

            if (panel == null)
            {
                Debug.LogError($"[UIManager] 无法挂载 {panelType.Name} 到 {panelId}");
                onReady?.Invoke(null);
                yield break;
            }

            ReparentToLayer(panel);
        }

        // 4. 首次初始化
        if (!panel.IsInitialized)
        {
            yield return panel.InitCoroutine();
        }

        // 5. OnOpen
        panel.OpenInternal(userData);

        // 6. 隐藏旧栈顶
        var layer = panel.Layer;
        if (_layerStacks[layer].Count > 0)
            _layerStacks[layer].Peek().HideInternal();

        // 7. 入栈 + 显示
        _layerStacks[layer].Push(panel);
        _openPanels[panel.PanelId] = panel;
        panel.ShowInternal();

        onReady?.Invoke(panel);
    }

    /// <summary>关闭面板</summary>
    public void Close(string panelId)
    {
        if (_openPanels.TryGetValue(panelId, out var panel))
            ClosePanel(panel);
    }

    public void Close(UIPanel panel)
    {
        if (panel) ClosePanel(panel);
    }

    private void ClosePanel(UIPanel panel)
    {
        var layer = panel.Layer;
        var stack = _layerStacks[layer];

        if (stack.Count == 0 || stack.Peek() != panel)
        {
            Debug.LogWarning($"[UIManager] 关闭非栈顶面板: {panel.PanelId}");
            RemoveFromStack(stack, panel);
        }
        else
        {
            stack.Pop();
        }

        panel.HideInternal();
        panel.CloseInternal();
        _openPanels.Remove(panel.PanelId);

        if (panel.CacheOnClose)
            _panelCache.Return(panel);
        else
            Destroy(panel.gameObject);

        if (stack.Count > 0)
            stack.Peek().ShowInternal();
    }

    public void CloseAll(UILayer layer)
    {
        var stack = _layerStacks[layer];
        while (stack.Count > 0)
        {
            var p = stack.Pop(); p.HideInternal(); p.CloseInternal();
            _openPanels.Remove(p.PanelId);
            if (p.CacheOnClose) _panelCache.Return(p); else Destroy(p.gameObject);
        }
    }

    public void CloseAll()
    {
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            CloseAll(layer);
    }

    #endregion

    #region 查询

    public UIPanel GetPanel(string panelId)
    {
        _openPanels.TryGetValue(panelId, out var p); return p;
    }

    public T GetPanel<T>(string panelId) where T : UIPanel
    {
        return GetPanel(panelId) as T;
    }

    public UIPanel GetTopPanel(UILayer layer)
    {
        var s = _layerStacks[layer]; return s.Count > 0 ? s.Peek() : null;
    }

    public bool IsPanelOpen(string panelId) => _openPanels.ContainsKey(panelId);

    public RectTransform GetLayerRoot(UILayer layer)
    {
        _layerRoots.TryGetValue(layer, out var r); return r;
    }

    #endregion

    #region 辅助

    private void BringToTop(UIPanel panel)
    {
        var stack = _layerStacks[panel.Layer];
        if (stack.Count > 0 && stack.Peek() == panel) return;

        RemoveFromStack(stack, panel);

        if (stack.Count > 0) stack.Peek().HideInternal();

        stack.Push(panel);
        panel.ShowInternal();
    }

    private void RemoveFromStack(Stack<UIPanel> stack, UIPanel panel)
    {
        var temp = new Stack<UIPanel>();
        while (stack.Count > 0) { var p = stack.Pop(); if (p == panel) break; temp.Push(p); }
        while (temp.Count > 0) stack.Push(temp.Pop());
    }

    private void ReparentToLayer(UIPanel panel)
    {
        if (_layerRoots.TryGetValue(panel.Layer, out var root))
            panel.transform.SetParent(root, false);
    }

    #endregion
}
