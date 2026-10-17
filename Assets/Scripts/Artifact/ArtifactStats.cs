using ArtifactDesigner.Core;

namespace ArtifactDesigner.Artifact
{
    /// <summary>
    /// 器坯属性统计工具：把基础属性与所有已挂载部件属性合并为最终属性。
    /// 技能类部件是否生效由能量网络决定，不直接计入攻击/耐久/速度数值。
    /// </summary>
    public static class ArtifactStats
    {
        public static AttributeSet SumStats(ArtifactBlank blank)
        {
            var total = blank.baseAttributes;
            foreach (var p in blank.GetMountedParts())
            {
                total += new AttributeSet
                {
                    attack = p.addAttack,
                    maxDurability = p.addDurability,
                    speed = p.addSpeed,
                    maxEnergy = p.partType == PartType.SpiritCore ? p.spiritOutput : 0
                };
            }
            return total;
        }
    }
}