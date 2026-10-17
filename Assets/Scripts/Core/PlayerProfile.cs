using System;
using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Entity;

namespace ArtifactDesigner.Core
{
    /// <summary>
    /// 玩家局外档案：等级、经验、灵石、死亡统计、三维属性、永久加成等。
    /// 挂在 GameManager 同物体上，单例访问。
    /// </summary>
    public class PlayerProfile
    {
        public static PlayerProfile Instance { get; private set; } = new PlayerProfile();

        // ---------- 等级 / 经验 ----------
        public int Level { get; private set; } = 1;          // 当前等级 1~100
        public long Exp { get; private set; } = 0;            // 当前累计经验（本级已拥有）
        public Realm CurrentRealm => ExpSystem.GetRealmByLevel(Level);

        // ---------- 货币 / 统计 ----------
        public int SpiritStones { get; private set; } = 0;   // 灵石
        public int TotalDeaths { get; set; }                 // 本局死亡次数（局内累计）

        // ---------- 聚灵阵（0.0.7 真实接入） ----------
        public float CultivationArrayValue { get; set; } = 0f;  // 当前聚灵阵等效值（局外成长）
        public float ArrayConcentration { get; set; } = 0f;     // 阵眼聚灵浓度原始值
        public ElementType ArrayDominantElement { get; set; } = ElementType.Metal; // 主导五行
        public bool ArrayIsChaotic { get; set; } = true;        // 是否为混沌灵气

        // ---------- 属性（局外成长后的面板值，局内再按 FormulaHelper 换算） ----------
        public int Body { get; private set; } = 5;            // 体
        public int Qi { get; private set; } = 5;             // 气
        public int Hun { get; private set; } = 5;            // 魂（原"神"，已全局改名）

        // ---------- 血量 / 蓝量 ----------
        public int MaxHp => FormulaHelper.PlayerMaxHp(Body);
        public int MaxMana => FormulaHelper.PlayerMaxMana(Qi);
        public int CurrentHp { get; set; } = 0;   // 局内实时（局外不持久化当前值）
        public int CurrentMana { get; set; } = 0;  // 局内实时

        // ---------- 永久属性加成（突破奖励等） ----------
        public PermanentStats PermanentStats { get; private set; } = new PermanentStats();

        // ---------- 突破状态（0.0.3 新增） ----------
        /// <summary>当前是否有待完成的境界突破（等级达到境界切换点但未突破）。</summary>
        public bool IsPendingBreakthrough { get; private set; } = false;

        /// <summary>当前待突破任务ID（如 B003）。</summary>
        public string CurrentBreakthroughQuestId { get; private set; } = string.Empty;

        /// <summary>最近一次突破达成的金丹/元婴品级（局外持久化）。</summary>
        public GoldenCoreRank LastBreakthroughRank { get; private set; } = GoldenCoreRank.Rank9;

        // ---------- 事件 ----------
        public static event Action<int, long> OnExpChanged;     // 等级, 本级经验
        public static event Action<int> OnLevelUp;                // 新等级
        public static event Action<int> OnSpiritStoneChanged;     // 新数量
        public static event Action<bool> OnBreakthroughPendingChanged; // 是否有待突破

        // ---------- 初始化 ----------
        public void ResetRunStats()
        {
            TotalDeaths = 0;
        }

        public void SetBreakthroughPending(bool pending, string questId = "")
        {
            IsPendingBreakthrough = pending;
            CurrentBreakthroughQuestId = pending ? questId : string.Empty;
            OnBreakthroughPendingChanged?.Invoke(pending);
        }

        public void CompleteBreakthrough(GoldenCoreRank rank)
        {
            LastBreakthroughRank = rank;
            IsPendingBreakthrough = false;
            CurrentBreakthroughQuestId = string.Empty;
            // 应用突破奖励到永久属性
            PermanentStats.ApplyBreakthroughRewards(rank);
        }

        public void SetBreakthroughRank(GoldenCoreRank rank)
        {
            LastBreakthroughRank = rank;
        }

        // ---------- 属性点分配 ----------
        public void AddBody(int amount) { Body = Math.Max(0, Body + amount); }
        public void AddQi(int amount) { Qi = Math.Max(0, Qi + amount); }
        public void AddHun(int amount) { Hun = Math.Max(0, Hun + amount); }

        // ---------- 经验 ----------
        public void AddExp(long amount)
        {
            if (amount <= 0) return;
            Exp += amount;
            OnExpChanged?.Invoke(Level, Exp);
            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            long need = ExpSystem.GetExpRequired(Level);
            while (need > 0 && Exp >= need)
            {
                Exp -= need;
                Level++;
                if (Level > 100)
                {
                    Level = 100;
                    Exp = 0;
                    break;
                }
                OnLevelUp?.Invoke(Level);
                need = ExpSystem.GetExpRequired(Level);
            }
            OnExpChanged?.Invoke(Level, Exp);
        }

        // ---------- 灵石 ----------
        public void AddSpiritStones(int amount)
        {
            if (amount == 0) return;
            SpiritStones = Math.Max(0, SpiritStones + amount);
            OnSpiritStoneChanged?.Invoke(SpiritStones);
        }

        // ---------- 属性成长 ----------
        private void RecalcAttributes()
        {
            // 凡人境 1~10 级：体/气/魂 每级+1，起始值=5
            if (Level <= 10)
            {
                Body = 4 + Level;
                Qi = 4 + Level;
                Hun = 4 + Level;
            }
            else if (Level <= 20) // 练气境
            {
                Body = 14 + (Level - 10) * 2;
                Qi = 14 + (Level - 10) * 2;
                Hun = 14 + (Level - 10) * 2;
            }
            else
            {
                // 更高境界占位，后续按设计文档扩展
                Body = 34;
                Qi = 34;
                Hun = 34;
            }
        }

        /// <summary>获取本级经验条进度 0~1。</summary>
        public float GetLevelProgress()
        {
            long need = ExpSystem.GetExpRequired(Level);
            return need <= 0 ? 0f : Mathf.Clamp01((float)Exp / need);
        }

        /// <summary>复制当前属性给局内玩家实体。</summary>
        public void ApplyToPlayer(PlayerEntity player)
        {
            if (player == null) return;
            player.Init(
                FormulaHelper.PlayerMaxHp(Body),
                FormulaHelper.BaseAttack(Body),
                FormulaHelper.BaseDefense(Body),
                FormulaHelper.PlayerAttackSpeed,
                FormulaHelper.PlayerMoveSpeed,
                attackRange: 3f,
                mana: FormulaHelper.PlayerMaxMana(Qi));
            // 同步蓝量（0.0.3 新增）
            player.maxMana = FormulaHelper.PlayerMaxMana(Qi);
            player.currentMana = player.maxMana;
        }
    }
}
