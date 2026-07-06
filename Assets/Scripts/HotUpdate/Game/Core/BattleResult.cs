using System;
using System.Collections.Generic;

/// <summary>战斗结果</summary>
[Serializable]
public class BattleResult
{
    public enum WinnerType { Player, Enemy, Draw }

    public WinnerType Winner;
    public int PlayerRemainingHP;
    public int EnemyRemainingHP;
    public int TotalRounds;
    public List<BattleLogEntry> Log;

    public bool IsPlayerWin => Winner == WinnerType.Player;
    public bool IsDraw => Winner == WinnerType.Draw;

    public override string ToString()
    {
        return Winner == WinnerType.Player
            ? $"胜利！剩余 HP: {PlayerRemainingHP}，共 {TotalRounds} 回合"
            : Winner == WinnerType.Enemy
                ? $"失败... 剩余 HP: {EnemyRemainingHP}，共 {TotalRounds} 回合"
                : $"平局！共 {TotalRounds} 回合";
    }
}
