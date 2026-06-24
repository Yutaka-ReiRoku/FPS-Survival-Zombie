using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Directional damage indicator on the unified HUD. The engine's PlayerStats.Damage
/// carries no direction, so attackers (ZombieAI.AttackHit) call ShowDamageFrom(worldPos)
/// here; we compute the bearing relative to the camera's facing and flash a red arc on
/// that side of the screen. Pooled so multiple simultaneous hits show. Self-building,
/// engine-free, unscaled time, singleton, root stays active.
/// </summary>
public class DamageDirectionHUD : MonoBehaviour
{
    public static DamageDirectionHUD Instance { get; private set; }

    [Tooltip("Ring radius (reference px) the arc sits at from screen center.")]
    public float radius = 160f;
    public float arcWidth = 120f, arcHeight = 16f;
    public float fadeTime = 1.0f;
    public int poolSize = 8;

    private readonly List<RectTransform> _pivots = new List<RectTransform>();
    private readonly List<CanvasGroup> _groups = new List<CanvasGroup>();
    private Color _color = new Color(0.95f, 0.32f, 0.27f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        var th = UITheme.Active;
        if (th != null) _color = th.dangerTop;
        Build();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Build()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var pivot = new GameObject("Dir", typeof(RectTransform));
            pivot.transform.SetParent(transform, false);
            var prt = (RectTransform)pivot.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = Vector2.zero;
            var cg = pivot.AddComponent<CanvasGroup>();
            cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

            // arc sits above center; rotating the pivot moves it around the ring.
            var arc = new GameObject("Arc", typeof(RectTransform));
            arc.transform.SetParent(prt, false);
            var art = (RectTransform)arc.transform;
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
            art.pivot = new Vector2(0.5f, 0.5f);
            art.anchoredPosition = new Vector2(0f, radius);
            art.sizeDelta = new Vector2(arcWidth, arcHeight);
            var img = arc.AddComponent<Image>();
            img.color = _color; img.raycastTarget = false;

            _pivots.Add(prt);
            _groups.Add(cg);
        }
    }

    /// <summary>Flash an indicator pointing toward a world-space attacker position.</summary>
    public void ShowDamageFrom(Vector3 worldAttackerPos)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 dir = worldAttackerPos - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Vector3 fwd = cam.transform.forward; fwd.y = 0f;
        float bearing = Vector3.SignedAngle(fwd, dir, Vector3.up); // +right, -left

        // pick the most-faded indicator to reuse
        int idx = 0; float min = float.MaxValue;
        for (int i = 0; i < _groups.Count; i++)
            if (_groups[i].alpha < min) { min = _groups[i].alpha; idx = i; }

        _pivots[idx].localEulerAngles = new Vector3(0f, 0f, -bearing); // uGUI Z+ is CCW
        _groups[idx].alpha = 1f;
    }

    private void Update()
    {
        float drop = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime);
        for (int i = 0; i < _groups.Count; i++)
            if (_groups[i].alpha > 0f)
                _groups[i].alpha = Mathf.Max(0f, _groups[i].alpha - drop);
    }
}
