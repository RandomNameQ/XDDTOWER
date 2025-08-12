using UnityEngine;

public class SpawnContentInMap : MonoBehaviour
{
    // вопрос когда спавнить контент
    public StorageAction storageAction;

    public void GenerateContent()
    {
        // монстров надо спавнить соотсвтетсно уровню, а не просто так = нужно сила монстров
        // нудна сила игрока и в целом баланс но пока покс

        FindMonsterCells();
    }

    public void FindMonsterCells()
    {
        var cells = FindObjectsByType<CellData>(FindObjectsSortMode.None);
        foreach (var cell in cells)
        {
            if (cell.cellVariant == CellData.CellVariant.Monster)
            {
                var randomMonster = storageAction.monster[RandomManager.Instance.Range(0, storageAction.monster.Count)];
                cell.InitAction(randomMonster);
            }
            if (cell.cellVariant == CellData.CellVariant.Shop)
            {
                var rndMonster = storageAction.shop[RandomManager.Instance.Range(0, storageAction.shop.Count)];
                cell.InitAction(rndMonster);
            }

        }
    }

}
