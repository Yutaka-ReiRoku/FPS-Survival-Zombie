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

    private VisualElement _container;
    private VisualElement[] _bars;
    private CowsinsHUDAdapter _adapter;
    private float _spread;
    private float _thickness;

    private const int BarCount = 13;
    private static readonly string[] BarNames =
    {
        "CHTop", "CHDown", "CHLeft", "CHRight", "CHCenter",
        "CHTL_H", "CHTL_V", "CHTR_H", "CHTR_V",
        "CHBL_H", "CHBL_V", "CHBR_H", "CHBR_V"
    };

    private void Awake()
    {
        _spread = defaultSpread;
        _thickness = lineThickness;
    }

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) doc = FindFirstObjectByType<UIDocument>();
        if (doc == null) { enabled = false; return; }
        _container = doc.rootVisualElement.Q("Crosshair");
        if (_container == null) { enabled = false; return; }
        Build();
        StartCoroutine(Bind());
    }

    private void Build()
    {
        _container.Clear();
        _bars = new VisualElement[BarCount];
        for (int i = 0; i < BarCount; i++)
        {
            var bar = new VisualElement { name = BarNames[i] };
            bar.AddToClassList("crosshair-bar");
            bar.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            _container.Add(bar);
            _bars[i] = bar;
        }
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
        _thickness = Mathf.Lerp(_thickness, a != null && a.EnemySpotted ? enemyThickness : lineThickness, resizeSpeed * dt);

        bool spotted = a != null && a.EnemySpotted;
        float cx = _container.resolvedStyle.width / 2f;
        float cy = _container.resolvedStyle.height / 2f;
        float L = lineLength, t = _thickness;
        float halfGap = _spread / 2f;
        float s = _spread;

        bool noWeapon = a == null || !a.HasWeapon;
        Set(_bars[0],  noWeapon || a.CHTop,      0f,     halfGap,   t,  L,   spotted, cx, cy);
        Set(_bars[1],  noWeapon || a.CHDown,      0f,     -halfGap,  t,  L,   spotted, cx, cy);
        Set(_bars[2],  noWeapon || a.CHRight,     halfGap, 0f,       L,  t,   spotted, cx, cy);
        Set(_bars[3],  noWeapon || a.CHLeft,      -halfGap,0f,       L,  t,   spotted, cx, cy);

        float d = Mathf.Min(t, L);
        Set(_bars[4],  a != null && a.HasWeapon && a.CHCenter, 0f, 0f, d, d, spotted, cx, cy);

        bool tl = a != null && a.HasWeapon && a.CHTopLeft;
        bool tr = a != null && a.HasWeapon && a.CHTopRight;
        bool bl = a != null && a.HasWeapon && a.CHBottomLeft;
        bool br = a != null && a.HasWeapon && a.CHBottomRight;
        Set(_bars[5],  tl, -s + L / 2f,  s - t / 2f,  L,  t,  spotted, cx, cy);
        Set(_bars[6],  tl, -s + t / 2f,  s - L / 2f,  t,  L,  spotted, cx, cy);
        Set(_bars[7],  tr,  s - L / 2f,  s - t / 2f,  L,  t,  spotted, cx, cy);
        Set(_bars[8],  tr,  s - t / 2f,  s - L / 2f,  t,  L,  spotted, cx, cy);
        Set(_bars[9],  bl, -s + L / 2f, -s + t / 2f,  L,  t,  spotted, cx, cy);
        Set(_bars[10], bl, -s + t / 2f, -s + L / 2f,  t,  L,  spotted, cx, cy);
        Set(_bars[11], br,  s - L / 2f, -s + t / 2f,  L,  t,  spotted, cx, cy);
        Set(_bars[12], br,  s - t / 2f, -s + L / 2f,  t,  L,  spotted, cx, cy);

        bool hidden = a != null && (a.IsDead || (a.IsAiming && removeCrosshairOnAiming));
        _container.style.opacity = Mathf.MoveTowards(_container.style.opacity.value, hidden ? 0f : 1f, 12f * dt);
    }

    private void Set(VisualElement bar, bool active, float posX, float posY, float w, float h, bool enemy, float cx, float cy)
    {
        if (bar == null) return;
        bar.EnableInClassList("crosshair-bar--hidden", !active);
        if (!active) return;
        bar.style.translate = new Translate(cx + posX - w / 2f, cy - posY - h / 2f);
        bar.style.width = w;
        bar.style.height = h;
        bar.EnableInClassList("crosshair-bar--enemy", enemy);
    }
}
