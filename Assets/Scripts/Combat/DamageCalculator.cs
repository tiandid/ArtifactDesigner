using System;
using ArtifactDesigner.Core;
using UnityEngine;

namespace ArtifactDesigner.Combat
{
    /// <summary>
    /// 实时战斗伤害结算（Unity 侧包装）。
    /// 公式：实际攻击 × 随机 [0.9, 1.1] × 100 / (100 + 目标防御)，最小 1。
    /// 纯逻辑部分见 FormulaHelper.EvaluateDamage。
    /// </summary>
    public static class DamageCalculator
    {
        public static int Calculate(int attack, int defense)
        {
            float variance = UnityEngine.Random.Range(
                FormulaHelper.AttackVarianceMin,
                FormulaHelper.AttackVarianceMax);
            return FormulaHelper.EvaluateDamage(attack, defense, variance);
        }
    }
}