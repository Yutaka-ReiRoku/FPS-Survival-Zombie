using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Custom interaction prompt on the unified HUD, fed by CowsinsHUDAdapter interact
/// events. Shows a centered prompt panel + label on "allowed", a hold-progress fill,
/// a brief red flash on "forbidden", and hides on disable/finish. The component stays
/// active (subscribed); only a child "Prompt" object toggles, so the adapter binding
/// is never lost. Engine-free (adapter only).
/// </summary>
public class InteractPromptWidget : MonoBehaviour
{
    private CowsinsHUDAdapter _adapter;
    private GameObject _prompt;
    private TMP_Text _label;
    private Image _panel;
    private RectTransform _progressFill;
    private GameObject _progressBar;
    private Color _panelColor = new Color(0.137f, 0.165f, 0.2f, 0.94f);
    private Color _forbidden = new Color(0.66f, 0.09f, 0.13f, 0.95f);
    private Color _accent = new Color(0.85f, 0.78f, 0.45f, 1f);
    private float _forbidTimer;

    private void Awake()
    {
        var th = UITheme.Active;
        if (th != null) { _panelColor = th.surfaceTop; _forbidden = th.dangerBottom; _accent = th.accent; }
        Build();
    }

    private void Build()
    {
        _prompt = NewChild("Prompt", transform).gameObject;
        var prt = (RectTransform)_prompt.transform;
        prt.anchorMin = new Vector2(0.5f, 0f); prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, 200f); prt.sizeDelta = new Vector2(540f, 64f);
        _panel = _prompt.AddComponent<Image>(); _panel.color = _panelColor; _panel.raycastTarget = false;

        var lbl = NewChild("Label", prt); Stretch(lbl, new Vector2(16f, 6f), new Vector2(-16f, -10f));
        _label = lbl.gameObject.AddComponent<TextMeshProUGUI>();
        _label.alignment = TextAlignmentOptions.Center; _label.fontSize = 26; _label.raycastTarget = false;
        PremiumUITheme.StyleLabel(_label);

        _progressBar = NewChild("ProgressBar", prt).gameObject;
        var pbrt = (RectTransform)_progressBar.transform;
        pbrt.anchorMin = new Vector2(0f, 0f); pbrt.anchorMax = new Vector2(1f, 0f); pbrt.pivot = new Vector2(0.5f, 0f);
        pbrt.sizeDelta = new Vector2(-8f, 5f); pbrt.anchoredPosition = new Vector2(0f, 4f);
        var pbBg = _progressBar.AddComponent<Image>(); pbBg.color = new Color(0f, 0f, 0f, 0.5f); pbBg.raycastTarget = false;
        _progressFill = NewChild("Fill", pbrt);
        _progressFill.anchorMin = Vector2.zero; _progressFill.anchorMax = new Vector2(0f, 1f);
        _progressFill.offsetMin = Vector2.zero; _progressFill.offsetMax = Vector2.zero; _progressFill.pivot = new Vector2(0f, 0.5f);
        var pfImg = _progressFill.gameObject.AddComponent<Image>(); pfImg.color = _accent; pfImg.raycastTarget = false;

        _prompt.SetActive(false);
    }

    private RectTransform NewChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    private void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = min; rt.offsetMax = max; }

    private void OnEnable() { StartCoroutine(Bind()); }

    private IEnumerator Bind()
    {
        float timeout = 12f;
        while (CowsinsHUDAdapter.Instance == null && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
        _adapter = CowsinsHUDAdapter.Instance;
        if (_adapter == null) yield break;
        _adapter.OnInteractPrompt += HandlePrompt;
        _adapter.OnInteractForbidden += HandleForbidden;
        _adapter.OnInteractProgress += HandleProgress;
    }

    private void OnDisable()
    {
        if (_adapter != null)
        {
            _adapter.OnInteractPrompt -= HandlePrompt;
            _adapter.OnInteractForbidden -= HandleForbidden;
            _adapter.OnInteractProgress -= HandleProgress;
        }
        StopAllCoroutines();
    }

    private void HandlePrompt(bool visible, string text)
    {
        if (visible)
        {
            if (_label != null) _label.text = text;
            if (_panel != null) _panel.color = _panelColor;
            SetProgress(0f);
            _prompt.SetActive(true);
        }
        else if (_forbidTimer <= 0f)
        {
            _prompt.SetActive(false);
        }
    }

    private void HandleForbidden()
    {
        if (_label != null) _label.text = "Cannot interact";
        if (_panel != null) _panel.color = _forbidden;
        SetProgress(0f);
        _prompt.SetActive(true);
        _forbidTimer = 0.6f;
    }

    private void HandleProgress(float v)
    {
        if (v > 0f && !_prompt.activeSelf) _prompt.SetActive(true);
        SetProgress(v);
    }

    private void SetProgress(float v)
    {
        if (_progressFill != null) _progressFill.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
        if (_progressBar != null) _progressBar.SetActive(v > 0.001f);
    }

    private void Update()
    {
        if (_forbidTimer > 0f)
        {
            _forbidTimer -= Time.unscaledDeltaTime;
            if (_forbidTimer <= 0f)
            {
                if (_panel != null) _panel.color = _panelColor;
                _prompt.SetActive(false);
            }
        }
    }
}
