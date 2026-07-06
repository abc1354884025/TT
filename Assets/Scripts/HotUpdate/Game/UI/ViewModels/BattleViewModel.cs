using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗回放 ViewModel。
/// </summary>
public class BattleViewModel : ObservableObject
{
    public BindableProperty<int> PlayerHP = new BindableProperty<int>();
    public BindableProperty<int> EnemyHP = new BindableProperty<int>();
    public BindableProperty<int> PlayerMaxHP = new BindableProperty<int>();
    public BindableProperty<int> EnemyMaxHP = new BindableProperty<int>();
    public BindableProperty<string> PlayerName = new BindableProperty<string>("玩家");
    public BindableProperty<string> EnemyName = new BindableProperty<string>("敌人");
    public BindableProperty<string> ResultText = new BindableProperty<string>("");
    public BindableProperty<bool> IsDone = new BindableProperty<bool>(false);

    public List<BattleLogEntry> LogEntries = new List<BattleLogEntry>();

    private BattleResult _result;

    public void SetResult(BattleResult result, string playerName, string enemyName, int playerMaxHP, int enemyMaxHP)
    {
        _result = result;
        PlayerName.Value = playerName;
        EnemyName.Value = enemyName;
        PlayerMaxHP.Value = playerMaxHP;
        EnemyMaxHP.Value = enemyMaxHP;
        PlayerHP.Value = playerMaxHP;
        EnemyHP.Value = enemyMaxHP;
        ResultText.Value = result.ToString();
        LogEntries = result.Log ?? new List<BattleLogEntry>();
    }

    /// <summary>逐行回放战斗日志的协程</summary>
    public IEnumerator ReplayLog(MonoBehaviour runner, float interval = 0.3f, System.Action<BattleLogEntry> onEntry = null)
    {
        IsDone.Value = false;
        int playerHP = PlayerMaxHP.Value;
        int enemyHP = EnemyMaxHP.Value;

        foreach (var entry in LogEntries)
        {
            yield return new WaitForSeconds(interval);

            if (entry.AttackerName == PlayerName.Value)
                enemyHP = entry.TargetRemainingHP;
            else
                playerHP = entry.TargetRemainingHP;

            PlayerHP.Value = playerHP;
            EnemyHP.Value = enemyHP;
            onEntry?.Invoke(entry);
        }

        IsDone.Value = true;
    }

    public override void Dispose()
    {
        PlayerHP.Clear(); EnemyHP.Clear(); PlayerMaxHP.Clear(); EnemyMaxHP.Clear();
        PlayerName.Clear(); EnemyName.Clear(); ResultText.Clear(); IsDone.Clear();
        LogEntries.Clear();
        base.Dispose();
    }
}
