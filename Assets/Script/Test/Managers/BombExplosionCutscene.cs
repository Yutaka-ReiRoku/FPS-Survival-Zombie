using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Plays a short cinematic when a target story quest completes (e.g. Quest 12 —
/// escaping the town): cuts to a temporary camera looking at the bomb site,
/// spawns a mushroom-cloud VFX + explosion SFX, holds the shot, then cuts back
/// to normal gameplay.
///
/// Listens to StoryManager.OnQuestCompleted and fires once when targetQuest
/// matches the completed quest (leave targetQuest null to fire on ANY quest
/// completion — not recommended). Does not modify QuestTrigger/StoryManager/
/// WaveQuestInteractable in any way; it is a pure additive listener, following
/// the same subscribe pattern used by ChapterBoundary.
/// </summary>
public class BombExplosionCutscene : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("Quest that must complete to fire this cutscene (e.g. Quest_12_EscapeTown). " +
             "Leave null to fire on ANY quest completion (not recommended).")]
    public QuestData targetQuest;

    [Header("Camera")]
    [Tooltip("World position for the temporary cutscene camera.")]
    public Vector3 cameraPoint = new Vector3(-271f, 37.08f, 42.47f);

    [Header("Explosion")]
    [Tooltip("World position where the bomb explodes (mushroom cloud VFX + SFX origin).")]
    public Vector3 explosionPoint = new Vector3(0.92f, 0.9f, 1.4f);

    [Tooltip("Mushroom cloud VFX prefab (e.g. FX_Nuke_Light_01).")]
    public GameObject nukeVfxPrefab;

    [Tooltip("Uniform scale multiplier applied to the instantiated VFX.")]
    public float vfxScale = 4f;

    [Tooltip("Explosion sound effect.")]
    public AudioClip explosionSfx;

    [Tooltip("Optional mixer group so the SFX respects the master volume slider. Safe to leave null.")]
    public AudioMixerGroup sfxMixerGroup;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("Timing")]
    [Tooltip("Fade to/from black duration at the start/end of the cutscene.")]
    public float fadeDuration = 0.6f;

    [Tooltip("How long (seconds) the camera holds on the explosion.")]
    public float holdDuration = 5f;

    [Tooltip("How long (real seconds) the VFX instance stays before being destroyed after the cutscene ends.")]
    public float vfxLifetime = 20f;

    private bool _fired;
    private CanvasGroup _fadeGroup;
    private GameObject _fadeCanvasGO;

    private void OnEnable() => Subscribe();

    private void Start() => Subscribe(); // Fallback in case OnEnable ran before StoryManager.Awake.

    private void OnDisable()
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
    }

    private void Subscribe()
    {
        if (StoryManager.Instance == null) return;
        StoryManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        StoryManager.Instance.OnQuestCompleted += HandleQuestCompleted;
    }

    private void HandleQuestCompleted(QuestData quest)
    {
        if (_fired) return;
        if (targetQuest != null && quest != targetQuest) return;

        _fired = true;
        Debug.Log($"[BombExplosionCutscene] Firing for quest '{quest?.title}'.");
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        BuildFadeCanvas();

        float prevTimeScale = Time.timeScale;

        // Fade to black before cutting the camera.
        yield return Fade(0f, 1f, fadeDuration);

        // Freeze gameplay for the duration of the cutscene (same approach as CutscenePlayer).
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        Rigidbody playerRb = playerGO != null ? playerGO.GetComponent<Rigidbody>() : null;
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }
        Time.timeScale = 0f;

        // --- Set up cutscene camera ---
        var mainCam = Camera.main;
        AudioListener mainListener = mainCam != null ? mainCam.GetComponent<AudioListener>() : null;
        if (mainListener != null) mainListener.enabled = false;

        var camGO = new GameObject("BombExplosion_CutsceneCamera");
        camGO.transform.position = cameraPoint;
        Vector3 lookDir = explosionPoint - cameraPoint;
        camGO.transform.rotation = lookDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(lookDir.normalized, Vector3.up)
            : Quaternion.identity;

        var cam = camGO.AddComponent<Camera>();
        if (mainCam != null)
        {
            cam.fieldOfView = mainCam.fieldOfView;
            cam.nearClipPlane = mainCam.nearClipPlane;
            cam.farClipPlane = mainCam.farClipPlane;
            cam.clearFlags = mainCam.clearFlags;
            cam.backgroundColor = mainCam.backgroundColor;
            cam.cullingMask = mainCam.cullingMask;
            cam.depth = mainCam.depth + 10f;

            // Reuse the main camera's PostProcessLayer settings, including the
            // internal PostProcessResources asset (m_Resources) which isn't set
            // when AddComponent is called at runtime and causes NREs inside
            // built-in effects like AmbientOcclusion.
            var mainPPLayer = mainCam.GetComponent<PostProcessLayer>();
            if (mainPPLayer != null)
            {
                var ppLayer = camGO.AddComponent<PostProcessLayer>();
                ppLayer.volumeLayer = mainPPLayer.volumeLayer;
                ppLayer.volumeTrigger = camGO.transform;
                ppLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;

                var resourcesField = typeof(PostProcessLayer).GetField(
                    "m_Resources", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (resourcesField != null)
                    resourcesField.SetValue(ppLayer, resourcesField.GetValue(mainPPLayer));
            }
        }
        else
        {
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2000f;
        }

        camGO.AddComponent<AudioListener>();

        // --- Spawn VFX + SFX ---
        GameObject vfxInstance = null;
        if (nukeVfxPrefab != null)
        {
            vfxInstance = Instantiate(nukeVfxPrefab, explosionPoint, Quaternion.identity);
            vfxInstance.transform.localScale *= vfxScale;

            // Keep particle systems animating while Time.timeScale is 0.
            var systems = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                var main = ps.main;
                main.useUnscaledTime = true;
            }
        }
        else
        {
            Debug.LogWarning("[BombExplosionCutscene] nukeVfxPrefab is not assigned — no VFX will be spawned.");
        }

        if (explosionSfx != null)
        {
            var sfxGO = new GameObject("BombExplosion_SFX");
            sfxGO.transform.position = explosionPoint;
            var src = sfxGO.AddComponent<AudioSource>();
            src.clip = explosionSfx;
            src.outputAudioMixerGroup = sfxMixerGroup;
            src.spatialBlend = 0f; // 2D — always audible for cinematic impact regardless of camera distance.
            src.volume = sfxVolume;
            src.playOnAwake = false;
            src.Play();
            Destroy(sfxGO, explosionSfx.length + 0.5f);
        }

        // Reveal the cutscene.
        yield return Fade(1f, 0f, fadeDuration);

        // Hold on the explosion.
        yield return WaitRealtime(holdDuration);

        // Fade out to cut back to gameplay.
        yield return Fade(0f, 1f, fadeDuration);

        Destroy(camGO);
        if (mainListener != null) mainListener.enabled = true;
        if (vfxInstance != null) Destroy(vfxInstance, vfxLifetime);

        Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;

        // Reveal gameplay again.
        yield return Fade(1f, 0f, fadeDuration);

        Destroy(_fadeCanvasGO);
        Debug.Log("[BombExplosionCutscene] Sequence complete.");
    }

    private IEnumerator WaitRealtime(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        _fadeGroup.alpha = from;
        if (duration <= 0f) { _fadeGroup.alpha = to; yield break; }
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _fadeGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _fadeGroup.alpha = to;
    }

    private void BuildFadeCanvas()
    {
        _fadeCanvasGO = new GameObject("BombExplosion_FadeCanvas", typeof(Canvas), typeof(CanvasGroup));
        _fadeCanvasGO.transform.SetParent(transform, false);
        var canvas = _fadeCanvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        var imgGO = new GameObject("Black", typeof(RectTransform));
        imgGO.transform.SetParent(_fadeCanvasGO.transform, false);
        var rt = (RectTransform)imgGO.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = imgGO.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        _fadeGroup = _fadeCanvasGO.GetComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f;
        _fadeGroup.blocksRaycasts = false;
        _fadeGroup.interactable = false;
    }
}
