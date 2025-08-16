using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;

public class CreatureLife : MonoBehaviour
{
    public int currentLife;
    public int maxLife;
    public Creature target;
    public Image life;
    [Range(0, 100)]
    public int lifeValue;
    
    [Header("Death State")]
    public bool isDead;

    private void Reset()
    {
        isDead = false;
        if (life == null)
        {
            life = GetComponent<Image>();
        }
        UpdateFill();
    }

    private void Start()
    {
        isDead = false;
        StartCoroutine(ASd());
        target = GetComponentInChildren<Creature>();
        UpdateFill();
    }
    public IEnumerator ASd()
    {
        yield return null;

        maxLife = (int)target.behaviorProfile.currentRang.deffence.maxHealh.baseValue;
        currentLife = maxLife;
        UpdateFill();

        yield return null;
    }

    private void OnValidate()
    {
        UpdateFill();
        if (Application.isPlaying)
        {
            CheckDeath();
        }
    }

    public void SetLife(int current, int max)
    {
        maxLife = Mathf.Max(1, max);
        currentLife = Mathf.Clamp(current, 0, maxLife);
        UpdateFill();
        CheckDeath();
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
            fill = Mathf.Clamp01(lifeValue / 100f);
        }
        else
        {
            float max = Mathf.Max(1, maxLife);
            float current = Mathf.Clamp(currentLife, 0, (int)max);
            fill = Mathf.Clamp01(current / max);
            lifeValue = Mathf.RoundToInt(fill * 100f);
        }

        life.fillAmount = fill;
    }

    public GeneratedEnums.EffectId effect;
    public Creature sources;
    public int valueEffect;
    public void HandleEffect(GeneratedEnums.EffectId effect, Creature sources)
    {
        this.effect = effect;
        this.sources = sources;
        FindValueEffectData();
        HandleOffenceData();
        switch (effect)
        {
            case GeneratedEnums.EffectId.Damage:
                HandleDamageEffect();
                break;

            case GeneratedEnums.EffectId.Burn:
                HandleDamageEffect();
                break;



        }
        UpdateFill();
        CheckDeath();

        UnitEvent.OnUnitRecieveEffectEvent(effect, target, sources);
        UnitEvent.OnUnitAppliedEffectEvent(effect, target, sources);
    }


    public void HandleOffenceData()
    {
    }

    public void HandleDamageEffect()
    {
        currentLife -= valueEffect;
    }

    public void HandleBurnEffect()
    {
        Debug.Log("birn");
    }

    public int FindValueEffectData()
    {
        valueEffect = sources.behaviorProfile.currentRang.rules[0].value.number.value;
        return valueEffect;
    }

    private void CheckDeath()
    {
        if (!isDead && currentLife <= 0)
        {
            isDead = true;
            UnitDied();
        }
    }

    [Button]
    public void UnitDied()
    {
        if (target == null) return;
        
        target.OnDeath(sources);
        
        if (life != null)
        {
            life.enabled = false;
        }
        
        gameObject.SetActive(false);
    }

}
