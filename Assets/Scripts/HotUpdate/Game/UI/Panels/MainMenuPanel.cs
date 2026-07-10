using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单面板——游戏入口。
/// </summary>
public class MainMenuPanel : UIPanel
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;

    private MainMenuViewModel _vm;
    private readonly List<Action> _unbind = new List<Action>();

    protected override void OnOpen(object data)
    {
        _vm = new MainMenuViewModel();

        if (_titleText) _titleText.text = "背包乱斗";

        if (_startButton)
            _unbind.Add(_startButton.BindClick(OnStart));

        if (_quitButton)
            _unbind.Add(_quitButton.BindClick(OnQuit));
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
