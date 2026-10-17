using System;
using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.SpiritGathering
{
    /// <summary>
    /// 聚灵阵浓度计算器。
    /// 负责计算阵眼处的总聚灵浓度、属性偏向、等效值等。
    /// </summary>
    public static class ConcentrationCalculator
    {
        // ---------- 距离衰减表（曼哈顿距离 → 衰减系数） ----------
        private static readonly float[] DistanceFactors = new float[]
        {
            0f,    // dist=0 (阵眼，不可放)
            1.0f,  // dist=1
            0.85f, // dist=2
            0.70f, // dist=3
            0.55f, // dist=4
            0.40f, // dist=5
            0.30f, // dist=6
            0.20f, // dist=7
            0.15f, // dist=8+
        };

        // ---------- 相生相克定义 ----------
        private static readonly ElementType[] GenerateCycle = new ElementType[]
        {
            ElementType.Metal, ElementType.Water, ElementType.Wood, ElementType.Fire, ElementType.Earth
        };

        // ---------- 浓度软上限表（按境界） ----------
        private static readonly float[] SoftCaps = new float[]
        {
            20f,   // Mortal
            50f,   // QiRefining
            100f,  // Foundation
            200f,  // GoldenCore
            350f,  // NascentSoul
            550f,  // SpiritSever
            800f,  // VoidRefining
            1100f, // BodyMerge
            1500f, // Mahayana
            2000f  // Tribulation
        };

        /// <summary>
        /// 计算指定阵盘的阵眼总聚灵浓度。
        /// </summary>
        public static float CalculateTotalConcentration(SpiritGatheringArray array)
        {
            if (array == null || array.Slots == null) return 0f;

            float total = 0f;
            var slots = array.Slots;

            // 第一步：计算每个灵物的基础贡献（含距离衰减）
            float[] baseContributions = new float[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.PlacedItem == null) continue;

                int dist = slot.ManhattanDistance;
                float distFactor = GetDistanceFactor(dist);
                int spiritValue = slot.PlacedItem.GetActualSpiritValue();
                baseContributions[i] = spiritValue * distFactor;
            }

            // 第二步：应用相邻加成
            float[] adjustedContributions = new float[slots.Count];
            Array.Copy(baseContributions, adjustedContributions, slots.Count);

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].PlacedItem == null) continue;
                var itemA = slots[i].PlacedItem;
                if (!itemA.Element.HasValue) continue;

                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (slots[j].PlacedItem == null) continue;
                    if (!slots[i].IsAdjacentTo(slots[j])) continue;

                    var itemB = slots[j].PlacedItem;
                    if (!itemB.Element.HasValue) continue;

                    // 检查 A → B 的关系
                    float multiplierAtoB = GetElementMultiplier(itemA.Element.Value, itemB.Element.Value);
                    float multiplierBtoA = GetElementMultiplier(itemB.Element.Value, itemA.Element.Value);

                    // A 对 B 的影响：B 的贡献 × multiplierAtoB（但只对 B 生效）
                    adjustedContributions[j] *= multiplierAtoB;

                    // B 对 A 的影响
                    adjustedContributions[i] *= multiplierBtoA;
                }
            }

            // 第三步：汇总 + 阵旗效果
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].PlacedItem == null) continue;
                total += adjustedContributions[i];

                // 阵旗特殊效果：周围 8 格 +5% 聚灵效率
                var item = slots[i].PlacedItem;
                if (item.IsFlag)
                {
                    for (int j = 0; j < slots.Count; j++)
                    {
                        if (i == j || slots[j].PlacedItem == null) continue;
                        if (slots[i].IsInSurrounding8(slots[j]))
                        {
                            total += adjustedContributions[j] * 0.05f;
                        }
                    }
                }
            }

            // 第四步：应用境界软上限
            total = ApplySoftCap(total, array.OwnerRealm);

            return total;
        }

        /// <summary>
        /// 计算阵眼灵气的五行属性偏向。
        /// </summary>
        public static Dictionary<ElementType, float> CalculateElementBias(SpiritGatheringArray array)
        {
            var bias = new Dictionary<ElementType, float>
            {
                { ElementType.Metal, 0f },
                { ElementType.Wood, 0f },
                { ElementType.Water, 0f },
                { ElementType.Fire, 0f },
                { ElementType.Earth, 0f }
            };

            float totalWeight = 0f;

            foreach (var slot in array.Slots)
            {
                if (slot.PlacedItem == null || !slot.PlacedItem.Element.HasValue) continue;

                int absValue = Mathf.Abs(slot.PlacedItem.GetActualSpiritValue());
                float distFactor = GetDistanceFactor(slot.ManhattanDistance);
                float weight = absValue * distFactor;

                var element = slot.PlacedItem.Element.Value;
                bias[element] += weight;
                totalWeight += weight;

                // 相生属性的间接权重
                ElementType? generates = GetGeneratedElement(element);
                if (generates.HasValue)
                    bias[generates.Value] += weight * 0.3f;

                // 相克属性的负权重
                ElementType? overcomes = GetOvercomeElement(element);
                if (overcomes.HasValue)
                    bias[overcomes.Value] -= weight * 0.2f;
            }

            // 归一化为百分比
            if (totalWeight > 0)
            {
                foreach (var key in new List<ElementType>(bias.Keys))
                {
                    bias[key] = Mathf.Max(0f, bias[key]) / totalWeight;
                }
            }

            return bias;
        }

        /// <summary>
        /// 获取阵眼主导五行属性。
        /// </summary>
        public static ElementType? GetDominantElement(SpiritGatheringArray array)
        {
            var bias = CalculateElementBias(array);
            ElementType? dominant = null;
            float maxBias = 0f;

            foreach (var kvp in bias)
            {
                if (kvp.Value > maxBias)
                {
                    maxBias = kvp.Value;
                    dominant = kvp.Key;
                }
            }

            // 主导属性阈值：至少 40% 才算有明确偏向
            return maxBias >= 0.4f ? dominant : null;
        }

        /// <summary>
        /// 计算线性衰减等效值（0~1）。
        /// </summary>
        public static float CalculateEffectiveValue(float concentration, Realm realm)
        {
            float maxTheoretical = GetMaxTheoreticalConcentration(realm);
            if (maxTheoretical <= 0) return 0f;

            float effective = Mathf.Log(concentration + 1f) / Mathf.Log(maxTheoretical + 1f);
            return Mathf.Clamp01(effective);
        }

        /// <summary>
        /// 获取当前境界的最大理论聚灵浓度（全红品聚灵珠满阵盘）。
        /// </summary>
        public static float GetMaxTheoreticalConcentration(Realm realm)
        {
            int radius = GetArrayRadius(realm);
            int slotCount = (radius * 2 + 1) * (radius * 2 + 1) - 1; // 减去阵眼
            // 红品聚灵珠基础值约 200，距离衰减平均约 0.5
            return slotCount * 200f * 0.5f;
        }

        /// <summary>
        /// 获取指定境界的阵盘半径。
        /// </summary>
        public static int GetArrayRadius(Realm realm)
        {
            switch (realm)
            {
                case Realm.Mortal:       return 3;
                case Realm.QiRefining:   return 3;
                case Realm.Foundation:   return 4;
                case Realm.GoldenCore:   return 4;
                case Realm.NascentSoul:  return 5;
                case Realm.SpiritSever:  return 5;
                case Realm.VoidRefining: return 6;
                case Realm.BodyMerge:    return 6;
                case Realm.Mahayana:     return 7;
                case Realm.Tribulation:  return 7;
                default: return 3;
            }
        }

        /// <summary>
        /// 获取距离衰减系数。
        /// </summary>
        public static float GetDistanceFactor(int manhattanDistance)
        {
            if (manhattanDistance < 0) return 0f;
            if (manhattanDistance < DistanceFactors.Length)
                return DistanceFactors[manhattanDistance];
            return 0.15f; // 最低底线
        }

        /// <summary>
        /// 应用境界软上限。
        /// </summary>
        public static float ApplySoftCap(float concentration, Realm realm)
        {
            int index = (int)realm;
            if (index < 0 || index >= SoftCaps.Length) return concentration;

            float cap = SoftCaps[index];
            if (concentration <= cap) return concentration;

            float excess = concentration - cap;
            float decayRate = 0.5f + index * 0.05f; // 凡人0.5, 渡劫0.95
            return cap + excess * decayRate;
        }

        /// <summary>
        /// 获取修炼速度加成（百分比）。
        /// </summary>
        public static float GetCultivationSpeedBonus(float effectiveValue)
        {
            return effectiveValue * 100f; // 0~100%
        }

        /// <summary>
        /// 获取突破成功率加成（百分比）。
        /// </summary>
        public static float GetBreakthroughSuccessBonus(float effectiveValue)
        {
            return effectiveValue * 20f; // 0~20%
        }

        /// <summary>
        /// 获取局内灵力回复加成（百分比）。
        /// </summary>
        public static float GetManaRegenBonus(float effectiveValue)
        {
            return effectiveValue * 50f; // 0~50%
        }

        /// <summary>
        /// 获取炼丹灵气供应（灵气/分钟）。
        /// </summary>
        public static float GetAlchemySpiritSupply(float concentration)
        {
            return concentration * 0.1f;
        }

        // ---------- 私有辅助方法 ----------

        /// <summary>
        /// 获取两个五行元素之间的相生相克倍数。
        /// source → target：source 对 target 的影响倍数。
        /// </summary>
        private static float GetElementMultiplier(ElementType source, ElementType target)
        {
            if (source == target) return 1.02f; // 相同属性共鸣

            // 检查 source 是否生 target
            if (IsGenerates(source, target)) return 1.05f;

            // 检查 source 是否克 target
            if (IsOvercomes(source, target)) return 0.95f;

            return 1.0f; // 无关
        }

        /// <summary>
        /// source 是否生 target。
        /// </summary>
        private static bool IsGenerates(ElementType source, ElementType target)
        {
            for (int i = 0; i < GenerateCycle.Length; i++)
            {
                if (GenerateCycle[i] == source)
                {
                    int next = (i + 1) % GenerateCycle.Length;
                    return GenerateCycle[next] == target;
                }
            }
            return false;
        }

        /// <summary>
        /// source 是否克 target。
        /// </summary>
        private static bool IsOvercomes(ElementType source, ElementType target)
        {
            // 相克链：金克木，木克土，土克水，水克火，火克金
            // 等价于：source 的"生"的"生" = target
            // 或者直接用固定映射
            switch (source)
            {
                case ElementType.Metal: return target == ElementType.Wood;
                case ElementType.Wood:  return target == ElementType.Earth;
                case ElementType.Earth: return target == ElementType.Water;
                case ElementType.Water: return target == ElementType.Fire;
                case ElementType.Fire:  return target == ElementType.Metal;
                default: return false;
            }
        }

        /// <summary>
        /// 获取指定元素"生"出的元素。
        /// </summary>
        private static ElementType? GetGeneratedElement(ElementType element)
        {
            for (int i = 0; i < GenerateCycle.Length; i++)
            {
                if (GenerateCycle[i] == element)
                    return GenerateCycle[(i + 1) % GenerateCycle.Length];
            }
            return null;
        }

        /// <summary>
        /// 获取指定元素"克"的元素。
        /// </summary>
        private static ElementType? GetOvercomeElement(ElementType element)
        {
            switch (element)
            {
                case ElementType.Metal: return ElementType.Wood;
                case ElementType.Wood:  return ElementType.Earth;
                case ElementType.Earth: return ElementType.Water;
                case ElementType.Water: return ElementType.Fire;
                case ElementType.Fire:  return ElementType.Metal;
                default: return null;
            }
        }
    }
}
