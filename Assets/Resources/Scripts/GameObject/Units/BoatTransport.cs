using System.Collections;
using System.Collections.Generic;
using Enums.Environment;
using UnityEngine;

[RequireComponent(typeof(UnitInstance))]
public class BoatTransport : MonoBehaviour
{
    [Header("Transport")]
    [SerializeField] public List<TransportedUnitData> transportedUnits = new List<TransportedUnitData>();
    [SerializeField, Min(0.1f)] private float autoBoardRadius = 2f;
    [SerializeField, Min(0.02f)] private float autoBoardScanInterval = 0.15f;
    [SerializeField, Min(0f)] private float disembarkBoardingBlockDuration = 2f;

    private static readonly Dictionary<UnitInstance, BoatTransport> PendingBoardings = new Dictionary<UnitInstance, BoatTransport>();
    private static readonly Dictionary<UnitInstance, float> BoardingBlockedUntil = new Dictionary<UnitInstance, float>();

    private UnitInstance boatUnit;
    private float nextAutoBoardScanTime;
    private int reservedTransportSlots;

    [System.Serializable]
    public class TransportedUnitData
    {
        public string unitName;
        public UnitData unitData;
        public float currentHealth;
        public int playerId;
    }

    /// <summary>
    /// Récupère la référence vers l'unité du bateau au chargement du composant.
    /// </summary>
    private void Awake()
    {
        boatUnit = GetComponent<UnitInstance>();
    }

    /// <summary>
    /// Déclenche la vérification automatique des unités proches à chaque frame.
    /// </summary>
    private void Update()
    {
        TryAutoBoardNearbyUnits();
    }

    /// <summary>
    /// Libère toutes les réservations d'embarquement lorsque le bateau est détruit.
    /// </summary>
    private void OnDestroy()
    {
        ReleaseAllReservationsForThisBoat();
    }

    /// <summary>
    /// Récupère ou ajoute le composant BoatTransport sur une unité de bateau.
    /// </summary>
    public static BoatTransport GetOrAdd(UnitInstance unit)
    {
        if (!IsBoatUnit(unit))
            return null;

        BoatTransport transport = unit.GetComponent<BoatTransport>();
        if (transport == null)
            transport = unit.gameObject.AddComponent<BoatTransport>();

        return transport;
    }

    /// <summary>
    /// Retourne la capacité de transport restante pour un bateau donné.
    /// </summary>
    public static int GetAvailableCapacity(UnitInstance boat)
    {
        BoatTransport transport = GetOrAdd(boat);
        return transport != null ? transport.GetAvailableTransportCapacity() : 0;
    }

    /// <summary>
    /// Prépare l'embarquement d'une unité sur un bateau.
    /// </summary>
    public static bool PrepareBoarding(UnitInstance unit, UnitInstance boat)
    {
        BoatTransport transport = GetOrAdd(boat);
        return transport != null && transport.PrepareBoarding(unit);
    }

    /// <summary>
    /// Supprime une réservation d'embarquement en attente pour une unité.
    /// </summary>
    public static void ClearPendingBoarding(UnitInstance unit)
    {
        if (unit == null)
            return;

        if (!PendingBoardings.TryGetValue(unit, out BoatTransport transport))
            return;

        if (transport != null)
            transport.ReleaseTransportReservation();

        PendingBoardings.Remove(unit);
    }

    /// <summary>
    /// Bloque temporairement l'embarquement d'une unité.
    /// </summary>
    public static void BlockBoarding(UnitInstance unit, float duration)
    {
        if (unit == null)
            return;

        ClearPendingBoarding(unit);
        BoardingBlockedUntil[unit] = Time.time + Mathf.Max(0f, duration);
    }

    /// <summary>
    /// Fait débarquer toutes les unités transportées par un bateau.
    /// </summary>
    public static bool ExitAllUnits(UnitInstance boat)
    {
        BoatTransport transport = GetOrAdd(boat);
        return transport != null && transport.ExitAllUnitsInBoat();
    }

    /// <summary>
    /// Indique si l'unité fournie correspond à un bateau transporteur.
    /// </summary>
    public static bool IsBoatUnit(UnitInstance unit)
    {
        if (unit == null || unit.unitData == null)
            return false;
            
        switch (unit.unitData.type)
        {
            case UnitsType.Fregate:
            case UnitsType.Destroyer:
            case UnitsType.Transport:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Calcule la capacité de transport disponible en tenant compte des réservations.
    /// </summary>
    public int GetAvailableTransportCapacity()
    {
        return Mathf.Max(0, GetMaxTransportCapacity() - transportedUnits.Count - reservedTransportSlots);
    }

    /// <summary>
    /// Réserve une place à bord pour une unité avant son embarquement.
    /// </summary>
    public bool PrepareBoarding(UnitInstance unit)
    {
        if (!CanReserveBoarding(unit))
            return false;

        ClearPendingBoarding(unit);

        if (!TryReserveTransportSlot())
            return false;

        PendingBoardings[unit] = this;
        return true;
    }

    /// <summary>
    /// Vérifie si une unité peut légalement réserver une place à bord.
    /// </summary>
    private bool CanReserveBoarding(UnitInstance unit)
    {
        if (!IsBoatUnit(boatUnit))
            return false;

        if (unit == null || unit == boatUnit || unit.unitData == null)
            return false;

        if (unit.playerId != boatUnit.playerId)
            return false;

        if (unit.currentHealth <= 0f || unit.unitData.isProtector || IsBoatUnit(unit))
            return false;

        return true;
    }

    /// <summary>
    /// Réserve une place de transport interne si une capacité est disponible.
    /// </summary>
    private bool TryReserveTransportSlot()
    {
        if (GetAvailableTransportCapacity() <= 0)
            return false;

        reservedTransportSlots++;
        return true;
    }

    /// <summary>
    /// Libère une réservation interne de transport.
    /// </summary>
    private void ReleaseTransportReservation()
    {
        if (reservedTransportSlots > 0)
            reservedTransportSlots--;
    }

    /// <summary>
    /// Retourne la capacité maximale de transport du bateau.
    /// </summary>
    private int GetMaxTransportCapacity()
    {
        if (!IsBoatUnit(boatUnit) || !(boatUnit.unitData is UnitBoat boatData))
            return 0;

        return Mathf.Max(0, Mathf.FloorToInt(boatData.maxTransportCapacity));
    }

    /// <summary>
    /// Cherche automatiquement les unités proches à embarquer.
    /// </summary>
    private void TryAutoBoardNearbyUnits()
    {
        if (!IsBoatUnit(boatUnit))
            return;

        if (Time.time < nextAutoBoardScanTime)
            return;

        nextAutoBoardScanTime = Time.time + autoBoardScanInterval;
        CleanupTransportState();

        int maxCapacity = GetMaxTransportCapacity();
        if (maxCapacity <= 0 || transportedUnits.Count >= maxCapacity)
            return;

        List<UnitInstance> units = UnitsRegistry.GetSnapshot();
        float radius = Mathf.Max(0.1f, autoBoardRadius);
        float radiusSq = radius * radius;

        for (int i = 0; i < units.Count && transportedUnits.Count < maxCapacity; i++)
        {
            UnitInstance candidate = units[i];
            if (!CanAutoBoardUnit(candidate))
                continue;

            if (GetFlatClosestDistanceSqToUnit(candidate) > radiusSq)
                continue;

            if (StoreTransportedUnit(candidate))
                Destroy(candidate.gameObject, 0f);
        }
    }

    /// <summary>
    /// Vérifie si une unité candidate peut être embarquée automatiquement.
    /// </summary>
    private bool CanAutoBoardUnit(UnitInstance candidate)
    {
        if (!CanReserveBoarding(candidate))
            return false;

        if (IsBoardingBlocked(candidate))
            return false;

        if (PendingBoardings.TryGetValue(candidate, out BoatTransport reservedBoat))
            return reservedBoat == this;

        return GetAvailableTransportCapacity() > 0;
    }

    /// <summary>
    /// Vérifie si l'embarquement d'une unité est actuellement bloqué.
    /// </summary>
    private bool IsBoardingBlocked(UnitInstance unit)
    {
        if (unit == null)
            return true;

        if (!BoardingBlockedUntil.TryGetValue(unit, out float blockedUntil))
            return false;

        if (Time.time < blockedUntil)
            return true;

        BoardingBlockedUntil.Remove(unit);
        return false;
    }

    /// <summary>
    /// Stocke une unité embarquée dans la liste des passagers du bateau.
    /// </summary>
    private bool StoreTransportedUnit(UnitInstance unit)
    {
        if (!CanReserveBoarding(unit))
            return false;

        bool hadReservationOnThisBoat = PendingBoardings.TryGetValue(unit, out BoatTransport reservedBoat) && reservedBoat == this;
        if (hadReservationOnThisBoat)
            ReleaseTransportReservation();

        PendingBoardings.Remove(unit);

        if (transportedUnits.Count >= GetMaxTransportCapacity())
        {
            Debug.Log($"[BoatTransport] Embarquement refuse: {boatUnit.name} est plein ({transportedUnits.Count}/{GetMaxTransportCapacity()}).", this);
            return false;
        }

        UnitData storedRuntimeData = Instantiate(unit.unitData);
        transportedUnits.Add(new TransportedUnitData
        {
            unitName = unit.name,
            unitData = storedRuntimeData,
            currentHealth = unit.currentHealth,
            playerId = unit.playerId,
        });

        return true;
    }

    /// <summary>
    /// Débarque toutes les unités actuellement transportées.
    /// </summary>
    private bool ExitAllUnitsInBoat()
    {
        if (!IsBoatUnit(boatUnit) || transportedUnits.Count == 0)
            return false;

        if (StructureManager.Instance == null)
        {
            return false;
        }

        List<Vector3> exitPositions = FindClosestLandExitPositions(transportedUnits.Count);
        if (exitPositions.Count == 0)
        {
            return false;
        }

        int spawnedCount = 0;
        for (int i = transportedUnits.Count - 1; i >= 0; i--)
        {
            TransportedUnitData storedUnit = transportedUnits[i];
            if (storedUnit == null || storedUnit.unitData == null)
            {
                transportedUnits.RemoveAt(i);
                continue;
            }

            Vector3 exitPosition = exitPositions[Mathf.Min(spawnedCount, exitPositions.Count - 1)];
            UnitInstance spawned = SpawnRuntimeUnitAtPosition(
                storedUnit.unitData,
                storedUnit.currentHealth,
                exitPosition
            );

            if (spawned == null)
                continue;

            BlockBoarding(spawned, disembarkBoardingBlockDuration);
            transportedUnits.RemoveAt(i);
            spawnedCount++;
        }

        return spawnedCount > 0;
    }

    /// <summary>
    /// Recherche des positions de débarquement sur les terres les plus proches.
    /// </summary>
    private List<Vector3> FindClosestLandExitPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        MapGenerator map = MapGenerator.Instance != null ? MapGenerator.Instance : FindFirstObjectByType<MapGenerator>();
        if (map == null || map.allTiles == null || count <= 0)
            return positions;

        List<TileData> landTiles = new List<TileData>();
        int width = map.allTiles.GetLength(0);
        int height = map.allTiles.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tile = map.allTiles[x, y];
                if (tile != null && tile.groundType != GroundType.Water && tile.isWalkable)
                    landTiles.Add(tile);
            }
        }

        landTiles.Sort((a, b) =>
        {
            float distA = FlatDistanceSq(boatUnit.transform.position, TileToWorldPosition(map, a));
            float distB = FlatDistanceSq(boatUnit.transform.position, TileToWorldPosition(map, b));
            return distA.CompareTo(distB);
        });

        for (int i = 0; i < landTiles.Count && positions.Count < count; i++)
        {
            Vector3 basePosition = TileToWorldPosition(map, landTiles[i]);
            positions.Add(basePosition + GetExitOffset(positions.Count, map.tileSize));
        }

        return positions;
    }

    /// <summary>
    /// Nettoie les réservations d'embarquement invalides ou obsolètes.
    /// </summary>
    private void CleanupTransportState()
    {
        List<UnitInstance> toRemove = null;
        foreach (var entry in PendingBoardings)
        {
            if (entry.Key != null && entry.Value != null)
                continue;

            if (entry.Value == this)
                ReleaseTransportReservation();

            if (toRemove == null)
                toRemove = new List<UnitInstance>();
            toRemove.Add(entry.Key);
        }

        if (toRemove != null)
        {
            for (int i = 0; i < toRemove.Count; i++)
                PendingBoardings.Remove(toRemove[i]);
        }
    }

    /// <summary>
    /// Supprime toutes les réservations associées à ce bateau.
    /// </summary>
    private void ReleaseAllReservationsForThisBoat()
    {
        List<UnitInstance> toRemove = null;
        foreach (var entry in PendingBoardings)
        {
            if (entry.Value != this)
                continue;

            if (toRemove == null)
                toRemove = new List<UnitInstance>();
            toRemove.Add(entry.Key);
        }

        if (toRemove == null)
            return;

        for (int i = 0; i < toRemove.Count; i++)
            PendingBoardings.Remove(toRemove[i]);

        reservedTransportSlots = 0;
    }

    /// <summary>
    /// Calcule la distance au carré la plus faible entre ce bateau et une autre unité.
    /// </summary>
    private float GetFlatClosestDistanceSqToUnit(UnitInstance other)
    {
        if (other == null)
            return float.MaxValue;

        Collider[] selfColliders = GetComponentsInChildren<Collider>();
        Collider[] otherColliders = other.GetComponentsInChildren<Collider>();
        float bestDistanceSq = float.MaxValue;

        for (int i = 0; i < selfColliders.Length; i++)
        {
            Collider selfCollider = selfColliders[i];
            if (!IsUsableBoardingCollider(selfCollider))
                continue;

            for (int j = 0; j < otherColliders.Length; j++)
            {
                Collider otherCollider = otherColliders[j];
                if (!IsUsableBoardingCollider(otherCollider))
                    continue;

                Vector3 selfPoint = selfCollider.ClosestPoint(otherCollider.transform.position);
                Vector3 otherPoint = otherCollider.ClosestPoint(selfPoint);
                Vector3 refinedSelfPoint = selfCollider.ClosestPoint(otherPoint);
                float distanceSq = FlatDistanceSq(refinedSelfPoint, otherPoint);
                if (distanceSq < bestDistanceSq)
                    bestDistanceSq = distanceSq;
            }
        }

        if (bestDistanceSq < float.MaxValue)
            return bestDistanceSq;

        return FlatDistanceSq(transform.position, other.transform.position);
    }

    /// <summary>
    /// Indique si un collider peut être utilisé pour calculer un embarquement.
    /// </summary>
    private bool IsUsableBoardingCollider(Collider candidate)
    {
        return candidate != null && candidate.enabled && !candidate.isTrigger;
    }

    /// <summary>
    /// Calcule un léger décalage pour éviter le chevauchement des unités débarquées.
    /// </summary>
    private Vector3 GetExitOffset(int index, float tileSize)
    {
        if (index == 0)
            return Vector3.zero;

        float offset = Mathf.Max(0.15f, tileSize * 0.25f);
        switch (index % 8)
        {
            case 1: return new Vector3(offset, 0f, 0f);
            case 2: return new Vector3(-offset, 0f, 0f);
            case 3: return new Vector3(0f, 0f, offset);
            case 4: return new Vector3(0f, 0f, -offset);
            case 5: return new Vector3(offset, 0f, offset);
            case 6: return new Vector3(-offset, 0f, offset);
            case 7: return new Vector3(offset, 0f, -offset);
            default: return new Vector3(-offset, 0f, -offset);
        }
    }

    /// <summary>
    /// Convertit une tuile de la carte en position monde.
    /// </summary>
    private Vector3 TileToWorldPosition(MapGenerator map, TileData tile)
    {
        return new Vector3(tile.coordX * map.tileSize, 0f, tile.coordY * map.tileSize);
    }

    /// <summary>
    /// Calcule la distance au carré entre deux positions en ignorant l'axe vertical.
    /// </summary>
    private float FlatDistanceSq(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    /// <summary>
    /// Instancie une unité à partir de ses données runtime à une position donnée.
    /// </summary>
    public static UnitInstance SpawnRuntimeUnitAtPosition(UnitData runtimeData, float currentHealth, Vector3 position)
    {
        if (runtimeData == null)
            return null;

        if (!StructureManager.Instance.TryGetUnitPrefab(runtimeData.type, out GameObject prefab) || prefab == null)
        {
            Debug.LogWarning($"[BoatTransport] SpawnRuntimeUnitAtPosition: Aucun prefab configuré dans l'inspecteur pour {runtimeData.type}.");
            return null;
        }

        UnitData spawnedRuntimeData = Object.Instantiate(runtimeData);
        GameObject unitGO = Object.Instantiate(prefab, position, Quaternion.identity);
        UnitInstance instance = unitGO.GetComponent<UnitInstance>();
        if (instance == null)
        {
            Debug.LogWarning($"[BoatTransport] Le prefab {prefab.name} ne contient pas de UnitInstance component. Tentative d'ajouter dynamiquement.");
            instance = unitGO.AddComponent<UnitInstance>();
        }

        instance.Initialize(spawnedRuntimeData);
        ApplyCurrentHealth(instance, currentHealth);
        instance.StartCoroutine(ApplyCurrentHealthAfterStart(instance, currentHealth));
        GetOrAdd(instance);
        return instance;
    }

    /// <summary>
    /// Réapplique la santé courante après le démarrage de l'unité instanciée.
    /// </summary>
    private static IEnumerator ApplyCurrentHealthAfterStart(UnitInstance instance, float currentHealth)
    {
        yield return null;
        ApplyCurrentHealth(instance, currentHealth);
    }

    /// <summary>
    /// Ajuste la santé courante de l'unité instanciée et synchronise la barre de vie.
    /// </summary>
    private static void ApplyCurrentHealth(UnitInstance instance, float currentHealth)
    {
        if (instance == null || instance.unitData == null)
            return;

        instance.currentHealth = Mathf.Clamp(currentHealth, 0f, instance.unitData.maxHealth);
        if (instance.healthBar != null)
        {
            instance.healthBar.SetMaxHealth(instance.unitData.maxHealth);
            instance.healthBar.SetHealth(instance.currentHealth);
        }
    }
}
