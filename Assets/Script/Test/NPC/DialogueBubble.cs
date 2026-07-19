using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Screen-space dialogue bubble for the companion NPC.
/// Uses the same approach as SimpleNotification — a screen-space UIDocument
/// overlay that shows the dialogue line + Y/N prompt in the center of the screen.
///
/// Two modes:
///   Speech — shows a single line, auto-hides after duration.
///   Choice — shows a line + "Nhấn Y để đồng ý / N để từ chối", waits for Y/N input,
///            fires OnChoiceMade(bool accepted).
/// </summary>
[RequireComponent(typeof(CompanionAI))]
public class DialogueBubble : MonoBehaviour
{
    [Header("Timing")]
    public float fadeIn = 0.3f;
    public float speechHoldDuration = 4f;
    public float fadeOut = 0.8f;

    [Header("Visuals")]
    public Color textColor = new Color(0.95f, 0.92f, 0.8f, 1f);
    public Color choiceColor = new Color(1f, 0.85f, 0.3f, 1f);
    public Color scrimColor = new Color(0f, 0f, 0f, 0.6f);
    public float lineFontSize = 26f;
    public float choiceFontSize = 22f;

    private GameObject _panelGO;
    private UIDocument _doc;
    private VisualElement _root;
    private VisualElement _scrim;
    private Label _lineLabel;
    private Label _choiceLabel;
    private Coroutine _routine;
    private bool _choiceActive;
    private System.Action<bool> _choiceCallback;

    public bool IsVisible => _panelGO != null && _root != null && _root.resolvedStyle.opacity > 0f;
    public bool IsChoiceActive => _choiceActive;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        _panelGO = new GameObject("DialogueBubblePanel", typeof(UIDocument));
        _panelGO.transform.SetParent(transform, false);
        _doc = _panelGO.GetComponent<UIDocument>();
        _doc.sortingOrder = 450; // Below SimpleNotification (500), above HUD

        // Borrow panel settings from an existing screen-space UIDocument
        // (same approach as SimpleNotification).
        UIDocument hudDoc = null;
        var allDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (var d in allDocs)
        {
            if (d != _doc && d.panelSettings != null)
            {
                hudDoc = d;
                break;
            }
        }
        if (hudDoc != null)
            _doc.panelSettings = hudDoc.panelSettings;
        else
            Debug.LogWarning("[DialogueBubble] No screen-space UIDocument found to borrow panel settings.");

        // Build visual tree programmatically.
        _root = new VisualElement();
        _root.name = "DialogueBubbleRoot";
        _root.style.position = Position.Absolute;
        _root.style.left = 0f;
        _root.style.top = 0f;
        _root.style.right = 0f;
        _root.style.bottom = 0f;
        _root.style.display = DisplayStyle.Flex;
        _root.style.alignItems = Align.Center;
        _root.style.justifyContent = Justify.Center;
        _root.style.opacity = 0f; // Hidden by default

        // Semi-transparent scrim behind the text.
        _scrim = new VisualElement();
        _scrim.name = "DialogueScrim";
        _scrim.style.backgroundColor = scrimColor;
        _scrim.style.borderTopLeftRadius = 12f;
        _scrim.style.borderTopRightRadius = 12f;
        _scrim.style.borderBottomLeftRadius = 12f;
        _scrim.style.borderBottomRightRadius = 12f;
        _scrim.style.paddingTop = 20f;
        _scrim.style.paddingBottom = 20f;
        _scrim.style.paddingLeft = 30f;
        _scrim.style.paddingRight = 30f;
        _scrim.style.marginLeft = 200f;
        _scrim.style.marginRight = 200f;
        _root.Add(_scrim);

        // Main dialogue line.
        _lineLabel = new Label();
        _lineLabel.name = "DialogueLine";
        _lineLabel.style.color = textColor;
        _lineLabel.style.fontSize = lineFontSize;
        _lineLabel.style.whiteSpace = WhiteSpace.Normal;
        _lineLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        _lineLabel.style.marginBottom = 12f;
        _scrim.Add(_lineLabel);

        // Y/N choice prompt.
        _choiceLabel = new Label();
        _choiceLabel.name = "DialogueChoice";
        _choiceLabel.style.color = choiceColor;
        _choiceLabel.style.fontSize = choiceFontSize;
        _choiceLabel.style.whiteSpace = WhiteSpace.Normal;
        _choiceLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        _choiceLabel.style.display = DisplayStyle.None;
        _scrim.Add(_choiceLabel);

        _doc.rootVisualElement.Add(_root);
    }

    private void Update()
    {
        if (!_choiceActive) return;

        if (Input.GetKeyDown(KeyCode.Y))
        {
            ResolveChoice(true);
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            ResolveChoice(false);
        }
    }

    // ---- Speech mode ----

    public void ShowSpeech(string line)
    {
        if (_lineLabel != null) _lineLabel.text = line;
        if (_choiceLabel != null) _choiceLabel.style.display = DisplayStyle.None;
        Show();
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(HideAfter(fadeIn + speechHoldDuration));
    }

    // ---- Choice mode ----

    public void ShowChoice(string line, System.Action<bool> onChoice)
    {
        if (_lineLabel != null) _lineLabel.text = line;
        if (_choiceLabel != null)
        {
            _choiceLabel.text = "Nhấn [Y] để đồng ý   |   Nhấn [N] để từ chối";
            _choiceLabel.style.display = DisplayStyle.Flex;
        }
        _choiceCallback = onChoice;
        _choiceActive = true;
        Show();
        if (_routine != null) StopCoroutine(_routine);
    }

    private void ResolveChoice(bool accepted)
    {
        _choiceActive = false;
        var cb = _choiceCallback;
        _choiceCallback = null;
        Hide();
        cb?.Invoke(accepted);
    }

    // ---- Show / Hide ----

    private void Show()
    {
        if (_root != null) _root.style.opacity = 1f;
    }

    private void Hide()
    {
        if (_root != null) _root.style.opacity = 0f;
    }

    private IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Hide();
        _routine = null;
    }

    private void OnDestroy()
    {
        if (_panelGO != null)
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.Clear();
            Destroy(_panelGO);
        }
    }
}
