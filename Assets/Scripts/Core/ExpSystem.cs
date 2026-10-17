using System;
using System.Collections.Generic;

namespace ArtifactDesigner.Core
{
    /// <summary>
    /// 经验系统：按 exp_level_requirement_new.md 维护升级所需经验表。
    /// 公式：升到下一级所需经验 = 560 × 当前等级(N) × 对应境界聚灵大道常数
    /// </summary>
    public static class ExpSystem
    {
        // 境界常数（对应 exp_level_requirement_new.md）
        public static readonly int[] RealmConstants = new int[]
        {
            28,   // 凡人   1~10
            56,   // 练气  11~20
            144,  // 筑基  21~30
            192,  // 金丹  31~40
            400,  // 元婴  41~50
            480,  // 化神  51~60
            784,  // 炼虚  61~70
            896,  // 合体  71~80
            1332, // 大乘  81~90
            1480  // 渡劫  91~100
        };

        private static readonly Dictionary<int, long> _cache = new Dictionary<int, long>();

        /// <summary>当前等级 N 升到下一级所需经验。</summary>
        public static long GetExpRequired(int currentLevel)
        {
            if (currentLevel < 1 || currentLevel >= 100) return 0;
            if (_cache.TryGetValue(currentLevel, out long val)) return val;

            int realmIndex = GetRealmIndexByLevel(currentLevel);
            int constant = RealmConstants[realmIndex];
            long need = 560L * currentLevel * constant;
            _cache[currentLevel] = need;
            return need;
        }

        /// <summary>根据等级获取境界。</summary>
        public static Realm GetRealmByLevel(int level)
        {
            if (level <= 10) return Realm.Mortal;
            if (level <= 20) return Realm.QiRefining;
            if (level <= 30) return Realm.Foundation;
            if (level <= 40) return Realm.GoldenCore;
            if (level <= 50) return Realm.NascentSoul;
            if (level <= 60) return Realm.SpiritSever;
            if (level <= 70) return Realm.VoidRefining;
            if (level <= 80) return Realm.BodyMerge;
            if (level <= 90) return Realm.Mahayana;
            return Realm.Tribulation;
        }

        /// <summary>获取境界中文名。</summary>
        public static string GetRealmName(Realm realm)
        {
            return realm switch
            {
                Realm.Mortal => "凡人",
                Realm.QiRefining => "练气",
                Realm.Foundation => "筑基",
                Realm.GoldenCore => "金丹",
                Realm.NascentSoul => "元婴",
                Realm.SpiritSever => "化神",
                Realm.VoidRefining => "炼虚",
                Realm.BodyMerge => "合体",
                Realm.Mahayana => "大乘",
                Realm.Tribulation => "渡劫",
                _ => "未知"
            };
        }

        private static int GetRealmIndexByLevel(int level)
        {
            if (level <= 10) return 0;
            if (level <= 20) return 1;
            if (level <= 30) return 2;
            if (level <= 40) return 3;
            if (level <= 50) return 4;
            if (level <= 60) return 5;
            if (level <= 70) return 6;
            if (level <= 80) return 7;
            if (level <= 90) return 8;
            return 9;
        }
    }
}
