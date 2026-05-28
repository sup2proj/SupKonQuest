using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DamageTable", menuName = "Game/Damage Table")]
public class DamageTable : ScriptableObject
{
    [System.Serializable]
    public class DamageRelation
    {
        public UnitsType attackerType;
        public UnitsType defenderType;
        public StructureType defenderStructureType;
        public float damageMultiplier = 1f;
    }

    public List<DamageRelation> relations = new List<DamageRelation>
    {
        // INFANTRY

        new DamageRelation { attackerType = UnitsType.Infantry, defenderType = UnitsType.Archer,         damageMultiplier = 1.3f },
        new DamageRelation { attackerType = UnitsType.Infantry, defenderType = UnitsType.Mortar,         damageMultiplier = 1.3f },

        new DamageRelation { attackerType = UnitsType.Infantry, defenderType = UnitsType.Heavy,          damageMultiplier = 0.7f },

        // ARCHER

        new DamageRelation { attackerType = UnitsType.Archer, defenderType = UnitsType.Infantry,         damageMultiplier = 1.3f },

        new DamageRelation { attackerType = UnitsType.Archer, defenderType = UnitsType.Heavy,            damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Archer, defenderType = UnitsType.AntiBlindage,     damageMultiplier = 0.7f },

        // HEAVY

        new DamageRelation { attackerType = UnitsType.Heavy, defenderType = UnitsType.Infantry,          damageMultiplier = 1.3f },
        new DamageRelation { attackerType = UnitsType.Heavy, defenderType = UnitsType.Archer,            damageMultiplier = 1.3f },

        new DamageRelation { attackerType = UnitsType.Heavy, defenderStructureType = StructureType.Structure,  damageMultiplier = 1.3f },
        new DamageRelation { attackerType = UnitsType.Heavy, defenderStructureType = StructureType.Harbour,    damageMultiplier = 1.3f },

        new DamageRelation { attackerType = UnitsType.Heavy, defenderType = UnitsType.AntiBlindage,      damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Heavy, defenderType = UnitsType.Mortar,            damageMultiplier = 0.7f },

        // ANTI-BLINDAGE

        new DamageRelation { attackerType = UnitsType.AntiBlindage, defenderType = UnitsType.Heavy,      damageMultiplier = 1.3f },

        new DamageRelation { attackerType = UnitsType.AntiBlindage, defenderStructureType = StructureType.Structure,  damageMultiplier = 1.3f },
        new DamageRelation { attackerType = UnitsType.AntiBlindage, defenderStructureType = StructureType.Harbour,    damageMultiplier = 1.3f },

        new DamageRelation { attackerType = UnitsType.AntiBlindage, defenderType = UnitsType.Infantry,   damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.AntiBlindage, defenderType = UnitsType.Archer,     damageMultiplier = 0.7f },

        // MORTAR

        new DamageRelation { attackerType = UnitsType.Mortar, defenderType = UnitsType.Heavy,            damageMultiplier = 1.3f },

        new DamageRelation { attackerType = UnitsType.Mortar, defenderType = UnitsType.Infantry,         damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Mortar, defenderType = UnitsType.Archer,           damageMultiplier = 0.7f },

        // SUPPORT

        new DamageRelation { attackerType = UnitsType.Support, defenderType = UnitsType.Infantry,           damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Support, defenderType = UnitsType.Archer,             damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Support, defenderType = UnitsType.Mortar,             damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Support, defenderType = UnitsType.Heavy,              damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Support, defenderType = UnitsType.AntiBlindage,       damageMultiplier = 0.7f }, 

        // HEALER

        new DamageRelation { attackerType = UnitsType.Healer, defenderType = UnitsType.Infantry,           damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Healer, defenderType = UnitsType.Archer,             damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Healer, defenderType = UnitsType.Mortar,             damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Healer, defenderType = UnitsType.Heavy,              damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Healer, defenderType = UnitsType.AntiBlindage,       damageMultiplier = 0.7f }, 

        // TRANSPORT NAVAL

        new DamageRelation { attackerType = UnitsType.Transport, defenderType = UnitsType.Fregate,       damageMultiplier = 0.7f },
        new DamageRelation { attackerType = UnitsType.Transport, defenderType = UnitsType.Destroyer,     damageMultiplier = 0.7f },

        // FREGATE

        new DamageRelation { attackerType = UnitsType.Fregate, defenderType = UnitsType.Transport,       damageMultiplier = 1.3f },
        new DamageRelation { attackerType = UnitsType.Fregate, defenderStructureType = StructureType.Harbour,    damageMultiplier = 1.3f },

        new DamageRelation { attackerType = UnitsType.Fregate, defenderType = UnitsType.Destroyer,       damageMultiplier = 0.7f },

        // DESTROYER

        new DamageRelation { attackerType = UnitsType.Destroyer, defenderType = UnitsType.Fregate,       damageMultiplier = 1.3f },
        new DamageRelation { attackerType = UnitsType.Destroyer, defenderType = UnitsType.Transport,     damageMultiplier = 1.3f },
        new DamageRelation { attackerType = UnitsType.Destroyer, defenderStructureType = StructureType.Harbour,    damageMultiplier = 1.3f },
    };

    /// <summary>
    /// Calcule le multiplicateur de dégâts en fonction de l'attaquant et de la cible.
    /// </summary>
    public float GetMultiplier(UnitInstance attacker, UnitInstance target, StructureInstance targetStructure = null)
    {
        Debug.Log($"[DamageTable] Calculating multiplier for {attacker?.name} attacking {target?.name}");
        if (attacker == null || target == null ||
            attacker.unitData == null || target.unitData == null)
            return 1f;

        foreach (var r in relations)
        {
            if (targetStructure != null)
            {
                if (r.attackerType == attacker.unitData.type &&
                    r.defenderStructureType == targetStructure.structureType)
                {
                    return r.damageMultiplier;
                }
            }
            if (r.attackerType == attacker.unitData.type &&
                r.defenderType == target.unitData.type)
            {
                return r.damageMultiplier;
            }
        }

        return 1f;
    }
}