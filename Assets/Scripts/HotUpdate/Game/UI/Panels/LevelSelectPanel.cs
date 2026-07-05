using System;
using System.Collections.Generic;
using KingSoft.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡选择面板。四种益智游戏共用。
/// 使用 LoopScrollView，cell 自己管理数据和点击。
/// </summary>
public class LevelSelectPanel : UIPanel
{
    [Header("标题")]
    [SerializeField] private TMP_Text _titleText;

    [Header("关卡列表")]
    [SerializeField] private LoopScrollView _levelScrollView;
    [Tooltip("cell prefab 的 Resources 路径")]
    [SerializeField] private string _cellResourcePath = "UI/Widgets/LevelItemWidget";

    [Header("按钮")]
    [SerializeField] private Button _backButton;

    private LevelSelectViewModel _vm;
    private readonly List<Action> _unbind = new List<Action>();

    protected override void OnOpen(object data)
    {
        PuzzleType type = data is PuzzleType pt ? pt : PuzzleType.Sudoku;

        _vm = new LevelSelectViewModel();
        _vm.Init(type);

        if (_titleText)
            _unbind.Add(_titleText.BindTo(_vm.PuzzleTitle));

        if (_levelScrollView)
        {
            var cellPrefab = Resources.Load<GameObject>(_cellResourcePath);
            if (cellPrefab == null)
            {
                Debug.LogError($"[LevelSelectPanel] 加载 cell prefab 失败: Resources/{_cellResourcePath}");
                return;
            }
            // OnCellInit 只执行一次：设置 context
            _levelScrollView.OnCellInit.AddListener(go =>
            {
                var widget = go.GetComponent<LevelItemWidget>();
                if (!widget) widget = go.AddComponent<LevelItemWidget>();
                widget.SetContext(type);
            });
            _levelScrollView.OnCellUpdate.AddListener(OnCellUpdate);
            _levelScrollView.Initialize(cellPrefab, _vm.Levels.Count);
        }

        if (_backButton) _unbind.Add(_backButton.BindClick(OnBackClicked));
    }

    protected override void OnShow()
    {
        // 从游戏面板返回时刷新（通关状态可能变了）
        _vm?.RefreshLevels();
        _levelScrollView?.ReloadData(_vm?.Levels.Count ?? 0);
    }

    protected override void OnClose()
    {
        if (_levelScrollView)
        {
            _levelScrollView.OnCellInit.RemoveAllListeners();
            _levelScrollView.OnCellUpdate.RemoveAllListeners();
        }

        foreach (var u in _unbind) u.Invoke();
        _unbind.Clear();
        _vm?.Dispose();
        _vm = null;
    }

    /// <summary>Panel 只传 index，Widget 自己搞定剩下的</summary>
    private void OnCellUpdate(int index, GameObject go)
    {
        var widget = go.GetComponent<LevelItemWidget>();
        widget?.SetIndex(index);
    }

    private void OnBackClicked() => UIManager.Instance.Close(this);
}
