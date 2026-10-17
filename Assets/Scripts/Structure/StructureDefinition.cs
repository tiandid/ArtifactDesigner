using System;
using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Structure
{
    /// <summary>
    /// 法宝结构定义：描述一种可装配到器坯上的结构部件。
    /// 涵盖灵核、输出结构、技能结构、推进器、核心、辅助六类。
    /// </summary>
    [Serializable]
    public class StructureDefinition
    {
        // ---------- 基础标识 ----------
        public string Id;                      // 唯一ID
        public string DisplayName;             // 显示名称
        public StructureCategory Category;     // 结构大类
        public Rarity Rarity;                  // 品质（白蓝紫金橙红）
        public ElementType Element;            // 五行属性
        public Realm MinRealm;                 // 最低可用境界

        // ---------- 尺寸与形状 ----------
        public StructureSize GridSize;         // 网格尺寸（宽x高）
        public int CellCount => GridSize.Area; // 占格总数
        public bool[,] OccupancyMask;          // true=该格被占用（用于非矩形形状）

        // ---------- 视觉 ----------
        public string BaseModelPath;           // 基础模型路径（同境同类型共用）
        public string ParticlePrefabPath;      // 粒子特效路径
        public Sprite Icon;                    // UI图标

        // ---------- 数值 ----------
        public float AttackBonus;              // 攻击加成
        public float DefenseBonus;             // 防御加成
        public float MaxHpBonus;               // 耐久/生命加成
        public float SpeedBonus;               // 速度加成（移速/攻速）
        public float SpiritOutput;             // 灵力输出（供技能部件）
        public float Weight;                   // 重量

        // ---------- 辅助结构专用 ----------
        public AuxiliaryEffectType? AuxType;   // 辅助效果类型（仅辅助结构）
        public float AuxBaseValue;             // 辅助效果基准值（百分比或固定值）
        public float AuxStackMultiplier;       // 辅助效果堆叠乘数

        // ---------- 核心结构专用 ----------
        public ArtifactType? CoreArtifactType; // 法宝类型（仅核心结构）
        public float CompatibilityBase;        // 契合度基础分

        // ---------- 运行时 ----------
        public int DurabilityPerCell;          // 每格血量（器胚扩展用）

        /// <summary>
        /// 获取该结构实际占用的格子坐标列表（相对于左上角原点）。
        /// </summary>
        public List<Vector2Int> GetOccupiedCells()
        {
            var cells = new List<Vector2Int>();
            if (OccupancyMask == null) BuildRectMask();
            for (int x = 0; x < GridSize.Width; x++)
                for (int y = 0; y < GridSize.Height; y++)
                    if (OccupancyMask[x, y]) cells.Add(new Vector2Int(x, y));
            return cells;
        }

        /// <summary>
        /// 构建默认的矩形占用掩码（所有格子都被占用）。
        /// </summary>
        public void BuildRectMask()
        {
            OccupancyMask = new bool[GridSize.Width, GridSize.Height];
            for (int x = 0; x < GridSize.Width; x++)
                for (int y = 0; y < GridSize.Height; y++)
                    OccupancyMask[x, y] = true;
        }

        /// <summary>
        /// 克隆结构定义（深拷贝数值，浅拷贝引用类型）。
        /// </summary>
        public StructureDefinition Clone()
        {
            var clone = (StructureDefinition)MemberwiseClone();
            if (OccupancyMask != null)
            {
                clone.OccupancyMask = new bool[GridSize.Width, GridSize.Height];
                Array.Copy(OccupancyMask, clone.OccupancyMask, OccupancyMask.Length);
            }
            return clone;
        }

        /// <summary>是否为灵核。</summary>
        public bool IsSpiritCore => Category == StructureCategory.SpiritCore;

        /// <summary>是否为输出结构。</summary>
        public bool IsOutput => Category == StructureCategory.Output;

        /// <summary>是否为技能结构。</summary>
        public bool IsSkill => Category == StructureCategory.Skill;

        /// <summary>是否为推进器。</summary>
        public bool IsThruster => Category == StructureCategory.Thruster;

        /// <summary>是否为核心结构。</summary>
        public bool IsCore => Category == StructureCategory.Core;

        /// <summary>是否为辅助结构。</summary>
        public bool IsAuxiliary => Category == StructureCategory.Auxiliary;

        /// <summary>获取灵智占用（单格=1，核心按占格数）。</summary>
        public int IntelligenceCost
        {
            get
            {
                if (IsAuxiliary)
                {
                    // 器灵之芯特殊占用3点
                    if (AuxType == AuxiliaryEffectType.ArtifactSpirit) return 3;
                    return 1; // 其他辅助结构单格占用1点
                }
                if (IsCore) return CellCount; // 核心按占格数
                return CellCount; // 其他按占格数
            }
        }
    }
}
