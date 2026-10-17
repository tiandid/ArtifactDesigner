using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Array
{
    /// <summary>
    /// 相邻格位的五行关系。
    /// </summary>
    public enum ElementRelation
    {
        Same,     // 相同 → 共鸣 ×1.02
        Generate, // 相生 → 增益 ×1.05
        Overcome, // 相克 → 减益 ×0.95
        Neutral   // 无关 → 无变化
    }

    /// <summary>
    /// 聚灵浓度计算器：纯逻辑，负责距离衰减、相邻加成、阵旗效果、浓度汇总、属性偏向、等效值。
    /// </summary>
    public static class ConcentrationCalculator
    {
        // 相生循环：金 → 水 → 木 → 火 → 土 → 金
        private static readonly ElementType[] GenerateCycle = new[]
        {
            ElementType.Metal, ElementType.Water, ElementType.Wood, ElementType.Fire, ElementType.Earth
        };

        /// <summary>
        /// 距离衰减系数（曼哈顿距离 → 衰减系数）。
        /// </summary>
        public static float GetDistanceFactor(int manhattanDist)
        {
            if (manhattanDist <= 0) return 0f;
            if (manhattanDist == 1) return 1.0f;
            float factor = 1.0f - (manhattanDist - 1) * 0.15f;
            return Mathf.Max(0.15f, factor);
        }

        public static bool IsGenerating(ElementType a, ElementType b)
        {
            for (int i = 0; i < GenerateCycle.Length; i++)
            {
                if (GenerateCycle[i] == a && GenerateCycle[(i + 1) % GenerateCycle.Length] == b)
                    return true;
            }
            return false;
        }

        public static bool IsOvercoming(ElementType a, ElementType b)
        {
            if (a == ElementType.Metal && b == ElementType.Wood) return true;
            if (a == ElementType.Wood && b == ElementType.Earth) return true;
            if (a == ElementType.Earth && b == ElementType.Water) return true;
            if (a == ElementType.Water && b == ElementType.Fire) return true;
            if (a == ElementType.Fire && b == ElementType.Metal) return true;
            return false;
        }

        public static ElementRelation GetRelation(ElementType a, ElementType b)
        {
            if (a == b) return ElementRelation.Same;
            if (IsGenerating(a, b)) return ElementRelation.Generate;
            if (IsOvercoming(a, b)) return ElementRelation.Overcome;
            return ElementRelation.Neutral;
        }

        public static float GetNeighborMultiplier(ElementRelation relation)
        {
            return relation switch
            {
                ElementRelation.Same => 1.02f,
                ElementRelation.Generate => 1.05f,
                ElementRelation.Overcome => 0.95f,
                _ => 1.0f
            };
        }

        // ========== 阵眼总浓度计算（含阵旗效果） ==========

        /// <summary>
        /// 计算阵眼总聚灵浓度与主导属性。
        /// 整合距离衰减 + 相邻加成 + 阵旗 surround8 加成。
        /// </summary>
        public static void Calculate(
            Dictionary<Vector2Int, ArraySlot> slots,
            int currentRadius,
            out float totalConcentration,
            out ElementType dominantElement,
            out bool isChaotic)
        {
            totalConcentration = 0f;
            dominantElement = ElementType.Metal;
            isChaotic = true;

            if (slots == null || slots.Count == 0) return;

            // 第一步：基础贡献（距离衰减）
            var baseValues = new Dictionary<Vector2Int, float>();
            var elements = new Dictionary<Vector2Int, ElementType>();
            var itemTypes = new Dictionary<Vector2Int, SpiritItemType>();

            foreach (var kvp in slots)
            {
                var slot = kvp.Value;
                if (slot == null || !slot.HasItem || slot.IsEye) continue;

                int dist = Mathf.Abs(slot.Position.x) + Mathf.Abs(slot.Position.y);
                float factor = GetDistanceFactor(dist);
                float baseVal = slot.Item.GetBaseValue() * factor;

                baseValues[slot.Position] = baseVal;
                elements[slot.Position] = slot.Item.Element;
                itemTypes[slot.Position] = slot.Item.Type;
                slot.EffectiveValue = baseVal;
            }

            // 第二步：相邻加成（乘法叠加，上下左右）
            var effectiveValues = new Dictionary<Vector2Int, float>(baseValues);
            var dirs = new Vector2Int[]
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };

            foreach (var pos in baseValues.Keys)
            {
                ElementType selfElem = elements[pos];
                foreach (var d in dirs)
                {
                    Vector2Int neighborPos = pos + d;
                    if (!elements.TryGetValue(neighborPos, out var neighborElem)) continue;

                    ElementRelation rel = GetRelation(neighborElem, selfElem);
                    float mult = GetNeighborMultiplier(rel);
                    effectiveValues[pos] *= mult;
                }
            }

            // 第三步：阵旗 surround8 加成
            var flagPositions = new List<Vector2Int>();
            foreach (var pos in baseValues.Keys)
            {
                if (itemTypes.TryGetValue(pos, out var type) && type == SpiritItemType.ArrayFlag)
                    flagPositions.Add(pos);
            }

            var flagBoosted = new Dictionary<Vector2Int, float>(effectiveValues);
            foreach (var flagPos in flagPositions)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        Vector2Int target = flagPos + new Vector2Int(dx, dy);
                        if (flagBoosted.ContainsKey(target))
                            flagBoosted[target] *= 1.05f;
                    }
                }
            }

            // 第四步：汇总浓度与属性偏向
            float[] elementWeights = new float[5];
            float totalAbsWeight = 0f;

            foreach (var pos in flagBoosted.Keys)
            {
                float val = flagBoosted[pos];
                if (slots.TryGetValue(pos, out var slot) && slot != null)
                    slot.EffectiveValue = val;

                totalConcentration += val;

                if (elements.TryGetValue(pos, out var elem))
                {
                    float absVal = Mathf.Abs(val);
                    elementWeights[(int)elem] += absVal;
                    totalAbsWeight += absVal;
                }
            }

            // 主导属性判定：最高占比 ≥ 40%
            if (totalAbsWeight > 0)
            {
                float maxWeight = -1f;
                int maxIdx = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (elementWeights[i] > maxWeight)
                    {
                        maxWeight = elementWeights[i];
                        maxIdx = i;
                    }
                }
                float ratio = maxWeight / totalAbsWeight;
                if (ratio >= 0.40f)
                {
                    dominantElement = (ElementType)maxIdx;
                    isChaotic = false;
                }
                else
                {
                    isChaotic = true;
                }
            }
        }

        // ========== 软上限 ==========

        /// <summary>
        /// 软上限：超过部分按境界衰减。
        /// </summary>
        public static float ApplySoftCap(float concentration, Realm realm)
        {
            float cap = realm switch
            {
                Realm.Mortal => 20f,
                Realm.QiRefining => 50f,
                Realm.Foundation => 100f,
                Realm.GoldenCore => 200f,
                Realm.NascentSoul => 350f,
                Realm.SpiritSever => 550f,
                Realm.VoidRefining => 800f,
                Realm.BodyMerge => 1100f,
                Realm.Mahayana => 1500f,
                Realm.Tribulation => 2000f,
                _ => 2000f
            };

            if (concentration <= cap) return concentration;
            float over = concentration - cap;
            float decay = realm switch
            {
                Realm.Mortal => 0.5f,
                Realm.QiRefining => 0.6f,
                Realm.Foundation => 0.7f,
                Realm.GoldenCore => 0.75f,
                Realm.NascentSoul => 0.8f,
                Realm.SpiritSever => 0.85f,
                Realm.VoidRefining => 0.9f,
                Realm.BodyMerge => 0.95f,
                _ => 1.0f
            };
            return cap + over * decay;
        }

        // ========== 等效值 ==========

        /// <summary>
        /// 线性衰减等效值：ln(浓度+1)/ln(最大理论浓度+1)。
        /// </summary>
        public static float CalcEquivalentValue(float concentration, int radius)
        {
            int slotCount = (radius * 2 + 1) * (radius * 2 + 1) - 1; // 不含阵眼
            float maxTheory = slotCount * 200f; // 红品灵石基础值
            if (maxTheory <= 1f) return 0f;
            float c = Mathf.Max(0f, concentration);
            return Mathf.Min(1.0f, Mathf.Log(c + 1f) / Mathf.Log(maxTheory + 1f));
        }

        // ========== 效果查询 ==========

        public static float GetCultivationSpeedBonus(float equivalentValue) => equivalentValue * 1.0f;
        public static float GetBreakthroughSuccessBonus(float equivalentValue) => equivalentValue * 0.2f;
        public static float GetManaRegenBonus(float equivalentValue) => equivalentValue * 0.5f;
        public static float GetAlchemySupplyPerMinute(float totalConcentration) => totalConcentration * 0.1f;
    }
}
