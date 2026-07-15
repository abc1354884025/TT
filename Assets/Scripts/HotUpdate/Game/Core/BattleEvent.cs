using System;

public enum BattleEventType
{
    Attack,
    Damage,
    Heal,
    Shield,
    EffectTriggered
}

/// <summary>战斗逻辑产生的确定性事件，由 UI 负责回放图片、动画、特效与数值。</summary>
[Serializable]
public class BattleEvent
{
    public BattleEventType Type;
    public int Round;
    public string SourceName;
    public string SourceItemInstanceId;
    public string EffectId;
    public string VfxPath;
    public int Value;
    public bool IsCrit;
    public bool TargetIsEnemy;
}
