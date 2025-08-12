using UnityEngine;

public class CreatureLink : MonoBehaviour, ICreatureComponent
{
    [SerializeField] private ScriptableObject creature;

    public ScriptableObject CreatureData
    {
        get => creature;
        set => creature = value;
    }
}


