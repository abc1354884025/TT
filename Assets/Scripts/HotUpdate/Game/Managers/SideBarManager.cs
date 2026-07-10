using System;
using TTSDK;
using UnityEngine;

/// <summary>
/// 抖音小游戏侧边栏管理器。
/// 启动时检查侧边栏可用性，提供打开/关闭侧边栏的入口。
/// </summary>
public class SideBarManager : MonoBehaviour
{
    public static SideBarManager Instance { get; private set; }

    /// <summary>侧边栏是否可用</summary>
    public bool IsSideBarAvailable { get; private set; }

    /// <summary>侧边栏状态变化事件</summary>
    public event Action<bool> OnSideBarStateChanged;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>初始化并检查侧边栏</summary>
    public void Init()
    {
        TT.CheckScene(TTSideBar.SceneEnum.SideBar,
            onSuccess: (available) =>
            {
                IsSideBarAvailable = available;
                Debug.Log($"[SideBar] 侧边栏可用: {available}");
                OnSideBarStateChanged?.Invoke(available);
            },
            onComplete: () =>
            {
                Debug.Log("[SideBar] CheckScene 完成");
            },
            onError: (code, msg) =>
            {
                IsSideBarAvailable = false;
                Debug.LogWarning($"[SideBar] CheckScene 失败: {code} {msg}");
                OnSideBarStateChanged?.Invoke(false);
            });
    }

    /// <summary>打开侧边栏</summary>
    public void Show()
    {
        if (!IsSideBarAvailable)
        {
            Debug.LogWarning("[SideBar] 侧边栏不可用，无法打开");
            return;
        }

        var param = new TTSDK.UNBridgeLib.LitJson.JsonData();
        param["scene"] = "sidebar";

        TT.NavigateToScene(param,
            onSuccess: () =>
            {
                Debug.Log("[SideBar] 已打开侧边栏");
            },
            onComplete: () => { },
            onError: (code, msg) =>
            {
                Debug.LogWarning($"[SideBar] 打开失败: {code} {msg}");
            });
    }

    /// <summary>打开侧边栏（带活动ID，用于推广活动）</summary>
    public void ShowWithActivity(string activityId)
    {
        if (!IsSideBarAvailable) return;

        var param = new TTSDK.UNBridgeLib.LitJson.JsonData();
        param["scene"] = "sidebar";
        param["activityId"] = activityId;

        TT.NavigateToScene(param,
            onSuccess: () => Debug.Log("[SideBar] 已打开侧边栏(活动)"),
            onComplete: () => { },
            onError: (code, msg) => Debug.LogWarning($"[SideBar] 打开失败: {code} {msg}"));
    }
}
