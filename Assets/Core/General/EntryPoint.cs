using System.Collections;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class EntryPoint : MonoBehaviour
{
    // пока без меню тестоваяр версия

    public void Start()
    {
        StartGame();
    }

    [Button]
    public void StartGame()
    {

        GenerateMap();

        PutPlayerOnMap();
        // StartCoroutine(LateStart());

    }
    public void GenerateMap()
    {
        FindAnyObjectByType<MapGenerator>().GenerateMap();
    }

    public void PutPlayerOnMap()
    {
        var player = FindAnyObjectByType<PlayerController>();
        var startCell = FindObjectsByType<CellData>(FindObjectsSortMode.None).FirstOrDefault(cell => cell.cellVariant == CellData.CellVariant.Start);
        player.ChangePositionOnMap(startCell);
    }

    public IEnumerator LateStart()
    {
        GenerateMap();

        yield return new WaitForSeconds(1f);
        PutPlayerOnMap();

        yield return null;


    }
}
