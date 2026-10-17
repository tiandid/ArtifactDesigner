using System;
using UnityEngine;

namespace ArtifactDesigner.SpiritGathering
{
    /// <summary>
    /// 灵物类型：可摆放在聚灵阵格位上的物品。
    /// </summary>
    public enum SpiritItemType
    {
        SpiritStone,   // 灵石
        SpiritPearl,   // 聚灵珠
        ArrayFlag,     // 阵旗
        ElementMaterial // 五行材料
    }

    /// <summary>
    /// 灵物品级（对应游戏全局 Rarity 的映射）。
    /// </summary>
    public enum SpiritItemGrade
    {
        White = 0,  // 白
        Blue,       // 蓝
        Purple,     // 紫
        Gold,       // 金
        Orange,     // 橙
        Red         // 红
    }

    /// <summary>
    /// 灵物数据定义。
    /// 每种灵物实例化后占用一个阵盘格位。
    /// </summary>
    [Serializable]
    public class SpiritItem
    {
        public string ItemId;
        public string DisplayName;
        public SpiritItemType ItemType;
        public SpiritItemGrade Grade;
        public ElementType? Element; // 五行属性，灵石可为 null（无属性或注入后确定）
        public int BaseSpiritValue;  // 基础聚灵值（可为正、负或零）
        public Sprite Icon;          // UI 图标

        // ---------- 聚灵值计算 ----------

        /// <summary>
        /// 获取该灵物的实际聚灵值（考虑品级系数）。
        /// </summary>
        public int GetActualSpiritValue()
        {
            float gradeMultiplier = GetGradeMultiplier();
            return Mathf.RoundToInt(BaseSpiritValue * gradeMultiplier);
        }

        /// <summary>
        /// 品级对聚灵值的加成系数。
        /// </summary>
        private float GetGradeMultiplier()
        {
            switch (Grade)
            {
                case SpiritItemGrade.White:  return 1.0f;
                case SpiritItemGrade.Blue:   return 1.5f;
                case SpiritItemGrade.Purple: return 2.2f;
                case SpiritItemGrade.Gold:   return 3.0f;
                case SpiritItemGrade.Orange: return 4.0f;
                case SpiritItemGrade.Red:    return 5.5f;
                default: return 1.0f;
            }
        }

        // ---------- 特殊效果判定 ----------

        /// <summary>
        /// 是否为负聚灵值材料（煞气/逆灵气）。
        /// </summary>
        public bool IsNegative => BaseSpiritValue < 0;

        /// <summary>
        /// 是否为聚灵珠（同属性相邻时额外 +10%）。
        /// </summary>
        public bool IsPearl => ItemType == SpiritItemType.SpiritPearl;

        /// <summary>
        /// 是否为阵旗（周围 8 格 +5% 聚灵效率）。
        /// </summary>
        public bool IsFlag => ItemType == SpiritItemType.ArrayFlag;

        // ---------- 静态工厂 ----------

        /// <summary>
        /// 创建灵石（无固定五行，可注入）。
        /// </summary>
        public static SpiritItem CreateSpiritStone(SpiritItemGrade grade, int baseValue)
        {
            return new SpiritItem
            {
                ItemId = $"stone_{grade}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                DisplayName = $"{GetGradeName(grade)}灵石",
                ItemType = SpiritItemType.SpiritStone,
                Grade = grade,
                Element = null,
                BaseSpiritValue = baseValue
            };
        }

        /// <summary>
        /// 创建聚灵珠（固定五行）。
        /// </summary>
        public static SpiritItem CreateSpiritPearl(SpiritItemGrade grade, ElementType element, int baseValue)
        {
            return new SpiritItem
            {
                ItemId = $"pearl_{element}_{grade}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                DisplayName = $"{GetElementName(element)}{GetGradeName(grade)}聚灵珠",
                ItemType = SpiritItemType.SpiritPearl,
                Grade = grade,
                Element = element,
                BaseSpiritValue = baseValue
            };
        }

        /// <summary>
        /// 创建阵旗（固定五行）。
        /// </summary>
        public static SpiritItem CreateArrayFlag(SpiritItemGrade grade, ElementType element, int baseValue)
        {
            return new SpiritItem
            {
                ItemId = $"flag_{element}_{grade}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                DisplayName = $"{GetElementName(element)}{GetGradeName(grade)}阵旗",
                ItemType = SpiritItemType.ArrayFlag,
                Grade = grade,
                Element = element,
                BaseSpiritValue = baseValue
            };
        }

        /// <summary>
        /// 创建五行材料（固定五行，正负值均可）。
        /// </summary>
        public static SpiritItem CreateElementMaterial(SpiritItemGrade grade, ElementType element, int baseValue)
        {
            return new SpiritItem
            {
                ItemId = $"mat_{element}_{grade}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                DisplayName = $"{GetElementName(element)}{GetGradeName(grade)}材料",
                ItemType = SpiritItemType.ElementMaterial,
                Grade = grade,
                Element = element,
                BaseSpiritValue = baseValue
            };
        }

        // ---------- 辅助方法 ----------

        private static string GetGradeName(SpiritItemGrade grade)
        {
            switch (grade)
            {
                case SpiritItemGrade.White:  return "白品";
                case SpiritItemGrade.Blue:   return "蓝品";
                case SpiritItemGrade.Purple: return "紫品";
                case SpiritItemGrade.Gold:   return "金品";
                case SpiritItemGrade.Orange: return "橙品";
                case SpiritItemGrade.Red:    return "红品";
                default: return "";
            }
        }

        private static string GetElementName(ElementType element)
        {
            switch (element)
            {
                case ElementType.Metal: return "金";
                case ElementType.Wood:  return "木";
                case ElementType.Water: return "水";
                case ElementType.Fire:  return "火";
                case ElementType.Earth: return "土";
                default: return "";
            }
        }
    }
}
