using ArtifactDesigner.Core;

namespace ArtifactDesigner.Artifact
{
    /// <summary>
    /// 演示/测试用部件工厂：按类型生成可用的示例部件，
    /// 供编辑器调试与后续打怪掉落演示复用。
    /// </summary>
    public static class DemoParts
    {
        public static ArtifactPart Create(PartType type)
        {
            var part = new ArtifactPart
            {
                partId = $"demo_{type}",
                partType = type,
                rarity = Rarity.Uncommon,
                weight = 2
            };

            switch (type)
            {
                case PartType.AddSlot:
                    part.partName = "扩容石";
                    part.addSlots = 1;
                    part.description = "增加 1 个格子，同时增加重量。";
                    break;
                case PartType.Durability:
                    part.partName = "坚固符文";
                    part.addDurability = 60;
                    part.description = "增加耐久上限 60。";
                    break;
                case PartType.Attack:
                    part.partName = "锋锐灵石";
                    part.addAttack = 15;
                    part.description = "增加攻击 15。";
                    break;
                case PartType.Speed:
                    part.partName = "疾风符";
                    part.addSpeed = 0.3f;
                    part.description = "增加速度 0.3。";
                    break;
                case PartType.ActiveSkill:
                    part.partName = "烈焰斩";
                    part.skills.Add(new SkillDefinition(
                        "skill_fire_slash", "烈焰斩", SkillType.Active,
                        energyCost: 10, power: 30, desc: "消耗 10 能量，对目标造成 30 点火焰伤害。"));
                    part.description = "获得主动技能「烈焰斩」，需接入能量线路。";
                    break;
                case PartType.PassiveSkill:
                    part.partName = "灵盾";
                    part.skills.Add(new SkillDefinition(
                        "skill_spirit_shield", "灵盾", SkillType.Passive,
                        energyCost: 5, power: 20, desc: "每回合消耗 5 能量，获得 20 点护盾。"));
                    part.description = "获得被动技能「灵盾」，需接入能量线路。";
                    break;
                case PartType.SpiritCore:
                    part.partName = "玄晶核心";
                    part.spiritOutput = 30;
                    part.description = "灵核核心，输出 30 点能量；需用线路连接技能部件。";
                    break;
                default:
                    part.partName = "未知部件";
                    break;
            }

            return part;
        }
    }
}