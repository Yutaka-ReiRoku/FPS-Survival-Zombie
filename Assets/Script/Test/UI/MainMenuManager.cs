using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]
    public string gameSceneName = "Story mode";

    private UIDocument _doc;
    private Label _bestLabel;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        if (_doc != null)
        {
            var root = _doc.rootVisualElement;

            // Set all TemplateContainers to ignore picking so clicks can pass through to inner panels
            foreach (var tc in root.Query<TemplateContainer>().ToList())
            {
                tc.pickingMode = PickingMode.Ignore;
            }

            var playBtn = root.Q("MainMenuModule_Play");
            var quitBtn = root.Q("TacticalQuitButton");
            _bestLabel = root.Q<Label>("BestText");

            if (playBtn != null)
                playBtn.RegisterCallback<ClickEvent>(_ => PlayGame());
            if (quitBtn != null)
                quitBtn.RegisterCallback<ClickEvent>(_ => QuitGame());
        }

        RefreshBestScore();
        StartCoroutine(BindToPlayFab());
    }

    private IEnumerator BindToPlayFab()
    {
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

    public void RefreshBestScore()
    {
        if (_bestLabel == null) return;
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        int bestWave = PlayerPrefs.GetInt("BestWave", 0);
        _bestLabel.text = bestScore > 0
            ? ("Best  " + bestScore + "    Wave " + bestWave)
            : "No record yet";
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.ResetProgress();
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
