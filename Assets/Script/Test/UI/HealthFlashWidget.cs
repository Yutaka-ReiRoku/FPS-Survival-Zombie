using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen edge-vignette flash on the unified HUD, replacing the Cowsins
/// "HealthStatesEffect" image that lived on the separate PlayerUI canvas. Pulses a
/// tinted vignette on damage (red), heal (green), coin pickup (gold) and xp gain
/// (violet), then fades out. Reads CowsinsHUDAdapter only; uses unscaled time so the
/// fade completes even if a pause/death freezes timeScale. Keeps its root active so
/// the adapter subscriptions are never torn down.
/// </summary>
public class HealthFlashWidget : MonoBehaviour
{
    [Tooltip("White vignette sprite tinted per event (Cowsins HurtVignetteWhite).")]
    public Sprite vignetteSprite;
    [Tooltip("Alpha units removed per second during fade (Cowsins fadeOutTime).")]
    public float fadeOutSpeed = 0.5f;

    // Faithful to the Cowsins UIController colours.
    public Color damageColor = new Color(1f, 0.382f, 0.382f, 0.71f);
    public Color healColor = new Color(0.637f, 1f, 0.683f, 0.78f);
    public Color coinColor = new Color(1f, 0.946f, 0.382f, 0.851f);
    public Color xpColor = new Color(0.5f, 0.099f, 1f, 0.604f);

    private Image _img;
    private CowsinsHUDAdapter _adapter;
    private float _lastHp = -1f;
    private int _lastCoins = -1;
    private float _lastXpFill = -1f;
    private int _lastLevel = -1;

    private void Awake() { Build(); }

    private void Build()
    {
        var go = new GameObject("Flash", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _img = go.AddComponent<Image>();
        _img.sprite = vignetteSprite;
        _img.type = Image.Type.Simple;
        _img.raycastTarget = false;
        var c = damageColor; c.a = 0f; _img.color = c;
    }

    private void OnEnable() { StartCoroutine(Bind()); }

    private IEnumerator Bind()
    {
        float timeout = 12f;
        while (CowsinsHUDAdapter.Instance == null && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
        _adapter = CowsinsHUDAdapter.Instance;
        if (_adapter == null) yield break;
        _lastHp = _adapter.Health;
        _lastCoins = _adapter.Coins;
        _lastXpFill = _adapter.XpFill;
        _lastLevel = _adapter.PlayerLevel;
        _adapter.OnHealthChanged += OnHealth;
        _adapter.OnCoinsChanged += OnCoins;
        _adapter.OnXpChanged += OnXp;
    }

    private void OnDisable()
    {
        if (_adapter != null)
        {
            _adapter.OnHealthChanged -= OnHealth;
            _adapter.OnCoinsChanged -= OnCoins;
            _adapter.OnXpChanged -= OnXp;
        }
        StopAllCoroutines();
    }

    private void OnHealth(float hp, float max, bool damaged)
    {
        if (damaged || (_lastHp >= 0f && hp < _lastHp - 0.001f)) Flash(damageColor);
        else if (_lastHp >= 0f && hp > _lastHp + 0.001f) Flash(healColor);
        _lastHp = hp;
    }

    private void OnCoins(int coins)
    {
        if (_lastCoins >= 0 && coins > _lastCoins) Flash(coinColor);
        _lastCoins = coins;
    }

    private void OnXp(int level, float fill)
    {
        bool gained = (_lastLevel >= 0 && level > _lastLevel) || (_lastXpFill >= 0f && fill > _lastXpFill + 0.001f);
        if (gained) Flash(xpColor);
        _lastXpFill = fill;
        _lastLevel = level;
    }

    private void Flash(Color color)
    {
        if (_img == null) return;
        _img.color = color; // includes the event's target alpha
    }

    private void Update()
    {
        if (_img == null) return;
        var c = _img.color;
        if (c.a > 0f)
        {
            c.a = Mathf.Max(0f, c.a - Time.unscaledDeltaTime * fadeOutSpeed);
            _img.color = c;
        }
    }
}
