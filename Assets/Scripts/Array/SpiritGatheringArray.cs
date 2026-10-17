using System;
using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Array
{
    /// <summary>
    /// 聚灵阵主管理器（单例 MonoBehaviour）。
    /// 职责：阵盘尺寸管理、灵物摆放/移除、浓度实时计算、效果映射、存档读写。
    /// </summary>
    public class SpiritGatheringArray : MonoBehaviour
    {
        public static SpiritGatheringArray Instance { get; private set; }

        public int CurrentRadius { get; private set; } = 3;

        public Dictionary<Vector2Int, ArraySlot> Slots { get; private set; } = new Dictionary<Vector2Int, ArraySlot>();

        public float TotalConcentration { get; private set; } = 0f;
        public float EquivalentValue { get; private set; } = 0f;
        public ElementType DominantElement { get; private set; } = ElementType.Metal;
        public bool IsChaotic { get; private set; } = true;

        public float AlchemySpiritPool { get; private set; } = 0f;
        public float SpiritCrystalCount { get; private set; } = 0f;

        public float AlchemyPoolMax
        {
            get
            {
                var profile = PlayerProfile.Instance;
                if (profile == null) return 1000f;
                return Mathf.Max(1000f, (int)profile.CurrentRealm * 1000f + 1000f);
            }
        }

        public static event Action OnArrayChanged;
        public static event Action<float, ElementType, float, bool> OnConcentrationUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var profile = PlayerProfile.Instance;
            if (profile != null)
                InitByRealm(profile.CurrentRealm);
            else
                RebuildSlots();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (TotalConcentration > 0 && AlchemySpiritPool < AlchemyPoolMax)
            {
                float supplyPerSecond = TotalConcentration * 0.1f / 60f;
                float add = supplyPerSecond * Time.deltaTime;
                float newPool = AlchemySpiritPool + add;
                if (newPool >= AlchemyPoolMax)
                {
                    float excess = newPool - AlchemyPoolMax;
                    AlchemySpiritPool = AlchemyPoolMax;
                    SpiritCrystalCount += excess / 10000f;
                }
                else
                {
                    AlchemySpiritPool = newPool;
                }
            }
        }

        // ============ 阵盘管理 ============
        public void InitByRealm(Realm realm)
        {
            int newRadius = RealmToRadius(realm);
            if (newRadius != CurrentRadius || Slots.Count == 0)
            {
                CurrentRadius = newRadius;
                RebuildSlots();
                Recalculate();
            }
        }

        public void ExpandToRealm(Realm realm)
        {
            int newRadius = RealmToRadius(realm);
            if (newRadius > CurrentRadius)
            {
                CurrentRadius = newRadius;
                RebuildSlots();
                Recalculate();
            }
        }

        private void RebuildSlots()
        {
            var oldSlots = new Dictionary<Vector2Int, ArraySlot>(Slots);
            Slots.Clear();
            for (int x = -CurrentRadius; x <= CurrentRadius; x++)
            {
                for (int y = -CurrentRadius; y <= CurrentRadius; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (oldSlots.TryGetValue(pos, out var oldSlot))
                        Slots[pos] = oldSlot;
                    else
                        Slots[pos] = new ArraySlot(x, y, CurrentRadius);
                }
            }
        }

        private int RealmToRadius(Realm realm)
        {
            return realm switch
            {
                Realm.Mortal => 3,
                Realm.QiRefining => 3,
                Realm.Foundation => 4,
                Realm.GoldenCore => 4,
                Realm.NascentSoul => 5,
                Realm.SpiritSever => 5,
                Realm.VoidRefining => 6,
                Realm.BodyMerge => 6,
                Realm.Mahayana => 7,
                Realm.Tribulation => 7,
                _ => 3
            };
        }

        // ============ 摆放与移除 ============
        public bool PlaceItem(int x, int y, SpiritItem item)
        {
            var pos = new Vector2Int(x, y);
            if (!Slots.TryGetValue(pos, out var slot) || slot == null) return false;
            if (slot.IsEye) return false;
            if (!slot.PlaceItem(item)) return false;
            Recalculate();
            OnArrayChanged?.Invoke();
            return true;
        }

        public SpiritItem RemoveItem(int x, int y)
        {
            var pos = new Vector2Int(x, y);
            if (!Slots.TryGetValue(pos, out var slot) || slot == null) return null;
            if (slot.IsEye) return null;
            var removed = slot.RemoveItem();
            Recalculate();
            OnArrayChanged?.Invoke();
            return removed;
        }

        public ArraySlot GetSlot(int x, int y)
        {
            var pos = new Vector2Int(x, y);
            return Slots.TryGetValue(pos, out var slot) ? slot : null;
        }

        // ============ 浓度计算 ============
        public void Recalculate()
        {
            ConcentrationCalculator.Calculate(Slots, CurrentRadius, out float total, out ElementType dominant, out bool chaotic);
            TotalConcentration = total;
            DominantElement = dominant;
            IsChaotic = chaotic;

            var profile = PlayerProfile.Instance;
            Realm realm = profile != null ? profile.CurrentRealm : Realm.Mortal;
            float capped = ConcentrationCalculator.ApplySoftCap(total, realm);
            EquivalentValue = ConcentrationCalculator.CalcEquivalentValue(capped, CurrentRadius);

            if (profile != null)
            {
                profile.CultivationArrayValue = EquivalentValue;
                profile.ArrayConcentration = total;
                profile.ArrayDominantElement = dominant;
                profile.ArrayIsChaotic = chaotic;
            }

            var bt = BreakthroughManager.Instance;
            if (bt != null)
            {
                float regenBonus = EquivalentValue * BreakthroughManager.BaseManaRegen * 0.5f;
                bt.ArrayRegenBonus = regenBonus;
            }

            OnConcentrationUpdated?.Invoke(TotalConcentration, DominantElement, EquivalentValue, IsChaotic);
        }

        // ============ 效果查询 ============
        public float GetCultivationSpeedBonus() => EquivalentValue * 1.0f;
        public float GetBreakthroughSuccessBonus() => EquivalentValue * 0.2f;
        public float GetManaRegenBonus() => EquivalentValue * 0.5f;
        public float GetAlchemySupplyPerMinute() => TotalConcentration * 0.1f;

        // ============ 存档 ============
        [Serializable]
        public class ArraySaveData
        {
            public int ArrayRadius;
            public List<SlotSaveData> SlotSaves = new List<SlotSaveData>();
            public float TotalConcentration;
            public float EquivalentValue;
            public int DominantElement;
            public int IsChaotic;
            public float AlchemySpiritPool;
            public float SpiritCrystalCount;
        }

        [Serializable]
        public class SlotSaveData
        {
            public int X;
            public int Y;
            public string ItemId;
            public int ItemType;
            public int Element;
            public int Quality;
            public int IsPositive;
            public int Quantity;
        }

        public ArraySaveData GetSaveData()
        {
            var data = new ArraySaveData
            {
                ArrayRadius = CurrentRadius,
                TotalConcentration = TotalConcentration,
                EquivalentValue = EquivalentValue,
                DominantElement = (int)DominantElement,
                IsChaotic = IsChaotic ? 1 : 0,
                AlchemySpiritPool = AlchemySpiritPool,
                SpiritCrystalCount = SpiritCrystalCount
            };
            foreach (var kvp in Slots)
            {
                var slot = kvp.Value;
                if (slot == null || !slot.HasItem) continue;
                var item = slot.Item;
                data.SlotSaves.Add(new SlotSaveData
                {
                    X = slot.Position.x,
                    Y = slot.Position.y,
                    ItemId = item.Id,
                    ItemType = (int)item.Type,
                    Element = (int)item.Element,
                    Quality = item.Quality,
                    IsPositive = item.IsPositive ? 1 : 0,
                    Quantity = item.Quantity
                });
            }
            return data;
        }

        public void LoadFromSaveData(ArraySaveData data)
        {
            if (data == null) return;
            CurrentRadius = data.ArrayRadius;
            AlchemySpiritPool = data.AlchemySpiritPool;
            SpiritCrystalCount = data.SpiritCrystalCount;
            RebuildSlots();
            foreach (var s in data.SlotSaves)
            {
                var item = new SpiritItem
                {
                    Id = s.ItemId,
                    Type = (SpiritItemType)s.ItemType,
                    Element = (ElementType)s.Element,
                    Quality = s.Quality,
                    IsPositive = s.IsPositive == 1,
                    Quantity = s.Quantity
                };
                PlaceItem(s.X, s.Y, item);
            }
            Recalculate();
        }
    }
}
