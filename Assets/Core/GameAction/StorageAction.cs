using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StorageAction", menuName = "ScriptableObjects/StorageAction")]
public class StorageAction : ScriptableObject
{
    public List<GameActionBase> shop = new();
    public List<GameActionBase> monster = new();
}
