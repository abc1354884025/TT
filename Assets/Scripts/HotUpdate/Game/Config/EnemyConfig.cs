using System;

/// <summary>敌人配置 JSON 结构</summary>
[Serializable]
public class EnemyConfig
{
    public EnemyEntry[] enemies;
}

[Serializable]
public class EnemyEntry
{
    public string name;
    public int difficulty;
    public int attack;
    public int defense;
    public int hp;
    public int critChance;
    public string iconPath;

    public CombatStats GetStats()
    {
        return new CombatStats
        {
            Attack = attack,
            Defense = defense,
            MaxHP = hp,
            CritChance = critChance
        };
    }
}
