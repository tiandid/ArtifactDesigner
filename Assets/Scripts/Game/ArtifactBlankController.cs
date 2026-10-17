using System;
using System.Collections.Generic;
using ArtifactDesigner.Artifact;
using UnityEngine;

namespace ArtifactDesigner.Game
{
    /// <summary>
    /// 器坯运行时控制器：在场景/预制体上承载一个器坯实例，
    /// 提供挂载/卸下部件接口，供战斗与锻造界面调用。
    /// </summary>
    public class ArtifactBlankController : MonoBehaviour
    {
        [SerializeField] private ArtifactBlank blank;

        public ArtifactBlank Blank => blank;

        public event Action Changed;

        public void Init(string blankId, string name, int baseSlots, int baseWeight, int weightPerSlot)
        {
            blank = new ArtifactBlank(blankId, name, baseSlots, baseWeight, weightPerSlot);
            Changed?.Invoke();
        }

        public bool MountAt(int index, ArtifactPart part)
        {
            if (blank == null || part == null) return false;
            bool ok = blank.MountPart(index, part);
            if (ok) Changed?.Invoke();
            return ok;
        }

        public bool TryMount(ArtifactPart part)
        {
            if (blank == null || part == null) return false;
            bool ok = blank.TryMountPart(part);
            if (ok) Changed?.Invoke();
            return ok;
        }

        public ArtifactPart Unmount(int index)
        {
            if (blank == null) return null;
            var part = blank.UnmountPart(index);
            if (part != null) Changed?.Invoke();
            return part;
        }

        public List<ArtifactPart> GetMounted() => blank == null
            ? new List<ArtifactPart>()
            : blank.GetMountedParts();
    }
}