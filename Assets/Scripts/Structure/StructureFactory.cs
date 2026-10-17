using System;
using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Structure
{
    /// <summary>
    /// 法宝结构工厂：程序化生成全部六类结构定义。
    /// 支持按境界、五行、子类型批量创建结构模板。
    /// </summary>
    public static class StructureFactory
    {
        // ---------- 常量：境界-圈数映射 ----------
        private static readonly int[] RealmRings = new int[10] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 };

        // ---------- 常量：输出/技能结构最大格子数 ----------
        private static readonly int[] MaxOutputSkillCells = new int[10] { 3, 3, 3, 4, 4, 5, 5, 6, 6, 7 };

        // ---------- 常量：核心最大占格 ----------
        private static readonly int[] MaxCoreCells = new int[10] { 4, 4, 6, 9, 12, 16, 20, 25, 30, 36 };

        // ---------- 常量：法宝类型名称 ----------
        private static readonly string[] ArtifactTypeNames = new string[10]
        {
            "剑", "钟", "环", "塔", "印", "幡", "葫芦", "镜", "鼎", "珠"
        };

        // ---------- 常量：输出结构子类型名称 ----------
        private static readonly string[] OutputSubtypeNames = new string[3] { "炮管", "刃口", "发射头" };

        // ---------- 常量：技能结构子类型名称 ----------
        private static readonly string[] SkillSubtypeNames = new string[3] { "符文板", "导引针", "蓄能珠" };

        // ---------- 常量：辅助结构名称 ----------
        private static readonly string[] AuxiliaryNames = new string[10]
        {
            "多重投射", "坚韧护甲", "器灵之芯", "灵力汲取", "护盾生成",
            "暴击增幅", "五行共鸣", "残影遁法", "灵气压缩", "噬灵转化"
        };

        // ---------- 常量：五行名称 ----------
        private static readonly string[] ElementNames = new string[5] { "金", "木", "水", "火", "土" };

        // ---------- 常量：境界名称 ----------
        private static readonly string[] RealmNames = new string[10]
        {
            "凡人", "练气", "筑基", "金丹", "元婴",
            "化神", "炼虚", "合体", "大乘", "渡劫"
        };

        // ========== 灵核生成 ==========

        /// <summary>
        /// 生成指定境界的全部灵核结构（2大小 × 5五行 = 10种）。
        /// </summary>
        public static List<StructureDefinition> CreateSpiritCores(Realm realm)
        {
            var list = new List<StructureDefinition>();
            int idx = (int)realm;
            int rings = RealmRings[idx];

            // 小尺寸：rings × rings
            int smallSize = rings;
            // 大尺寸：rings+1 × rings+1
            int largeSize = rings + 1;

            foreach (ElementType elem in Enum.GetValues(typeof(ElementType)))
            {
                // 小灵核
                list.Add(CreateSpiritCoreDef(realm, elem, CoreSizeLabel.Small, smallSize));
                // 大灵核
                list.Add(CreateSpiritCoreDef(realm, elem, CoreSizeLabel.Large, largeSize));
            }
            return list;
        }

        private static StructureDefinition CreateSpiritCoreDef(Realm realm, ElementType elem, CoreSizeLabel sizeLabel, int side)
        {
            string sizeStr = sizeLabel == CoreSizeLabel.Small ? "小" : "大";
            var def = new StructureDefinition
            {
                Id = $"sc_{(int)realm}_{(int)elem}_{(int)sizeLabel}",
                DisplayName = $"{RealmNames[(int)realm]}{sizeStr}灵核·{ElementNames[(int)elem]}",
                Category = StructureCategory.SpiritCore,
                Rarity = RealmToRarity(realm),
                Element = elem,
                MinRealm = realm,
                GridSize = new StructureSize(side, side),
                BaseModelPath = $"Models/Structures/SpiritCore/Realm{(int)realm}_{sizeLabel}",
                ParticlePrefabPath = $"Effects/Particles/{ElementNames[(int)elem]}Swirl",
                MaxHpBonus = 10f * (1 + (int)realm) * side,
                SpiritOutput = 5f * (1 + (int)realm) * side,
                Weight = side * side * 2f
            };
            def.BuildRectMask();
            return def;
        }

        // ========== 输出结构生成 ==========

        /// <summary>
        /// 生成指定境界的全部输出结构（3种 × 5五行 = 15种）。
        /// </summary>
        public static List<StructureDefinition> CreateOutputStructures(Realm realm)
        {
            var list = new List<StructureDefinition>();
            int idx = (int)realm;
            int maxCells = MaxOutputSkillCells[idx];

            for (int sub = 0; sub < 3; sub++)
            {
                foreach (ElementType elem in Enum.GetValues(typeof(ElementType)))
                {
                    list.Add(CreateOutputDef(realm, (OutputSubtype)sub, elem, maxCells));
                }
            }
            return list;
        }

        private static StructureDefinition CreateOutputDef(Realm realm, OutputSubtype sub, ElementType elem, int maxCells)
        {
            var (size, nameSuffix) = GetOutputShape(realm, sub, maxCells);
            var def = new StructureDefinition
            {
                Id = $"out_{(int)realm}_{(int)sub}_{(int)elem}",
                DisplayName = $"{RealmNames[(int)realm]}输出·{nameSuffix}·{ElementNames[(int)elem]}",
                Category = StructureCategory.Output,
                Rarity = RealmToRarity(realm),
                Element = elem,
                MinRealm = realm,
                GridSize = size,
                BaseModelPath = $"Models/Structures/Output/Realm{(int)realm}_{sub}",
                ParticlePrefabPath = $"Effects/Particles/{ElementNames[(int)elem]}Glow",
                AttackBonus = 5f * (1 + (int)realm),
                Weight = size.Area * 1.5f
            };
            def.BuildRectMask(); // 基础矩形，后续可按形状细化掩码
            return def;
        }

        private static (StructureSize size, string name) GetOutputShape(Realm realm, OutputSubtype sub, int maxCells)
        {
            // 返回形状尺寸与名称后缀
            return ((int)realm, sub) switch
            {
                // 凡人
                (0, OutputSubtype.Barrel)  => (new StructureSize(1, 3), "短炮管"),
                (0, OutputSubtype.Blade)   => (new StructureSize(2, 2), "双刃口"),
                (0, OutputSubtype.Emitter) => (new StructureSize(1, 3), "尖锥头"),
                // 练气
                (1, OutputSubtype.Barrel)  => (new StructureSize(1, 3), "中炮管"),
                (1, OutputSubtype.Blade)   => (new StructureSize(2, 2), "三刃口"),
                (1, OutputSubtype.Emitter) => (new StructureSize(2, 2), "棱锥头"),
                // 筑基
                (2, OutputSubtype.Barrel)  => (new StructureSize(1, 3), "重炮管"),
                (2, OutputSubtype.Blade)   => (new StructureSize(2, 2), "环刃口"),
                (2, OutputSubtype.Emitter) => (new StructureSize(2, 2), "多头锥"),
                // 金丹
                (3, OutputSubtype.Barrel)  => (new StructureSize(2, 2), "连装炮"),
                (3, OutputSubtype.Blade)   => (new StructureSize(1, 4), "破甲刃"),
                (3, OutputSubtype.Emitter) => (new StructureSize(3, 3), "散射口"),
                // 元婴
                (4, OutputSubtype.Barrel)  => (new StructureSize(2, 2), "相位炮"),
                (4, OutputSubtype.Blade)   => (new StructureSize(2, 2), "回旋刃"),
                (4, OutputSubtype.Emitter) => (new StructureSize(2, 2), "脉冲口"),
                // 化神
                (5, OutputSubtype.Barrel)  => (new StructureSize(2, 3), "裂解炮"),
                (5, OutputSubtype.Blade)   => (new StructureSize(1, 5), "链锯刃"),
                (5, OutputSubtype.Emitter) => (new StructureSize(3, 3), "浮游口"),
                // 炼虚
                (6, OutputSubtype.Barrel)  => (new StructureSize(2, 3), "坍缩炮"),
                (6, OutputSubtype.Blade)   => (new StructureSize(3, 3), "虚空刃"),
                (6, OutputSubtype.Emitter) => (new StructureSize(3, 3), "虫洞口"),
                // 合体
                (7, OutputSubtype.Barrel)  => (new StructureSize(2, 3), "因果炮"),
                (7, OutputSubtype.Blade)   => (new StructureSize(3, 3), "万象刃"),
                (7, OutputSubtype.Emitter) => (new StructureSize(3, 3), "星门口"),
                // 大乘
                (8, OutputSubtype.Barrel)  => (new StructureSize(3, 3), "奇点炮"),
                (8, OutputSubtype.Blade)   => (new StructureSize(3, 3), "维度刃"),
                (8, OutputSubtype.Emitter) => (new StructureSize(3, 3), "创世纪口"),
                // 渡劫
                (9, OutputSubtype.Barrel)  => (new StructureSize(3, 3), "终焉炮"),
                (9, OutputSubtype.Blade)   => (new StructureSize(3, 3), "虚无刃"),
                (9, OutputSubtype.Emitter) => (new StructureSize(3, 3), "轮回口"),
                _ => (new StructureSize(1, 1), "未知")
            };
        }

        // ========== 技能结构生成 ==========

        /// <summary>
        /// 生成指定境界的全部技能结构（3种 × 5五行 = 15种）。
        /// </summary>
        public static List<StructureDefinition> CreateSkillStructures(Realm realm)
        {
            var list = new List<StructureDefinition>();
            int idx = (int)realm;
            int maxCells = MaxOutputSkillCells[idx];

            for (int sub = 0; sub < 3; sub++)
            {
                foreach (ElementType elem in Enum.GetValues(typeof(ElementType)))
                {
                    list.Add(CreateSkillDef(realm, (SkillSubtype)sub, elem, maxCells));
                }
            }
            return list;
        }

        private static StructureDefinition CreateSkillDef(Realm realm, SkillSubtype sub, ElementType elem, int maxCells)
        {
            var (size, nameSuffix) = GetSkillShape(realm, sub, maxCells);
            var def = new StructureDefinition
            {
                Id = $"sk_{(int)realm}_{(int)sub}_{(int)elem}",
                DisplayName = $"{RealmNames[(int)realm]}技能·{nameSuffix}·{ElementNames[(int)elem]}",
                Category = StructureCategory.Skill,
                Rarity = RealmToRarity(realm),
                Element = elem,
                MinRealm = realm,
                GridSize = size,
                BaseModelPath = $"Models/Structures/Skill/Realm{(int)realm}_{sub}",
                ParticlePrefabPath = $"Effects/Particles/{ElementNames[(int)elem]}Rune",
                SpiritOutput = -2f * (1 + (int)realm), // 技能结构消耗灵力
                Weight = size.Area * 1.2f
            };
            def.BuildRectMask();
            return def;
        }

        private static (StructureSize size, string name) GetSkillShape(Realm realm, SkillSubtype sub, int maxCells)
        {
            return ((int)realm, sub) switch
            {
                // 凡人
                (0, SkillSubtype.Rune)   => (new StructureSize(1, 3), "符文板"),
                (0, SkillSubtype.Needle) => (new StructureSize(2, 2), "导引针"),
                (0, SkillSubtype.Orb)    => (new StructureSize(1, 3), "蓄能珠"),
                // 练气
                (1, SkillSubtype.Rune)   => (new StructureSize(1, 3), "连锁符"),
                (1, SkillSubtype.Needle) => (new StructureSize(2, 2), "阵列针"),
                (1, SkillSubtype.Orb)    => (new StructureSize(2, 2), "分流珠"),
                // 筑基
                (2, SkillSubtype.Rune)   => (new StructureSize(2, 2), "共鸣符"),
                (2, SkillSubtype.Needle) => (new StructureSize(2, 2), "螺旋针"),
                (2, SkillSubtype.Orb)    => (new StructureSize(1, 3), "压缩珠"),
                // 金丹
                (3, SkillSubtype.Rune)   => (new StructureSize(2, 2), "法阵盘"),
                (3, SkillSubtype.Needle) => (new StructureSize(3, 3), "聚灵针"),
                (3, SkillSubtype.Orb)    => (new StructureSize(3, 3), "延时珠"),
                // 元婴
                (4, SkillSubtype.Rune)   => (new StructureSize(2, 2), "万象符"),
                (4, SkillSubtype.Needle) => (new StructureSize(2, 2), "相位针"),
                (4, SkillSubtype.Orb)    => (new StructureSize(2, 2), "复制珠"),
                // 化神
                (5, SkillSubtype.Rune)   => (new StructureSize(2, 3), "领域盘"),
                (5, SkillSubtype.Needle) => (new StructureSize(3, 3), "因果针"),
                (5, SkillSubtype.Orb)    => (new StructureSize(3, 3), "黑洞珠"),
                // 炼虚
                (6, SkillSubtype.Rune)   => (new StructureSize(3, 3), "平行盘"),
                (6, SkillSubtype.Needle) => (new StructureSize(3, 3), "维度针"),
                (6, SkillSubtype.Orb)    => (new StructureSize(3, 3), "时间珠"),
                // 合体
                (7, SkillSubtype.Rune)   => (new StructureSize(3, 3), "天道盘"),
                (7, SkillSubtype.Needle) => (new StructureSize(3, 3), "混沌针"),
                (7, SkillSubtype.Orb)    => (new StructureSize(3, 3), "轮回珠"),
                // 大乘
                (8, SkillSubtype.Rune)   => (new StructureSize(3, 3), "宇宙盘"),
                (8, SkillSubtype.Needle) => (new StructureSize(3, 3), "真空针"),
                (8, SkillSubtype.Orb)    => (new StructureSize(3, 3), "奇点珠"),
                // 渡劫
                (9, SkillSubtype.Rune)   => (new StructureSize(3, 3), "终焉盘"),
                (9, SkillSubtype.Needle) => (new StructureSize(3, 3), "虚无针"),
                (9, SkillSubtype.Orb)    => (new StructureSize(3, 3), "永恒珠"),
                _ => (new StructureSize(1, 1), "未知")
            };
        }

        // ========== 推进器生成 ==========

        /// <summary>
        /// 生成指定境界的全部推进器（2大小 × 5五行 = 10种）。
        /// </summary>
        public static List<StructureDefinition> CreateThrusters(Realm realm)
        {
            var list = new List<StructureDefinition>();
            int idx = (int)realm;
            int rings = RealmRings[idx];

            // 小尺寸：rings × (rings+1)
            int smallW = rings, smallH = rings + 1;
            // 大尺寸：(rings+1) × (rings+2)
            int largeW = rings + 1, largeH = rings + 2;

            foreach (ElementType elem in Enum.GetValues(typeof(ElementType)))
            {
                list.Add(CreateThrusterDef(realm, elem, ThrusterSizeLabel.Small, smallW, smallH));
                list.Add(CreateThrusterDef(realm, elem, ThrusterSizeLabel.Large, largeW, largeH));
            }
            return list;
        }

        private static StructureDefinition CreateThrusterDef(Realm realm, ElementType elem, ThrusterSizeLabel sizeLabel, int w, int h)
        {
            string sizeStr = sizeLabel == ThrusterSizeLabel.Small ? "小" : "大";
            var def = new StructureDefinition
            {
                Id = $"th_{(int)realm}_{(int)elem}_{(int)sizeLabel}",
                DisplayName = $"{RealmNames[(int)realm]}{sizeStr}推进器·{ElementNames[(int)elem]}",
                Category = StructureCategory.Thruster,
                Rarity = RealmToRarity(realm),
                Element = elem,
                MinRealm = realm,
                GridSize = new StructureSize(w, h),
                BaseModelPath = $"Models/Structures/Thruster/Realm{(int)realm}_{sizeLabel}",
                ParticlePrefabPath = $"Effects/Particles/{ElementNames[(int)elem]}Flame",
                SpeedBonus = 2f * (1 + (int)realm) * w * h,
                Weight = w * h * 1.0f
            };
            def.BuildRectMask();
            return def;
        }

        // ========== 核心结构生成 ==========

        /// <summary>
        /// 生成指定境界的全部核心结构（10种法宝类型 × 1个 = 10种）。
        /// </summary>
        public static List<StructureDefinition> CreateCores(Realm realm)
        {
            var list = new List<StructureDefinition>();
            foreach (ArtifactType type in Enum.GetValues(typeof(ArtifactType)))
            {
                list.Add(CreateCoreDef(realm, type));
            }
            return list;
        }

        private static StructureDefinition CreateCoreDef(Realm realm, ArtifactType type)
        {
            int idx = (int)realm;
            int maxCells = MaxCoreCells[idx];
            var (w, h) = GetCoreSize(type, maxCells);

            var def = new StructureDefinition
            {
                Id = $"core_{(int)realm}_{(int)type}",
                DisplayName = $"{RealmNames[idx]}{ArtifactTypeNames[(int)type]}核心",
                Category = StructureCategory.Core,
                Rarity = RealmToRarity(realm),
                Element = ElementType.Metal, // 核心无固定五行，由使用者决定
                MinRealm = realm,
                GridSize = new StructureSize(w, h),
                BaseModelPath = $"Models/Structures/Core/Realm{(int)realm}_{type}",
                ParticlePrefabPath = $"Effects/Particles/CoreAura",
                CoreArtifactType = type,
                CompatibilityBase = 0.5f + idx * 0.05f,
                Weight = w * h * 3f
            };
            def.BuildRectMask();
            return def;
        }

        private static (int w, int h) GetCoreSize(ArtifactType type, int maxCells)
        {
            // 根据类型和境界上限选择近似尺寸
            return type switch
            {
                ArtifactType.Sword    => (Mathf.Min(2, maxCells), Mathf.Min(4, maxCells / 2 + 1)),
                ArtifactType.Bell     => (Mathf.Min(3, maxCells / 3 + 1), Mathf.Min(3, maxCells / 3 + 1)),
                ArtifactType.Ring     => (Mathf.Min(3, maxCells / 3 + 1), Mathf.Min(3, maxCells / 3 + 1)),
                ArtifactType.Tower    => (Mathf.Min(2, maxCells / 4 + 1), Mathf.Min(4, maxCells / 2 + 1)),
                ArtifactType.Seal     => (Mathf.Min(3, maxCells / 3 + 1), Mathf.Min(3, maxCells / 3 + 1)),
                ArtifactType.Banner   => (Mathf.Min(2, maxCells / 4 + 1), Mathf.Min(4, maxCells / 2 + 1)),
                ArtifactType.Gourd    => (Mathf.Min(2, maxCells / 3 + 1), Mathf.Min(3, maxCells / 2 + 1)),
                ArtifactType.Mirror   => (Mathf.Min(3, maxCells / 3 + 1), Mathf.Min(3, maxCells / 3 + 1)),
                ArtifactType.Cauldron => (Mathf.Min(3, maxCells / 3 + 1), Mathf.Min(3, maxCells / 3 + 1)),
                ArtifactType.Pearl    => (Mathf.Min(2, maxCells / 2 + 1), Mathf.Min(2, maxCells / 2 + 1)),
                _ => (1, 1)
            };
        }

        // ========== 辅助结构生成 ==========

        /// <summary>
        /// 生成全部10种辅助结构（境界无关，效果按境界缩放）。
        /// </summary>
        public static List<StructureDefinition> CreateAllAuxiliaries()
        {
            var list = new List<StructureDefinition>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(CreateAuxiliaryDef((AuxiliaryEffectType)i));
            }
            return list;
        }

        private static StructureDefinition CreateAuxiliaryDef(AuxiliaryEffectType type)
        {
            var def = new StructureDefinition
            {
                Id = $"aux_{(int)type}",
                DisplayName = $"辅助·{AuxiliaryNames[(int)type]}",
                Category = StructureCategory.Auxiliary,
                Rarity = Rarity.Rare,
                Element = ElementType.Metal, // 辅助结构无固定五行
                MinRealm = Realm.Mortal,
                GridSize = new StructureSize(1, 1),
                BaseModelPath = $"Models/Structures/Auxiliary/{type}",
                ParticlePrefabPath = $"Effects/Particles/Auxiliary_{type}",
                AuxType = type,
                AuxBaseValue = GetAuxBaseValue(type),
                AuxStackMultiplier = 1.0f,
                Weight = 0.5f
            };
            def.BuildRectMask();
            return def;
        }

        private static float GetAuxBaseValue(AuxiliaryEffectType type)
        {
            return type switch
            {
                AuxiliaryEffectType.MultiShot        => 0.5f,   // 每0.5个+1投射物
                AuxiliaryEffectType.DurableArmor     => 0.15f,  // +15%耐久
                AuxiliaryEffectType.ArtifactSpirit   => 1.0f,   // 开启AI
                AuxiliaryEffectType.ManaLeech        => 0.01f,  // +1%灵力回复
                AuxiliaryEffectType.ShieldGenerator  => 1.0f,   // 间隔缩减
                AuxiliaryEffectType.CriticalSurge    => 0.03f,  // +3%暴击率
                AuxiliaryEffectType.ElementalResonance => 0.02f, // +2%共鸣
                AuxiliaryEffectType.PhantomStep      => 1.0f,   // 间隔缩减
                AuxiliaryEffectType.QiCompression    => 0.08f,  // +8%凝实度
                AuxiliaryEffectType.SoulConversion   => 0.05f,  // +5%击杀回血
                _ => 0f
            };
        }

        // ========== 批量生成全部 ==========

        /// <summary>
        /// 生成指定境界的全部六类结构。
        /// </summary>
        public static Dictionary<StructureCategory, List<StructureDefinition>> CreateAllForRealm(Realm realm)
        {
            var dict = new Dictionary<StructureCategory, List<StructureDefinition>>();
            dict[StructureCategory.SpiritCore] = CreateSpiritCores(realm);
            dict[StructureCategory.Output] = CreateOutputStructures(realm);
            dict[StructureCategory.Skill] = CreateSkillStructures(realm);
            dict[StructureCategory.Thruster] = CreateThrusters(realm);
            dict[StructureCategory.Core] = CreateCores(realm);
            // 辅助结构境界无关，但为统一接口也放入
            if (!dict.ContainsKey(StructureCategory.Auxiliary))
                dict[StructureCategory.Auxiliary] = CreateAllAuxiliaries();
            return dict;
        }

        // ========== 工具方法 ==========

        /// <summary>境界 → 稀有度映射（用于结构品质显示）。</summary>
        private static Rarity RealmToRarity(Realm realm)
        {
            return realm switch
            {
                Realm.Mortal      => Rarity.Common,
                Realm.QiRefining  => Rarity.Common,
                Realm.Foundation  => Rarity.Uncommon,
                Realm.GoldenCore    => Rarity.Uncommon,
                Realm.NascentSoul  => Rarity.Rare,
                Realm.SpiritSever  => Rarity.Rare,
                Realm.VoidRefining => Rarity.Epic,
                Realm.BodyMerge    => Rarity.Epic,
                Realm.Mahayana     => Rarity.Legendary,
                Realm.Tribulation  => Rarity.Mythic,
                _ => Rarity.Common
            };
        }
    }
}
