using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Custom combat feedback on the unified GameUICanvas (no Cowsins HUD dependency):
///  - Hitmarker: brief center X flash on hit (red + bigger on headshot/kill).
///  - Damage numbers: world-projected floating TMP that rises + fades (pooled).
///  - Killfeed: top-right entries that auto-expire.
/// Self-building: creates its child UI at runtime under a full-stretch root, themed
/// via UITheme. All animation uses unscaled time so it reads while paused. Add this
/// component to a full-stretch RectTransform under the HUD canvas.
/// </summary>
public class CombatFeedbackHUD : MonoBehaviour
{
    public static CombatFeedbackHUD Instance;

    [Tooltip("Camera used to project world hit positions. Defaults to Camera.main.")]
    public Camera worldCamera;

    private static readonly string[] _numberStrings = new string[1000];
    private static string GetDamageString(int dmg, bool crit)
    {
        if (dmg >= 0 && dmg < 1000)
        {
            if (_numberStrings[dmg] == null)
            {
                _numberStrings[dmg] = dmg.ToString();
            }
            return crit ? (_numberStrings[dmg] + "!") : _numberStrings[dmg];
        }
        return crit ? (dmg + "!") : dmg.ToString();
    }

    private RectTransform _root;

    // ---- Hitmarker ----
    private RectTransform _hit;
    private CanvasGroup _hitCg;
    private float _hitTimer, _hitDuration = 0.18f;
    private Color _hitNormal = new Color(1f, 1f, 1f, 0.9f);
    private Color _hitKill = new Color(0.95f, 0.32f, 0.27f, 1f);
    private Color _critColor = new Color(1f, 0.78f, 0.2f, 1f);

    // Skill-crit flag, set by the weapon (Bullet) just before damage is applied, then
    // consumed by the very next ShowHit (same frame). Lets us style crits distinctly
    // without the engine's Damage signature carrying a crit flag.
    private bool _critPending;
    private int _critFrame;
    public void FlagCriticalHit() { _critPending = true; _critFrame = Time.frameCount; }

    // ---- Damage numbers (pool) ----
    private class Dmg { public RectTransform rt; public TMP_Text tmp; public CanvasGroup cg; public float life; public Vector2 vel; public bool active; }
    private readonly List<Dmg> _pool = new List<Dmg>();
    private RectTransform _dmgContainer;
    private const int PoolSize = 28;
    private float _dmgLife = 0.8f;

    // ---- Killfeed ----
    private RectTransform _killContainer;
    private class Kill { public GameObject go; public CanvasGroup cg; public TextMeshProUGUI tmp; public float life; public bool active; }
    private readonly List<Kill> _kills = new List<Kill>();
    private readonly List<Kill> _killPool = new List<Kill>();
    private float _killLife = 4f;

    private UITheme _theme;

    private void Awake()
    {
        Instance = this;
        _theme = UITheme.Active;
        _root = (RectTransform)transform;
        _root.anchorMin = Vector2.zero; _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero; _root.offsetMax = Vector2.zero; _root.pivot = new Vector2(0.5f, 0.5f);
        if (_theme != null) { _hitKill = _theme.dangerTop; _critColor = _theme.accent; }
        BuildHitmarker();
        BuildDamageContainer();
        BuildKillfeed();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    // ---------- Build ----------
    private RectTransform NewChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private Image Bar(Transform parent, float w, float h, float angle)
    {
        var rt = NewChild("Bar", parent);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localRotation = Quaternion.Euler(0, 0, angle);
        var img = rt.gameObject.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private void BuildHitmarker()
    {
        _hit = NewChild("Hitmarker", _root);
        _hit.anchorMin = _hit.anchorMax = _hit.pivot = new Vector2(0.5f, 0.5f);
        _hit.sizeDelta = new Vector2(40, 40);
        _hit.anchoredPosition = Vector2.zero;
        _hitCg = _hit.gameObject.AddComponent<CanvasGroup>();
        _hitCg.alpha = 0f; _hitCg.interactable = false; _hitCg.blocksRaycasts = false;
        // four diagonal ticks forming an X
        Bar(_hit, 14f, 4f, 45f).rectTransform.anchoredPosition = new Vector2(-12, 12);
        Bar(_hit, 14f, 4f, -45f).rectTransform.anchoredPosition = new Vector2(12, 12);
        Bar(_hit, 14f, 4f, -45f).rectTransform.anchoredPosition = new Vector2(-12, -12);
        Bar(_hit, 14f, 4f, 45f).rectTransform.anchoredPosition = new Vector2(12, -12);
    }

    private void BuildDamageContainer()
    {
        _dmgContainer = NewChild("DamageNumbers", _root);
        _dmgContainer.anchorMin = Vector2.zero; _dmgContainer.anchorMax = Vector2.one;
        _dmgContainer.offsetMin = Vector2.zero; _dmgContainer.offsetMax = Vector2.zero;
        _dmgContainer.pivot = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < PoolSize; i++)
        {
            var rt = NewChild("Dmg", _dmgContainer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(140, 44);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center; tmp.fontSize = 28; tmp.raycastTarget = false;
            PremiumUITheme.StyleValue(tmp);
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f; cg.blocksRaycasts = false;
            rt.gameObject.SetActive(false);
            _pool.Add(new Dmg { rt = rt, tmp = tmp, cg = cg, active = false });
        }
    }

    private void BuildKillfeed()
    {
        _killContainer = NewChild("Killfeed", _root);
        _killContainer.anchorMin = new Vector2(1, 1); _killContainer.anchorMax = new Vector2(1, 1);
        _killContainer.pivot = new Vector2(1, 1);
        _killContainer.anchoredPosition = new Vector2(-24, -24);
        _killContainer.sizeDelta = new Vector2(360, 400);
        var vlg = _killContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperRight;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
        vlg.spacing = _theme != null ? _theme.spaceS : 8f;
    }

    // ---------- API ----------
    public void ShowHit(Vector3 worldPos, float damage, bool headshot)
    {
        bool crit = _critPending && Time.frameCount == _critFrame;
        _critPending = false;

        // hitmarker
        _hitTimer = _hitDuration;
        _hit.localScale = Vector3.one * (crit ? 1.6f : headshot ? 1.4f : 1f);
        var col = crit ? _critColor : headshot ? _hitKill : _hitNormal;
        foreach (var img in _hit.GetComponentsInChildren<Image>()) img.color = col;

        // damage number
        var d = GetDmg();
        if (d == null) return;
        var cam = worldCamera != null ? worldCamera : Camera.main;
        Vector2 local = Vector2.zero;
        if (cam != null)
        {
            Vector3 sp = cam.WorldToScreenPoint(worldPos + Vector3.up * 1.6f);
            if (sp.z < 0f) { d.active = false; d.rt.gameObject.SetActive(false); return; } // behind camera
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_dmgContainer, sp, null, out local);
        }
        d.rt.anchoredPosition = local + new Vector2(Random.Range(-18f, 18f), 0f);
        int dmgInt = Mathf.Max(1, Mathf.RoundToInt(damage));
        d.tmp.text = GetDamageString(dmgInt, crit);
        d.tmp.color = crit ? _critColor : headshot ? _hitKill : (_theme != null ? _theme.textPrimary : Color.white);
        d.tmp.fontSize = crit ? 42 : headshot ? 36 : 28;
        d.cg.alpha = 1f;
        d.life = _dmgLife;
        d.vel = new Vector2(Random.Range(-10f, 10f), 90f);
        d.active = true;
        d.rt.gameObject.SetActive(true);
    }

    public void ShowKill(string name)
    {
        Kill k = null;
        for (int i = 0; i < _killPool.Count; i++)
        {
            if (!_killPool[i].active)
            {
                k = _killPool[i];
                break;
            }
        }

        if (k == null)
        {
            var rt = NewChild("KillEntry", _killContainer);
            rt.sizeDelta = new Vector2(360, 40);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 40; le.preferredWidth = 360;
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = _theme != null ? _theme.surfaceTop : new Color(0.137f, 0.165f, 0.2f, 0.9f);
            bg.raycastTarget = false;
            
            var txtRT = NewChild("Text", rt);
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(12, 0); txtRT.offsetMax = new Vector2(-12, 0);
            var tmp = txtRT.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.MidlineRight; tmp.fontSize = 22; tmp.raycastTarget = false;
            PremiumUITheme.StyleLabel(tmp);
            tmp.color = _theme != null ? _theme.textPrimary : Color.white;
            
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            
            k = new Kill { go = rt.gameObject, cg = cg, tmp = tmp, active = true };
            _killPool.Add(k);
        }
        else
        {
            k.go.transform.SetAsLastSibling();
            k.go.SetActive(true);
            k.active = true;
        }

        k.tmp.text = "Killed  " + (string.IsNullOrEmpty(name) ? "Zombie" : name);
        k.cg.alpha = 1f;
        k.life = _killLife;
        _kills.Add(k);

        while (_kills.Count > 6)
        {
            var oldest = _kills[0];
            _kills.RemoveAt(0);
            oldest.active = false;
            if (oldest.go != null) oldest.go.SetActive(false);
        }
    }

    private Dmg GetDmg()
    {
        for (int i = 0; i < _pool.Count; i++) if (!_pool[i].active) return _pool[i];
        // reuse oldest (lowest life)
        Dmg oldest = null; float min = float.MaxValue;
        foreach (var d in _pool) if (d.life < min) { min = d.life; oldest = d; }
        return oldest;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // clear a stale crit flag if it wasn't consumed the frame it was set
        if (_critPending && Time.frameCount != _critFrame) _critPending = false;

        // hitmarker fade
        if (_hitTimer > 0f)
        {
            _hitTimer -= dt;
            _hitCg.alpha = Mathf.Clamp01(_hitTimer / _hitDuration);
            if (_hitTimer <= 0f) _hitCg.alpha = 0f;
        }

        // damage numbers
        for (int i = 0; i < _pool.Count; i++)
        {
            var d = _pool[i];
            if (!d.active) continue;
            d.life -= dt;
            if (d.life <= 0f) { d.active = false; d.cg.alpha = 0f; d.rt.gameObject.SetActive(false); continue; }
            d.rt.anchoredPosition += d.vel * dt;
            d.cg.alpha = Mathf.Clamp01(d.life / _dmgLife);
        }

        // killfeed lifetime
        for (int i = _kills.Count - 1; i >= 0; i--)
        {
            var k = _kills[i];
            k.life -= dt;
            if (k.life <= 0f)
            {
                _kills.RemoveAt(i);
                k.active = false;
                if (k.go) k.go.SetActive(false);
                continue;
            }
            if (k.cg != null && k.life < 0.6f) k.cg.alpha = Mathf.Clamp01(k.life / 0.6f);
        }
    }
}
