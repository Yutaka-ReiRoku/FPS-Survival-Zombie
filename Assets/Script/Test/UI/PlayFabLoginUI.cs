using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayFabLoginUI : MonoBehaviour
{
    [Header("Panel Size (px @1920x1080)")]
    public float panelWidth = 420f;

    [Header("Main Menu (optional — auto-detected if null)")]
    public GameObject mainMenuContent;
    public GameObject profileWidget;

    private UIDocument _doc;
    private VisualElement _panel;
    private TextField _usernameInput;
    private TextField _passwordInput;
    private Label _statusText;
    private Label _titleText;
    private Label _toggleText;
    private VisualElement _actionButton;
    private Label _actionButtonLabel;
    private VisualElement _toggleButton;
    private Label _toggleButtonLabel;
    private VisualElement _logoutButton;

    private bool _isRegisterMode;
    private bool _isBusy;

    private static readonly Color ErrorColor = new Color(0.85f, 0.35f, 0.15f, 1f);
    private static readonly Color SuccessColor = new Color(0.31f, 0.878f, 0.541f, 1f);

    private void Awake()
    {
        var asset = Resources.Load<VisualTreeAsset>("PlayFabLogin");
        if (asset == null) { enabled = false; return; }

        var go = new GameObject("PlayFabLogin_Doc", typeof(UIDocument));
        go.transform.SetParent(transform, false);
        _doc = go.GetComponent<UIDocument>();
        _doc.sortingOrder = 100;
        asset.CloneTree(_doc.rootVisualElement);

        var root = _doc.rootVisualElement;
        _panel = root.Q("LoginPanel");
        _panel.style.width = panelWidth;

        _titleText = root.Q<Label>("LoginTitle");
        _statusText = root.Q<Label>("LoginStatus");
        _usernameInput = root.Q<TextField>("UsernameInput");
        _passwordInput = root.Q<TextField>("PasswordInput");

        _actionButton = root.Q("ActionButton");
        _actionButton.focusable = true;
        _actionButton.RegisterCallback<ClickEvent>(_ => OnActionButtonClicked());
        _actionButtonLabel = _actionButton.Q<Label>("ActionLabel");

        _toggleText = root.Q<Label>("ToggleText");
        _toggleButton = root.Q("ToggleButton");
        _toggleButton.focusable = true;
        _toggleButton.RegisterCallback<ClickEvent>(_ => ToggleMode());
        _toggleButtonLabel = _toggleButton.Q<Label>("ToggleBtnLabel");

        _logoutButton = root.Q("LogoutButton");
        _logoutButton.focusable = true;
        _logoutButton.RegisterCallback<ClickEvent>(_ => OnLogoutClicked());
        _logoutButton.style.display = DisplayStyle.None;
    }

    private void OnEnable()
    {
        AutoDetectMainMenu();
        HideMainMenu();
        StartCoroutine(BindAndShow());
    }

    private void AutoDetectMainMenu()
    {
        if (mainMenuContent == null)
        {
            var tr = transform.parent?.Find("Content");
            if (tr != null) mainMenuContent = tr.gameObject;
        }
        if (profileWidget == null)
        {
            var tr = transform.parent?.Find("PlayerProfileWidget");
            if (tr != null) profileWidget = tr.gameObject;
        }
    }

    private void HideMainMenu()
    {
        if (mainMenuContent != null) mainMenuContent.SetActive(false);
        if (profileWidget != null) profileWidget.SetActive(false);
    }

    private void ShowMainMenu()
    {
        if (mainMenuContent != null) mainMenuContent.SetActive(true);
        if (profileWidget != null) profileWidget.SetActive(true);
    }

    private void OnDisable()
    {
        var pm = PlayFabManager.Instance;
        if (pm != null)
        {
            pm.OnLoginSuccess -= HandleLoginSuccess;
            pm.OnLoginError -= HandleLoginError;
            pm.OnLogout -= HandleLogout;
        }
    }

    private IEnumerator BindAndShow()
    {
        float timeout = 10f;
        while (PlayFabManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        var pm = PlayFabManager.Instance;
        if (pm == null)
        {
            Debug.LogWarning("[PlayFabLoginUI] PlayFabManager not found.");
            ShowStatus("PlayFabManager not found.", ErrorColor);
            _panel.style.display = DisplayStyle.Flex;
            yield break;
        }

        pm.OnLoginSuccess += HandleLoginSuccess;
        pm.OnLoginError += HandleLoginError;
        pm.OnLogout += HandleLogout;

        if (pm.IsLoggedIn)
        {
            _panel.style.display = DisplayStyle.None;
            ShowMainMenu();
            yield break;
        }

        HideMainMenu();
        _panel.style.display = DisplayStyle.Flex;
        UpdateUI();
    }

    private void ToggleMode()
    {
        _isRegisterMode = !_isRegisterMode;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_isRegisterMode)
        {
            if (_titleText != null) _titleText.text = "REGISTER";
            if (_actionButtonLabel != null) _actionButtonLabel.text = "CREATE ACCOUNT";
            if (_toggleText != null) _toggleText.text = "Already have an account?";
            if (_toggleButtonLabel != null) _toggleButtonLabel.text = "Login";
        }
        else
        {
            if (_titleText != null) _titleText.text = "LOGIN";
            if (_actionButtonLabel != null) _actionButtonLabel.text = "LOGIN";
            if (_toggleText != null) _toggleText.text = "Don't have an account?";
            if (_toggleButtonLabel != null) _toggleButtonLabel.text = "Register";
        }
    }

    private void OnActionButtonClicked()
    {
        if (_isBusy) return;

        string username = _usernameInput?.text.Trim() ?? "";
        string password = _passwordInput?.text ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowStatus("Please enter username and password.", ErrorColor);
            return;
        }

        if (_isRegisterMode && password.Length < 6)
        {
            ShowStatus("Password must be at least 6 characters.", ErrorColor);
            return;
        }

        SetBusy(true);
        ShowStatus(_isRegisterMode ? "Creating account..." : "Logging in...", new Color(0.62f, 0.66f, 0.72f, 1f));

        var pm = PlayFabManager.Instance;
        if (pm == null)
        {
            ShowStatus("PlayFabManager not found.", ErrorColor);
            SetBusy(false);
            return;
        }

        if (_isRegisterMode)
        {
            pm.Register(username, password, (success, error) =>
            {
                SetBusy(false);
                if (success)
                    ShowStatus("Account created! Welcome.", SuccessColor);
                else
                    ShowStatus(error ?? "Registration failed.", ErrorColor);
            });
        }
        else
        {
            pm.Login(username, password, (success, error) =>
            {
                SetBusy(false);
                if (success)
                    ShowStatus("Login successful!", SuccessColor);
                else
                    ShowStatus(error ?? "Login failed.", ErrorColor);
            });
        }
    }

    private void OnLogoutClicked()
    {
        PlayFabManager.Instance?.Logout();
        _panel.style.display = DisplayStyle.Flex;
        _logoutButton.style.display = DisplayStyle.None;
        HideMainMenu();
        ShowStatus("Logged out.", new Color(0.62f, 0.66f, 0.72f, 1f));
    }

    private void HandleLogout()
    {
        _isRegisterMode = false;
        _isBusy = false;
        if (_usernameInput != null) _usernameInput.value = "";
        if (_passwordInput != null) _passwordInput.value = "";
        _actionButton?.SetEnabled(true);
        _logoutButton.style.display = DisplayStyle.None;
        HideMainMenu();
        _panel.style.display = DisplayStyle.Flex;
        UpdateUI();
        ShowStatus("Logged out. Please log in again.", new Color(0.62f, 0.66f, 0.72f, 1f));
    }

    public void ShowLoginPanel() { HandleLogout(); }

    private void HandleLoginSuccess(string username)
    {
        ShowStatus($"Welcome, {username}!", SuccessColor);
        StartCoroutine(HideAfterDelay(0.8f));
        _logoutButton.style.display = DisplayStyle.Flex;
    }

    private void HandleLoginError(string error)
    {
        ShowStatus(error, ErrorColor);
        SetBusy(false);
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _panel.style.display = DisplayStyle.None;
        ShowMainMenu();
    }

    private void ShowStatus(string message, Color color)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
            _statusText.style.color = color;
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        _actionButton?.SetEnabled(!busy);
    }

    private void OnDestroy()
    {
        if (_doc != null && _doc.gameObject != null)
            Destroy(_doc.gameObject);
    }
}
