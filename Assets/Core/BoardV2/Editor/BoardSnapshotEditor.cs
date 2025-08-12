using UnityEditor;
using UnityEngine;

namespace Core.BoardV2.Snapshots.Editor
{
    [CustomEditor(typeof(BoardSnapshotSO))]
    public class BoardSnapshotEditor : UnityEditor.Editor
    {
        private static GameObject tempPicked;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var snapshot = (BoardSnapshotSO)target;
            GUILayout.Space(8);
            EditorGUILayout.HelpBox("Выбери объект со сцены (корень/дочерний BoardGridV2) или используй текущее выделение в Hierarchy.", MessageType.Info);

            tempPicked = (GameObject)EditorGUILayout.ObjectField("Объект со сцены", tempPicked, typeof(GameObject), true);
            using (new EditorGUI.DisabledScope(tempPicked == null || tempPicked.GetComponentInParent<BoardGridV2>() == null))
            {
                if (GUILayout.Button("Сохранить из выбранного грида"))
                {
                    var grid = tempPicked.GetComponentInParent<BoardGridV2>();
                    snapshot.CaptureFromGrid(grid);
                    EditorUtility.SetDirty(snapshot);
                    AssetDatabase.SaveAssets();
                }
            }

            GUILayout.Space(4);
            using (new EditorGUI.DisabledScope(Selection.activeGameObject == null || Selection.activeGameObject.GetComponentInParent<BoardGridV2>() == null))
            {
                if (GUILayout.Button("Сохранить из выделенного грида (Selection)"))
                {
                    var sel = Selection.activeGameObject;
                    var grid = sel.GetComponentInParent<BoardGridV2>();
                    snapshot.CaptureFromGrid(grid);
                    EditorUtility.SetDirty(snapshot);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}


