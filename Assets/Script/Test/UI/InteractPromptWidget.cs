using UnityEngine;
using UnityEngine.UIElements;

public class InteractPromptWidget : MonoBehaviour
{
    private CowsinsHUDAdapter _adapter;
    private VisualElement _root;
    private VisualElement _panel;
    private Label _label;
    private VisualElement _progressBar;
    private VisualElement _progressFill;
    private float _forbidTimer;
    private bool _bound;

    private static readonly Color PanelColor = new(0.137f, 0.165f, 0.2f, 0.94f);
    private static readonly Color ForbiddenColor = new(0.66f, 0.09f, 0.13f, 0.95f);

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;
        _root = doc.rootVisualElement.Q("InteractPrompt");
        if (_root == null) return;

        Build();
        TryBind();
    }

    private void Build()
    {
        if (_root.childCount > 0) return;

        _panel = new VisualElement();
        _panel.AddToClassList("interact-panel");
        _root.Add(_panel);

        _label = new Label();
        _label.AddToClassList("interact-label");
        _panel.Add(_label);

        _progressBar = new VisualElement();
        _progressBar.AddToClassList("interact-progress");
        _panel.Add(_progressBar);

        _progressFill = new VisualElement();
        _progressFill.AddToClassList("interact-progress-fill");
        _progressFill.usageHints = UsageHints.DynamicTransform;
        _progressBar.Add(_progressFill);
    }

    private void TryBind()
    {
        _adapter = CowsinsHUDAdapter.Instance;
        if (_adapter == null) return;

        _bound = true;
        _adapter.OnInteractPrompt += HandlePrompt;
        _adapter.OnInteractForbidden += HandleForbidden;
        _adapter.OnInteractProgress += HandleProgress;
    }

    private void OnDisable()
    {
        var a = _adapter ?? CowsinsHUDAdapter.Instance;
        if (a != null && _bound)
        {
            a.OnInteractPrompt -= HandlePrompt;
            a.OnInteractForbidden -= HandleForbidden;
            a.OnInteractProgress -= HandleProgress;
        }
        _adapter = null;
        _bound = false;
    }

    private void HandlePrompt(bool visible, string text)
    {
        if (visible)
        {
            _label.text = text;
            _label.RemoveFromClassList("interact-label--forbidden");
            _panel.style.backgroundColor = PanelColor;
            SetProgress(0f);
            _root.style.display = DisplayStyle.Flex;
        }
        else if (_forbidTimer <= 0f)
        {
            _root.style.display = DisplayStyle.None;
        }
    }

    private void HandleForbidden()
    {
        _label.text = "Cannot interact";
        _label.AddToClassList("interact-label--forbidden");
        _panel.style.backgroundColor = ForbiddenColor;
        SetProgress(0f);
        _root.style.display = DisplayStyle.Flex;
        _forbidTimer = 0.6f;
    }

    private void HandleProgress(float v)
    {
        if (v > 0f && _root.style.display == DisplayStyle.None)
            _root.style.display = DisplayStyle.Flex;
        SetProgress(v);
    }

    private void SetProgress(float v)
    {
        float pct = Mathf.Clamp01(v);
        _progressFill.style.width = Length.Percent(pct * 100f);
        _progressBar.style.display = pct > 0.001f ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void Update()
    {
        if (!_bound)
        {
            _adapter = CowsinsHUDAdapter.Instance;
            if (_adapter != null)
            {
                _bound = true;
                _adapter.OnInteractPrompt += HandlePrompt;
                _adapter.OnInteractForbidden += HandleForbidden;
                _adapter.OnInteractProgress += HandleProgress;
            }
        }

        if (_forbidTimer > 0f)
        {
            _forbidTimer -= Time.unscaledDeltaTime;
            if (_forbidTimer <= 0f)
            {
                _label.RemoveFromClassList("interact-label--forbidden");
                _panel.style.backgroundColor = PanelColor;
                _root.style.display = DisplayStyle.None;
            }
        }
    }
}
