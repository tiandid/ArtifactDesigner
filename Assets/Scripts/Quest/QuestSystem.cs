using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtifactDesigner.Quest
{
    // ---------- 数据定义 ----------

    public enum QuestState { NotStarted, InProgress, Completed, Failed }

    [Serializable]
    public class QuestConfig
    {
        public string questId;          // Q001
        public string questName;        // 初战
        public string issuer;           // 引导精灵·小仙
        public string acceptCondition;  // 描述性条件（代码中按波次/事件硬编码）
        public string objectiveDesc;    // 任务目标描述
        public int expReward;
        public int stoneReward;
        public string rewardPool;       // 物品A:概率%;物品B:概率%
        // 【屏蔽规则】未经策划/制作人书面授权，禁止在 rewardPool 中写入任何物品名称或新概念。所有物品必须先通过设计文档评审并签字确认后方可入表。
        public bool autoAccept = true;
        public bool showInTracker = true;   // 是否在左侧追踪面板显示
        public string[] nextQuestIds;       // 完成后自动解锁的任务
        public string bubbleOnAccept;       // 接取时气泡文案
        public string bubbleOnComplete;     // 完成时气泡文案
        public string bubbleColorOnAccept;    // Blue / Red / Gold / White
        public string bubbleColorOnComplete; // Blue / Red / Gold / White
        public float bubbleDurationAccept = 3f;
        public float bubbleDurationComplete = 5f;
    }

    [Serializable]
    public class QuestInstance
    {
        public QuestConfig Config;
        public QuestState State = QuestState.NotStarted;
        public int CurrentProgress;
        public int TargetProgress;
        public bool RewardClaimed;

        public QuestInstance(QuestConfig config, int targetProgress = 1)
        {
            Config = config;
            TargetProgress = targetProgress;
            CurrentProgress = 0;
            State = QuestState.NotStarted;
        }

        public bool IsCompleted => State == QuestState.Completed;
        public float ProgressRatio => TargetProgress <= 0 ? 0f : Mathf.Clamp01((float)CurrentProgress / TargetProgress);
    }

    /// <summary>
    /// 任务系统（单例 MonoBehaviour）。
    /// 职责：维护任务配置表、任务实例生命周期、进度更新、奖励发放。
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        [Header("任务配置表（只读配置，运行时不修改）")]
        public List<QuestConfig> questConfigs = new List<QuestConfig>();

        private readonly Dictionary<string, QuestInstance> activeQuests = new Dictionary<string, QuestInstance>();
        private readonly HashSet<string> completedQuestIds = new HashSet<string>();

        // 事件
        public static event Action<QuestInstance> OnQuestAccepted;
        public static event Action<QuestInstance> OnQuestProgressUpdated;
        public static event Action<QuestInstance> OnQuestCompleted;

        // 运行时缓存：按波次自动接取的任务（避免每帧查表）
        private readonly Dictionary<int, List<string>> waveAcceptMap = new Dictionary<int, List<string>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildTutorialQuests();
            BuildWaveAcceptMap();
            BreakthroughManager.OnBreakthroughCompleted += OnBreakthroughFinished;
        }

        private void OnBreakthroughFinished(GoldenCoreRank rank)
        {
            // 如果 B003 正在进行，自动完成并发放突破奖励
            if (IsInProgress("B003"))
            {
                var profile = Core.PlayerProfile.Instance;
                if (profile != null)
                {
                    profile.CompleteBreakthrough(rank);
                }
                CompleteQuest("B003");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            BreakthroughManager.OnBreakthroughCompleted -= OnBreakthroughFinished;
        }

        // ============ 初始化 Q001~Q005 + B003 配置 ============
        // 【屏蔽规则】本方法内所有 rewardPool 字段严禁填入未经策划授权的物品或新概念。
        // 如需新增奖励物品，必须先提交设计变更单（CCB）并经签字确认后，方可修改 rewardPool 内容。
        // 当前 rewardPool 已全部清空，待策划补全正式物品表后再回填。
        private void BuildTutorialQuests()
        {
            questConfigs.Clear();

            // Q001 遭遇
            questConfigs.Add(new QuestConfig
            {
                questId = "Q001",
                questName = "遭遇",
                issuer = "引导精灵·小仙",
                objectiveDesc = "击败第1波出现的敌人",
                expReward = 300,
                stoneReward = 10,
                rewardPool = "基础碎片:1000;灵石:1000;新手剑:1",
                autoAccept = true,
                showInTracker = true,
                bubbleOnAccept = "前方出现敌人，准备迎战！",
                bubbleColorOnAccept = "Blue",
                bubbleDurationAccept = 3f
            });

            // Q002 确认装配（炼器教学最终步骤）
            questConfigs.Add(new QuestConfig
            {
                questId = "Q002",
                questName = "确认装配",
                issuer = "引导精灵·小仙",
                objectiveDesc = "确认法宝装配方案，系统将自动检查契合度",
                expReward = 150,
                stoneReward = 5,
                rewardPool = "",
                autoAccept = true,      // 由Q007完成后自动接取
                showInTracker = true,
                bubbleOnAccept = "最后一步：确认你的装配方案。系统将检查各部件与器胚的契合度。",
                bubbleColorOnAccept = "Blue",
                bubbleDurationAccept = 5f,
                bubbleOnComplete = "契合度检查通过！你的第一件法宝已成型，准备迎战下一波敌人。",
                bubbleColorOnComplete = "Gold",
                bubbleDurationComplete = 5f
            });

            // Q003 小试牛刀
            questConfigs.Add(new QuestConfig
            {
                questId = "Q003",
                questName = "小试牛刀",
                issuer = "引导精灵·小仙",
                objectiveDesc = "使用炼制的法宝击败第4波全部敌人",
                expReward = 250,
                stoneReward = 10,
                rewardPool = "",
                autoAccept = true,
                showInTracker = true,
                bubbleOnAccept = "注意！前方通道出现三只敌人，正朝你袭来！",
                bubbleColorOnAccept = "Red",
                bubbleDurationAccept = 3f,
                bubbleOnComplete = "很好，你已经掌握了基本战斗技巧！",
                bubbleColorOnComplete = "Gold",
                bubbleDurationComplete = 5f
            });

            // Q004 抵御攻击
            questConfigs.Add(new QuestConfig
            {
                questId = "Q004",
                questName = "抵御攻击",
                issuer = "引导精灵·小仙",
                objectiveDesc = "抵御剩下的进攻",
                expReward = 150,
                stoneReward = 10,
                rewardPool = "",
                autoAccept = true,
                showInTracker = true,
                bubbleOnAccept = "敌人持续进攻，坚守阵地！",
                bubbleColorOnAccept = "Red",
                bubbleDurationAccept = 3f
            });

            // Q005 完美首关（隐藏成就型）
            questConfigs.Add(new QuestConfig
            {
                questId = "Q005",
                questName = "完美首关",
                issuer = "系统",
                objectiveDesc = "以0死亡次数通关第1关全部10波",
                expReward = 150,
                stoneReward = 20,
                rewardPool = "",
                autoAccept = true,
                showInTracker = false,
                bubbleOnAccept = "若能不死通关全部十波，可获得额外奖励！",
                bubbleColorOnAccept = "Gold",
                bubbleDurationAccept = 5f,
                bubbleOnComplete = "完美通关！额外奖励已发放。",
                bubbleColorOnComplete = "Gold",
                bubbleDurationComplete = 5f
            });

            // Q006 初次炼器
            questConfigs.Add(new QuestConfig
            {
                questId = "Q006",
                questName = "初次炼器",
                issuer = "引导精灵·小仙",
                objectiveDesc = "从五行聚灵材料中选择一种，放入炼器炉完成初次炼制，获得对应五行灵核",
                expReward = 0,
                stoneReward = 0,
                rewardPool = "",
                autoAccept = false,     // 由Q001完成后触发
                showInTracker = true,
                nextQuestIds = new[] { "Q009" },
                bubbleOnAccept = "请选择金、木、水、火、土中的一种最低级聚灵材料，开始你的初次炼器。",
                bubbleColorOnAccept = "Blue",
                bubbleDurationAccept = 5f,
                bubbleOnComplete = "灵核已成！选择与灵核五行契合的基础器胚，将灵核装配进去。",
                bubbleColorOnComplete = "Gold",
                bubbleDurationComplete = 5f
            });

            // Q007 熔炼技能结构
            questConfigs.Add(new QuestConfig
            {
                questId = "Q007",
                questName = "熔炼技能结构",
                issuer = "引导精灵·小仙",
                objectiveDesc = "选择聚灵为负1的白色五行材料，放入炼器炉熔炼出对应属性的输出/技能结构，并在第三波战斗中存活",
                expReward = 0,
                stoneReward = 0,
                rewardPool = "",
                autoAccept = false,     // 由Q010完成后触发
                showInTracker = true,
                nextQuestIds = new[] { "Q012" },
                bubbleOnAccept = "选择一种聚灵为负的白色五行材料，准备熔炼输出/技能结构。注意：第三波将有5只敌人，请同时操纵角色与法宝作战。",
                bubbleColorOnAccept = "Blue",
                bubbleDurationAccept = 5f,
                bubbleOnComplete = "输出/技能结构熔炼完成！安装并连接功能线后，契合度升满，法宝品质提升为绿色。",
                bubbleColorOnComplete = "Gold",
                bubbleDurationComplete = 5f
            });

            // Q008 设计成型（设计课·进阶）
            questConfigs.Add(new QuestConfig
            {
                questId = "Q008",
                questName = "设计成型",
                issuer = "引导精灵·小仙",
                objectiveDesc = "用首关掉落组装1件完整器胚",
                expReward = 0,
                stoneReward = 0,
                rewardPool = "",
                autoAccept = false,     // 由Q005结算后触发
                showInTracker = true,
                nextQuestIds = new[] { "Q013" },
                bubbleOnAccept = "用战斗中获得的材料，组装一件完整的器胚吧。",
                bubbleColorOnAccept = "Blue",
                bubbleDurationAccept = 5f,
                bubbleOnComplete = "器胚已成型，只差最后一步炼制！",
                bubbleColorOnComplete = "Gold",
                bubbleDurationComplete = 3f
            });

            // Q013 首件仙器（原Q009顺延）
            questConfigs.Add(new QuestConfig
            {
                questId = "Q013",
                questName = "首件仙器",
                issuer = "引导精灵·小仙",
                objectiveDesc = "完成1次炼制，获得并装备≥白的仙器",
                expReward = 0,
                stoneReward = 0,
                rewardPool = "",
                autoAccept = true,      // 由Q008完成后自动接取
                showInTracker = true,
                bubbleOnAccept = "将器胚放入炼炉，炼制你的第一件仙器！",
                bubbleColorOnAccept = "Blue",
                bubbleDurationAccept = 5f,
                bubbleOnComplete = "恭喜你炼成了第一件仙器！它将伴随你征战四方。",
                bubbleColorOnComplete = "Gold",
                bubbleDurationComplete = 5f
            });

            // Q009 器胚装配（新）
            questConfigs.Add(new QuestConfig
            {
                questId = "Q009",
                questName = "器胚装配",
                issuer = "引导精灵·小仙",
                objectiveDesc = "选择基础器胚，将灵核放入器胚并确认装配",
                expReward = 0,
                stoneReward = 0,
                rewardPool = "",
                autoAccept = true,      // 由Q006完成后自动接取
                showInTracker = true,
                nextQuestIds = new[] { "Q010" },
                bubbleOnAccept = "选择一件基础器胚，将灵核装配进去。",
                bubbleColorOnAccept = "Blue",
                bubbleDurationAccept = 5f,
                bubbleOnComplete = "灵核已入胚。由于只有灵核，契合度较低，法宝暂时无法运转。",
                bubbleColorOnComplete = "Blue",
                bubbleDurationComplete = 5f
            });

            // Q010 推进器装配（新）
            questConfigs.Add(new QuestConfig
            {
                questId = "Q010",
                questName = "推进器装配",
                issuer = "引导精灵·小仙",
                objectiveDesc = "安装三个推进器并连接供能线，提升契合度",
                expReward = 0,
                stoneReward = 0,
                rewardPool = "",
                autoAccept = true,      // 由Q009完成后自动接取
                showInTracker = true,
                nextQuestIds = new[] { "Q007", "Q002" },
                bubbleOnAccept = "安装三个推进器并连接供能线，为法宝提供运转动力。",
                bubbleColorOnAccept = "Blue",
                bubbleDurationAccept = 5f,
                bubbleOnComplete = "供能线已连接！契合度提升，法宝初步具备运转能力。",
                bubbleColorOnComplete = "Gold",
                bubbleDurationComplete = 3f
            });

            // Q012 最终试炼（新）
            questConfigs.Add(new QuestConfig
            {
                questId = "Q012",
                questName = "最终试炼",
                issuer = "引导精灵·小仙",
                objectiveDesc = "抵御后续波次的敌人，直到第十波出现并击败Boss",
                expReward = 0,
                stoneReward = 0,
                rewardPool = "",
                autoAccept = true,      // 由Q007完成后自动接取
                showInTracker = true,
                bubbleOnAccept = "后续波次即将来袭！坚持到第十波并击败最终Boss。提示：一次不死完成全部波次有额外奖励。",
                bubbleColorOnAccept = "Red",
                bubbleDurationAccept = 5f,
                bubbleOnComplete = "恭喜通关！你已成功完成第一关的全部挑战。",
                bubbleColorOnComplete = "Gold",
                bubbleDurationComplete = 5f
            });

            // B003 筑基→金丹突破（0.0.3 新增）
            // 触发条件：筑基10级满经验 + 玩家主动触发，非波次自动接取
            questConfigs.Add(new QuestConfig
            {
                questId = "B003",
                questName = "结丹突破",
                issuer = "天道",
                objectiveDesc = "完成金丹品级双轨突破（最低九品，最高超品）",
                expReward = 0,          // 突破本身无经验奖励，奖励由突破品级决定
                stoneReward = 150,      // 等级30 × 5
                rewardPool = "",
                autoAccept = false,     // 需玩家主动触发
                showInTracker = true,
                bubbleOnAccept = "筑基已满，是时候凝结金丹了！",
                bubbleColorOnAccept = "Gold",
                bubbleDurationAccept = 5f,
                bubbleOnComplete = "金丹已成，道基稳固！",
                bubbleColorOnComplete = "Gold",
                bubbleDurationComplete = 5f
            });
        }

        private void BuildWaveAcceptMap()
        {
            waveAcceptMap.Clear();
            foreach (var cfg in questConfigs)
            {
                int wave = 0;
                // Q001 -> 第1波
                if (cfg.questId == "Q001") wave = 1;
                // Q003 -> 第4波（原第2波，现移至第4波避免与Q006冲突）
                else if (cfg.questId == "Q003") wave = 4;
                // Q004 -> 第3波
                else if (cfg.questId == "Q004") wave = 3;
                // Q002 由 Q007 完成触发（炼器教学最终步骤），不绑定波次
                // Q005 由关卡开始时触发

                if (wave > 0)
                {
                    if (!waveAcceptMap.TryGetValue(wave, out var list))
                    {
                        list = new List<string>();
                        waveAcceptMap[wave] = list;
                    }
                    list.Add(cfg.questId);
                }
            }
        }

        // ============ 接取 ============
        public void AcceptQuest(string questId, int targetProgress = 1)
        {
            if (activeQuests.ContainsKey(questId) || completedQuestIds.Contains(questId))
                return;

            var cfg = questConfigs.Find(q => q.questId == questId);
            if (cfg == null) return;

            var inst = new QuestInstance(cfg, targetProgress);
            inst.State = QuestState.InProgress;
            activeQuests[questId] = inst;

            OnQuestAccepted?.Invoke(inst);
            GameEvents.RaiseQuestAccepted(inst);

            // 自动弹出接取气泡
            var bs = BubbleSystem.Instance;
            if (bs != null && !string.IsNullOrEmpty(inst.Config.bubbleOnAccept))
            {
                bs.Show(inst.Config.bubbleOnAccept, inst.Config.bubbleColorOnAccept, inst.Config.bubbleDurationAccept);
            }
        }

        /// <summary>波次开始时检查是否有任务需要自动接取。</summary>
        public void OnWaveStarted(int waveIndex)
        {
            // 波次绑定任务
            if (waveAcceptMap.TryGetValue(waveIndex, out var ids))
            {
                foreach (var id in ids) AcceptQuest(id);
            }

            // Q005 首关开始时静默接取（仅在第1关第1波开始时）
            if (waveIndex == 1 && !completedQuestIds.Contains("Q005") && !activeQuests.ContainsKey("Q005"))
            {
                AcceptQuest("Q005", targetProgress: 1);
            }
        }

        /// <summary>前置任务完成时自动接取后续任务。</summary>
        public void OnQuestCompletedAutoChain(string completedQuestId)
        {
            var cfg = questConfigs.Find(q => q.questId == completedQuestId);
            if (cfg?.nextQuestIds == null) return;
            foreach (var nextId in cfg.nextQuestIds)
            {
                if (!activeQuests.ContainsKey(nextId) && !completedQuestIds.Contains(nextId))
                {
                    AcceptQuest(nextId);
                }
            }
        }

        // ============ 进度 ============
        public void AddProgress(string questId, int amount = 1)
        {
            if (!activeQuests.TryGetValue(questId, out var inst)) return;
            if (inst.State != QuestState.InProgress) return;

            inst.CurrentProgress += amount;
            OnQuestProgressUpdated?.Invoke(inst);
            GameEvents.RaiseQuestProgressUpdated(inst);

            if (inst.CurrentProgress >= inst.TargetProgress)
            {
                CompleteQuest(questId);
            }
        }

        public void SetProgress(string questId, int value)
        {
            if (!activeQuests.TryGetValue(questId, out var inst)) return;
            if (inst.State != QuestState.InProgress) return;

            inst.CurrentProgress = value;
            OnQuestProgressUpdated?.Invoke(inst);

            if (inst.CurrentProgress >= inst.TargetProgress)
            {
                CompleteQuest(questId);
            }
        }

        public QuestInstance GetQuest(string questId)
        {
            activeQuests.TryGetValue(questId, out var inst);
            return inst;
        }

        public bool IsInProgress(string questId) => activeQuests.TryGetValue(questId, out var q) && q.State == QuestState.InProgress;
        public bool IsCompleted(string questId) => completedQuestIds.Contains(questId);

        // ============ 完成 / 奖励 ============
        public void CompleteQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out var inst)) return;
            if (inst.State == QuestState.Completed) return;

            inst.State = QuestState.Completed;
            inst.CurrentProgress = inst.TargetProgress;
            activeQuests.Remove(questId);
            completedQuestIds.Add(questId);

            // 发放奖励
            var profile = Core.PlayerProfile.Instance;
            if (profile != null)
            {
                // 支线任务动态经验公式：10% × 当前升级所需经验 + 25 × 关卡数 × 聚灵阵值
                long expReward = inst.Config.expReward;
                if (questId.StartsWith("Q"))
                {
                    long expToNext = Core.ExpSystem.GetExpRequired(profile.Level);
                    int levelNumber = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
                    float arrayValue = profile.CultivationArrayValue;
                    expReward = Math.Max(1L, (long)(expToNext * 0.1f + 25 * levelNumber * (arrayValue + 1)));
                }
                profile.AddExp(expReward);
                profile.AddSpiritStones(inst.Config.stoneReward);
            }

            OnQuestCompleted?.Invoke(inst);
            GameEvents.RaiseQuestCompleted(inst);

            // Q001（第1波战斗）完成后，自动进入初次炼器教学
            if (questId == "Q001")
            {
                AcceptQuest("Q006", targetProgress: 1);
            }

            // 完成气泡
            var bs = BubbleSystem.Instance;
            if (bs != null && !string.IsNullOrEmpty(inst.Config.bubbleOnComplete))
            {
                bs.Show(inst.Config.bubbleOnComplete, inst.Config.bubbleColorOnComplete, inst.Config.bubbleDurationComplete);
            }

            OnQuestCompletedAutoChain(questId);
        }

        /// <summary>关卡结束时结算 Q005（完美首关）并触发 Q008 接取。</summary>
        public void OnLevelEnd(bool victory, int deathCount)
        {
            var q005 = GetQuest("Q005");
            if (q005 != null && q005.State == QuestState.InProgress)
            {
                if (victory && deathCount == 0)
                {
                    CompleteQuest("Q005");
                }
                else
                {
                    // 不完美：标记失败（但不从activeQuests移除，保持一次记录）
                    q005.State = QuestState.Failed;
                    activeQuests.Remove("Q005");
                }
            }

            // 第1关通关后（无论是否完美），若 Q008 未接取且未做过，自动接取设计课·进阶
            if (victory && !completedQuestIds.Contains("Q008") && !activeQuests.ContainsKey("Q008"))
            {
                AcceptQuest("Q008", targetProgress: 1);
            }
        }

        // ============ 查询 ============
        public List<QuestInstance> GetTrackedQuests(int maxCount = 3)
        {
            var list = new List<QuestInstance>();
            foreach (var kv in activeQuests)
            {
                if (kv.Value.Config.showInTracker && kv.Value.State == QuestState.InProgress)
                    list.Add(kv.Value);
                if (list.Count >= maxCount) break;
            }
            return list;
        }
        // ============ Q006~Q009 设计课触发接口 ============

        /// <summary>首次进入炼器界面时调用（由炼器UI系统触发）。</summary>
        public void OnFirstEnterArtifactCraft()
        {
            if (!completedQuestIds.Contains("Q006") && !activeQuests.ContainsKey("Q006"))
            {
                AcceptQuest("Q006", targetProgress: 1);
            }
        }

        /// <summary>安装核心完成时调用（由炼器系统触发）。</summary>
        public void OnCoreInstalled()
        {
            if (IsInProgress("Q006"))
            {
                CompleteQuest("Q006");
            }
        }

        /// <summary>完成一条能量连接时调用（由炼器系统触发）。</summary>
        public void OnEnergyLinkCompleted()
        {
            if (IsInProgress("Q007"))
            {
                CompleteQuest("Q007");
            }
        }

        /// <summary>组装完整器胚完成时调用（由炼器系统触发）。</summary>
        public void OnArtifactAssembled()
        {
            if (IsInProgress("Q008"))
            {
                CompleteQuest("Q008");
            }
        }

        /// <summary>完成一次炼制并获得仙器时调用（由炼器系统触发）。现对应Q013。</summary>
        public void OnArtifactCrafted()
        {
            if (IsInProgress("Q013"))
            {
                CompleteQuest("Q013");
            }
        }
        /// <summary>
        /// 确认装配并检查契合度完成时调用（由炼器系统触发）。
        /// </summary>
        public void OnArtifactAssemblyConfirmed()
        {
            if (IsInProgress("Q002"))
            {
                CompleteQuest("Q002");
            }
        }

        /// <summary>初次炼器完成、灵核产出时调用（由炼器系统+波次系统联合触发）。</summary>
        public void OnFirstCraftCompleted()
        {
            if (IsInProgress("Q006"))
            {
                CompleteQuest("Q006");
            }
        }

        /// <summary>灵核装入器胚并确认后调用（由炼器系统触发）。</summary>
        public void OnCoreEmbeded()
        {
            if (IsInProgress("Q009"))
            {
                CompleteQuest("Q009");
            }
        }

        /// <summary>推进器供能线连接完成并确认后调用（由炼器系统触发）。</summary>
        public void OnThrusterLinkCompleted()
        {
            if (IsInProgress("Q010"))
            {
                CompleteQuest("Q010");
            }
        }

        /// <summary>第10波Boss被击败时调用（由战斗系统触发）。</summary>
        public void OnFinalBossDefeated()
        {
            if (IsInProgress("Q012"))
            {
                CompleteQuest("Q012");
            }
        }
    }
}
