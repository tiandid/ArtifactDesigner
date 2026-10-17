using System;
using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Artifact;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Game
{
    /// <summary>
    /// 灵石经济系统（单例 MonoBehaviour）。
    /// 0.0.5 新增：统一管理局内灵石的收入与支出，所有灵石变动必须通过本系统接口完成，
    /// 以便统计、校验与事件广播。
    /// </summary>
    public class SpiritEconomySystem : MonoBehaviour
    {
        public static SpiritEconomySystem Instance { get; private set; }

        // ---------- 事件 ----------
        public static event Action<int, int> OnBalanceChanged;       // (新余额, 变动量)
        public static event Action<string, int> OnIncome;          // (来源, 数量)
        public static event Action<string, int> OnExpense;         // (用途, 数量)

        // ---------- 配置：收入基准 ----------
        [Header("收入配置")]
        public int baseKillReward = 5;          // 第1波小怪基础掉落
        public float killRewardWaveScale = 1.5f; // 每波掉落倍率
        public int[] levelClearReward = { 50, 100, 150, 200, 300, 500, 700, 1000, 1500, 3000 }; // 通关奖励（按评分档位F~SSS，索引0~8）
        public float sellPartRate = 0.3f;        // 出售部件回收比例（部件价值的30%）

        // ---------- 配置：支出基准 ----------
        [Header("支出配置")]
        public int materialCostBase = 20;        // 白品质材料单价
        public float materialCostQualityScale = 2.0f; // 每提升一档品质，单价×2
        public int repairCostPerDurability = 2;  // 每点耐久修复消耗
        public int arraySlotCostBase = 100;      // 聚灵阵扩展第一格成本
        public float arraySlotCostScale = 1.5f;  // 每扩展一格成本×1.5
        public int[] breakthroughCost = { 0, 500, 2000, 8000, 30000, 100000, 300000, 800000, 2000000, 5000000 }; // 各境界突破消耗（凡人→渡劫，索引0~9）

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ==================== 收入接口 ====================

        /// <summary>击杀敌人获得灵石（随波次与敌人品质递增）。</summary>
        public void AddKillReward(int waveIndex, Rarity enemyRarity = Rarity.Common)
        {
            int baseReward = Mathf.RoundToInt(baseKillReward * Mathf.Pow(killRewardWaveScale, waveIndex - 1));
            int qualityBonus = (int)enemyRarity * 5; // 品质每高一档+5
            int total = baseReward + qualityBonus;
            Add(total, $"击杀_波{waveIndex}");
        }

        /// <summary>任务奖励灵石（直接调用任务配置中的 stoneReward）。</summary>
        public void AddQuestReward(int stoneAmount, string questId = "")
        {
            if (stoneAmount <= 0) return;
            Add(stoneAmount, $"任务_{questId}");
        }

        /// <summary>关卡通关奖励（按评分档位发放）。</summary>
        public void AddLevelClearReward(int scoreGradeIndex)
        {
            int idx = Mathf.Clamp(scoreGradeIndex, 0, levelClearReward.Length - 1);
            int reward = levelClearReward[idx];
            Add(reward, "通关奖励");
        }

        /// <summary>出售部件获得灵石（回收比例由 sellPartRate 控制）。</summary>
        public int SellArtifactPart(ArtifactPart part)
        {
            if (part == null) return 0;
            int value = CalculatePartValue(part);
            int refund = Mathf.RoundToInt(value * sellPartRate);
            Add(refund, "出售部件");
            return refund;
        }

        /// <summary>出售完整仙器获得灵石（所有部件价值之和×回收比例）。</summary>
        public int SellArtifact(ArtifactBlank blank)
        {
            if (blank == null) return 0;
            int totalValue = 0;
            foreach (var p in blank.slots)
            {
                if (p != null) totalValue += CalculatePartValue(p);
            }
            int refund = Mathf.RoundToInt(totalValue * sellPartRate);
            Add(refund, "出售仙器");
            return refund;
        }

        // ==================== 支出接口 ====================

        /// <summary>购买炼器材料（五行材料按品质计价）。成功返回true并扣款，余额不足返回false。</summary>
        public bool SpendForMaterial(ElementType element, Rarity quality, int amount = 1)
        {
            if (amount <= 0) return true;
            int unitCost = Mathf.RoundToInt(materialCostBase * Mathf.Pow(materialCostQualityScale, (int)quality));
            int total = unitCost * amount;
            return TryDeduct(total, $"购买材料_{element}_{quality}");
        }

        /// <summary>修复部件耐久。</summary>
        public bool SpendForRepair(ArtifactPart part, int durabilityToRestore)
        {
            if (part == null || durabilityToRestore <= 0) return true;
            int cost = durabilityToRestore * repairCostPerDurability;
            return TryDeduct(cost, "修复部件");
        }

        /// <summary>聚灵阵扩展格位（第m格成本 = base × scale^(m-1)）。</summary>
        public bool SpendForArrayUpgrade(int currentSlotCount, int expandCount = 1)
        {
            if (expandCount <= 0) return true;
            int totalCost = 0;
            for (int i = 0; i < expandCount; i++)
            {
                int m = currentSlotCount + i + 1; // 第m格
                totalCost += Mathf.RoundToInt(arraySlotCostBase * Mathf.Pow(arraySlotCostScale, m - 1));
            }
            return TryDeduct(totalCost, "聚灵阵扩展");
        }

        /// <summary>境界突破消耗。</summary>
        public bool SpendForBreakthrough(Realm targetRealm)
        {
            int idx = (int)targetRealm;
            if (idx < 0 || idx >= breakthroughCost.Length) return false;
            return TryDeduct(breakthroughCost[idx], $"突破_{targetRealm}");
        }

        // ==================== 内部工具 ====================

        private void Add(int amount, string source)
        {
            var profile = PlayerProfile.Instance;
            if (profile == null) return;
            int before = profile.SpiritStones;
            profile.AddSpiritStones(amount);
            int after = profile.SpiritStones;
            OnBalanceChanged?.Invoke(after, after - before);
            OnIncome?.Invoke(source, amount);
        }

        private bool TryDeduct(int amount, string usage)
        {
            var profile = PlayerProfile.Instance;
            if (profile == null) return false;
            if (profile.SpiritStones < amount) return false;
            int before = profile.SpiritStones;
            profile.AddSpiritStones(-amount);
            int after = profile.SpiritStones;
            OnBalanceChanged?.Invoke(after, after - before);
            OnExpense?.Invoke(usage, amount);
            return true;
        }

        /// <summary>计算部件基础价值（用于出售回收）。</summary>
        public int CalculatePartValue(ArtifactPart part)
        {
            if (part == null) return 0;
            int baseValue = 10;
            int qualityMult = (int)part.rarity + 1; // 白=1, 蓝=2...
            int typeValue = part.partType switch
            {
                PartType.SpiritCore => 50,
                PartType.ActiveSkill => 30,
                PartType.PassiveSkill => 25,
                PartType.Attack => 20,
                PartType.Speed => 15,
                PartType.AddSlot => 10,
                PartType.Durability => 8,
                _ => 10
            };
            return baseValue + typeValue * qualityMult;
        }
    }
}
