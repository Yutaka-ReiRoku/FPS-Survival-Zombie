using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayFabLoginUI : MonoBehaviour
{
    [Header("Panel Size (px @1920x1080)")]
    public float panelWidth = 420f;
    public float panelHeight = 520f;

    [Header("Main Menu (optional — auto-detected if null)")]
    public GameObject mainMenuContent;
    public GameObject profileWidget;

    private VisualElement _root;
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
    private GameObject _docGO;

    private bool _isRegisterMode;
    private bool _isBusy;

    private static readonly Color ErrorColor = new Color(0.85f, 0.35f, 0.15f, 1f);
    private static readonly Color SuccessColor = new Color(0.31f, 0.878f, 0.541f, 1f);

    private void Awake()
    {
        Build();
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

    private void Build()
    {
        _docGO = new GameObject("PlayFabLogin_Doc", typeof(UIDocument));
        _docGO.transform.SetParent(transform, false);
        var doc = _docGO.GetComponent<UIDocument>();
        doc.sortingOrder = 100;

        _root = new VisualElement();
        _root.name = "PlayFabLoginRoot";
        _root.AddToClassList("overlay");

        _panel = new VisualElement();
        _panel.name = "LoginPanel";
        _panel.style.width = panelWidth;
        _root.Add(_panel);

        _titleText = new Label("LOGIN");
        _titleText.AddToClassList("login-title");
        _panel.Add(_titleText);

        _statusText = new Label("");
        _statusText.AddToClassList("login-status");
        _panel.Add(_statusText);

        AddFieldRow("UsernameLabel", "Username");
        _usernameInput = new TextField();
        _usernameInput.name = "UsernameInput";
        _usernameInput.AddToClassList("login-input");
        _usernameInput.style.marginBottom = 12;
        _panel.Add(_usernameInput);

        AddFieldRow("PasswordLabel", "Password");
        _passwordInput = new TextField();
        _passwordInput.name = "PasswordInput";
        _passwordInput.isPasswordField = true;
        _passwordInput.AddToClassList("login-input");
        _passwordInput.style.marginBottom = 16;
        _panel.Add(_passwordInput);

        _actionButton = new VisualElement();
        _actionButton.name = "ActionButton";
        _actionButton.AddToClassList("login-action-btn");
        _actionButton.focusable = true;
        _actionButton.RegisterCallback<ClickEvent>(_ => OnActionButtonClicked());

        var actionLabel = new Label("LOGIN");
        actionLabel.AddToClassList("btn-label");
        _actionButton.Add(actionLabel);
        _panel.Add(_actionButton);

        var toggleRow = new VisualElement();
        toggleRow.name = "ToggleRow";
        _panel.Add(toggleRow);

        _toggleText = new Label("Don't have an account? Register");
        _toggleText.AddToClassList("toggle-label");
        toggleRow.Add(_toggleText);

        _toggleButton = new VisualElement();
        _toggleButton.name = "ToggleButton";
        _toggleButton.AddToClassList("login-toggle-btn");
        _toggleButton.focusable = true;
        _toggleButton.RegisterCallback<ClickEvent>(_ => ToggleMode());
        var toggleBtnLabel = new Label("Register");
        toggleBtnLabel.AddToClassList("btn-label");
        _toggleButton.Add(toggleBtnLabel);
        toggleRow.Add(_toggleButton);

        _logoutButton = new VisualElement();
        _logoutButton.name = "LogoutButton";
        _logoutButton.AddToClassList("login-logout-btn");
        _logoutButton.focusable = true;
        _logoutButton.RegisterCallback<ClickEvent>(_ => OnLogoutClicked());
        _logoutButton.style.display = DisplayStyle.None;
        var logoutLabel = new Label("LOGOUT");
        logoutLabel.AddToClassList("btn-label");
        _logoutButton.Add(logoutLabel);
        _panel.Add(_logoutButton);

        var sheet = Resources.Load<StyleSheet>("PlayFabLogin");
        if (sheet != null) _root.styleSheets.Add(sheet);

        doc.rootVisualElement.Add(_root);
    }

    private void AddFieldRow(string name, string label)
    {
        var lbl = new Label(label);
        lbl.name = name;
        lbl.AddToClassList("field-label");
        _panel.Add(lbl);
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
            _titleText.text = "REGISTER";
            _actionButtonLabel = _actionButton.Q<Label>();
            if (_actionButtonLabel != null) _actionButtonLabel.text = "CREATE ACCOUNT";
            _toggleText.text = "Already have an account?";
            _toggleButtonLabel = _toggleButton.Q<Label>();
            if (_toggleButtonLabel != null) _toggleButtonLabel.text = "Login";
        }
        else
        {
            _titleText.text = "LOGIN";
            _actionButtonLabel = _actionButton.Q<Label>();
            if (_actionButtonLabel != null) _actionButtonLabel.text = "LOGIN";
            _toggleText.text = "Don't have an account?";
            _toggleButtonLabel = _toggleButton.Q<Label>();
            if (_toggleButtonLabel != null) _toggleButtonLabel.text = "Register";
        }
    }

    private void OnActionButtonClicked()
    {
        if (_isBusy) return;

        string username = _usernameInput.text.Trim();
        string password = _passwordInput.text;

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
        _actionButton.SetEnabled(true);
        _logoutButton.style.display = DisplayStyle.None;
        HideMainMenu();
        _panel.style.display = DisplayStyle.Flex;
        UpdateUI();
        ShowStatus("Logged out. Please log in again.", new Color(0.62f, 0.66f, 0.72f, 1f));
    }

    public void ShowLoginPanel()
    {
        HandleLogout();
    }

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
        _actionButton.SetEnabled(!busy);
    }
}
