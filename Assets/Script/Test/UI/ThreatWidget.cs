using UnityEngine;
using UnityEngine.UIElements;

public class ThreatWidget : MonoBehaviour
{
    private VisualElement _fill;
    private Label _state;
    private Label _warn;
    private IVisualElementScheduledItem _warnSched;

    private Color _calm = new Color(0.31f, 0.878f, 0.541f, 1f);
    private Color _build = new Color(1f, 0.83f, 0.30f, 1f);
    private Color _attack = new Color(0.95f, 0.32f, 0.27f, 1f);
    private Color _recover = new Color(0.45f, 0.78f, 0.95f, 1f);

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;
        var root = uiDoc.rootVisualElement;
        if (root == null) return;

        _fill = root.Q<VisualElement>("ThreatFill");
        _state = root.Q<Label>("ThreatState");
        _warn = root.Q<Label>("ThreatWarn");

        var d = AIDirector.Instance;
        if (d != null)
        {
            d.OnThreatChanged += Refresh;
            Refresh();
        }

        _warnSched = _warn?.schedule.Execute(AnimateWarn).Every(50);
    }

    private void OnDisable()
    {
        _warnSched?.Pause();
        var d = AIDirector.Instance;
        if (d != null) d.OnThreatChanged -= Refresh;
        _fill = null;
        _state = null;
        _warn = null;
    }

    private void Refresh()
    {
        var d = AIDirector.Instance;
        if (d == null || _fill == null || _state == null || _warn == null) return;

        float target = Mathf.Clamp01(d.threatLevel / 100f);
        _fill.style.width = Length.Percent(target * 100f);

        Color c; string label;
        switch (d.currentState)
        {
            case AIDirector.DirectorState.Calm: c = _calm; label = "CALM"; break;
            case AIDirector.DirectorState.BuildUp: c = _build; label = "BUILD-UP"; break;
            case AIDirector.DirectorState.Attack: c = _attack; label = "ATTACK"; break;
            default: c = _recover; label = "RECOVERY"; break;
        }
        _fill.style.backgroundColor = c;
        if (_state.text != label) _state.text = label;
        _state.style.color = c;
    }

    private void AnimateWarn()
    {
        if (_warn == null) return;
        bool flank = AIDirector.Instance != null && AIDirector.Instance.ShouldPunishCamper();
        _warn.style.opacity = flank ? (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 8f)) : 0f;
    }
}
