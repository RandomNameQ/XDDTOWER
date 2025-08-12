using System.Collections.Generic;
using UnityEngine;

public abstract class RegistryDatabaseSO<T> : ScriptableObject where T : RegistryItemSO
{
    public List<T> Items = new();

#if UNITY_EDITOR
    [ContextMenu("Scan Assets and Populate")] 
    public void ScanAndPopulate()
    {
        Items.Clear();
        string filter = $"t:{typeof(T).Name}";
        var guids = UnityEditor.AssetDatabase.FindAssets(filter);
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                Items.Add(asset);
            }
        }
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }
#endif
}


