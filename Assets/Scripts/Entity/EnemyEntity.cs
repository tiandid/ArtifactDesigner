using UnityEngine;
using ArtifactDesigner.AI;
using ArtifactDesigner.Combat;
using ArtifactDesigner.Core;
using System.Collections.Generic;
using System.Linq;
using ArtifactDesigner;
using ArtifactDesigner.Quest;

namespace ArtifactDesigner.Entity
{
    /// <summary>
    /// 敌人实体（Unity 侧运行实例）。
    /// 数值由设计文档换算：第 1 关小怪 体=5 → HP=60、攻=10、防=10、
    /// 攻速 1.5、移速 2.5。AI 行为在 EnemyAI 组件中驱动。
    /// 0.0.6 新增：分身/真·分身特性系统。
    /// </summary>
    public class EnemyEntity : BattleEntity
    {
        public Enemy enemyModel;      // 纯逻辑模型（掉落表/经验奖励）
        public int waveIndex;         // 所属波次
        public bool isBoss;           // 是否为Boss（0.0.6）
        public EnemyAI ai;

        [Header("特性")]
        public List<EnemyTrait> enemyTraits = new List<EnemyTrait>();

        [Header("分身")]
        [SerializeField] private bool _hasSpawnedClone = false; // 真身：是否已触发分身
        [SerializeField] private bool _isClone = false;         // 是否为分身
        [SerializeField] private int _trueCloneLevel = 0;       // 0=普通分身(0%攻), 1~10=真分身(10%~100%攻)
        public EnemyEntity originEnemy;                          // 真身引用（分身用）

        public bool IsClone => _isClone;
        public int TrueCloneLevel => _trueCloneLevel;

        public static EnemyEntity Spawn(Enemy model, Vector3 position, int wave, bool bossFlag = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = bossFlag ? "Boss_" + (model == null ? "unknown" : model.enemyId)
                                : "Enemy_" + (model == null ? "unknown" : model.enemyId);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = bossFlag ? new Color(0.6f, 0.1f, 0.1f) : new Color(0.85f, 0.3f, 0.3f);
            go.transform.position = position;
            if (bossFlag)
                go.transform.localScale = new Vector3(2f, 2.5f, 2f);

            var unit = go.AddComponent<EnemyEntity>();
            unit.enemyModel = model;
            unit.waveIndex = wave;
            unit.isBoss = bossFlag;
            unit.radius = bossFlag ? 1.4f : 0.8f;

            // 0.0.9：注册到碰撞管理器
            if (CollisionManager.Instance != null)
                CollisionManager.Instance.Register(unit);

            // 0.0.6：优先使用 Enemy 模型中的数值；否则回退到默认值
            int hp = model != null ? model.maxHp : FormulaHelper.EnemyMaxHp(5);
            int atk = model != null ? model.attack : FormulaHelper.BaseAttack(5);
            int def = model != null ? (int)(model.attack * 0.5f) : FormulaHelper.BaseDefense(5); // 防御≈攻击/2
            float atkSpd = model != null ? model.speed : FormulaHelper.EnemyAttackSpeed;
            float moveSpd = bossFlag ? FormulaHelper.EnemyMoveSpeed * 0.8f : FormulaHelper.EnemyMoveSpeed;
            float range = bossFlag ? 2.5f : 1.6f;

            unit.Init(hp, atk, def, atkSpd, moveSpd, range);
            unit.faction = Faction.Enemy;
            unit.ai = go.AddComponent<EnemyAI>();
            unit.ai.owner = unit;

            // 0.0.6：按波次规则分配特性
            unit.ApplyTraitsForWave(wave, bossFlag);

            return unit;
        }

        private void OnEnable()
        {
            GameEvents.RaiseEntitySpawned(this);
            GameEvents.RaiseEnemySpawned(this);
        }

        /// <summary>开战时尝试触发分身（由 EnemyAI 调用）。</summary>
        public void TrySpawnCloneOnCombatStart()
        {
            if (!_isClone && !_hasSpawnedClone && IsAlive &&
                (HasTrait(EnemyTrait.Clone) || HasTrait(EnemyTrait.TrueClone)))
            {
                SpawnClone();
            }
        }

        /// <summary>是否拥有指定特性。</summary>
        public bool HasTrait(EnemyTrait trait)
        {
            return enemyTraits.Contains(trait);
        }

        // ==================== 特性分配 ====================

        /// <summary>根据波次规则分配特性。</summary>
        private void ApplyTraitsForWave(int wave, bool isBoss)
        {
            enemyTraits.Clear();

            // 普通关：小怪+boss 公共1个；Boss额外+1 = 2
            // 逢5关：小怪2个，boss同2不额外
            // 逢10关：小怪2个，boss再+1 = 3
            bool isMultipleOf5 = wave % 5 == 0;
            bool isMultipleOf10 = wave % 10 == 0;

            int traitCount = isBoss
                ? (isMultipleOf10 ? 3 : (isMultipleOf5 ? 2 : 2))
                : (isMultipleOf5 ? 2 : 1);

            List<EnemyTrait> availableTraits = new List<EnemyTrait>
            {
                EnemyTrait.DoubleTi,
                EnemyTrait.DoubleQi,
                EnemyTrait.DoubleShen,
                EnemyTrait.TripleAttackSpeed,
                EnemyTrait.TripleRange,
                EnemyTrait.ElementalImmune,
                EnemyTrait.PhysicalImmune,
                EnemyTrait.SpiritualImmune,
                EnemyTrait.CrystalFocus,
                EnemyTrait.Demonized,
                EnemyTrait.Clone,
                EnemyTrait.TrueClone
            };

            for (int i = 0; i < traitCount && availableTraits.Count > 0; i++)
            {
                int idx = Random.Range(0, availableTraits.Count);
                EnemyTrait trait = availableTraits[idx];

                if (!IsTraitValid(trait)) continue;

                enemyTraits.Add(trait);
                ApplyTraitEffect(trait);

                // 互斥移除
                if (trait == EnemyTrait.PhysicalImmune)
                    availableTraits.Remove(EnemyTrait.SoulBody);
                if (trait == EnemyTrait.Demonized)
                    availableTraits.Remove(EnemyTrait.Obsessed);
                if (trait == EnemyTrait.Clone)
                    availableTraits.Remove(EnemyTrait.TrueClone);
                if (trait == EnemyTrait.TrueClone)
                    availableTraits.Remove(EnemyTrait.Clone);
            }
        }

        private bool IsTraitValid(EnemyTrait trait)
        {
            // 同单位最多1个免伤类
            if (trait == EnemyTrait.ElementalImmune || trait == EnemyTrait.PhysicalImmune ||
                trait == EnemyTrait.SpiritualImmune || trait == EnemyTrait.SoulBody)
            {
                foreach (var t in enemyTraits)
                {
                    if (t == EnemyTrait.ElementalImmune || t == EnemyTrait.PhysicalImmune ||
                        t == EnemyTrait.SpiritualImmune || t == EnemyTrait.SoulBody)
                        return false;
                }
            }
            // 体攻免伤与魂体互斥
            if (trait == EnemyTrait.PhysicalImmune && enemyTraits.Contains(EnemyTrait.SoulBody)) return false;
            // 入魔与执念互斥
            if (trait == EnemyTrait.Demonized && enemyTraits.Contains(EnemyTrait.Obsessed)) return false;
            // 分身与真分身互斥
            if (trait == EnemyTrait.Clone && enemyTraits.Contains(EnemyTrait.TrueClone)) return false;
            if (trait == EnemyTrait.TrueClone && enemyTraits.Contains(EnemyTrait.Clone)) return false;

            return true;
        }

        private void ApplyTraitEffect(EnemyTrait trait)
        {
            switch (trait)
            {
                case EnemyTrait.DoubleTi:
                    maxHp *= 2; currentHp = maxHp;
                    attack *= 2; defense *= 2;
                    break;
                case EnemyTrait.DoubleQi:
                    // 气属性在 BattleEntity 中未单独建模，此处预留接口
                    break;
                case EnemyTrait.DoubleShen:
                    // 神属性在 BattleEntity 中未单独建模，此处预留接口
                    attackRange *= 2f;
                    break;
                case EnemyTrait.TripleAttackSpeed:
                    attackSpeed *= 3f;
                    break;
                case EnemyTrait.TripleRange:
                    moveSpeed *= 3f;
                    attackRange *= 3f;
                    break;
                case EnemyTrait.ElementalImmune:
                    // 属性伤害-50%，预留接口
                    break;
                case EnemyTrait.PhysicalImmune:
                    defense = 999999; // 近似物理免疫
                    break;
                case EnemyTrait.SpiritualImmune:
                    // 精神伤害免疫，预留接口
                    break;
                case EnemyTrait.SoulBody:
                    // 0.0.9：魂体可重叠，不参与推挤与碰撞伤害
                    isSoulBody = true;
                    break;
                case EnemyTrait.CrystalFocus:
                    // 对水晶攻击加倍在攻击时处理
                    break;
                case EnemyTrait.Demonized:
                    moveSpeed *= 2f;
                    attackSpeed *= 2f;
                    break;
                case EnemyTrait.Obsessed:
                    maxHp *= 2; currentHp = maxHp;
                    attack *= 2; defense *= 2;
                    break;
                case EnemyTrait.Clone:
                    _trueCloneLevel = 0;
                    break;
                case EnemyTrait.TrueClone:
                    _trueCloneLevel = Random.Range(1, 11); // 1~10：对应10%~100%攻击力
                    break;
            }
        }

        // ==================== 分身系统 ====================

        /// <summary>
        /// 开战或受击时消耗10%当前生命制造分身。
        /// 分身面板与真身相同，外观一致，有碰撞体积。
        /// 普通分身（Clone）：攻击动作存在但无伤害。
        /// 真·分身（TrueClone Lv1~10）：攻击力 = 本体 × (等级/10)。
        /// </summary>
        private void SpawnClone()
        {
            if (_hasSpawnedClone || !IsAlive || _isClone) return;
            _hasSpawnedClone = true;

            // 消耗10%当前生命
            int hpCost = Mathf.RoundToInt(currentHp * 0.1f);
            currentHp = Mathf.Max(1, currentHp - hpCost);

            // 在侧翼生成，避免与真身重叠
            Vector3 offset = new Vector3(
                Random.value > 0.5f ? 1.5f : -1.5f,
                0f,
                Random.Range(-0.5f, 0.5f));
            Vector3 spawnPos = transform.position + offset;

            GameObject cloneObj = Instantiate(gameObject, spawnPos, transform.rotation);
            EnemyEntity clone = cloneObj.GetComponent<EnemyEntity>();
            if (clone == null)
            {
                Destroy(cloneObj);
                return;
            }

            // 设置分身核心属性
            clone._isClone = true;
            clone.originEnemy = this;
            clone._trueCloneLevel = _trueCloneLevel;
            clone._hasSpawnedClone = true; // 分身不能再生分身
            clone.maxHp = maxHp;
            clone.currentHp = currentHp;   // 同样生命值（扣除后的真身HP）
            clone.attack = attack;
            clone.defense = defense;
            clone.attackSpeed = attackSpeed;
            clone.moveSpeed = moveSpeed;
            clone.attackRange = attackRange;
            clone.radius = radius;
            clone.faction = faction;
            clone.waveIndex = waveIndex;
            clone.isBoss = false; // 分身不是Boss

            // AI同步
            if (clone.ai != null)
            {
                clone.ai.owner = clone;
                if (ai != null)
                {
                    clone.ai.attackRange = ai.attackRange;
                    clone.ai.notifyRadius = ai.notifyRadius;
                }
            }

            // 面板与真身相同
            clone.enemyModel = enemyModel;

            // 移除分身特性防止无限分身
            clone.enemyTraits = new List<EnemyTrait>(enemyTraits);
            clone.enemyTraits.Remove(EnemyTrait.Clone);
            clone.enemyTraits.Remove(EnemyTrait.TrueClone);

            // 注册到游戏管理器
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterEnemy(clone);

            string traitLabel = _trueCloneLevel > 0 ? $"TrueCloneLv{_trueCloneLevel}" : "Clone";
            Debug.Log($"[Enemy {name}] Spawned {traitLabel} (HP={currentHp}) at {spawnPos}");
        }

        // ==================== 受击与死亡 ====================

        public override bool TakeDamage(int amount, BattleEntity source)
        {
            bool result = base.TakeDamage(amount, source);
            if (result && !_isClone && !_hasSpawnedClone && IsAlive &&
                (HasTrait(EnemyTrait.Clone) || HasTrait(EnemyTrait.TrueClone)))
            {
                SpawnClone();
            }
            return result;
        }

        /// <summary>死亡：掉落（调用纯逻辑掉落表）→ 广播事件 → 解绑。</summary>
        public override void Die()
        {
            if (!IsAlive) return;

            // 0.0.9：从碰撞管理器注销
            if (CollisionManager.Instance != null)
                CollisionManager.Instance.Unregister(this);

            base.Die();

            if (_isClone)
            {
                // 分身死亡：不广播击杀事件，不触发击杀计数
                if (GameManager.Instance != null)
                    GameManager.Instance.UnregisterEnemy(this);
                Destroy(gameObject);
                return;
            }

            GameEvents.RaiseEnemyKilled(this);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterEnemy(this);
                GameManager.Instance.NotifyEnemyKilled(this);
            }

            // 0.0.6：Boss死亡时触发Q012最终试炼结算
            if (isBoss && QuestSystem.Instance != null)
            {
                QuestSystem.Instance.OnFinalBossDefeated();
            }

            Destroy(gameObject);
        }
    }
}