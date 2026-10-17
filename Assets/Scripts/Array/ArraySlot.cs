using UnityEngine;

namespace ArtifactDesigner.Array
{
    /// <summary>
    /// 阵盘格位：坐标、是否阵眼、当前灵物、计算后的有效值。
    /// </summary>
    public class ArraySlot
    {
        public Vector2Int Position { get; }
        public bool IsEye => Position.x == 0 && Position.y == 0;
        public int Radius { get; private set; }
        public SpiritItem Item { get; private set; }
        public bool HasItem => Item != null;
        public float EffectiveValue { get; set; }

        public ArraySlot(int x, int y, int radius)
        {
            Position = new Vector2Int(x, y);
            Radius = radius;
            EffectiveValue = 0f;
        }

        public bool PlaceItem(SpiritItem item)
        {
            if (IsEye) return false;
            if (Item != null) return false;
            Item = item;
            return true;
        }

        public SpiritItem RemoveItem()
        {
            if (IsEye) return null;
            var removed = Item;
            Item = null;
            EffectiveValue = 0f;
            return removed;
        }

        public void Clear()
        {
            Item = null;
            EffectiveValue = 0f;
        }
    }
}
