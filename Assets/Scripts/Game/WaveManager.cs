using UnityEngine;
using ArtifactDesigner.Combat;
using ArtifactDesigner.Core;
using ArtifactDesigner.Entity;
using ArtifactDesigner.Quest;
using ArtifactDesigner.UI;

namespace ArtifactDesigner
{
    /// <summary>
    /// 波次管理器（单例，挂在 GameManager 同物体上）。
    /// 第 1 关波次结构（design_v01 §五）：1 / 3 / 5 / 10 / 15 / 30×5 共 10 波。
    /// 0.0.1 仅开放第 1 波（maxWave = 1），后续版本按迭代计划逐波开放。
    /// 流程：BeginPreparation(准备秒) → StartWave → 清波 → 间隔 → 下一波 / 全部结束。
    /// 0.0.2 新增：波次开始时自动接取任务 + 弹出气泡。
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        /// <summary>第 1 关 10 波怪物数量。</summary>
        public static readonly int[] WavePlan = { 1, 3, 5, 10, 15, 30, 30, 30, 30, 30 };

        [Header("波次配置")]
        public int maxWave = 10;             // 当前开放的最大波次（0.0.5 = 10）
        public float prepareSeconds = 30f;   // 首波准备时间（0.0.6调整为30秒）
        public float interWaveSeconds = 60f; // 波间间隔（0.0.6调整为60秒）
        public Vector3[] spawnPositions = new Vector3[]
        {
            new Vector3(0f, 1f, 30f)         // 第 1 波单通道出生点
        };

        public int CurrentWave { get; private set; }
        public int WaveAliveCount => GameManager.Instance != null ? GameManager.Instance.AliveEnemyCount : 0;
        public GameState Stage { get; private set; } = GameState.Preparing;

        private float timer;
        private bool waitingNextWave;

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

        /// <summary>进入准备阶段，倒计时结束后开始第 1 波。</summary>
        public void BeginPreparation()
        {
            CurrentWave = 0;
            Stage = GameState.Preparing;
            GameEvents.RaiseGameStateChanged(GameState.Preparing);
            timer = prepareSeconds;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.State == GameState.Defeat ||
                GameManager.Instance.State == GameState.Victory)
                return;

            if (Stage == GameState.Preparing)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    StartWave(1);
                }
            }
            else if (Stage == GameState.WaveActive)
            {
                // 清波判定交给 OnEnemyKilled（WaveAliveCount == 0 时触发）
            }
            else if (Stage == GameState.WaveClear && waitingNextWave)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    waitingNextWave = false;
                    StartWave(CurrentWave + 1);
                }
            }
        }

        /// <summary>开始指定波次，生成对应数量敌人。</summary>
        public void StartWave(int waveIndex)
        {
            if (waveIndex < 1 || waveIndex > WavePlan.Length) return;
            waitingNextWave = false; // 强制重置，防止外部调用 StartWave 后 timer 到期再次触发跳波
            if (waveIndex > maxWave)
            {
                // 当前版本未开放：视为通关
                GameManager.Instance.OnWaveCleared(waveIndex - 1);
                return;
            }

            CurrentWave = waveIndex;
            Stage = GameState.WaveActive;
            GameManager.Instance.SetState(GameState.WaveActive);
            GameEvents.RaiseWaveStarted(waveIndex);

            // 0.0.2：任务自动接取 + 气泡
            var qs = QuestSystem.Instance;
            if (qs != null) qs.OnWaveStarted(waveIndex);

            int count = WavePlan[waveIndex - 1];
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = spawnPositions[i % spawnPositions.Length];
                // 0.0.6：第10波最后一只替换为Boss
                bool isBoss = (waveIndex == 10 && i == count - 1);
                Enemy model = isBoss
                    ? WaveEnemyFactory.CreateRealm1Boss(waveIndex)
                    : WaveEnemyFactory.CreateRealm1Enemy(waveIndex, i);
                var enemy = EnemyEntity.Spawn(
                    model,
                    pos + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f)),
                    waveIndex,
                    isBoss);
                GameManager.Instance.RegisterEnemy(enemy);
            }
        }

        /// <summary>敌人死亡回调：剩余为 0 且处于战斗阶段 → 清波。</summary>
        public void NotifyEnemyKilled(EnemyEntity enemy)
        {
            if (Stage != GameState.WaveActive) return;
            if (GameManager.Instance.AliveEnemyCount > 0) return;

            Stage = GameState.WaveClear;
            GameEvents.RaiseWaveCleared(CurrentWave);
            GameManager.Instance.OnWaveCleared(CurrentWave);
        }

        /// <summary>当前波次是否为最终波（决定 Victory）。</summary>
        public bool IsFinalWave(int waveIndex) => waveIndex >= maxWave;

        /// <summary>清波后启动下一波倒计时。</summary>
        public void StartNextWaveDelay()
        {
            if (Stage != GameState.WaveClear) return;
            waitingNextWave = true;
            timer = interWaveSeconds;
        }
    }

    /// <summary>
    /// 波次敌人工厂：按 design_v01 第 1 关小怪参数（体=5 → HP60/攻10/防10/攻速1.5/移速2.5）
    /// 构造纯逻辑 Enemy 模型，接入现有掉落/经验字段。
    /// </summary>
    public static class WaveEnemyFactory
    {
        public static Enemy CreateRealm1Enemy(int wave, int index)
        {
            // 0.0.6：使用波次成长公式
            int hp = Mathf.RoundToInt(FormulaHelper.CalcEnemyHp(wave));
            int atk = Mathf.RoundToInt(FormulaHelper.CalcEnemyAtk(wave));

            var enemy = new Enemy(
                id: $"l1w{wave}_{index}",
                name: "山精",
                level: 1,
                maxHp: hp,
                attack: atk,
                speed: FormulaHelper.EnemyAttackSpeed,
                exp: 20 + wave * 5);
            enemy.dropTable = null; // 0.0.1 掉落表后续版本接入
            return enemy;
        }

        /// <summary>创建第1关第10波守关Boss。</summary>
        public static Enemy CreateRealm1Boss(int wave)
        {
            int hp = Mathf.RoundToInt(FormulaHelper.CalcBossHp(wave));
            int atk = Mathf.RoundToInt(FormulaHelper.CalcBossAtk(wave));

            var boss = new Enemy(
                id: $"l1w{wave}_boss",
                name: "山精首领",
                level: 1,
                maxHp: hp,
                attack: atk,
                speed: FormulaHelper.EnemyAttackSpeed * 0.8f, // Boss攻速略慢但攻高血厚
                exp: 200 + wave * 20);

            // Boss掉落2-3件（设计文档：Boss掉2-3件）
            boss.dropTable = new DropTable
            {
                tableId = "Boss_L1W10",
                entries = new System.Collections.Generic.List<DropEntry>
                {
                    new DropEntry(PartType.SpiritCore, Rarity.Uncommon, 1.0f, 1, 1),    // 必掉1个蓝灵核
                    new DropEntry(PartType.Attack,       Rarity.Uncommon, 0.7f, 1, 1),   // 70%掉1个蓝攻击部件
                    new DropEntry(PartType.ActiveSkill,  Rarity.Common,   0.5f, 1, 1),   // 50%掉1个白主动技能
                }
            };
            return boss;
        }
    }
}