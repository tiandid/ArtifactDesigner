using System;
using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Array
{
    /// <summary>
    /// 灵物类型（可摆放在聚灵阵阵盘上的物品）。
    /// </summary>
    public enum SpiritItemType
    {
        SpiritStone,      // 灵石
        GatheringPearl,   // 聚灵珠
        ArrayFlag,        // 阵旗
        ElementMaterial   // 五行材料
    }

    /// <summary>
    /// 灵物定义：属性、基础聚灵值计算、克隆。
    /// </summary>
    public class SpiritItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public SpiritItemType Type { get; set; }
        public ElementType Element { get; set; }
        public int Quality { get; set; }        // 灵石 1-9，五行材料 1-6
        public bool IsPositive { get; set; }    // 五行材料正负极性
        public int Quantity { get; set; } = 1;
        public int MaxStack { get; set; } = 99;

        // 灵石基础聚灵值（下标 = 品质）
        private static readonly int[] SpiritStoneValues = { 0, 2, 5, 10, 18, 30, 50, 80, 120, 200 };

        public float GetBaseValue()
        {
            switch (Type)
            {
                case SpiritItemType.SpiritStone:
                    return (Quality >= 1 && Quality <= 9) ? SpiritStoneValues[Quality] : 0;
                case SpiritItemType.GatheringPearl:
                    return (Quality >= 1 && Quality <= 9) ? SpiritStoneValues[Quality] * 2 : 0;
                case SpiritItemType.ArrayFlag:
                    return (Quality >= 1 && Quality <= 9) ? (int)(SpiritStoneValues[Quality] * 1.5f) : 0;
                case SpiritItemType.ElementMaterial:
                    int q = Mathf.Clamp(Quality, 1, 6);
                    return IsPositive ? q : -q;
                default:
                    return 0;
            }
        }

        public SpiritItem Clone()
        {
            return new SpiritItem
            {
                Id = Id,
                Name = Name,
                Type = Type,
                Element = Element,
                Quality = Quality,
                IsPositive = IsPositive,
                Quantity = Quantity,
                MaxStack = MaxStack
            };
        }

        // ========== 静态工厂 ==========

        public static SpiritItem CreateSpiritStone(int quality, int baseValue = 0)
        {
            quality = Mathf.Clamp(quality, 1, 9);
            if (baseValue <= 0) baseValue = SpiritStoneValues[quality];
            return new SpiritItem
            {
                Id = $"stone_{quality}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Name = $"{GetQualityName(quality)}灵石",
                Type = SpiritItemType.SpiritStone,
                Element = ElementType.Metal, // 灵石初始无属性，可注入
                Quality = quality,
                IsPositive = true,
                Quantity = 1,
                MaxStack = 99
            };
        }

        public static SpiritItem CreateGatheringPearl(int quality, ElementType element)
        {
            quality = Mathf.Clamp(quality, 1, 9);
            return new SpiritItem
            {
                Id = $"pearl_{element}_{quality}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Name = $"{GetElementName(element)}{GetQualityName(quality)}聚灵珠",
                Type = SpiritItemType.GatheringPearl,
                Element = element,
                Quality = quality,
                IsPositive = true,
                Quantity = 1,
                MaxStack = 99
            };
        }

        public static SpiritItem CreateArrayFlag(int quality, ElementType element)
        {
            quality = Mathf.Clamp(quality, 1, 9);
            return new SpiritItem
            {
                Id = $"flag_{element}_{quality}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Name = $"{GetElementName(element)}{GetQualityName(quality)}阵旗",
                Type = SpiritItemType.ArrayFlag,
                Element = element,
                Quality = quality,
                IsPositive = true,
                Quantity = 1,
                MaxStack = 50
            };
        }

        public static SpiritItem CreateElementMaterial(int quality, ElementType element, bool isPositive)
        {
            quality = Mathf.Clamp(quality, 1, 6);
            return new SpiritItem
            {
                Id = $"mat_{element}_{quality}_{(isPositive ? "pos" : "neg")}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Name = $"{GetElementName(element)}{GetQualityName(quality)}材料",
                Type = SpiritItemType.ElementMaterial,
                Element = element,
                Quality = quality,
                IsPositive = isPositive,
                Quantity = 1,
                MaxStack = 99
            };
        }

        private static string GetQualityName(int q)
        {
            return q switch
            {
                1 => "下品",
                2 => "中品",
                3 => "上品",
                4 => "极品",
                5 => "地品",
                6 => "天品",
                7 => "仙品",
                8 => "神品",
                9 => "圣品",
                _ => "未知"
            };
        }

        private static string GetElementName(ElementType element)
        {
            return element switch
            {
                ElementType.Metal => "金",
                ElementType.Wood => "木",
                ElementType.Water => "水",
                ElementType.Fire => "火",
                ElementType.Earth => "土",
                _ => ""
            };
        }
    }
}
