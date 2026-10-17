using System;
using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Entity
{
    /// <summary>
    /// 地形实体基类（0.0.9）：墙 / 护盾。
    /// 有血量/耐久、碰撞体积、阻挡通过；自然寿命到期自动消散。
    /// 创建者（creator）用于统计"每来源≤3"限制。
    /// </summary>
    public abstract class TerrainEntity : MonoBehaviour
    {
        [Header("阵营")]
        public Faction faction = Faction.Neutral;

        [Header("几何")]
        [Tooltip("碰撞半径（圆-圆判定）")]
        public float radius = 1.5f;

        [Header("耐久")]
        public float maxDurability = 100f;
        public float currentDurability = 100f;

        [Header("寿命")]
        [Tooltip("自然寿命（秒），≤0 表示无自然寿命限制")]
        public float maxLifetime = 20f;
        private float _lifetimeTimer;

        [Header("来源")]
        [Tooltip("创建该地形的实体（用于统计上限3与冷却）")]
        public BattleEntity creator;

        /// <summary>是否存活（耐久>0 且 寿命未到）。</summary>
        public bool IsAlive => currentDurability > 0f && (_lifetimeTimer > 0f || maxLifetime <= 0f);

        /// <summary>地形类型标记（由子类重写）。</summary>
        public abstract TerrainType TerrainType { get; }

        public event Action<TerrainEntity> OnBroken;
        public event Action<TerrainEntity> OnDestroyed;

        protected virtual void Start()
        {
            _lifetimeTimer = maxLifetime;
            GameEvents.RaiseTerrainSpawned(this);
        }

        protected virtual void Update()
        {
            if (maxLifetime > 0f)
            {
                _lifetimeTimer -= Time.deltaTime;
                if (_lifetimeTimer <= 0f)
                {
                    Break(autoDispel: true);
                }
            }
        }

        /// <summary>承受伤害（墙无防御，直接扣耐久）。</summary>
        public virtual void TakeDamage(float amount, BattleEntity source = null)
        {
            if (!IsAlive || amount <= 0f) return;
            currentDurability -= amount;
            if (currentDurability <= 0f)
            {
                Break(autoDispel: false);
            }
        }

        /// <summary>强制破碎/消散。</summary>
        public virtual void Break(bool autoDispel = false)
        {
            if (currentDurability <= 0f && !autoDispel) return; // 已处理过
            currentDurability = 0f;

            OnBroken?.Invoke(this);
            GameEvents.RaiseTerrainDestroyed(this);
            OnDestroyed?.Invoke(this);

            // 释放创建者的地形计数
            if (creator != null)
            {
                creator.activeTerrainCount = Mathf.Max(0, creator.activeTerrainCount - 1);
            }

            Destroy(gameObject);
        }

        /// <summary>主动收取（强A 或 技能回收）。立即破碎并进入冷却。</summary>
        public void Collect()
        {
            Break(autoDispel: true);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
