using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;
using ArtifactDesigner.Entity;
using ArtifactDesigner.Quest;
using ArtifactDesigner.UI;
using ArtifactDesigner.Array;

namespace ArtifactDesigner
{
    /// <summary>
    /// 游戏主管理器（单例）。
    /// 职责：场景搭建（0.0.1 占位 Primitive）、游戏状态机、
    /// 玩家/水晶引用、敌人管理、胜负判定。
    /// 波次节奏由 WaveManager 驱动，战斗数据由 BattleManager 维护。
    /// 0.0.2 新增：PlayerProfile、QuestSystem、BubbleSystem 初始化。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("场景引用")]
        public PlayerEntity Player;
        public CrystalEntity Crystal;

        [Header("状态")]
        public GameState State = GameState.Preparing;
        public int CurrentLevel = 1;          // 当前关卡编号（占位，后续从关卡选择/存档读取）

        [Header("0.0.9 碰撞与投射物")]
        public CollisionManager CollisionMgr;
        public ProjectilePool ProjectilePool;

        [Header("占位场景")]
        public float groundSize = 80f;
        public Vector3 playerSpawn = new Vector3(0f, 1f, 0f);
        public Vector3 crystalPos = new Vector3(0f, 1.5f, 32f);

        private readonly List<EnemyEntity> aliveEnemies = new List<EnemyEntity>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 初始化局外档案（若已存在则保留）
            var profile = PlayerProfile.Instance;
            profile.ResetRunStats();

            // 0.0.9：确保碰撞与投射物池子系统存在
            EnsureCombatSubsystems();

            BuildPlaceholderScene();
            InitializeSubsystems();
            if (WaveManager.Instance != null) WaveManager.Instance.BeginPreparation();
        }

        // ============ 0.0.9 战斗子系统初始化 ============
        private void EnsureCombatSubsystems()
        {
            CollisionMgr = FindObjectOfType<CollisionManager>();
            if (CollisionMgr == null)
                CollisionMgr = gameObject.AddComponent<CollisionManager>();

            ProjectilePool = FindObjectOfType<ProjectilePool>();
            if (ProjectilePool == null)
                ProjectilePool = gameObject.AddComponent<ProjectilePool>();

            // 0.0.9：地形管理器
            if (FindObjectOfType<TerrainManager>() == null)
                gameObject.AddComponent<TerrainManager>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ============ 子系统初始化（0.0.2 / 0.0.4） ============
        private void InitializeSubsystems()
        {
            // QuestSystem
            if (FindObjectOfType<QuestSystem>() == null)
                gameObject.AddComponent<QuestSystem>();

            // BubbleSystem
            if (FindObjectOfType<BubbleSystem>() == null)
                gameObject.AddComponent<BubbleSystem>();

            // 0.0.4：炼器系统 + 最简炼器 UI
            if (FindObjectOfType<ArtifactCraftingSystem>() == null)
                gameObject.AddComponent<ArtifactCraftingSystem>();
            if (FindObjectOfType<SimpleCraftUI>() == null)
                gameObject.AddComponent<SimpleCraftUI>();

            // 0.0.5：灵石经济系统 + 契合度评级系统
            if (FindObjectOfType<SpiritEconomySystem>() == null)
                gameObject.AddComponent<SpiritEconomySystem>();
            if (FindObjectOfType<ArtifactCompatibilitySystem>() == null)
                gameObject.AddComponent<ArtifactCompatibilitySystem>();

            // 0.0.7：聚灵阵系统 + UI
            if (FindObjectOfType<SpiritGatheringArray>() == null)
                gameObject.AddComponent<SpiritGatheringArray>();
            if (FindObjectOfType<SimpleArrayUI>() == null)
                gameObject.AddComponent<SimpleArrayUI>();
        }

        // ============ 场景搭建（0.0.1 占位表现） ============
        private void BuildPlaceholderScene()
        {
            // 地面
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(groundSize / 10f, 1f, groundSize / 10f);
            var gr = ground.GetComponent<Renderer>();
            if (gr != null) gr.material.color = new Color(0.25f, 0.3f, 0.2f);

            // 水晶（防守目标）
            var crystalGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crystalGo.name = "Crystal";
            crystalGo.transform.position = crystalPos;
            crystalGo.transform.localScale = new Vector3(2.5f, 3f, 2.5f);
            var cr = crystalGo.GetComponent<Renderer>();
            if (cr != null) cr.material.color = new Color(0.2f, 0.6f, 1f);
            Crystal = crystalGo.AddComponent<CrystalEntity>();

            // 0.0.9：将实体注册到碰撞管理器
            if (CollisionMgr != null)
            {
                CollisionMgr.Register(Crystal);
            }

            // 玩家
            var playerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerGo.name = "Player";
            playerGo.transform.position = playerSpawn;
            playerGo.transform.localScale = new Vector3(1.6f, 1.8f, 1.6f);
            var pr = playerGo.GetComponent<Renderer>();
            if (pr != null) pr.material.color = new Color(0.9f, 0.8f, 0.2f);
            Player = playerGo.AddComponent<PlayerEntity>();
            playerGo.AddComponent<PlayerController>();

            if (CollisionMgr != null)
            {
                CollisionMgr.Register(Player);
            }

            // 摄像机（若场景中没有主相机）
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.AddComponent<Camera>();
                camGo.transform.position = new Vector3(0f, 40f, 5f);
                camGo.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            }

            // UI 管理器（单例挂载）
            if (UIManager.Instance == null)
            {
                gameObject.AddComponent<UIManager>();
            }
        }

        // ============ 敌人管理 ============
        public void RegisterEnemy(EnemyEntity e)
        {
            if (e != null && !aliveEnemies.Contains(e)) aliveEnemies.Add(e);
        }

        public void UnregisterEnemy(EnemyEntity e)
        {
            aliveEnemies.Remove(e);
        }

        // ============ 地形管理（0.0.9） ============
        public void RegisterTerrain(Entity.TerrainEntity t)
        {
            if (CollisionMgr != null && t != null)
                CollisionMgr.Register(t);
        }

        public void UnregisterTerrain(Entity.TerrainEntity t)
        {
            if (CollisionMgr != null && t != null)
                CollisionMgr.Unregister(t);
        }

        /// <summary>获取场上所有存活敌人。</summary>
        public List<EnemyEntity> GetAliveEnemies()
        {
            return aliveEnemies.FindAll(e => e != null && e.IsAlive);
        }

        public List<EnemyEntity> AliveEnemies => aliveEnemies;

        public int AliveEnemyCount => aliveEnemies.Count;

        public BattleEntity FindNearestEnemy(Vector3 pos, float maxRange)
        {
            BattleEntity best = null;
            float bestSqr = maxRange * maxRange;
            foreach (var e in aliveEnemies)
            {
                if (e == null || !e.IsAlive) continue;
                float sqr = (e.transform.position - pos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
            }
            return best;
        }

        // ============ 击杀/清波回调（由 EnemyEntity / WaveManager 调用） ============
        public void NotifyEnemyKilled(EnemyEntity enemy)
        {
            var wave = WaveManager.Instance;
            if (wave != null) wave.NotifyEnemyKilled(enemy);

            // 0.0.2：更新击杀类任务进度
            var qs = QuestSystem.Instance;
            if (qs != null)
            {
                // Q001：击败第1波第1只敌人（波次内任意击杀即完成，因只有1只）
                if (qs.IsInProgress("Q001") && enemy != null && enemy.waveIndex == 1)
                {
                    qs.AddProgress("Q001", 1);
                }
                // Q003：击败第4波敌人
                if (qs.IsInProgress("Q003") && enemy != null && enemy.waveIndex == 4)
                {
                    qs.AddProgress("Q003", 1);
                }
            }
        }

        // ============ 胜负 ============
        public void OnWaveCleared(int waveIndex)
        {
            if (State == GameState.Defeat || State == GameState.Victory) return;
            var wave = WaveManager.Instance;
            if (wave != null && wave.IsFinalWave(waveIndex))
            {
                SetState(GameState.Victory);
                // 0.0.2：关卡结束时结算 Q005（完美首关）
                var qs = QuestSystem.Instance;
                if (qs != null && Player != null)
                {
                    qs.OnLevelEnd(victory: true, Player.deathCount);
                }
            }
            else if (wave != null)
            {
                wave.StartNextWaveDelay();
            }
        }

        public void OnCrystalDestroyed(CrystalEntity crystal)
        {
            if (State == GameState.Defeat || State == GameState.Victory) return;
            SetState(GameState.Defeat);
            // 0.0.2：关卡失败时也结算 Q005
            var qs = QuestSystem.Instance;
            if (qs != null && Player != null)
            {
                qs.OnLevelEnd(victory: false, Player.deathCount);
            }
        }

        public void SetState(GameState state)
        {
            if (State == state) return;
            State = state;
            GameEvents.RaiseGameStateChanged(state);
        }
    }
}