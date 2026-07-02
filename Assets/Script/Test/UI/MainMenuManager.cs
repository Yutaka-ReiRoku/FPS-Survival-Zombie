using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Drives the dedicated Main Menu scene: starts the game, quits, and shows the
/// persisted best score. Buttons are wired in code (no editor onClick needed).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button quitButton;

    [Header("Scenes")]
    public string gameSceneName = "Demo_City_Test";

    [Header("Best Score (optional)")]
    public TMP_Text bestScoreText;

    private void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void Start()
    {
        // A menu must be interactive with a visible cursor and normal time.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshBestScore();
        StartCoroutine(BindToPlayFab());
    }

    private IEnumerator BindToPlayFab()
    {
        // Wait for PlayFabManager to be ready (it may live in a bootstrap
        // scene and load slightly after this MainMenu scene).
        float timeout = 10f;
        while (PlayFabManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        var pm = PlayFabManager.Instance;
        if (pm != null)
        {
            pm.OnLoginSuccess += HandleLoginSuccess;
            pm.OnCloudDataLoaded += HandleCloudDataLoaded;
            // If already logged in (e.g. returning to main menu), refresh now.
            if (pm.IsLoggedIn) RefreshBestScore();
        }
    }

    private void OnDisable()
    {
        var pm = PlayFabManager.Instance;
        if (pm != null)
        {
            pm.OnLoginSuccess -= HandleLoginSuccess;
            pm.OnCloudDataLoaded -= HandleCloudDataLoaded;
        }
    }

    private void HandleLoginSuccess(string username)
    {
        RefreshBestScore();
    }

    private void HandleCloudDataLoaded(bool success)
    {
        if (success) RefreshBestScore();
    }

    /// <summary>
    /// Refresh the best score text from PlayerPrefs. Call this after cloud
    /// data has been merged so the UI reflects the logged-in account's stats.
    /// </summary>
    public void RefreshBestScore()
    {
        if (bestScoreText == null) return;
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        int bestWave = PlayerPrefs.GetInt("BestWave", 0);
        bestScoreText.text = bestScore > 0
            ? ("Best  " + bestScore + "    Wave " + bestWave)
            : "No record yet";
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
