using System.Collections.Generic;
using UnityEngine;

public class Spells : MonoBehaviour
{
    public static Spells Instance { get; private set; }

    [SerializeField] private float regenTickInterval = 0.25f;
    
    private int circleSegments = 128; 
    private float circleWidth = 0.05f;
    private float scanRadius;
    
    private float fillAlpha = 0.06f;
    private float circleOutlineAlpha = 0.45f;
    private float circleHeightOffset = 0.05f;
    private LineRenderer rangeCircle;
    private MeshRenderer rangeFillRenderer;
    private MeshFilter rangeFillFilter;
    private float circleHideAtTime = -1f;
    private Color currentCircleColor = Color.red;
    public float spellLockedUntil = -1f;
    private float healerSpellLockedUntil = -1f;
    private float supportSpellLockedUntil = -1f;

    private UnitInstance casterUnit;

    private readonly Dictionary<UnitInstance, Coroutine> regenByTarget = new Dictionary<UnitInstance, Coroutine>();
    private readonly Dictionary<UnitInstance, Coroutine> AttackSpeedBuffByTarget = new Dictionary<UnitInstance, Coroutine>();

    /// <summary>
    /// Initialise le lanceur de sorts et détermine sa portée initiale.
    /// </summary>
    private void Awake()
    {
        Instance = this;
        casterUnit = GetComponent<UnitInstance>();
        if (casterUnit.unitData is UnitHealerData healerData && healerData.healRange > 0f)
        {
            scanRadius = healerData.healRange;
        }
    }
    
    /// <summary>
    /// Indique si le sort actif est encore en temps de recharge.
    /// </summary>
    public bool IsSpellOnCooldown
    {
        get
        {
            if (casterUnit.unitData is UnitHealerData)
                return healerSpellLockedUntil > Time.time;
            
            if (casterUnit.unitData is UnitSupportData)
                return supportSpellLockedUntil > Time.time;

            return false;
        }
    }


    /// <summary>
    /// Met à jour l'affichage de la portée du sort et sa visibilité.
    /// </summary>
    private void Update()
    {
        if (circleHideAtTime >= 0f && Time.time >= circleHideAtTime)
        {
            if (rangeCircle != null)
                rangeCircle.enabled = false;
            if (rangeFillRenderer != null)
                rangeFillRenderer.enabled = false;

            circleHideAtTime = -1f;
        }

        if (rangeCircle != null && rangeCircle.enabled)
            UpdateRangeCirclePositions();
        if (rangeFillRenderer != null && rangeFillRenderer.enabled)
            UpdateRangeFillTransform(currentCircleColor);
    }

    /// <summary>
    /// Réagit à l'action utilisateur pour déclencher le sort correspondant.
    /// </summary>
    public void ButtonListener(int choice)
    {
        if (casterUnit == null || casterUnit.unitData == null)
            return;
        UnitsAnimation anim = casterUnit.GetComponent<UnitsAnimation>();
        float iconHideCooldown = 0f;
        bool shouldHideIcon = false;

        if (choice == 3 && casterUnit.unitData is UnitHealerData)
        {
            TriggerSpell(Color.red, 1);
            anim.StartAttackAnimationFromSpell();
            iconHideCooldown = GetHealerCooldwon();
            if (iconHideCooldown > 0f)
            {
                healerSpellLockedUntil = Time.time + iconHideCooldown;
                spellLockedUntil = healerSpellLockedUntil;
            }

            shouldHideIcon = true;
        }
        else if (casterUnit.unitData is UnitSupportData)
        {
            Color spellColor;
            int spellId;

            switch (choice)
            {
                case 0:
                    spellColor = Color.green;
                    spellId = 2;
                    break;
                case 1:
                    spellColor = Color.yellow;
                    spellId = 3;
                    break;
                case 2:
                    spellColor = Color.blue;
                    spellId = 4;
                    break;
                default:
                    spellColor = default;
                    spellId = 0;
                    break;
            }

            if (spellId != 0)
            {
                TriggerSpell(spellColor, spellId);
                anim.StartAttackAnimationFromSpell();
                iconHideCooldown = GetSupportCooldwon();
                if (iconHideCooldown > 0f)
                {
                    supportSpellLockedUntil = Time.time + iconHideCooldown;
                    spellLockedUntil = supportSpellLockedUntil;
                }

                shouldHideIcon = true;
            }
        }

        if (shouldHideIcon && InterfaceInstance.Instance != null)
        {
            InterfaceInstance.Instance.HideBuffIconForCooldown(choice, iconHideCooldown);
        }
    }

    /// <summary>
    /// Déclenche l'affichage de la zone du sort et le scan des unités alliées.
    /// </summary>
    private void TriggerSpell(Color spellColor, int spell)
    {
        scanRadius = GetScanRadiusFromUnitsData();
        currentCircleColor = spellColor;
        ShowRangeCircle(spellColor);
        ScanFriendlyUnits(spell);
    }

    /// <summary>
    /// Crée le contour circulaire de portée si nécessaire.
    /// </summary>
    private void EnsureRangeCircle(Color circleColor)
    {
        if (rangeCircle != null)
            return;

        GameObject go = new GameObject("Spell_RangeCircle");
        go.transform.SetParent(transform, worldPositionStays: false);
        go.transform.localPosition = Vector3.up * circleHeightOffset;
        go.transform.localRotation = Quaternion.identity;

        rangeCircle = go.AddComponent<LineRenderer>();
        rangeCircle.useWorldSpace = true;
        rangeCircle.loop = true;
        rangeCircle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rangeCircle.receiveShadows = false;

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

    /// <summary>
    /// Crée le disque de remplissage de portée si nécessaire.
    /// </summary>
    private void EnsureRangeFill(Color circleColor)
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

        UpdateRangeFillTransform(circleColor);
    }

    /// <summary>
    /// Construit un maillage circulaire simple pour le disque de portée.
    /// </summary>
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

    /// <summary>
    /// Met à jour la transformation et la couleur du disque de portée.
    /// </summary>
    private void UpdateRangeFillTransform(Color circleColor)
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

    /// <summary>
    /// Affiche les éléments visuels de la zone du sort.
    /// </summary>
    private void ShowRangeCircle(Color circleColor)
    {
        currentCircleColor = circleColor;
        EnsureRangeCircle(circleColor);

        Color outline = circleColor;
        outline.a = Mathf.Clamp01(circleOutlineAlpha);

        rangeCircle.startColor = outline;
        rangeCircle.endColor = outline;
        rangeCircle.startWidth = circleWidth;
        rangeCircle.endWidth = circleWidth;
        rangeCircle.positionCount = Mathf.Max(3, circleSegments);

        rangeCircle.enabled = true;
        float duration = 0f;
        if (casterUnit != null && casterUnit.unitData is UnitHealerData) 
        {
            duration = GetHealingDuration(); 
        } else if (casterUnit != null && casterUnit.unitData is UnitSupportData)
        {
            duration = GetBuffSupportDuration();
        }
        circleHideAtTime = (duration > 0f) ? (Time.time + duration) : -1f;

        UpdateRangeCirclePositions();

        EnsureRangeFill(circleColor);
        rangeFillRenderer.enabled = true;
        UpdateRangeFillTransform(circleColor);
    }

    /// <summary>
    /// Repositionne les points du contour circulaire autour du lanceur.
    /// </summary>
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

    /// <summary>
    /// Scanne les unités alliées proches pour appliquer l'effet du sort.
    /// </summary>
    private void ScanFriendlyUnits(int spell)
    {
        // On resynchronise au moment du cast, au cas où le UnitData a changé côté runtime.
        if (casterUnit != null && casterUnit.unitData is UnitHealerData hd && hd.healRange > 0f)
            scanRadius = hd.healRange;

        if (casterUnit == null)
            return;

        int casterPlayerId = casterUnit.playerId;
        int totalUnitsFound = 0;
        int friendlyUnitsFound = 0;
        float scanRadiusSq = scanRadius * scanRadius;
        Vector3 casterPosition = casterUnit.transform.position;

        List<UnitInstance> units = UnitsRegistry.GetSnapshot();
        for (int i = 0; i < units.Count; i++)
        {
            UnitInstance unit = units[i];
            if (unit == null)
                continue;

            Vector3 delta = unit.transform.position - casterPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude > scanRadiusSq)
                continue;

            totalUnitsFound++;
            if (unit.playerId != casterPlayerId)
                continue;
            if (casterUnit != null && unit == casterUnit)
                continue;
            friendlyUnitsFound++;
            if (casterUnit.unitData is UnitHealerData targetHealerData && spell == 1)
            {
                Buffs.ApplyRegen(this, regenByTarget, unit, targetHealerData, regenTickInterval, casterPlayerId);
                
            } else if (casterUnit.unitData is UnitSupportData supportData && spell != 1)
            {
                float buffMultiplicator = GetBuffMultiplicatorFromSpell(supportData, spell);
                Buffs.BuffStatistics(this, AttackSpeedBuffByTarget, unit, supportData,
                    buffMultiplicator, spell, casterPlayerId);
            }
        }
    }

    /// <summary>
    /// Retourne la durée du soin pour un lanceur de type healer.
    /// </summary>
    private float GetHealingDuration()
    {
        if (casterUnit != null && casterUnit.unitData is UnitHealerData healerData && healerData.healDuration > 0f)
            return healerData.healDuration;
        return 0f;
    }

    /// <summary>
    /// Retourne la durée du buff pour un lanceur de type support.
    /// </summary>
    private float GetBuffSupportDuration()
    {
        if (casterUnit != null && casterUnit.unitData is UnitSupportData supportData && supportData.buffDuration > 0f)
            return supportData.buffDuration;
        return 0f;
    }
    
    /// <summary>
    /// Retourne le temps de recharge du sort de soin.
    /// </summary>
    private float GetHealerCooldwon()
    {
        if (casterUnit != null && casterUnit.unitData is UnitHealerData healerData)
            return healerData.healCooldown;
        return 0f;
    }
    
    /// <summary>
    /// Retourne le temps de recharge du sort de support.
    /// </summary>
    private float GetSupportCooldwon()
    {
        if (casterUnit != null && casterUnit.unitData is UnitSupportData supportData)
            return supportData.buffCooldown;
        return 0f;
    }

    /// <summary>
    /// Détermine le rayon de scan à partir des données de l'unité.
    /// </summary>
    private float GetScanRadiusFromUnitsData()
    {
        if (casterUnit != null && casterUnit.unitData is UnitHealerData hd && hd.healRange > 0f)
        {
            scanRadius = hd.healRange;
        } else if (casterUnit != null && casterUnit.unitData is UnitSupportData sd && sd.buffRange > 0f)
        {
            scanRadius = sd.buffRange;
        }
        
        return scanRadius;
    }

    /// <summary>
    /// Calcule le multiplicateur de buff associé à un sort de support.
    /// </summary>
    private float GetBuffMultiplicatorFromSpell(UnitSupportData supportData, int spell)
    {
        return spell switch
        {
            2 => supportData.buffAttackSpeed,
            3 => supportData.buffSpeed,
            4 => supportData.buffDamage,
            _ => 1f
        };
    }
}

