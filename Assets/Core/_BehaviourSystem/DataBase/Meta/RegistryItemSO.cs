using Sirenix.OdinInspector;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;

public abstract class RegistryItemSO : ScriptableObject
{
    [SerializeField]
    private string id;


    public string Id => id;
    public virtual string DisplayName => name;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        var computed = ComputeDeterministicId();
        if (id != computed)
        {
            id = computed;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        var type = GetType();
        string filter = $"t:{type.Name}";
        var guids = UnityEditor.AssetDatabase.FindAssets(filter);
        int duplicates = 0;
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) as RegistryItemSO;
            if (asset == null) continue;
            if (asset == this) continue;
            if (asset.Id == id)
            {
                duplicates++;
            }
        }
        if (duplicates > 0)
        {
            UnityEngine.Debug.LogError($"Duplicate Id '{id}' detected for {type.Name} on asset '{name}'.");
        }
    }

    private string ComputeDeterministicId()
    {
        var input = name ?? string.Empty;
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = md5.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
        return sb.ToString();
    }
#endif
}


