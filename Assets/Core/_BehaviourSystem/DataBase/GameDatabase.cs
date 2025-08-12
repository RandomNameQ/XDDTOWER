using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "GameDatabase", menuName = "DB/GameDatabase", order = 10)]

public class GameDatabase : ScriptableObject
{
    public List<RaceSO> Races = new();
    public List<EffectSO> Effects = new();
    public List<ActionSO> Actions = new();
    public List<ModifierSO> Modifiers = new();
    public List<AttitudeSO> Attitudes = new();
    public List<DirectionSO> Directions = new();
    public List<StatsSO> Stats = new();

#if UNITY_EDITOR
    [Button]
    public void ScanAndPopulate()
    {
        Races.Clear();
        Effects.Clear();
        Actions.Clear();
        Modifiers.Clear();
        Attitudes.Clear();
        Directions.Clear();
        Stats.Clear();

        foreach (var race in LoadAllAssets<RaceSO>())
            Races.Add(race);

        foreach (var effect in LoadAllAssets<EffectSO>())
            Effects.Add(effect);

        foreach (var action in LoadAllAssets<ActionSO>())
            Actions.Add(action);

        foreach (var modifier in LoadAllAssets<ModifierSO>())
            Modifiers.Add(modifier);

        foreach (var attitude in LoadAllAssets<AttitudeSO>())
            Attitudes.Add(attitude);

        foreach (var direction in LoadAllAssets<DirectionSO>())
            Directions.Add(direction);

        foreach (var stats in LoadAllAssets<StatsSO>())
            Stats.Add(stats);

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }

    private static IEnumerable<T> LoadAllAssets<T>() where T : ScriptableObject
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject");
        int total = 0;
        int matched = 0;
        int nameMatchedButDifferentType = 0;
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (obj == null) continue;
            total++;
            var objType = obj.GetType();
            var targetType = typeof(T);
            bool exactOrSubclass = objType == targetType || objType.IsSubclassOf(targetType);
            if (exactOrSubclass)
            {
                matched++;
                var casted = obj as T;
                if (casted != null) yield return casted;
                continue;
            }
            if (objType.Name == targetType.Name)
            {
                nameMatchedButDifferentType++;
                UnityEngine.Debug.LogWarning($"Asset '{path}' has type '{objType.FullName}' which matches name '{targetType.Name}' but is a different runtime Type.");
            }
        }
        UnityEngine.Debug.Log($"Scan {typeof(T).Name}: total:{total} matched:{matched} mismatchedByName:{nameMatchedButDifferentType}");
    }
#endif
}


