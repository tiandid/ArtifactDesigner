using UnityEngine;
using ArtifactDesigner.Entity;

namespace ArtifactDesigner
{
    /// <summary>
    /// 全局事件总线（轻量静态事件，无依赖注入）。
    /// 用于 GameManager / Wave / UI 等系统解耦。
    /// </summary>
    public static class GameEvents
    {
        // —— 实体生命周期 ——
        public static event System.Action<BattleEntity> OnEntitySpawned;
        public static event System.Action<BattleEntity> OnEntityDied;
        public static event System.Action<BattleEntity, int, BattleEntity> OnEntityDamaged; // 目标, 伤害, 来源

        // —— 敌人 ——
        public static event System.Action<EnemyEntity> OnEnemySpawned;
        public static event System.Action<EnemyEntity> OnEnemyKilled;

        // —— 玩家 ——
        public static event System.Action<PlayerEntity> OnPlayerDied;
        public static event System.Action<PlayerEntity> OnPlayerRespawned;

        // —— 水晶 ——
        public static event System.Action<CrystalEntity, int> OnCrystalDamaged;
        public static event System.Action<CrystalEntity> OnCrystalDestroyed;

        // —— 波次 ——
        public static event System.Action<int> OnWaveStarted;   // 波次号（从 1）
        public static event System.Action<int> OnWaveCleared;   // 波次号

        // —— 输入（技能/自我选中占位，0.0.6 接入技能系统）——
        public static event System.Action<int> OnSkillPressed;  // 技能槽位 0..3
        public static event System.Action OnSelfSelected;

        // —— 游戏流程 ——
        public static event System.Action<Core.GameState> OnGameStateChanged;

        // —— 任务（0.0.2 接入）——
        public static event System.Action<Quest.QuestInstance> OnQuestAccepted;
        public static event System.Action<Quest.QuestInstance> OnQuestProgressUpdated;
        public static event System.Action<Quest.QuestInstance> OnQuestCompleted;

        // —— 经验 / 等级（0.0.2 接入）——
        public static event System.Action<int, long> OnPlayerExpChanged;   // 等级, 本级经验
        public static event System.Action<int> OnPlayerLevelUp;            // 新等级

        // —— 地形（0.0.9）——
        public static event System.Action<Entity.TerrainEntity> OnTerrainSpawned;
        public static event System.Action<Entity.TerrainEntity> OnTerrainDestroyed;

        // —— 投射物（0.0.9）——
        public static event System.Action<GameObject> OnProjectileSpawned;
        public static event System.Action<GameObject> OnProjectileDestroyed;

        // —— 内部触发（由 GameManager / 实体调用）——
        public static void RaiseEntitySpawned(BattleEntity e) => OnEntitySpawned?.Invoke(e);
        public static void RaiseEntityDied(BattleEntity e) => OnEntityDied?.Invoke(e);
        public static void RaiseEntityDamaged(BattleEntity target, int amount, BattleEntity source)
            => OnEntityDamaged?.Invoke(target, amount, source);
        public static void RaiseEnemySpawned(EnemyEntity e) => OnEnemySpawned?.Invoke(e);
        public static void RaiseEnemyKilled(EnemyEntity e) => OnEnemyKilled?.Invoke(e);
        public static void RaisePlayerDied(PlayerEntity p) => OnPlayerDied?.Invoke(p);
        public static void RaisePlayerRespawned(PlayerEntity p) => OnPlayerRespawned?.Invoke(p);
        public static void RaiseCrystalDamaged(CrystalEntity c, int amount) => OnCrystalDamaged?.Invoke(c, amount);
        public static void RaiseCrystalDestroyed(CrystalEntity c) => OnCrystalDestroyed?.Invoke(c);
        public static void RaiseWaveStarted(int wave) => OnWaveStarted?.Invoke(wave);
        public static void RaiseWaveCleared(int wave) => OnWaveCleared?.Invoke(wave);
        public static void RaiseGameStateChanged(Core.GameState state) => OnGameStateChanged?.Invoke(state);
        public static void RaiseSkillPressed(int slot) => OnSkillPressed?.Invoke(slot);
        public static void RaiseSelfSelected() => OnSelfSelected?.Invoke();

        // —— 0.0.2 新增 ——
        public static void RaiseQuestAccepted(Quest.QuestInstance q) => OnQuestAccepted?.Invoke(q);
        public static void RaiseQuestProgressUpdated(Quest.QuestInstance q) => OnQuestProgressUpdated?.Invoke(q);
        public static void RaiseQuestCompleted(Quest.QuestInstance q) => OnQuestCompleted?.Invoke(q);
        public static void RaisePlayerExpChanged(int level, long exp) => OnPlayerExpChanged?.Invoke(level, exp);
        public static void RaisePlayerLevelUp(int newLevel) => OnPlayerLevelUp?.Invoke(newLevel);

        // —— 0.0.9 新增 ——
        public static void RaiseTerrainSpawned(Entity.TerrainEntity t) => OnTerrainSpawned?.Invoke(t);
        public static void RaiseTerrainDestroyed(Entity.TerrainEntity t) => OnTerrainDestroyed?.Invoke(t);
        public static void RaiseProjectileSpawned(GameObject p) => OnProjectileSpawned?.Invoke(p);
        public static void RaiseProjectileDestroyed(GameObject p) => OnProjectileDestroyed?.Invoke(p);
    }
}