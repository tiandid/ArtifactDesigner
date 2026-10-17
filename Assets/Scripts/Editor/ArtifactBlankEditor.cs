using ArtifactDesigner.Artifact;
using ArtifactDesigner.Core;
using ArtifactDesigner.Game;
using UnityEditor;
using UnityEngine;

namespace ArtifactDesigner.EditorTools
{
    /// <summary>
    /// 器坯自定义 Inspector：直观显示格子数、重量与每个槽位的部件挂载状态，
    /// 并可在编辑器内挂载/卸下部件调试。
    /// </summary>
    [CustomEditor(typeof(ArtifactBlankController))]
    public class ArtifactBlankEditor : UnityEditor.Editor
    {
        private PartType _dragType = PartType.Attack;

        public override void OnInspectorGUI()
        {
            var controller = (ArtifactBlankController)target;
            if (controller.Blank == null)
            {
                EditorGUILayout.HelpBox("尚未初始化器坯，请先调用 Init()。", MessageType.Info);
                DrawDefaultInspector();
                return;
            }

            var blank = controller.Blank;

            EditorGUILayout.LabelField("器坯信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("名称", blank.blankName);
            EditorGUILayout.LabelField("格子数", blank.TotalSlots.ToString());
            EditorGUILayout.LabelField("已用/空闲", $"{blank.UsedSlots}/{blank.FreeSlots}");
            EditorGUILayout.LabelField("重量", blank.TotalWeight.ToString());
            EditorGUILayout.LabelField("基础属性", blank.baseAttributes.ToString());
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("槽位", EditorStyles.boldLabel);
            for (int i = 0; i < blank.slots.Count; i++)
            {
                var part = blank.slots[i];
                EditorGUILayout.BeginHorizontal();
                if (part == null)
                {
                    EditorGUILayout.LabelField($"[{i}] 空", GUILayout.Width(280));
                }
                else
                {
                    EditorGUILayout.LabelField($"[{i}] {part.partName}（{part.partType}）", GUILayout.Width(280));
                    if (GUILayout.Button("卸下", GUILayout.Width(60)))
                        controller.Unmount(i);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("调试挂载", EditorStyles.boldLabel);
            _dragType = (PartType)EditorGUILayout.EnumPopup("待挂载部件类型", _dragType);
            if (GUILayout.Button("生成并挂载演示部件"))
            {
                var part = DemoParts.Create(_dragType);
                if (controller.TryMount(part))
                    Debug.Log($"已挂载演示部件 {part.partName}（{part.partType}）");
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("重置为 2 格器坯"))
            {
                controller.Init("blank_demo", "演示剑胚", 2, 10, 5);
            }
        }
    }
}