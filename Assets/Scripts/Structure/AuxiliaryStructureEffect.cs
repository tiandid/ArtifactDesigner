using System;
using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Structure
{
    /// <summary>
    /// 辅助结构效果系统：管理法宝上所有辅助结构的运行时效果。
    /// 10种辅助效果：投射物数量加成、耐久度倍数、器灵自动攻击、灵气回收、
    /// 护盾发生、暴击增幅、吸血转化、冷却缩减、凝实强化、追踪模块。
    /// </summary>
    public class AuxiliaryStructureEffect
    {
        // ---------- 计数器：记录法宝上各辅助结构的数量 ----------
        private readonly Dictionary<AuxiliaryEffectType, int> _counts = new Dictionary<AuxiliaryEffectType, int>();
        private readonly List<StructureDefinition> _auxiliaries = new List<StructureDefinition>();

        // ---------- 器灵AI状态 ----------
        public bool IsArtifactSpiritActive { get; private set; } = false;
        public float ArtifactSpiritReactionSpeed { get; private set; } = 1.0f; // 1.0=正常
        public bool IsArtifactSpiritOverloaded { get; private set; } = false;

        // ---------- 运行时缓存（每帧刷新） ----------
        public float ShieldTimer { get; set; } = 0f;       // 护盾生成计时器
        public float PhantomTimer { get; set; } = 0f;      // 残影遁法计时器
        public float PhantomPassiveCd { get; set; } = 0f;  // 残影被动触发冷却
        public float ManaLeechTimer { get; set; } = 0f;    // 灵力汲取目标CD追踪（按目标ID）

        // ========== 结构管理 ==========

        public void AddAuxiliary(StructureDefinition def)
        {
            if (def == null || def.Category != StructureCategory.Auxiliary || !def.AuxType.HasValue)
                return;
            _auxiliaries.Add(def);
            var type = def.AuxType.Value;
            _counts[type] = _counts.GetValueOrDefault(type, 0) + 1;
            UpdateArtifactSpiritState();
        }

        public void RemoveAuxiliary(StructureDefinition def)
        {
            if (def == null || !_auxiliaries.Contains(def)) return;
            _auxiliaries.Remove(def);
            if (def.AuxType.HasValue)
            {
                var type = def.AuxType.Value;
                if (_counts.ContainsKey(type) && _counts[type] > 0)
                    _counts[type]--;
            }
            UpdateArtifactSpiritState();
        }

        public void Clear()
        {
            _auxiliaries.Clear();
            _counts.Clear();
            IsArtifactSpiritActive = false;
            ArtifactSpiritReactionSpeed = 1.0f;
            IsArtifactSpiritOverloaded = false;
        }

        public int GetCount(AuxiliaryEffectType type) => _counts.GetValueOrDefault(type, 0);

        // ========== 器灵AI管理 ==========

        /// <summary>
        /// 更新器灵AI状态（调用时机：添加/移除结构、境界变化时）。
        /// </summary>
        private void UpdateArtifactSpiritState()
        {
            int spiritCount = GetCount(AuxiliaryEffectType.ArtifactSpirit);
            IsArtifactSpiritActive = spiritCount > 0;
            if (!IsArtifactSpiritActive)
            {
                ArtifactSpiritReactionSpeed = 1.0f;
                IsArtifactSpiritOverloaded = false;
                return;
            }
            // 器灵状态由外部灵智系统计算后传入，此处仅标记存在性
        }

        /// <summary>
        /// 由外部灵智系统调用，传入当前灵智状态。
        /// </summary>
        public void SetSpiritState(float reactionSpeed, bool overloaded)
        {
            if (!IsArtifactSpiritActive) return;
            ArtifactSpiritReactionSpeed = Mathf.Clamp01(reactionSpeed);
            IsArtifactSpiritOverloaded = overloaded;
            if (overloaded) IsArtifactSpiritActive = false;
        }

        // ========== 效果查询（投射物） ==========

        /// <summary>
        /// 额外投射物数量（N = count/2 向上取整，6个以上递减，上限5个）。
        /// </summary>
        public int GetExtraProjectiles()
        {
            int count = GetCount(AuxiliaryEffectType.MultiShot);
            if (count <= 0) return 0;
            int n = (count + 1) / 2; // count/2 向上取整
            if (count > 5)
            {
                int over = count - 5;
                n += (int)(over * 0.5f); // 第6个起每1个+0.5（向下取整）
            }
            return Mathf.Min(5, n);
        }

        /// <summary>额外投射物伤害倍率（60%）。</summary>
        public float GetExtraProjectileDamageMultiplier() => 0.60f;

        /// <summary>额外投射物扇形夹角。</summary>
        public float GetExtraProjectileSpreadAngle()
        {
            int n = GetExtraProjectiles();
            return 15f * n;
        }

        // ========== 效果查询（耐久） ==========

        /// <summary>
        /// 耐久上限倍率（乘法叠加，每15%）。
        /// </summary>
        public float GetDurabilityMultiplier()
        {
            int count = GetCount(AuxiliaryEffectType.DurableArmor);
            if (count <= 0) return 1.0f;
            return Mathf.Pow(1.15f, count);
        }

        // ========== 效果查询（灵力汲取） ==========

        /// <summary>
        /// 命中回复最大灵力百分比（每1个+1%）。
        /// </summary>
        public float GetManaLeechPercent() => GetCount(AuxiliaryEffectType.ManaLeech) * 0.01f;

        /// <summary>灵力汲取对同一目标冷却（秒）。</summary>
        public float ManaLeechTargetCooldown => 3f;

        // ========== 效果查询（护盾） ==========

        /// <summary>
        /// 护盾触发间隔（秒），最低5秒。
        /// </summary>
        public float GetShieldInterval()
        {
            int count = GetCount(AuxiliaryEffectType.ShieldGenerator);
            if (count <= 0) return float.MaxValue;
            return Mathf.Max(5f, 20f - count);
        }

        /// <summary>护盾吸收量 = 法宝攻击力 × N（N = count × 0.5）。</summary>
        public float GetShieldAbsorbMultiplier(float artifactAttack)
        {
            int count = GetCount(AuxiliaryEffectType.ShieldGenerator);
            if (count <= 0) return 0f;
            return artifactAttack * count * 0.5f;
        }

        /// <summary>护盾存在时免疫控制。</summary>
        public bool HasShieldImmunity => GetCount(AuxiliaryEffectType.ShieldGenerator) > 0;

        // ========== 效果查询（暴击） ==========

        /// <summary>暴击率提升（每3%）。</summary>
        public float GetCritRateBonus()
        {
            int count = GetCount(AuxiliaryEffectType.CriticalSurge);
            return Mathf.Min(0.80f, count * 0.03f); // 上限80%
        }

        /// <summary>暴击伤害加成（基础150% + 每5%）。</summary>
        public float GetCritDamageMultiplier()
        {
            int count = GetCount(AuxiliaryEffectType.CriticalSurge);
            float rateBonus = count * 0.03f;
            float dmgBonus = count * 0.05f;
            if (rateBonus > 0.80f)
            {
                float overflow = rateBonus - 0.80f;
                dmgBonus += overflow * 2f; // 每1%溢出生成2%暴伤
            }
            return 1.50f + dmgBonus;
        }

        // ========== 效果查询（五行共鸣） ==========

        /// <summary>
        /// 同属性结构加成百分比（每2%）。
        /// 注意：此值需要外部传入"同属性结构数量"后计算最终加成。
        /// </summary>
        public float GetResonanceSamePercentPerMatch() => GetCount(AuxiliaryEffectType.ElementalResonance) * 0.02f;

        /// <summary>相生属性结构加成百分比（同属性的一半）。</summary>
        public float GetResonanceGeneratePercentPerMatch() => GetResonanceSamePercentPerMatch() * 0.5f;

        // ========== 效果查询（残影遁法） ==========

        /// <summary>主动瞬移间隔（秒），最低3秒。</summary>
        public float GetPhantomActiveInterval()
        {
            int count = GetCount(AuxiliaryEffectType.PhantomStep);
            if (count <= 0) return float.MaxValue;
            return Mathf.Max(3f, 15f - count);
        }

        /// <summary>瞬移距离（每0.5单位）。</summary>
        public float GetPhantomDistance()
        {
            int count = GetCount(AuxiliaryEffectType.PhantomStep);
            return count * 0.5f;
        }

        /// <summary>被动触发概率（每5%，上限50%）。</summary>
        public float GetPhantomPassiveChance()
        {
            int count = GetCount(AuxiliaryEffectType.PhantomStep);
            return Mathf.Min(0.50f, count * 0.05f);
        }

        /// <summary>被动触发内部冷却。</summary>
        public float PhantomPassiveInternalCooldown => 5f;

        /// <summary>残影持续时间。</summary>
        public float PhantomDuration => 1f;

        // ========== 效果查询（灵气压缩） ==========

        /// <summary>凝实度提升（每8%，上限200%）。</summary>
        public float GetCohesionBonus()
        {
            int count = GetCount(AuxiliaryEffectType.QiCompression);
            float bonus = count * 0.08f;
            if (bonus > 2.0f)
            {
                float overflow = bonus - 2.0f;
                // 溢出转化为飞行速度提升（由外部读取）
                OverflowCohesionSpeedBonus = overflow * 0.5f; // 每10%溢出=+5%速度
                return 2.0f;
            }
            OverflowCohesionSpeedBonus = 0f;
            return bonus;
        }

        /// <summary>凝实度溢出转化的飞行速度加成（百分比）。</summary>
        public float OverflowCohesionSpeedBonus { get; private set; } = 0f;

        /// <summary>与"压缩珠"类技能结构的联动加成（+20%每层）。</summary>
        public float GetCompressionSynergyBonus(int compressionOrbCount)
        {
            int count = GetCount(AuxiliaryEffectType.QiCompression);
            if (count <= 0 || compressionOrbCount <= 0) return 0f;
            return Mathf.Min(1.0f, compressionOrbCount * 0.20f);
        }

        // ========== 效果查询（噬灵转化） ==========

        /// <summary>击杀回复耐久百分比（每5%）。</summary>
        public float GetSoulConversionHealPercent() => GetCount(AuxiliaryEffectType.SoulConversion) * 0.05f;

        /// <summary>击杀精英/首领时额外回复最大生命百分比（每2%）。</summary>
        public float GetSoulConversionMaxHpHealPercent() => GetCount(AuxiliaryEffectType.SoulConversion) * 0.02f;

        // ========== 汇总：获取全部当前生效的效果描述 ==========

        public List<string> GetEffectDescriptions()
        {
            var list = new List<string>();
            foreach (var kvp in _counts)
            {
                if (kvp.Value <= 0) continue;
                string desc = kvp.Key switch
                {
                    AuxiliaryEffectType.MultiShot => $"多重投射 ×{kvp.Value}：额外发射 {GetExtraProjectiles()} 个投射物（60%伤害）",
                    AuxiliaryEffectType.DurableArmor => $"坚韧护甲 ×{kvp.Value}：耐久上限 ×{GetDurabilityMultiplier():F2}",
                    AuxiliaryEffectType.ArtifactSpirit => $"器灵之芯 ×{kvp.Value}：自动攻击 AI（反应速度 {ArtifactSpiritReactionSpeed:P0}）",
                    AuxiliaryEffectType.ManaLeech => $"灵力汲取 ×{kvp.Value}：命中回复 {GetManaLeechPercent():P0} 最大灵力",
                    AuxiliaryEffectType.ShieldGenerator => $"护盾生成 ×{kvp.Value}：每 {GetShieldInterval():F1} 秒生成护盾",
                    AuxiliaryEffectType.CriticalSurge => $"暴击增幅 ×{kvp.Value}：暴击率 +{GetCritRateBonus():P0}，暴伤 ×{GetCritDamageMultiplier():F2}",
                    AuxiliaryEffectType.ElementalResonance => $"五行共鸣 ×{kvp.Value}：同属性 +{GetResonanceSamePercentPerMatch():P0}，相生 +{GetResonanceGeneratePercentPerMatch():P0}",
                    AuxiliaryEffectType.PhantomStep => $"残影遁法 ×{kvp.Value}：每 {GetPhantomActiveInterval():F1} 秒瞬移，被动闪避 {GetPhantomPassiveChance():P0}",
                    AuxiliaryEffectType.QiCompression => $"灵气压缩 ×{kvp.Value}：凝实度 +{GetCohesionBonus():P0}",
                    AuxiliaryEffectType.SoulConversion => $"噬灵转化 ×{kvp.Value}：击杀回复 {GetSoulConversionHealPercent():P0} 耐久",
                    _ => $"未知辅助效果 ×{kvp.Value}"
                };
                list.Add(desc);
            }
            return list;
        }
    }
}
