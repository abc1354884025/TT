using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 奖励 ViewModel。
/// </summary>
public class RewardViewModel : ObservableObject
{
    public BindableProperty<string> ResultSummary = new BindableProperty<string>();
    public List<ItemData> RewardItems = new List<ItemData>();
    public BindableProperty<int> GoldEarned = new BindableProperty<int>(0);

    public void GenerateRewards(BattleResult result, int currentRound)
    {
        ResultSummary.Value = result.IsPlayerWin ? "胜利！选择你的战利品" : "失败... 再试一次";

        if (result.IsPlayerWin)
        {
            var rng = new System.Random();
            GoldEarned.Value = 20 + currentRound * 5;

            // 随机 3 个奖励
            for (int i = 0; i < 3; i++)
            {
                var item = ItemDatabase.GetRandom(rng);
                if (item != null) RewardItems.Add(item);
            }
        }
    }

    public override void Dispose()
    {
        ResultSummary.Clear(); GoldEarned.Clear();
        RewardItems.Clear();
        base.Dispose();
    }
}
