using System;

namespace ArtifactDesigner.Structure
{
    /// <summary>
    /// 法宝结构大类枚举。
    /// 法宝由多个结构部件拼接而成，每个结构占若干网格格子。
    /// </summary>
    public enum StructureCategory
    {
        SpiritCore,  // 灵核：正方形，提供能量/生命
        Output,      // 输出结构：攻击发射端
        Skill,       // 技能结构：法术增幅/导引
        Thruster,    // 推进器：略扁长方形，提供移速
        Core,        // 核心：决定法宝类型，造型与契合度模板相似
        Auxiliary    // 辅助结构：1×1单格，提供特殊机制
    }

    /// <summary>
    /// 法宝类型（决定核心结构造型）。
    /// </summary>
    public enum ArtifactType
    {
        Sword,    // 剑
        Bell,     // 钟
        Ring,     // 环
        Tower,    // 塔
        Seal,     // 印
        Banner,   // 幡
        Gourd,    // 葫芦
        Mirror,   // 镜
        Cauldron, // 鼎
        Pearl     // 珠
    }

    /// <summary>
    /// 输出结构子类型（每境界3种）。
    /// </summary>
    public enum OutputSubtype
    {
        Barrel,    // 炮管类
        Blade,     // 刃口类
        Emitter    // 发射头类
    }

    /// <summary>
    /// 技能结构子类型（每境界3种）。
    /// </summary>
    public enum SkillSubtype
    {
        Rune,      // 符文板类
        Needle,    // 导引针类
        Orb        // 蓄能珠类
    }

    /// <summary>
    /// 推进器尺寸标签。
    /// </summary>
    public enum ThrusterSizeLabel
    {
        Small,     // 小尺寸
        Large      // 大尺寸
    }

    /// <summary>
    /// 灵核尺寸标签。
    /// </summary>
    public enum CoreSizeLabel
    {
        Small,     // 小尺寸
        Large      // 大尺寸
    }

    /// <summary>
    /// 辅助结构效果类型（10种）。
    /// </summary>
    public enum AuxiliaryEffectType
    {
        MultiShot,        // 多重投射：额外发射投射物
        DurableArmor,     // 坚韧护甲：耐久上限提升
        ArtifactSpirit,   // 器灵之芯：自动攻击AI
        ManaLeech,        // 灵力汲取：命中回复灵力
        ShieldGenerator,  // 护盾生成：周期性护盾
        CriticalSurge,    // 暴击增幅：暴击率/暴伤
        ElementalResonance,// 五行共鸣：同属性加成
        PhantomStep,      // 残影遁法：瞬移/闪避
        QiCompression,    // 灵气压缩：凝实度提升
        SoulConversion    // 噬灵转化：击杀回血
    }
}
