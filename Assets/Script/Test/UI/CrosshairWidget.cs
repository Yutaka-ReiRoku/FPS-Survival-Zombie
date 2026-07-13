using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class CrosshairWidget : MonoBehaviour
{
    [Header("Geometry (reference px @1920x1080)")]
    public float lineLength = 10f;
    public float lineThickness = 2f;
    public float enemyThickness = 5f;

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

    private VisualElement _container;
    private VisualElement _top, _down, _left, _right, _center;
    private VisualElement _tlH, _tlV, _trH, _trV, _blH, _blV, _brH, _brV;
    private CowsinsHUDAdapter _adapter;
    private float _spread;
    private float _thickness;

    private void Awake()
    {
        _spread = defaultSpread;
        _thickness = lineThickness;
    }

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) doc = FindObjectOfType<UIDocument>();
        if (doc == null) { enabled = false; return; }
        _container = doc.rootVisualElement.Q("Crosshair");
        if (_container == null) { enabled = false; return; }
        Build();
        StartCoroutine(Bind());
    }

    private void Build()
    {
        _container.Clear();
        _top = MakeBar("CHTop");
        _down = MakeBar("CHDown");
        _left = MakeBar("CHLeft");
        _right = MakeBar("CHRight");
        _center = MakeBar("CHCenter");
        _tlH = MakeBar("CHTL_H"); _tlV = MakeBar("CHTL_V");
        _trH = MakeBar("CHTR_H"); _trV = MakeBar("CHTR_V");
        _blH = MakeBar("CHBL_H"); _blV = MakeBar("CHBL_V");
        _brH = MakeBar("CHBR_H"); _brV = MakeBar("CHBR_V");
    }

    private VisualElement MakeBar(string name)
    {
        var el = new VisualElement { name = name };
        el.AddToClassList("crosshair-bar");
        _container.Add(el);
        return el;
    }

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

    private void HandleFired()
    {
        if (_adapter == null) return;
        float kick = _adapter.WeaponCrosshairResize * 10f;
        _spread = Mathf.Lerp(_spread, kick, 0.25f);
    }

    private void Update()
    {
        if (_container == null) return;
        float dt = Time.unscaledDeltaTime;
        var a = _adapter;

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

        bool spotted = a != null && a.EnemySpotted;
        Color col = spotted ? enemySpottedColor : defaultColor;
        _thickness = Mathf.Lerp(_thickness, spotted ? enemyThickness : lineThickness, resizeSpeed * dt);

        float cx = _container.resolvedStyle.width / 2f;
        float cy = _container.resolvedStyle.height / 2f;
        float L = lineLength, t = _thickness;
        float halfGap = _spread / 2f;
        float s = _spread;

        bool noWeapon = a == null || !a.HasWeapon;
        Set(_top, noWeapon || a.CHTop, 0f, halfGap, t, L, col, cx, cy);
        Set(_down, noWeapon || a.CHDown, 0f, -halfGap, t, L, col, cx, cy);
        Set(_right, noWeapon || a.CHRight, halfGap, 0f, L, t, col, cx, cy);
        Set(_left, noWeapon || a.CHLeft, -halfGap, 0f, L, t, col, cx, cy);

        float d = Mathf.Min(t, L);
        Set(_center, a != null && a.HasWeapon && a.CHCenter, 0f, 0f, d, d, col, cx, cy);

        bool tl = a != null && a.HasWeapon && a.CHTopLeft;
        bool tr = a != null && a.HasWeapon && a.CHTopRight;
        bool bl = a != null && a.HasWeapon && a.CHBottomLeft;
        bool br = a != null && a.HasWeapon && a.CHBottomRight;
        Set(_tlH, tl, -s + L / 2f, s - t / 2f, L, t, col, cx, cy);
        Set(_tlV, tl, -s + t / 2f, s - L / 2f, t, L, col, cx, cy);
        Set(_trH, tr, s - L / 2f, s - t / 2f, L, t, col, cx, cy);
        Set(_trV, tr, s - t / 2f, s - L / 2f, t, L, col, cx, cy);
        Set(_blH, bl, -s + L / 2f, -s + t / 2f, L, t, col, cx, cy);
        Set(_blV, bl, -s + t / 2f, -s + L / 2f, t, L, col, cx, cy);
        Set(_brH, br, s - L / 2f, -s + t / 2f, L, t, col, cx, cy);
        Set(_brV, br, s - t / 2f, -s + L / 2f, t, L, col, cx, cy);

        bool hidden = a != null && (a.IsDead || (a.IsAiming && removeCrosshairOnAiming));
        _container.style.opacity = Mathf.MoveTowards(_container.style.opacity.value, hidden ? 0f : 1f, 12f * dt);
    }

    private void Set(VisualElement bar, bool active, float posX, float posY, float w, float h, Color col, float cx, float cy)
    {
        if (bar == null) return;
        bar.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        if (!active) return;
        bar.style.left = cx + posX - w / 2f;
        bar.style.top = cy - posY - h / 2f;
        bar.style.width = w;
        bar.style.height = h;
        bar.style.backgroundColor = new StyleColor(col);
    }
}
