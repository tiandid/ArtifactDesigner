using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Entity
{
    /// <summary>
    /// 墙实体（0.0.9）。
    /// - 无防御，只有血量/耐久；
    /// - 自然寿命 20 秒后消散；
    /// - 可主动收取（强A：A+点击己方地形 → 破碎进 CD）。
    /// </summary>
    public class WallEntity : TerrainEntity
    {
        public override TerrainType TerrainType => TerrainType.Wall;

        protected override void Start()
        {
            maxLifetime = 20f;
            base.Start();
        }
    }
}
