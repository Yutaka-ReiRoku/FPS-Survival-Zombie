using TMPro;
using UnityEngine;
using UnityEngine.UI;
using cowsins;

public class JournalUI : MonoBehaviour
{
    public static JournalUI Instance;

    public GameObject panel;

    public TMP_Text title;

    public TMP_Text content;

    public Image image;

    public AudioSource audioSource;

    private PlayerControl _playerControl;
    private bool _open;

    /// <summary>True while the journal panel is visible.</summary>
    public bool IsOpen => _open;

    void Awake()
    {
        Instance = this;

        panel.SetActive(false);
    }

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerControl = player.GetComponentInChildren<PlayerControl>();
    }

    public void Show(JournalData journal)
    {
        // Don't open the journal while the pause menu or skill tree is already open.
        bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        bool skillTreeOpen = false;
        var skillTree = FindAnyObjectByType<SkillTreeWidget>();
        if (skillTree != null) skillTreeOpen = skillTree.IsOpen;
        if (pauseOpen || skillTreeOpen) return;

        _open = true;
        panel.SetActive(true);

        title.text = journal.title;
        content.text = journal.content;

        image.sprite = journal.image;

        if (journal.voiceLog != null)
        {
            audioSource.Stop();
            audioSource.clip = journal.voiceLog;
            audioSource.Play();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0;

        // Strip control from the player so they can't shoot/look around while
        // the journal is open (Time.timeScale=0 alone doesn't block input).
        if (_playerControl != null)
            _playerControl.LoseControl();

        // Hide gameplay HUD while the journal is open.
        var canvas = transform.parent.GetComponentInParent<Canvas>();
        PauseManager.SetHUDVisible(canvas != null ? canvas.transform : transform.parent, false);
    }

    public void Close()
    {
        if (!_open) return;
        _open = false;
        panel.SetActive(false);

        audioSource.Stop();

        // Only restore time/cursor/control if neither the pause menu nor the
        // skill tree is holding them, and the game isn't over.
        bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
        if (!pauseOpen && !gameOver)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Time.timeScale = 1;

            if (_playerControl != null)
                _playerControl.GrantControl();

            // Restore gameplay HUD when no other overlay is holding it.
            var canvas = transform.parent.GetComponentInParent<Canvas>();
            PauseManager.SetHUDVisible(canvas != null ? canvas.transform : transform.parent, true);
        }
    }
}
