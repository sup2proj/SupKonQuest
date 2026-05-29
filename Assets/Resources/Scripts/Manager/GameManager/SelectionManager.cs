using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    public RectTransform SelectionBox;
    public List<SelectableObject> AllSelectableObjects;
    public List<SelectableObject> CurrentlySelectedObjects;
    [SerializeField, Min(0.05f)] private float selectableRefreshInterval = 0.25f;
    private float groupMoveStoppingDistance = 0.3f;
    [SerializeField, Min(1f)] private float enemyUnitClickScreenRadius = 45f;

    bool isMouseDown, isDragging = false;
    float selectableRefreshTimer;

    Vector3 mouseStartPos;

    /// <summary>
    /// Enregistre cette instance comme gestionnaire de sélection global.
    /// </summary>
    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Gère les entrées souris, la sélection et les ordres de déplacement/attaque.
    /// </summary>
    void Update()
    {
        if (Mouse.current == null)
            return;

        if (IsActivePlayerDefeated())
        {
            ClearCurrentSelection();
            return;
        }

        AnalyzeSelectableObjectsContinuously();

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (IsPointerOverUi())
                return;

            bool attackOrderIssued = TryIssueAttackMoveOrder();
            if (!attackOrderIssued)
                TryIssueGroupMoveOrder();
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUi())
            {
                ResetSelectionDrag();
                return;
            }

            if (TryIssueAttackMoveOrder())
            {
                ResetSelectionDrag();
                return;
            }

            if (TryIssueBoatShoreOrder())
            {
                ResetSelectionDrag();
                return;
            }

            isMouseDown = true;
            mouseStartPos = Mouse.current.position.ReadValue();
            foreach (SelectableObject so in CurrentlySelectedObjects)
            {
                so.DeselectMe();
            }
            CurrentlySelectedObjects.Clear();
        }
    
        if (isMouseDown)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();

            if (Vector3.Distance(mouseStartPos, currentMousePos) > 1 && !isDragging)
            {
                isDragging = true;
                SelectionBox.gameObject.SetActive(true);
            }
            if (isDragging)
            {
                float boxWidth = Mathf.Abs(currentMousePos.x - mouseStartPos.x);
                float boxHeight = Mathf.Abs(currentMousePos.y - mouseStartPos.y);

                SelectionBox.sizeDelta = new Vector2(boxWidth, boxHeight);
                SelectionBox.anchoredPosition = (mouseStartPos + currentMousePos) / 2;

                SelectUnits();
            }
        }
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ResetSelectionDrag();
        }
    }

    /// <summary>
    /// Réinitialise l'état du drag de sélection.
    /// </summary>
    private void ResetSelectionDrag()
    {
        isMouseDown = false;
        isDragging = false;
        if (SelectionBox != null)
            SelectionBox.gameObject.SetActive(false);
    }

    /// <summary>
    /// Désélectionne toutes les unités actuellement sélectionnées.
    /// </summary>
    public void ClearCurrentSelection()
    {
        if (CurrentlySelectedObjects != null)
        {
            for (int i = CurrentlySelectedObjects.Count - 1; i >= 0; i--)
            {
                SelectableObject so = CurrentlySelectedObjects[i];
                if (so != null)
                    so.DeselectMe();
            }

            CurrentlySelectedObjects.Clear();
        }

        ResetSelectionDrag();
    }

    /// <summary>
    /// Indique si le joueur actif a déjà été vaincu.
    /// </summary>
    private bool IsActivePlayerDefeated()
    {
        if (PlayerManager.Instance == null)
            return false;

        return Defeat.IsPlayerDefeated(PlayerManager.Instance.GetActivePlayerId());
    }

    /// <summary>
    /// Vérifie si le pointeur est au-dessus d'un élément d'interface.
    /// </summary>
    private bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
            return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        return EventSystem.current.IsPointerOverGameObject(-1);
    }

    /// <summary>
    /// Envoie un ordre de déplacement de groupe vers la position cliquée.
    /// </summary>
    private void TryIssueGroupMoveOrder()
    {
        if (IsActivePlayerDefeated())
            return;

        if (CurrentlySelectedObjects == null || CurrentlySelectedObjects.Count == 0 || Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        int activePlayerId = PlayerManager.Instance.GetActivePlayerId();
        List<UnitInstance> groupUnits = CollectSelectedPlayerUnits(activePlayerId);

        if (groupUnits.Count == 0)
            return;

        List<Vector3> slots = GetFormationPositions(hit.point, groupUnits.Count);

        for (int i = 0; i < groupUnits.Count; i++)
        {
            UnitInstance unit = groupUnits[i];
            Vector3 destination = slots[i];
            destination.y = unit.transform.position.y;
            MovementManager.Instance.MoveBoatsUnitToPositionAsGroup(unit, destination, groupMoveStoppingDistance);
        }
    }

    /// <summary>
    /// Génère des positions de formation autour d'un point central.
    /// </summary>
    private List<Vector3> GetFormationPositions(Vector3 center, int total)
    {
        List<Vector3> slots = new List<Vector3>();

        if (total == 1)
        {
            slots.Add(SampleNavMesh(center));
            return slots;
        }

        float unitSpacing = 1f; // distance entre deux unités voisines

        // Cercle 0 : le centre lui-même
        slots.Add(SampleNavMesh(center));
        if (slots.Count >= total) return slots;

        // Cercles concentriques
        int ring = 1;
        while (slots.Count < total)
        {
            float radius = ring * unitSpacing;
            // Nombre d'unités qui tiennent sur ce cercle (circonférence / espacement)
            int unitsOnRing = Mathf.Max(1, Mathf.RoundToInt(2f * Mathf.PI * radius / unitSpacing));
            int toPlace = Mathf.Min(unitsOnRing, total - slots.Count);

            for (int i = 0; i < toPlace; i++)
            {
                float angle = i * (360f / unitsOnRing) * Mathf.Deg2Rad;
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                slots.Add(SampleNavMesh(candidate));
            }

            ring++;
        }

        return slots;
    }

    /// <summary>
    /// Ajuste une position candidate sur le NavMesh si possible.
    /// </summary>
    private Vector3 SampleNavMesh(Vector3 candidate)
    {
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;
        return candidate;
    }

    /// <summary>
    /// Tente d'envoyer un ordre de rive pour rapprocher un bateau et ses unités terrestres.
    /// </summary>
    private bool TryIssueBoatShoreOrder()
    {
        if (IsActivePlayerDefeated())
            return false;

        if (CurrentlySelectedObjects == null || CurrentlySelectedObjects.Count == 0 || Camera.main == null)
            return false;

        int activePlayerId = PlayerManager.Instance.GetActivePlayerId();
        UnitInstance targetBoat = GetFriendlyBoatUnderMouse(activePlayerId);
        if (targetBoat == null)
            return false;

        List<UnitInstance> selectedUnits = CollectSelectedPlayerUnits(activePlayerId);
        List<UnitInstance> landUnits = new List<UnitInstance>();

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            UnitInstance unit = selectedUnits[i];
            if (unit == null || unit == targetBoat || BoatTransport.IsBoatUnit(unit))
                continue;

            landUnits.Add(unit);
        }

        if (landUnits.Count == 0)
        {
            Debug.Log("[SelectionManager] Ordre bateau ignore: aucune unite terrestre selectionnee.");
            return false;
        }

        int availableCapacity = BoatTransport.GetAvailableCapacity(targetBoat);
        if (availableCapacity <= 0)
        {
            Debug.Log($"[SelectionManager] Ordre bateau refuse: {targetBoat.name} est plein.");
            return true;
        }

        List<UnitInstance> boardingUnits = new List<UnitInstance>();
        for (int i = 0; i < landUnits.Count && boardingUnits.Count < availableCapacity; i++)
            boardingUnits.Add(landUnits[i]);

        if (boardingUnits.Count < landUnits.Count)
        {
            Debug.Log($"[SelectionManager] Capacite limitee: {boardingUnits.Count}/{landUnits.Count} unite(s) vont embarquer dans {targetBoat.name}.");
        }

        if (!TryFindBestShoreRendezvous(boardingUnits, targetBoat, out Vector3 landDestination, out Vector3 waterDestination))
        {
            Debug.LogWarning("[SelectionManager] Impossible de trouver une rive valide pour rapprocher le bateau de la terre.");
            return true;
        }

        for (int i = 0; i < boardingUnits.Count; i++)
        {
            UnitInstance unit = boardingUnits[i];
            MovementManager.Instance.MoveBoatsUnitToPositionAsGroup(unit, landDestination, groupMoveStoppingDistance);
            BoatTransport.PrepareBoarding(unit, targetBoat);
        }

        MovementManager.Instance.MoveBoatsUnitToPositionAsGroup(targetBoat, waterDestination, groupMoveStoppingDistance);

        Debug.Log($"[SelectionManager] Ordre rive bateau: {boardingUnits.Count} unite(s) -> {landDestination}, bateau {targetBoat.name} -> {waterDestination}.");
        return true;
    }

    /// <summary>
    /// Recherche le bateau allié sous le curseur de la souris.
    /// </summary>
    private UnitInstance GetFriendlyBoatUnderMouse(int activePlayerId)
    {
        RaycastHit[] hits = GetMouseRaycastHits();
        UnitInstance bestBoat = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitInstance unit = GetUnitFromCollider(hits[i].collider);
            if (unit == null || unit.playerId != activePlayerId || !BoatTransport.IsBoatUnit(unit))
                continue;

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                bestBoat = unit;
            }
        }

        if (bestBoat != null)
            return bestBoat;

        return GetFriendlyBoatNearMouseOnScreen(activePlayerId);
    }

    /// <summary>
    /// Recherche le bateau allié le plus proche du pointeur à l'écran.
    /// </summary>
    private UnitInstance GetFriendlyBoatNearMouseOnScreen(int activePlayerId)
    {
        if (AllSelectableObjects == null || Camera.main == null || Mouse.current == null)
            return null;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        float bestDistanceSq = enemyUnitClickScreenRadius * enemyUnitClickScreenRadius;
        UnitInstance bestBoat = null;

        for (int i = AllSelectableObjects.Count - 1; i >= 0; i--)
        {
            SelectableObject selectable = AllSelectableObjects[i];
            if (selectable == null)
            {
                AllSelectableObjects.RemoveAt(i);
                continue;
            }

            UnitInstance unit = selectable.GetComponent<UnitInstance>();
            if (unit == null || unit.playerId != activePlayerId || !BoatTransport.IsBoatUnit(unit))
                continue;

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(unit.transform.position);
            if (screenPosition.z < 0f)
                continue;

            float distanceSq = ((Vector2)screenPosition - mousePosition).sqrMagnitude;
            if (distanceSq <= bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                bestBoat = unit;
            }
        }

        return bestBoat;
    }

    /// <summary>
    /// Récupère une unité à partir d'un collider touché par le raycast.
    /// </summary>
    private UnitInstance GetUnitFromCollider(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        UnitInstance unit = hitCollider.GetComponentInParent<UnitInstance>();
        if (unit == null)
            unit = hitCollider.GetComponent<UnitInstance>();
        if (unit == null)
            unit = hitCollider.GetComponentInChildren<UnitInstance>();

        return unit;
    }

    /// <summary>
    /// Recherche le meilleur point de rendez-vous entre la rive et le bateau quand on veut déposer les unités d'un bateau.
    /// </summary>
    private bool TryFindBestShoreRendezvous(List<UnitInstance> landUnits, UnitInstance targetBoat, out Vector3 landDestination, out Vector3 waterDestination)
    {
        landDestination = Vector3.zero;
        waterDestination = Vector3.zero;

        MapGenerator map = MapGenerator.Instance != null ? MapGenerator.Instance : FindFirstObjectByType<MapGenerator>();
        if (map == null || map.allTiles == null || targetBoat == null)
            return false;

        Vector3 landAnchor = GetUnitsCenter(landUnits);
        Vector3 boatPosition = targetBoat.transform.position;
        float bestScore = float.MaxValue;
        bool found = false;

        int width = map.allTiles.GetLength(0);
        int height = map.allTiles.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData waterTile = map.allTiles[x, y];
                if (!TileData.IsNavigableWater(waterTile))
                    continue;

                TryEvaluateShorePair(map, waterTile, x + 1, y, landAnchor, boatPosition, ref landDestination, ref waterDestination, ref bestScore, ref found);
                TryEvaluateShorePair(map, waterTile, x - 1, y, landAnchor, boatPosition, ref landDestination, ref waterDestination, ref bestScore, ref found);
                TryEvaluateShorePair(map, waterTile, x, y + 1, landAnchor, boatPosition, ref landDestination, ref waterDestination, ref bestScore, ref found);
                TryEvaluateShorePair(map, waterTile, x, y - 1, landAnchor, boatPosition, ref landDestination, ref waterDestination, ref bestScore, ref found);
            }
        }

        return found;
    }

    /// <summary>
    /// Évalue un couple eau/terre pour déterminer un meilleur point de débarquement.
    /// </summary>
    private void TryEvaluateShorePair(
        MapGenerator map,
        TileData waterTile,
        int landX,
        int landY,
        Vector3 landAnchor,
        Vector3 boatPosition,
        ref Vector3 bestLandDestination,
        ref Vector3 bestWaterDestination,
        ref float bestScore,
        ref bool found)
    {
        if (landX < 0 || landY < 0 || landX >= map.allTiles.GetLength(0) || landY >= map.allTiles.GetLength(1))
            return;

        TileData landTile = map.allTiles[landX, landY];
        if (!TileData.IsWalkableLand(landTile))
            return;

        Vector3 candidateLand = TileData.ToWorldPosition(map, landTile);
        Vector3 candidateWater = TileData.ToWorldPosition(map, waterTile);
        float score = TileData.FlatDistanceSq(landAnchor, candidateLand) + TileData.FlatDistanceSq(boatPosition, candidateWater);

        if (score >= bestScore)
            return;

        bestScore = score;
        bestLandDestination = candidateLand;
        bestWaterDestination = candidateWater;
        found = true;
    }

    /// <summary>
    /// Calcule le centre moyen d'une liste d'unités.
    /// </summary>
    private Vector3 GetUnitsCenter(List<UnitInstance> units)
    {
        if (units == null || units.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < units.Count; i++)
        {
            UnitInstance unit = units[i];
            if (unit == null)
                continue;

            sum += unit.transform.position;
            count++;
        }

        return count > 0 ? sum / count : Vector3.zero;
    }

    /// <summary>
    /// Rafraîchit périodiquement la liste des objets sélectionnables.
    /// </summary>
    private void AnalyzeSelectableObjectsContinuously()
    {
        selectableRefreshTimer += Time.deltaTime;
        if (selectableRefreshTimer < selectableRefreshInterval)
            return;

        selectableRefreshTimer = 0f;
        ForceRefreshSelectableObjects();
    }

    /// <summary>
    /// Reconstruit immédiatement la liste des objets sélectionnables.
    /// </summary>
    private void ForceRefreshSelectableObjects()
    {
        var found = Object.FindObjectsByType<SelectableObject>(FindObjectsSortMode.None);

        AllSelectableObjects.Clear();
        AllSelectableObjects.AddRange(found);
    }

    /// <summary>
    /// Enregistre un objet comme sélectionnable s'il ne l'est pas déjà.
    /// </summary>
    public void RegisterSelectable(SelectableObject selectable)
    {
        if (selectable == null)
            return;

        if (!AllSelectableObjects.Contains(selectable))
            AllSelectableObjects.Add(selectable);
    }


    /// <summary>
    /// Sélectionne les unités visibles dans le rectangle de sélection.
    /// </summary>
    private void SelectUnits()
    {
        if (IsActivePlayerDefeated())
            return;

        if (Camera.main == null)
            return;

        int activePlayerId = PlayerManager.Instance.GetActivePlayerId();

        for (int i = AllSelectableObjects.Count - 1; i >= 0; i--)
        {
            SelectableObject so = AllSelectableObjects[i];
            if (so == null)
            {
                AllSelectableObjects.RemoveAt(i);
                continue;
            }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(so.transform.position);
            float boxLeft = SelectionBox.anchoredPosition.x - (SelectionBox.sizeDelta.x / 2);
            float boxRight = SelectionBox.anchoredPosition.x + (SelectionBox.sizeDelta.x / 2);
            float boxTop = SelectionBox.anchoredPosition.y + (SelectionBox.sizeDelta.y / 2);
            float boxBottom = SelectionBox.anchoredPosition.y - (SelectionBox.sizeDelta.y / 2);

            UnitInstance unit = so.GetComponent<UnitInstance>();
            bool isProtectorUnit = unit != null && unit.unitData != null && unit.unitData.isProtector;

            if (isProtectorUnit)
            {
                if (CurrentlySelectedObjects.Contains(so))
                {
                    CurrentlySelectedObjects.Remove(so);
                    so.DeselectMe();
                }
                continue;
            }

            if (screenPos.x > boxLeft && screenPos.x < boxRight && screenPos.y > boxBottom && screenPos.y < boxTop)
            {
                if (unit != null && unit.playerId != activePlayerId)
                    continue;

                if (!CurrentlySelectedObjects.Contains(so))
                {
                    CurrentlySelectedObjects.Add(so);
                    so.SelectMe();
                }
            }
            else
            {
                if (CurrentlySelectedObjects.Contains(so))
                {
                    CurrentlySelectedObjects.Remove(so);
                    so.DeselectMe();
                }

            }
        }

        if (InterfaceInstance.Instance != null)
            InterfaceInstance.Instance.RefreshBuffIconsForSelection();
    }

    /// <summary>
    /// Tente d'envoyer un ordre d'attaque sur l'unité ou la structure sous la souris.
    /// </summary>
    private bool TryIssueAttackMoveOrder()
    {
        if (IsActivePlayerDefeated())
            return false;

        if (CurrentlySelectedObjects == null || CurrentlySelectedObjects.Count == 0 || Camera.main == null)
            return false;

        if (TryIssueUnitAttackOrder())
            return true;

        return TryIssueStructureAttackOrder();
    }

    /// <summary>
    /// Tente d'envoyer un ordre d'attaque contre une unité ennemie.
    /// </summary>
    private bool TryIssueUnitAttackOrder()
    {
        if (IsActivePlayerDefeated())
            return false;

        int activePlayerId = PlayerManager.Instance.GetActivePlayerId();
        UnitInstance targetUnit = GetEnemyUnitUnderMouse(activePlayerId);
        if (targetUnit == null)
            return false;

        List<UnitInstance> groupUnits = CollectSelectedPlayerUnits(activePlayerId);
        if (!SelectionHasCombatUnit(groupUnits))
            return groupUnits.Count > 0;

        for (int i = 0; i < groupUnits.Count; i++)
        {
            UnitInstance unit = groupUnits[i];
            if (unit == null || unit.unitData == null)
                continue;

            float stopDistance = GetAttackStopDistance(unit);
            MovementManager.Instance.MoveBoatsUnitToTarget(unit, targetUnit.transform, stopDistance);
            Debug.Log($"[SelectionManager] Ordre d'attaque unite: {unit.name} -> {targetUnit.name}");
        }

        return true;
    }

    /// <summary>
    /// Tente d'envoyer un ordre d'attaque contre une structure ennemie.
    /// </summary>
    private bool TryIssueStructureAttackOrder()
    {
        if (IsActivePlayerDefeated())
            return false;

        int activePlayerId = PlayerManager.Instance.GetActivePlayerId();
        StructureInstance targetStructure = GetEnemyStructureUnderMouse(activePlayerId);
        if (targetStructure == null)
            return false;
        List<UnitInstance> groupUnits = CollectSelectedPlayerUnits(activePlayerId);
        if (!SelectionHasCombatUnit(groupUnits))
            return groupUnits.Count > 0;

        for (int i = 0; i < groupUnits.Count; i++)
        {
            UnitInstance unit = groupUnits[i];
            if (unit == null || unit.unitData == null)
                continue;

            float stopDistance = GetAttackStopDistance(unit);
            MovementManager.Instance.MoveBoatsUnitToTarget(unit, targetStructure.transform, stopDistance);
        }

        return true;
    }

    /// <summary>
    /// Recherche l'unité ennemie la plus pertinente sous le curseur.
    /// </summary>
    private UnitInstance GetEnemyUnitUnderMouse(int activePlayerId)
    {
        RaycastHit[] hits = GetMouseRaycastHits();
        UnitInstance bestUnit = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            UnitInstance unit = hitCollider.GetComponentInParent<UnitInstance>();
            if (unit == null)
                unit = hitCollider.GetComponent<UnitInstance>();
            if (unit == null)
                unit = hitCollider.GetComponentInChildren<UnitInstance>();

            if (unit == null || unit.playerId == activePlayerId)
                continue;

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                bestUnit = unit;
            }
        }

        if (bestUnit != null)
            return bestUnit;

        return GetEnemyUnitNearMouseOnScreen(activePlayerId);
    }

    /// <summary>
    /// Recherche l'unité ennemie la plus proche du pointeur à l'écran.
    /// </summary>
    private UnitInstance GetEnemyUnitNearMouseOnScreen(int activePlayerId)
    {
        if (AllSelectableObjects == null || Camera.main == null || Mouse.current == null)
            return null;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        float bestDistanceSq = enemyUnitClickScreenRadius * enemyUnitClickScreenRadius;
        UnitInstance bestUnit = null;

        for (int i = AllSelectableObjects.Count - 1; i >= 0; i--)
        {
            SelectableObject selectable = AllSelectableObjects[i];
            if (selectable == null)
            {
                AllSelectableObjects.RemoveAt(i);
                continue;
            }

            UnitInstance unit = selectable.GetComponent<UnitInstance>();
            if (unit == null || unit.playerId == activePlayerId)
                continue;

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(unit.transform.position);
            if (screenPosition.z < 0f)
                continue;

            float distanceSq = ((Vector2)screenPosition - mousePosition).sqrMagnitude;
            if (distanceSq <= bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                bestUnit = unit;
            }
        }

        return bestUnit;
    }

    /// <summary>
    /// Recherche la structure ennemie la plus pertinente sous le curseur.
    /// </summary>
    private StructureInstance GetEnemyStructureUnderMouse(int activePlayerId)
    {
        RaycastHit[] hits = GetMouseRaycastHits();
        StructureInstance bestStructure = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            StructureInstance structure = hitCollider.GetComponentInParent<StructureInstance>();
            if (structure == null)
                structure = hitCollider.GetComponent<StructureInstance>();
            if (structure == null)
                structure = hitCollider.GetComponentInChildren<StructureInstance>();

            if (structure == null || structure.playerId == activePlayerId)
                continue;

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                bestStructure = structure;
            }
        }

        return bestStructure;
    }

    /// <summary>
    /// Lance un raycast depuis la souris et retourne tous les impacts.
    /// </summary>
    private RaycastHit[] GetMouseRaycastHits()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.RaycastAll(ray);
    }

    /// <summary>
    /// Vérifie si la sélection contient au moins une unité de combat. Pour vérifier si on peut lancer l'attaque
    /// </summary>
    private bool SelectionHasCombatUnit(List<UnitInstance> units)
    {
        if (units == null || units.Count == 0)
            return false;

        for (int i = 0; i < units.Count; i++)
        {
            UnitInstance unit = units[i];
            if (unit != null && unit.unitData is UnitCombatData)
                return true;
        }

        Debug.Log("Ordre refuse: uniquement des unites support/healer selectionnees.");
        return false;
    }

    /// <summary>
    /// Calcule la distance d'arrêt à utiliser pour une unité lors d'un ordre d'attaque.
    /// </summary>
    private float GetAttackStopDistance(UnitInstance unit)
    {
        if (unit != null && unit.unitData is UnitCombatData combatData)
        {
            return Mathf.Max(0f, combatData.attackRange);
        }

        return 0.1f;
    }

    /// <summary>
    /// Récupère les unités du joueur actif présentes dans la sélection actuelle.
    /// </summary>
    private List<UnitInstance> CollectSelectedPlayerUnits(int activePlayerId)
    {
        List<UnitInstance> units = new List<UnitInstance>();

        if (CurrentlySelectedObjects == null)
            return units;

        for (int i = 0; i < CurrentlySelectedObjects.Count; i++)
        {
            SelectableObject so = CurrentlySelectedObjects[i];
            if (so == null)
                continue;

            UnitInstance unit = so.GetComponent<UnitInstance>();
            if (unit == null || unit.unitData == null || unit.playerId != activePlayerId || unit.unitData.isProtector)
                continue;

            MovementManager movement = unit.GetComponent<MovementManager>();
            if (movement == null)
                continue;

            units.Add(unit);
        }
        return units;
    }
}
