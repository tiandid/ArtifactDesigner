using System;
using System.Collections.Generic;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Artifact
{
    /// <summary>
    /// 器坯（器胚）数据模型（纯逻辑层）。
    /// 器坯拥有多个格子，部件挂载在格子上。
    /// 规则：格子越多，重量越大。总重量 = 基础重量 + 格子数 * 每格重量 + 部件自身重量。
    /// 器坯的最终属性 = 基础属性 + 所挂载部件属性之和；
    /// 其中技能类部件需要能量线路供电后才生效（见 Energy 模块）。
    /// </summary>
    [Serializable]
    public class ArtifactBlank
    {
        public string blankId;          // 唯一 ID
        public string blankName;        // 名称
        public Rarity rarity;
        public int baseSlots;           // 基础格子数
        public int baseWeight;          // 基础重量
        public int weightPerSlot;       // 每个格子的重量增量（格子越多重量越大）
        public AttributeSet baseAttributes; // 器坯基础属性

        /// <summary>槽位列表，长度与当前总格子数同步；null 表示空位。</summary>
        public List<ArtifactPart> slots = new List<ArtifactPart>();

        public ArtifactBlank() { }

        public ArtifactBlank(string id, string name, int baseSlots, int baseWeight, int weightPerSlot)
        {
            blankId = id;
            blankName = name;
            this.baseSlots = baseSlots;
            this.baseWeight = baseWeight;
            this.weightPerSlot = weightPerSlot;
        }

        /// <summary>当前有效格子数（基础格子 + 增加格子类部件提供的格子）。</summary>
        public int TotalSlots
        {
            get
            {
                int extra = 0;
                foreach (var p in slots)
                    if (p != null && p.partType == PartType.AddSlot)
                        extra += p.addSlots;
                return baseSlots + extra;
            }
        }

        /// <summary>总重量：基础重量 + 格子数*每格重量 + 部件重量。</summary>
        public int TotalWeight
        {
            get
            {
                int partWeight = 0;
                foreach (var p in slots)
                    if (p != null) partWeight += p.weight;
                return baseWeight + TotalSlots * weightPerSlot + partWeight;
            }
        }

        /// <summary>当前已占用的槽位数（非空部件数）。</summary>
        public int UsedSlots
        {
            get
            {
                int used = 0;
                foreach (var p in slots)
                    if (p != null) used++;
                return used;
            }
        }

        /// <summary>当前还有多少空槽位。</summary>
        public int FreeSlots => TotalSlots - UsedSlots;

        /// <summary>判断某个槽位是否可挂载部件（索引合法且槽位为空）。</summary>
        public bool CanMount(int index)
        {
            return index >= 0 && index < slots.Count && slots[index] == null;
        }

        /// <summary>按槽位挂载部件；成功返回 true。</summary>
        public bool MountPart(int index, ArtifactPart part)
        {
            if (part == null || index < 0) return false;

            // 挂载后应有的总格子数（若本部件为 AddSlot，则额外扩展格子）
            int targetTotal = TotalSlots;
            if (part.partType == PartType.AddSlot)
                targetTotal += Math.Max(0, part.addSlots);

            // 将槽位列表扩展到目标长度（用 null 补齐空位）
            while (slots.Count < targetTotal) slots.Add(null);

            if (index >= targetTotal || slots[index] != null) return false;
            slots[index] = part;
            return true;
        }

        /// <summary>将部件挂载到第一个空槽位；无空位返回 false。</summary>
        public bool TryMountPart(ArtifactPart part)
        {
            if (part == null) return false;
            while (slots.Count < TotalSlots) slots.Add(null);
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) return MountPart(i, part);
            }
            return false;
        }

        /// <summary>卸下指定槽位部件，返回被卸下的部件（没有则为 null）。</summary>
        public ArtifactPart UnmountPart(int index)
        {
            if (index < 0 || index >= slots.Count) return null;
            var p = slots[index];
            slots[index] = null;
            return p;
        }

        /// <summary>获取某个槽位上的部件。</summary>
        public ArtifactPart GetPart(int index)
        {
            if (index < 0 || index >= slots.Count) return null;
            return slots[index];
        }

        /// <summary>获取所有已挂载的非空部件。</summary>
        public List<ArtifactPart> GetMountedParts()
        {
            var list = new List<ArtifactPart>();
            foreach (var p in slots)
                if (p != null) list.Add(p);
            return list;
        }
    }
}