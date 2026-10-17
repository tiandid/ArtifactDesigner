using System;
using System.Collections.Generic;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Combat
{
    /// <summary>
    /// 掉落条目：某种部件类型以独立概率掉落，每次掉落 minCount~maxCount 件。
    /// chance 取值范围 0~1；掉落部件的稀有度由 rarity 决定。
    /// </summary>
    [Serializable]
    public class DropEntry
    {
        public PartType partType;               // 部件类型（对应七类部件）
        public Rarity rarity;                   // 掉落稀有度
        public float chance;                    // 独立掉落概率（0~1）
        public int minCount;                    // 单次最少掉落件数
        public int maxCount;                    // 单次最多掉落件数

        public DropEntry() { }

        public DropEntry(PartType type, Rarity rarity, float chance, int min, int max)
        {
            partType = type;
            this.rarity = rarity;
            this.chance = chance;
            minCount = min;
            maxCount = max;
        }
    }

    /// <summary>
    /// 击杀掉落表：一组掉落条目，击杀敌人后逐条独立判定。
    /// </summary>
    [Serializable]
    public class DropTable
    {
        public string tableId;      // 掉落表 ID（可多敌人共用）
        public List<DropEntry> entries = new List<DropEntry>();
    }
}