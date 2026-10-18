using System;
using UnityEngine;
using ArtifactDesigner.Combat;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Entity
{
    /// <summary>
    /// 局内战斗实体基类（玩家/敌人/水晶共用）。
    /// 持有运行时属性（HP/攻/防/攻速/移速/攻击范围），
    /// 提供受击、死亡、治疗接口；阵营差异由子类与 AI 决定不在此判断。
    /// </summary>
    public abstract class BattleEntity : MonoBehaviour
    {
        [Header("基础配置")]
        public Faction faction = Faction.Neutral;
        [Tooltip("碰撞/攻击半径（圆-圆判定用）")]
        public float radius = 1f;

        [Header("实时属性（由 Init 或 InitFromConfig 注入）")]
        public int maxHp = 100;
        public int currentHp = 100;
        public int maxMana = 0;       // 0.0.3 新增：蓝量上限（玩家特有）
        public int currentMana = 0;    // 0.0.3 新增：当前蓝量
        public int attack = 10;
        public int defense = 10;
        [Tooltip("次/秒")]
        public float attackSpeed = 2f;
        public float moveSpeed = 3.5f;
        [Tooltip("攻击距离（投射物生成或近战判定用）")]
        public float attackRange = 3f;

        /// <summary>距离下次普攻/技能可出手的最早时间。</summary>
        public float nextAttackTime;

        [Header("0.0.9 碰撞与地形")]
        [Tooltip("是否为魂体（可重叠站位，不参与推挤与碰撞伤害）")]
        public bool isSoulBody = false;

        [Tooltip("当前由该实体创建并存活的地形数量（墙/护盾等），上限3")]
        public int activeTerrainCount = 0;

        public const int MaxTerrainPerSource = 3;

        public bool IsAlive => currentHp > 0;

        /// <summary>实体被攻击事件（amount 为结算后实际伤害）。</summary>
        public event Action<BattleEntity, int> OnDamaged;
        /// <summary>实体死亡事件。</summary>
        public event Action<BattleEntity> OnDied;

        public void Init(int hp, int atk, int def, float atkSpeed, float moveSpd, float range, int mana = 0)
        {
            maxHp = Mathf.Max(1, hp);
            currentHp = maxHp;
            maxMana = Mathf.Max(0, mana);
            currentMana = maxMana;
            attack = Mathf.Max(0, atk);
            defense = Mathf.Max(0, def);
            attackSpeed = Mathf.Max(0.01f, atkSpeed);
            moveSpeed = Mathf.Max(0f, moveSpd);
            attackRange = Mathf.Max(0.1f, range);
            nextAttackTime = 0f;
        }

        public virtual void InitFromConfig(BattleEntityConfig config)
        {
            if (config == null) return;
            faction = config.faction;
            radius = config.radius;
            Init(config.maxHp, config.attack, config.defense, config.attackSpeed,
                config.moveSpeed, config.attackRange);
        }

        /// <summary>是否可以出手攻击（冷却就绪）。</summary>
        public bool CanAttack => IsAlive && Time.time >= nextAttackTime;

        /// <summary>记录一次出手，重置冷却。派生类在真正造成伤害前调用。</summary>
        public void ConsumeAttackCooldown()
        {
            nextAttackTime = Time.time + FormulaHelper.AttackInterval(attackSpeed);
        }

        /// <summary>承受伤害。自动广播伤害事件并在归零时触发死亡。</summary>
        public virtual bool TakeDamage(int amount, BattleEntity source)
        {
            if (!IsAlive || amount <= 0) return false;
            currentHp = Mathf.Max(0, currentHp - amount);
            OnDamaged?.Invoke(this, amount);
            GameEvents.RaiseEntityDamaged(this, amount, source);
            if (currentHp == 0)
            {
                Die();
            }
            return true;
        }

        /// <summary>治疗（不超过上限）。</summary>
        public virtual void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
        }

        /// <summary>死亡：广播事件。派生类重写时先调用 base 再处理掉落/解绑。</summary>
        public virtual void Die()
        {
            if (!IsAlive) return;
            currentHp = 0;
            OnDied?.Invoke(this);
            GameEvents.RaiseEntityDied(this);
        }
    }

    /// <summary>实体配置（常量表 / 波次配置均可用）。</summary>
    [Serializable]
    public class BattleEntityConfig
    {
        public Faction faction = Faction.Neutral;
        public float radius = 1f;
        public int maxHp = 100;
        public int attack = 10;
        public int defense = 10;
        public float attackSpeed = 2f;
        public float moveSpeed = 3.5f;
        public float attackRange = 3f;
    }
}