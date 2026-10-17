using System.Collections.Generic;

namespace ArtifactDesigner.Core
{
    /// <summary>
    /// 境界属性表：人物境界 → 基础属性（成长体系预留）。
    /// 目前仅提供数值表与查询，后续可扩展修炼突破等玩法。
    /// </summary>
    public static class RealmSystem
    {
        private static readonly Dictionary<Realm, AttributeSet> RealmBase =
            new Dictionary<Realm, AttributeSet>
            {
                { Realm.Mortal, new AttributeSet { attack = 5, defense = 2, speed = 1.0f, maxDurability = 50, maxEnergy = 0 } },
                { Realm.QiRefining, new AttributeSet { attack = 10, defense = 5, speed = 1.2f, maxDurability = 80, maxEnergy = 10 } },
                { Realm.Foundation, new AttributeSet { attack = 20, defense = 10, speed = 1.4f, maxDurability = 120, maxEnergy = 20 } },
                { Realm.GoldenCore, new AttributeSet { attack = 40, defense = 20, speed = 1.6f, maxDurability = 180, maxEnergy = 35 } },
                { Realm.NascentSoul, new AttributeSet { attack = 80, defense = 40, speed = 1.8f, maxDurability = 260, maxEnergy = 55 } },
                { Realm.SpiritSever, new AttributeSet { attack = 150, defense = 80, speed = 2.0f, maxDurability = 380, maxEnergy = 80 } },
                { Realm.VoidRefining, new AttributeSet { attack = 280, defense = 150, speed = 2.2f, maxDurability = 560, maxEnergy = 120 } },
                { Realm.BodyMerge, new AttributeSet { attack = 500, defense = 280, speed = 2.5f, maxDurability = 820, maxEnergy = 180 } },
                { Realm.Mahayana, new AttributeSet { attack = 900, defense = 500, speed = 2.8f, maxDurability = 1200, maxEnergy = 260 } },
                { Realm.Tribulation, new AttributeSet { attack = 1600, defense = 900, speed = 3.2f, maxDurability = 1800, maxEnergy = 380 } }
            };

        /// <summary>获取某境界的基础属性。</summary>
        public static AttributeSet GetBaseAttributes(Realm realm)
        {
            return RealmBase.TryGetValue(realm, out var attrs) ? attrs : RealmBase[Realm.Mortal];
        }

        /// <summary>获取下一个境界（用于修炼突破判定）。</summary>
        public static Realm? NextRealm(Realm realm)
        {
            if (realm == Realm.Tribulation) return null;
            return (Realm)((int)realm + 1);
        }
    }
}