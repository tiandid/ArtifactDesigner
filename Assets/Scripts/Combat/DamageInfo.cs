using ArtifactDesigner.Core;
using ArtifactDesigner.Entity;

namespace ArtifactDesigner.Combat
{
    /// <summary>一次伤害结算的完整信息，供事件总线广播与 UI 飘字。</summary>
    public struct DamageInfo
    {
        public int amount;          // 最终伤害值（≥1）
        public DamageType type;     // 伤害类型
        public BattleEntity source; // 伤害来源（可为 null）
        public BattleEntity target; // 承受方

        public static DamageInfo Create(int amount, DamageType type, BattleEntity source, BattleEntity target)
        {
            return new DamageInfo { amount = amount, type = type, source = source, target = target };
        }
    }
}