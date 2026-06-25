using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Custom HUD widget for coins + XP/level, fed by CowsinsHUDAdapter (engine-free).
/// Mirrors the bind/unbind pattern of the other widgets.
/// </summary>
public class ProgressionHUDWidget : MonoBehaviour
{
    [Header("Coins")]
    public TMP_Text coinsText;
    public string coinsPrefix = "";

    [Header("XP / Level")]
    public TMP_Text levelText;
    public Slider xpSlider;       // non-interactable; value driven by adapter
    public Image xpGhost;         // optional delayed trail (Filled Image)
    public string levelPrefix = "LV ";

    [Header("Tuning")]
    [Tooltip("Damped lerp rate for XP fill (higher = snappier).")]
    public float xpDamping = 6f;
    [Tooltip("Damped lerp rate for XP ghost trail.")]
    public float xpGhostDamping = 2.5f;

    private CowsinsHUDAdapter _adapter;
    private float _xpTarget;

    private void OnEnable() { StartCoroutine(Bind()); }

    private IEnumerator Bind()
    {
        float timeout = 12f;
        while (CowsinsHUDAdapter.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        _adapter = CowsinsHUDAdapter.Instance;
        if (_adapter == null) yield break;
        // Configure slider as read-only HUD bar
        if (xpSlider != null)
        {
            xpSlider.interactable = false;
            xpSlider.minValue = 0f;
            xpSlider.maxValue = 1f;
            xpSlider.wholeNumbers = false;
        }
        _adapter.OnCoinsChanged += HandleCoins;
        _adapter.OnXpChanged += HandleXp;
        HandleCoins(_adapter.Coins);
        HandleXp(_adapter.PlayerLevel, _adapter.XpFill);
    }

    private void OnDisable()
    {
        if (_adapter != null)
        {
            _adapter.OnCoinsChanged -= HandleCoins;
            _adapter.OnXpChanged -= HandleXp;
        }
    }

    private void HandleCoins(int coins)
    {
        if (coinsText != null) coinsText.text = coinsPrefix + coins.ToString();
    }

    private void HandleXp(int level, float fill)
    {
        if (levelText != null) levelText.text = levelPrefix + level.ToString();
        _xpTarget = Mathf.Clamp01(fill);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (xpSlider != null)
        {
            float k = 1f - Mathf.Exp(-xpDamping * dt);
            xpSlider.value = Mathf.Lerp(xpSlider.value, _xpTarget, k);
        }
        if (xpGhost != null)
        {
            if (xpGhost.fillAmount < _xpTarget) xpGhost.fillAmount = _xpTarget;
            else xpGhost.fillAmount = Mathf.Lerp(xpGhost.fillAmount, _xpTarget, 1f - Mathf.Exp(-xpGhostDamping * dt));
        }
    }
}
