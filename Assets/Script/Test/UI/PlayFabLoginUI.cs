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

    private Material _headerMaterial;
    private Material _userMaterial;
    private Material _passMaterial;
    private Material _actionMaterial;
    private Material _footerMaterial;

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

        // Load custom CRT glitch shader and instantiate materials for individual modules
        var shader = Shader.Find("Custom/CRTGlitchShader");
        if (shader != null)
        {
            _headerMaterial = new Material(shader);
            _userMaterial = new Material(shader);
            _passMaterial = new Material(shader);
            _actionMaterial = new Material(shader);
            _footerMaterial = new Material(shader);
        }
        else
        {
            Debug.LogWarning("[PlayFabLoginUI] Custom/CRTGlitchShader not found. Falling back to solid color rendering.");
        }

        // Apply custom vector API chamfer drawing with large prominent cuts for rusted steel aesthetic
        SetupChamferedPlaque(root.Q("HeaderModule"), 28f, true, false, _headerMaterial);
        SetupChamferedPlaque(root.Q("InputModule_User"), 24f, true, false, _userMaterial);
        SetupChamferedPlaque(root.Q("InputModule_Pass"), 24f, true, false, _passMaterial);
        SetupChamferedPlaque(root.Q("ActionModule"), 28f, true, false, _actionMaterial);
        SetupChamferedPlaque(root.Q("FooterModule"), 20f, true, false, _footerMaterial);

        // Find child input fields and button sections to apply matching chamfered cuts
        _usernameInputContainer = root.Q("InputModule_User")?.Q(className: "input-container-inner");
        _passwordInputContainer = root.Q("InputModule_Pass")?.Q(className: "input-container-inner");
        _btnMainSec = root.Q("ActionButton")?.Q(className: "btn-main-section");

        SetupChamferedPlaque(_usernameInputContainer, 10f, false, false, null);
        SetupChamferedPlaque(_passwordInputContainer, 10f, false, false, null);
        SetupChamferedPlaque(_btnMainSec, 8f, false, true, null);
    }

    private void OnEnable()
    {
        AutoDetectMainMenu();
        HideMainMenu();
        StartCoroutine(BindAndShow());
    }

    private void Update()
    {
        var root = _doc?.rootVisualElement;
        if (root == null || _loginRoot == null || _loginRoot.style.display == DisplayStyle.None) return;

        // Feed layout-dependent sizes and real-time parameters to module shaders
        UpdateMaterial(_headerMaterial, root.Q("HeaderModule"), 28f);
        UpdateMaterial(_userMaterial, root.Q("InputModule_User"), 24f);
        UpdateMaterial(_passMaterial, root.Q("InputModule_Pass"), 24f);
        UpdateMaterial(_actionMaterial, root.Q("ActionModule"), 28f);
        UpdateMaterial(_footerMaterial, root.Q("FooterModule"), 20f);

        // Repaint all modules to drive vector laser scanlines and glowing breathing pulse animations
        root.Q("HeaderModule")?.MarkDirtyRepaint();
        root.Q("InputModule_User")?.MarkDirtyRepaint();
        root.Q("InputModule_Pass")?.MarkDirtyRepaint();
        root.Q("ActionModule")?.MarkDirtyRepaint();
        root.Q("FooterModule")?.MarkDirtyRepaint();

        _usernameInputContainer?.MarkDirtyRepaint();
        _passwordInputContainer?.MarkDirtyRepaint();
        _btnMainSec?.MarkDirtyRepaint();
    }

    private void UpdateMaterial(Material mat, VisualElement element, float chamfer)
    {
        if (mat != null && element != null)
        {
            var rect = element.layout;
            mat.SetFloat("_GlitchTime", Time.realtimeSinceStartup);
            mat.SetFloat("_Width", rect.width);
            mat.SetFloat("_Height", rect.height);
            mat.SetFloat("_ChamferSize", chamfer);
        }
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

        // Programmatically inject an absolute positioned background plaque element mapped with the shader material
        if (customMat != null)
        {
            var bgPlaque = element.Q("BackgroundPlaque");
            if (bgPlaque == null)
            {
                bgPlaque = new VisualElement() { name = "BackgroundPlaque" };
                bgPlaque.style.position = Position.Absolute;
                bgPlaque.style.left = 0;
                bgPlaque.style.top = 0;
                bgPlaque.style.right = 0;
                bgPlaque.style.bottom = 0;
                bgPlaque.style.unityMaterial = new StyleMaterialDefinition(customMat);
                element.Insert(0, bgPlaque);
            }
        }

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

            // 1. Draw background shape only for elements NOT using the shader material background plaque
            if (customMat == null)
            {
                Color fillCol;
                if (isButton)
                {
                    fillCol = isHovered 
                        ? new Color(250f / 255f, 100f / 255f, 60f / 255f, 1f)
                        : new Color(230f / 255f, 80f / 255f, 40f / 255f, 1f);
                }
                else
                {
                    fillCol = new Color(30f / 255f, 20f / 255f, 20f / 255f, 0.75f);
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
            }

            // 2. Draw Vector scanline sweep (subtle cybermatic effect)
            if (drawScanline)
            {
                float scanY = (Time.realtimeSinceStartup * 45f) % rect.height;
                painter.strokeColor = new Color(230f / 255f, 80f / 255f, 40f / 255f, 0.07f);
                painter.lineWidth = 1.0f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(16f, scanY));
                painter.LineTo(new Vector2(rect.width - 16f, scanY));
                painter.Stroke();
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
            }
        };
    }

    private void OnDestroy()
    {
        // Clean up created materials to avoid memory leaks
        if (_headerMaterial != null) Destroy(_headerMaterial);
        if (_userMaterial != null) Destroy(_userMaterial);
        if (_passMaterial != null) Destroy(_passMaterial);
        if (_actionMaterial != null) Destroy(_actionMaterial);
        if (_footerMaterial != null) Destroy(_footerMaterial);
    }
}
