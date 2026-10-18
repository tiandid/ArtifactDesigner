using UnityEngine;
using ArtifactDesigner.Core;
using ArtifactDesigner.Quest;
using ArtifactDesigner.Artifact;

namespace ArtifactDesigner.Game
{
    /// <summary>
    /// 最简炼器 UI 控制器（OnGUI，不依赖 uGUI 资源）。
    /// 0.0.4 最小闭环：按当前进行中的炼器教学任务自动切换面板，
    /// 提供按钮调用 ArtifactCraftingSystem 完成 Q006~Q010 交互。
    /// 按键 C 打开/关闭；任务接取时自动弹出。
    /// </summary>
    public class SimpleCraftUI : MonoBehaviour
    {
        public static SimpleCraftUI Instance { get; private set; }

        private bool visible = false;
        private Rect windowRect = new Rect(120, 80, 440, 520);
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallLabelStyle;

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
            QuestSystem.OnQuestAccepted -= OnQuestAccepted;
        }

        private void Start()
        {
            QuestSystem.OnQuestAccepted += OnQuestAccepted;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C)) Toggle();
        }

        public void Toggle() { visible = !visible; }
        public void Open() { visible = true; }
        public void Close() { visible = false; }

        private void OnQuestAccepted(QuestInstance q)
        {
            string id = q.Config.questId;
            if (id == "Q006" || id == "Q009" || id == "Q010" || id == "Q007" || id == "Q013" || id == "Q002" || id == "Q008")
            {
                Open();
            }
        }

        private void OnGUI()
        {
            if (!visible) return;

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
                smallLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            }

            windowRect = GUI.Window(2, windowRect, DrawWindow, "炼器炉 (C键开关)");
        }

        private void DrawWindow(int id)
        {
            var qs = QuestSystem.Instance;
            var craft = ArtifactCraftingSystem.Instance;
            if (qs == null || craft == null)
            {
                GUI.Label(new Rect(10, 20, 420, 30), "系统尚未初始化。", labelStyle);
                GUI.DragWindow();
                return;
            }

            // 按当前进行中的任务自动切换面板
            if (qs.IsInProgress("Q006")) DrawCraftCorePanel(craft);
            else if (qs.IsInProgress("Q009")) DrawEmbedCorePanel(craft);
            else if (qs.IsInProgress("Q010")) DrawThrusterPanel(craft);
            else if (qs.IsInProgress("Q007")) DrawSkillStructurePanel(craft);
            else if (qs.IsInProgress("Q002")) DrawConfirmAssemblyPanel(craft);
            else if (qs.IsInProgress("Q013")) DrawCraftArtifactPanel(craft);
            else if (qs.IsInProgress("Q008")) DrawAssembleBlankPanel(craft);
            else
            {
                // 无进行中的炼器任务：显示当前装配概览
                DrawOverviewPanel(craft);
            }

            GUI.DragWindow();
        }

        // ===== Q006：初次炼器 =====
        private void DrawCraftCorePanel(ArtifactCraftingSystem craft)
        {
            GUI.Label(new Rect(10, 20, 420, 26), "【Q006 初次炼器】选择一种五行材料", titleStyle);
            GUI.Label(new Rect(10, 50, 420, 24), "点击下方按钮选择材料，然后确认炼制。", labelStyle);

            string[] names = { "金", "木", "水", "火", "土" };
            for (int i = 0; i < 5; i++)
            {
                bool selected = craft.SelectedElement.HasValue && (int)craft.SelectedElement.Value == i;
                string btnText = selected ? $"[{names[i]}]" : names[i];
                if (GUI.Button(new Rect(10 + i * 84, 82, 78, 32), btnText, buttonStyle))
                {
                    craft.SelectMaterial((ElementType)i, 1);
                }
            }

            if (craft.SelectedElement.HasValue)
            {
                GUI.Label(new Rect(10, 122, 420, 24),
                    $"已选择：{ArtifactCraftingSystem.GetElementName(craft.SelectedElement.Value)}（白色品质）", labelStyle);

                if (craft.IsCrafting)
                {
                    GUI.Label(new Rect(10, 152, 420, 24), $"炼制中... {craft.CraftProgress * 100f:F0}%", labelStyle);
                }
                else if (craft.CurrentSpiritCore != null)
                {
                    GUI.Label(new Rect(10, 152, 420, 24), $"炼制完成！获得：{craft.CurrentSpiritCore.partName}", labelStyle);
                    GUI.Label(new Rect(10, 180, 420, 40),
                        "灵核已成！基础器胚已发放。\n请等待自动接取下一任务（器胚装配）。", smallLabelStyle);
                }
                else if (!craft.IsCrafting)
                {
                    if (GUI.Button(new Rect(10, 152, 120, 36), "开始炼制", buttonStyle))
                    {
                        craft.ConfirmCraft();
                    }
                }
            }
        }

        // ===== Q009：器胚装配 =====
        private void DrawEmbedCorePanel(ArtifactCraftingSystem craft)
        {
            GUI.Label(new Rect(10, 20, 420, 26), "【Q009 器胚装配】将灵核装入器胚", titleStyle);

            if (craft.CurrentBlank == null)
            {
                GUI.Label(new Rect(10, 50, 420, 30), "器胚尚未发放。", labelStyle);
                return;
            }

            if (craft.CurrentSpiritCore == null)
            {
                GUI.Label(new Rect(10, 50, 420, 30), "灵核尚未产出，请先完成初次炼器。", labelStyle);
                return;
            }

            // 显示器胚状态
            GUI.Label(new Rect(10, 50, 420, 24), $"器胚：{craft.CurrentBlank.blankName}（{craft.CurrentBlank.TotalSlots}格 / 已用{craft.CurrentBlank.UsedSlots}格）", labelStyle);
            GUI.Label(new Rect(10, 76, 420, 24), $"灵核：{craft.CurrentSpiritCore.partName}", labelStyle);

            bool alreadyEmbedded = false;
            for (int i = 0; i < craft.CurrentBlank.slots.Count; i++)
            {
                if (craft.CurrentBlank.slots[i] == craft.CurrentSpiritCore)
                {
                    alreadyEmbedded = true;
                    break;
                }
            }

            if (alreadyEmbedded)
            {
                GUI.Label(new Rect(10, 110, 420, 40), "灵核已装入器胚。\n契合度较低，法宝暂时无法运转。", smallLabelStyle);
                GUI.Label(new Rect(10, 154, 420, 40), "请等待自动接取下一任务（推进器装配）。", smallLabelStyle);
            }
            else
            {
                if (GUI.Button(new Rect(10, 110, 140, 36), "放入灵核", buttonStyle))
                {
                    craft.EmbedCore();
                }
            }
        }

        // ===== Q010：推进器装配 =====
        private void DrawThrusterPanel(ArtifactCraftingSystem craft)
        {
            GUI.Label(new Rect(10, 20, 420, 26), "【Q010 推进器装配】安装推进器并连接供能线", titleStyle);

            if (craft.CurrentBlank == null)
            {
                GUI.Label(new Rect(10, 50, 420, 30), "器胚尚未准备。", labelStyle);
                return;
            }

            GUI.Label(new Rect(10, 50, 420, 24), $"器胚：{craft.CurrentBlank.blankName}（已用{craft.CurrentBlank.UsedSlots}/{craft.CurrentBlank.TotalSlots}格）", labelStyle);
            GUI.Label(new Rect(10, 76, 420, 24), $"已安装推进器：{craft.CurrentThrusters.Count}/3", labelStyle);

            if (craft.CurrentThrusters.Count < 3)
            {
                if (GUI.Button(new Rect(10, 108, 160, 36), $"安装推进器 ({craft.CurrentThrusters.Count + 1}/3)", buttonStyle))
                {
                    craft.InstallThrusters(1);
                }
            }
            else if (!craft.EnergyLineConnected)
            {
                GUI.Label(new Rect(10, 108, 420, 24), "推进器已就位，请连接供能线。", labelStyle);
                if (GUI.Button(new Rect(10, 138, 140, 36), "连接供能线", buttonStyle))
                {
                    craft.ConnectEnergyLine();
                }
            }
            else
            {
                GUI.Label(new Rect(10, 108, 420, 40), "供能线已连接！契合度提升，法宝初步具备运转能力。", smallLabelStyle);
                GUI.Label(new Rect(10, 152, 420, 40), "请等待自动接取下一任务（熔炼技能结构 / 确认装配）。", smallLabelStyle);
            }
        }

        // ===== Q007：熔炼技能结构（简化） =====
        private void DrawSkillStructurePanel(ArtifactCraftingSystem craft)
        {
            GUI.Label(new Rect(10, 20, 420, 26), "【Q007 熔炼技能结构】选择负聚灵材料", titleStyle);
            GUI.Label(new Rect(10, 50, 420, 40), "0.0.4 占位：点击按钮直接熔炼出一个攻击结构部件。\n（完整材料选择后续版本实现）", smallLabelStyle);

            if (GUI.Button(new Rect(10, 96, 160, 36), "熔炼攻击结构", buttonStyle))
            {
                craft.CraftSkillStructure(ElementType.Metal, 1);
            }

            if (craft.CurrentSkillStructure != null)
            {
                GUI.Label(new Rect(10, 142, 420, 24), $"已产出：{craft.CurrentSkillStructure.partName}", labelStyle);
                GUI.Label(new Rect(10, 168, 420, 40), "输出/技能结构已熔炼完成！\n安装并连接功能线后，契合度升满，法宝品质提升。", smallLabelStyle);
            }
        }

        // ===== Q002：确认装配 =====
        private void DrawConfirmAssemblyPanel(ArtifactCraftingSystem craft)
        {
            GUI.Label(new Rect(10, 20, 420, 26), "【Q002 确认装配】检查契合度", titleStyle);

            float fit = craft.CalculateFitScore();
            GUI.Label(new Rect(10, 50, 420, 24), $"当前契合度：{fit * 100f:F0}%", labelStyle);

            if (fit >= 0.5f)
            {
                GUI.Label(new Rect(10, 78, 420, 40), "契合度检查通过！你的第一件法宝已成型。", smallLabelStyle);
                if (GUI.Button(new Rect(10, 124, 140, 36), "确认装配", buttonStyle))
                {
                    craft.ConfirmAssembly();
                }
            }
            else
            {
                GUI.Label(new Rect(10, 78, 420, 40), "契合度不足。请确保已装入灵核、3个推进器并连接供能线。", smallLabelStyle);
            }
        }

        // ===== Q013：首件仙器 =====
        private void DrawCraftArtifactPanel(ArtifactCraftingSystem craft)
        {
            GUI.Label(new Rect(10, 20, 420, 26), "【Q013 首件仙器】完成最终炼制", titleStyle);

            if (GUI.Button(new Rect(10, 50, 140, 36), "开始炼制", buttonStyle))
            {
                craft.CraftArtifact();
            }
        }

        // ===== Q008：设计成型 =====
        private void DrawAssembleBlankPanel(ArtifactCraftingSystem craft)
        {
            GUI.Label(new Rect(10, 20, 420, 26), "【Q008 设计成型】组装器胚", titleStyle);
            GUI.Label(new Rect(10, 50, 420, 40), "0.0.4 占位：基础器胚已预置，点击确认即可。\n（完整掉落组装后续版本实现）", smallLabelStyle);

            if (craft.CurrentBlank == null)
            {
                craft.CurrentBlank = new ArtifactBlank("blank_basic", "基础器胚", baseSlots: 4, baseWeight: 5, weightPerSlot: 2);
            }

            if (GUI.Button(new Rect(10, 96, 140, 36), "确认器胚", buttonStyle))
            {
                var qs = QuestSystem.Instance;
                if (qs != null) qs.OnArtifactAssembled();
            }
        }

        // ===== 概览面板 =====
        private void DrawOverviewPanel(ArtifactCraftingSystem craft)
        {
            GUI.Label(new Rect(10, 20, 420, 26), "炼器炉 — 当前状态", titleStyle);

            if (craft.CurrentBlank != null)
            {
                GUI.Label(new Rect(10, 52, 420, 24), $"器胚：{craft.CurrentBlank.blankName}", labelStyle);
                GUI.Label(new Rect(10, 78, 420, 24), $"格子：已用 {craft.CurrentBlank.UsedSlots} / {craft.CurrentBlank.TotalSlots}", labelStyle);
            }
            else
            {
                GUI.Label(new Rect(10, 52, 420, 24), "器胚：无", labelStyle);
            }

            if (craft.CurrentSpiritCore != null)
                GUI.Label(new Rect(10, 104, 420, 24), $"灵核：{craft.CurrentSpiritCore.partName}", labelStyle);
            if (craft.CurrentThrusters.Count > 0)
                GUI.Label(new Rect(10, 130, 420, 24), $"推进器：{craft.CurrentThrusters.Count} 个", labelStyle);
            GUI.Label(new Rect(10, 156, 420, 24), $"供能线：{(craft.EnergyLineConnected ? "已连接" : "未连接")}", labelStyle);
            GUI.Label(new Rect(10, 182, 420, 24), $"契合度：{craft.CalculateFitScore() * 100f:F0}%", labelStyle);

            GUI.Label(new Rect(10, 220, 420, 40), "提示：炼器教学任务进行时，本窗口会自动弹出。\n按 C 键可随时手动打开。", smallLabelStyle);
        }
    }
}
