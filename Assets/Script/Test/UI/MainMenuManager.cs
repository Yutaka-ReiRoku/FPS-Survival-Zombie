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

        if (bestScoreText != null)
        {
            int bestScore = PlayerPrefs.GetInt("BestScore", 0);
            int bestWave = PlayerPrefs.GetInt("BestWave", 0);
            bestScoreText.text = bestScore > 0
                ? ("Best  " + bestScore + "    Wave " + bestWave)
                : "No record yet";
        }
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
