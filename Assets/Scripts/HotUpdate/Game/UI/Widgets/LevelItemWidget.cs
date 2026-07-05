using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡选择列表项。挂在 LevelItemWidget.prefab 上。
/// Panel 只需调 SetContext(type) → SetIndex(i)，其余自己搞定。
/// </summary>
public class LevelItemWidget : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _levelLabel;
    [SerializeField] private Image _lockIcon;
    [SerializeField] private Image _starIcon;
    [SerializeField] private TMP_Text _bestTimeText;

    private PuzzleType _puzzleType;
    private int _index = -1;
    private LevelSelectItemData _data;

    /// <summary>设置上下文（哪个游戏的关卡列表）</summary>
    public void SetContext(PuzzleType puzzleType)
    {
        _puzzleType = puzzleType;
    }

    /// <summary>设置索引，自动拉数据并刷新 UI</summary>
    public void SetIndex(int index)
    {
        _index = index;
        Refresh();
    }

    private void Refresh()
    {
        if (_index < 0) return;

        // 自己从数据库拿数据
        var items = LevelDatabase.GetLevelSelectItems(_puzzleType);
        _data = _index < items.Count ? items[_index] : null;
        if (_data == null) return;

        // 更新 UI
        if (_levelLabel)
            _levelLabel.text = _data.Label;

        if (_button)
            _button.interactable = _data.IsUnlocked;

        if (_lockIcon)
            _lockIcon.gameObject.SetActive(!_data.IsUnlocked);

        if (_starIcon)
            _starIcon.gameObject.SetActive(_data.IsCompleted);

        if (_bestTimeText)
        {
            if (_data.IsCompleted && _data.BestTime > 0)
            {
                int m = (int)(_data.BestTime / 60);
                int s = (int)(_data.BestTime % 60);
                _bestTimeText.text = $"{m:D2}:{s:D2}";
                _bestTimeText.gameObject.SetActive(true);
            }
            else
            {
                _bestTimeText.gameObject.SetActive(false);
            }
        }
    }

    private void Awake()
    {
        if (_button)
            _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (_data == null || !_data.IsUnlocked) return;

        var levelData = LevelDatabase.CreateLevel(_puzzleType, _data.LevelIndex);
        string panelName = GetPuzzlePanelName();

        if (!string.IsNullOrEmpty(panelName) && levelData != null)
            UIManager.Instance.Open(panelName, levelData);
    }

    private string GetPuzzlePanelName()
    {
        switch (_puzzleType)
        {
            case PuzzleType.Sudoku: return "SudokuPanel";
            case PuzzleType.Nurikabe: return "NurikabePanel";
            case PuzzleType.NumberLink: return "NumberLinkPanel";
            case PuzzleType.HashiBridge: return "HashiBridgePanel";
            default: return "";
        }
    }
}
