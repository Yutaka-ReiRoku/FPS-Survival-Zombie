using UnityEngine;
using UnityEngine.UIElements;

public class GiftBoxManager : MonoBehaviour
{
    public static GiftBoxManager Instance { get; private set; }

    private UIDocument _doc;
    private VisualElement _panel;
    private Label _coinLabel;
    private Label _healthLabel;
    private Label _rewardList;
    private Button _closeButton;
    private bool _ready;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        SetupUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void UnsubscribeEvents()
    {
        if (_closeButton != null)
            _closeButton.clicked -= Close;
    }

    private void SetupUI()
    {
        if (_doc == null)
            _doc = GetComponent<UIDocument>();
        if (_doc == null)
        {
            var canvasGo = GameObject.Find("GameUICanvas");
            if (canvasGo != null) _doc = canvasGo.GetComponent<UIDocument>();
        }
        if (_doc == null || _doc.rootVisualElement == null) return;

        var root = _doc.rootVisualElement;
        _panel = root.Q("GiftBoxPanel");
        if (_panel == null) return;

        _coinLabel = _panel.Q<Label>("CoinReward");
        _healthLabel = _panel.Q<Label>("HealthReward");
        _rewardList = _panel.Q<Label>("RewardList");
        _closeButton = _panel.Q<Button>("CloseButton");

        _panel.style.display = DisplayStyle.None;
        if (_closeButton != null)
            _closeButton.clicked += Close;
        _ready = true;
    }

    public void Show(string[] rewards, int coins, int health)
    {
        if (!_ready) return;

        if (_coinLabel != null)
            _coinLabel.text = $"+{coins}";
        if (_healthLabel != null)
            _healthLabel.text = $"+{health}";
        if (_rewardList != null && rewards != null)
            _rewardList.text = string.Join("\n", rewards);

        _panel.style.display = DisplayStyle.Flex;
    }

    public void Close()
    {
        if (_ready)
            _panel.style.display = DisplayStyle.None;
    }
}
