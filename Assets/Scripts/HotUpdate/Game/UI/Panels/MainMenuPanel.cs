using System;
using System.Collections.Generic;
using TTSDK;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单面板——游戏入口，包含侧边栏逻辑。
/// </summary>
public class MainMenuPanel : UIPanel
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _sideBarButton;
    [SerializeField] private Button _quitButton;

    private MainMenuViewModel _vm;
    private readonly List<Action> _unbind = new List<Action>();
    private bool _sideBarAvailable;

    protected override void OnOpen(object data)
    {
        _vm = new MainMenuViewModel();

        if (_titleText) _titleText.text = "背包乱斗";
        if (_startButton) _unbind.Add(_startButton.BindClick(OnStart));
        if (_quitButton) _unbind.Add(_quitButton.BindClick(OnQuit));

        // 侧边栏
        CheckSideBar();
        if (_sideBarButton)
            _unbind.Add(_sideBarButton.BindClick(OnSideBar));
    }

    /// <summary>检查侧边栏是否可用</summary>
    private void CheckSideBar()
    {
        TT.CheckScene(TTSideBar.SceneEnum.SideBar,
            onSuccess: (available) =>
            {
                _sideBarAvailable = available;
                if (_sideBarButton) _sideBarButton.gameObject.SetActive(available);
                Debug.Log($"[MainMenu] 侧边栏可用: {available}");
            },
            onComplete: () => { },
            onError: (code, msg) =>
            {
                _sideBarAvailable = false;
                if (_sideBarButton) _sideBarButton.gameObject.SetActive(false);
            });
    }

    /// <summary>打开侧边栏</summary>
    private void OnSideBar()
    {
        if (!_sideBarAvailable) return;

        var param = new TTSDK.UNBridgeLib.LitJson.JsonData();
        param["scene"] = "sidebar";

        TT.NavigateToScene(param,
            onSuccess: () => Debug.Log("[MainMenu] 侧边栏已打开"),
            onComplete: () => { },
            onError: (code, msg) => Debug.LogWarning($"[MainMenu] 侧边栏打开失败: {code} {msg}"));
    }

    private void OnStart()
    {
        UIManager.Instance.Close(this);
        UIManager.Instance.Open("PreparePanel");
    }

    private void OnQuit()
    {
        Application.Quit();
    }

    protected override void OnClose()
    {
        foreach (var u in _unbind) u.Invoke();
        _unbind.Clear();
        _vm?.Dispose();
        _vm = null;
    }
}
