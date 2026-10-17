namespace ArtifactDesigner.Core
{
    /// <summary>阵营：决定碰撞伤害是否结算、AI 目标选择与视觉颜色。</summary>
    public enum Faction
    {
        Player = 0,   // 玩家
        Enemy = 1,    // 敌方单位（小怪/Boss）
        Crystal = 2,  // 我方水晶（防守目标）
        Neutral = 3   // 中立（地形、护盾等无仇恨单位）
    }

    /// <summary>伤害类型。0.0.1 统一按物理结算，五行/属性伤害在技能系统接入后扩展。</summary>
    public enum DamageType
    {
        Physical = 0, // 物理（普攻与法宝撞击）
        Element = 1,  // 属性（火/水/木/土/金）
        Spirit = 2    // 精神（法宝/技能）
    }

    /// <summary>局内战斗状态机。</summary>
    public enum GameState
    {
        Preparing = 0,   // 准备阶段（波前倒计时）
        WaveActive = 1,  // 战斗进行中
        WaveClear = 2,   // 波次已清空（波间过渡）
        Victory = 3,     // 关卡胜利
        Defeat = 4       // 关卡失败（水晶被摧毁）
    }
}