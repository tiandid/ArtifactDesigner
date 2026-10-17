using System;
using System.Collections.Generic;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Artifact
{
    /// <summary>
    /// 炼器部件（数据模型，纯逻辑层，不依赖 UnityEngine）。
    /// 部件是打怪掉落的主要产出，可挂载到器坯格子中。
    /// 七类部件：
    ///   AddSlot      增加格子
    ///   Durability   增加耐久
    ///   Attack       增加攻击
    ///   Speed        增加速度
    ///   ActiveSkill  增加主动技能
    ///   PassiveSkill  增加被动技能
    ///   SpiritCore   灵核（原能量核心，为技能部件供能）
    /// </summary>
    [Serializable]
    public class ArtifactPart
    {
        public string partId;          // 唯一 ID
        public string partName;        // 名称
        public PartType partType;      // 部件类型
        public Rarity rarity;          // 稀有度
        public int weight;             // 部件自身重量
        public string description;     // 描述
        public ElementType element;      // 五行属性

        // —— 各类部件生效数值 ——
        public int addSlots;           // AddSlot: 额外格子数
        public int addDurability;      // Durability: 耐久加成
        public int addAttack;          // Attack: 攻击加成
        public float addSpeed;         // Speed: 速度加成
        public int spiritOutput;       // SpiritCore: 灵核输出（供电量）
        public List<SkillDefinition> skills = new List<SkillDefinition>(); // ActiveSkill / PassiveSkill

        public ArtifactPart Clone()
        {
            var clone = (ArtifactPart)MemberwiseClone();
            clone.skills = new List<SkillDefinition>();
            foreach (var s in skills) clone.skills.Add(s.Clone());
            return clone;
        }

        /// <summary>是否为灵核部件（原能量核心）。</summary>
        public bool IsSpiritCore => partType == PartType.SpiritCore;

        /// <summary>是否为技能类部件（主动/被动）。</summary>
        public bool IsSkillPart => partType == PartType.ActiveSkill || partType == PartType.PassiveSkill;

        /// <summary>获取部件携带的全部技能（技能类部件才有数据）。</summary>
        public List<SkillDefinition> GetSkills()
        {
            return skills ?? new List<SkillDefinition>();
        }

        /// <summary>是否携带指定类型的技能。</summary>
        public bool HasSkill(SkillType type)
        {
            foreach (var s in GetSkills())
                if (s.skillType == type) return true;
            return false;
        }
    }
}