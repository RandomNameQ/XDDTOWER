using UnityEngine;

namespace Core.BoardV2
{
    public static class CreatureFactory
    {
        public static GameObject InstantiateCreature(ScriptableObject profile)
        {
            if (profile == null) return null;
            GameObject go;
            var prefab = TryGetField<GameObject>(profile, "spellPrefab");
            if (prefab != null) go = Object.Instantiate(prefab);
            else { go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = profile.name; }

            var link = go.GetComponent<ICreatureComponent>();
            if (link == null) link = go.AddComponent<CreatureLink>();
            link.CreatureData = profile;

            var init = go.GetComponent<IInitFromSO>();
            init?.InitDataSO();
            return go;
        }

        private static T TryGetField<T>(ScriptableObject obj, string fieldName) where T : class
        {
            if (obj == null || string.IsNullOrEmpty(fieldName)) return null;
            var fi = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (fi == null) return null;
            var v = fi.GetValue(obj);
            return v as T;
        }
    }
}


