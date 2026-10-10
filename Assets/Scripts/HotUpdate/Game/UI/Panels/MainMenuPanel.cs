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

        if (_titleText) _titleText.text = "新游戏";
        if (_startButton) _startButton.interactable = false;
        if (_quitButton) _unbind.Add(_quitButton.BindClick(OnQuit));
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
