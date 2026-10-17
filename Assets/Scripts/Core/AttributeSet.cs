using System;

namespace ArtifactDesigner.Core
{
    /// <summary>
    /// 属性集合。用于角色、器坯与部件的数值表示。
    /// 属性通过加法合并：器坯最终属性 = 基础属性 + 所挂载部件属性之和。
    /// </summary>
    [Serializable]
    public struct AttributeSet
    {
        public int attack;        // 攻击
        public int defense;       // 防御（预留）
        public float speed;       // 速度
        public int maxDurability; // 耐久上限
        public int maxEnergy;     // 能量上限（配合能量核心）

        public bool IsZero =>
            attack == 0 && defense == 0 && speed == 0f && maxDurability == 0 && maxEnergy == 0;

        public static AttributeSet operator +(AttributeSet a, AttributeSet b)
        {
            return new AttributeSet
            {
                attack = a.attack + b.attack,
                defense = a.defense + b.defense,
                speed = a.speed + b.speed,
                maxDurability = a.maxDurability + b.maxDurability,
                maxEnergy = a.maxEnergy + b.maxEnergy
            };
        }

        public override string ToString()
        {
            return $"攻击:{attack} 防御:{defense} 速度:{speed:0.##} 耐久:{maxDurability} 能量:{maxEnergy}";
        }
    }
}