using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Combat;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Entity
{
    /// <summary>
    /// 投射物（0.0.9 升级版）。
    /// 核心机制：
    ///   1. 凝实度（Cohesion）：飞行中参与空中对碰互扣，归零消散；
    ///   2. 命中伤害 = baseDamage × (currentCohesion / totalCohesion)，未拦截=100%；
    ///   3. 单体投射物命中实体后消散回池；
    ///   4. 防永续 = 最远射程 / 持续时间；
    ///   5. 群体技能 = 非投射物，不受本类管理；穿透型暂不实现但保留接口。
    /// 碰撞检测由 CollisionManager 统一在 FixedUpdate 中执行。
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("运动")]
        public float speed = 12f;
        public Vector3 direction = Vector3.forward;
        public float maxRange = 10f;       // 最远射程（神决定）
        public float maxLifetime = 4f;     // 持续时间（防永续）

        [Header("伤害")]
        public int baseDamage = 10;        // 释放者攻击力决定的基础伤害
        public Faction ownerFaction = Faction.Player;
        public BattleEntity source;        // 释放者

        [Header("凝实度")]
        [Tooltip("总凝实度")]
        public float totalCohesion = 100f;
        [Tooltip("当前凝实度（飞行中仅空中对碰会改变）")]
        public float currentCohesion = 100f;

        [Header("类型")]
        public ProjectileType projectileType = ProjectileType.SingleTarget;

        [Header("几何")]
        public float radius = 0.3f;        // 碰撞半径

        // 运行时
        private Vector3 _startPosition;
        private float _aliveTime;
        private bool _isDead;
        private int _hitEntityCount = 0;   // 已命中实体计数（单体=1即消散）

        public bool IsAlive => !_isDead && currentCohesion > 0f && gameObject != null && gameObject.activeSelf;
        public float CohesionPercent => totalCohesion > 0f ? currentCohesion / totalCohesion : 0f;

        // 事件
        public event System.Action<Projectile> OnDestroyed;

        /// <summary>
        /// 初始化投射物。
        /// </summary>
        /// <param name="src">释放者</param>
        /// <param name="dir">飞行方向（已归一化）</param>
        /// <param name="baseDmg">基础伤害</param>
        /// <param name="spd">飞行速度</param>
        /// <param name="cohesion">凝实度（默认100）</param>
        /// <param name="range">最远射程</param>
        /// <param name="lifetime">最大存活时间</param>
        /// <param name="faction">所属阵营</param>
        public void Setup(
            BattleEntity src,
            Vector3 dir,
            int baseDmg,
            float spd,
            float cohesion = 100f,
            float range = 10f,
            float lifetime = 4f,
            Faction? faction = null)
        {
            source = src;
            direction = dir.normalized;
            baseDamage = baseDmg;
            speed = spd;
            totalCohesion = cohesion;
            currentCohesion = cohesion;
            maxRange = range;
            maxLifetime = lifetime;
            ownerFaction = faction ?? (src != null ? src.faction : Faction.Player);
            projectileType = ProjectileType.SingleTarget; // 默认单体

            _startPosition = transform.position;
            _aliveTime = 0f;
            _isDead = false;
            _hitEntityCount = 0;
        }

        /// <summary>重置状态（回池复用前调用）。</summary>
        public void ResetState()
        {
            _aliveTime = 0f;
            _isDead = false;
            _hitEntityCount = 0;
            currentCohesion = totalCohesion;
            direction = Vector3.zero;
            source = null;
            baseDamage = 0;
            speed = 0f;
            maxRange = 0f;
            maxLifetime = 0f;
            ownerFaction = Faction.Neutral;
        }

        private void OnEnable()
        {
            _startPosition = transform.position;
            if (CollisionManager.Instance != null)
                CollisionManager.Instance.Register(this);
            GameEvents.RaiseProjectileSpawned(gameObject);
        }

        private void OnDisable()
        {
            if (CollisionManager.Instance != null)
                CollisionManager.Instance.Unregister(this);
            GameEvents.RaiseProjectileDestroyed(gameObject);
        }

        private void Update()
        {
            if (_isDead || !IsAlive) return;

            _aliveTime += Time.deltaTime;

            // 防永续：超时或超射程
            float traveled = Vector3.Distance(_startPosition, transform.position);
            if (_aliveTime >= maxLifetime || traveled >= maxRange)
            {
                Die();
                return;
            }

            // 直线飞行
            transform.position += direction * (speed * Time.deltaTime);
        }

        // ==================== 命中与死亡 ====================

        /// <summary>命中实体（由 CollisionManager 调用）。单体命中后消散回池。</summary>
        public void HitEntity(BattleEntity target)
        {
            if (_isDead || !IsAlive || target == null || !target.IsAlive) return;

            // 友军不伤
            if (CollisionManager.IsFriendly(ownerFaction, target.faction)) return;

            // 伤害 = baseDamage × (当前凝实度 / 总凝实度)
            float damagePercent = CohesionPercent;
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * damagePercent));

            target.TakeDamage(finalDamage, source);
            _hitEntityCount++;

            // 单体：命中即消散回池
            if (projectileType == ProjectileType.SingleTarget)
            {
                Die();
            }
            // Piercing 暂不实现，若未来启用则继续飞行
        }

        /// <summary>投射物死亡/消散：回池或销毁。</summary>
        public void Die()
        {
            if (_isDead) return;
            _isDead = true;

            OnDestroyed?.Invoke(this);

            if (ProjectilePool.Instance != null)
            {
                ProjectilePool.Instance.Return(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.4f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, radius);
            if (direction.sqrMagnitude > 0.001f)
            {
                Gizmos.DrawLine(transform.position, transform.position + direction * 2f);
            }
        }
    }
}
