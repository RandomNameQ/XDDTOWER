using System;
using System.Collections.Generic;
using GeneratedEnums;
public static class EffectExecutor
{
    private static readonly Dictionary<EffectId, Action<Creature, Creature, EffectSO>> Handlers = new();
    private static bool _initialized;

    public static void Register(EffectId id, Action<Creature, Creature, EffectSO> handler)
    {
        if (handler == null) return;
        Handlers[id] = handler;
    }

    public static void Unregister(EffectId id)
    {
        if (Handlers.ContainsKey(id)) Handlers.Remove(id);
    }

    public static void Apply(Creature self, Creature target, EffectSO effect)
    {
        if (effect == null || self == null || target == null) return;
        EnsureBuiltins();

        var id = ResolveId(effect.name);
        if (id == EffectId.None)
        {
            return;
        }

        if (Handlers.TryGetValue(id, out var handler))
        {
            handler(self, target, effect);
        }
    }

    private static EffectId ResolveId(string effectName)
    {
        if (string.IsNullOrWhiteSpace(effectName)) return EffectId.None;
        // Имя enum генерируется из имён ассетов. Совпадение по точному имени.
        // Пытаемся распарсить в GeneratedEnums.EffectId
        if (Enum.TryParse<EffectId>(effectName, ignoreCase: false, out var id))
        {
            return id;
        }
        return EffectId.None;
    }

    private static void EnsureBuiltins()
    {
        if (_initialized) return;
        _initialized = true;

        Register(EffectId.Damage, (self, target, so) =>
        {
            var amount = Math.Max(0, so.amount);
            target.currentHealth -= amount;
        });

        Register(EffectId.Heal, (self, target, so) =>
        {
            var amount = Math.Max(0, so.amount);
            target.currentHealth += amount;
        });
    }
}


