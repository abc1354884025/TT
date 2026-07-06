using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 奖励面板——战后选战利品。
/// </summary>
public class RewardPanel : UIPanel
{
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private Transform _rewardContainer;
    [SerializeField] private Button _continueButton;

    private RewardViewModel _vm;
    private readonly List<Action> _unbind = new List<Action>();
    private PrepareViewModel _prepareVM;

    protected override void OnOpen(object data)
    {
        var battleVM = data as BattleViewModel;
        if (battleVM == null) return;

        _vm = new RewardViewModel();
        _vm.GenerateRewards(new BattleResult { Winner = battleVM.PlayerHP.Value > 0 ? BattleResult.WinnerType.Player : BattleResult.WinnerType.Enemy }, 1);

        _unbind.Add(_resultText.BindTo(_vm.ResultSummary));
        _unbind.Add(_goldText.BindTo(_vm.GoldEarned));

        // 简单展示奖励（Phase 4 先做文字列表）
        if (_rewardContainer)
        {
            foreach (var item in _vm.RewardItems)
            {
                var txt = new GameObject("Item", typeof(Text)).GetComponent<Text>();
                txt.text = $"{item.Name} ({item.Rarity})";
                txt.transform.SetParent(_rewardContainer, false);
            }
        }

        if (_continueButton)
            _unbind.Add(_continueButton.BindClick(OnContinue));
    }

    private void OnContinue()
    {
        UIManager.Instance.Close(this);
        UIManager.Instance.Open("PreparePanel");
    }

    protected override void OnClose()
    {
        foreach (var u in _unbind) u.Invoke();
        _unbind.Clear();
        _vm?.Dispose();
        _vm = null;
    }
}
