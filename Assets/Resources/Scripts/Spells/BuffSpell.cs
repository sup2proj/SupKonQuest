using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffSpell : MonoBehaviour
{
    [Header("Scan")]
    [SerializeField, Min(0f)] private float scanRadius = 5f;

    [SerializeField] private LayerMask unitsLayerMask = ~0;

    [Header("Input")]
    [SerializeField] private KeyCode debugScanKey = KeyCode.P;

    [Header("Range circle (runtime)")]
    [SerializeField] private bool showCircleWhenPressingKey = true;
    [SerializeField, Min(3)] private int circleSegments = 64;
    [SerializeField, Min(0.001f)] private float circleWidth = 0.05f;

    [SerializeField] private Color circleColor = new Color(0.55f, 1f, 0.55f, 1f);
    [SerializeField, Range(0f, 1f), Tooltip("Alpha (transparence) du contour du cercle.")] private float circleOutlineAlpha = 0.45f;

    [SerializeField, Min(0f)] private float circleDurationSeconds = 1.0f;
    [SerializeField, Tooltip("Décalage vertical pour éviter le z-fighting avec le sol.")] private float circleHeightOffset = 0.05f;

    [Header("Range fill (runtime)")]
    [SerializeField] private bool showFilledDisc = true;
    [SerializeField, Range(0f, 1f), Tooltip("Remplissage. 0 = invisible, 1 = opaque.")] private float fillAlpha = 0.06f;

    [Header("Regen (from caster UnitHealerData)")]
    [SerializeField, Tooltip("Inclure l'unité qui lance le sort dans les soins.")] private bool includeSelf = true;
    [SerializeField, Min(0.01f), Tooltip("Intervalle (secondes) entre chaque tick de régénération.")] private float regenTickInterval = 0.25f;

    [Header("Auto-config")]
    [SerializeField, Tooltip("Si activé, scanRadius est automatiquement aligné sur healRange du UnitHealerData du lanceur.")] private bool autoUseCasterHealRange = true;

    [Header("Filtering")]
    [SerializeField, Tooltip("Si activé, on ne considère comme cibles que les UnitInstance dont le GameObject est taggé 'Units'.")] private bool requireUnitsTag = true;

    private LineRenderer rangeCircle;
    private MeshRenderer rangeFillRenderer;
    private MeshFilter rangeFillFilter;
    private float circleHideAtTime = -1f;

    private UnitInstance casterUnit;

    // Empêche de lancer plusieurs régénérations en parallèle sur la même unité.
    private readonly Dictionary<UnitInstance, Coroutine> regenByTarget = new Dictionary<UnitInstance, Coroutine>();

    private void Awake()
    {
        // BuffSpell peut être posé sur le même GO que UnitInstance (recommandé),
        // mais on sécurise le cas où il est sur un child: on remonte jusqu'au root.
        casterUnit = GetComponent<UnitInstance>();
        if (casterUnit == null)
            casterUnit = GetComponentInParent<UnitInstance>();
        if (casterUnit == null)
            casterUnit = GetComponentInChildren<UnitInstance>();

        if (casterUnit == null)
        {
            Debug.LogError("[BuffSpell] Aucun UnitInstance trouvé sur l'objet (ni parent/enfant). Place BuffSpell sur le prefab de l'unité (même GameObject que UnitInstance) ou sur un de ses enfants.", this);
            return;
        }

        if (autoUseCasterHealRange && casterUnit.unitData is UnitHealerData healerData && healerData.healRange > 0f)
        {
            scanRadius = healerData.healRange;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(debugScanKey))
        {
            // Synchronise éventuellement la portée avec le data du lanceur AVANT d'afficher
            if (autoUseCasterHealRange && casterUnit != null && casterUnit.unitData is UnitHealerData hd && hd.healRange > 0f)
                scanRadius = hd.healRange;

            if (showCircleWhenPressingKey)
                ShowRangeCircle();
            ScanAndPrintFriendlyUnits();
        }

        if (circleHideAtTime >= 0f && Time.time >= circleHideAtTime)
        {
            if (rangeCircle != null)
                rangeCircle.enabled = false;
            if (rangeFillRenderer != null)
                rangeFillRenderer.enabled = false;

            circleHideAtTime = -1f;
        }

        // Fait suivre le cercle/disque à l'unité tant qu'il est visible
        if (rangeCircle != null && rangeCircle.enabled)
            UpdateRangeCirclePositions();
        if (rangeFillRenderer != null && rangeFillRenderer.enabled)
            UpdateRangeFillTransform();
    }

    private void EnsureRangeCircle()
    {
        if (rangeCircle != null)
            return;

        // On crée un enfant pour que la position suive l'unité
        GameObject go = new GameObject("SupportSpell_RangeCircle");
        go.transform.SetParent(transform, worldPositionStays: false);
        go.transform.localPosition = Vector3.up * circleHeightOffset;
        go.transform.localRotation = Quaternion.identity;

        rangeCircle = go.AddComponent<LineRenderer>();
        rangeCircle.useWorldSpace = true;
        rangeCircle.loop = true;
        rangeCircle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rangeCircle.receiveShadows = false;

        // Matériau simple pour éviter les soucis de shader
        rangeCircle.material = new Material(Shader.Find("Sprites/Default"));

        rangeCircle.startWidth = circleWidth;
        rangeCircle.endWidth = circleWidth;

        Color outline = circleColor;
        outline.a = Mathf.Clamp01(circleOutlineAlpha);
        rangeCircle.startColor = outline;
        rangeCircle.endColor = outline;

        rangeCircle.positionCount = Mathf.Max(3, circleSegments);
        rangeCircle.enabled = false;

        UpdateRangeCirclePositions();
    }

    private void EnsureRangeFill()
    {
        if (rangeFillRenderer != null && rangeFillFilter != null)
            return;
        GameObject go = new GameObject("SupportSpell_RangeFill");
        go.transform.SetParent(transform, worldPositionStays: false);
        go.transform.localPosition = Vector3.up * circleHeightOffset;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        rangeFillFilter = go.AddComponent<MeshFilter>();
        rangeFillRenderer = go.AddComponent<MeshRenderer>();

        // Matériau simple. On règle la couleur avec alpha (semi transparent)
        var mat = new Material(Shader.Find("Sprites/Default"));
        Color c = circleColor;
        c.a = Mathf.Clamp01(fillAlpha);
        mat.color = c;
        rangeFillRenderer.material = mat;

        rangeFillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rangeFillRenderer.receiveShadows = false;
        rangeFillRenderer.enabled = false;

        rangeFillFilter.sharedMesh = BuildDiscMesh(Mathf.Max(3, circleSegments));

        UpdateRangeFillTransform();
    }

    private static Mesh BuildDiscMesh(int segments)
    {
        var mesh = new Mesh();
        mesh.name = "SupportSpell_Disc";

        int vertCount = segments + 1;
        var vertices = new Vector3[vertCount];
        var uv = new Vector2[vertCount];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        float step = (Mathf.PI * 2f) / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = step * i;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            vertices[i + 1] = new Vector3(x, y, 0f);
            uv[i + 1] = new Vector2((x + 1f) * 0.5f, (y + 1f) * 0.5f);
        }

        var triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int ti = i * 3;
            triangles[ti] = 0;
            triangles[ti + 1] = i + 1;
            triangles[ti + 2] = next + 1;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void UpdateRangeFillTransform()
    {
        if (rangeFillRenderer == null)
            return;

        rangeFillRenderer.transform.position = transform.position + Vector3.up * circleHeightOffset;
        rangeFillRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        rangeFillRenderer.transform.localScale = new Vector3(scanRadius, scanRadius, 1f);

        if (rangeFillRenderer.material != null)
        {
            Color c = circleColor;
            c.a = Mathf.Clamp01(fillAlpha);
            rangeFillRenderer.material.color = c;
        }
    }

    private void ShowRangeCircle()
    {
        EnsureRangeCircle();

        Color outline = circleColor;
        outline.a = Mathf.Clamp01(circleOutlineAlpha);

        rangeCircle.startColor = outline;
        rangeCircle.endColor = outline;
        rangeCircle.startWidth = circleWidth;
        rangeCircle.endWidth = circleWidth;
        rangeCircle.positionCount = Mathf.Max(3, circleSegments);

        rangeCircle.enabled = true;

        float duration = GetRangeDisplayDurationSeconds();
        circleHideAtTime = (duration > 0f) ? (Time.time + duration) : -1f;

        UpdateRangeCirclePositions();

        if (showFilledDisc)
        {
            EnsureRangeFill();
            rangeFillRenderer.enabled = true;
            UpdateRangeFillTransform();
        }
    }

    private void UpdateRangeCirclePositions()
    {
        if (rangeCircle == null)
            return;

        int segments = Mathf.Max(3, circleSegments);
        if (rangeCircle.positionCount != segments)
            rangeCircle.positionCount = segments;

        Vector3 center = transform.position + Vector3.up * circleHeightOffset;
        float step = (Mathf.PI * 2f) / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = step * i;
            float x = Mathf.Cos(angle) * scanRadius;
            float z = Mathf.Sin(angle) * scanRadius;
            rangeCircle.SetPosition(i, center + new Vector3(x, 0f, z));
        }
    }

    private void ScanAndPrintFriendlyUnits()
    {
        // On resynchronise au moment du cast, au cas où le UnitData a changé côté runtime.
        if (autoUseCasterHealRange && casterUnit != null && casterUnit.unitData is UnitHealerData hd && hd.healRange > 0f)
            scanRadius = hd.healRange;

        int currentPlayerId = PlayerManager.Instance != null ? PlayerManager.Instance.ActivePlayerId : -1;
        int totalUnitsFound = 0;
        int friendlyUnitsFound = 0;

        if (currentPlayerId < 0)
        {
            Debug.LogWarning("[BuffSpell] PlayerManager.Instance introuvable, impossible de déterminer le joueur actuel.", this);
            return;
        }

        // Le caster doit avoir un UnitHealerData pour définir duration/amount
        if (casterUnit == null || casterUnit.unitData == null)
        {
            Debug.LogWarning("[BuffSpell] UnitInstance/unitData manquant sur le lanceur.", this);
            return;
        }

        if (casterUnit.unitData is not UnitHealerData healerData)
        {
            Debug.LogWarning("[BuffSpell] Le lanceur doit avoir un UnitHealerData pour appliquer une régénération (healAmount/healDuration).", this);
            return;
        }

        if (healerData.healDuration <= 0f || healerData.healAmount <= 0f)
        {
            Debug.LogWarning($"[BuffSpell] Paramètres de heal invalides sur le lanceur: healAmount={healerData.healAmount}, healDuration={healerData.healDuration}", this);
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, unitsLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            // Le collider peut être sur un enfant: on cherche directement l'unit sur le parent
            UnitInstance unit = hits[i].GetComponentInParent<UnitInstance>();
            if (unit == null)
                continue;

            if (requireUnitsTag && !unit.CompareTag("Units"))
                continue;

            totalUnitsFound++;

            if (unit.playerId != currentPlayerId)
                continue;

            if (!includeSelf && casterUnit != null && unit == casterUnit)
                continue;

            friendlyUnitsFound++;
            Debug.Log($"[BuffSpell] Unité alliée trouvée: {unit.name} (playerId={unit.playerId})", unit);

            ApplyRegen(unit, healerData);
        }

        Debug.Log($"[BuffSpell] Scan radius={scanRadius} => unités détectées={totalUnitsFound}, alliées (playerId={currentPlayerId})={friendlyUnitsFound}", this);
    }

    private void ApplyRegen(UnitInstance target, UnitHealerData healerData)
    {
        if (target == null)
            return;

        if (regenByTarget.TryGetValue(target, out Coroutine running) && running != null)
        {
            StopCoroutine(running);
            regenByTarget.Remove(target);
        }

        Coroutine c = StartCoroutine(RegenCoroutine(target, healerData.healAmount, healerData.healDuration));
        regenByTarget[target] = c;
    }

    private IEnumerator RegenCoroutine(UnitInstance target, float totalHealAmount, float duration)
    {
        float tick = Mathf.Max(0.01f, regenTickInterval);
        float elapsed = 0f;

        // On répartit healAmount sur la durée
        float healPerSecond = totalHealAmount / Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            if (target == null)
                yield break;

            float dt = Mathf.Min(tick, duration - elapsed);
            float healThisTick = healPerSecond * dt;

            // UnitInstance.Heal() est réservé aux unités healer dans ton code.
            // Pour soigner n'importe quelle unité, on utilise SetHealth().
            target.SetHealth(healThisTick);

            elapsed += dt;
            yield return new WaitForSeconds(dt);
        }

        regenByTarget.Remove(target);
    }

    private float GetRangeDisplayDurationSeconds()
    {
        // Aligne la durée d'affichage sur la durée du sort de régénération du lanceur.
        if (casterUnit != null && casterUnit.unitData is UnitHealerData healerData && healerData.healDuration > 0f)
            return healerData.healDuration;

        // Fallback (valeur inspector)
        return circleDurationSeconds;
    }
}
