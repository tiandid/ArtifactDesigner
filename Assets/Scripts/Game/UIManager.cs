using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Core;
using ArtifactDesigner.Quest;
using ArtifactDesigner.UI;

namespace ArtifactDesigner
{
    /// <summary>
    /// 简易 HUD（0.0.1 用 OnGUI 绘制，无需 uGUI 资源）：
    /// 左上角显示波次/存活敌人/击杀进度，左下角玩家血条，顶部游戏状态。
    /// 0.0.2 新增：左侧任务追踪面板（最多 3 条）、玩家等级/经验/灵石显示、
    /// 气泡由 BubbleSystem 独立绘制。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private GUIStyle stateStyle;
        private GUIStyle infoStyle;
        private GUIStyle questStyle;
        private GUIStyle questTitleStyle;

        // 任务追踪缓存
        private List<QuestInstance> trackedQuests = new List<QuestInstance>();
        private bool questsDirty = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 订阅任务事件以刷新追踪面板
            QuestSystem.OnQuestAccepted += _ => questsDirty = true;
            QuestSystem.OnQuestProgressUpdated += _ => questsDirty = true;
            QuestSystem.OnQuestCompleted += _ => questsDirty = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            QuestSystem.OnQuestAccepted -= _ => questsDirty = true;
            QuestSystem.OnQuestProgressUpdated -= _ => questsDirty = true;
            QuestSystem.OnQuestCompleted -= _ => questsDirty = true;
        }

        private void OnGUI()
        {
            if (stateStyle == null)
            {
                stateStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
                infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                questStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                questTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            }

            DrawState();
            DrawInfo();
            DrawPlayerHp();
            DrawPlayerLevelExp();
            DrawQuestTracker();
        }

        private void DrawState()
        {
            string text = "仙器设计师";
            Color color = Color.white;
            if (GameManager.Instance != null)
            {
                switch (GameManager.Instance.State)
                {
                    case GameState.Preparing:
                        text = "准备中…";
                        color = Color.cyan;
                        break;
                    case GameState.WaveActive:
                        text = "第 " + (WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 1) + " 波";
                        color = Color.yellow;
                        break;
                    case GameState.Victory:
                        text = "胜利！";
                        color = Color.green;
                        break;
                    case GameState.Defeat:
                        text = "水晶被摧毁…";
                        color = Color.red;
                        break;
                }
            }
            var old = GUI.color;
            GUI.color = color;
            GUI.Label(new Rect(Screen.width * 0.5f - 150f, 16f, 300f, 40f), text, stateStyle);
            GUI.color = old;
        }

        private void DrawInfo()
        {
            string text = "波次: " + (WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0)
                        + "/" + (WaveManager.Instance != null ? WaveManager.Instance.maxWave : 1);
            var gm = GameManager.Instance;
            if (gm != null)
            {
                text += "   存活敌人: " + gm.AliveEnemyCount;
                text += "   水晶: " + (gm.Crystal != null ? gm.Crystal.currentHp + "/" + gm.Crystal.maxHp : "-");
            }
            GUI.Label(new Rect(16f, 16f, 600f, 28f), text, infoStyle);

            GUI.Label(new Rect(16f, 44f, 600f, 24f),
                "操作: WASD/方向键移动，鼠标右键点击地面移动 | Q/W/E/R 技能（0.0.6 接入）| F1 选中自己", infoStyle);
        }

        private void DrawPlayerHp()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return;

            float w = 320f;
            float h = 22f;
            Rect back = new Rect(16f, Screen.height - h - 20f, w, h);
            Rect fill = new Rect(back.x, back.y, back.width, back.height);

            float pct = (float)gm.Player.currentHp / Mathf.Max(1, gm.Player.maxHp);
            fill.width = back.width * Mathf.Clamp01(pct);

            var old = GUI.color;
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            GUI.Box(back, "");
            GUI.color = pct > 0.5f ? Color.green : (pct > 0.25f ? Color.yellow : Color.red);
            GUI.Box(fill, "");
            GUI.color = old;

            GUI.Label(new Rect(back.x, back.y - 2f, back.width, h),
                "玩家 HP: " + gm.Player.currentHp + " / " + gm.Player.maxHp, infoStyle);
        }

        // ============ 0.0.2 新增：等级 / 经验 / 灵石 ============
        private void DrawPlayerLevelExp()
        {
            var profile = PlayerProfile.Instance;
            if (profile == null) return;

            string realmName = ExpSystem.GetRealmName(profile.CurrentRealm);
            long need = ExpSystem.GetExpRequired(profile.Level);
            string expText = need > 0
                ? $"{profile.Exp} / {need}"
                : "MAX";
            float progress = profile.GetLevelProgress();

            string text = $"{realmName} Lv.{profile.Level}   经验 {expText}   灵石 {profile.SpiritStones}";
            GUI.Label(new Rect(16f, 72f, 500f, 22f), text, infoStyle);

            // 经验条（细）
            float barW = 200f;
            float barH = 6f;
            Rect barBack = new Rect(16f, 96f, barW, barH);
            Rect barFill = new Rect(barBack.x, barBack.y, barBack.width * progress, barBack.height);
            var old = GUI.color;
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.Box(barBack, "");
            GUI.color = new Color(0.2f, 0.8f, 1f);
            GUI.Box(barFill, "");
            GUI.color = old;
        }

        // ============ 0.0.2 新增：左侧任务追踪面板 ============
        private void DrawQuestTracker()
        {
            var qs = QuestSystem.Instance;
            if (qs == null) return;

            if (questsDirty)
            {
                trackedQuests = qs.GetTrackedQuests(3);
                questsDirty = false;
            }
            if (trackedQuests.Count == 0) return;

            float x = 16f;
            float y = 120f;
            float cardW = 220f;
            float cardH = 64f;
            float gap = 8f;

            foreach (var q in trackedQuests)
            {
                // 卡片背景
                var old = GUI.color;
                GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.7f);
                GUI.Box(new Rect(x, y, cardW, cardH), "");
                GUI.color = old;

                // 任务名
                GUI.Label(new Rect(x + 8f, y + 4f, cardW - 16f, 20f), q.Config.questName, questTitleStyle);

                // 进度文本
                string progressText = $"{q.Config.objectiveDesc}  ({q.CurrentProgress}/{q.TargetProgress})";
                GUI.Label(new Rect(x + 8f, y + 26f, cardW - 16f, 18f), progressText, questStyle);

                // 奖励缩略
                string rewardText = $"经验 {q.Config.expReward}  灵石 {q.Config.stoneReward}";
                GUI.Label(new Rect(x + 8f, y + 44f, cardW - 16f, 16f), rewardText, questStyle);

                y += cardH + gap;
            }
        }
    }
}
