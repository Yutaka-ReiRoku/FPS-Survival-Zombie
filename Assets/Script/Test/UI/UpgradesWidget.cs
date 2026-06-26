using UnityEngine;
using TMPro;
using cowsins;

/// <summary>
/// Compact readout of the permanent perks banked via the level-up cards
/// (PlayerUpgradeManager bonusHealth/bonusShield/bonusMagazine). These were tracked
/// but never shown. Self-builds a small labeled line, polls the manager, and hides
/// itself while nothing is banked. Engine-free read of the custom manager only.
/// </summary>
public class UpgradesWidget : MonoBehaviour
{
    public float fontSize = 20f;

    private CanvasGroup _group;
    private TMP_Text _label;
    private int _lastH = int.MinValue, _lastS = int.MinValue;
    private float _lastSta = float.MinValue, _lastDmg = float.MinValue;
    private Color _accent = new Color(0.85f, 0.78f, 0.45f, 1f);

    private void Awake()
    {
        var th = UITheme.Active;
        if (th != null) _accent = th.accent;
        Build();
    }

    private void Build()
    {
        var th = UITheme.Active;
        var container = new GameObject("Perks", typeof(RectTransform));
        container.transform.SetParent(transform, false);
        var crt = (RectTransform)container.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(320f, 28f);
        _group = container.AddComponent<CanvasGroup>();
        _group.alpha = 0f; _group.interactable = false; _group.blocksRaycasts = false;

        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(crt, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _label = go.AddComponent<TextMeshProUGUI>();
        _label.alignment = TextAlignmentOptions.Right;
        _label.fontSize = fontSize;
        _label.color = _accent;
        _label.raycastTarget = false;
        if (th != null && th.headerFont != null) _label.font = th.headerFont;
    }

    private void Update()
    {
        var m = PlayerUpgradeManager.Instance;
        int h = m != null ? m.bonusHealth : 0;
        int s = m != null ? m.bonusShield : 0;
        float sta = m != null ? m.bonusStamina : 0f;
        float dmg = m != null ? m.bonusDamage : 0f;
        if (h == _lastH && s == _lastS && Mathf.Approximately(sta, _lastSta) && Mathf.Approximately(dmg, _lastDmg)) return;
        _lastH = h; _lastS = s; _lastSta = sta; _lastDmg = dmg;

        if (h == 0 && s == 0 && sta <= 0f && dmg <= 0f) { _group.alpha = 0f; return; }

        var parts = new System.Collections.Generic.List<string>(4);
        if (h > 0) parts.Add("+" + h + " HP");
        if (s > 0) parts.Add("+" + s + " ARMOR");
        if (sta > 0f) parts.Add("+" + sta.ToString("0.#") + " STA");
        if (dmg > 0f) parts.Add("+" + (dmg * 100f).ToString("0.#") + "% DMG");
        _label.text = "PERKS   " + string.Join("   ", parts);
        _group.alpha = 1f;
    }
}
