using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using cowsins;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("Player")]
    [Tooltip("Cowsins PlayerStats to listen to. Auto-found if left empty.")]
    public PlayerStats playerStats;

    [Header("UI")]
    [Tooltip("Root of the Game Over panel. Starts disabled.")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text waveReachedText;
    public TMP_Text killsText;
    public Button restartButton;
    public Button quitButton;
    public Button mainMenuButton;
    public TMP_Text bestScoreText;

    [Header("Extended Stats (optional — assign for full breakdown)")]
    public TMP_Text playTimeText;
    public TMP_Text distanceText;
    public TMP_Text totalDamageText;
    public TMP_Text zombieKillsText;
    public TMP_Text boomerKillsText;
    public TMP_Text tankKillsText;
    public TMP_Text critsText;
    public TMP_Text accuracyText;
    public TMP_Text coinsText;
    public TMP_Text healthLostText;
    public TMP_Text healthHealedText;
    public TMP_Text reloadsText;
    public TMP_Text journalsText;
    public TMP_Text deathsText;

    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Behaviour")]
    public float showDelay = 1.5f;
    public bool freezeTimeOnGameOver = true;

    public bool IsGameOver => isGameOver;

    /// <summary>Fired when the player dies (before the game over panel shows). Other systems (e.g. WaveQuestInteractable) use this to abort and reset.</summary>
    public event System.Action OnPlayerDied;

    private bool isGameOver;
    private bool subscribed;

    private void Awake()
    {
        Instance = this;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    private void OnEnable() { TrySubscribe(); }

    private void Start() { TrySubscribe(); }

    private void OnDisable()
    {
        if (subscribed && playerStats != null)
        {
            playerStats.RemoveOnDieListener(OnPlayerDeath);
            subscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed)
            return;
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.AddOnDieListener(OnPlayerDeath);
            subscribed = true;
        }
    }

    public void OnPlayerDeath()
    {
        if (isGameOver)
            return;
        isGameOver = true;
        OnPlayerDied?.Invoke();
        StartCoroutine(ShowAfterDelay());
    }

    private System.Collections.IEnumerator ShowAfterDelay()
    {
        if (showDelay > 0f)
            yield return new WaitForSecondsRealtime(showDelay);
        ShowGameOver();
    }

    private void ShowGameOver()
    {
        var sm = StoryManager.Instance;
        bool isStoryMode = sm != null;

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetFinalScore() : 0;
        int wave = WaveManager.Instance != null ? WaveManager.Instance.currentWave : 0;
        int kills = ScoreManager.Instance != null ? ScoreManager.Instance.kills : 0;

        if (isStoryMode)
        {
            // Story mode: show chapter/quest/journal stats instead of deathmatch score.
            int chapter = sm.CurrentChapter;
            int questsDone = sm.TotalQuestsCompleted;
            int journals = CollectibleManager.Instance != null ? CollectibleManager.Instance.Count : 0;
            int journalsTotal = CollectibleManager.Instance != null ? CollectibleManager.Instance.Total : 0;

            if (finalScoreText != null)
                finalScoreText.text = "Chương : " + chapter;
            if (waveReachedText != null)
                waveReachedText.text = "Nhiệm vụ : " + questsDone;
            if (killsText != null)
                killsText.text = "Nhật ký : " + journals + (journalsTotal > 0 ? " / " + journalsTotal : "");
            if (bestScoreText != null)
                bestScoreText.text = sm.StoryComplete ? "Cốt truyện hoàn thành" : "Tiến độ: Chương " + chapter;
        }
        else
        {
            // Deathmatch mode: original score/wave/kills/best display.
            // High score persistence (best score + best wave).
            int bestScore = PlayerPrefs.GetInt("BestScore", 0);
            int bestWave = PlayerPrefs.GetInt("BestWave", 0);
            if (finalScore > bestScore) { bestScore = finalScore; PlayerPrefs.SetInt("BestScore", bestScore); }
            if (wave > bestWave) { bestWave = wave; PlayerPrefs.SetInt("BestWave", bestWave); }
            PlayerPrefs.Save();

            // Upload to PlayFab cloud if logged in
            if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn)
                PlayFabManager.Instance.SaveAllToCloud();

            if (finalScoreText != null)
                finalScoreText.text = "Score : " + finalScore;
            if (waveReachedText != null)
                waveReachedText.text = "Wave : " + wave;
            if (killsText != null)
                killsText.text = "Kills : " + kills;
            if (bestScoreText != null)
                bestScoreText.text = "Best : " + bestScore + "  (Wave " + bestWave + ")";
        }

        // Extended stats breakdown (only if the text fields are assigned).
        var tracker = PlayerStatsTracker.Instance;
        if (tracker != null)
        {
            if (playTimeText != null)
                playTimeText.text = "Time : " + PlayerStatsTracker.FormatTime(tracker.GetPlayTime());
            if (distanceText != null)
                distanceText.text = "Distance : " + PlayerStatsTracker.FormatDistance(tracker.GetDistanceMoved());
            if (totalDamageText != null)
                totalDamageText.text = "Damage : " + PlayerStatsTracker.FormatDamage(tracker.totalDamageDealt);
            if (zombieKillsText != null)
                zombieKillsText.text = "Zombies : " + tracker.zombieKills;
            if (boomerKillsText != null)
                boomerKillsText.text = "Boomers : " + tracker.boomerKills;
            if (tankKillsText != null)
                tankKillsText.text = "Tanks : " + tracker.tankKills;
            if (critsText != null)
                critsText.text = "Crits : " + tracker.GetCrits();
            if (accuracyText != null)
                accuracyText.text = "Accuracy : " + tracker.GetAccuracy().ToString("F1") + "%";
            if (coinsText != null)
                coinsText.text = "Coins : " + tracker.GetCoins();
            if (healthLostText != null)
                healthLostText.text = "Health Lost : " + PlayerStatsTracker.FormatHealth(tracker.GetHealthLost());
            if (healthHealedText != null)
                healthHealedText.text = "Health Healed : " + PlayerStatsTracker.FormatHealth(tracker.GetHealthHealed());
            if (reloadsText != null)
                reloadsText.text = "Reloads : " + tracker.GetReloadCount();
            if (journalsText != null)
            {
                int jCol = tracker.GetJournalsCollected();
                int jTot = tracker.GetJournalsTotal();
                journalsText.text = jTot > 0 ? $"Journals : {jCol} / {jTot}" : "Journals : " + jCol;
            }
            if (deathsText != null)
                deathsText.text = "Deaths : " + tracker.GetDeathCount();
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (freezeTimeOnGameOver)
            Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        // Reset achievement progress so it only tracks the new playthrough.
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.ResetProgress();

        // Story mode: respawn at the last save room checkpoint instead of
        // reloading the scene. This preserves quest progress, killed zombies,
        // collected journals, etc. If no checkpoint was ever set (player died
        // before reaching any save room), fall back to a full scene reload.
        var sm = StoryManager.Instance;
        if (sm != null && SaveRoom.LastCheckpoint.HasValue)
        {
            isGameOver = false;

            // Flush all active zombies and special enemies
            if (AIDirector.Instance != null)
            {
                AIDirector.Instance.FlushActiveZombies();
            }

            var spawners = FindObjectsByType<Spawm>(FindObjectsSortMode.None);
            foreach (var spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.FlushSpawner();
                }
            }

            if (SpecialEnemyDirector.Instance != null)
            {
                SpecialEnemyDirector.Instance.FlushSpecialEnemies();
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.Respawn(SaveRoom.LastCheckpoint.Value);

                // Respawn() calls TeleportPlayer which ForceChangeState(Default),
                // but PlayerDeadState.EnterState() called LoseControl() — that is
                // NOT undone by the state switch. We must explicitly restore control
                // so the player can move/look again.
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                {
                    var control = playerGO.GetComponentInChildren<PlayerControl>();
                    if (control != null) control.GrantControl();
                }

                Debug.Log($"[GameOverManager] Respawned at save room checkpoint {SaveRoom.LastCheckpoint.Value}.");
            }
            else
            {
                // No PlayerStats found — fall back to scene reload.
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        // Deathmatch or no checkpoint: full scene reload.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }


    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
