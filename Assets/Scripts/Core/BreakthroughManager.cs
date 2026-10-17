using System;
using UnityEngine;

namespace ArtifactDesigner.Core
{
    /// <summary>
    /// 双轨突破管理器（单例 MonoBehaviour）。
    /// 负责 B003 及以后涉及金丹/元婴/元神品级的突破流程。
    /// 
    /// 双轨机制：
    /// 1. 资源轨：依次充能 九品→八品→…→一品 共9条进度条，超品不设独立进度条。
    ///    先扣蓝条（气/法力），蓝空后扣血条（体/生命），血保留1点。
    /// 2. 时间轨：倒计时从玩家魂属性值倒数至0，归零强制结束。
    ///    先到者决定最终品级。
    /// 
    /// 进度条充能速度固定 = 15 灵气/秒，与回复无关。
    /// 回复仅影响资源消耗结构（蓝条撑更久，少掉血）。
    /// </summary>
    public class BreakthroughManager : MonoBehaviour
    {
        public static BreakthroughManager Instance { get; private set; }

        // ---------- 设计常数（与 design_golden_core_breakthrough_v3.md 同步） ----------
        /// <summary>进度条基础消耗速率 = 15 灵气/秒。</summary>
        public const float BaseDrainPerSecond = 15f;

        /// <summary>蓝条自然回复 = 5 灵气/秒（基础）。</summary>
        public const float BaseManaRegen = 5f;

        /// <summary>聚灵阵映射系数 = 0.1。</summary>
        public const float ArrayRegenMultiplier = 0.1f;

        /// <summary>10条品级进度条长度（九品→一品）。</summary>
        public static readonly int[] ProgressBarLengths = new int[]
        {
            300,   // 九品
            370,  // 八品
            460,  // 七品
            570,  // 六品
            700,  // 五品
            850,  // 四品
            1020, // 三品
            1210, // 二品
            1420  // 一品
        };

        /// <summary>超品判定阈值：一品累计 + 749 = 7,649。</summary>
        public const int TranscendentThreshold = 7649;

        // ---------- 运行时状态 ----------
        public BreakthroughState State { get; private set; } = BreakthroughState.Idle;

        /// <summary>当前激活的品级索引（0=九品, 8=一品）。</summary>
        public int CurrentRankIndex { get; private set; } = 0;

        /// <summary>当前进度条已充能数值。</summary>
        public float CurrentBarProgress { get; private set; } = 0f;

        /// <summary>已累计完成的进度条总长度。</summary>
        public float TotalProgressConsumed { get; private set; } = 0f;

        /// <summary>当前蓝量（实时变化）。</summary>
        public float CurrentMana { get; private set; } = 0f;

        /// <summary>当前血量（实时变化，保留1点）。</summary>
        public float CurrentHp { get; private set; } = 0f;

        /// <summary>剩余倒计时（秒）。</summary>
        public float RemainingTime { get; private set; } = 0f;

        /// <summary>当前聚灵阵气回复加成（由外部按境界注入）。</summary>
        public float ArrayRegenBonus { get; set; } = 0f;

        /// <summary>最终判定的品级（仅在 Completed/Failed 后有效）。</summary>
        public GoldenCoreRank FinalRank { get; private set; } = GoldenCoreRank.Rank9;

        /// <summary>是否已进入超品充能阶段（一品完成后继续消耗资源冲击超品）。</summary>
        private bool _isTranscendentPhase = false;

        // ---------- 玩家快照（突破开始时复制） ----------
        private float _snapshotMaxHp;
        private float _snapshotMaxMana;
        private float _snapshotHun;      // 魂属性 = 时间上限

        // ---------- 事件 ----------
        public static event Action<BreakthroughState> OnStateChanged;
        public static event Action<int, float> OnBarProgressUpdated; // 品级索引, 当前条进度0~1
        public static event Action<float> OnTimeUpdated;             // 剩余时间
        public static event Action<float, float> OnResourcesUpdated;   // 当前蓝, 当前血
        public static event Action<GoldenCoreRank> OnBreakthroughCompleted;

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

        // ============ 公开接口 ============

        /// <summary>
        /// 开始一次突破。
        /// </summary>
        /// <param name="arrayRegenBonus">当前境界聚灵阵提供的气回复加成（如筑基=4.8）。</param>
        public void StartBreakthrough(float arrayRegenBonus = 0f)
        {
            if (State == BreakthroughState.Ongoing)
            {
                Debug.LogWarning("[BreakthroughManager] 突破已在进行中，忽略重复调用。");
                return;
            }

            var profile = PlayerProfile.Instance;
            if (profile == null)
            {
                Debug.LogError("[BreakthroughManager] PlayerProfile 为空，无法开始突破。");
                return;
            }

            // 快照玩家属性
            _snapshotMaxHp = profile.MaxHp;
            _snapshotMaxMana = profile.MaxMana;
            _snapshotHun = profile.Hun;

            // 初始化状态
            CurrentRankIndex = 0;
            CurrentBarProgress = 0f;
            TotalProgressConsumed = 0f;
            CurrentMana = _snapshotMaxMana;
            CurrentHp = _snapshotMaxHp;
            RemainingTime = _snapshotHun; // 时间上限 = 魂属性
            ArrayRegenBonus = Mathf.Max(0f, arrayRegenBonus);
            FinalRank = GoldenCoreRank.Rank9;
            _isTranscendentPhase = false;

            ChangeState(BreakthroughState.Ongoing);
            Debug.Log($"[BreakthroughManager] 突破开始：HP={CurrentHp}, Mana={CurrentMana}, 时间={RemainingTime}s, 聚灵加成={ArrayRegenBonus:F1}/s");
        }

        /// <summary>立即强制结束当前突破（用于调试或外部中断）。</summary>
        public void ForceEnd()
        {
            if (State != BreakthroughState.Ongoing) return;
            FinalizeBreakthrough();
        }

        // ============ 核心循环 ============

        private void Update()
        {
            if (State != BreakthroughState.Ongoing) return;

            float dt = Time.deltaTime;

            // --- 时间轨 ---
            RemainingTime -= dt;
            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                FinalizeBreakthrough();
                return;
            }

            // --- 资源轨：充能当前进度条 ---
            float charge = BaseDrainPerSecond * dt; // 本次充能量
            CurrentBarProgress += charge;
            TotalProgressConsumed += charge;

            // 扣除资源（先蓝后血）
            ConsumeResources(charge, dt);

            // 检查资源是否耗尽（血=1且蓝=0）
            if (CurrentHp <= 1f && CurrentMana <= 0f)
            {
                // 资源耗尽，根据总进度判定品级（Completed，非Failed）
                FinalRank = DetermineRankByProgress(TotalProgressConsumed);
                ChangeState(BreakthroughState.Completed);
                OnBreakthroughCompleted?.Invoke(FinalRank);
                Debug.Log($"[BreakthroughManager] 资源耗尽，突破结束，品级={FinalRank}");
                return;
            }

            // 超品充能阶段：一品完成后继续消耗资源冲击超品
            if (_isTranscendentPhase)
            {
                if (TotalProgressConsumed >= TranscendentThreshold)
                {
                    FinalRank = GoldenCoreRank.Transcendent;
                    ChangeState(BreakthroughState.Completed);
                    OnBreakthroughCompleted?.Invoke(FinalRank);
                    Debug.Log($"[BreakthroughManager] 超品达成！总消耗={TotalProgressConsumed:F0}");
                    return;
                }

                // 超品阶段：用一品条作为超品条的基础，显示 749 的进度
                float transcendentRatio = Mathf.Clamp01((TotalProgressConsumed - 6900f) / (TranscendentThreshold - 6900f));
                OnBarProgressUpdated?.Invoke(9, transcendentRatio);
                OnTimeUpdated?.Invoke(RemainingTime);
                OnResourcesUpdated?.Invoke(CurrentMana, CurrentHp);
                return;
            }

            // 检查当前进度条是否完成（支持溢出到下一条）
            while (CurrentRankIndex < ProgressBarLengths.Length && CurrentBarProgress >= ProgressBarLengths[CurrentRankIndex])
            {
                float overflow = CurrentBarProgress - ProgressBarLengths[CurrentRankIndex];
                CurrentBarProgress = overflow;
                CurrentRankIndex++;

                // 检查是否一品已完成
                if (CurrentRankIndex >= ProgressBarLengths.Length)
                {
                    // 进入超品充能阶段
                    _isTranscendentPhase = true;
                    Debug.Log($"[BreakthroughManager] 一品完成，进入超品充能阶段...");

                    if (TotalProgressConsumed >= TranscendentThreshold)
                    {
                        // 一品完成瞬间即达到超品阈值（仅理论可能，因为一品累计 6900 < 7649）
                        FinalRank = GoldenCoreRank.Transcendent;
                        ChangeState(BreakthroughState.Completed);
                        OnBreakthroughCompleted?.Invoke(FinalRank);
                        Debug.Log($"[BreakthroughManager] 超品达成！总消耗={TotalProgressConsumed:F0}");
                        return;
                    }

                    // 超品阶段 UI 更新将在下一帧由 _isTranscendentPhase 分支处理
                    return;
                }
            }

            // 正常进度条阶段：触发进度更新事件（每帧，供 UI 刷新）
            float barRatio = CurrentBarProgress / ProgressBarLengths[CurrentRankIndex];
            OnBarProgressUpdated?.Invoke(CurrentRankIndex, barRatio);
            OnTimeUpdated?.Invoke(RemainingTime);
            OnResourcesUpdated?.Invoke(CurrentMana, CurrentHp);
        }

        // ============ 资源消耗逻辑 ============

        private void ConsumeResources(float charge, float dt)
        {
            // 蓝条阶段：净消耗 = 15 - (5 + 聚灵阵加成)
            float netManaDrain = BaseDrainPerSecond - (BaseManaRegen + ArrayRegenBonus);
            // 注意：charge = BaseDrainPerSecond * dt，但回复是独立的
            // 实际上蓝条变化 = 回复 - 扣除
            // 这里重新计算：每秒蓝条变化 = -(BaseDrainPerSecond) + (BaseManaRegen + ArrayRegenBonus)
            // 因为 charge 是进度条充能量，不是资源扣除量
            // 资源扣除量 = charge（即15*dt），回复量 = (BaseManaRegen + ArrayRegenBonus) * dt

            float regenThisFrame = (BaseManaRegen + ArrayRegenBonus) * dt;
            float drainThisFrame = BaseDrainPerSecond * dt;

            if (CurrentMana > 0f)
            {
                // 蓝条阶段
                float newMana = CurrentMana - drainThisFrame + regenThisFrame;
                if (newMana >= 0f)
                {
                    CurrentMana = Mathf.Min(newMana, _snapshotMaxMana);
                }
                else
                {
                    // 蓝条不够，部分扣血
                    float manaOverdraw = -newMana;
                    CurrentMana = 0f;
                    CurrentHp -= manaOverdraw; // 蓝不够的部分从血扣
                }
            }
            else
            {
                // 血条阶段（无回复）
                CurrentHp -= drainThisFrame;
            }

            // 血条保底1点
            if (CurrentHp < 1f) CurrentHp = 1f;
        }

        // ============ 结算 ============

        private void FinalizeBreakthrough()
        {
            if (State != BreakthroughState.Ongoing) return;

            // 根据总进度消耗量直接判定品级（与资源耗尽路径统一）
            FinalRank = DetermineRankByProgress(TotalProgressConsumed);

            ChangeState(BreakthroughState.Completed);
            OnBreakthroughCompleted?.Invoke(FinalRank);
            Debug.Log($"[BreakthroughManager] 突破结束，品级={FinalRank}，总进度={TotalProgressConsumed:F0}");
        }

        /// <summary>根据总进度消耗量判定品级。</summary>
        public static GoldenCoreRank DetermineRankByProgress(float totalProgress)
        {
            float cumulative = 0f;
            for (int i = 0; i < ProgressBarLengths.Length; i++)
            {
                cumulative += ProgressBarLengths[i];
                if (totalProgress < cumulative)
                {
                    // 没读完第 i 条进度条，说明已完成的上一条就是最终品级
                    // i=0：连九品都没完成，返回九品
                    // i=1：完成了九品，八品没完成，返回九品
                    return i == 0 ? GoldenCoreRank.Rank9 : (GoldenCoreRank)(i - 1);
                }
            }
            // 一品进度条已读完（累计达到6900）
            if (totalProgress >= TranscendentThreshold)
                return GoldenCoreRank.Transcendent;
            return GoldenCoreRank.Rank1;
        }

        private void ChangeState(BreakthroughState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
