using System.Collections.Generic;
using UnityEngine;

public static class UnitsRegistry
{
    private static readonly HashSet<UnitInstance> units = new HashSet<UnitInstance>();
    public static IEnumerable<UnitInstance> AllUnits
    {
        get
        {
            units.RemoveWhere(u => u == null);
            return units;
        }
    }

    /// <summary>
    /// Enregistre une unité dans le registre global.
    /// </summary>
    public static void Register(UnitInstance unit, int unitsPlayerId)
    {
        if (unit != null)
        {
            units.Add(unit);
            Debug.Log($"Unit registered: {unit.name}. Total units: {units.Count}");
        }
    }

    /// <summary>
    /// Retire une unité du registre global.
    /// </summary>
    public static void Unregister(UnitInstance unit)
    {
        if (unit == null) return;
        units.Remove(unit);
    }

    /// <summary>
    /// Retourne une copie nettoyée du registre des unités.
    /// </summary>
    public static List<UnitInstance> GetSnapshot()
    {
        units.RemoveWhere(u => u == null);
        return new List<UnitInstance>(units);
    }
}
