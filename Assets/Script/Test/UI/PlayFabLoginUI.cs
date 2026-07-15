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
    private VisualElement _usernameInputContainer;
    private VisualElement _passwordInputContainer;
    private VisualElement _btnMainSec;


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

        // Apply custom vector API chamfer drawing with large prominent cuts for rusted steel aesthetic
        SetupChamferedPlaque(root.Q("HeaderModule"), 34f, true, false, null);
        SetupChamferedPlaque(root.Q("InputModule_User"), 30f, true, false, null);
        SetupChamferedPlaque(root.Q("InputModule_Pass"), 30f, true, false, null);
        SetupChamferedPlaque(root.Q("ActionModule"), 34f, true, false, null);

        // Find child input fields and button sections to apply matching chamfered cuts
        _usernameInputContainer = root.Q("InputModule_User")?.Q(className: "input-container-inner");
        _passwordInputContainer = root.Q("InputModule_Pass")?.Q(className: "input-container-inner");
        _btnMainSec = root.Q("ActionButton")?.Q(className: "btn-main-section");

        SetupChamferedPlaque(_usernameInputContainer, 12f, false, false, null);
        SetupChamferedPlaque(_passwordInputContainer, 12f, false, false, null);
        SetupChamferedPlaque(_btnMainSec, 10f, false, true, null);
    }

    private void OnEnable()
    {
        AutoDetectMainMenu();
        HideMainMenu();
        StartCoroutine(BindAndShow());
    }

    private void Update()
    {
        // Repaint all modules to drive glowing breathing pulse animations
        var root = _doc?.rootVisualElement;
        if (root == null || _loginRoot == null || _loginRoot.style.display == DisplayStyle.None) return;

        root.Q("HeaderModule")?.MarkDirtyRepaint();
        root.Q("InputModule_User")?.MarkDirtyRepaint();
        root.Q("InputModule_Pass")?.MarkDirtyRepaint();
        root.Q("ActionModule")?.MarkDirtyRepaint();

        _usernameInputContainer?.MarkDirtyRepaint();
        _passwordInputContainer?.MarkDirtyRepaint();
        _btnMainSec?.MarkDirtyRepaint();
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

    private void SetupChamferedPlaque(VisualElement element, float chamferSize, bool drawScanline, bool isButton, Material customMat)
    {
        if (element == null) return;
        
        // Remove default USS backgrounds/borders to avoid double rendering
        element.style.backgroundColor = Color.clear;
        element.style.borderLeftWidth = 0;
        element.style.borderRightWidth = 0;
        element.style.borderTopWidth = 0;
        element.style.borderBottomWidth = 0;

        // All drawing is done directly on the element's own generateVisualContent callback

        bool isHovered = false;

        element.RegisterCallback<MouseEnterEvent>(_ => {
            isHovered = true;
            element.MarkDirtyRepaint();
        });

        element.RegisterCallback<MouseLeaveEvent>(_ => {
            isHovered = false;
            element.MarkDirtyRepaint();
        });

        element.generateVisualContent += mgc =>
        {
            var rect = element.layout;
            if (rect.width <= 0 || rect.height <= 0) return;

            var painter = mgc.painter2D;

            // 1. Draw solid background shape for all elements
            Color fillCol;
            if (isButton)
            {
                fillCol = isHovered 
                    ? new Color(250f / 255f, 100f / 255f, 60f / 255f, 1f)
                    : new Color(230f / 255f, 80f / 255f, 40f / 255f, 1f);
            }
            else if (drawScanline)
            {
                // Dark rusted-iron brown backing for modules
                fillCol = new Color(36f / 255f, 22f / 255f, 22f / 255f, 0.94f);
            }
            else
            {
                // Darker backing for input boxes
                fillCol = new Color(22f / 255f, 15f / 255f, 15f / 255f, 0.85f);
            }

            painter.fillColor = fillCol;
            painter.BeginPath();
            painter.MoveTo(new Vector2(chamferSize, 0));
            painter.LineTo(new Vector2(rect.width, 0));
            painter.LineTo(new Vector2(rect.width, rect.height - chamferSize));
            painter.LineTo(new Vector2(rect.width - chamferSize, rect.height));
            painter.LineTo(new Vector2(0, rect.height));
            painter.LineTo(new Vector2(0, chamferSize));
            painter.ClosePath();
            painter.Fill();

            // 2. Draw yellow-black diagonal warning stripes at the top edge of main modules
            if (drawScanline)
            {
                float badgeW = 60f;
                float badgeH = 7f;
                float startX = rect.width - badgeW - 24f;
                float startY = 4f;

                painter.lineWidth = 1.0f;
                for (float offset = 0; offset < badgeW; offset += 6f)
                {
                    // Draw a yellow stripe
                    painter.strokeColor = new Color(230f / 255f, 180f / 255f, 20f / 255f, 0.8f);
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(startX + offset, startY));
                    painter.LineTo(new Vector2(startX + offset - 4f, startY + badgeH));
                    painter.Stroke();

                    // Draw a black stripe
                    painter.strokeColor = new Color(16f / 255f, 14f / 255f, 14f / 255f, 0.9f);
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(startX + offset + 3f, startY));
                    painter.LineTo(new Vector2(startX + offset - 1f, startY + badgeH));
                    painter.Stroke();
                }
            }

            // 3. Determine outer stroke color (Breathing pulse for modules, static for inputs/buttons)
            Color strokeCol;
            if (isButton)
            {
                strokeCol = new Color(255f / 255f, 255f / 255f, 255f / 255f, 0.3f);
            }
            else if (drawScanline) // Pulsing modules
            {
                float pulse = 0.35f + Mathf.PingPong(Time.realtimeSinceStartup * 1.5f, 0.45f);
                strokeCol = new Color(230f / 255f, 80f / 255f, 40f / 255f, isHovered ? 0.9f : pulse);
            }
            else // Input containers
            {
                strokeCol = new Color(230f / 255f, 80f / 255f, 40f / 255f, isHovered ? 0.8f : 0.25f);
            }

            // Draw outer border
            painter.strokeColor = strokeCol;
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(chamferSize, 0));
            painter.LineTo(new Vector2(rect.width, 0));
            painter.LineTo(new Vector2(rect.width, rect.height - chamferSize));
            painter.LineTo(new Vector2(rect.width - chamferSize, rect.height));
            painter.LineTo(new Vector2(0, rect.height));
            painter.LineTo(new Vector2(0, chamferSize));
            painter.ClosePath();
            painter.Stroke();

            // 4. Draw inner offset double-line border (only for main modules)
            if (drawScanline)
            {
                float d = 3.5f;
                if (rect.width > d * 2 && rect.height > d * 2)
                {
                    Color innerCol = isHovered 
                        ? new Color(230f / 255f, 80f / 255f, 40f / 255f, 0.45f)
                        : new Color(230f / 255f, 80f / 255f, 40f / 255f, 0.15f);
                    
                    painter.strokeColor = innerCol;
                    painter.lineWidth = 1.0f;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(chamferSize, d));
                    painter.LineTo(new Vector2(rect.width - d, d));
                    painter.LineTo(new Vector2(rect.width - d, rect.height - chamferSize));
                    painter.LineTo(new Vector2(rect.width - chamferSize, rect.height - d));
                    painter.LineTo(new Vector2(d, rect.height - d));
                    painter.LineTo(new Vector2(d, chamferSize));
                    painter.ClosePath();
                    painter.Stroke();
                }

                // 5. Draw 4 3D metallic corner rivets (screws) to lock down the plates
                System.Action<Vector2> drawRivet = center =>
                {
                    // Dark outline shadow
                    painter.fillColor = new Color(10f / 255f, 8f / 255f, 8f / 255f, 0.9f);
                    painter.BeginPath();
                    painter.Arc(center, 3.5f, 0f, 360f);
                    painter.Fill();

                    // Inner metallic copper/steel disc
                    painter.fillColor = new Color(130f / 255f, 100f / 255f, 90f / 255f, 0.9f);
                    painter.BeginPath();
                    painter.Arc(center, 2.2f, 0f, 360f);
                    painter.Fill();

                    // Tiny shiny specular highlight
                    painter.fillColor = new Color(255f / 255f, 255f / 255f, 255f / 255f, 0.4f);
                    painter.BeginPath();
                    painter.Arc(center + new Vector2(-0.8f, -0.8f), 0.7f, 0f, 360f);
                    painter.Fill();
                };

                drawRivet(new Vector2(chamferSize + 12f, 12f));
                drawRivet(new Vector2(rect.width - 24f, 12f));
                drawRivet(new Vector2(24f, rect.height - 12f));
                drawRivet(new Vector2(rect.width - chamferSize - 12f, rect.height - 12f));
            }
        };
    }


}
