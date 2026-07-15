using UnityEngine;
using UnityEngine.UIElements;
using cowsins;
using Cursor = UnityEngine.Cursor;

public class JournalUI : MonoBehaviour
{
    public static JournalUI Instance;

    private UIDocument _doc;
    private VisualElement _panel;
    private Label _title;
    private Label _content;
    private VisualElement _illustration;
    private Button _closeButton;
    private AudioSource _audioSource;
    private PlayerControl _playerControl;
    private bool _open;

    public bool IsOpen => _open;

    void Awake()
    {
        Instance = this;
        _doc = GetComponent<UIDocument>();
        _audioSource = GetComponent<AudioSource>();

        var root = _doc.rootVisualElement;
        _panel = root.Q("JournalUI");
        _title = root.Q<Label>("JournalTitle");
        _content = root.Q<Label>("JournalContent");
        _illustration = root.Q("Illustration");
        _closeButton = root.Q<Button>("CloseButton");

        if (_closeButton != null)
            _closeButton.RegisterCallback<ClickEvent>(_ => Close());
        if (_panel != null)
            _panel.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        if (_open) Close();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_open)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerControl = player.GetComponentInChildren<PlayerControl>();
    }

    public void Show(JournalData journal)
    {
        bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        bool skillTreeOpen = false;
        var skillTree = FindAnyObjectByType<SkillTreeWidget>();
        if (skillTree != null) skillTreeOpen = skillTree.IsOpen;
        if (pauseOpen || skillTreeOpen) return;

        _open = true;
        _panel.style.display = DisplayStyle.Flex;
        _panel.AddToClassList("visible");

        _title.text = journal.title;
        _content.text = journal.content;
        _illustration.style.backgroundImage = new StyleBackground(journal.image);

        if (journal.voiceLog != null)
        {
            _audioSource.Stop();
            _audioSource.clip = journal.voiceLog;
            _audioSource.Play();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0;

        if (_playerControl != null)
            _playerControl.LoseControl();

        PauseManager.SetHUDVisible(transform, false);
    }

    public void Close()
    {
        if (!_open) return;
        _open = false;
        _panel.RemoveFromClassList("visible");
        
        var card = _panel.Q("Card");
        if (card != null)
        {
            card.RegisterCallback<TransitionEndEvent>(OnJournalExitTransitionEnd);
        }
        else
        {
            _panel.style.display = DisplayStyle.None;
        }

        _audioSource.Stop();

        bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
        if (!pauseOpen && !gameOver)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Time.timeScale = 1;

            if (_playerControl != null)
                _playerControl.GrantControl();

            PauseManager.SetHUDVisible(transform, true);
        }
    }

    private void OnJournalExitTransitionEnd(TransitionEndEvent evt)
    {
        var card = evt.currentTarget as VisualElement;
        if (card != null)
        {
            card.UnregisterCallback<TransitionEndEvent>(OnJournalExitTransitionEnd);
        }
        if (!_open && _panel != null)
        {
            _panel.style.display = DisplayStyle.None;
        }
    }
}
