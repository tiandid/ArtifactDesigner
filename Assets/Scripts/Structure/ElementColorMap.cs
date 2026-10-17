using UnityEngine;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Structure
{
    /// <summary>
    /// 五行颜色映射：主色、发光色、粒子色。
    /// 同类型同境界的五行变体共用模型骨架，仅通过颜色区分。
    /// </summary>
    public static class ElementColorMap
    {
        // 主色（RGB）
        public static readonly Color[] MainColors = new Color[5]
        {
            new Color(0.788f, 0.690f, 0.216f), // 金 #C9B037
            new Color(0.133f, 0.545f, 0.133f), // 木 #228B22
            new Color(0.118f, 0.565f, 1.000f), // 水 #1E90FF
            new Color(0.863f, 0.078f, 0.235f), // 火 #DC143C
            new Color(0.545f, 0.271f, 0.075f)  // 土 #8B4513
        };

        // 发光色（RGB）
        public static readonly Color[] GlowColors = new Color[5]
        {
            new Color(1.000f, 0.973f, 0.863f), // 金 #FFF8DC
            new Color(0.565f, 0.933f, 0.565f), // 木 #90EE90
            new Color(0.529f, 0.808f, 0.980f), // 水 #87CEFA
            new Color(1.000f, 0.388f, 0.278f), // 火 #FF6347
            new Color(0.871f, 0.722f, 0.529f)  // 土 #DEB887
        };

        // 粒子色（RGB）
        public static readonly Color[] ParticleColors = new Color[5]
        {
            new Color(1.000f, 0.843f, 0.000f), // 金 #FFD700
            new Color(0.196f, 0.804f, 0.196f), // 木 #32CD32
            new Color(0.000f, 0.749f, 1.000f), // 水 #00BFFF
            new Color(1.000f, 0.271f, 0.000f), // 火 #FF4500
            new Color(0.824f, 0.412f, 0.118f)  // 土 #D2691E
        };

        public static Color GetMainColor(ElementType element) => MainColors[(int)element];
        public static Color GetGlowColor(ElementType element) => GlowColors[(int)element];
        public static Color GetParticleColor(ElementType element) => ParticleColors[(int)element];

        public static Color32 GetMainColor32(ElementType element)
        {
            var c = MainColors[(int)element];
            return new Color32(
                (byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
        }

        public static Color32 GetGlowColor32(ElementType element)
        {
            var c = GlowColors[(int)element];
            return new Color32(
                (byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
        }

        public static Color32 GetParticleColor32(ElementType element)
        {
            var c = ParticleColors[(int)element];
            return new Color32(
                (byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
        }
    }
}
