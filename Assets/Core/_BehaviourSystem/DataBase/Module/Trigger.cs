using System;
using UnityEngine;

/// <summary>
/// Базовый триггер: сообщает, когда правило должно сработать (OR между триггерами правила).
/// </summary>
[Serializable]
public abstract class Trigger
{
    /// <summary>
    /// Инициализация на старте носителя (подписки, сброс временных полей и т.п.).
    /// </summary>
    public virtual void Initialize(Creature self) { }

    /// <summary>
    /// Возвращает true в кадр, когда триггер сработал.
    /// </summary>
    public virtual bool TryFire(float deltaTime, Creature self) => false;
}


public class OnCooldown : Trigger
{
    public float cooldown = 2f;

    [NonSerialized]
    private float _timer;

    public override void Initialize(Creature self)
    {
        _timer = 0f;
    }

    public override bool TryFire(float deltaTime, Creature self)
    {
        _timer += deltaTime;
        if (_timer >= cooldown)
        {
            _timer = 0f;
            return true;
        }
        return false;
    }
}

public class OnDied : Trigger { }
public class OnPassive : Trigger { }

public class OnAllyDied : Trigger { }
public class OnNeighbourBonuses : Trigger
{
}

public class OnBattleBegin : Trigger
{
    [NonSerialized]
    private bool _pending;

    public override void Initialize(Creature self)
    {
        // Простейшая модель: считаем, что "начало боя" = первый кадр жизни существа
        _pending = true;
    }

    public override bool TryFire(float deltaTime, Creature self)
    {
        if (!_pending) return false;
        _pending = false;
        return true;
    }
}


public class OnAppliedEffect : Trigger { }

public class OnReceivedEffect : Trigger { }