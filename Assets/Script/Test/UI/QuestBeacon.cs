using System.Collections;
using UnityEngine;

/// <summary>
/// Icon displayed on the floating beacon marker. Auto picks an icon based on
/// the beacon's activation source (chapter = house, side quest = book, main
/// quest = skull/exclamation) unless overridden.
/// </summary>
public enum BeaconIconType
{
    Auto,
    Skull,
    Book,
    House,
    Exclamation,
    None
}

/// <summary>
/// World-space waypoint beacon that guides the player to the next quest
/// objective. Shows a tall vertical light beam + a floating bobbing icon so
/// the player can spot the destination from anywhere in the chapter.
///
/// The beacon auto-activates when its `showOnQuest` becomes the active quest
/// (or when `showOnChapter` matches the current chapter and no quest is
/// assigned — useful for guiding the player to the save room on chapter entry).
/// It auto-deactivates when the quest is completed or the chapter advances.
///
/// Place one QuestBeacon per key objective location. The beacon is purely
/// visual (no collider, no raycast target) so it never blocks gameplay.
/// </summary>
public class QuestBeacon : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("Quest that activates this beacon. When this quest is active, the beacon shows. " +
             "Leave null to use showOnChapter instead.")]
    public QuestData showOnQuest;

    [Tooltip("If showOnQuest is null, show the beacon while the current chapter matches this number. " +
             "Useful for guiding the player to the save room when they first enter a chapter.")]
    public int showOnChapter = 0;

    [Tooltip("Side quest that activates this beacon. When this side quest is active in " +
             "SideQuestManager, the beacon shows. Used for side-quest objective markers " +
             "(typically green to distinguish from main-quest gold beacons).")]
    public QuestData showOnSideQuest;

    [Tooltip("If true, hide the beacon once the player gets within this distance.")]
    public bool hideWhenClose = true;

    [Tooltip("Distance at which the beacon hides (if hideWhenClose is true).")]
    public float hideDistance = 4f;

    [Header("Beam")]
    [Tooltip("Height of the light beam.")]
    public float beamHeight = 12f;

    [Tooltip("Radius of the beam cylinder.")]
    public float beamRadius = 0.4f;

    [Tooltip("Beam color.")]
    public Color beamColor = new Color(1f, 0.85f, 0.3f, 0.5f);

    [Header("Floating Icon")]
    [Tooltip("Icon height above the beacon origin.")]
    public float iconHeight = 3f;

    [Tooltip("Icon bob amplitude.")]
    public float iconBobAmplitude = 0.4f;

    [Tooltip("Icon bob speed.")]
    public float iconBobSpeed = 2f;

    [Tooltip("Icon size (world units).")]
    public float iconSize = 1.2f;

    [Tooltip("Icon color.")]
    public Color iconColor = new Color(1f, 0.9f, 0.4f, 1f);

    [Tooltip("Icon to display on the floating marker. Auto picks based on activation source " +
             "(chapter = house, side quest = book, main quest = skull).")]
    public BeaconIconType iconType = BeaconIconType.Auto;

    [Header("Ground Ring")]
    [Tooltip("Show a pulsing ring on the ground.")]
    public bool showGroundRing = true;

    [Tooltip("Ground ring radius.")]
    public float ringRadius = 2f;

    [Tooltip("Ground ring color.")]
    public Color ringColor = new Color(1f, 0.85f, 0.3f, 0.6f);

    [Header("VFX")]
    [Tooltip("Add a rising particle stream inside the beam for extra glow.")]
    public bool beamParticles = true;

    [Tooltip("Add an expanding ripple particle effect on the ground ring.")]
    public bool ringParticles = true;

    [Tooltip("Add a point light at the beam base for ambient glow.")]
    public bool beamLight = true;

    private GameObject _beam;
    private GameObject _icon;
    private GameObject _ring;
    private GameObject _beamParticles;
    private GameObject _ringParticles;
    private Light _beamLight;
    private Material _beamMat;
    private Material _iconMat;
    private Material _ringMat;
    private bool _active;
    private float _bobTime;
    private Transform _player;

    // Cached base materials loaded once from Resources/Assets.
    private static Material _baseBeamMat;
    private static Material _baseRingParticleMat;
    private static Material _baseBeamParticleMat;

    private void OnEnable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnActiveQuestChanged += HandleQuestChanged;
            StoryManager.Instance.OnChapterChanged += HandleChapterChanged;
        }
        if (SideQuestManager.Instance != null)
        {
            SideQuestManager.Instance.OnSideQuestActivated += HandleSideQuestChanged;
            SideQuestManager.Instance.OnSideQuestCompleted += HandleSideQuestChanged;
        }
        BuildVisuals();
        EvaluateActivation();
    }

    private void OnDisable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnActiveQuestChanged -= HandleQuestChanged;
            StoryManager.Instance.OnChapterChanged -= HandleChapterChanged;
        }
        if (SideQuestManager.Instance != null)
        {
            SideQuestManager.Instance.OnSideQuestActivated -= HandleSideQuestChanged;
            SideQuestManager.Instance.OnSideQuestCompleted -= HandleSideQuestChanged;
        }
    }

    private void OnDestroy()
    {
        if (_beamMat != null) Destroy(_beamMat);
        if (_iconMat != null) Destroy(_iconMat);
        if (_ringMat != null) Destroy(_ringMat);
    }

    /// <summary>
    /// Resolve the effective icon type based on the Auto setting and the
    /// beacon's activation source (chapter, side quest, or main quest).
    /// </summary>
    private BeaconIconType ResolveIconType()
    {
        if (iconType != BeaconIconType.Auto) return iconType;

        if (showOnChapter > 0 && showOnQuest == null && showOnSideQuest == null)
            return BeaconIconType.House;
        if (showOnSideQuest != null)
            return BeaconIconType.Book;
        // Main quest: use skull for combat quests, exclamation for reach quests.
        // Simple heuristic: if the quest name contains "kill" or "bomb" use skull.
        if (showOnQuest != null && showOnQuest.title != null)
        {
            string t = showOnQuest.title.ToLower();
            if (t.Contains("bắn") || t.Contains("đối đầu") || t.Contains("dọn") || t.Contains("bomb"))
                return BeaconIconType.Skull;
        }
        return BeaconIconType.Exclamation;
    }

    /// <summary>
    /// Load the sprite for the given icon type from Resources/BeaconIcons.
    /// Returns null for None.
    /// </summary>
    private static Sprite LoadIconSprite(BeaconIconType type)
    {
        switch (type)
        {
            case BeaconIconType.Skull: return Resources.Load<Sprite>("BeaconIcons/icon_skull");
            case BeaconIconType.Book: return Resources.Load<Sprite>("BeaconIcons/icon_book");
            case BeaconIconType.House: return Resources.Load<Sprite>("BeaconIcons/icon_house");
            case BeaconIconType.Exclamation: return Resources.Load<Sprite>("BeaconIcons/icon_exclaim");
            default: return null;
        }
    }

    private void HandleQuestChanged(QuestData oldQuest, QuestData newQuest)
    {
        EvaluateActivation();
    }

    private void HandleChapterChanged(int oldCh, int newCh)
    {
        EvaluateActivation();
    }

    private void HandleSideQuestChanged(QuestData quest)
    {
        EvaluateActivation();
    }

    private void EvaluateActivation()
    {
        var sm = StoryManager.Instance;
        if (sm == null) { SetActive(false); return; }

        // Priority: side quest > main quest > chapter
        if (showOnSideQuest != null)
        {
            var sqm = SideQuestManager.Instance;
            bool active = sqm != null && sqm.IsActive(showOnSideQuest) && !sqm.IsCompleted(showOnSideQuest);
            SetActive(active);
            return;
        }

        if (showOnQuest != null)
        {
            // Show when this quest is the active quest.
            SetActive(sm.ActiveQuest == showOnQuest);
        }
        else if (showOnChapter > 0)
        {
            // Show when the current chapter matches and no quest-specific beacon
            // is handling things. This guides the player to the save room on entry.
            SetActive(sm.CurrentChapter == showOnChapter);
        }
        else
        {
            SetActive(false);
        }
    }

    private void SetActive(bool active)
    {
        _active = active;
        if (_beam != null) _beam.SetActive(active);
        if (_icon != null) _icon.SetActive(active);
        if (_ring != null) _ring.SetActive(active);
        if (_beamParticles != null) _beamParticles.SetActive(active);
        if (_ringParticles != null) _ringParticles.SetActive(active);
        if (_beamLight != null) _beamLight.enabled = active;
    }

    private void BuildVisuals()
    {
        if (_beam != null) return; // Already built.

        Shader unlitShader = Shader.Find("Sprites/Default");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Transparent");

        // Load base materials once (cached statically so all beacons share the source).
        if (_baseBeamMat == null)
            _baseBeamMat = Resources.Load<Material>("VFX/Beacon/BeaconBeam");
        if (_baseBeamParticleMat == null)
        {
            _baseBeamParticleMat = Resources.Load<Material>("VFX/Beacon/BeaconBeamParticle");
            if (_baseBeamParticleMat == null && unlitShader != null)
                _baseBeamParticleMat = new Material(unlitShader);
        }
        if (_baseRingParticleMat == null)
        {
            _baseRingParticleMat = Resources.Load<Material>("VFX/Beacon/BeaconRingParticle");
            if (_baseRingParticleMat == null && unlitShader != null)
                _baseRingParticleMat = new Material(unlitShader);
        }

        // Reusable primitive meshes (avoid CreatePrimitive which leaks GameObjects).
        Mesh cylinderMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);
        Mesh quadMesh = GetPrimitiveMesh(PrimitiveType.Quad);

        // ---- Beam (vertical cylinder with emission glow) ----
        _beam = new GameObject("Beacon_Beam");
        _beam.transform.SetParent(transform, false);
        _beam.transform.localPosition = new Vector3(0f, beamHeight * 0.5f, 0f);
        _beam.transform.localScale = new Vector3(beamRadius * 2f, beamHeight * 0.5f, beamRadius * 2f);

        var beamFilter = _beam.AddComponent<MeshFilter>();
        beamFilter.sharedMesh = cylinderMesh;
        var beamRenderer = _beam.AddComponent<MeshRenderer>();
        // Use Standard shader with emission if base material loaded, else fallback.
        if (_baseBeamMat != null)
        {
            _beamMat = new Material(_baseBeamMat) { name = "BeaconBeam_Runtime" };
            _beamMat.SetColor("_Color", new Color(beamColor.r, beamColor.g, beamColor.b, beamColor.a));
            _beamMat.SetColor("_EmissionColor", new Color(beamColor.r, beamColor.g, beamColor.b) * 0.8f);
        }
        else
        {
            _beamMat = new Material(unlitShader) { name = "BeaconBeam_Runtime" };
            _beamMat.color = beamColor;
        }
        beamRenderer.material = _beamMat;
        beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beamRenderer.receiveShadows = false;

        // ---- Beam point light (ambient glow at base) ----
        if (beamLight)
        {
            var lightGO = new GameObject("Beacon_Light");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            _beamLight = lightGO.AddComponent<Light>();
            _beamLight.type = LightType.Point;
            _beamLight.color = new Color(beamColor.r, beamColor.g, beamColor.b, 1f);
            _beamLight.intensity = 1.2f;
            _beamLight.range = 6f;
            _beamLight.shadows = LightShadows.None;
        }

        // ---- Beam particles (rising sparks inside the beam) ----
        if (beamParticles && _baseBeamParticleMat != null)
        {
            _beamParticles = new GameObject("Beacon_BeamParticles");
            _beamParticles.transform.SetParent(transform, false);
            _beamParticles.transform.localPosition = Vector3.zero;
            var ps = _beamParticles.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = beamHeight / 2f;
            main.startSpeed = beamHeight / main.startLifetime.constant;
            main.startSize = 0.15f;
            main.startColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.7f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emit = ps.emission;
            emit.rateOverTime = 15f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = beamRadius * 0.5f;
            shape.angle = 5f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(beamColor, 0f), new GradientColorKey(beamColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var rend = _beamParticles.GetComponent<ParticleSystemRenderer>();
            rend.material = _baseBeamParticleMat;
        }

        // ---- Floating icon (quad with sprite texture, billboard) ----
        BeaconIconType resolvedIcon = ResolveIconType();
        Sprite iconSprite = LoadIconSprite(resolvedIcon);

        _icon = new GameObject("Beacon_Icon");
        _icon.transform.SetParent(transform, false);
        _icon.transform.localPosition = new Vector3(0f, iconHeight, 0f);
        _icon.transform.localScale = new Vector3(iconSize, iconSize, iconSize);

        var iconFilter = _icon.AddComponent<MeshFilter>();
        iconFilter.sharedMesh = quadMesh;
        var iconRenderer = _icon.AddComponent<MeshRenderer>();
        _iconMat = new Material(unlitShader) { name = "BeaconIcon_Runtime" };
        _iconMat.color = iconColor;
        if (iconSprite != null)
            _iconMat.SetTexture("_MainTex", iconSprite.texture);
        iconRenderer.material = _iconMat;
        iconRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        iconRenderer.receiveShadows = false;

        // ---- Ground ring (flat cylinder with pulse) ----
        if (showGroundRing)
        {
            _ring = new GameObject("Beacon_Ring");
            _ring.transform.SetParent(transform, false);
            _ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            _ring.transform.localScale = new Vector3(ringRadius * 2f, 0.05f, ringRadius * 2f);

            var ringFilter = _ring.AddComponent<MeshFilter>();
            ringFilter.sharedMesh = cylinderMesh;
            var ringRenderer = _ring.AddComponent<MeshRenderer>();
            _ringMat = new Material(unlitShader) { name = "BeaconRing_Runtime" };
            _ringMat.color = ringColor;
            ringRenderer.material = _ringMat;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;

            // ---- Ring particles (expanding ripple) ----
            if (ringParticles && _baseRingParticleMat != null)
            {
                _ringParticles = new GameObject("Beacon_RingParticles");
                _ringParticles.transform.SetParent(transform, false);
                _ringParticles.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                var rps = _ringParticles.AddComponent<ParticleSystem>();
                var rmain = rps.main;
                rmain.loop = true;
                rmain.startLifetime = 2f;
                rmain.startSpeed = 0f;
                rmain.startSize = 0.1f;
                rmain.startColor = new Color(ringColor.r, ringColor.g, ringColor.b, 0.6f);
                rmain.maxParticles = 30;
                rmain.simulationSpace = ParticleSystemSimulationSpace.Local;
                var remit = rps.emission;
                remit.rateOverTime = 5f;
                var rshape = rps.shape;
                rshape.shapeType = ParticleSystemShapeType.Circle;
                rshape.radius = 0.1f;
                // Size grows over lifetime (ripple outward)
                var rsize = rps.sizeOverLifetime;
                rsize.enabled = true;
                rsize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.1f), new Keyframe(1f, ringRadius * 2f)));
                // Fade out
                var rcol = rps.colorOverLifetime;
                rcol.enabled = true;
                var rgrad = new Gradient();
                rgrad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(ringColor, 0f), new GradientColorKey(ringColor, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) });
                rcol.color = rgrad;
                var rrend = _ringParticles.GetComponent<ParticleSystemRenderer>();
                rrend.material = _baseRingParticleMat;
            }
        }
    }

    /// <summary>
    /// Returns the built-in primitive mesh without creating a GameObject.
    /// Uses a temporary primitive once and caches the mesh statically so
    /// subsequent calls don't leak GameObjects into the scene.
    /// </summary>
    private static Mesh _cylinderMesh;
    private static Mesh _quadMesh;
    private static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        if (type == PrimitiveType.Cylinder && _cylinderMesh != null) return _cylinderMesh;
        if (type == PrimitiveType.Quad && _quadMesh != null) return _quadMesh;

        var temp = GameObject.CreatePrimitive(type);
        var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temp);
        if (type == PrimitiveType.Cylinder) _cylinderMesh = mesh;
        else if (type == PrimitiveType.Quad) _quadMesh = mesh;
        return mesh;
    }

    private void EnsurePlayer()
    {
        if (_player != null) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    private void Update()
    {
        if (!_active) return;

        EnsurePlayer();

        // Bob the icon.
        _bobTime += Time.deltaTime * iconBobSpeed;
        if (_icon != null)
        {
            float bob = Mathf.Sin(_bobTime) * iconBobAmplitude;
            _icon.transform.localPosition = new Vector3(0f, iconHeight + bob, 0f);

            // Face the camera (billboard).
            if (Camera.main != null)
            {
                _icon.transform.rotation = Quaternion.LookRotation(
                    _icon.transform.position - Camera.main.transform.position);
            }
        }

        // Pulse the ring.
        if (_ring != null)
        {
            float pulse = 1f + Mathf.Sin(_bobTime * 1.5f) * 0.15f;
            _ring.transform.localScale = new Vector3(ringRadius * 2f * pulse, 0.05f, ringRadius * 2f * pulse);
        }

        // Hide when player is close.
        if (hideWhenClose && _player != null)
        {
            float dist = Vector3.Distance(
                new Vector3(_player.position.x, 0f, _player.position.z),
                new Vector3(transform.position.x, 0f, transform.position.z));
            bool show = dist > hideDistance;
            if (_beam != null) _beam.SetActive(show);
            if (_ring != null) _ring.SetActive(show);
            if (_beamParticles != null) _beamParticles.SetActive(show);
            if (_ringParticles != null) _ringParticles.SetActive(show);
            if (_beamLight != null) _beamLight.enabled = show;
        }
    }
}
