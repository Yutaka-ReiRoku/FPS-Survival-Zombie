using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Current weapon name + icon, with a quick fade-in on swap. Reads CowsinsHUDAdapter only.</summary>
public class WeaponIndicatorWidget : MonoBehaviour
{
    public TMP_Text nameText;
    public Image iconImage;
    public CanvasGroup group;     // optional, for crossfade on swap
    public float fadeSpeed = 6f;

    private Coroutine _fade;

    private void OnEnable() { StartCoroutine(Bind()); }

    private void OnDisable()
    {
        var a = CowsinsHUDAdapter.Instance;
        if (a != null) a.OnWeaponChanged -= OnWeapon;
        StopAllCoroutines();
    }

    private IEnumerator Bind()
    {
        while (CowsinsHUDAdapter.Instance == null) yield return null;
        var a = CowsinsHUDAdapter.Instance;
        a.OnWeaponChanged += OnWeapon;
        OnWeapon(a.WeaponName, a.WeaponIcon);
    }

    private void OnWeapon(string n, Sprite icon)
    {
        if (nameText != null) nameText.text = string.IsNullOrEmpty(n) ? string.Empty : n.ToUpperInvariant();
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        if (group != null)
        {
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeIn());
        }
    }

    private IEnumerator FadeIn()
    {
        group.alpha = 0.25f;
        while (group.alpha < 0.999f)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, fadeSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        group.alpha = 1f;
    }
}
