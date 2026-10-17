using System;

namespace ArtifactDesigner.Energy
{
    /// <summary>
    /// 能量线路：连接能量核心与技能部件（或两个能量节点之间）的边。
    /// 只有通过线路与能量核心连通的技能部件，其能量需求才能被满足并生效。
    /// </summary>
    [Serializable]
    public class EnergyLink
    {
        public string linkId;   // 线路 ID
        public string nodeAId;  // 端点 A
        public string nodeBId;  // 端点 B

        public EnergyLink() { }

        public EnergyLink(string id, string a, string b)
        {
            linkId = id;
            nodeAId = a;
            nodeBId = b;
        }

        public bool Connects(string id)
        {
            return nodeAId == id || nodeBId == id;
        }

        public string Other(string id)
        {
            if (nodeAId == id) return nodeBId;
            if (nodeBId == id) return nodeAId;
            return null;
        }
    }
}