using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Custom dash-charge pips on the unified HUD, fed by CowsinsHUDAdapter.OnDashChanged.
/// Self-builds a centered row of pips; filled = available charges, dim = on cooldown.
/// Hidden when the player has no dash / infinite dashes. Engine-free (adapter only).
/// </summary>
public class DashWidget : MonoBehaviour
{
    [Tooltip("Pip size in px.")] public float pipSize = 20f;
    [Tooltip("Gap between pips in px.")] public float spacing = 8f;

    private RectTransform _row;
    private readonly List<Image> _pips = new List<Image>();
    private readonly List<Image> _regen = new List<Image>();
    private int _current, _max;
    private float _regenStart;
    private Color _on = new Color(0.45f, 0.78f, 0.95f, 1f);
    private Color _off = new Color(1f, 1f, 1f, 0.18f);
    private CowsinsHUDAdapter _adapter;

    private void Awake()
    {
        var th = UITheme.Active;
        if (th != null) { _on = th.shield; }
        Build();
    }

    private void Build()
    {
        _row = (RectTransform)transform;
        // expect to be placed on a bottom-centered RectTransform; ensure a layout row
        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false; hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.spacing = spacing;
    }

    private void OnEnable() { StartCoroutine(Bind()); }

    private IEnumerator Bind()
    {
        float timeout = 12f;
        while (CowsinsHUDAdapter.Instance == null && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
        _adapter = CowsinsHUDAdapter.Instance;
        if (_adapter == null) yield break;
        _adapter.OnDashChanged += HandleDash;
        HandleDash(_adapter.CurrentDashes, _adapter.MaxDashes);
    }

    private void OnDisable()
    {
        if (_adapter != null) _adapter.OnDashChanged -= HandleDash;
        StopAllCoroutines();
    }

    private void HandleDash(int current, int max)
    {
        _current = current; _max = max;
        if (max <= 0)
        {
            EnsurePips(0); // hide pips but keep this component active + subscribed
            return;
        }
        EnsurePips(max);
        for (int i = 0; i < _pips.Count; i++)
            _pips[i].color = i < current ? _on : _off;
        // restart the visible regen timer whenever charges aren't full
        _regenStart = current < max ? Time.unscaledTime : 0f;
    }

    private void Update()
    {
        float cd = _adapter != null ? _adapter.DashCooldown : 0f;
        for (int i = 0; i < _regen.Count; i++)
        {
            bool show = i == _current && _current < _max && cd > 0f && _regenStart > 0f;
            if (_regen[i].enabled != show) _regen[i].enabled = show;
            if (show) _regen[i].fillAmount = Mathf.Clamp01((Time.unscaledTime - _regenStart) / cd);
        }
    }

    private void EnsurePips(int count)
    {
        while (_pips.Count < count)
        {
            var go = new GameObject("Pip", typeof(RectTransform));
            go.transform.SetParent(_row, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(pipSize, pipSize);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = pipSize; le.preferredHeight = pipSize;
            var img = go.AddComponent<Image>();
            img.color = _off; img.raycastTarget = false;
            _pips.Add(img);

            // radial regen fill overlay on this pip (shown while it's the recharging slot)
            var rg = new GameObject("Regen", typeof(RectTransform));
            rg.transform.SetParent(go.transform, false);
            var rrt = (RectTransform)rg.transform;
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one; rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var rimg = rg.AddComponent<Image>();
            rimg.color = new Color(_on.r, _on.g, _on.b, 0.55f);
            rimg.raycastTarget = false;
            rimg.type = Image.Type.Filled;
            rimg.fillMethod = Image.FillMethod.Radial360;
            rimg.fillOrigin = (int)Image.Origin360.Top;
            rimg.fillClockwise = true;
            rimg.fillAmount = 0f;
            rimg.enabled = false;
            _regen.Add(rimg);
        }
        for (int i = 0; i < _pips.Count; i++)
            _pips[i].gameObject.SetActive(i < count);
    }
}
