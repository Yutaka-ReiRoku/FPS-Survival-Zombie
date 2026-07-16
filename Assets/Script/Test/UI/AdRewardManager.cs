using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using cowsins;

public class AdRewardManager : MonoBehaviour
{
    public static AdRewardManager Instance { get; private set; }

    private UIDocument _doc;
    private VisualElement _panel;
    private Label _titleLabel;
    private Label _timerLabel;
    private Label _rewardLabel;
    private VisualElement _adContainer;
    private Button _watchButton;
    private Button _closeButton;
    private bool _ready;
    private Transform _currentPlayer;
    private PlayerControl _playerControl;
    private float _previousTimeScale = 1f;

    [Header("Ad Settings")]
    [Tooltip("Thời lượng quảng cáo giả lập (giây).")]
    public float adDuration = 5f;

    [Header("Reward Amounts")]
    public int coinAmount = 150;
    public float expAmount = 75f;
    public int ammoMagazines = 2;
    public int healthAmount = 40;

    private bool _isAdPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable() { SetupUI(); }

    private void OnDisable()
    {
        if (_watchButton != null) _watchButton.clicked -= StartAd;
        if (_closeButton != null) _closeButton.clicked -= ClosePanel;
        StopAllCoroutines();
    }

    private void SetupUI()
    {
        if (_doc == null) _doc = GetComponent<UIDocument>();
        if (_doc == null)
        {
            var go = GameObject.Find("GameUICanvas");
            if (go != null) _doc = go.GetComponent<UIDocument>();
        }
        if (_doc == null || _doc.rootVisualElement == null) return;

        var root = _doc.rootVisualElement;
        _panel = root.Q("AdRewardPanel");
        if (_panel == null) return;

        _titleLabel = _panel.Q<Label>("AdTitle");
        _timerLabel = _panel.Q<Label>("AdTimer");
        _rewardLabel = _panel.Q<Label>("AdRewardText");
        _adContainer = _panel.Q("AdContent");
        _watchButton = _panel.Q<Button>("WatchAdButton");
        _closeButton = _panel.Q<Button>("AdCloseButton");

        _panel.style.display = DisplayStyle.None;

        if (_watchButton != null) _watchButton.clicked += StartAd;
        if (_closeButton != null) _closeButton.clicked += ClosePanel;

        _ready = true;
    }

    public void ShowAd(Transform player)
    {
        if (!_ready) return;
        _currentPlayer = player;
        _playerControl = player != null ? player.GetComponentInChildren<PlayerControl>() : null;

        PauseGame();

        _panel.style.display = DisplayStyle.Flex;
        if (_titleLabel != null) _titleLabel.text = "QUẢNG CÁO";
        if (_timerLabel != null) _timerLabel.text = "Nhấn XEM để nhận thưởng";
        if (_rewardLabel != null) _rewardLabel.text = "";
        if (_watchButton != null) _watchButton.style.display = DisplayStyle.Flex;
        if (_closeButton != null) _closeButton.style.display = DisplayStyle.None;
        if (_adContainer != null) _adContainer.RemoveFromClassList("ad-playing");
    }

    private void StartAd()
    {
        if (_isAdPlaying) return;
        if (_watchButton != null) _watchButton.style.display = DisplayStyle.None;
        if (_adContainer != null) _adContainer.AddToClassList("ad-playing");
        StartCoroutine(PlayAdCoroutine());
    }

    private IEnumerator PlayAdCoroutine()
    {
        _isAdPlaying = true;
        float timer = adDuration;

        while (timer > 0)
        {
            if (_timerLabel != null)
                _timerLabel.text = $"Quảng cáo kết thúc sau {timer:F0}s";
            timer -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (_timerLabel != null) _timerLabel.text = "Hoàn tất!";
        GrantRandomReward();
        if (_closeButton != null) _closeButton.style.display = DisplayStyle.Flex;
        _isAdPlaying = false;
    }

    private void GrantRandomReward()
    {
        string[] rewards = { "Coin", "Exp", "Ammo", "Health" };
        string selected = rewards[Random.Range(0, rewards.Length)];

        switch (selected)
        {
            case "Coin":
                if (CoinManager.Instance != null)
                {
                    CoinManager.Instance.AddCoins(coinAmount, false);
                    if (_rewardLabel != null)
                        _rewardLabel.text = $"+{coinAmount} Coins!";
                }
                break;

            case "Exp":
                if (ExperienceManager.Instance != null)
                {
                    ExperienceManager.Instance.AddExperience(expAmount);
                    if (_rewardLabel != null)
                        _rewardLabel.text = $"+{expAmount} EXP!";
                }
                break;

            case "Ammo":
                if (_currentPlayer != null)
                {
                    var wRef = _currentPlayer.GetComponent<IWeaponReferenceProvider>();
                    if (wRef != null && wRef.Id != null)
                    {
                        int amount = Mathf.Max(10, wRef.Id.magazineSize * ammoMagazines);
                        wRef.Id.totalBullets += amount;
                        var wEvents = _currentPlayer.GetComponent<IWeaponEventsProvider>();
                        if (wEvents != null && wEvents.Events != null)
                            wEvents.Events.OnAmmoChanged?.Invoke(false);
                        if (_rewardLabel != null)
                            _rewardLabel.text = $"+{amount} đạn!";
                    }
                }
                break;

            case "Health":
                if (_currentPlayer != null)
                {
                    var stats = _currentPlayer.GetComponent<PlayerStats>();
                    if (stats != null)
                    {
                        stats.Heal(healthAmount);
                        if (_rewardLabel != null)
                            _rewardLabel.text = $"+{healthAmount} HP!";
                    }
                }
                break;
        }
    }

    private void PauseGame()
    {
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (_playerControl != null)
            _playerControl.LoseControl();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void ResumeGame()
    {
        Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
        if (_playerControl != null)
            _playerControl.GrantControl();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    private void ClosePanel()
    {
        if (_panel != null)
            _panel.style.display = DisplayStyle.None;
        StopAllCoroutines();
        _isAdPlaying = false;
        ResumeGame();
        _currentPlayer = null;
        _playerControl = null;
    }
}
