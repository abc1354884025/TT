using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗面板——展示自动战斗回放。
/// </summary>
public class BattlePanel : UIPanel
{
    [Header("玩家")]
    [SerializeField] private Text _playerNameText;
    [SerializeField] private Slider _playerHPBar;
    [SerializeField] private Text _playerHPText;

    [Header("敌人")]
    [SerializeField] private Text _enemyNameText;
    [SerializeField] private Slider _enemyHPBar;
    [SerializeField] private Text _enemyHPText;

    [Header("结果")]
    [SerializeField] private Text _resultText;
    [SerializeField] private Button _continueButton;

    [Header("日志")]
    [SerializeField] private ScrollRect _logScroll;
    [SerializeField] private Transform _logContainer;

    private BattleViewModel _vm;
    private readonly List<Action> _unbind = new List<Action>();

    protected override void OnOpen(object data)
    {
        var prepareVM = data as PrepareViewModel;
        if (prepareVM == null) { Debug.LogError("[BattlePanel] 没有 PrepareViewModel"); return; }

        // 获取敌人
        var playerStats = prepareVM.BagGrid.GetTotalStats();
        var enemy = ConfigLoader.GetEnemyByDifficulty(prepareVM.Round.Value);
        var enemyStats = enemy?.GetStats() ?? new CombatStats { Attack = 5, Defense = 2, MaxHP = 20 };

        // 模拟战斗
        BattleResolver.SetSeed(UnityEngine.Random.Range(0, int.MaxValue));
        var result = BattleResolver.Simulate("玩家", playerStats, enemy?.name ?? "敌人", enemyStats);

        // 初始化 VM
        _vm = new BattleViewModel();
        _vm.SetResult(result, "玩家", enemy?.name ?? "敌人", playerStats.MaxHP, enemyStats.MaxHP);

        // 绑定
        _unbind.Add(_playerNameText.BindTo(_vm.PlayerName));
        _unbind.Add(_enemyNameText.BindTo(_vm.EnemyName));
        _unbind.Add(_resultText.BindTo(_vm.ResultText));

        _vm.PlayerHP.OnChanged += v => UpdateHPBar(_playerHPBar, _playerHPText, v, _vm.PlayerMaxHP.Value);
        _vm.EnemyHP.OnChanged += v => UpdateHPBar(_enemyHPBar, _enemyHPText, v, _vm.EnemyMaxHP.Value);
        _vm.PlayerHP.Refresh();
        _vm.EnemyHP.Refresh();

        if (_continueButton)
        {
            _continueButton.gameObject.SetActive(false);
            _unbind.Add(_continueButton.BindClick(OnContinue));
        }

        // 开始回放
        StartCoroutine(_vm.ReplayLog(this, 0.3f, entry =>
        {
            var go = new GameObject("LogEntry", typeof(BattleLogEntryWidget), typeof(LayoutElement));
            go.transform.SetParent(_logContainer, false);
            var widget = go.GetComponent<BattleLogEntryWidget>();
            widget.SetEntry(entry.ToString(), entry.AttackerName == "玩家", entry.IsCrit);
        }));

        StartCoroutine(ShowContinueWhenDone());
    }

    private IEnumerator ShowContinueWhenDone()
    {
        yield return new WaitUntil(() => _vm.IsDone.Value);
        if (_continueButton) _continueButton.gameObject.SetActive(true);
    }

    private void UpdateHPBar(Slider slider, Text text, int current, int max)
    {
        if (slider) slider.value = max > 0 ? (float)current / max : 0;
        if (text) text.text = $"{current}/{max}";
    }

    private void OnContinue()
    {
        UIManager.Instance.Close(this);
        UIManager.Instance.Open("RewardPanel", _vm);
    }

    protected override void OnClose()
    {
        foreach (var u in _unbind) u.Invoke();
        _unbind.Clear();
        _vm?.Dispose();
        _vm = null;
    }
}
