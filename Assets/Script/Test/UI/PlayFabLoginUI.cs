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

    private static readonly Color BgColor = new Color(0.078f, 0.094f, 0.118f, 0.96f);
    private static readonly Color ButtonColor = new Color(0.31f, 0.878f, 0.541f, 1f);
    private static readonly Color TextPrimary = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color TextMuted = new Color(0.62f, 0.66f, 0.72f, 1f);
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
        _root.style.position = Position.Absolute;
        _root.style.left = 0;
        _root.style.right = 0;
        _root.style.top = 0;
        _root.style.bottom = 0;
        _root.style.alignItems = Align.Center;
        _root.style.justifyContent = Justify.Center;

        _panel = new VisualElement();
        _panel.name = "LoginPanel";
        _panel.style.width = panelWidth;
        _panel.style.backgroundColor = BgColor;
        _panel.style.paddingLeft = 40;
        _panel.style.paddingRight = 40;
        _panel.style.paddingTop = 40;
        _panel.style.paddingBottom = 40;
        _panel.style.flexDirection = FlexDirection.Column;
        _root.Add(_panel);

        _titleText = new Label("LOGIN");
        _titleText.name = "Title";
        _titleText.style.fontSize = 28;
        _titleText.style.color = TextPrimary;
        _titleText.style.unityTextAlign = TextAnchor.UpperCenter;
        _titleText.style.marginBottom = 16;
        _titleText.style.height = 40;
        _panel.Add(_titleText);

        _statusText = new Label("");
        _statusText.name = "Status";
        _statusText.style.fontSize = 14;
        _statusText.style.color = TextMuted;
        _statusText.style.unityTextAlign = TextAnchor.UpperCenter;
        _statusText.style.height = 24;
        _statusText.style.marginBottom = 8;
        _panel.Add(_statusText);

        AddFieldRow("UsernameLabel", "Username", 14, TextMuted);
        _usernameInput = new TextField();
        _usernameInput.name = "UsernameInput";
        _usernameInput.style.height = 40;
        _usernameInput.style.marginBottom = 12;
        StylizeInput(_usernameInput);
        _panel.Add(_usernameInput);

        AddFieldRow("PasswordLabel", "Password", 14, TextMuted);
        _passwordInput = new TextField();
        _passwordInput.name = "PasswordInput";
        _passwordInput.isPasswordField = true;
        _passwordInput.style.height = 40;
        _passwordInput.style.marginBottom = 16;
        StylizeInput(_passwordInput);
        _panel.Add(_passwordInput);

        _actionButton = CreateButton("ActionButton", "LOGIN", ButtonColor);
        _actionButton.style.height = 44;
        _actionButton.style.marginBottom = 12;
        _actionButton.RegisterCallback<ClickEvent>(_ => OnActionButtonClicked());
        _panel.Add(_actionButton);

        var toggleRow = new VisualElement();
        toggleRow.name = "ToggleRow";
        toggleRow.style.flexDirection = FlexDirection.Row;
        toggleRow.style.alignItems = Align.Center;
        toggleRow.style.justifyContent = Justify.Center;
        toggleRow.style.marginBottom = 12;
        _panel.Add(toggleRow);

        _toggleText = new Label("Don't have an account? Register");
        _toggleText.style.fontSize = 13;
        _toggleText.style.color = TextMuted;
        toggleRow.Add(_toggleText);

        _toggleButton = CreateButton("ToggleButton", "Register", new Color(0.2f, 0.25f, 0.3f, 1f));
        _toggleButton.style.width = 80;
        _toggleButton.style.height = 30;
        _toggleButton.RegisterCallback<ClickEvent>(_ => ToggleMode());
        toggleRow.Add(_toggleButton);

        _logoutButton = CreateButton("LogoutButton", "LOGOUT", new Color(0.5f, 0.2f, 0.2f, 1f));
        _logoutButton.style.height = 36;
        _logoutButton.style.display = DisplayStyle.None;
        _logoutButton.RegisterCallback<ClickEvent>(_ => OnLogoutClicked());
        _panel.Add(_logoutButton);

        doc.rootVisualElement.Add(_root);
    }

    private void AddFieldRow(string name, string label, float fontSize, Color color)
    {
        var lbl = new Label(label);
        lbl.name = name;
        lbl.style.fontSize = fontSize;
        lbl.style.color = color;
        lbl.style.unityTextAlign = TextAnchor.MiddleLeft;
        lbl.style.marginBottom = 4;
        _panel.Add(lbl);
    }

    private void StylizeInput(TextField tf)
    {
        tf.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        tf.style.borderLeftWidth = 0;
        tf.style.borderRightWidth = 0;
        tf.style.borderTopWidth = 0;
        tf.style.borderBottomWidth = 0;
        tf.style.color = TextPrimary;
        tf.style.fontSize = 16;
    }

    private VisualElement CreateButton(string name, string label, Color bgColor)
    {
        var btn = new VisualElement();
        btn.name = name;
        btn.style.backgroundColor = bgColor;
        btn.style.alignItems = Align.Center;
        btn.style.justifyContent = Justify.Center;
        btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.focusable = true;
        btn.RegisterCallback<PointerEnterEvent>(_ => btn.style.backgroundColor = Color.Lerp(bgColor, Color.white, 0.15f));
        btn.RegisterCallback<PointerLeaveEvent>(_ => btn.style.backgroundColor = bgColor);

        var lbl = new Label(label);
        lbl.style.fontSize = 16;
        lbl.style.color = Color.white;
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.Add(lbl);

        return btn;
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
        ShowStatus(_isRegisterMode ? "Creating account..." : "Logging in...", TextMuted);

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
        ShowStatus("Logged out.", TextMuted);
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
        ShowStatus("Logged out. Please log in again.", TextMuted);
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
