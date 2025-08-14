using UnityEngine;
using UnityEngine.UI;

public class CreatureLife : MonoBehaviour
{
    public int currentLifeForTest = 100;
    public int maxLifeForTest = 100;
    public Image life;
    [Range(0, 100)]
    public int lifeValue;

    private void Reset()
    {
        if (life == null)
        {
            life = GetComponent<Image>();
        }
        UpdateFill();
    }

    private void Start()
    {
        UpdateFill();
    }

    private void OnValidate()
    {
        UpdateFill();
    }

    public void SetLife(int current, int max)
    {
        currentLifeForTest = Mathf.Max(0, current);
        maxLifeForTest = Mathf.Max(1, max);
        UpdateFill();
    }

    private void UpdateFill()
    {
        if (life == null)
        {
            return;
        }

        float fill = 0f;

        if (!Application.isPlaying)
        {
            // В редакторе (вне Play Mode) используем lifeValue как процент
            fill = Mathf.Clamp01(lifeValue / 100f);
        }
        else
        {
            // Во время игры используем текущее/максимальное здоровье
            float max = Mathf.Max(1, maxLifeForTest);
            float current = Mathf.Clamp(currentLifeForTest, 0, (int)max);
            fill = Mathf.Clamp01(current / max);
            lifeValue = Mathf.RoundToInt(fill * 100f);
        }

        life.fillAmount = fill;
    }
}
