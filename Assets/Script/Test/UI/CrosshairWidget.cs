using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Custom uGUI crosshair on the unified HUD, replacing the Cowsins IMGUI Crosshair
/// (drawn outside any Canvas via OnGUI). Faithfully reproduces:
///   - the four lines (top/down/left/right), a centre dot and the four corner
///     brackets, toggled per the equipped weapon's crosshairParts,
///   - spread that widens with movement state (idle/walk/run/crouch/jump) and kicks
///     open on firing, easing back,
///   - the enemy-spotted colour + line-thickening,
///   - hidden while aiming down sights or dead.
/// Geometry mirrors the Cowsins OnGUI math: LINES offset by spread/2, BRACKETS by the
/// full spread. Reads CowsinsHUDAdapter only; values mirror the scene's Crosshair.
/// Sizes are 1920x1080 reference px (scaled by the canvas). Root stays active.
/// </summary>
public class CrosshairWidget : MonoBehaviour
{
    [Header("Geometry (reference px @1920x1080)")]
    public float lineLength = 10f;      // Cowsins 'size'
    public float lineThickness = 2f;    // Cowsins 'width'
    public float enemyThickness = 5f;   // Cowsins 'enemySpottedWidth'

    [Header("Spread per state")]
    public float defaultSpread = 20f;
    public float walkSpread = 45f;
    public float runSpread = 100f;
    public float crouchSpread = 20f;
    public float jumpSpread = 160f;
    public float resizeSpeed = 15f;

    [Header("Behaviour")]
    public bool removeCrosshairOnAiming = true;

    [Header("Colours")]
    public Color defaultColor = new Color(0.171f, 1f, 0f, 1f);
    public Color enemySpottedColor = new Color(1f, 0f, 0f, 1f);

    private CanvasGroup _group;
    private Image _top, _down, _left, _right, _center;
    private Image _tlH, _tlV, _trH, _trV, _blH, _blV, _brH, _brV;
    private CowsinsHUDAdapter _adapter;
    private float _spread;
    private float _thickness;

    private void Awake()
    {
        _spread = defaultSpread;
        _thickness = lineThickness;
        Build();
    }

    private void Build()
    {
        var container = new GameObject("Crosshair", typeof(RectTransform));
        container.transform.SetParent(transform, false);
        var crt = (RectTransform)container.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = Vector2.zero;
        _group = container.AddComponent<CanvasGroup>();
        _group.interactable = false; _group.blocksRaycasts = false; _group.alpha = 1f;

        _top = MakeBar(crt, "Top");
        _down = MakeBar(crt, "Down");
        _left = MakeBar(crt, "Left");
        _right = MakeBar(crt, "Right");
        _center = MakeBar(crt, "Center");
        _tlH = MakeBar(crt, "TL_H"); _tlV = MakeBar(crt, "TL_V");
        _trH = MakeBar(crt, "TR_H"); _trV = MakeBar(crt, "TR_V");
        _blH = MakeBar(crt, "BL_H"); _blV = MakeBar(crt, "BL_V");
        _brH = MakeBar(crt, "BR_H"); _brV = MakeBar(crt, "BR_V");
    }

    private Image MakeBar(RectTransform parent, string n)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        var img = go.AddComponent<Image>();
        img.color = defaultColor;
        img.raycastTarget = false;
        return img;
    }

    private void OnEnable() { StartCoroutine(Bind()); }

    private IEnumerator Bind()
    {
        float timeout = 12f;
        while (CowsinsHUDAdapter.Instance == null && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
        _adapter = CowsinsHUDAdapter.Instance;
        if (_adapter == null) yield break;
        _adapter.OnFired += HandleFired;
    }

    private void OnDisable()
    {
        if (_adapter != null) _adapter.OnFired -= HandleFired;
        StopAllCoroutines();
    }

    // Fire kick: bloom the spread toward the weapon's resize value (one Lerp step,
    // matching the Cowsins per-shot Resize), then Update eases it back.
    private void HandleFired()
    {
        if (_adapter == null) return;
        float kick = _adapter.WeaponCrosshairResize * 10f;
        _spread = Mathf.Lerp(_spread, kick, 0.25f);
    }

    private void Update()
    {
        if (_group == null) return;
        float dt = Time.unscaledDeltaTime;
        var a = _adapter;

        // --- target spread from movement state ---
        float target = defaultSpread;
        if (a != null)
        {
            if (a.MoveGrounded)
            {
                float cs = a.MoveCurrentSpeed, run = a.MoveRunSpeed, walk = a.MoveWalkSpeed, crouch = a.MoveCrouchSpeed;
                if (run > 0f && Mathf.Approximately(cs, run) && !a.MoveIsIdle) target = runSpread;
                else if (walk > 0f && Mathf.Approximately(cs, walk)) target = a.MoveIsIdle ? defaultSpread : walkSpread;
                else if (crouch > 0f && Mathf.Approximately(cs, crouch)) target = crouchSpread;
                else target = defaultSpread;
            }
            else target = jumpSpread;
        }
        _spread = Mathf.Lerp(_spread, target, resizeSpeed * dt);

        // --- colour + thickness on enemy spotted ---
        bool spotted = a != null && a.EnemySpotted;
        Color col = spotted ? enemySpottedColor : defaultColor;
        _thickness = Mathf.Lerp(_thickness, spotted ? enemyThickness : lineThickness, resizeSpeed * dt);

        float L = lineLength, t = _thickness;
        float halfGap = _spread / 2f;   // lines use spread/2
        float s = _spread;              // brackets use full spread

        // lines
        Set(_top, a == null || a.CHTop, new Vector2(0f, halfGap), new Vector2(t, L), col);
        Set(_down, a == null || a.CHDown, new Vector2(0f, -halfGap), new Vector2(t, L), col);
        Set(_right, a == null || a.CHRight, new Vector2(halfGap, 0f), new Vector2(L, t), col);
        Set(_left, a == null || a.CHLeft, new Vector2(-halfGap, 0f), new Vector2(L, t), col);
        // centre dot
        float d = Mathf.Min(t, L);
        Set(_center, a != null && a.CHCenter, Vector2.zero, new Vector2(d, d), col);
        // brackets (each = horizontal + vertical bar)
        bool tl = a != null && a.CHTopLeft, tr = a != null && a.CHTopRight, bl = a != null && a.CHBottomLeft, br = a != null && a.CHBottomRight;
        Set(_tlH, tl, new Vector2(-s + L / 2f, s - t / 2f), new Vector2(L, t), col);
        Set(_tlV, tl, new Vector2(-s + t / 2f, s - L / 2f), new Vector2(t, L), col);
        Set(_trH, tr, new Vector2(s - L / 2f, s - t / 2f), new Vector2(L, t), col);
        Set(_trV, tr, new Vector2(s - t / 2f, s - L / 2f), new Vector2(t, L), col);
        Set(_blH, bl, new Vector2(-s + L / 2f, -s + t / 2f), new Vector2(L, t), col);
        Set(_blV, bl, new Vector2(-s + t / 2f, -s + L / 2f), new Vector2(t, L), col);
        Set(_brH, br, new Vector2(s - L / 2f, -s + t / 2f), new Vector2(L, t), col);
        Set(_brV, br, new Vector2(s - t / 2f, -s + L / 2f), new Vector2(t, L), col);

        // --- visibility ---
        bool hidden = a != null && (a.IsDead || (a.IsAiming && removeCrosshairOnAiming));
        _group.alpha = Mathf.MoveTowards(_group.alpha, hidden ? 0f : 1f, 12f * dt);
    }

    private void Set(Image bar, bool active, Vector2 pos, Vector2 size, Color col)
    {
        if (bar == null) return;
        if (bar.gameObject.activeSelf != active) bar.gameObject.SetActive(active);
        if (!active) return;
        var rt = (RectTransform)bar.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        bar.color = col;
    }
}
