using Enums.Environment;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class MovementManager : NetworkBehaviour
{
    public static MovementManager Instance;

    private const float NavMeshSampleDistance = 2.0f;
    private const float AgentStoppedSqrVelocity = 0.01f;
    private const float AgentStuckVelocityThreshold = 0.05f;
    private const float AgentStuckTimeout = 1.5f;
    private const float AnimationMoveVelocityThreshold = 0.1f;
    private const float TransformMoveSqrThreshold = 0.0001f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float stoppingDistance = 0.3f;
    public float rotationSpeed = 12f;

    private Vector3 movement;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;
    private Transform followTarget;
    private bool followTargetPosition = true;
    private float stuckTimer = 0f;

    private UnitInstance unitInstance;
    private NavMeshAgent agent;
    private Animator animator;
    private MapGenerator mapGenerator;

    private static int priorityCounter = 0;

    /// <summary>
    /// Récupère les composants nécessaires et initialise la priorité d'évitement de l'agent.
    /// </summary>
    private void Awake()
    {
        Instance = this;
        unitInstance = GetComponent<UnitInstance>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent != null)
        {
            RefreshAgentSettings();
            agent.avoidancePriority = Mathf.Clamp(priorityCounter % 99, 1, 99);
            priorityCounter++;
        }
    }

    /// <summary>
    /// Positionne l'agent sur le NavMesh au démarrage si possible.
    /// </summary>
    private void Start()
    {
        if (NetworkManager.Singleton != null && IsClient && !IsServer)
        {
            if (agent != null) 
                agent.enabled = false;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) 
                rb.isKinematic = true;
                
            return;
        }

        if (agent == null)
            return;

        RefreshAgentSettings();
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSampleDistance, agent.areaMask))
            agent.Warp(hit.position);
    }

    /// <summary>
    /// Gère les entrées de déplacement et met à jour le comportement de mouvement.
    /// Bloque la physique chez le client pour laisser l'autorité au Serveur.
    /// </summary>
    private void Update()
    {
        if (SelectionManager.Instance == null)
            HandleMouseClick();

        // Seul le Serveur (l'Hôte) a le droit de faire bouger l'objet ou de calculer le chemin NavMesh.
        // Les Clients se contentent de regarder l'objet bouger grâce au NetworkTransform !
        if (NetworkManager.Singleton != null && IsClient && !IsServer)
        {
            UpdateMovementAnimation(); // Le client gère juste ses animations visuelles
            return;
        }

        HandleMovement();
    }

    /// <summary>
    /// Déplace l'unité vers la position cliquée avec le bouton droit si le joueur en est le propriétaire.
    /// </summary>
    private void HandleMouseClick()
    {
        // Seul le propriétaire de l'unité peut lui donner des ordres
        if (unitInstance == null || unitInstance.playerId != PlayerManager.Instance.GetActivePlayerId()) 
            return;

        if (Mouse.current == null || Camera.main == null)
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Vector3 destination = hit.point;
        destination.y = transform.position.y;
        
        MoveToPosition(destination, stoppingDistance);
    }

    /// <summary>
    /// (RÉSEAU) Envoie une requête radio au serveur pour déplacer cette unité vers des coordonnées précises.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestMoveToPositionServerRpc(Vector3 destination, float stopDistance)
    {
        // Le Serveur reçoit l'ordre et le fait exécuter !
        MoveToPosition(destination, stopDistance);
    }

    /// <summary>
    /// Lance un déplacement vers une position du monde autorisée. 
    /// Relaie automatiquement l'ordre au serveur si appelé par un Client.
    /// </summary>
    public void MoveToPosition(Vector3 destination, float stopDistance)
    {
        if (NetworkManager.Singleton != null && IsClient && !IsServer)
        {
            // Je suis un Client : j'envoie l'ordre par radio au Serveur
            RequestMoveToPositionServerRpc(destination, stopDistance);
            return;
        }

        // La suite s'exécute uniquement sur le Serveur (ou en mode Solo)
        if (!CanMoveOnWorldPosition(destination)) 
            return;

        SetPositionDestination(destination, stopDistance, clearPendingBoarding: true);
    }

    /// <summary>
    /// Lance un déplacement de groupe vers une position donnée.
    /// </summary>
    public void MoveToPositionAsGroup(Vector3 destination, float stopDistance)
    {
        StopAttackForMovement();
        MoveToPosition(destination, stopDistance);
    }

    /// <summary>
    /// (RÉSEAU) Envoie une requête radio au serveur pour que cette unité suive/attaque une cible réseau.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestMoveToTargetServerRpc(ulong networkObjectId, float stopDistance)
    {
        // Le serveur retrouve l'objet réseau grâce à son ID unique et lui dit d'attaquer
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject targetNetObj))
        {
            MoveToTarget(targetNetObj.transform, stopDistance);
        }
    }

    /// <summary>
    /// Lance un déplacement vers une cible en annulant l'attaque en cours.
    /// Relaie l'ordre au serveur si la cible est un objet réseau.
    /// </summary>
    public void MoveToTarget(Transform target, float stopDistance)
    {
        if (NetworkManager.Singleton != null && IsClient && !IsServer)
        {
            // Si la cible est un objet réseau (unité, bâtiment), on envoie son ID au serveur
            NetworkObject targetNetObj = target.GetComponent<NetworkObject>();
            if (targetNetObj != null)
            {
                RequestMoveToTargetServerRpc(targetNetObj.NetworkObjectId, stopDistance);
            }
            return;
        }

        StopAttackForMovement();
        MoveToTargetInternal(target, Mathf.Max(0f, stopDistance));
    }

    /// <summary>
    /// Orchestre la mise à jour du déplacement selon le mode disponible (NavMesh ou Transform).
    /// </summary>
    private void HandleMovement()
    {
        RefreshAgentSettings();
        if (CanUseNavMeshAgent()) 
            HandleNavMeshMovement();
        else 
            HandleTransformMovement();
            
        UpdateMovementAnimation();
    }

    /// <summary>
    /// Met à jour le déplacement en utilisant le NavMeshAgent.
    /// </summary>
    private void HandleNavMeshMovement()
    {
        if (!isMovingToTarget) 
            return;

        if (followTarget != null && followTargetPosition)
            agent.SetDestination(followTarget.position);

        if (HasReachedAgentDestination())
        {
            StopNavMeshMovement(notifyComplete: true, clearVelocity: false);
            return;
        }

        if (IsAgentStuckTimedOut())
            StopNavMeshMovement(notifyComplete: false, clearVelocity: true);
    }

    /// <summary>
    /// Vérifie si l'agent NavMesh a atteint sa destination.
    /// </summary>
    private bool HasReachedAgentDestination()
    {
        float dynamicStoppingDistance = Mathf.Max(agent.stoppingDistance, agent.speed * 0.1f);
        return !agent.pathPending && agent.remainingDistance <= dynamicStoppingDistance && agent.velocity.sqrMagnitude < AgentStoppedSqrVelocity;
    }

    /// <summary>
    /// Détecte si l'agent est bloqué trop longtemps sans avancer.
    /// </summary>
    private bool IsAgentStuckTimedOut()
    {
        if (agent.velocity.magnitude >= AgentStuckVelocityThreshold) 
        { 
            stuckTimer = 0f; 
            return false; 
        }
        stuckTimer += Time.deltaTime;
        return stuckTimer > AgentStuckTimeout;
    }

    /// <summary>
    /// Arrête le déplacement NavMesh et notifie éventuellement la fin.
    /// </summary>
    private void StopNavMeshMovement(bool notifyComplete, bool clearVelocity)
    {
        isMovingToTarget = false;
        stuckTimer = 0f;
        if (clearVelocity) 
            agent.velocity = Vector3.zero;
        agent.ResetPath();
        if (notifyComplete) 
            NotifyMovementComplete();
    }

    /// <summary>
    /// Met à jour le déplacement manuel basé sur le transform (hors NavMesh).
    /// </summary>
    private void HandleTransformMovement()
    {
        if (!isMovingToTarget) 
        { 
            movement = Vector3.zero; 
            return; 
        }
        if (followTarget != null && followTargetPosition) 
            targetPosition = followTarget.position;

        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= stoppingDistance)
        {
            isMovingToTarget = false;
            movement = Vector3.zero;
            NotifyMovementComplete();
            return;
        }

        movement = toTarget.normalized;
        transform.position += movement * GetUnitSpeed() * Time.deltaTime;
        Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Synchronise le booléen d'animation de déplacement avec l'état courant.
    /// </summary>
    private void UpdateMovementAnimation()
    {
        if (animator == null) 
            return;
        animator.SetBool("isMoving", IsActuallyMoving());
    }

    /// <summary>
    /// Détermine si l'unité est réellement en train de se déplacer physiquement.
    /// </summary>
    private bool IsActuallyMoving()
    {
        // Petite sécurité supplémentaire pour le client qui lit les infos du réseau
        if (NetworkManager.Singleton != null && IsClient && !IsServer)
        {
            // Chez le client, on regarde si le NetworkTransform bouge vraiment physiquement
            return GetComponent<Rigidbody>() != null && GetComponent<Rigidbody>().linearVelocity.sqrMagnitude > AnimationMoveVelocityThreshold 
                   || movement.sqrMagnitude > TransformMoveSqrThreshold;
        }

        if (CanUseNavMeshAgent()) 
            return isMovingToTarget && agent.velocity.magnitude > AnimationMoveVelocityThreshold;
            
        return isMovingToTarget && movement.sqrMagnitude > TransformMoveSqrThreshold;
    }

    /// <summary>
    /// Retourne la vitesse de déplacement effective de l'unité.
    /// </summary>
    private float GetUnitSpeed()
    {
        if (unitInstance != null && unitInstance.unitData != null && unitInstance.unitData.speed > 0f) 
            return unitInstance.unitData.speed;
            
        return moveSpeed;
    }

    /// <summary>
    /// Met à jour les paramètres de vitesse et de zone du NavMeshAgent.
    /// </summary>
    private void RefreshAgentSettings()
    {
        if (agent == null) 
            return;
        agent.speed = GetUnitSpeed();
        agent.areaMask = GetAllowedNavMeshAreaMask();
    }

    /// <summary>
    /// Prépare et exécute le déplacement interne vers une cible.
    /// </summary>
    private void MoveToTargetInternal(Transform target, float stopDistance)
    {
        BoatTransport.ClearPendingBoarding(unitInstance);
        followTarget = target;
        followTargetPosition = false;

        Vector3 destination = target != null ? target.position : transform.position;
        if (target != null)
        {
            StructureInstance structure = target.GetComponent<StructureInstance>() ?? target.GetComponentInParent<StructureInstance>() ?? target.GetComponentInChildren<StructureInstance>();
            if (structure != null) 
            { 
                followTargetPosition = false; 
                if (!TryGetApproachDestinationForTarget(target, stopDistance, out destination)) 
                    return; 
            }
            else if (CanMoveOnWorldPosition(target.position)) 
            { 
                followTargetPosition = true; 
                destination = target.position; 
            }
            else if (!TryGetApproachDestinationForTarget(target, stopDistance, out destination)) 
            {
                return;
            }
        }

        if (target != null) 
            targetPosition = destination;
            
        StartMovement(GetUnitAttackRange(stopDistance));
        
        if (target != null) 
            ApplyAgentDestination(destination);
    }

    /// <summary>
    /// Cherche un point d'approche valide autour d'une cible pour éviter les zones interdites.
    /// </summary>
    private bool TryGetApproachDestinationForTarget(Transform target, float stopDistance, out Vector3 destination)
    {
        destination = target != null ? target.position : transform.position;
        if (target == null) 
            return false;

        StructureInstance structure = target.GetComponent<StructureInstance>() ?? target.GetComponentInParent<StructureInstance>() ?? target.GetComponentInChildren<StructureInstance>();
        if (structure == null) 
            return false;

        int areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas;
        Vector3 center = structure.StructurePosition;
        float baseRadius = Mathf.Max(0.75f, stopDistance);
        float maxRadius = baseRadius + 8f;
        const int directionCount = 8;

        for (float radius = baseRadius; radius <= maxRadius; radius += 1f)
        {
            for (int i = 0; i < directionCount; i++)
            {
                float angle = (Mathf.PI * 2f * i) / directionCount;
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, areaMask))
                {
                    destination = hit.position;
                    return true;
                }
            }
        }
        
        if (NavMesh.SamplePosition(center, out NavMeshHit centerHit, NavMeshSampleDistance * 2f, areaMask)) 
        { 
            destination = centerHit.position; 
            return true; 
        }
        
        return false;
    }

    /// <summary>
    /// Définit une destination de déplacement directe sur le monde.
    /// </summary>
    private void SetPositionDestination(Vector3 destination, float stopDistance, bool clearPendingBoarding)
    {
        if (clearPendingBoarding) 
            BoatTransport.ClearPendingBoarding(unitInstance);
            
        followTarget = null;
        followTargetPosition = true;
        targetPosition = destination;
        StartMovement(stopDistance);
        ApplyAgentDestination(destination);
    }

    /// <summary>
    /// Démarre l'état de mouvement avec une distance d'arrêt donnée.
    /// </summary>
    private void StartMovement(float stopDistance)
    {
        stoppingDistance = Mathf.Max(0f, stopDistance);
        isMovingToTarget = true;
    }

    /// <summary>
    /// Applique la destination au NavMeshAgent si celui-ci peut être utilisé.
    /// </summary>
    private void ApplyAgentDestination(Vector3 destination)
    {
        if (!CanUseNavMeshAgent()) 
            return;
            
        RefreshAgentSettings();
        agent.stoppingDistance = stoppingDistance;
        agent.ResetPath();
        agent.SetDestination(destination);
    }

    /// <summary>
    /// Demande à l'animation d'interrompre l'attaque lors d'un déplacement.
    /// </summary>
    private void StopAttackForMovement()
    {
        UnitsAnimation animatedMover = GetComponent<UnitsAnimation>();
        if (animatedMover != null) 
            animatedMover.StopAttackForMovement();
    }

    /// <summary>
    /// Arrête immédiatement tout déplacement en cours (Autorité Serveur).
    /// </summary>
    public void StopMovement()
    {
        // Seul le Serveur arrête le mouvement physique
        if (NetworkManager.Singleton != null && IsClient && !IsServer) 
            return;

        BoatTransport.ClearPendingBoarding(unitInstance);
        isMovingToTarget = false;
        movement = Vector3.zero;
        followTarget = null;
        followTargetPosition = true;

        if (animator != null) 
            animator.SetBool("isMoving", false);
            
        if (CanUseNavMeshAgent()) 
            agent.ResetPath();
    }

    /// <summary>
    /// Indique si l'unité est en train de se déplacer.
    /// </summary>
    public bool IsMoving() 
    { 
        return isMovingToTarget; 
    }

    /// <summary>
    /// Retourne la portée d'attaque de l'unité ou une valeur de repli.
    /// </summary>
    private float GetUnitAttackRange(float fallback)
    {
        if (unitInstance != null && unitInstance.unitData is UnitCombatData combatData) 
            return Mathf.Max(0f, combatData.attackRange);
            
        return fallback;
    }

    /// <summary>
    /// Vérifie si une position monde est autorisée selon le type de terrain.
    /// </summary>
    public bool CanMoveOnWorldPosition(Vector3 worldPosition)
    {
        MapGenerator map = GetMapGenerator();
        if (map == null) 
            return true;
            
        if (!map.TryGetTileAtWorldPosition(worldPosition, out TileData tile)) 
            return false;
            
        return IsGroundAllowed(tile.groundType);
    }

    /// <summary>
    /// Indique si le terrain est compatible avec l'unité courante.
    /// </summary>
    private bool IsGroundAllowed(GroundType groundType) 
    { 
        return IsBoatUnit() ? groundType == GroundType.Water : groundType != GroundType.Water; 
    }

    /// <summary>
    /// Récupère le générateur de carte courant ou le recherche dans la scène.
    /// </summary>
    private MapGenerator GetMapGenerator()
    {
        if (mapGenerator == null) 
            mapGenerator = MapGenerator.Instance != null ? MapGenerator.Instance : FindFirstObjectByType<MapGenerator>();
            
        return mapGenerator;
    }

    /// <summary>
    /// Calcule le masque de zones NavMesh autorisées pour l'unité.
    /// </summary>
    private int GetAllowedNavMeshAreaMask()
    {
        if (IsBoatUnit())
        {
            int waterArea = NavMesh.GetAreaFromName("Water");
            return waterArea >= 0 ? 1 << waterArea : NavMesh.AllAreas;
        }
        int walkableArea = NavMesh.GetAreaFromName("Walkable");
        return walkableArea >= 0 ? 1 << walkableArea : NavMesh.AllAreas;
    }

    /// <summary>
    /// Indique si l'unité courante est un bateau.
    /// </summary>
    private bool IsBoatUnit()
    {
        if (unitInstance == null || unitInstance.unitData == null) 
            return false;
            
        if (unitInstance.unitData is UnitBoat) 
            return true;
            
        switch (unitInstance.unitData.type)
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
    /// Vérifie si l'agent NavMesh peut être utilisé pour le déplacement.
    /// </summary>
    private bool CanUseNavMeshAgent() 
    { 
        return agent != null && agent.enabled && agent.isOnNavMesh; 
    }

    /// <summary>
    /// Retourne la direction actuelle de déplacement.
    /// </summary>
    public Vector3 GetMovementDirection() 
    { 
        return movement; 
    }

    public System.Action<Transform> OnMovementComplete;
    
    /// <summary>
    /// Notifie les abonnés que le déplacement est terminé.
    /// </summary>
    private void NotifyMovementComplete() 
    { 
        if (OnMovementComplete != null) OnMovementComplete(followTarget); 
    }

    /// <summary>
    /// Déplace une unité bateau vers une position en mode groupe.
    /// </summary>
    public void MoveBoatsUnitToPositionAsGroup(UnitInstance unit, Vector3 destination, float stopDistance)
    {
        if (!TryGetMovementForUnit(unit, destination, out MovementManager move)) 
            return;
            
        move.MoveToPositionAsGroup(destination, stopDistance);
    }

    /// <summary>
    /// Déplace une unité bateau vers une cible.
    /// </summary>
    public void MoveBoatsUnitToTarget(UnitInstance unit, Transform target, float stopDistance)
    {
        if (unit == null || target == null) 
            return;
            
        MovementManager move = unit.GetComponent<MovementManager>();
        if (move == null) 
            return;
            
        move.MoveToTarget(target, stopDistance);
    }

    /// <summary>
    /// Récupère le MovementManager d'une unité et vérifie si la destination est valide.
    /// </summary>
    private bool TryGetMovementForUnit(UnitInstance unit, Vector3 destination, out MovementManager move)
    {
        move = null;
        if (unit == null) 
            return false;
            
        move = unit.GetComponent<MovementManager>();
        return move != null && move.CanMoveOnWorldPosition(destination);
    }

    /// <summary>
    /// Force un déplacement direct sans nettoyage des réservations en attente.
    /// </summary>
    public void ForceMoveToPosition(Vector3 destination, float stopDistance) 
    { 
        SetPositionDestination(destination, stopDistance, clearPendingBoarding: false); 
    }
}