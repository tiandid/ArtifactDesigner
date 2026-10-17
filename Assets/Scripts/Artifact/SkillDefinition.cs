using System;
using System.Collections.Generic;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Artifact
{
    /// <summary>
    /// 技能定义。主动/被动技能统一由 SkillDefinition 描述。
    /// 技能要生效必须接入能量线路（见 Energy 模块）。
    /// </summary>
    [Serializable]
    public class SkillDefinition
    {
        public string skillId;          // 技能唯一 ID
        public string skillName;        // 技能名称
        public SkillType skillType;     // 主动/被动
        public int energyCostPerUse;    // 每次使用（或维持）消耗的能量
        public int basePower;           // 基础威力/效果数值
        public string description;      // 描述

        public SkillDefinition() { }

        public SkillDefinition(string id, string name, SkillType type, int energyCost, int power, string desc)
        {
            skillId = id;
            skillName = name;
            skillType = type;
            energyCostPerUse = energyCost;
            basePower = power;
            description = desc;
        }

        public SkillDefinition Clone()
        {
            return new SkillDefinition(skillId, skillName, skillType, energyCostPerUse, basePower, description);
        }
    }
}