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
    public Image xpFill;
    public string levelPrefix = "LV ";

    private CowsinsHUDAdapter _adapter;

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
        if (xpFill != null) xpFill.fillAmount = Mathf.Clamp01(fill);
    }
}
