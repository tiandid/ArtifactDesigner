using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Entity
{
    /// <summary>
    /// 我方水晶（防守目标）。血量归零 → 关卡失败。
    /// 0.0.1 仅提供血量/受击/胜负广播，第 10 波 Boss 与掉落占用水晶血量结算后续接入。
    /// </summary>
    public class CrystalEntity : BattleEntity
    {
        [Header("水晶配置")]
        public int crystalMaxHp = 300;

        private void Start()
        {
            if (maxHp == 100 && currentHp == 100)
            {
                // 首次启动时按配置初始化（默认从 BattleManager 注入亦可）
                Init(crystalMaxHp, attack: 0, def: 0, atkSpeed: 0f, moveSpd: 0f, range: 0.1f);
            }
            faction = Faction.Crystal;
            radius = 1.5f;
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

        public override bool TakeDamage(int amount, BattleEntity source)
        {
            bool took = base.TakeDamage(amount, source);
            if (took)
            {
                GameEvents.OnCrystalDamaged?.Invoke(this, amount);
            }
            return took;
        }

        public override void Die()
        {
            base.Die();
            GameEvents.OnCrystalDestroyed?.Invoke(this);
        }
    }
}