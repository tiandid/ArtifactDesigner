using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtifactDesigner.UI
{
    /// <summary>
    /// 对话气泡数据。
    /// 位置：局内屏幕底部中央，距底边约 15% 屏幕高度。
    /// 颜色：White / Red / Gold / Blue。
    /// </summary>
    public struct BubbleData
    {
        public string text;
        public Color color;
        public float durationSeconds;
        public float startTime;
        public bool dismissed;

        public bool IsExpired => dismissed || (Time.time - startTime) >= durationSeconds;
    }

    /// <summary>
    /// 气泡引导系统（挂在 UIManager 同物体或独立 MonoBehaviour）。
    /// 最大同时显示 2 条，新气泡从下方顶出旧气泡。
    /// 支持点击立即消失（0.0.2 用 OnGUI 实现，后续可替换 uGUI）。
    /// </summary>
    public class BubbleSystem : MonoBehaviour
    {
        public static BubbleSystem Instance { get; private set; }

        [Header("布局参数")]
        public float bottomOffsetRatio = 0.15f;   // 距底边 15%
        public float maxWidthRatio = 0.60f;       // 占屏幕宽 60%
        public float bubbleHeight = 48f;
        public float spacing = 8f;
        public int maxBubbles = 2;

        private readonly List<BubbleData> bubbles = new List<BubbleData>();
        private GUIStyle bubbleStyle;
        private GUIStyle shadowStyle;

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
            // 移除过期气泡
            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                if (bubbles[i].IsExpired)
                    bubbles.RemoveAt(i);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawBubbles();
        }

        private void EnsureStyles()
        {
            if (bubbleStyle != null) return;

            bubbleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true
            };
            bubbleStyle.normal.background = MakeColorTexture(new Color(0.05f, 0.05f, 0.05f, 0.75f));
            bubbleStyle.normal.textColor = Color.white;
            bubbleStyle.padding = new RectOffset(12, 12, 8, 8);

            shadowStyle = new GUIStyle(bubbleStyle);
            shadowStyle.normal.background = MakeColorTexture(new Color(0, 0, 0, 0.35f));
        }

        private void DrawBubbles()
        {
            if (bubbles.Count == 0) return;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float maxW = screenW * maxWidthRatio;
            float baseY = screenH * (1f - bottomOffsetRatio);

            // 从旧到新依次向上堆叠（旧的在上方，新的在下方顶出）
            for (int i = 0; i < bubbles.Count; i++)
            {
                var b = bubbles[i];
                float alpha = GetAlpha(ref b);
                if (alpha <= 0f) continue;

                float h = bubbleHeight;
                float y = baseY - (bubbles.Count - 1 - i) * (h + spacing);

                Rect shadowRect = new Rect(screenW * 0.5f - maxW * 0.5f + 2f, y + 2f, maxW, h);
                Rect rect = new Rect(screenW * 0.5f - maxW * 0.5f, y, maxW, h);

                // 阴影
                var old = GUI.color;
                GUI.color = new Color(0, 0, 0, 0.35f * alpha);
                GUI.Box(shadowRect, "", shadowStyle);

                // 背景 + 文字
                GUI.color = new Color(b.color.r, b.color.g, b.color.b, alpha);
                GUI.Box(rect, b.text, bubbleStyle);
                GUI.color = old;

                // 点击 dismiss
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    var updated = b;
                    updated.dismissed = true;
                    bubbles[i] = updated;
                }
            }
        }

        private float GetAlpha(ref BubbleData b)
        {
            float elapsed = Time.time - b.startTime;
            float fadeIn = 0.2f;
            float fadeOut = 0.4f;
            if (elapsed < fadeIn) return elapsed / fadeIn;
            if (b.durationSeconds - elapsed < fadeOut)
                return Mathf.Clamp01((b.durationSeconds - elapsed) / fadeOut);
            return 1f;
        }

        // ============ 公共 API ============

        /// <summary>弹出普通白色气泡（3秒）。</summary>
        public void Show(string text) => Show(text, Color.white, 3f);

        /// <summary>弹出指定颜色气泡。</summary>
        public void Show(string text, Color color, float duration)
        {
            if (string.IsNullOrEmpty(text)) return;

            var data = new BubbleData
            {
                text = text,
                color = color,
                durationSeconds = duration,
                startTime = Time.time,
                dismissed = false
            };

            bubbles.Add(data);
            if (bubbles.Count > maxBubbles)
                bubbles.RemoveAt(0); // 顶出最旧的气泡
        }

        /// <summary>按颜色名快捷弹出。</summary>
        public void Show(string text, string colorName, float duration)
        {
            Color c = ParseColor(colorName);
            Show(text, c, duration);
        }

        /// <summary>清除所有气泡。</summary>
        public void Clear()
        {
            bubbles.Clear();
        }

        private Color ParseColor(string name)
        {
            return name?.ToLower() switch
            {
                "red" => new Color(1f, 0.27f, 0.27f),   // #FF4444
                "gold" => new Color(1f, 0.84f, 0f),     // #FFD700
                "blue" => new Color(0.27f, 0.67f, 1f),  // #44AAFF
                _ => Color.white
            };
        }

        private static Texture2D MakeColorTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
