using System;
using System.Collections.Generic;
using ArtifactDesigner.Artifact;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Combat
{
    /// <summary>战斗结果。</summary>
    public class BattleResult
    {
        public bool victory;          // 是否击杀敌人
        public int rounds;            // 战斗回合数
        public int playerHpLeft;      // 玩家剩余生命
        public int enemyHpLeft;       // 敌人剩余生命
        public int expGained;         // 获得经验（击杀成功时）
        public List<ArtifactPart> drops = new List<ArtifactPart>(); // 击杀掉落部件
    }

    /// <summary>
    /// 打怪战斗与掉落结算（纯逻辑层，不依赖 UnityEngine）。
    /// 流程：
    ///   1. 模拟回合制战斗（速度高者先手，双方互击，任一生命归零结束；
    ///      超过最大回合仍未分胜负视为玩家防守失败）；
    ///   2. 击杀成功 → 按掉落表逐条独立掷骰，产出部件；
    ///   3. 战斗失败 → 不掉落任何部件。
    /// </summary>
    public static class CombatSystem
    {
        public const int MaxRounds = 60;

        /// <summary>
        /// 模拟一次打怪战斗并结算掉落。
        /// </summary>
        /// <param name="playerMaxHp">玩家生命上限</param>
        /// <param name="playerStats">玩家当前属性（攻击、防御、速度）</param>
        /// <param name="enemy">敌人实例（含掉落表）</param>
        /// <param name="rng">随机源（掉落掷骰用）；传入 null 时使用默认随机</param>
        public static BattleResult SimulateBattle(int playerMaxHp, AttributeSet playerStats, Enemy enemy, System.Random rng)
        {
            var result = new BattleResult();
            if (enemy == null) return result;

            int playerHp = playerMaxHp;
            int enemyHp = enemy.maxHp;
            bool playerFirst = playerStats.speed >= enemy.speed;

            int playerDamage = Math.Max(1, playerStats.attack - 0);      // 玩家攻击直接造成伤害
            int enemyDamage = Math.Max(1, enemy.attack - playerStats.defense); // 敌人攻击受玩家防御减免

            for (int round = 1; round <= MaxRounds; round++)
            {
                // 先手方攻击
                if (playerFirst)
                {
                    enemyHp -= playerDamage;
                    if (enemyHp <= 0)
                    {
                        result.victory = true;
                        result.rounds = round;
                        result.playerHpLeft = playerHp;
                        result.enemyHpLeft = 0;
                        result.expGained = enemy.expReward;
                        result.drops = RollDrops(enemy.dropTable, rng);
                        return result;
                    }
                    playerHp -= enemyDamage;
                    if (playerHp <= 0)
                    {
                        result.victory = false;
                        result.rounds = round;
                        result.playerHpLeft = 0;
                        result.enemyHpLeft = enemyHp;
                        return result;
                    }
                }
                else
                {
                    playerHp -= enemyDamage;
                    if (playerHp <= 0)
                    {
                        result.victory = false;
                        result.rounds = round;
                        result.playerHpLeft = 0;
                        result.enemyHpLeft = enemyHp;
                        return result;
                    }
                    enemyHp -= playerDamage;
                    if (enemyHp <= 0)
                    {
                        result.victory = true;
                        result.rounds = round;
                        result.playerHpLeft = playerHp;
                        result.enemyHpLeft = 0;
                        result.expGained = enemy.expReward;
                        result.drops = RollDrops(enemy.dropTable, rng);
                        return result;
                    }
                }
            }

            // 超过最大回合：视为防守失败，不掉落
            result.victory = false;
            result.rounds = MaxRounds;
            result.playerHpLeft = playerHp;
            result.enemyHpLeft = enemyHp;
            return result;
        }

        /// <summary>
        /// 按掉落表掷骰产出部件。
        /// 每条目独立判定：命中 chance 后产出 minCount~maxCount 件对应类型部件（含稀有度）。
        /// </summary>
        public static List<ArtifactPart> RollDrops(DropTable table, System.Random rng)
        {
            var drops = new List<ArtifactPart>();
            if (table == null || table.entries == null) return drops;

            var random = rng ?? new System.Random();
            foreach (var entry in table.entries)
            {
                if (entry == null || entry.chance <= 0f || entry.maxCount < entry.minCount || entry.maxCount <= 0)
                    continue;
                if (random.NextDouble() > entry.chance) continue;

                int count = entry.minCount + random.Next(entry.maxCount - entry.minCount + 1);
                for (int i = 0; i < count; i++)
                {
                    var part = DemoParts.Create(entry.partType);
                    part.rarity = entry.rarity;
                    drops.Add(part);
                }
            }
            return drops;
        }
    }
}