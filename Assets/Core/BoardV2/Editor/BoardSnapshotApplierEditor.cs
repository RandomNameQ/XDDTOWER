using UnityEditor;
using UnityEngine;

namespace Core.BoardV2.Snapshots.Editor
{
    [CustomEditor(typeof(BoardSnapshotApplier))]
    public class BoardSnapshotApplierEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var applier = (BoardSnapshotApplier)target;
            GUILayout.Space(8);
            if (GUILayout.Button("Применить снимок (Apply)"))
            {
                applier.Apply();
                EditorUtility.SetDirty(applier);
            }
        }
    }
}


