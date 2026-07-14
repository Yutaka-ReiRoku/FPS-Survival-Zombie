using UnityEngine;
using UnityEngine.UIElements;

public class GameplayHUDController : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _hudChips;
    private Label _waveLabel, _killsLabel, _scoreLabel, _coinsLabel, _critsLabel, _timerLabel, _collectiblesLabel;
    private VisualElement _waveChip, _killsChip, _scoreChip, _coinsChip, _critsChip, _timerChip, _collectiblesChip;

    private int _lastWave = -1, _lastKilled = -1, _lastToKill = -1;
    private int _lastKills = -1, _lastScore = -1, _lastCrits = -1, _lastSec = -1;
    private int _lastCollectibles = -1;
    private int _lastCoins = -1;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
        if (_doc == null) return;

        _hudChips = _doc.rootVisualElement.Q("HUDChips");
        if (_hudChips == null) return;

        _waveChip = _hudChips.Q("WaveChip");
        _killsChip = _hudChips.Q("KillsChip");
        _scoreChip = _hudChips.Q("ScoreChip");
        _coinsChip = _hudChips.Q("CoinsChip");
        _critsChip = _hudChips.Q("HeadshotsChip");
        _timerChip = _hudChips.Q("TimerChip");
        _collectiblesChip = _hudChips.Q("CollectiblesChip");

        _waveLabel = _waveChip?.Q<Label>("ChipLabel");
        _killsLabel = _killsChip?.Q<Label>("ChipLabel");
        _scoreLabel = _scoreChip?.Q<Label>("ChipLabel");
        _coinsLabel = _coinsChip?.Q<Label>("ChipLabel");
        _critsLabel = _critsChip?.Q<Label>("ChipLabel");
        _timerLabel = _timerChip?.Q<Label>("ChipLabel");
        _collectiblesLabel = _collectiblesChip?.Q<Label>("ChipLabel");

        if (CowsinsHUDAdapter.Instance != null)
            CowsinsHUDAdapter.Instance.OnCoinsChanged += OnCoinsChanged;
    }

    private void OnDestroy()
    {
        if (CowsinsHUDAdapter.Instance != null)
            CowsinsHUDAdapter.Instance.OnCoinsChanged -= OnCoinsChanged;
    }

    private void Update()
    {
        UpdateWave();
        UpdateScore();
        UpdateTimer();
        UpdateCollectibles();
    }

    private void ShowChip(VisualElement chip, bool show)
    {
        if (chip == null) return;
        if (show)
            chip.RemoveFromClassList("chip-hidden");
        else
            chip.AddToClassList("chip-hidden");
    }

    private void UpdateWave()
    {
        var wm = WaveManager.Instance;
        if (wm == null || _waveChip == null || _waveLabel == null) return;

        bool hasWave = wm.currentWave > 0;
        ShowChip(_waveChip, hasWave);

        if (!hasWave) return;

        if (wm.currentWave != _lastWave || wm.zombiesKilledThisWave != _lastKilled || wm.zombiesToKill != _lastToKill)
        {
            _lastWave = wm.currentWave;
            _lastKilled = wm.zombiesKilledThisWave;
            _lastToKill = wm.zombiesToKill;
            _waveLabel.text = $"Wave {wm.currentWave} \u00B7 {wm.zombiesKilledThisWave}/{wm.zombiesToKill}";
        }
    }

    private void UpdateScore()
    {
        var sm = ScoreManager.Instance;
        if (sm == null) return;

        if (_killsChip != null && _killsLabel != null)
        {
            bool hasKills = sm.kills > 0;
            ShowChip(_killsChip, hasKills);
            if (hasKills && sm.kills != _lastKills)
            {
                _lastKills = sm.kills;
                _killsLabel.text = "Kills : " + sm.kills;
            }
        }

        if (_scoreChip != null && _scoreLabel != null)
        {
            bool hasScore = sm.score > 0;
            ShowChip(_scoreChip, hasScore);
            if (hasScore && sm.score != _lastScore)
            {
                _lastScore = sm.score;
                _scoreLabel.text = "Score : " + sm.score;
            }
        }

        if (_coinsChip != null && _coinsLabel != null)
        {
            int coins = CowsinsHUDAdapter.Instance != null ? CowsinsHUDAdapter.Instance.Coins : 0;
            bool hasCoins = coins > 0;
            ShowChip(_coinsChip, hasCoins);
        }

        if (_critsChip != null && _critsLabel != null)
        {
            bool hasCrits = sm.crits > 0;
            ShowChip(_critsChip, hasCrits);
            if (hasCrits && sm.crits != _lastCrits)
            {
                _lastCrits = sm.crits;
                _critsLabel.text = "Crits : " + sm.crits;
            }
        }
    }

    private void OnCoinsChanged(int coins)
    {
        if (_coinsLabel == null || _coinsChip == null) return;

        ShowChip(_coinsChip, coins > 0);
        if (coins != _lastCoins)
        {
            _lastCoins = coins;
            _coinsLabel.text = "Coins : " + coins;
        }
    }

    private void UpdateTimer()
    {
        var sm = ScoreManager.Instance;
        if (sm == null || _timerChip == null || _timerLabel == null) return;

        int sec = Mathf.FloorToInt(sm.GetSurvivalTime());
        bool hasTime = sec > 0;
        ShowChip(_timerChip, hasTime);

        if (!hasTime) return;

        if (sec != _lastSec)
        {
            _lastSec = sec;
            int m = sec / 60, s = sec % 60;
            _timerLabel.text = $"{m:00}:{s:00}";
        }
    }

    private void UpdateCollectibles()
    {
        var cm = CollectibleManager.Instance;
        if (cm == null || _collectiblesChip == null || _collectiblesLabel == null) return;

        int c = cm.Count;
        bool hasCollectibles = c > 0;
        ShowChip(_collectiblesChip, hasCollectibles);

        if (!hasCollectibles) return;

        if (c != _lastCollectibles)
        {
            _lastCollectibles = c;
            _collectiblesLabel.text = $"Journals : {c}/{cm.Total}";
        }
    }
}
