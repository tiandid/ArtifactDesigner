using UnityEngine;
using ArtifactDesigner.Core;
using ArtifactDesigner.Entity;

namespace ArtifactDesigner.AI
{
    /// <summary>
    /// 简单寻路（0.0.9 升级版）：
    /// - 基础直线移动；
    /// - 检测前方敌方地形（墙/护盾）并做简单绕行（绕路方向交替左右）；
    /// - 完整 A* / NavMesh 在后续版本接入。
    /// </summary>
    public static class SimplePathfinder
    {
        /// <summary>
        /// 求从 from 到 to 的水平移动方向（单位向量）。
        /// 距离过近返回零向量。
        /// </summary>
        public static Vector3 GetDirection(Vector3 from, Vector3 to)
        {
            Vector3 diff = to - from;
            diff.y = 0f;
            if (diff.sqrMagnitude < 0.0001f) return Vector3.zero;
            return diff.normalized;
        }

        /// <summary>两点水平距离。</summary>
        public static float DistanceXZ(Vector3 a, Vector3 b) => Vector3.Distance(a, b);

        /// <summary>
        /// 0.0.9 避障：检测前方是否有敌方地形，若有则返回绕行方向。
        /// 参数 moverFaction 用于区分"敌方地形"（只有异阵营地形才需要绕行）。
        /// </summary>
        public static Vector3 AvoidObstacles(Vector3 current, Vector3 desired, Faction moverFaction, float moverRadius = 0.8f)
        {
            if (desired.sqrMagnitude < 0.0001f) return desired;

            var cm = CollisionManager.Instance;
            if (cm == null) return desired;

            // 向前方投射短射线检测地形
            float checkDistance = moverRadius + 1.5f; // 略提前检测
            Vector3 checkPos = current + desired * checkDistance;

            // 寻找最近的阻挡地形
            TerrainEntity nearest = null;
            float nearestSqr = float.MaxValue;
            foreach (var t in cm.FindAllTerrains())
            {
                if (t == null || !t.IsAlive) continue;
                if (t.faction == moverFaction) continue; // 同阵营地形不绕行（双向阻挡，但不作为"敌方障碍"）
                float rSum = t.radius + moverRadius + 0.3f;
                float sqr = (t.transform.position - checkPos).sqrMagnitude;
                if (sqr < rSum * rSum && sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = t;
                }
            }

            if (nearest == null) return desired;

            // 计算绕行方向（地形切线方向）
            Vector3 toTerrain = nearest.transform.position - current;
            toTerrain.y = 0f;
            Vector3 perp = new Vector3(-toTerrain.z, 0f, toTerrain.x).normalized;

            // 选择让目标更近的那个切线方向（点积判断）
            Vector3 toTarget = desired; // 已经归一化
            float dotA = Vector3.Dot(perp, toTarget);
            float dotB = Vector3.Dot(-perp, toTarget);
            Vector3 avoidDir = (dotA >= dotB) ? perp : -perp;

            // 混合：70% 原方向 + 30% 避让，使运动更平滑
            return (desired * 0.7f + avoidDir * 0.5f).normalized;
        }

        /// <summary>
        /// 获取从当前位置到目标的粗略路径上是否存在地形阻挡（用于AI决策击碎/绕路）。
        /// </summary>
        public static bool HasTerrainOnPath(Vector3 from, Vector3 to, Faction moverFaction, float moverRadius, out TerrainEntity blocker)
        {
            blocker = null;
            var cm = CollisionManager.Instance;
            if (cm == null) return false;

            Vector3 dir = to - from;
            float len = dir.magnitude;
            if (len < 0.001f) return false;
            dir /= len;

            // 步进检测
            const float step = 1.0f;
            float traveled = 0f;
            while (traveled < len)
            {
                Vector3 checkPos = from + dir * traveled;
                foreach (var t in cm.FindAllTerrains())
                {
                    if (t == null || !t.IsAlive) continue;
                    if (t.faction == moverFaction) continue;
                    float rSum = t.radius + moverRadius + 0.5f;
                    if ((t.transform.position - checkPos).sqrMagnitude <= rSum * rSum)
                    {
                        blocker = t;
                        return true;
                    }
                }
                traveled += step;
            }
            return false;
        }
    }
}
