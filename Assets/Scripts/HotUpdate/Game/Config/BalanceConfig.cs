using System;

/// <summary>全局平衡参数（从 balance.json 反序列化）</summary>
[Serializable]
public class BalanceConfig
{
    public int gridWidth = 6;
    public int gridHeight = 8;
    public int maxRounds = 60;
    public float flatDamageReduction = 0.5f;
    public int jitterRange = 2;
    public float critMultiplier = 1.5f;
    public int startingGold = 100;
    public int shopRefreshCost = 2;
    public int shopSlotCount = 5;
}
