using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Array;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.UI
{
    /// <summary>
    /// 聚灵阵最简 UI（OnGUI 实现）。
    /// 按键 K 开关界面；左侧阵盘网格，右侧灵物背包，底部效果预览。
    /// </summary>
    public class SimpleArrayUI : MonoBehaviour
    {
        [Header("界面开关")]
        public bool IsOpen = false;
        public KeyCode ToggleKey = KeyCode.K;

        [Header("布局")]
        public float panelX = 20f;
        public float panelY = 20f;
        public float panelW = 900f;
        public float panelH = 600f;

        private Vector2 backpackScroll;
        private List<SpiritItem> backpack = new List<SpiritItem>();
        private SpiritItem selectedItem;
        private Vector2Int? hoverSlot;

        private void Awake()
        {
            InitTestBackpack();
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
                IsOpen = !IsOpen;
        }

        private void InitTestBackpack()
        {
            backpack.Add(new SpiritItem { Id = "stone_1", Name = "下品灵石", Type = SpiritItemType.SpiritStone, Quality = 1, Element = ElementType.Metal, Quantity = 10 });
            backpack.Add(new SpiritItem { Id = "stone_3", Name = "上品灵石", Type = SpiritItemType.SpiritStone, Quality = 3, Element = ElementType.Water, Quantity = 5 });
            backpack.Add(new SpiritItem { Id = "pearl_2", Name = "中品聚灵珠", Type = SpiritItemType.GatheringPearl, Quality = 2, Element = ElementType.Wood, Quantity = 3 });
            backpack.Add(new SpiritItem { Id = "flag_2", Name = "中品阵旗", Type = SpiritItemType.ArrayFlag, Quality = 2, Element = ElementType.Fire, Quantity = 2 });
            backpack.Add(new SpiritItem { Id = "mat_metal_2", Name = "蓝品金材料(+)", Type = SpiritItemType.ElementMaterial, Quality = 2, Element = ElementType.Metal, IsPositive = true, Quantity = 5 });
            backpack.Add(new SpiritItem { Id = "mat_wood_3", Name = "紫品木材料(+)", Type = SpiritItemType.ElementMaterial, Quality = 3, Element = ElementType.Wood, IsPositive = true, Quantity = 3 });
            backpack.Add(new SpiritItem { Id = "mat_water_2n", Name = "蓝品水材料(-)", Type = SpiritItemType.ElementMaterial, Quality = 2, Element = ElementType.Water, IsPositive = false, Quantity = 2 });
        }

        private void OnGUI()
        {
            if (!IsOpen) return;
            var array = SpiritGatheringArray.Instance;
            if (array == null) return;

            hoverSlot = null;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "聚灵阵");

            int radius = array.CurrentRadius;
            float gridSize = Mathf.Min(360f / (radius * 2 + 1), 48f);
            float gridOriginX = panelX + 40f;
            float gridOriginY = panelY + 60f;

            // 绘制阵盘
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var slot = array.GetSlot(x, y);
                    bool isEye = (x == 0 && y == 0);
                    bool hasItem = slot != null && slot.HasItem;

                    float cx = gridOriginX + (x + radius) * gridSize;
                    float cy = gridOriginY + (radius - y) * gridSize;
                    Rect cellRect = new Rect(cx, cy, gridSize - 2f, gridSize - 2f);

                    Color bg;
                    if (isEye)
                        bg = new Color(0.9f, 0.7f, 0.2f);
                    else if (hasItem)
                    {
                        bg = GetElementColor(slot.Item.Element);
                        bg = Color.Lerp(bg, Color.white, 0.3f);
                    }
                    else
                        bg = new Color(0.15f, 0.15f, 0.18f);

                    GUI.color = bg;
                    GUI.DrawTexture(cellRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;

                    if (isEye)
                    {
                        GUI.Label(cellRect, "眼", new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 10,
                            normal = new GUIStyleState { textColor = Color.black }
                        });
                    }
                    else if (hasItem)
                    {
                        string label = slot.Item.Type switch
                        {
                            SpiritItemType.SpiritStone => "石",
                            SpiritItemType.GatheringPearl => "珠",
                            SpiritItemType.ArrayFlag => "旗",
                            SpiritItemType.ElementMaterial => slot.Item.IsPositive ? "+" : "-",
                            _ => "?"
                        };
                        GUI.Label(cellRect, label, new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 12,
                            normal = new GUIStyleState { textColor = Color.white }
                        });
                    }

                    if (cellRect.Contains(Event.current.mousePosition))
                        hoverSlot = pos;
                }
            }

            // 右侧背包
            float backpackX = panelX + 440f;
            float backpackY = panelY + 60f;
            GUI.Label(new Rect(backpackX, panelY + 30f, 200f, 24f), "灵物背包");

            backpackScroll = GUI.BeginScrollView(
                new Rect(backpackX, backpackY, 240f, 300f),
                backpackScroll,
                new Rect(0, 0, 220f, backpack.Count * 32f));

            for (int i = 0; i < backpack.Count; i++)
            {
                var item = backpack[i];
                Rect itemRect = new Rect(0, i * 32f, 220f, 30f);
                bool isSelected = selectedItem == item;
                GUI.color = isSelected ? new Color(0.3f, 0.5f, 0.8f) : new Color(0.2f, 0.2f, 0.25f);
                GUI.DrawTexture(itemRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                string qty = item.Quantity > 1 ? $"x{item.Quantity}" : "";
                if (GUI.Button(itemRect, $"{item.Name}{qty}"))
                    selectedItem = (selectedItem == item) ? null : item;
            }
            GUI.EndScrollView();

            // 摆放/移除按钮
            if (selectedItem != null && hoverSlot.HasValue)
            {
                var hs = hoverSlot.Value;
                var slot = array.GetSlot(hs.x, hs.y);
                if (slot != null && !slot.IsEye && !slot.HasItem)
                {
                    Rect placeBtn = new Rect(backpackX, backpackY + 320f, 100f, 30f);
                    if (GUI.Button(placeBtn, "摆放"))
                    {
                        var clone = selectedItem.Clone();
                        clone.Quantity = 1;
                        if (array.PlaceItem(hs.x, hs.y, clone))
                        {
                            selectedItem.Quantity--;
                            if (selectedItem.Quantity <= 0)
                            {
                                backpack.Remove(selectedItem);
                                selectedItem = null;
                            }
                        }
                    }
                }
                else if (slot != null && slot.HasItem)
                {
                    Rect removeBtn = new Rect(backpackX, backpackY + 320f, 100f, 30f);
                    if (GUI.Button(removeBtn, "移除"))
                    {
                        var removed = array.RemoveItem(hs.x, hs.y);
                        if (removed != null)
                        {
                            var existing = backpack.Find(x => x.Id == removed.Id && x.IsPositive == removed.IsPositive);
                            if (existing != null)
                                existing.Quantity += removed.Quantity;
                            else
                                backpack.Add(removed);
                        }
                    }
                }
            }

            // 底部信息面板
            float infoX = panelX + 40f;
            float infoY = gridOriginY + (radius * 2 + 1) * gridSize + 20f;
            GUI.Label(new Rect(infoX, infoY, 400f, 24f), $"阵眼灵气浓度: {array.TotalConcentration:F2}");
            string elemStr = array.IsChaotic ? "混沌" : array.DominantElement.ToString();
            GUI.Label(new Rect(infoX, infoY + 24f, 400f, 24f), $"灵气属性偏向: {elemStr}");
            GUI.Label(new Rect(infoX, infoY + 48f, 400f, 24f), $"等效值: {array.EquivalentValue:F3}");

            float infoX2 = panelX + 440f;
            GUI.Label(new Rect(infoX2, infoY, 300f, 24f), $"修炼速度加成: +{array.GetCultivationSpeedBonus() * 100f:F1}%");
            GUI.Label(new Rect(infoX2, infoY + 24f, 300f, 24f), $"突破成功率加成: +{array.GetBreakthroughSuccessBonus() * 100f:F1}%");
            GUI.Label(new Rect(infoX2, infoY + 48f, 300f, 24f), $"局内灵力回复加成: +{array.GetManaRegenBonus() * 100f:F1}%");
            GUI.Label(new Rect(infoX2, infoY + 72f, 300f, 24f), $"炼丹供应: {array.GetAlchemySupplyPerMinute():F1} 灵气/分");
            GUI.Label(new Rect(infoX2, infoY + 96f, 300f, 24f), $"灵池: {array.AlchemySpiritPool:F0}/{array.AlchemyPoolMax:F0}");
        }

        private Color GetElementColor(ElementType elem)
        {
            return elem switch
            {
                ElementType.Metal => new Color(0.9f, 0.9f, 0.85f),
                ElementType.Wood => new Color(0.3f, 0.7f, 0.3f),
                ElementType.Water => new Color(0.2f, 0.5f, 0.9f),
                ElementType.Fire => new Color(0.9f, 0.3f, 0.2f),
                ElementType.Earth => new Color(0.7f, 0.5f, 0.2f),
                _ => Color.gray
            };
        }
    }
}
