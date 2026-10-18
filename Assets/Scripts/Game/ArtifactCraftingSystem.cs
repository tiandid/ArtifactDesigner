using System;
using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Artifact;
using ArtifactDesigner.Core;
using ArtifactDesigner.Energy;
using ArtifactDesigner.Quest;
using ArtifactDesigner;

namespace ArtifactDesigner.Game
{
    /// <summary>
    /// 炼器系统核心控制器（单例 MonoBehaviour）。
    /// 0.0.4 最小闭环：支撑 Q006~Q010 任务链的交互逻辑。
    /// 职责：管理材料选择、炼制流程、器胚装配、契合度计算、供能线连接。
    /// 不依赖完整背包/掉落系统，教学所需材料直接生成。
    /// </summary>
    public class ArtifactCraftingSystem : MonoBehaviour
    {
        public static ArtifactCraftingSystem Instance { get; private set; }

        // ---------- 当前炼制状态 ----------
        public ElementType? SelectedElement { get; private set; }
        public int SelectedQuality { get; private set; } = 1; // 1=白, 2=蓝...

        // 玩家当前拥有的炼器产物（局内临时，不持久化到 PlayerProfile）
        public ArtifactPart CurrentSpiritCore { get; private set; }
        public ArtifactBlank CurrentBlank { get; set; }
        public List<ArtifactPart> CurrentThrusters { get; private set; } = new List<ArtifactPart>();
        public ArtifactPart CurrentSkillStructure { get; private set; }
        public bool EnergyLineConnected { get; private set; }
        public EnergyNetwork CurrentEnergyNetwork { get; private set; } // 0.0.5：当前供能网络（供契合度系统读取）

        // 炼制进度
        public bool IsCrafting { get; private set; }
        public float CraftProgress { get; private set; } // 0~1
        private float craftTimer;
        private float craftDuration = 2f;

        // 事件
        public static event Action OnCraftFinished;
        public static event Action<float> OnCraftProgressChanged;

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

        private void Update()
        {
            if (IsCrafting)
            {
                craftTimer -= Time.deltaTime;
                CraftProgress = 1f - Mathf.Clamp01(craftTimer / craftDuration);
                OnCraftProgressChanged?.Invoke(CraftProgress);
                if (craftTimer <= 0f)
                {
                    IsCrafting = false;
                    CompleteCraft();
                }
            }
        }

        // ========== Q006：初次炼器 —— 产出灵核 ==========

        /// <summary>选择五行材料（品质固定为白=1，除非外部指定）。</summary>
        public void SelectMaterial(ElementType element, int quality = 1)
        {
            SelectedElement = element;
            SelectedQuality = Mathf.Clamp(quality, 1, 6);
        }

        /// <summary>确认开始炼制（校验材料 → 进入炼制倒计时 → 产出）。</summary>
        public bool ConfirmCraft()
        {
            if (!SelectedElement.HasValue) return false;
            if (IsCrafting) return false;

            IsCrafting = true;
            craftTimer = craftDuration;
            CraftProgress = 0f;
            return true;
        }

        private void CompleteCraft()
        {
            // 产出对应五行的灵核
            string elementName = GetElementName(SelectedElement.Value);
            CurrentSpiritCore = new ArtifactPart
            {
                partId = $"spirit_core_{SelectedElement.Value}_q{SelectedQuality}",
                partName = $"{elementName}灵核",
                partType = PartType.SpiritCore,
                rarity = (Rarity)(SelectedQuality - 1), // 白=0 (Common)
                weight = 1,
                spiritOutput = SelectedQuality * 10,
                description = $"{elementName}属性灵核，输出 {SelectedQuality * 10} 点灵气，需线路连接技能部件。"
            };

            // 同时发放一个基础器胚（供 Q009 使用）
            if (CurrentBlank == null)
            {
                CurrentBlank = new ArtifactBlank("blank_basic", "基础器胚", baseSlots: 4, baseWeight: 5, weightPerSlot: 2);
            }

            OnCraftFinished?.Invoke();

            // 通知任务系统：初次炼器完成
            var qs = QuestSystem.Instance;
            if (qs != null) qs.OnFirstCraftCompleted();

            // 若已开放第 2 波，触发之（0.0.5 后 maxWave 提升时生效）
            var wave = WaveManager.Instance;
            if (wave != null && wave.maxWave >= 2 && wave.CurrentWave < 2)
            {
                wave.StartWave(2);
            }
        }

        // ========== Q009：器胚装配 —— 灵核入胚 ==========

        /// <summary>将已产出的灵核装入当前器胚的第一个空槽位。</summary>
        public bool EmbedCore()
        {
            if (CurrentBlank == null || CurrentSpiritCore == null) return false;
            if (CurrentBlank.FreeSlots <= 0) return false;

            bool ok = CurrentBlank.TryMountPart(CurrentSpiritCore);
            if (!ok) return false;

            // 触发任务
            var qs = QuestSystem.Instance;
            if (qs != null) qs.OnCoreEmbeded();
            return true;
        }

        // ========== Q010：推进器装配 —— 3 推进器 + 供能线 ==========

        /// <summary>生成并安装推进器（Speed 类型部件）到器胚。</summary>
        public bool InstallThrusters(int count = 3)
        {
            if (CurrentBlank == null) return false;

            for (int i = 0; i < count; i++)
            {
                if (CurrentBlank.FreeSlots <= 0) break;
                var thruster = new ArtifactPart
                {
                    partId = $"thruster_{CurrentThrusters.Count}",
                    partName = "推进器",
                    partType = PartType.Speed,
                    rarity = Rarity.Common,
                    weight = 1,
                    addSpeed = 0.2f,
                    description = "提升法宝运转速度的推进器。"
                };
                CurrentBlank.TryMountPart(thruster);
                CurrentThrusters.Add(thruster);
            }
            return CurrentThrusters.Count >= count;
        }

        /// <summary>连接供能线（最小实现：灵核 → 所有已挂载推进器 的虚拟能量网络）。</summary>
        public bool ConnectEnergyLine()
        {
            if (CurrentBlank == null || CurrentSpiritCore == null) return false;
            if (CurrentThrusters.Count == 0) return false;

            // 构建能量网络：灵核为 Core，推进器为 Consumer
            var network = new EnergyNetwork();
            var coreNode = new EnergyNode("core_0", EnergyNodeType.SpiritCore, CurrentSpiritCore.spiritOutput, CurrentSpiritCore.partId);
            network.AddNode(coreNode);

            for (int i = 0; i < CurrentThrusters.Count; i++)
            {
                var t = CurrentThrusters[i];
                var consumerNode = new EnergyNode($"thruster_{i}", EnergyNodeType.Consumer, 5, t.partId);
                network.AddNode(consumerNode);
                network.AddLink(new EnergyLink($"link_{i}", "core_0", $"thruster_{i}"));
            }

            // 校验是否全部供电
            bool allPowered = true;
            for (int i = 0; i < CurrentThrusters.Count; i++)
            {
                if (!network.IsPowered($"thruster_{i}"))
                {
                    allPowered = false;
                    break;
                }
            }

            EnergyLineConnected = allPowered;
            CurrentEnergyNetwork = network; // 0.0.5：保存供契合度系统读取
            if (allPowered)
            {
                var qs = QuestSystem.Instance;
                if (qs != null) qs.OnThrusterLinkCompleted();
            }
            return allPowered;
        }

        /// <summary>获取当前器胚的能量网络（供契合度系统使用）。</summary>
        public EnergyNetwork GetEnergyNetwork()
        {
            return CurrentEnergyNetwork;
        }

        // ========== Q007：熔炼技能结构（简化占位） ==========

        /// <summary>熔炼技能结构：选择负聚灵白色材料 → 产出 Attack/ActiveSkill 部件。</summary>
        public bool CraftSkillStructure(ElementType element, int quality = 1)
        {
            if (CurrentBlank == null) return false;
            if (CurrentBlank.FreeSlots <= 0) return false;

            var skillPart = new ArtifactPart
            {
                partId = $"skill_struct_{element}_q{quality}",
                partName = $"{GetElementName(element)}输出结构",
                partType = PartType.Attack,
                rarity = Rarity.Common,
                weight = 1,
                addAttack = 10 + quality * 5,
                description = "由负聚灵材料熔炼而成的攻击结构部件。"
            };

            CurrentSkillStructure = skillPart;
            CurrentBlank.TryMountPart(skillPart);

            // 触发任务（Q007）
            var qs = QuestSystem.Instance;
            if (qs != null)
            {
                // Q007 的完成条件还包含"第三波存活"，这里仅完成炼制部分；
                // 波次存活由 GameManager / 任务系统另行判断。
                // 为简化 0.0.4 闭环，直接调用已有的 OnEnergyLinkCompleted 作为 Q007 完成信号
                //（若 Q007 目标进度为 1，则此处可直接推进；否则需额外逻辑）
                qs.AddProgress("Q007", 1);
            }
            return true;
        }

        // ========== Q002 / Q013：装配确认 / 炼制仙器 ==========

        /// <summary>检查当前器胚的契合度（占位公式）。</summary>
        public float CalculateFitScore()
        {
            if (CurrentBlank == null) return 0f;
            float score = 0f;
            // 有灵核 +30%
            if (CurrentSpiritCore != null && CurrentBlank.GetMountedParts().Contains(CurrentSpiritCore)) score += 0.30f;
            // 有推进器：每个 +10%，最多 +30%
            score += Mathf.Min(CurrentThrusters.Count, 3) * 0.10f;
            // 供能线连接 +20%
            if (EnergyLineConnected) score += 0.20f;
            // 有技能结构 +20%
            if (CurrentSkillStructure != null) score += 0.20f;
            return Mathf.Clamp01(score);
        }

        /// <summary>确认装配方案（Q002 触发）。0.0.5 接入契合度评级系统。</summary>
        public bool ConfirmAssembly()
        {
            float fit = CalculateFitScore();
            // 0.0.4 占位：只要有灵核+推进器+供能线即视为通过
            bool passed = fit >= 0.5f;
            if (passed)
            {
                // 0.0.5：调用契合度系统进行完整品质判定与进阶
                var compat = ArtifactCompatibilitySystem.Instance;
                if (compat != null && CurrentBlank != null)
                {
                    compat.TryUpgradeQuality(CurrentBlank, CurrentBlank.blankName, GetEnergyNetwork());
                }

                var qs = QuestSystem.Instance;
                if (qs != null) qs.OnArtifactAssemblyConfirmed();
            }
            return passed;
        }

        /// <summary>完成仙器炼制（Q013 触发）。0.0.5 接入契合度评级进行最终品质判定。</summary>
        public bool CraftArtifact()
        {
            if (CurrentBlank == null) return false;
            // 占位：器胚+灵核+至少1个推进器+供能线 即视为完整仙器
            bool ready = CurrentSpiritCore != null && CurrentThrusters.Count > 0 && EnergyLineConnected;
            if (ready)
            {
                // 0.0.5：最终契合度判定，确定法宝最终品质
                var compat = ArtifactCompatibilitySystem.Instance;
                if (compat != null)
                {
                    bool upgraded = compat.TryUpgradeQuality(CurrentBlank, CurrentBlank.blankName, GetEnergyNetwork());
                    if (upgraded)
                    {
                        Debug.Log($"[ArtifactCraftingSystem] 仙器品质提升至 {CurrentBlank.rarity}");
                    }
                }

                var qs = QuestSystem.Instance;
                if (qs != null) qs.OnArtifactCrafted();
            }
            return ready;
        }

        // ========== 辅助 ==========

        public static string GetElementName(ElementType element)
        {
            switch (element)
            {
                case ElementType.Metal: return "金";
                case ElementType.Wood: return "木";
                case ElementType.Water: return "水";
                case ElementType.Fire: return "火";
                case ElementType.Earth: return "土";
                default: return "未知";
            }
        }

        public void ResetCraftingState()
        {
            SelectedElement = null;
            SelectedQuality = 1;
            IsCrafting = false;
            CraftProgress = 0f;
        }
    }
}
