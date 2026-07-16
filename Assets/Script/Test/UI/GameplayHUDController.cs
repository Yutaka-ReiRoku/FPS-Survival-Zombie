using System.Collections;
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
        // Freeze timescale immediately at Awake to prevent game logic / physics before transition ends
        Time.timeScale = 0f;

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

        _waveLabel = _waveChip?.Q<Label>("WaveLabel");
        _killsLabel = _killsChip?.Q<Label>("KillsLabel");
        _scoreLabel = _scoreChip?.Q<Label>("ScoreLabel");
        _coinsLabel = _coinsChip?.Q<Label>("CoinsLabel");
        _critsLabel = _critsChip?.Q<Label>("HeadshotsLabel");
        _timerLabel = _timerChip?.Q<Label>("TimerLabel");
        _collectiblesLabel = _collectiblesChip?.Q<Label>("CollectiblesLabel");

        if (CowsinsHUDAdapter.Instance != null)
            CowsinsHUDAdapter.Instance.OnCoinsChanged += OnCoinsChanged;
    }

    private void Start()
    {
        StartCoroutine(StartFadeOut());
    }

    private IEnumerator StartFadeOut()
    {
        if (_doc == null) yield break;
        var root = _doc.rootVisualElement;
        var overlay = root?.Q("BlackOverlay");
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.Flex;
            overlay.RemoveFromClassList("fade-out");
        }

        yield return null;

        if (overlay != null)
        {
            overlay.AddToClassList("fade-out");
        }

        // Wait 3.0 seconds in REALTIME because Time.timeScale is 0!
        yield return new WaitForSecondsRealtime(3.0f);

        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
        }

        // Unfreeze timescale to resume gameplay
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnKillsChanged += OnKillsChanged;
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
            ScoreManager.Instance.OnCritsChanged += OnCritsChanged;
        }
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
            WaveManager.Instance.OnWaveCompleted += OnWaveCompleted;
        }
        StartCoroutine(TimerRoutine());
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnKillsChanged -= OnKillsChanged;
            ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
            ScoreManager.Instance.OnCritsChanged -= OnCritsChanged;
        }
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
            WaveManager.Instance.OnWaveCompleted -= OnWaveCompleted;
        }
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (CowsinsHUDAdapter.Instance != null)
            CowsinsHUDAdapter.Instance.OnCoinsChanged -= OnCoinsChanged;
    }

    private void ShowChip(VisualElement chip, bool show)
    {
        if (chip == null) return;
        if (show)
            chip.RemoveFromClassList("hud-chip-hidden");
        else
            chip.AddToClassList("hud-chip-hidden");
    }

    private void OnWaveStarted(int wave)
    {
        if (_waveChip == null || _waveLabel == null) return;
        ShowChip(_waveChip, true);
        var wm = WaveManager.Instance;
        if (wm == null) return;
        _lastWave = wm.currentWave;
        _lastKilled = wm.zombiesKilledThisWave;
        _lastToKill = wm.zombiesToKill;
        _waveLabel.text = $"Wave {wm.currentWave} \u00B7 {wm.zombiesKilledThisWave}/{wm.zombiesToKill}";
    }

    private void OnWaveCompleted(int wave)
    {
        if (_waveChip == null || _waveLabel == null) return;
        _waveLabel.text = $"Wave {wave} Complete!";
    }

    private void OnKillsChanged()
    {
        if (_killsChip == null || _killsLabel == null) return;
        var sm = ScoreManager.Instance;
        if (sm == null) return;
        ShowChip(_killsChip, sm.kills > 0);
        if (sm.kills != _lastKills)
        {
            _lastKills = sm.kills;
            _killsLabel.text = "Kills : " + sm.kills;
        }
    }

    private void OnScoreChanged()
    {
        if (_scoreChip == null || _scoreLabel == null) return;
        var sm = ScoreManager.Instance;
        if (sm == null) return;
        ShowChip(_scoreChip, sm.score > 0);
        if (sm.score != _lastScore)
        {
            _lastScore = sm.score;
            _scoreLabel.text = "Score : " + sm.score;
        }
    }

    private void OnCritsChanged()
    {
        if (_critsChip == null || _critsLabel == null) return;
        var sm = ScoreManager.Instance;
        if (sm == null) return;
        ShowChip(_critsChip, sm.crits > 0);
        if (sm.crits != _lastCrits)
        {
            _lastCrits = sm.crits;
            _critsLabel.text = "Crits : " + sm.crits;
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

    private IEnumerator TimerRoutine()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            yield return wait;
            UpdateTimer();
            UpdateCollectibles();
        }
    }

    private void UpdateTimer()
    {
        var sm = ScoreManager.Instance;
        if (sm == null || _timerChip == null || _timerLabel == null) return;

        int sec = Mathf.FloorToInt(sm.GetSurvivalTime());
        ShowChip(_timerChip, sec > 0);
        if (sec > 0 && sec != _lastSec)
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
        ShowChip(_collectiblesChip, c > 0);
        if (c > 0 && c != _lastCollectibles)
        {
            _lastCollectibles = c;
            _collectiblesLabel.text = $"Journals : {c}/{cm.Total}";
        }
    }
}
