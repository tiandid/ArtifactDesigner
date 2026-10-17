using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Entity
{
    /// <summary>
    /// 护盾实体（0.0.9）。
    /// - 耐久制（类似血量，但语义为护盾值）；
    /// - 通常有明确的时长限制；
    /// - 破碎后进入冷却（由创建者维护）。
    /// </summary>
    public class ShieldEntity : TerrainEntity
    {
        public override TerrainType TerrainType => TerrainType.Shield;

        [Header("护盾特效")]
        [Tooltip("是否吸收全部类型伤害（false 则只吸收投射物/物理）")]
        public bool absorbAll = true;

        protected override void Start()
        {
            if (maxLifetime <= 0f) maxLifetime = 10f; // 默认10秒，若未配置
            base.Start();
        }
    }
}
