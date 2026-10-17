using System;

namespace ArtifactDesigner.Core
{
    /// <summary>游戏基础枚举</summary>
    public static class GameEnums
    {
    }

    /// <summary>人物境界。境界越高，基础属性越强（成长体系：人物境界获得属性）。</summary>
    public enum Realm
    {
        Mortal = 0,   // 凡人
        QiRefining,   // 练气
        Foundation,   // 筑基
        GoldenCore,   // 金丹
        NascentSoul,  // 元婴
        SpiritSever,  // 化神
        VoidRefining, // 炼虚
        BodyMerge,    // 合体
        Mahayana,     // 大乘
        Tribulation   // 渡劫
    }

    /// <summary>部件类型（炼器部件七大类）。</summary>
    public enum PartType
    {
        AddSlot,       // 增加格子
        Durability,    // 增加耐久
        Attack,        // 增加攻击
        Speed,         // 增加速度
        ActiveSkill,   // 增加主动技能
        PassiveSkill,  // 增加被动技能
        SpiritCore     // 灵核（原能量核心，为法宝技能部件供能）
    }

    /// <summary>稀有度（品质）—— 白 / 蓝 / 紫 / 金 / 橙 / 红。</summary>
    public enum Rarity
    {
        Common = 0,      // 白
        Uncommon,        // 蓝
        Rare,            // 紫
        Epic,            // 金
        Legendary,       // 橙
        Mythic           // 红
    }

    /// <summary>五行元素。</summary>
    public enum ElementType
    {
        Metal = 0,    // 金
        Wood,         // 木
        Water,        // 水
        Fire,         // 火
        Earth         // 土
    }

    /// <summary>技能类型。</summary>
    public enum SkillType
    {
        Active,   // 主动技能（需要能量线路供电）
        Passive   // 被动技能（需要能量线路供电）
    }

    /// <summary>金丹品级（九品→超品，数字越小品质越高）。</summary>
    public enum GoldenCoreRank
    {
        Rank9 = 0,   // 九品
        Rank8,       // 八品
        Rank7,       // 七品
        Rank6,       // 六品
        Rank5,       // 五品
        Rank4,       // 四品
        Rank3,       // 三品
        Rank2,       // 二品
        Rank1,       // 一品
        Transcendent // 超品
    }

    /// <summary>突破阶段状态。</summary>
    public enum BreakthroughState
    {
        Idle,         // 未在突破
        Preparing,    // 准备中（进入场景、读取属性）
        Ongoing,      // 突破进行中
        Completed,    // 突破完成
        Failed        // 突破失败（时间/资源耗尽且未达成最低品级）
    }

    /// <summary>敌人特性（0.0.6）。</summary>
    public enum EnemyTrait
    {
        None,
        DoubleTi,           // 体双倍
        DoubleQi,           // 气双倍
        DoubleShen,         // 神双倍
        TripleAttackSpeed,  // 攻速三倍
        TripleRange,        // 攻击范围和移速三倍
        ElementalImmune,    // 元素免伤
        PhysicalImmune,     // 体攻免伤
        SpiritualImmune,    // 元攻免伤
        SoulBody,           // 魂体
        CrystalFocus,       // 对水晶攻击加倍
        Demonized,          // 入魔
        Obsessed,           // 执念
        Clone,              // 分身：开战/受击消耗10%生命造幻影，面板同真身，攻击无伤害
        TrueClone           // 真·分身：开战/受击消耗10%生命造分身，面板同真身，攻击10%~100%（等级1-10）
    }

    /// <summary>投射物类型（0.0.9）。</summary>
    public enum ProjectileType
    {
        SingleTarget,   // 单体：命中实体即消散回池
        AoE,            // 群体：非投射物，不受凝实度/空中对碰/碰撞消散影响
        Piercing        // 穿透型：暂不实现，保留接口
    }

    /// <summary>地形类型（0.0.9）。</summary>
    public enum TerrainType
    {
        Wall,    // 墙：自然寿命20秒，无防御只有血量/耐久
        Shield   // 护盾：有时长限制，耐久制
    }

    /// <summary>敌人AI状态（0.0.9）。</summary>
    public enum EnemyAIState
    {
        Moving,              // 行进态：向目标移动
        Attacking,           // 攻击态：进入范围后普攻
        Kiting,              // 追仇态：追击玩家中（用于风筝判定）
        Demonized,           // 入魔态：10秒移速攻速×2，锁水晶，不可拉仇恨
        DestroyingTerrain,   // 击碎态：正在攻击阻挡地形
        Detouring            // 绕路态：绕开阻挡地形
    }
}