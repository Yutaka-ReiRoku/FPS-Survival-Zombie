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

    [Header("Behaviour")]
    public float showDelay = 1.5f;
    public bool freezeTimeOnGameOver = true;

    public bool IsGameOver => isGameOver;

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
            playerStats = FindObjectOfType<PlayerStats>();
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
        if (finalScoreText != null)
            finalScoreText.text = "Score : " + (ScoreManager.Instance != null ? ScoreManager.Instance.GetFinalScore() : 0);
        if (waveReachedText != null)
            waveReachedText.text = "Wave : " + (WaveManager.Instance != null ? WaveManager.Instance.currentWave : 0);
        if (killsText != null)
            killsText.text = "Kills : " + (ScoreManager.Instance != null ? ScoreManager.Instance.kills : 0);
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
