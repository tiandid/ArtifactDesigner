using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;
using ArtifactDesigner.Entity;

namespace ArtifactDesigner.Combat
{
    /// <summary>
    /// 群体技能效果（0.0.9）。
    /// 非投射物，不受凝实度 / 空中对碰 / 碰撞消散影响。
    /// 在固定范围内按固定 tick 间隔结算伤害 / 治疗 / 增益。
    /// 穿透型技能仍通过 Projectile 保留接口，暂不实现。
    /// </summary>
    public class AreaEffect : MonoBehaviour
    {
        [Header("范围与时长")]
        public float radius = 3f;
        public float duration = 5f;
        public float tickInterval = 1f;

        [Header("效果")]
        public int baseDamage = 10;
        public bool isHealing = false;   // true = 治疗友军；false = 伤害敌军
        public bool affectsCrystal = true; // 是否影响水晶（敌方 AOE 可伤水晶，友方 AOE 可治疗水晶）

        [Header("阵营")]
        public Faction ownerFaction = Faction.Player;
        public BattleEntity source;

        [Header("视觉占位")]
        public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);

        // 运行时
        private float _aliveTime;
        private float _tickTimer;
        private bool _isDead;
        private bool _firstTickImmediate = true; // 第 0 秒立即生效一次

        public bool IsAlive => !_isDead && gameObject != null && gameObject.activeSelf;

        public void Setup(BattleEntity src, float rad, int dmg, float dur, float tick,
            bool healing = false, Faction? faction = null, bool affectCrystal = true)
        {
            source = src;
            radius = rad;
            baseDamage = dmg;
            duration = dur;
            tickInterval = tick;
            isHealing = healing;
            affectsCrystal = affectCrystal;
            ownerFaction = faction ?? (src != null ? src.faction : Faction.Player);
            _aliveTime = 0f;
            _tickTimer = 0f;
            _isDead = false;
            _firstTickImmediate = true;
        }

        private void Update()
        {
            if (_isDead || !IsAlive) return;

            _aliveTime += Time.deltaTime;

            if (_aliveTime >= duration)
            {
                Die();
                return;
            }

            _tickTimer += Time.deltaTime;
            if (_firstTickImmediate || _tickTimer >= tickInterval)
            {
                if (_firstTickImmediate) _firstTickImmediate = false;
                else _tickTimer -= tickInterval;
                ApplyEffect();
            }
        }

        /// <summary>
        /// 对范围内目标生效。
        /// 友方 AOE：治疗同阵营实体 + 水晶（若 affectsCrystal）。
        /// 敌方 AOE：伤害异阵营实体 + 水晶（若 affectsCrystal）。
        /// </summary>
        private void ApplyEffect()
        {
            var targets = CollisionManager.Instance?.GetEntitiesInRadius(transform.position, radius);
            if (targets == null) return;

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive) continue;

                bool isCrystal = target is CrystalEntity;
                if (isCrystal && !affectsCrystal) continue;

                bool sameFaction = CollisionManager.IsFriendly(ownerFaction, target.faction);

                if (isHealing)
                {
                    if (sameFaction)
                    {
                        target.Heal(baseDamage);
                    }
                }
                else
                {
                    if (!sameFaction)
                    {
                        // 群体技能不经过凝实度，直接按伤害公式结算
                        int dmg = DamageCalculator.Calculate(baseDamage, target.defense);
                        target.TakeDamage(dmg, source);
                    }
                }
            }
        }

        public void Die()
        {
            if (_isDead) return;
            _isDead = true;
            Destroy(gameObject);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
