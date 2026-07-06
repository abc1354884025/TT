using System;

/// <summary>
/// 战斗属性结构体。值类型，堆栈分配，聚合零 GC。
/// </summary>
[Serializable]
public struct CombatStats
{
    public int Attack;
    public int Defense;
    public int MaxHP;
    public int CritChance;     // 0-100
    public float AttackSpeed;  // 攻速倍率

    public static CombatStats Zero => new CombatStats();

    public static CombatStats operator +(CombatStats a, CombatStats b)
    {
        return new CombatStats
        {
            Attack = a.Attack + b.Attack,
            Defense = a.Defense + b.Defense,
            MaxHP = a.MaxHP + b.MaxHP,
            CritChance = a.CritChance + b.CritChance,
            AttackSpeed = a.AttackSpeed + b.AttackSpeed
        };
    }

    /// <summary>总战力评估（粗略）</summary>
    public int PowerRating => Attack * 2 + Defense + MaxHP + CritChance;
}
