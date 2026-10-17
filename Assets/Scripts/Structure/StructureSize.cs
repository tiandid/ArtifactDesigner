using System;
using UnityEngine;

namespace ArtifactDesigner.Structure
{
    /// <summary>
    /// 结构尺寸定义：宽高（网格格数）与占格总数。
    /// </summary>
    [Serializable]
    public struct StructureSize
    {
        public int Width;   // X方向格数
        public int Height;  // Y方向格数
        public int Area => Width * Height;

        public StructureSize(int w, int h)
        {
            Width = w;
            Height = h;
        }

        public override string ToString() => $"{Width}x{Height}";

        /// <summary>是否为正方形（灵核）。</summary>
        public bool IsSquare => Width == Height;

        /// <summary>长边与短边之差（推进器要求差1）。</summary>
        public int EdgeDiff => Mathf.Abs(Width - Height);
    }
}
