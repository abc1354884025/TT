using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗模拟引擎。回合制自动战斗，瞬间完成。
/// </summary>
public static class BattleResolver
{
    /// <summary>默认随机数（可控种子）</summary>
    private static System.Random _rng = new System.Random();

    /// <summary>设置随机种子（用于战斗回放）</summary>
    public static void SetSeed(int seed)
    {
        _rng = new System.Random(seed);
    }

    /// <summary>
    /// 模拟一场自动战斗
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <param name="playerStats">玩家聚合属性</param>
    /// <param name="enemyName">敌人名称</param>
    /// <param name="enemyStats">敌人属性</param>
    /// <param name="maxRounds">最大回合数</param>
    public static BattleResult Simulate(
        string playerName,
        CombatStats playerStats,
        string enemyName,
        CombatStats enemyStats,
        int maxRounds = 60)
    {
        int playerHP = playerStats.MaxHP;
        int enemyHP = enemyStats.MaxHP;
        var log = new List<BattleLogEntry>();

        for (int round = 1; round <= maxRounds; round++)
        {
            // 玩家攻击
            bool pCrit = _rng.Next(0, 100) < Mathf.Min(playerStats.CritChance, 80);
            int pDmg = CalcDamage(playerStats.Attack, enemyStats.Defense, pCrit);
            enemyHP = Mathf.Max(0, enemyHP - pDmg);
            log.Add(new BattleLogEntry
            {
                Round = round,
                AttackerName = playerName,
                Damage = pDmg,
                IsCrit = pCrit,
                TargetRemainingHP = enemyHP
            });

            if (enemyHP <= 0) break;

            // 敌人攻击
            bool eCrit = _rng.Next(0, 100) < Mathf.Min(enemyStats.CritChance, 80);
            int eDmg = CalcDamage(enemyStats.Attack, playerStats.Defense, eCrit);
            playerHP = Mathf.Max(0, playerHP - eDmg);
            log.Add(new BattleLogEntry
            {
                Round = round,
                AttackerName = enemyName,
                Damage = eDmg,
                IsCrit = eCrit,
                TargetRemainingHP = playerHP
            });

            if (playerHP <= 0) break;
        }

        return new BattleResult
        {
            Winner = playerHP > 0
                ? (enemyHP > 0 ? BattleResult.WinnerType.Draw : BattleResult.WinnerType.Player)
                : BattleResult.WinnerType.Enemy,
            PlayerRemainingHP = playerHP,
            EnemyRemainingHP = enemyHP,
            TotalRounds = log.Count,
            Log = log
        };
    }

    /// <summary>伤害计算公式</summary>
    private static int CalcDamage(int attack, int defense, bool isCrit)
    {
        int jitter = _rng.Next(-2, 3); // -2 ~ +2
        int baseDmg = attack - Mathf.FloorToInt(defense * 0.5f);
        int damage = Mathf.Max(1, baseDmg + jitter);
        if (isCrit) damage = Mathf.FloorToInt(damage * 1.5f);
        return damage;
    }
}
