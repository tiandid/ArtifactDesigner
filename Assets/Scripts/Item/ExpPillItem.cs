using ArtifactDesigner.Core;

namespace ArtifactDesigner.Item
{
    /// <summary>
    /// 经验丹物品定义与使用逻辑。
    /// 依据 exp_pill_setting_v3.md：
    /// 经验增加值 = 境界第9级 × 560 × 对应境界聚灵阵线性衰减等效值 × 品质系数
    /// 品质（8档）：下品0.25 / 中品0.55 / 上品0.85 / 一纹1.00 / 二纹1.10 / 三纹1.20 / 六纹1.50 / 九纹1.80
    /// </summary>
    public enum PillQuality
    {
        Inferior = 0,   // 下品    0.25
        Medium,          // 中品    0.55
        Superior,        // 上品    0.85
        OneMark,         // 一纹    1.00
        TwoMark,         // 二纹    1.10
        ThreeMark,       // 三纹    1.20
        SixMark,         // 六纹    1.50
        NineMark         // 九纹    1.80
    }

    public static class ExpPillSettings
    {
        // 品质系数（与枚举顺序一致）
        public static readonly float[] QualityMultipliers = new float[]
        {
            0.25f, 0.55f, 0.85f, 1.00f, 1.10f, 1.20f, 1.50f, 1.80f
        };

        // 各境界第9级（作为统一经验基准）
        public static readonly int[] BaseLevels = new int[]
        {
            9,   // 凡人
            19,  // 练气
            29,  // 筑基
            39,  // 金丹
            49,  // 元婴
            59,  // 化神
            69,  // 炼虚
            79,  // 合体
            89,  // 大乘
            99   // 渡劫
        };

        // 各境界天道常数（线性衰减等效值，作为经验丹乘数）
        // 来源：julingzhen_max_power_table_v2.md
        public static readonly float[] RealmEquivalentConstants = new float[]
        {
            8.4f,   // 凡人
            16.8f,  // 练气
            47.2f,  // 筑基
            63.0f,  // 金丹
            125.3f, // 元婴
            150.3f, // 化神
            256.8f, // 炼虚
            293.5f, // 合体
            452.5f, // 大乘
            502.8f  // 渡劫
        };

        /// <summary>获取品质中文名。</summary>
        public static string GetQualityName(PillQuality q)
        {
            return q switch
            {
                PillQuality.Inferior => "下品",
                PillQuality.Medium => "中品",
                PillQuality.Superior => "上品",
                PillQuality.OneMark => "一纹",
                PillQuality.TwoMark => "二纹",
                PillQuality.ThreeMark => "三纹",
                PillQuality.SixMark => "六纹",
                PillQuality.NineMark => "九纹",
                _ => "未知"
            };
        }
    }

    /// <summary>经验丹物品数据。</summary>
    public class ExpPillItem
    {
        public PillQuality Quality { get; set; } = PillQuality.Inferior;
        public Realm TargetRealm { get; set; } = Realm.Mortal; // 丹药适用的境界

        /// <summary>计算该丹药可提供的经验值。</summary>
        public long CalculateExp()
        {
            int realmIndex = (int)TargetRealm;
            if (realmIndex < 0 || realmIndex >= ExpPillSettings.BaseLevels.Length) return 0;

            int baseLevel = ExpPillSettings.BaseLevels[realmIndex];
            float constant = ExpPillSettings.RealmEquivalentConstants[realmIndex];
            float qualityMult = ExpPillSettings.QualityMultipliers[(int)Quality];

            long exp = (long)(baseLevel * 560L * constant * qualityMult);
            return exp;
        }

        /// <summary>使用丹药，增加玩家经验。</summary>
        public bool Use(PlayerProfile profile)
        {
            if (profile == null) return false;
            long exp = CalculateExp();
            if (exp <= 0) return false;
            profile.AddExp(exp);
            return true;
        }

        /// <summary>获取物品描述（用于UI属性栏）。</summary>
        public string GetDescription()
        {
            long exp = CalculateExp();
            string realmName = ExpSystem.GetRealmName(TargetRealm);
            string qualityName = ExpPillSettings.GetQualityName(Quality);
            return $"{qualityName}{realmName}经验丹\n增加玩家大量经验\n经验值：{exp:N0}";
        }
    }
}
