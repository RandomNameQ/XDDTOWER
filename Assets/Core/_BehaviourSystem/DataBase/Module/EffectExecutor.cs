using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GeneratedEnums;
public static class EffectExecutor
{
    private static readonly Dictionary<EffectId, Action<Creature, Creature, EffectSO>> Handlers = new();
    private static readonly Dictionary<StatsId, Action<Creature, Creature, int>> StatHandlers = new();
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
        UnityEngine.Debug.Log($"EffectExecutor.Apply: {effect.name}");
    }

    public static void RegisterStat(StatsId id, Action<Creature, Creature, int> handler)
    {
        if (handler == null) return;
        StatHandlers[id] = handler;
    }

    public static void UnregisterStat(StatsId id)
    {
        if (StatHandlers.ContainsKey(id)) StatHandlers.Remove(id);
    }

    public static void ApplyStatistic(Creature self, Creature target, StatsId statId, int amount)
    {
        if (self == null || target == null) return;
        EnsureBuiltins();

        if (StatHandlers.TryGetValue(statId, out var handler))
        {
            handler(self, target, amount);
            UnityEngine.Debug.Log($"EffectExecutor.ApplyStatistic: {statId} += {amount}");
            return;
        }

        // Generic reflection-based fallback: try to find a numeric field or property matching statId
        TryApplyStatViaReflection(target, statId.ToString(), amount);
        UnityEngine.Debug.Log($"EffectExecutor.ApplyStatistic (fallback): {statId} += {amount}");
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

        // Builtin example for stats: Health via reflection-friendly path
        RegisterStat(default, (self, target, amount) => { }); // placeholder to ensure dictionary is non-empty
    }

    private static void TryApplyStatViaReflection(Creature target, string statName, int amount)
    {
        if (target == null || string.IsNullOrWhiteSpace(statName)) return;
        var type = target.GetType();
        var candidates = new List<string>
        {
            statName,
            "current" + statName,
            "Current" + statName,
        };

        // Try fields first
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var f in fields)
        {
            foreach (var n in candidates)
            {
                if (string.Equals(f.Name, n, StringComparison.OrdinalIgnoreCase))
                {
                    if (f.FieldType == typeof(int))
                    {
                        int cur = (int)f.GetValue(target);
                        f.SetValue(target, cur + amount);
                        return;
                    }
                    if (f.FieldType == typeof(float))
                    {
                        float cur = (float)f.GetValue(target);
                        f.SetValue(target, cur + amount);
                        return;
                    }
                }
            }
        }

        // Try properties next
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var p in props)
        {
            foreach (var n in candidates)
            {
                if (!p.CanRead || !p.CanWrite) continue;
                if (string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase))
                {
                    if (p.PropertyType == typeof(int))
                    {
                        int cur = (int)p.GetValue(target);
                        p.SetValue(target, cur + amount);
                        return;
                    }
                    if (p.PropertyType == typeof(float))
                    {
                        float cur = (float)p.GetValue(target);
                        p.SetValue(target, cur + amount);
                        return;
                    }
                }
            }
        }
    }
}


