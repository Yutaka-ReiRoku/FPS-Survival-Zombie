using System.Collections;
using UnityEngine;

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

    [Header("Ground Ring")]
    [Tooltip("Show a pulsing ring on the ground.")]
    public bool showGroundRing = true;

    [Tooltip("Ground ring radius.")]
    public float ringRadius = 2f;

    [Tooltip("Ground ring color.")]
    public Color ringColor = new Color(1f, 0.85f, 0.3f, 0.6f);

    private GameObject _beam;
    private GameObject _icon;
    private GameObject _ring;
    private Material _beamMat;
    private Material _iconMat;
    private Material _ringMat;
    private bool _active;
    private float _bobTime;
    private Transform _player;

    private void OnEnable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnActiveQuestChanged += HandleQuestChanged;
            StoryManager.Instance.OnChapterChanged += HandleChapterChanged;
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
    }

    private void HandleQuestChanged(QuestData oldQuest, QuestData newQuest)
    {
        EvaluateActivation();
    }

    private void HandleChapterChanged(int oldCh, int newCh)
    {
        EvaluateActivation();
    }

    private void EvaluateActivation()
    {
        var sm = StoryManager.Instance;
        if (sm == null) { SetActive(false); return; }

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
    }

    private void BuildVisuals()
    {
        if (_beam != null) return; // Already built.

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");

        // ---- Beam (vertical cylinder) ----
        _beam = new GameObject("Beacon_Beam");
        _beam.transform.SetParent(transform, false);
        _beam.transform.localPosition = new Vector3(0f, beamHeight * 0.5f, 0f);
        _beam.transform.localScale = new Vector3(beamRadius * 2f, beamHeight * 0.5f, beamRadius * 2f);

        var beamFilter = _beam.AddComponent<MeshFilter>();
        beamFilter.sharedMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder).GetComponent<MeshFilter>().sharedMesh;
        var beamRenderer = _beam.AddComponent<MeshRenderer>();
        _beamMat = new Material(shader) { name = "BeaconBeam_Runtime" };
        _beamMat.color = beamColor;
        beamRenderer.material = _beamMat;
        beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beamRenderer.receiveShadows = false;
        DestroyIfExists(_beam.GetComponent<Collider>());

        // ---- Floating icon (quad that always faces camera) ----
        _icon = new GameObject("Beacon_Icon");
        _icon.transform.SetParent(transform, false);
        _icon.transform.localPosition = new Vector3(0f, iconHeight, 0f);

        var iconFilter = _icon.AddComponent<MeshFilter>();
        iconFilter.sharedMesh = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshFilter>().sharedMesh;
        var iconRenderer = _icon.AddComponent<MeshRenderer>();
        _iconMat = new Material(shader) { name = "BeaconIcon_Runtime" };
        _iconMat.color = iconColor;
        iconRenderer.material = _iconMat;
        iconRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        iconRenderer.receiveShadows = false;
        DestroyIfExists(_icon.GetComponent<Collider>());

        // ---- Ground ring (flat cylinder) ----
        if (showGroundRing)
        {
            _ring = new GameObject("Beacon_Ring");
            _ring.transform.SetParent(transform, false);
            _ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            _ring.transform.localScale = new Vector3(ringRadius * 2f, 0.05f, ringRadius * 2f);

            var ringFilter = _ring.AddComponent<MeshFilter>();
            ringFilter.sharedMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder).GetComponent<MeshFilter>().sharedMesh;
            var ringRenderer = _ring.AddComponent<MeshRenderer>();
            _ringMat = new Material(shader) { name = "BeaconRing_Runtime" };
            _ringMat.color = ringColor;
            ringRenderer.material = _ringMat;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            DestroyIfExists(_ring.GetComponent<Collider>());
        }
    }

    private void DestroyIfExists(Component c)
    {
        if (c != null) DestroyImmediate(c);
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
        }
    }
}
