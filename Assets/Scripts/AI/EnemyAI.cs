using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Combat;
using ArtifactDesigner.Core;
using ArtifactDesigner.Entity;

namespace ArtifactDesigner.AI
{
    /// <summary>
    /// 敌人 AI（0.0.9 升级版状态机）：
    ///   - 行进态：向目标移动（优先玩家，否则水晶）；
    ///   - 攻击态：进入范围后按攻速普攻；
    ///   - 追仇态：追击玩家中（风筝判定计时）；
    ///   - 入魔态：连续10秒未命中→入魔10秒（移速攻速×2、锁水晶、不可拉仇恨）；
    ///   - 击碎态 / 绕路态：遭遇墙/护盾时的决策（击碎耗时<绕路耗时则击碎，否则绕路；
    ///     绕路N秒失败或累计远离水晶超D则转击碎）。
    /// 0.0.6 保留：分身攻击处理（普通分身无伤害，真·分身按等级缩放）。
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        public EnemyEntity owner;
        public float attackRange = 1.8f;   // 物理近战判定范围
        public float notifyRadius = 30f;   // 感知范围

        [Header("0.0.9 状态机")]
        public EnemyAIState State = EnemyAIState.Moving;

        [Header("0.0.9 入魔参数")]
        [Tooltip("追仇态中连续未命中计时（秒），≥10则入魔")]
        public float kitingTimer = 0f;
        public const float KitingThreshold = 10f;

        [Tooltip("入魔剩余持续时间")]
        public float demonizedTimer = 0f;
        public const float DemonizedDuration = 10f;

        [Header("0.0.9 绕路/击碎参数")]
        [Tooltip("绕路最大允许时间（秒）")]
        public float maxDetourTime = 3f;
        [Tooltip("绕路累计远离水晶最大允许距离")]
        public float maxAwayDistance = 4.5f; // 约3倍墙半径

        private BattleEntity _target;
        private bool _combatStarted = false;

        // 绕路/击碎运行时
        private TerrainEntity _blockingTerrain = null;
        private float _detourTimer = 0f;
        private float _accumulatedAwayDistance = 0f;
        private Vector3 _detourDirection = Vector3.zero;
        private Vector3 _lastPosition;
        private float _lastDistanceToCrystal = float.MaxValue;

        private void Update()
        {
            if (owner == null || !owner.IsAlive) return;

            // 1. 状态更新：入魔计时
            if (State == EnemyAIState.Demonized)
            {
                demonizedTimer -= Time.deltaTime;
                if (demonizedTimer <= 0f)
                {
                    ExitDemonized();
                }
                else
                {
                    // 入魔期间：不可拉仇恨，直接锁水晶
                    MoveToCrystal();
                    TryAttackTarget(GameManager.Instance != null ? GameManager.Instance.Crystal : null);
                    return;
                }
            }

            // 2. 正常目标选择
            _target = PickTarget();

            // 3. 首次开战触发分身
            if (!_combatStarted && _target != null)
            {
                _combatStarted = true;
                owner.TrySpawnCloneOnCombatStart();
            }

            // 4. 风筝判定：追仇态计时
            UpdateKitingState();

            // 5. 若正在击碎/绕路地形，优先处理
            if (State == EnemyAIState.DestroyingTerrain || State == EnemyAIState.Detouring)
            {
                HandleTerrainObstacle();
                return;
            }

            // 6. 检测前方是否有阻挡地形
            if (CheckBlockingTerrain(out var terrain))
            {
                _blockingTerrain = terrain;
                DecideTerrainAction(terrain);
                return;
            }

            // 7. 正常移动/攻击
            if (_target == null)
            {
                MoveToCrystal();
                return;
            }

            float dist = Vector3.Distance(transform.position, _target.transform.position);
            if (dist <= attackRange && owner.CanAttack)
            {
                owner.ConsumeAttackCooldown();
                PerformAttack(_target);
            }
            else
            {
                MoveToward(_target.transform.position);
            }
        }

        // ==================== 目标选择 ====================

        private BattleEntity PickTarget()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return gm != null ? gm.Crystal : null;

            // 入魔 / 执念特性：无视玩家锁定水晶
            if (State == EnemyAIState.Demonized ||
                owner.HasTrait(EnemyTrait.Demonized) ||
                owner.HasTrait(EnemyTrait.Obsessed))
            {
                return gm.Crystal;
            }

            if (gm.Player.IsAlive &&
                Vector3.Distance(transform.position, gm.Player.transform.position) <= notifyRadius)
            {
                return gm.Player;
            }
            return gm.Crystal;
        }

        // ==================== 入魔机制 ====================

        private void UpdateKitingState()
        {
            if (_target is PlayerEntity && _target.IsAlive)
            {
                // 处于追仇态：计时递增
                kitingTimer += Time.deltaTime;
                if (kitingTimer >= KitingThreshold && State != EnemyAIState.Demonized)
                {
                    EnterDemonized();
                }
            }
            else if (State == EnemyAIState.Kiting)
            {
                // 丢失目标：重置计时
                kitingTimer = 0f;
            }
        }

        /// <summary>攻击命中时由 PerformAttack 调用，刷新风筝计时。</summary>
        private void RefreshKitingTimer()
        {
            kitingTimer = 0f;
        }

        private void EnterDemonized()
        {
            State = EnemyAIState.Demonized;
            demonizedTimer = DemonizedDuration;
            // 入魔 buff：移速攻速×2，对水晶攻击×2（在 PerformAttack 中对水晶结算时处理）
            Debug.Log($"[Enemy {owner.name}] 入魔！锁水晶10秒");
        }

        private void ExitDemonized()
        {
            State = EnemyAIState.Moving;
            demonizedTimer = 0f;
            kitingTimer = 0f;
        }

        // ==================== 地形阻挡：绕路/击碎决策 ====================

        private bool CheckBlockingTerrain(out TerrainEntity terrain)
        {
            terrain = null;
            var gm = GameManager.Instance;
            if (gm == null || gm.CollisionMgr == null) return false;

            // 检测行进方向前方是否有敌方地形
            Vector3 dest = (_target != null && _target.IsAlive)
                ? _target.transform.position
                : (gm.Crystal != null ? gm.Crystal.transform.position : transform.position);

            Vector3 dir = dest - transform.position;
            if (dir.sqrMagnitude < 0.001f) return false;

            // 前方1.5倍半径处检测
            Vector3 checkPos = transform.position + dir.normalized * (owner.radius + 0.5f);
            terrain = gm.CollisionMgr.FindBlockingTerrain(checkPos, owner.radius, owner.faction);
            return terrain != null;
        }

        private void DecideTerrainAction(TerrainEntity terrain)
        {
            if (terrain == null || !terrain.IsAlive)
            {
                State = EnemyAIState.Moving;
                return;
            }

            // 估算击碎耗时 vs 绕路耗时
            float dps = owner.attack * owner.attackSpeed; // 每秒伤害（粗略估算，含防御减免）
            if (dps < 1f) dps = 1f;
            float timeToBreak = terrain.currentDurability / dps;

            // 绕路耗时：取墙体直径+余量 除以 移速
            float detourLength = terrain.radius * 2f + owner.radius * 2f + 1f;
            float timeToDetour = detourLength / Mathf.Max(0.01f, owner.moveSpeed);

            if (timeToBreak < timeToDetour)
            {
                State = EnemyAIState.DestroyingTerrain;
                _detourTimer = 0f;
                _accumulatedAwayDistance = 0f;
            }
            else
            {
                State = EnemyAIState.Detouring;
            _detourTimer = 0f;
            _accumulatedAwayDistance = 0f;
            // 选一个垂直于朝向的绕路方向
            Vector3 toDest = (_target != null ? _target.transform.position : (GameManager.Instance != null && GameManager.Instance.Crystal != null ? GameManager.Instance.Crystal.transform.position : transform.position)) - transform.position;
            toDest.y = 0f;
            Vector3 perp = new Vector3(-toDest.z, 0f, toDest.x).normalized;
            // 随机选左或右
            _detourDirection = (Random.value > 0.5f ? perp : -perp);
            _lastPosition = transform.position;
            _lastDistanceToCrystal = (GameManager.Instance != null && GameManager.Instance.Crystal != null)
                ? Vector3.Distance(transform.position, GameManager.Instance.Crystal.transform.position)
                : float.MaxValue;
        }
    }

    private void HandleTerrainObstacle()
        {
            var gm = GameManager.Instance;
            if (_blockingTerrain == null || !_blockingTerrain.IsAlive)
            {
                State = EnemyAIState.Moving;
                _blockingTerrain = null;
                return;
            }

            if (State == EnemyAIState.DestroyingTerrain)
            {
                // 击碎：走到地形旁边并在射程内攻击
                float distToTerrain = Vector3.Distance(transform.position, _blockingTerrain.transform.position);
                float attackReach = attackRange + _blockingTerrain.radius + owner.radius;
                if (distToTerrain > attackReach)
                {
                    MoveToward(_blockingTerrain.transform.position);
                    return;
                }

                if (owner.CanAttack)
                {
                    owner.ConsumeAttackCooldown();
                    // 击碎地形：直接扣耐久（不经过防御）
                    int dmg = DamageCalculator.Calculate(owner.attack, 0); // 地形无防御
                    _blockingTerrain.TakeDamage(dmg, owner);
                    RefreshKitingTimer(); // 命中刷新风筝计时
                }
            }
            else if (State == EnemyAIState.Detouring)
            {
                // 绕路
                _detourTimer += Time.deltaTime;

                // 计算是否累计远离水晶
                if (gm != null && gm.Crystal != null)
                {
                    float distNow = Vector3.Distance(transform.position, gm.Crystal.transform.position);
                    if (distNow > _lastDistanceToCrystal)
                    {
                        _accumulatedAwayDistance += Vector3.Distance(transform.position, _lastPosition);
                    }
                    _lastDistanceToCrystal = distNow;
                }
                _lastPosition = transform.position;

                // 超时或远离超限 → 转击碎
                if (_detourTimer >= maxDetourTime || _accumulatedAwayDistance >= maxAwayDistance)
                {
                    State = EnemyAIState.DestroyingTerrain;
                    return;
                }

                // 沿绕路方向移动，同时向目标偏转
                Vector3 toTarget = (_target != null && _target.IsAlive)
                    ? _target.transform.position
                    : (gm != null && gm.Crystal != null ? gm.Crystal.transform.position : transform.position);
                Vector3 targetDir = SimplePathfinder.GetDirection(transform.position, toTarget);
                Vector3 combined = (_detourDirection * 0.7f + targetDir * 0.3f).normalized;
                transform.position += combined * (owner.moveSpeed * Time.deltaTime);
            }
        }

        // ==================== 移动与攻击 ====================

        private void MoveToCrystal()
        {
            var crystal = GameManager.Instance != null ? GameManager.Instance.Crystal : null;
            if (crystal != null)
            {
                MoveToward(crystal.transform.position);
            }
        }

        /// <summary>执行攻击，处理分身/入魔/特性规则。</summary>
        private void PerformAttack(BattleEntity target)
        {
            // 普通分身：攻击动作存在但无伤害
            if (owner.IsClone && owner.TrueCloneLevel <= 0)
            {
                RefreshKitingTimer();
                return;
            }

            int damage = owner.attack;
            // 真·分身：攻击力按等级缩放
            if (owner.IsClone && owner.TrueCloneLevel > 0)
            {
                damage = Mathf.RoundToInt(owner.attack * (owner.TrueCloneLevel / 10f));
            }

            // 入魔 / 执念：对水晶攻击加倍
            bool isCrystal = target is CrystalEntity;
            if (isCrystal && (State == EnemyAIState.Demonized || owner.HasTrait(EnemyTrait.CrystalFocus)))
            {
                damage *= 2;
            }

            target.TakeDamage(DamageCalculator.Calculate(damage, target.defense), owner);
            RefreshKitingTimer();
        }

        private void TryAttackTarget(BattleEntity target)
        {
            if (target == null || !target.IsAlive || !owner.CanAttack) return;
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= attackRange)
            {
                owner.ConsumeAttackCooldown();
                PerformAttack(target);
            }
        }

        private void MoveToward(Vector3 position)
        {
            Vector3 dir = SimplePathfinder.GetDirection(transform.position, position);
            if (dir == Vector3.zero) return;
            dir = SimplePathfinder.AvoidObstacles(transform.position, dir, owner.faction, owner.radius);

            // 入魔时移速×2
            float speedMult = (State == EnemyAIState.Demonized) ? 2f : 1f;
            transform.position += dir * (owner.moveSpeed * speedMult * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = new Color(1f, 1f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, notifyRadius);

            // 0.0.9：绘制当前状态
            if (State == EnemyAIState.Demonized)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, owner.radius + 0.2f);
            }
            else if (State == EnemyAIState.Detouring)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                Gizmos.DrawRay(transform.position, _detourDirection * 3f);
            }
            else if (State == EnemyAIState.DestroyingTerrain)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                if (_blockingTerrain != null)
                    Gizmos.DrawLine(transform.position, _blockingTerrain.transform.position);
            }
        }
    }
}