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

    private RenderTexture _rt;
    private Texture2D _tex2D;
    private Material _crtMaterial;

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
        SetupChamferedPlaque(root.Q("HeaderModule"), 28f, true, false);
        SetupChamferedPlaque(root.Q("InputModule_User"), 24f, true, false);
        SetupChamferedPlaque(root.Q("InputModule_Pass"), 24f, true, false);
        SetupChamferedPlaque(root.Q("ActionModule"), 28f, true, false);
        SetupChamferedPlaque(root.Q("FooterModule"), 20f, true, false);

        // Find child input fields and button sections to apply matching chamfered cuts
        _usernameInputContainer = root.Q("InputModule_User")?.Q(className: "input-container-inner");
        _passwordInputContainer = root.Q("InputModule_Pass")?.Q(className: "input-container-inner");
        _btnMainSec = root.Q("ActionButton")?.Q(className: "btn-main-section");

        SetupChamferedPlaque(_usernameInputContainer, 10f, false, false);
        SetupChamferedPlaque(_passwordInputContainer, 10f, false, false);
        SetupChamferedPlaque(_btnMainSec, 8f, false, true);

        // Load custom CRT glitch shader and build RenderTexture
        var shader = Shader.Find("Custom/CRTGlitchShader");
        if (shader != null)
        {
            _crtMaterial = new Material(shader);
        }
        else
        {
            Debug.LogWarning("[PlayFabLoginUI] Custom/CRTGlitchShader not found. Falling back to solid color rendering.");
        }

        _rt = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
        _rt.filterMode = FilterMode.Bilinear;
        _rt.Create();

        _tex2D = new Texture2D(256, 256, TextureFormat.ARGB32, false);
        _tex2D.filterMode = FilterMode.Bilinear;
    }

    private void OnEnable()
    {
        AutoDetectMainMenu();
        HideMainMenu();
        StartCoroutine(BindAndShow());
    }

    private void Update()
    {
        // Drive CRT glitch shader rendering onto RenderTexture, then copy to Texture2D via GPU
        if (_crtMaterial != null && _rt != null && _tex2D != null)
        {
            _crtMaterial.SetFloat("_GlitchTime", Time.realtimeSinceStartup);
            Graphics.Blit(Texture2D.whiteTexture, _rt, _crtMaterial);
            Graphics.CopyTexture(_rt, _tex2D);
            _tex2D.IncrementUpdateCount();
        }

        // Repaint all modules to drive vector laser scanlines and glowing breathing pulse animations
        var root = _doc?.rootVisualElement;
        if (root == null || _loginRoot == null || _loginRoot.style.display == DisplayStyle.None) return;

        root.Q("HeaderModule")?.MarkDirtyRepaint();
        root.Q("InputModule_User")?.MarkDirtyRepaint();
        root.Q("InputModule_Pass")?.MarkDirtyRepaint();
        root.Q("ActionModule")?.MarkDirtyRepaint();
        root.Q("FooterModule")?.MarkDirtyRepaint();

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

    private void SetupChamferedPlaque(VisualElement element, float chamferSize, bool drawScanline, bool isButton)
    {
        if (element == null) return;
        
        // Remove default USS backgrounds/borders to avoid double rendering
        element.style.backgroundColor = Color.clear;
        element.style.borderLeftWidth = 0;
        element.style.borderRightWidth = 0;
        element.style.borderTopWidth = 0;
        element.style.borderBottomWidth = 0;

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

            // 1. Draw background shape
            if (drawScanline && _tex2D != null) // Draw via Mesh API mapping Texture2D copy of shader
            {
                var mesh = mgc.Allocate(6, 12, _tex2D);

                var p0 = new Vector2(chamferSize, 0);
                var p1 = new Vector2(rect.width, 0);
                var p2 = new Vector2(rect.width, rect.height - chamferSize);
                var p3 = new Vector2(rect.width - chamferSize, rect.height);
                var p4 = new Vector2(0, rect.height);
                var p5 = new Vector2(0, chamferSize);

                mesh.SetNextVertex(new Vertex() { position = new Vector3(p0.x, p0.y, Vertex.nearZ), uv = new Vector2(chamferSize / rect.width, 1f), tint = Color.white });
                mesh.SetNextVertex(new Vertex() { position = new Vector3(p1.x, p1.y, Vertex.nearZ), uv = new Vector2(1f, 1f), tint = Color.white });
                mesh.SetNextVertex(new Vertex() { position = new Vector3(p2.x, p2.y, Vertex.nearZ), uv = new Vector2(1f, chamferSize / rect.height), tint = Color.white });
                mesh.SetNextVertex(new Vertex() { position = new Vector3(p3.x, p3.y, Vertex.nearZ), uv = new Vector2(1f - chamferSize / rect.width, 0f), tint = Color.white });
                mesh.SetNextVertex(new Vertex() { position = new Vector3(p4.x, p4.y, Vertex.nearZ), uv = new Vector2(0f, 0f), tint = Color.white });
                mesh.SetNextVertex(new Vertex() { position = new Vector3(p5.x, p5.y, Vertex.nearZ), uv = new Vector2(0f, 1f - chamferSize / rect.height), tint = Color.white });

                // Triangulation: 4 triangles covering the 6-sided polygon clockwise
                mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(5);
                mesh.SetNextIndex(1); mesh.SetNextIndex(2); mesh.SetNextIndex(5);
                mesh.SetNextIndex(2); mesh.SetNextIndex(3); mesh.SetNextIndex(5);
                mesh.SetNextIndex(3); mesh.SetNextIndex(4); mesh.SetNextIndex(5);
            }
            else // Draw via Painter2D fallback solid/transparent fill
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
        // Clean up created textures and materials to avoid memory leaks
        if (_rt != null)
        {
            _rt.Release();
            Destroy(_rt);
        }
        if (_tex2D != null)
        {
            Destroy(_tex2D);
        }
        if (_crtMaterial != null)
        {
            Destroy(_crtMaterial);
        }
    }
}
