using System;

/// <summary>战斗单条日志</summary>
[Serializable]
public class BattleLogEntry
{
    public int Round;
    public string AttackerName;   // "Player" or enemy name
    public int Damage;
    public bool IsCrit;
    public int TargetRemainingHP;
    public string SourceItemInstanceId;
    public string EffectId;
    public string VfxPath;

    public override string ToString()
    {
        string crit = IsCrit ? "【暴击！】" : "";
        string effect = string.IsNullOrEmpty(EffectId) ? "" : $"【{EffectId}】";
        return $"Round {Round}: {AttackerName}{effect} 造成 {Damage} 伤害{crit}，剩余 HP: {TargetRemainingHP}";
    }
}
