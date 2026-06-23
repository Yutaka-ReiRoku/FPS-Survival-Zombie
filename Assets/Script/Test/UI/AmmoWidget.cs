using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Premium ammo display: magazine number + reserve + optional overheat. Reads CowsinsHUDAdapter only.</summary>
public class AmmoWidget : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text ammoText;        // big magazine number
    public TMP_Text reserveText;     // small reserve count
    public Image heatFill;           // optional overheat bar (Filled)
    public RectTransform punchRoot;  // scales on each shot
    [Header("Style")]
    public string infiniteSymbol = "\u221E";
    public Color normalColor = new Color(0.92f, 0.88f, 0.78f, 1f);
    public Color lowColor = new Color(0.85f, 0.35f, 0.15f, 1f);
    [Range(0f, 1f)] public float lowFraction = 0.34f;
    public float punchScale = 1.14f;

    private Vector3 _home = Vector3.one;
    private Coroutine _punch;

    private void OnEnable() { StartCoroutine(Bind()); }

    private void OnDisable()
    {
        var a = CowsinsHUDAdapter.Instance;
        if (a != null) { a.OnAmmoChanged -= OnAmmo; a.OnHeatChanged -= OnHeat; a.OnFired -= OnFired; }
        StopAllCoroutines();
    }

    private IEnumerator Bind()
    {
        var th = UITheme.Active;
        if (th != null) { normalColor = th.ammoNormal; lowColor = th.ammoLow; punchScale = th.ammoPunchScale; }
        if (punchRoot != null) _home = punchRoot.localScale;
        while (CowsinsHUDAdapter.Instance == null) yield return null;
        var a = CowsinsHUDAdapter.Instance;
        a.OnAmmoChanged += OnAmmo;
        a.OnHeatChanged += OnHeat;
        a.OnFired += OnFired;
        OnAmmo(a.Ammo, a.Reserve);
        OnHeat(a.Heat);
    }

    private void OnAmmo(int mag, int reserve)
    {
        var a = CowsinsHUDAdapter.Instance;
        if (ammoText != null)
        {
            ammoText.text = mag.ToString();
            bool low = a != null && a.MagazineSize > 0 && mag <= Mathf.CeilToInt(a.MagazineSize * lowFraction);
            ammoText.color = low ? lowColor : normalColor;
        }
        if (reserveText != null)
            reserveText.text = (a != null && !a.LimitedReserve) ? infiniteSymbol : reserve.ToString();
    }

    private void OnHeat(float heat)
    {
        if (heatFill == null) return;
        heatFill.gameObject.SetActive(heat > 0.001f);
        heatFill.fillAmount = Mathf.Clamp01(heat);
        heatFill.color = Color.Lerp(normalColor, lowColor, Mathf.Clamp01(heat));
    }

    private void OnFired()
    {
        if (punchRoot == null) return;
        if (_punch != null) StopCoroutine(_punch);
        _punch = StartCoroutine(Punch());
    }

    private IEnumerator Punch()
    {
        float t = 0f, dur = 0.12f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - (t / dur);
            punchRoot.localScale = _home * (1f + (punchScale - 1f) * k);
            yield return null;
        }
        punchRoot.localScale = _home;
    }
}
