using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;
using ArtifactDesigner.Entity;

namespace ArtifactDesigner.Combat
{
    /// <summary>
    /// 碰撞与推挤管理器（0.0.9）。
    /// 职责：
    ///   1. 圆-圆碰撞检测（玩家/敌人/法宝/水晶圆柱体、投射物、地形）；
    ///   2. 同阵营实体无伤害有推挤（弹性0.5），魂体可重叠不推挤；
    ///   3. 异阵营实体碰撞对称双伤+推开；
    ///   4. 投射物空中对碰（异阵营1:1互扣凝实度）；
    ///   5. 投射物命中实体/地形结算。
    /// 在 FixedUpdate 中统一执行，避免各 MonoBehaviour 分散处理。
    /// </summary>
    public class CollisionManager : MonoBehaviour
    {
        public static CollisionManager Instance { get; private set; }

        [Header("推挤参数")]
        [Tooltip("同阵营推挤弹性系数")]
        public float pushElasticity = 0.5f;

        [Tooltip("异阵营碰撞推开距离")]
        public float clashPushDistance = 0.3f;

        [Tooltip("投射物-实体命中判定半径补偿（投射物半径很小，放宽判定）")]
        public float projectileHitTolerance = 0.15f;

        // 注册表
        private readonly List<BattleEntity> entities = new List<BattleEntity>();
        private readonly List<Projectile> projectiles = new List<Projectile>();
        private readonly List<TerrainEntity> terrains = new List<TerrainEntity>();

        // 帧级去重：记录本 FixedUpdate 已处理过的投射物碰撞对
        private readonly HashSet<(int, int)> _projProcessed = new HashSet<(int, int)>();

        /// <summary>判断两个阵营是否互为友军（同阵营或玩家/水晶互认友军）。</summary>
        public static bool IsFriendly(Faction a, Faction b)
        {
            if (a == b) return true;
            if ((a == Faction.Player && b == Faction.Crystal) || (a == Faction.Crystal && b == Faction.Player))
                return true;
            return false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ==================== 注册 / 注销 ====================

        public void Register(BattleEntity e)
        {
            if (e != null && !entities.Contains(e)) entities.Add(e);
        }

        public void Unregister(BattleEntity e)
        {
            entities.Remove(e);
        }

        public void Register(Projectile p)
        {
            if (p != null && !projectiles.Contains(p)) projectiles.Add(p);
        }

        public void Unregister(Projectile p)
        {
            projectiles.Remove(p);
        }

        public void Register(TerrainEntity t)
        {
            if (t != null && !terrains.Contains(t)) terrains.Add(t);
        }

        public void Unregister(TerrainEntity t)
        {
            terrains.Remove(t);
        }

        // ==================== 帧更新 ====================

        private void FixedUpdate()
        {
            _projProcessed.Clear();
            CleanupLists();

            // 1. 实体-实体（同阵营推挤 / 异阵营对称双伤+推开 / 魂体跳过）
            ResolveEntityEntityCollisions();

            // 2. 投射物-投射物（空中对碰，异阵营1:1互扣凝实度）
            ResolveProjectileProjectileCollisions();

            // 3. 投射物-实体（命中结算，单体消散回池）
            ResolveProjectileEntityCollisions();

            // 4. 投射物-地形（墙/护盾拦截）
            ResolveProjectileTerrainCollisions();
        }

        // ==================== 实体-实体 ====================

        private void ResolveEntityEntityCollisions()
        {
            int count = entities.Count;
            for (int i = 0; i < count; i++)
            {
                var a = entities[i];
                if (a == null || !a.IsAlive) continue;
                for (int j = i + 1; j < count; j++)
                {
                    var b = entities[j];
                    if (b == null || !b.IsAlive) continue;

                    float rSum = a.radius + b.radius;
                    float sqrDist = (a.transform.position - b.transform.position).sqrMagnitude;
                    if (sqrDist > rSum * rSum) continue;

                    // 魂体可重叠：双方任一为魂体 → 不推挤、不碰撞伤害
                    if (a.isSoulBody || b.isSoulBody) continue;

                    float dist = Mathf.Sqrt(sqrDist);
                    float overlap = Mathf.Max(0f, rSum - dist);

                    if (IsFriendly(a.faction, b.faction))
                    {
                        // 同阵营：无伤害，有推挤（弹性0.5）
                        PushApart(a, b, overlap * pushElasticity);
                    }
                    else
                    {
                        // 异阵营：对称双伤 + 推开
                        int dmgA = DamageCalculator.Calculate(b.attack, a.defense);
                        int dmgB = DamageCalculator.Calculate(a.attack, b.defense);
                        a.TakeDamage(dmgA, b);
                        b.TakeDamage(dmgB, a);
                        PushApart(a, b, clashPushDistance);
                    }
                }
            }
        }

        private void PushApart(BattleEntity a, BattleEntity b, float distance)
        {
            Vector3 dir = (a.transform.position - b.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f).normalized;
            }
            dir.Normalize();
            float half = distance * 0.5f;
            a.transform.position += dir * half;
            b.transform.position -= dir * half;
        }

        // ==================== 投射物-投射物（空中对碰） ====================

        private void ResolveProjectileProjectileCollisions()
        {
            int count = projectiles.Count;
            for (int i = 0; i < count; i++)
            {
                var pA = projectiles[i];
                if (pA == null || !pA.IsAlive) continue;
                for (int j = i + 1; j < count; j++)
                {
                    var pB = projectiles[j];
                    if (pB == null || !pB.IsAlive) continue;

                    // 同阵营不互碰
                    if (pA.ownerFaction == pB.ownerFaction) continue;

                    int idA = pA.GetInstanceID();
                    int idB = pB.GetInstanceID();
                    var pair = idA < idB ? (idA, idB) : (idB, idA);
                    if (_projProcessed.Contains(pair)) continue;

                    float rSum = pA.radius + pB.radius + projectileHitTolerance;
                    float sqrDist = (pA.transform.position - pB.transform.position).sqrMagnitude;
                    if (sqrDist > rSum * rSum) continue;

                    _projProcessed.Add(pair);

                    // 1:1 互扣凝实度
                    float cohesionA = pA.currentCohesion;
                    float cohesionB = pB.currentCohesion;

                    pA.currentCohesion -= cohesionB;
                    pB.currentCohesion -= cohesionA;

                    if (pA.currentCohesion <= 0f) pA.Die();
                    if (pB.currentCohesion <= 0f) pB.Die();
                }
            }
        }

        // ==================== 投射物-实体 ====================

        private void ResolveProjectileEntityCollisions()
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var proj = projectiles[i];
                if (proj == null || !proj.IsAlive) continue;

                for (int j = 0; j < entities.Count; j++)
                {
                    var ent = entities[j];
                    if (ent == null || !ent.IsAlive) continue;
                    if (IsFriendly(proj.ownerFaction, ent.faction)) continue; // 友军无伤

                    float rSum = proj.radius + ent.radius + projectileHitTolerance;
                    float sqrDist = (proj.transform.position - ent.transform.position).sqrMagnitude;
                    if (sqrDist > rSum * rSum) continue;

                    // 命中：伤害 = baseDamage × (当前凝实度 / 总凝实度)
                    // 单体投射物：命中后消散回池
                    proj.HitEntity(ent);
                    break; // 单体一次只命中一个实体
                }
            }
        }

        // ==================== 投射物-地形 ====================

        private void ResolveProjectileTerrainCollisions()
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var proj = projectiles[i];
                if (proj == null || !proj.IsAlive) continue;

                for (int j = 0; j < terrains.Count; j++)
                {
                    var terrain = terrains[j];
                    if (terrain == null || !terrain.IsAlive) continue;

                    float rSum = proj.radius + terrain.radius + projectileHitTolerance;
                    float sqrDist = (proj.transform.position - terrain.transform.position).sqrMagnitude;
                    if (sqrDist > rSum * rSum) continue;

                    // 同阵营：己方投射物撞己方地形 = 拦截消失，无伤害
                    if (IsFriendly(proj.ownerFaction, terrain.faction))
                    {
                        proj.Die();
                        break;
                    }
                    else
                    {
                        // 异阵营：按当前凝实度比例对地形造成伤害，投射物消失
                        float damageRatio = proj.CohesionPercent;
                        float dmg = proj.baseDamage * damageRatio;
                        terrain.TakeDamage(dmg, proj.source);
                        proj.Die();
                        break;
                    }
                }
            }
        }

        // ==================== 工具 ====================

        private void CleanupLists()
        {
            entities.RemoveAll(e => e == null);
            projectiles.RemoveAll(p => p == null);
            terrains.RemoveAll(t => t == null);
        }

        /// <summary>获取指定半径内所有存活实体（供 AreaEffect 等范围查询）。</summary>
        public List<BattleEntity> GetEntitiesInRadius(Vector3 center, float radius)
        {
            CleanupLists();
            var result = new List<BattleEntity>();
            float sqr = radius * radius;
            foreach (var e in entities)
            {
                if (e == null || !e.IsAlive) continue;
                if ((e.transform.position - center).sqrMagnitude <= sqr)
                    result.Add(e);
            }
            return result;
        }

        /// <summary>获取当前所有存活地形的只读遍历（供外部避障查询）。</summary>
        public IEnumerable<TerrainEntity> FindAllTerrains()
        {
            CleanupLists();
            foreach (var t in terrains)
            {
                if (t != null && t.IsAlive) yield return t;
            }
        }

        /// <summary>查询指定位置附近是否有阻挡地形（用于敌方AI绕路）。</summary>
        public TerrainEntity FindBlockingTerrain(Vector3 position, float radius, Faction moverFaction)
        {
            foreach (var t in terrains)
            {
                if (t == null || !t.IsAlive) continue;
                // 只有异阵营地形才阻挡（己方地形双向阻挡但不造成伤害，对敌方来说异阵营地形就是玩家方的）
                if (t.faction == moverFaction) continue;
                float rSum = t.radius + radius;
                if ((t.transform.position - position).sqrMagnitude <= rSum * rSum)
                    return t;
            }
            return null;
        }

        /// <summary>查询从 from 到 to 的直线上是否有敌方地形阻挡（粗略检测）。</summary>
        public bool HasTerrainOnPath(Vector3 from, Vector3 to, float radius, Faction moverFaction, out TerrainEntity blocker)
        {
            blocker = null;
            Vector3 dir = to - from;
            float len = dir.magnitude;
            if (len < 0.001f) return false;
            dir /= len;

            const float step = 1.0f;
            float traveled = 0f;
            while (traveled < len)
            {
                Vector3 checkPos = from + dir * traveled;
                foreach (var t in terrains)
                {
                    if (t == null || !t.IsAlive) continue;
                    if (t.faction == moverFaction) continue;
                    float rSum = t.radius + radius + 0.5f; // 留余量
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

        /// <summary>获取某来源当前存活的地形数量（用于≤3限制）。</summary>
        public int CountTerrainByCreator(BattleEntity creator)
        {
            int count = 0;
            foreach (var t in terrains)
            {
                if (t != null && t.IsAlive && t.creator == creator) count++;
            }
            return count;
        }
    }
}
