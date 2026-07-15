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

    private VisualElement _loginRoot;
    private VisualElement _centerGroup;
    private VisualElement _profileRoot;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
        if (_doc == null) { enabled = false; return; }

        var root = _doc.rootVisualElement;
        
        // Find elements within the single shared document
        _loginRoot = root.Q("PlayFabLoginRoot");
        _panel = root.Q("LoginPanel");
        if (_panel != null) _panel.style.width = panelWidth;

        _centerGroup = root.Q("CenterGroup");
        _profileRoot = root.Q("ProfileRoot");

        _titleText = root.Q<Label>("LoginTitle");
        _statusText = root.Q<Label>("LoginStatus");
        _usernameInput = root.Q<TextField>("UsernameInput");
        _passwordInput = root.Q<TextField>("PasswordInput");

        _actionButton = root.Q("ActionButton");
        if (_actionButton != null)
        {
            _actionButton.focusable = true;
            _actionButton.RegisterCallback<ClickEvent>(_ => OnActionButtonClicked());
            _actionButtonLabel = _actionButton.Q<Label>("ActionLabel");
        }

        _toggleText = root.Q<Label>("ToggleText");
        _toggleButton = root.Q("ToggleButton");
        if (_toggleButton != null)
        {
            _toggleButton.focusable = true;
            _toggleButton.RegisterCallback<ClickEvent>(_ => ToggleMode());
            _toggleButtonLabel = _toggleButton.Q<Label>("ToggleBtnLabel");
        }

        _logoutButton = root.Q("LogoutButton");
        if (_logoutButton != null)
        {
            _logoutButton.focusable = true;
            _logoutButton.RegisterCallback<ClickEvent>(_ => OnLogoutClicked());
            _logoutButton.style.display = DisplayStyle.None;
        }
    }

    private void OnEnable()
    {
        AutoDetectMainMenu();
        HideMainMenu();
        StartCoroutine(BindAndShow());
    }

    private void AutoDetectMainMenu()
    {
        // Keep fallback support for manually assigned GameObjects
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
        if (_centerGroup != null) _centerGroup.style.display = DisplayStyle.None;
        if (_profileRoot != null) _profileRoot.style.display = DisplayStyle.None;
        if (mainMenuContent != null) mainMenuContent.SetActive(false);
        if (profileWidget != null) profileWidget.SetActive(false);
    }

    private void ShowMainMenu()
    {
        if (_centerGroup != null) _centerGroup.style.display = DisplayStyle.Flex;
        if (_profileRoot != null) _profileRoot.style.display = DisplayStyle.Flex;
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
            if (_loginRoot != null) _loginRoot.style.display = DisplayStyle.Flex;
            _panel.style.display = DisplayStyle.Flex;
            yield break;
        }

        pm.OnLoginSuccess += HandleLoginSuccess;
        pm.OnLoginError += HandleLoginError;
        pm.OnLogout += HandleLogout;

        if (pm.IsLoggedIn)
        {
            if (_loginRoot != null) _loginRoot.style.display = DisplayStyle.None;
            _panel.style.display = DisplayStyle.None;
            ShowMainMenu();
            yield break;
        }

        HideMainMenu();
        if (_loginRoot != null) _loginRoot.style.display = DisplayStyle.Flex;
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
        if (_loginRoot != null) _loginRoot.style.display = DisplayStyle.Flex;
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
        if (_loginRoot != null) _loginRoot.style.display = DisplayStyle.Flex;
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
        if (_loginRoot != null) _loginRoot.style.display = DisplayStyle.None;
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
        // Shared UIDocument is managed by the MainMenu scene GameObject, do not destroy it.
    }
}
