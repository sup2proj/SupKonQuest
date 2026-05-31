using UnityEngine;
using System.Collections.Generic;

public class ProductionEasyNormal : MonoBehaviour
{
    [Header("Production")]
    [SerializeField, Min(0.1f)] private float productionCheckInterval = 0.25f;
    [SerializeField, Range(0f, 1f)] private float poweredRatio = 0.25f;
    [SerializeField, Min(1)] private int maxUnitCount = 40;

    [Header("Références")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private StructureManager structureManager;
    [SerializeField] private MapGenerator mapGenerator;

    private int playerId = -1;
    private float productionTimer;
    private float nextProductionReadyTime;
    private bool mapReady;
    private bool loggedWaitingForMap;

    /// <summary>
    /// Initialise le composant de production avec les références aux managers
    /// et l'identifiant du joueur géré.
    /// </summary>
    public void Initialize(PlayerManager pm, StructureManager sm, MapGenerator mg, int id)
    {
        if (playerManager == null) 
            playerManager = pm;
        if (structureManager == null) 
            structureManager = sm;
        if (mapGenerator == null) 
            mapGenerator = mg;
        playerId = id;
    }

    /// <summary>
    /// Indique si la map et les structures sont prêtes pour la production.
    /// </summary>
    /// <param name="ready">True si la map est prête.</param>
    public void SetMapReady(bool ready)
    {
        mapReady = ready;
    }

    /// <summary>
    /// Appelé régulièrement pour vérifier si la production peut avoir lieu
    /// et déclencher la création d'unités si les conditions sont réunies.
    /// </summary>
    public void Tick()
    {
        if (playerManager == null)
            playerManager = PlayerManager.Instance;
        if (structureManager == null)
            structureManager = StructureManager.Instance;
        if (mapGenerator == null)
            mapGenerator = MapGenerator.Instance;

        if (playerManager == null || structureManager == null)
            return;

        if (playerId < 0)
            playerId = playerManager.GetActivePlayerId();

        var ownedStructures = GetOwnedStructures();
        if (!mapReady && (ownedStructures == null || ownedStructures.Count == 0))
        {
            if (!loggedWaitingForMap)
                loggedWaitingForMap = true;
            return;
        }

        if (!mapReady && ownedStructures != null && ownedStructures.Count > 0)
            mapReady = true;

        productionTimer += Time.deltaTime;
        if (productionTimer < productionCheckInterval)
            return;

        productionTimer = 0f;
        TryProduceUnit();
    }

    /// <summary>
    /// Tente de produire une unité en vérifiant les conditions : nombre maximum,
    /// ressources disponibles et positions de spawn.
    /// </summary>
    private void TryProduceUnit()
    {
        if (GetOwnedUnitCount() >= maxUnitCount)
            return;
    
        if (Time.time < nextProductionReadyTime)
            return;
    
        PlayerSession session = playerManager.GetSession(playerId);
        if (session == null)
            return;
    
        if (structureManager == null || structureManager.unitData == null || structureManager.unitData.Count == 0)
            return;
    
        int gold = session.Gold;
        List<StructureInstance> owned = GetOwnedStructures();
    
        if (owned == null || owned.Count == 0)
            return;
    
        StructureInstance chosenStructure = owned[Random.Range(0, owned.Count)];
    
        bool chosenIsSpecial = chosenStructure.structureType == StructureType.NeutralStructure;
        bool hasSpecialStructure = HasSpecialStructure();
    
        UnitData chosenData = PickUnitForProduction(gold, hasSpecialStructure, chosenIsSpecial, out bool powered);
        if (chosenData == null)
            return;
    
        if (chosenIsSpecial)
            powered = true;
    
        if (!session.SpendGold(chosenData.price))
            return;
    
        Vector3 spawnPos = chosenStructure.StructurePosition;
        bool spawned = structureManager.SpawnUnitByTypeAtPosition(
            playerId,
            chosenData.type,
            spawnPos.x,
            spawnPos.z,
            powered,
            false,
            chosenStructure
        );
    
        if (!spawned)
        {
            session.AddGold(chosenData.price);
            return;
        }
    
        nextProductionReadyTime = Time.time + Mathf.Max(0.1f, chosenData.creationTime);
    }

    /// <summary>
    /// Compte le nombre d'unités appartenant au joueur géré par cette IA.
    /// </summary>
    /// <returns>Nombre d'unités possédées.</returns>
    private int GetOwnedUnitCount()
    {
        List<UnitInstance> units = UnitsRegistry.GetSnapshot();
        if (units == null)
            return 0;

        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            UnitInstance unit = units[i];
            if (unit == null || unit.playerId != playerId)
                continue;
            count++;
        }

        return count;
    }

    /// <summary>
    /// Sélectionne un <see cref="UnitData"/> candidat pour la production en
    /// fonction de l'or disponible et des structures spéciales.
    /// </summary>
    private UnitData PickUnitForProduction(int gold, bool hasSpecialStructure, bool requirePowered, out bool powered)
    {
        powered = false;

        List<UnitData> classics = new List<UnitData>();
        List<UnitData> poweredChoices = new List<UnitData>();

        for (int i = 0; i < structureManager.unitData.Count; i++)
        {
            UnitData data = structureManager.unitData[i];
            if (data == null || data.price > gold)
                continue;

            if (IsBoatType(data.type))
                continue;

            if (requirePowered)
            {
                poweredChoices.Add(data);
                continue;
            }

            classics.Add(data);
        }

        if (requirePowered)
        {
            if (poweredChoices.Count == 0)
                return null;

            powered = true;
            return poweredChoices[Random.Range(0, poweredChoices.Count)];
        }

        int roll = Random.Range(0, 100);
        int poweredThreshold = Mathf.RoundToInt(poweredRatio * 100f);

        if (hasSpecialStructure && poweredChoices.Count > 0 && roll < poweredThreshold)
        {
            powered = true;
            return poweredChoices[Random.Range(0, poweredChoices.Count)];
        }

        if (classics.Count > 0)
            return classics[Random.Range(0, classics.Count)];

        if (poweredChoices.Count > 0)
        {
            powered = true;
            return poweredChoices[Random.Range(0, poweredChoices.Count)];
        }

        return null;
    }

    /// <summary>
    /// Récupère la liste des structures appartenant à ce joueur (hors ports).
    /// </summary>
    /// <returns>Liste des <see cref="StructureInstance"/> possédées.</returns>
    private List<StructureInstance> GetOwnedStructures()
    {
        var result = new List<StructureInstance>();
        var structures = Object.FindObjectsByType<StructureInstance>(FindObjectsSortMode.None);
        if (structures == null) return result;
        for (int i = 0; i < structures.Length; i++)
        {
            var s = structures[i];
            if (s == null) continue;
            if (s.structureType == StructureType.Harbour) continue;
            if (s.playerId == playerId) result.Add(s);
        }
        return result;
    }

    /// <summary>
    /// Indique si le joueur possède au moins une structure de type spécial
    /// (par ex. NeutralStructure).
    /// </summary>
    /// <returns>True si une structure spéciale est présente.</returns>
    private bool HasSpecialStructure()
    {
        var structures = Object.FindObjectsByType<StructureInstance>(FindObjectsSortMode.None);
        if (structures == null)
            return false;
        for (int i = 0; i < structures.Length; i++)
        {
            var s = structures[i];
            if (s == null)
                continue;
            if (s.playerId == playerId && s.structureType == StructureType.NeutralStructure)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Indique si le type d'unité est un type naval (bateau) et doit être ignoré
    /// pour la production terrestre automatique.
    /// </summary>
    private bool IsBoatType(UnitsType type)
    {
        return type == UnitsType.Fregate || type == UnitsType.Destroyer || type == UnitsType.Transport;
    }
}

