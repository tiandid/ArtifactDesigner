using System;

namespace ArtifactDesigner.Energy
{
    /// <summary>能量节点类型。</summary>
    public enum EnergyNodeType
    {
        SpiritCore,   // 灵核节点（供能来源，原能量核心）
        Consumer  // 能量消耗者（技能部件）
    }

    /// <summary>
    /// 能量节点。一个节点对应器坯上的一个能量相关部件：
    /// 能量核心部件 → Core 节点，技能部件 → Consumer 节点。
    /// </summary>
    [Serializable]
    public class EnergyNode
    {
        public string nodeId;      // 节点 ID（建议用部件实例 ID 或唯一键）
        public EnergyNodeType nodeType;
        public int energyCapacity; // Core: 可输出能量；Consumer: 需要的能量（每次/维持）
        public string sourcePartId;// 来源部件的 partId（用于追溯）
        public bool isActive;      // 部件是否已挂载且未被损坏

        public EnergyNode() { }

        public EnergyNode(string id, EnergyNodeType type, int capacity, string sourcePartId)
        {
            nodeId = id;
            nodeType = type;
            energyCapacity = capacity;
            this.sourcePartId = sourcePartId;
            isActive = true;
        }
    }
}