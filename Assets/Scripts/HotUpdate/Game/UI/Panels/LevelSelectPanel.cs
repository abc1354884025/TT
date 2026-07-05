using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 关卡选择面板。四种益智游戏共用，根据传入的 PuzzleType 显示不同关卡列表。
    /// </summary>
    public class LevelSelectPanel : UIPanel
    {
        [Header("标题")]
        [SerializeField] private TMP_Text _titleText;

        [Header("关卡列表")]
        [SerializeField] private UIList _levelList;

        [Header("按钮")]
        [SerializeField] private Button _backButton;

        private LevelSelectViewModel _vm;
        private readonly List<Action> _unbind = new List<Action>();

        protected override void OnOpen(object data)
        {
            PuzzleType type;
            if (data is PuzzleType pt)
                type = pt;
            else
                type = PuzzleType.Sudoku; // fallback

            _vm = new LevelSelectViewModel();
            _vm.Init(type);

            // 标题绑定
            if (_titleText)
                _unbind.Add(_titleText.BindTo(_vm.PuzzleTitle));

            // 关卡列表
            if (_levelList)
                _levelList.SetData(_vm.Levels, BindLevelItem);

            // 返回按钮
            if (_backButton) _unbind.Add(_backButton.BindClick(OnBackClicked));
        }

        protected override void OnClose()
        {
            foreach (var u in _unbind) u.Invoke();
            _unbind.Clear();
            _vm?.Dispose();
            _vm = null;
        }

        private void BindLevelItem(GameObject go, object data, int index)
        {
            var itemData = data as LevelSelectItemData;
            if (itemData == null) return;

            var widget = go.GetComponent<LevelItemWidget>();
            if (widget == null)
                widget = go.AddComponent<LevelItemWidget>();

            widget.Bind(itemData);
            widget.OnClicked += OnLevelSelected;
        }

        private void OnLevelSelected(LevelSelectItemData data)
        {
            if (!data.IsUnlocked) return;

            var levelData = _vm.GetLevelData(data.LevelIndex);
            string panelName = _vm.GetPuzzlePanelName();

            if (!string.IsNullOrEmpty(panelName) && levelData != null)
            {
                UIManager.Instance.Open(panelName, levelData);
            }
        }

        private void OnBackClicked()
        {
            UIManager.Instance.Close(this);
        }
    }
