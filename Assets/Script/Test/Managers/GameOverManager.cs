using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using cowsins;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("Player")]
    [Tooltip("Cowsins PlayerStats to listen to. Auto-found if left empty.")]
    public PlayerStats playerStats;

    [Header("UIDocument")]
    [Tooltip("UIDocument containing GameOverPanel. Auto-found if left empty.")]
    public UIDocument uiDocument;

    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Behaviour")]
    public float showDelay = 1.5f;
    public bool freezeTimeOnGameOver = true;

    public bool IsGameOver => isGameOver;

    public event System.Action OnPlayerDied;

    private VisualElement _gameOverPanel;
    private VisualElement _card;
    private Label _finalScoreText;
    private Label _waveReachedText;
    private Label _killsText;
    private Label _bestScoreText;
    private Button _restartButton;
    private Button _mainMenuButton;
    private Button _quitButton;

    private bool isGameOver;
    private bool subscribed;
    private bool _uiReady;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable() { TrySubscribe(); }

    private void Start()
    {
        TrySubscribe();
        SetupUI();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void SetupUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            var canvasGo = GameObject.Find("GameUICanvas");
            if (canvasGo != null) uiDocument = canvasGo.GetComponent<UIDocument>();
        }
        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            Debug.LogError("[GameOverManager] No UIDocument found! GameOverPanel will not function.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        _gameOverPanel = root.Q("GameOverPanel");
        if (_gameOverPanel == null)
        {
            Debug.LogError("[GameOverManager] GameOverPanel not found in UXML!");
            return;
        }
        _card = _gameOverPanel.Q("Card");
        _finalScoreText = _gameOverPanel.Q<Label>("FinalScoreText");
        _waveReachedText = _gameOverPanel.Q<Label>("WaveReachedText");
        _killsText = _gameOverPanel.Q<Label>("KillsText");
        _bestScoreText = _gameOverPanel.Q<Label>("BestScoreText");
        _restartButton = _gameOverPanel.Q<Button>("RestartButton");
        _mainMenuButton = _gameOverPanel.Q<Button>("MainMenuButton");
        _quitButton = _gameOverPanel.Q<Button>("QuitButton");

        if (_gameOverPanel != null)
            _gameOverPanel.style.display = DisplayStyle.None;

        if (_restartButton != null)
            _restartButton.clicked += RestartGame;
        if (_quitButton != null)
            _quitButton.clicked += QuitGame;
        if (_mainMenuButton != null)
            _mainMenuButton.clicked += GoToMainMenu;

        _uiReady = true;
    }

    private void Unsubscribe()
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
        if (!_uiReady)
        {
            Debug.LogWarning("[GameOverManager] UI not ready, cannot show GameOverPanel.");
            return;
        }

        var sm = StoryManager.Instance;
        bool isStoryMode = sm != null;

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetFinalScore() : 0;
        int wave = WaveManager.Instance != null ? WaveManager.Instance.currentWave : 0;
        int kills = ScoreManager.Instance != null ? ScoreManager.Instance.kills : 0;

        if (isStoryMode)
        {
            int chapter = sm.CurrentChapter;
            int questsDone = sm.TotalQuestsCompleted;
            int journals = CollectibleManager.Instance != null ? CollectibleManager.Instance.Count : 0;
            int journalsTotal = CollectibleManager.Instance != null ? CollectibleManager.Instance.Total : 0;

            if (_finalScoreText != null)
                _finalScoreText.text = "Chương : " + chapter;
            if (_waveReachedText != null)
                _waveReachedText.text = "Nhiệm vụ : " + questsDone;
            if (_killsText != null)
                _killsText.text = "Nhật ký : " + journals + (journalsTotal > 0 ? " / " + journalsTotal : "");
            if (_bestScoreText != null)
                _bestScoreText.text = sm.StoryComplete ? "Cốt truyện hoàn thành" : "Tiến độ: Chương " + chapter;
        }
        else
        {
            int bestScore = PlayerPrefs.GetInt("BestScore", 0);
            int bestWave = PlayerPrefs.GetInt("BestWave", 0);
            if (finalScore > bestScore) { bestScore = finalScore; PlayerPrefs.SetInt("BestScore", bestScore); }
            if (wave > bestWave) { bestWave = wave; PlayerPrefs.SetInt("BestWave", bestWave); }
            PlayerPrefs.Save();

            if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn)
                PlayFabManager.Instance.SaveAllToCloud();

            if (_finalScoreText != null)
                _finalScoreText.text = "Score : " + finalScore;
            if (_waveReachedText != null)
                _waveReachedText.text = "Wave : " + wave;
            if (_killsText != null)
                _killsText.text = "Kills : " + kills;
            if (_bestScoreText != null)
                _bestScoreText.text = "Best : " + bestScore + "  (Wave " + bestWave + ")";
        }

        if (_gameOverPanel != null)
        {
            _gameOverPanel.style.display = DisplayStyle.Flex;
        }

        if (_card != null)
        {
            _card.AddToClassList("visible");
        }

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        if (freezeTimeOnGameOver)
            Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (AchievementManager.Instance != null)
            AchievementManager.Instance.ResetProgress();

        var sm = StoryManager.Instance;
        if (sm != null && SaveRoom.LastCheckpoint.HasValue)
        {
            isGameOver = false;

            if (AIDirector.Instance != null)
                AIDirector.Instance.FlushActiveZombies();

            var spawners = FindObjectsByType<Spawm>(FindObjectsSortMode.None);
            foreach (var spawner in spawners)
            {
                if (spawner != null)
                    spawner.FlushSpawner();
            }

            if (SpecialEnemyDirector.Instance != null)
                SpecialEnemyDirector.Instance.FlushSpecialEnemies();

            if (_gameOverPanel != null)
                _gameOverPanel.style.display = DisplayStyle.None;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            var ps = FindAnyObjectByType<PlayerStats>();
            if (ps != null)
            {
                ps.Respawn(SaveRoom.LastCheckpoint.Value);
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
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

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
