using System;
using System.Collections.Generic;
using System.Linq;

namespace ArtifactDesigner.Energy
{
    /// <summary>
    /// 能量网络：管理能量节点与线路，并判定技能部件是否获得供电。
    /// 规则：能量必须通过线路从能量核心连接到技能部件，技能才能生效。
    /// 采用无向图（节点 + 无向线路边）建模。
    /// </summary>
    [Serializable]
    public class EnergyNetwork
    {
        private readonly Dictionary<string, EnergyNode> _nodes = new Dictionary<string, EnergyNode>();
        private readonly List<EnergyLink> _links = new List<EnergyLink>();
        private readonly Dictionary<string, List<string>> _adj = new Dictionary<string, List<string>>();

        /// <summary>全部节点。</summary>
        public IReadOnlyCollection<EnergyNode> Nodes => _nodes.Values;

        /// <summary>全部线路。</summary>
        public IReadOnlyList<EnergyLink> Links => _links;

        public EnergyNode GetNode(string nodeId)
        {
            return _nodes.TryGetValue(nodeId, out var n) ? n : null;
        }

        public bool HasNode(string nodeId) => _nodes.ContainsKey(nodeId);

        /// <summary>注册节点（覆盖同 ID）。</summary>
        public void AddNode(EnergyNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.nodeId)) return;
            if (!_nodes.ContainsKey(node.nodeId))
                _adj[node.nodeId] = new List<string>();
            _nodes[node.nodeId] = node;
        }

        /// <summary>移除节点及其关联线路。</summary>
        public void RemoveNode(string nodeId)
        {
            if (!_nodes.ContainsKey(nodeId)) return;
            _nodes.Remove(nodeId);
            _links.RemoveAll(l => l.Connects(nodeId));
            _adj.Remove(nodeId);
            foreach (var kv in _adj) kv.Value.Remove(nodeId);
        }

        /// <summary>添加线路（两端节点必须已存在；自动去重）。</summary>
        public bool AddLink(EnergyLink link)
        {
            if (link == null) return false;
            if (!_nodes.ContainsKey(link.nodeAId) || !_nodes.ContainsKey(link.nodeBId)) return false;
            if (link.nodeAId == link.nodeBId) return false; // 自环不允许
            if (_links.Any(l => l.Connects(link.nodeAId) && l.Connects(link.nodeBId))) return false;
            _links.Add(link);
            _adj[link.nodeAId].Add(link.nodeBId);
            _adj[link.nodeBId].Add(link.nodeAId);
            return true;
        }

        /// <summary>移除线路。</summary>
        public bool RemoveLink(string linkId)
        {
            var link = _links.FirstOrDefault(l => l.linkId == linkId);
            if (link == null) return false;
            _links.Remove(link);
            _adj[link.nodeAId].Remove(link.nodeBId);
            _adj[link.nodeBId].Remove(link.nodeAId);
            return true;
        }

        /// <summary>移除两个节点之间的线路（自动去重场景下两节点间至多一条）。</summary>
        public bool RemoveLinkBetween(string a, string b)
        {
            var link = _links.FirstOrDefault(l => l.Connects(a) && l.Connects(b));
            return link != null && RemoveLink(link.linkId);
        }

        /// <summary>节点是否可通过线路到达目标节点（BFS）。</summary>
        public bool IsReachable(string fromId, string toId)
        {
            if (fromId == toId) return true;
            if (!_nodes.ContainsKey(fromId) || !_nodes.ContainsKey(toId)) return false;
            var visited = new HashSet<string> { fromId };
            var queue = new Queue<string>();
            queue.Enqueue(fromId);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (!_adj.TryGetValue(cur, out var neighbors)) continue;
                foreach (var nb in neighbors)
                {
                    if (nb == toId) return true;
                    if (visited.Add(nb)) queue.Enqueue(nb);
                }
            }
            return false;
        }

        /// <summary>
        /// 按连通分量计算供电分配。
        /// 规则（能量共享电网模型）：
        ///   分量总供给 = 分量内所有活跃能量核心输出之和；
        ///   分量总需求 = 分量内所有活跃技能部件能量需求之和。
        /// 若 供给 >= 需求，则分量内所有技能部件均获得供电；
        /// 否则分量过载，按节点 ID 字典序优先分配，直到供给耗尽，其余技能部件不供电。
        /// 未与任何能量核心连通的技能部件（分量总供给为 0）一律不供电。
        /// </summary>
        public Dictionary<string, bool> ComputeSupply()
        {
            var result = new Dictionary<string, bool>();
            var visited = new HashSet<string>();

            foreach (var node in _nodes.Values)
            {
                if (!visited.Add(node.nodeId)) continue;

                // 收集当前节点所在连通分量
                var component = new List<string>();
                var queue = new Queue<string>();
                queue.Enqueue(node.nodeId);
                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    component.Add(cur);
                    if (!_adj.TryGetValue(cur, out var nbs)) continue;
                    foreach (var nb in nbs)
                        if (visited.Add(nb)) queue.Enqueue(nb);
                }

                int supply = 0;
                var consumers = new List<EnergyNode>();
                foreach (var id in component)
                {
                    var n = GetNode(id);
                    if (n == null) continue;
                    if (n.nodeType == EnergyNodeType.Core && n.isActive)
                        supply += n.energyCapacity;
                    if (n.nodeType == EnergyNodeType.Consumer && n.isActive)
                        consumers.Add(n);
                }

                if (consumers.Count == 0) continue;

                if (supply >= consumers.Sum(c => c.energyCapacity))
                {
                    // 供给充足：全部供电
                    foreach (var c in consumers) result[c.nodeId] = true;
                }
                else
                {
                    // 过载：按稳定顺序优先分配
                    consumers.Sort((a, b) => string.CompareOrdinal(a.nodeId, b.nodeId));
                    int remaining = supply;
                    foreach (var c in consumers)
                    {
                        if (remaining >= c.energyCapacity)
                        {
                            result[c.nodeId] = true;
                            remaining -= c.energyCapacity;
                        }
                        else
                        {
                            result[c.nodeId] = false;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 判断某个消费节点（技能部件）是否已获得供电。
        /// 判定规则：该技能部件所在连通分量的能量供给足以覆盖其（按分配顺序）需求。
        /// 未接入线路、未连接能量核心或分量过载时返回 false，技能不生效。
        /// </summary>
        public bool IsPowered(string consumerNodeId, out EnergyNode poweredCore)
        {
            poweredCore = null;
            var consumer = GetNode(consumerNodeId);
            if (consumer == null || consumer.nodeType != EnergyNodeType.Consumer || !consumer.isActive)
                return false;

            var supply = ComputeSupply();
            if (!supply.TryGetValue(consumerNodeId, out bool powered) || !powered)
                return false;

            // 返回一个可达核心作为供电来源信息（仅用于展示）
            foreach (var node in _nodes.Values)
            {
                if (node.nodeType != EnergyNodeType.Core || !node.isActive) continue;
                if (IsReachable(node.nodeId, consumerNodeId))
                {
                    poweredCore = node;
                    break;
                }
            }
            return true;
        }

        /// <summary>同 IsPowered 的简化重载。</summary>
        public bool IsPowered(string consumerNodeId)
        {
            return IsPowered(consumerNodeId, out _);
        }

        /// <summary>获取所有已获得供电的消费节点（基于一次供电分配计算）。</summary>
        public List<EnergyNode> GetPoweredConsumers()
        {
            var result = new List<EnergyNode>();
            var supply = ComputeSupply();
            foreach (var node in _nodes.Values)
                if (node.nodeType == EnergyNodeType.Consumer && supply.TryGetValue(node.nodeId, out bool powered) && powered)
                    result.Add(node);
            return result;
        }

        /// <summary>清空网络。</summary>
        public void Clear()
        {
            _nodes.Clear();
            _links.Clear();
            _adj.Clear();
        }
    }
}