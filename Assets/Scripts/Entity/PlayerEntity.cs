using UnityEngine;
using ArtifactDesigner.Combat;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Entity
{
    /// <summary>
    /// 玩家实体：负责移动（右击寻路 + WASD 直接位移）、自动普攻（攻速 2.0）、
    /// 死亡复活（0.0.1 采用短暂倒地复活，死亡次数计入评分）。
    /// 具体输入读取在 PlayerController，本类只消耗输入结果。
    /// </summary>
    public class PlayerEntity : BattleEntity
    {
        [Header("移动")]
        public bool moveToTarget;
        public Vector3 moveTarget;
        public float arriveDistance = 0.3f;
        public float respawnDelay = 3f;   // 倒地复活等待秒数（0.0.1 占位）
        private float respawnTimer;

        [Header("死亡统计")]
        public int deathCount;

        /// <summary>当前锁定的攻击目标（null = 自动选择最近敌人）。</summary>
        public BattleEntity lockedTarget;

        /// <summary>场景边界（0.0.1 用盒子约束，避免离开战斗区域）。</summary>
        public Bounds battleBounds = new Bounds(Vector3.zero, new Vector3(60f, 10f, 60f));

        public int PlayerBody => PlayerProfile.Instance != null ? PlayerProfile.Instance.Body : 5;
        public int PlayerQi => PlayerProfile.Instance != null ? PlayerProfile.Instance.Qi : 5;
        public int PlayerHun => PlayerProfile.Instance != null ? PlayerProfile.Instance.Hun : 5;

        public bool IsRespawning => !IsAlive;

        protected virtual void Awake()
        {
            // 0.0.2：优先从 PlayerProfile 读取局外成长后的属性
            var profile = PlayerProfile.Instance;
            int body = profile != null ? profile.Body : 5;
            Init(FormulaHelper.PlayerMaxHp(body),
                 FormulaHelper.BaseAttack(body),
                 FormulaHelper.BaseDefense(body),
                 FormulaHelper.PlayerAttackSpeed,
                 FormulaHelper.PlayerMoveSpeed,
                 attackRange: 3f,
                 mana: profile != null ? FormulaHelper.PlayerMaxMana(profile.Qi) : 0);
            faction = Faction.Player;
            radius = 1f;

            // 订阅等级提升，局内属性随局外升级实时刷新（0.0.2 预留接口）
            PlayerProfile.OnLevelUp += OnLevelUpRefresh;
        }

        private void OnDestroy()
        {
            PlayerProfile.OnLevelUp -= OnLevelUpRefresh;
        }

        private void OnLevelUpRefresh(int newLevel)
        {
            // 局内升级时按新等级刷新属性（保留当前 HP 比例）
            float hpRatio = (float)currentHp / Mathf.Max(1, maxHp);
            int body = PlayerProfile.Instance != null ? PlayerProfile.Instance.Body : 5;
            int qi = PlayerProfile.Instance != null ? PlayerProfile.Instance.Qi : 5;
            int newMaxHp = FormulaHelper.PlayerMaxHp(body);
            int newAtk = FormulaHelper.BaseAttack(body);
            int newDef = FormulaHelper.BaseDefense(body);
            int newMaxMana = FormulaHelper.PlayerMaxMana(qi);
            maxHp = newMaxHp;
            attack = newAtk;
            defense = newDef;
            currentHp = Mathf.Min(maxHp, Mathf.Max(1, Mathf.RoundToInt(maxHp * hpRatio)));
            // 同步蓝量（保留比例）
            float manaRatio = maxMana > 0 ? (float)currentMana / maxMana : 1f;
            maxMana = newMaxMana;
            currentMana = Mathf.Min(maxMana, Mathf.Max(0, Mathf.RoundToInt(maxMana * manaRatio)));
        }

        private void OnEnable()
        {
            GameEvents.OnEntitySpawned?.Invoke(this);
            // 0.0.9：注册到碰撞管理器
            if (CollisionManager.Instance != null)
                CollisionManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (CollisionManager.Instance != null)
                CollisionManager.Instance.Unregister(this);
        }

        private void Update()
        {
            if (!IsAlive)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
                return;
            }

            HandleMovement();
            AutoAttack();
        }

        private void HandleMovement()
        {
            if (moveToTarget)
            {
                Vector3 to = moveTarget - transform.position;
                to.y = 0f;
                if (to.magnitude <= arriveDistance)
                {
                    moveToTarget = false;
                    return;
                }
                transform.position += to.normalized * (moveSpeed * Time.deltaTime);
            }
            ClampToBounds();
        }

        private void ClampToBounds()
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, battleBounds.min.x, battleBounds.max.x);
            p.z = Mathf.Clamp(p.z, battleBounds.min.z, battleBounds.max.z);
            transform.position = p;
        }

        /// <summary>自动普攻：优先锁定目标，否则攻击攻击范围内最近敌人。</summary>
        private void AutoAttack()
        {
            if (!CanAttack) return;

            BattleEntity target = lockedTarget;
            if (target == null || !target.IsAlive)
            {
                target = GameManager.Instance != null
                    ? GameManager.Instance.FindNearestEnemy(transform.position, attackRange)
                    : null;
                lockedTarget = target;
            }

            if (target == null || !target.IsAlive) return;
            if (Vector3.Distance(transform.position, target.transform.position) > attackRange) return;

            ConsumeAttackCooldown();
            target.TakeDamage(DamageCalculator.Calculate(attack, target.defense), this);
        }

        /// <summary>死亡：记录统计并启动复活计时（0.0.1 波内不失败）。0.0.2 同步到 PlayerProfile。</summary>
        public override void Die()
        {
            if (!IsAlive) return;
            deathCount++;
            var profile = PlayerProfile.Instance;
            if (profile != null) profile.TotalDeaths++;
            base.Die();
            respawnTimer = respawnDelay;
            GameEvents.OnPlayerDied?.Invoke(this);
        }

        private void Respawn()
        {
            currentHp = maxHp;
            lockedTarget = null;
            GameEvents.OnPlayerRespawned?.Invoke(this);
        }
    }
}