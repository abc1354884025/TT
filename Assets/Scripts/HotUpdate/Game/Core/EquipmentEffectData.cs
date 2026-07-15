using System;
using System.Collections.Generic;

/// <summary>装备效果的结算触发点。逻辑层只依赖此枚举，不依赖动画资源。</summary>
public enum EquipmentEffectTrigger
{
    OnBattleStart,
    OnAttackHit,
    OnDamageTaken,
    OnRoundStart,
    OnRoundEnd
}

public enum EquipmentEffectOperation
{
    Damage,
    Heal,
    AddShield
}

/// <summary>可由配置表定义、可被多个装备复用的技能效果。</summary>
[Serializable]
public class EquipmentEffectData
{
    public string Id;
    public EquipmentEffectTrigger Trigger;
    public EquipmentEffectOperation Operation;
    public int Value;
    public int Chance = 100;
    public int CooldownRounds;
    public string VfxPath;
    public string SfxPath;
}

/// <summary>
/// MVP 内置技能表。后续接入 effects.json 时只需替换此类的数据来源，结算器无需修改。
/// </summary>
public static class EquipmentEffectDatabase
{
    private static readonly Dictionary<string, EquipmentEffectData> Effects =
        new Dictionary<string, EquipmentEffectData>
        {
            {
                "fire_sword_on_hit",
                new EquipmentEffectData
                {
                    Id = "fire_sword_on_hit",
                    Trigger = EquipmentEffectTrigger.OnAttackHit,
                    Operation = EquipmentEffectOperation.Damage,
                    Value = 6,
                    Chance = 35,
                    VfxPath = "Effects/fire_hit"
                }
            }
        };

    public static EquipmentEffectData GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        Effects.TryGetValue(id, out var effect);
        return effect;
    }
}
