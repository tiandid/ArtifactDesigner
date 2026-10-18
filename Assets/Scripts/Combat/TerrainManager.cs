using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;
using ArtifactDesigner.Entity;
using ArtifactDesigner;

namespace ArtifactDesigner.Combat
{
    /// <summary>
    /// 地形管理器（0.0.9）。
    /// 职责：
    ///   1. 统一管理墙 / 护盾的创建、销毁、冷却；
    ///   2. 每来源（单个玩家 / 法宝）各自 ≤3，独立计数独立 CD；
    ///   3. 强 A 收取：创建者主动销毁其地形并进入冷却；
    ///   4. 地形破碎 / 自然消散时自动释放来源计数并开始冷却。
    /// 创建接口由玩家控制器 / 技能系统调用。
    /// </summary>
    public class TerrainManager : MonoBehaviour
    {
        public static TerrainManager Instance { get; private set; }

        [Header("冷却配置（秒）")]
        public float wallCooldown = 8f;
        public float shieldCooldown = 12f;

        // 来源 ID -> 剩余冷却（按 TerrainType 索引）
        private readonly Dictionary<int, float>[] _cooldowns;
        private const int _typeCount = 2;

        public TerrainManager()
        {
            _cooldowns = new Dictionary<int, float>[_typeCount];
            for (int i = 0; i < _typeCount; i++)
                _cooldowns[i] = new Dictionary<int, float>();
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

        private void Update()
        {
            // 递减所有冷却
            for (int i = 0; i < _typeCount; i++)
            {
                var dict = _cooldowns[i];
                var keys = new List<int>(dict.Keys);
                foreach (var key in keys)
                {
                    dict[key] -= Time.deltaTime;
                    if (dict[key] <= 0f)
                        dict.Remove(key);
                }
            }
        }

        // ==================== 创建接口 ====================

        /// <summary>尝试在指定位置创建墙。成功返回实例，失败返回 null。</summary>
        public WallEntity TryCreateWall(Vector3 position, BattleEntity creator, float durability = 100f, float radius = 1.5f)
        {
            if (!CanCreate(creator, TerrainType.Wall)) return null;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Wall_{creator.name}_{creator.activeTerrainCount}";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (radius * 2f);
            var rend = go.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.5f, 0.35f, 0.2f, 0.8f);
                rend.material.SetFloat("_Mode", 3); // transparent-ish placeholder
            }
            Destroy(go.GetComponent<Collider>()); // 碰撞由 CollisionManager 管理

            WallEntity wall = go.AddComponent<WallEntity>();
            wall.faction = creator.faction;
            wall.creator = creator;
            wall.radius = radius;
            wall.maxDurability = durability;
            wall.currentDurability = durability;

            // 订阅破碎事件以便开始冷却
            wall.OnBroken += OnTerrainBroken;

            creator.activeTerrainCount++;
            GameManager.Instance?.RegisterTerrain(wall);
            return wall;
        }

        /// <summary>尝试在指定位置创建护盾。成功返回实例，失败返回 null。</summary>
        public ShieldEntity TryCreateShield(Vector3 position, BattleEntity creator,
            float durability = 80f, float radius = 1.2f, float lifetime = 10f, bool absorbAll = true)
        {
            if (!CanCreate(creator, TerrainType.Shield)) return null;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Shield_{creator.name}_{creator.activeTerrainCount}";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (radius * 2f);
            var rend = go.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.3f, 0.6f, 1f, 0.4f);
                rend.material.SetFloat("_Mode", 3);
            }
            Destroy(go.GetComponent<Collider>());

            ShieldEntity shield = go.AddComponent<ShieldEntity>();
            shield.faction = creator.faction;
            shield.creator = creator;
            shield.radius = radius;
            shield.maxDurability = durability;
            shield.currentDurability = durability;
            shield.maxLifetime = lifetime;
            shield.absorbAll = absorbAll;

            shield.OnBroken += OnTerrainBroken;

            creator.activeTerrainCount++;
            GameManager.Instance?.RegisterTerrain(shield);
            return shield;
        }

        // ==================== 收取（强 A） ====================

        /// <summary>
        /// 强 A 收取：创建者主动销毁其创建的地形，并立即进入冷却。
        /// 规则：A + 点击己方地形 → 破碎进 CD；A + 空地 = 走 A；A + 己方单位无伤害。
        /// </summary>
        public bool CollectTerrain(BattleEntity creator, TerrainEntity terrain)
        {
            if (creator == null || terrain == null) return false;
            if (terrain.creator != creator) return false;
            if (!terrain.IsAlive) return false;

            terrain.Collect(); // Break(autoDispel:true) → OnBroken → 开始冷却
            return true;
        }

        /// <summary>尝试收取创建者指定位置最近的地形。</summary>
        public bool CollectNearestTerrain(BattleEntity creator, Vector3 position, float maxRange = 3f)
        {
            if (creator == null) return false;
            TerrainEntity nearest = FindNearestTerrainByCreator(creator, position, maxRange);
            if (nearest == null) return false;
            return CollectTerrain(creator, nearest);
        }

        // ==================== 查询 ====================

        public bool IsOnCooldown(BattleEntity creator, TerrainType type)
        {
            if (creator == null) return false;
            int id = creator.GetInstanceID();
            int idx = (int)type;
            return _cooldowns[idx].ContainsKey(id) && _cooldowns[idx][id] > 0f;
        }

        public float GetRemainingCooldown(BattleEntity creator, TerrainType type)
        {
            if (creator == null) return 0f;
            int id = creator.GetInstanceID();
            int idx = (int)type;
            if (_cooldowns[idx].TryGetValue(id, out float cd)) return Mathf.Max(0f, cd);
            return 0f;
        }

        public bool CanCreate(BattleEntity creator, TerrainType type)
        {
            if (creator == null) return false;
            if (creator.activeTerrainCount >= BattleEntity.MaxTerrainPerSource) return false;
            return !IsOnCooldown(creator, type);
        }

        // ==================== 内部 ====================

        private void OnTerrainBroken(TerrainEntity terrain)
        {
            if (terrain == null) return;
            if (terrain.creator != null)
            {
                terrain.creator.activeTerrainCount = Mathf.Max(0, terrain.creator.activeTerrainCount - 1);
                StartCooldown(terrain.creator.GetInstanceID(), terrain.TerrainType);
            }
            // 解除订阅防止残留引用
            terrain.OnBroken -= OnTerrainBroken;
        }

        private void StartCooldown(int creatorId, TerrainType type)
        {
            int idx = (int)type;
            float cd = (type == TerrainType.Wall) ? wallCooldown : shieldCooldown;
            _cooldowns[idx][creatorId] = cd;
        }

        private TerrainEntity FindNearestTerrainByCreator(BattleEntity creator, Vector3 position, float maxRange)
        {
            TerrainEntity best = null;
            float bestSqr = maxRange * maxRange;
            var cm = CollisionManager.Instance;
            if (cm == null) return null;
            foreach (var t in cm.FindAllTerrains())
            {
                if (t == null || !t.IsAlive) continue;
                if (t.creator != creator) continue;
                float sqr = (t.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = t;
                }
            }
            return best;
        }
    }
}
