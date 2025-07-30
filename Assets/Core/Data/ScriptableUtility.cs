using System;
using UnityEditor;
using UnityEngine;

namespace Core.Data
{
    public class ScriptableUtility
    {
        // тут сделай напиши зачем нужен скрипт и что он делает, чтобы помнить и праймиться

#if UNITY_EDITOR
        /// <summary>
        ///     Changes the name of a ScriptableObject and its asset file to match the value of its enum field
        /// </summary>
        /// <typeparam name="T">Type of the enum</typeparam>
        /// <param name="scriptableObject">The ScriptableObject to rename</param>
        /// <param name="enumValue">The enum value to use for the name</param>
        public static void ChangeNameFromEnum<T>(ScriptableObject scriptableObject, T enumValue) where T : Enum
        {
            var newName = enumValue.ToString();
            var path = AssetDatabase.GetAssetPath(scriptableObject);

            // Rename the object
            scriptableObject.name = newName;

            // Rename the asset file
            AssetDatabase.RenameAsset(path, newName);
            EditorUtility.SetDirty(scriptableObject);
            AssetDatabase.SaveAssets();
        }
#endif

        /// <summary>
        ///     Converts a string to an enum value
        /// </summary>
        /// <typeparam name="T">Type of the enum to convert to</typeparam>
        /// <param name="value">String value to convert</param>
        /// <returns>Enum value</returns>
        public static T ConvertStringToEnum<T>(string value) where T : Enum
        {
            return (T)Enum.Parse(typeof(T), value);
        }
    }
}