using System;
using UnityEngine;

namespace ArtifactDesigner.SpiritGathering
{
    /// <summary>
    /// 阵盘上的单个格位。
    /// 每个格位有坐标、是否可摆放、当前摆放的灵物。
    /// </summary>
    [Serializable]
    public class ArraySlot
    {
        /// <summary>格位在阵盘上的 X 坐标（阵眼为 0）。</summary>
        public int X;

        /// <summary>格位在阵盘上的 Y 坐标（阵眼为 0）。</summary>
        public int Y;

        /// <summary>此格位是否可用于摆放灵物（阵眼通常为 false）。</summary>
        public bool IsPlaceable;

        /// <summary>当前摆放在此格位上的灵物（null 表示空）。</summary>
        public SpiritItem PlacedItem;

        /// <summary>此格位到阵眼的曼哈顿距离。</summary>
        public int ManhattanDistance => Mathf.Abs(X) + Mathf.Abs(Y);

        /// <summary>此格位是否在阵盘有效范围内。</summary>
        public bool IsValid(int arrayRadius)
        {
            return Mathf.Abs(X) <= arrayRadius && Mathf.Abs(Y) <= arrayRadius;
        }

        /// <summary>判断是否与另一格位相邻（上下左右，不含对角）。</summary>
        public bool IsAdjacentTo(ArraySlot other)
        {
            int dx = Mathf.Abs(X - other.X);
            int dy = Mathf.Abs(Y - other.Y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        /// <summary>获取该格位上的灵物对阵眼的距离衰减系数。</summary>
        public float GetDistanceFactor()
        {
            int dist = ManhattanDistance;
            if (dist == 0) return 0f; // 阵眼不可放
            if (dist == 1) return 1.0f;
            float factor = 1.0f - (dist - 1) * 0.15f;
            return Mathf.Max(0.15f, factor);
        }

        /// <summary>判断目标格位是否在此格位的周围 8 格范围内（用于阵旗效果）。</summary>
        public bool IsInSurrounding8(ArraySlot target)
        {
            int dx = Mathf.Abs(X - target.X);
            int dy = Mathf.Abs(Y - target.Y);
            return dx <= 1 && dy <= 1 && (dx + dy) > 0;
        }

        /// <summary>判断目标格位是否在此格位的周围 4 格范围内（上下左右，用于相邻加成）。</summary>
        public bool IsInSurrounding4(ArraySlot target)
        {
            return IsAdjacentTo(target);
        }

        /// <summary>摆放灵物到此格位。</summary>
        public bool PlaceItem(SpiritItem item)
        {
            if (!IsPlaceable || PlacedItem != null || item == null)
                return false;
            PlacedItem = item;
            return true;
        }

        /// <summary>从此格位移除灵物。</summary>
        public SpiritItem RemoveItem()
        {
            var item = PlacedItem;
            PlacedItem = null;
            return item;
        }

        /// <summary>检查此格位是否为空。</summary>
        public bool IsEmpty => PlacedItem == null;
    }
}
