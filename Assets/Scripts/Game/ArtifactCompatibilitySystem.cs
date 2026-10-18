using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArtifactDesigner.Artifact;
using ArtifactDesigner.Core;
using ArtifactDesigner.Energy;

namespace ArtifactDesigner.Game
{
    /// <summary>
    /// 法宝契合度评级系统（单例 MonoBehaviour）。
    /// 0.0.5 新增：根据器胚大小、灵核品质/境界、输出结构数量/类型/品质，
    /// 判定法宝契合度档位，达标后提升法宝质量并进入下一档判定。
    /// 高级灵核和结构在判定时可兼容低级。
    /// </summary>
    public class ArtifactCompatibilitySystem : MonoBehaviour
    {
        public static ArtifactCompatibilitySystem Instance { get; private set; }

        // ---------- 事件 ----------
        public static event Action<ArtifactBlank, Rarity, Rarity> OnArtifactQualityUpgraded; // (器胚, 旧品质, 新品质)
        public static event Action<ArtifactBlank, float, string> OnCompatibilityEvaluated;  // (器胚, 契合度百分比, 评语)

        // ---------- 配置：各品质档位的准入条件 ----------
        [Serializable]
        public class CompatibilityTier
        {
            public Rarity targetRarity;          // 目标品质（白→蓝→紫→金→橙→红）
            public int minBlankSlots;              // 器胚最小总格子数
            public int minCoreQuality;             // 灵核最小品质（0=白,1=蓝...）
            public int minOutputStructureCount;    // 输出结构最小数量
            public bool requireEnergyLine;         // 是否要求供能线已连接
            public int minEnergyCoveragePercent;   // 供能覆盖最小百分比（0~100）
        }

        public List<CompatibilityTier> tiers = new List<CompatibilityTier>
        {
            // 白：最基础，只要有灵核即可
            new CompatibilityTier
            {
                targetRarity = Rarity.Common,
                minBlankSlots = 1,
                minCoreQuality = 0,
                minOutputStructureCount = 0,
                requireEnergyLine = false,
                minEnergyCoveragePercent = 0
            },
            // 蓝：需要至少3格，蓝/白灵核，1个输出结构
            new CompatibilityTier
            {
                targetRarity = Rarity.Uncommon,
                minBlankSlots = 3,
                minCoreQuality = 1,
                minOutputStructureCount = 1,
                requireEnergyLine = false,
                minEnergyCoveragePercent = 0
            },
            // 紫：5格，紫/蓝/白灵核，2个输出结构，需供能线
            new CompatibilityTier
            {
                targetRarity = Rarity.Rare,
                minBlankSlots = 5,
                minCoreQuality = 2,
                minOutputStructureCount = 2,
                requireEnergyLine = true,
                minEnergyCoveragePercent = 50
            },
            // 金：7格，金/紫/蓝/白灵核，3个输出结构，供能线覆盖≥70%
            new CompatibilityTier
            {
                targetRarity = Rarity.Epic,
                minBlankSlots = 7,
                minCoreQuality = 3,
                minOutputStructureCount = 3,
                requireEnergyLine = true,
                minEnergyCoveragePercent = 70
            },
            // 橙：9格，橙/金/紫/蓝/白灵核，4个输出结构，供能线覆盖≥85%
            new CompatibilityTier
            {
                targetRarity = Rarity.Legendary,
                minBlankSlots = 9,
                minCoreQuality = 4,
                minOutputStructureCount = 4,
                requireEnergyLine = true,
                minEnergyCoveragePercent = 85
            },
            // 红：12格，红/橙/金/紫/蓝/白灵核，5个输出结构，供能线全覆盖
            new CompatibilityTier
            {
                targetRarity = Rarity.Mythic,
                minBlankSlots = 12,
                minCoreQuality = 5,
                minOutputStructureCount = 5,
                requireEnergyLine = true,
                minEnergyCoveragePercent = 100
            }
        };

        // ---------- 配置：各法宝类型的理想输出结构 ----------
        [Serializable]
        public class ArtifactTypeIdealConfig
        {
            public string typeName;                         // 法宝类型名称
            public List<PartType> idealStructureTypes;        // 理想结构类型列表（顺序即优先级）
            public string description;                      // 类型说明
        }

        public List<ArtifactTypeIdealConfig> artifactTypeIdeals = new List<ArtifactTypeIdealConfig>
        {
            new ArtifactTypeIdealConfig
            {
                typeName = "剑",
                idealStructureTypes = new List<PartType> { PartType.Attack, PartType.ActiveSkill },
                description = "剑类法宝：追求锋锐与主动技，理想配置为1攻击+1主动技能"
            },
            new ArtifactTypeIdealConfig
            {
                typeName = "钟",
                idealStructureTypes = new List<PartType> { PartType.PassiveSkill, PartType.PassiveSkill },
                description = "钟类法宝：以守为主，理想配置为2被动技能"
            },
            new ArtifactTypeIdealConfig
            {
                typeName = "环",
                idealStructureTypes = new List<PartType> { PartType.Attack, PartType.ActiveSkill, PartType.ActiveSkill },
                description = "环类法宝：攻守兼备，理想配置为1攻击+2主动技能"
            },
            new ArtifactTypeIdealConfig
            {
                typeName = "塔",
                idealStructureTypes = new List<PartType> { PartType.Attack, PartType.PassiveSkill, PartType.ActiveSkill },
                description = "塔类法宝：镇压四方，理想配置为1攻击+1被动+1主动技能"
            },
            new ArtifactTypeIdealConfig
            {
                typeName = "印",
                idealStructureTypes = new List<PartType> { PartType.Attack, PartType.Attack, PartType.PassiveSkill },
                description = "印类法宝：重攻轻守，理想配置为2攻击+1被动技能"
            },
            new ArtifactTypeIdealConfig
            {
                typeName = "幡",
                idealStructureTypes = new List<PartType> { PartType.ActiveSkill, PartType.ActiveSkill, PartType.ActiveSkill },
                description = "幡类法宝：神通变化，理想配置为3主动技能"
            },
            new ArtifactTypeIdealConfig
            {
                typeName = "葫芦",
                idealStructureTypes = new List<PartType> { PartType.Attack, PartType.Attack, PartType.ActiveSkill, PartType.ActiveSkill },
                description = "葫芦类法宝：收放自如，理想配置为2攻击+2主动技能"
            },
            new ArtifactTypeIdealConfig
            {
                typeName = "镜",
                idealStructureTypes = new List<PartType> { PartType.Attack, PartType.PassiveSkill, PartType.ActiveSkill, PartType.ActiveSkill },
                description = "镜类法宝：虚实相映，理想配置为1攻击+1被动+2主动技能"
            },
            new ArtifactTypeIdealConfig
            {
                typeName = "鼎",
                idealStructureTypes = new List<PartType> { PartType.PassiveSkill, PartType.PassiveSkill, PartType.PassiveSkill },
                description = "鼎类法宝：稳如泰山，理想配置为3被动技能"
            },
            new ArtifactTypeIdealConfig
            {
                typeName = "珠",
                idealStructureTypes = new List<PartType> { PartType.Attack, PartType.ActiveSkill, PartType.PassiveSkill },
                description = "珠类法宝：圆融贯通，理想配置为1攻击+1主动+1被动技能"
            }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ==================== 核心：契合度判定与品质进阶 ====================

        /// <summary>
        /// 对指定器胚进行契合度判定，返回可达成的最高品质。
        /// 规则：从当前品质的下一档开始逐档检查，高级兼容低级（允许使用≤目标档位的灵核/结构）。
        /// </summary>
        public Rarity EvaluateMaxQuality(ArtifactBlank blank, string artifactTypeName, EnergyNetwork network = null)
        {
            if (blank == null) return Rarity.Common;

            Rarity current = blank.rarity;
            int currentIdx = (int)current;

            for (int i = currentIdx + 1; i < tiers.Count; i++)
            {
                var tier = tiers[i];
                if (!MeetsTier(blank, tier, artifactTypeName, network))
                    break; // 不满足该档，更高档也必然不满足（要求递增）
                current = tier.targetRarity;
            }

            return current;
        }

        /// <summary>
        /// 尝试提升器胚品质到判定后的最高档。返回是否发生提升。</summary>
        public bool TryUpgradeQuality(ArtifactBlank blank, string artifactTypeName, EnergyNetwork network = null)
        {
            if (blank == null) return false;
            Rarity oldRarity = blank.rarity;
            Rarity newRarity = EvaluateMaxQuality(blank, artifactTypeName, network);
            if (newRarity > oldRarity)
            {
                blank.rarity = newRarity;
                float fitPercent = CalculateFitPercentage(blank, artifactTypeName, network);
                string comment = GetCompatibilityComment(newRarity, fitPercent);
                OnArtifactQualityUpgraded?.Invoke(blank, oldRarity, newRarity);
                OnCompatibilityEvaluated?.Invoke(blank, fitPercent, comment);
                return true;
            }
            return false;
        }

        /// <summary>计算当前器胚的契合度百分比（0~100）。</summary>
        public float CalculateFitPercentage(ArtifactBlank blank, string artifactTypeName, EnergyNetwork network = null)
        {
            if (blank == null) return 0f;

            float score = 0f;

            // 1. 器胚大小占比 15%（原20%，0.0.6调整）
            int maxNeededSlots = tiers[tiers.Count - 1].minBlankSlots;
            float slotRatio = Mathf.Clamp01((float)blank.TotalSlots / maxNeededSlots);
            score += slotRatio * 15f;

            // 2. 灵核品质占比 15%（原25%，0.0.6调整）
            var core = blank.slots.FirstOrDefault(p => p != null && p.partType == PartType.SpiritCore);
            if (core != null)
            {
                float coreRatio = ((int)core.rarity + 1) / 6f; // 白=1/6, 红=6/6
                score += coreRatio * 15f;
            }

            // 3. 结构配置匹配度占比 20%（原35%，0.0.6调整）
            var ideal = artifactTypeIdeals.FirstOrDefault(t => t.typeName == artifactTypeName);
            if (ideal != null)
            {
                float structureScore = CalculateStructureMatchScore(blank, ideal);
                score += structureScore * 20f;
            }
            else
            {
                // 无特定类型要求时，按输出结构总数评分
                int outputCount = blank.slots.Count(p => p != null && IsOutputStructure(p));
                float countRatio = Mathf.Clamp01((float)outputCount / 5f);
                score += countRatio * 20f;
            }

            // 4. 供能完整度占比 15%（原20%，0.0.6调整）
            if (network != null)
            {
                float energyScore = CalculateEnergyCoverage(blank, network);
                score += energyScore * 15f;
            }

            // 5. 五行匹配加成 20%（0.0.6新增）
            float elementScore = CalculateElementMatchScore(blank);
            score += elementScore * 20f;

            // 6. 重量效率比 10%（0.0.6新增）
            float weightEfficiency = CalculateWeightEfficiencyScore(blank);
            score += weightEfficiency * 10f;

            // 7. 技能协同度 5%（0.0.6新增）
            float synergyScore = CalculateSkillSynergyScore(blank);
            score += synergyScore * 5f;

            return Mathf.Clamp(score, 0f, 100f);
        }

        // ==================== 分档条件检查 ====================

        private bool MeetsTier(ArtifactBlank blank, CompatibilityTier tier, string artifactTypeName, EnergyNetwork network)
        {
            // 1. 器胚大小
            if (blank.TotalSlots < tier.minBlankSlots) return false;

            // 2. 灵核品质（高级兼容低级：允许使用≤目标档位的品质）
            var core = blank.slots.FirstOrDefault(p => p != null && p.partType == PartType.SpiritCore);
            if (core == null) return false;
            if ((int)core.rarity < tier.minCoreQuality && !IsCompatibleQuality(core.rarity, tier.minCoreQuality))
                return false;

            // 3. 输出结构数量
            int outputCount = blank.slots.Count(p => p != null && IsOutputStructure(p));
            if (outputCount < tier.minOutputStructureCount) return false;

            // 4. 输出结构品质检查（所有输出结构必须≥目标档允许的最小品质，允许降级兼容）
            foreach (var part in blank.slots)
            {
                if (part == null || !IsOutputStructure(part)) continue;
                if ((int)part.rarity < tier.minCoreQuality && !IsCompatibleQuality(part.rarity, tier.minCoreQuality))
                    return false;
            }

            // 5. 供能线要求
            if (tier.requireEnergyLine)
            {
                if (network == null) return false;
                float coverage = CalculateEnergyCoverage(blank, network);
                if (coverage * 100f < tier.minEnergyCoveragePercent) return false;
            }

            // 6. 法宝类型结构匹配（额外加分项，但不作为硬性门槛——不同品质档对类型匹配度的容忍度不同）
            var ideal = artifactTypeIdeals.FirstOrDefault(t => t.typeName == artifactTypeName);
            if (ideal != null)
            {
                float match = CalculateStructureMatchScore(blank, ideal);
                // 紫及以上要求结构匹配度≥50%，金及以上≥70%
                float minMatch = tier.targetRarity switch
                {
                    Rarity.Rare => 0.50f,
                    Rarity.Epic => 0.70f,
                    Rarity.Legendary => 0.80f,
                    Rarity.Mythic => 0.90f,
                    _ => 0f
                };
                if (match < minMatch) return false;
            }

            return true;
        }

        /// <summary>高级品质是否兼容低级品质：蓝兼容白，紫兼容蓝白，以此类推。</summary>
        private bool IsCompatibleQuality(Rarity actual, int requiredMin)
        {
            return (int)actual <= requiredMin; // 实际品质档次≤要求的最小档，即为兼容
        }

        private bool IsOutputStructure(ArtifactPart part)
        {
            return part.partType == PartType.Attack
                || part.partType == PartType.ActiveSkill
                || part.partType == PartType.PassiveSkill;
        }

        // ==================== 五行匹配评分（0.0.6） ====================

        private static readonly ElementType[] GENERATE_CYCLE = {
            ElementType.Metal, ElementType.Water, ElementType.Wood, ElementType.Fire, ElementType.Earth
        };

        private static readonly ElementType[] OVERCOME_CYCLE = {
            ElementType.Metal, ElementType.Wood, ElementType.Earth, ElementType.Water, ElementType.Fire
        };

        private bool IsGenerating(ElementType a, ElementType b)
        {
            int idxA = System.Array.IndexOf(GENERATE_CYCLE, a);
            int idxB = System.Array.IndexOf(GENERATE_CYCLE, b);
            return (idxA + 1) % 5 == idxB;
        }

        private bool IsOvercoming(ElementType a, ElementType b)
        {
            int idxA = System.Array.IndexOf(OVERCOME_CYCLE, a);
            int idxB = System.Array.IndexOf(OVERCOME_CYCLE, b);
            return (idxA + 1) % 5 == idxB;
        }

        /// <summary>
        /// 获取五行匹配倍率。
        /// 规则：同属性100%，相生100%，无关60%，相克50%。
        /// </summary>
        private float GetElementMatchMultiplier(ElementType coreElement, ElementType partElement)
        {
            if (coreElement == partElement) return 1.00f;                // 同属性 100%
            if (IsGenerating(coreElement, partElement)) return 1.00f;        // 相生 100%
            if (IsOvercoming(coreElement, partElement)) return 0.50f;        // 相克 50%
            return 0.60f;                                                 // 无关 60%
        }

        /// <summary>
        /// 计算所有输出/功能部件与灵核的五行匹配度（0~1）。
        /// 要求：必须有灵核；无功能部件视为满分。
        /// </summary>
        private float CalculateElementMatchScore(ArtifactBlank blank)
        {
            var core = blank.slots.FirstOrDefault(p => p != null && p.partType == PartType.SpiritCore);
            if (core == null) return 0f;

            float totalMatch = 0f;
            int count = 0;

            foreach (var part in blank.slots)
            {
                if (part == null) continue;
                // 只统计功能输出类部件（不统计扩展格子的AddSlot和灵核本身）
                if (part.partType == PartType.AddSlot || part.partType == PartType.SpiritCore) continue;

                float multiplier = GetElementMatchMultiplier(core.element, part.element);
                totalMatch += multiplier;
                count++;
            }

            return count == 0 ? 1f : totalMatch / count;
        }

        // ==================== 重量效率比评分（0.0.6） ====================

        /// <summary>
        /// 计算重量效率比（0~1）。
        /// 公式：各功能部件的（有效属性值 / 自身重量）的平均值，归一化到理想上限5。
        /// </summary>
        private float CalculateWeightEfficiencyScore(ArtifactBlank blank)
        {
            float totalRatio = 0f;
            int count = 0;

            foreach (var part in blank.slots)
            {
                if (part == null || part.weight <= 0) continue;

                float value = 0f;
                switch (part.partType)
                {
                    case PartType.Attack:
                        value = part.addAttack;
                        break;
                    case PartType.Speed:
                        value = part.addSpeed * 10f;
                        break;
                    case PartType.SpiritCore:
                        value = part.spiritOutput;
                        break;
                    case PartType.ActiveSkill:
                    case PartType.PassiveSkill:
                        foreach (var s in part.skills)
                            value += s.basePower;
                        break;
                    default:
                        continue; // AddSlot/Durability不计算效率
                }

                totalRatio += value / part.weight;
                count++;
            }

            if (count == 0) return 1f; // 无功能部件视为满分
            float avgRatio = totalRatio / count;
            return Mathf.Clamp01(avgRatio / 5f); // 理想效率上限为5
        }

        // ==================== 技能协同度评分（0.0.6） ====================

        /// <summary>
        /// 计算技能协同度（0~1）。
        /// 评分维度：
        /// 1. 主动+被动混合 = 高协同；
        /// 2. 多属性技能配合 = 加成；
        /// 3. 技能过多 = 轻微惩罚。
        /// </summary>
        private float CalculateSkillSynergyScore(ArtifactBlank blank)
        {
            var skillList = new List<(SkillType type, ElementType element)>();
            foreach (var part in blank.slots)
            {
                if (part == null || !IsOutputStructure(part)) continue;
                foreach (var s in part.skills)
                    skillList.Add((s.skillType, part.element));
            }

            if (skillList.Count == 0) return 0.5f; // 无技能视为中等协同
            if (skillList.Count == 1) return 0.6f; // 单一技能

            float synergy = 0.5f;

            // 主动被动混合加成
            bool hasActive = skillList.Any(s => s.type == SkillType.Active);
            bool hasPassive = skillList.Any(s => s.type == SkillType.Passive);
            if (hasActive && hasPassive) synergy += 0.2f;

            // 五行多样性加成（多属性配合）
            var distinctElements = skillList.Select(s => s.element).Distinct().ToList();
            if (distinctElements.Count > 1) synergy += 0.15f;

            // 技能数量过多惩罚
            if (skillList.Count > 4)
                synergy -= 0.1f * (skillList.Count - 4);

            return Mathf.Clamp01(synergy);
        }

        // ==================== 结构匹配评分 ====================

        /// <summary>计算实际结构配置与理想配置的匹配度（0~1）。</summary>
        private float CalculateStructureMatchScore(ArtifactBlank blank, ArtifactTypeIdealConfig ideal)
        {
            if (ideal?.idealStructureTypes == null || ideal.idealStructureTypes.Count == 0) return 1f;

            // 统计实际各类型数量
            var actualCounts = new Dictionary<PartType, int>();
            foreach (var p in blank.slots)
            {
                if (p == null || !IsOutputStructure(p)) continue;
                if (!actualCounts.ContainsKey(p.partType)) actualCounts[p.partType] = 0;
                actualCounts[p.partType]++;
            }

            // 统计理想各类型数量
            var idealCounts = new Dictionary<PartType, int>();
            foreach (var t in ideal.idealStructureTypes)
            {
                if (!idealCounts.ContainsKey(t)) idealCounts[t] = 0;
                idealCounts[t]++;
            }

            // 匹配度 = 各理想类型中实际满足比例的平均值
            float totalMatch = 0f;
            int typeCount = 0;
            foreach (var kv in idealCounts)
            {
                int actual = actualCounts.TryGetValue(kv.Key, out var a) ? a : 0;
                float ratio = Mathf.Clamp01((float)actual / kv.Value);
                totalMatch += ratio;
                typeCount++;
            }

            // 额外惩罚：非理想类型的输出结构过多（偏离理想配置）
            int totalOutput = actualCounts.Values.Sum();
            int idealTotal = idealCounts.Values.Sum();
            if (totalOutput > idealTotal)
            {
                float excessRatio = (float)(totalOutput - idealTotal) / Mathf.Max(idealTotal, 1);
                totalMatch -= excessRatio * 0.2f; // 每多一个非理想类型，扣20%匹配度（上限内）
            }

            return typeCount == 0 ? 1f : Mathf.Clamp01(totalMatch / typeCount);
        }

        // ==================== 供能覆盖评分 ====================

        private float CalculateEnergyCoverage(ArtifactBlank blank, EnergyNetwork network)
        {
            if (blank == null || network == null) return 0f;

            // 统计器胚中所有需要供能的技能类部件
            var skillParts = blank.slots.Where(p => p != null && p.IsSkillPart).ToList();
            if (skillParts.Count == 0) return 1f; // 无技能部件=无需供能=满分

            int poweredCount = 0;
            foreach (var part in skillParts)
            {
                // 通过 sourcePartId 找到对应节点，检查是否供电
                var node = network.Nodes.FirstOrDefault(n => n.sourcePartId == part.partId);
                if (node != null && network.IsPowered(node.nodeId))
                    poweredCount++;
            }

            return (float)poweredCount / skillParts.Count;
        }

        // ==================== 评语 ====================

        private string GetCompatibilityComment(Rarity rarity, float fitPercent)
        {
            string baseDesc = rarity switch
            {
                Rarity.Common => "初具雏形",
                Rarity.Uncommon => "小有所成",
                Rarity.Rare => "融会贯通",
                Rarity.Epic => "炉火纯青",
                Rarity.Legendary => "出神入化",
                Rarity.Mythic => "返璞归真",
                _ => "未知"
            };

            string suffix = fitPercent switch
            {
                >= 95f => "，天作之合",
                >= 85f => "，珠联璧合",
                >= 70f => "，相得益彰",
                >= 50f => "，勉力可成",
                _ => "，尚需打磨"
            };

            return baseDesc + suffix;
        }

        // ==================== 工具：获取器胚当前所有输出结构信息 ====================

        public List<ArtifactPart> GetOutputStructures(ArtifactBlank blank)
        {
            if (blank == null) return new List<ArtifactPart>();
            return blank.slots.Where(p => p != null && IsOutputStructure(p)).ToList();
        }

        public ArtifactPart GetSpiritCore(ArtifactBlank blank)
        {
            if (blank == null) return null;
            return blank.slots.FirstOrDefault(p => p != null && p.partType == PartType.SpiritCore);
        }
    }
}
