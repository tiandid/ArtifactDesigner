using System;

namespace ArtifactDesigner.Core
{
    /// <summary>
    /// 永久属性系统：存储升级+加点+属性丹+突破奖励等不会随装备/功法/法宝切换而消失的固定加成。
    /// 挂在 PlayerProfile 上，随档案持久化。
    /// </summary>
    [Serializable]
    public class PermanentStats
    {
        // ---------- 突破奖励系数（九品→超品） ----------
        // 护体倍率：九品+0% → 超品+50%，每品+5%
        public float ShieldMultiplier { get; private set; } = 0f;

        // 真气品质：九品+0% → 超品+45%，每品+5%
        public float QiQualityMultiplier { get; private set; } = 0f;

        // 凝实度系数：复用血量倍率（九品×2 ~ 超品×11）
        public float SolidityMultiplier { get; private set; } = 1f;

        // 物理攻击系数：九品+0% → 超品+30%，每品+3%
        public float PhysicalAttackMultiplier { get; private set; } = 0f;

        // 属性攻击系数：九品+0% → 超品+30%，每品+3%
        public float ElementalAttackMultiplier { get; private set; } = 0f;

        // 精神攻击系数：九品+0% → 超品+30%，每品+3%
        public float SpiritualAttackMultiplier { get; private set; } = 0f;

        // 物理防御系数：九品+0% → 超品+25%，每品+2.5%
        public float PhysicalDefenseMultiplier { get; private set; } = 0f;

        // 属性防御系数：九品+0% → 超品+25%，每品+2.5%
        public float ElementalDefenseMultiplier { get; private set; } = 0f;

        // 精神防御系数：九品+0% → 超品+25%，每品+2.5%
        public float SpiritualDefenseMultiplier { get; private set; } = 0f;

        // ---------- 累计奖励接口 ----------

        /// <summary>
        /// 应用一次突破奖励。每次突破后调用一次，系数累加。
        /// </summary>
        public void ApplyBreakthroughRewards(GoldenCoreRank rank)
        {
            int step = (int)rank; // 九品=0, 八品=1, ... 一品=8, 超品=9

            // 防御类：每品+2.5%
            float defInc = step * 0.025f;
            PhysicalDefenseMultiplier += defInc;
            ElementalDefenseMultiplier += defInc;
            SpiritualDefenseMultiplier += defInc;

            // 攻击类：每品+3%
            float atkInc = step * 0.03f;
            PhysicalAttackMultiplier += atkInc;
            ElementalAttackMultiplier += atkInc;
            SpiritualAttackMultiplier += atkInc;

            // 护体：每品+5%
            ShieldMultiplier += step * 0.05f;

            // 真气：每品+5%（超品封顶45%，九品封顶40%？按step×5%，超品=45%）
            QiQualityMultiplier += step * 0.05f;

            // 凝实度：直接乘倍率
            float[] solidityTable = { 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f };
            SolidityMultiplier = solidityTable[step];

            // 触发事件（如有UI需要刷新）
            OnStatsChanged?.Invoke();
        }

        public static event Action OnStatsChanged;

        /// <summary>调试用：重置所有系数。</summary>
        public void Reset()
        {
            ShieldMultiplier = 0f;
            QiQualityMultiplier = 0f;
            SolidityMultiplier = 1f;
            PhysicalAttackMultiplier = 0f;
            ElementalAttackMultiplier = 0f;
            SpiritualAttackMultiplier = 0f;
            PhysicalDefenseMultiplier = 0f;
            ElementalDefenseMultiplier = 0f;
            SpiritualDefenseMultiplier = 0f;
        }
    }
}
