using System;

namespace ArtifactDesigner.Core
{
    /// <summary>
    /// 战斗公式（纯逻辑层，不依赖 UnityEngine）。
    /// 依据 design_v01_integrated_attribute_task_bubble.md 第 1.3/1.4 节：
    /// - 攻击（基准） = 体 × 2；防御 = 体 × 2
    /// - 玩家血量 = 100 + 体 × 10；小怪血量 = 10 + 体 × 10
    /// - 玩家攻速 2.0 次/秒、移速 3.5；小怪攻速 1.5 次/秒、移速 2.5
    /// - 伤害 = 实际攻击 × 随机 [0.9, 1.1] × 100 / (100 + 目标防御)
    /// </summary>
    public static class FormulaHelper
    {
        public const float AttackVarianceMin = 0.9f;
        public const float AttackVarianceMax = 1.1f;

        /// <summary>玩家/小怪共用的攻击基准。</summary>
        public static int BaseAttack(int body) => body * 2;

        /// <summary>玩家/小怪共用的防御。</summary>
        public static int BaseDefense(int body) => body * 2;

        /// <summary>玩家血量上限。</summary>
        public static int PlayerMaxHp(int body) => 100 + body * 10;

        /// <summary>玩家蓝量上限 = 50 + 气 × 5。</summary>
        public static int PlayerMaxMana(int qi) => 50 + qi * 5;

        /// <summary>小怪血量上限。</summary>
        public static int EnemyMaxHp(int body) => 10 + body * 10;

        public const float PlayerAttackSpeed = 2.0f; // 次/秒
        public const float PlayerMoveSpeed = 3.5f;   // 单位/秒
        public const float EnemyAttackSpeed = 1.5f;  // 次/秒
        public const float EnemyMoveSpeed = 2.5f;    // 单位/秒

        /// <summary>攻速 → 攻击间隔（秒）。</summary>
        public static float AttackInterval(float attackSpeed)
            => 1f / Math.Max(0.01f, attackSpeed);

        /// <summary>
        /// 按给定波动系数结算一次伤害（不含随机取数，便于纯逻辑测试）。
        /// 结果最小为 1。
        /// </summary>
        public static int EvaluateDamage(int attack, int defense, float variance)
        {
            float raw = attack * variance * 100f / (100f + defense);
            return Math.Max(1, (int)Math.Round(raw));
        }

        // ==================== 波次成长公式（0.0.6） ====================
        // 第1关小怪：体=5 → HP=60、攻=10
        // 守关Boss：血=该关小怪×3、攻×2.5

        private const float WAVE_HP_MULTIPLIER_SAME_REALM = 1.15f;
        private const float WAVE_ATK_MULTIPLIER_SAME_REALM = 1.08f;
        private const float REALM_SWITCH_HP_MULTIPLIER = 1.8f;
        private const float REALM_SWITCH_ATK_MULTIPLIER = 1.6f;
        private const float BOSS_HP_MULTIPLIER = 3f;
        private const float BOSS_ATK_MULTIPLIER = 2.5f;
        private const float BASE_ENEMY_HP = 100f;
        private const float BASE_ENEMY_ATK = 10f;

        /// <summary>计算指定波次小怪的基准血量。</summary>
        public static float CalcEnemyHp(int waveNumber)
        {
            int realmIdx = (waveNumber - 1) / 10;
            int waveInRealm = ((waveNumber - 1) % 10) + 1;

            float hp = BASE_ENEMY_HP;
            for (int i = 1; i < waveInRealm; i++)
                hp *= WAVE_HP_MULTIPLIER_SAME_REALM;
            for (int i = 0; i < realmIdx; i++)
                hp *= REALM_SWITCH_HP_MULTIPLIER;
            return hp;
        }

        /// <summary>计算指定波次小怪的基准攻击。</summary>
        public static float CalcEnemyAtk(int waveNumber)
        {
            int realmIdx = (waveNumber - 1) / 10;
            int waveInRealm = ((waveNumber - 1) % 10) + 1;

            float atk = BASE_ENEMY_ATK;
            for (int i = 1; i < waveInRealm; i++)
                atk *= WAVE_ATK_MULTIPLIER_SAME_REALM;
            for (int i = 0; i < realmIdx; i++)
                atk *= REALM_SWITCH_ATK_MULTIPLIER;
            return atk;
        }

        /// <summary>计算指定波次Boss的血量。</summary>
        public static float CalcBossHp(int waveNumber) => CalcEnemyHp(waveNumber) * BOSS_HP_MULTIPLIER;

        /// <summary>计算指定波次Boss的攻击。</summary>
        public static float CalcBossAtk(int waveNumber) => CalcEnemyAtk(waveNumber) * BOSS_ATK_MULTIPLIER;
    }
}