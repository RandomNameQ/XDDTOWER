using UnityEditor;
using UnityEngine;

namespace Core.BoardV2.Editor
{
    [CustomEditor(typeof(BoardGridV2))]
    public class BoardGridV2Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var board = (BoardGridV2)target;

            GUILayout.Space(8);
            if (GUILayout.Button("Генерация"))
            {
                board.Regenerate();
                EditorUtility.SetDirty(board.gameObject);
            }
        }
    }
}


