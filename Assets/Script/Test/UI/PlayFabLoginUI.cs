using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Self-building login/register panel for PlayFab authentication.
/// Creates all UI elements in code (no prefab needed) using the project's
/// UITheme design system. Shows a login form with username/password fields,
/// a register toggle, and a status message. On successful login, the panel
/// hides itself and the game can proceed.
///
/// Place on a child of the GameUICanvas (or any Canvas). The panel is
/// shown automatically on enable if not logged in, and hidden on
/// PlayFabManager.OnLoginSuccess.
/// </summary>
public class PlayFabLoginUI : MonoBehaviour
{
    [Header("Panel Size (px @1920x1080)")]
    public float panelWidth = 420f;
    public float panelHeight = 520f;

    [Header("References (optional — auto-created if null)")]
    [Tooltip("Assign a TMP_FontAsset to use for all text. If null, falls back to default.")]
    public TMP_FontAsset fontAsset;

    private GameObject _panel;
    private TMP_InputField _usernameInput;
    private TMP_InputField _passwordInput;
    private TMP_Text _statusText;
    private TMP_Text _titleText;
    private TMP_Text _toggleText;
    private Button _actionButton;
    private Button _toggleButton;
    private Button _logoutButton;

    private bool _isRegisterMode;
    private bool _isBusy;

    private static readonly Color BgColor = new Color(0.078f, 0.094f, 0.118f, 0.96f);
    private static readonly Color InputBgColor = new Color(0.12f, 0.14f, 0.18f, 1f);
    private static readonly Color ButtonColor = new Color(0.31f, 0.878f, 0.541f, 1f);
    private static readonly Color ButtonHoverColor = new Color(0.4f, 0.95f, 0.6f, 1f);
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
        StartCoroutine(BindAndShow());
    }

    private void OnDisable()
    {
        var pm = PlayFabManager.Instance;
        if (pm != null)
        {
            pm.OnLoginSuccess -= HandleLoginSuccess;
            pm.OnLoginError -= HandleLoginError;
        }
    }

    // =========================================================================
    //  UI Construction
    // =========================================================================

    private void Build()
    {
        // Root panel — centered, sized
        _panel = new GameObject("PlayFabLoginPanel", typeof(RectTransform));
        _panel.transform.SetParent(transform, false);
        var prt = (RectTransform)_panel.transform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(panelWidth, panelHeight);

        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = BgColor;
        panelImg.raycastTarget = true;

        // Use a vertical layout group to stack elements
        var vlg = _panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(40, 40, 40, 40);
        vlg.spacing = 16;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // --- Title ---
        _titleText = CreateText("Title", prt, "LOGIN", 28, TextPrimary, TextAnchor.UpperCenter);
        SetLayoutElement(_titleText.gameObject, minHeight: 40, flexibleHeight: 0);

        // --- Status ---
        _statusText = CreateText("Status", prt, "", 14, TextMuted, TextAnchor.UpperCenter);
        SetLayoutElement(_statusText.gameObject, minHeight: 24);

        // --- Username label + input ---
        CreateText("UsernameLabel", prt, "Username", 14, TextMuted, TextAnchor.MiddleLeft);
        _usernameInput = CreateInputField("UsernameInput", prt, "Enter username");

        // --- Password label + input ---
        CreateText("PasswordLabel", prt, "Password", 14, TextMuted, TextAnchor.MiddleLeft);
        _passwordInput = CreateInputField("PasswordInput", prt, "Enter password");
        _passwordInput.inputType = TMP_InputField.InputType.Password;

        // --- Action button (Login / Register) ---
        _actionButton = CreateButton("ActionButton", prt, "LOGIN", ButtonColor);
        SetLayoutElement(_actionButton.gameObject, minHeight: 44);
        _actionButton.onClick.AddListener(OnActionButtonClicked);

        // --- Toggle (switch between login and register) ---
        var toggleRow = new GameObject("ToggleRow", typeof(RectTransform));
        toggleRow.transform.SetParent(prt, false);
        var trt = (RectTransform)toggleRow.transform;
        var tlg = toggleRow.AddComponent<HorizontalLayoutGroup>();
        tlg.childAlignment = TextAnchor.MiddleCenter;
        tlg.spacing = 8;
        tlg.childControlWidth = true;
        tlg.childForceExpandWidth = true;

        _toggleText = CreateText("ToggleLabel", trt, "Don't have an account? Register", 13, TextMuted, TextAnchor.MiddleCenter);
        _toggleButton = CreateButton("ToggleButton", trt, "Register", new Color(0.2f, 0.25f, 0.3f, 1f));
        _toggleButton.onClick.AddListener(ToggleMode);

        // --- Logout button (hidden until logged in) ---
        _logoutButton = CreateButton("LogoutButton", prt, "LOGOUT", new Color(0.5f, 0.2f, 0.2f, 1f));
        SetLayoutElement(_logoutButton.gameObject, minHeight: 36);
        _logoutButton.onClick.AddListener(OnLogoutClicked);
        _logoutButton.gameObject.SetActive(false);

        // --- Spacer to push content up ---
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(prt, false);
        SetLayoutElement(spacer, flexibleHeight: 1);
    }

    private TMP_Text CreateText(string name, Transform parent, string content, float fontSize, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = ConvertAnchor(anchor);
        txt.raycastTarget = false;
        if (fontAsset != null) txt.font = fontAsset;
        return txt;
    }

    private TMP_InputField CreateInputField(string name, Transform parent, string placeholder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(0, 40);

        var bg = go.AddComponent<Image>();
        bg.color = InputBgColor;
        bg.raycastTarget = true;

        var input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = bg;

        // Text area
        var textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(go.transform, false);
        var taRt = (RectTransform)textArea.transform;
        taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10, 6); taRt.offsetMax = new Vector2(-10, -6);
        var taRectMask = textArea.AddComponent<RectMask2D>();
        taRectMask.padding = new Vector4(0, 0, 0, 0);

        // Placeholder
        var phGo = new GameObject("Placeholder", typeof(RectTransform));
        phGo.transform.SetParent(textArea.transform, false);
        var phRt = (RectTransform)phGo.transform;
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = Vector2.zero; phRt.offsetMax = Vector2.zero;
        var ph = phGo.AddComponent<TextMeshProUGUI>();
        ph.text = placeholder;
        ph.fontSize = 16;
        ph.color = new Color(0.5f, 0.5f, 0.55f, 0.6f);
        ph.fontStyle = FontStyles.Italic;
        ph.raycastTarget = false;
        if (fontAsset != null) ph.font = fontAsset;

        // Actual text
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(textArea.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = 16;
        text.color = TextPrimary;
        text.raycastTarget = false;
        if (fontAsset != null) text.font = fontAsset;

        input.textViewport = taRt;
        input.textComponent = text;
        input.placeholder = ph;
        input.contentType = TMP_InputField.ContentType.Standard;

        return input;
    }

    private Button CreateButton(string name, Transform parent, string label, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(0, 40);

        var img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.pressedColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f, 1f);
        btn.colors = colors;

        // Label
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lRt = (RectTransform)labelGo.transform;
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
        var labelTxt = labelGo.AddComponent<TextMeshProUGUI>();
        labelTxt.text = label;
        labelTxt.fontSize = 16;
        labelTxt.color = Color.white;
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.raycastTarget = false;
        if (fontAsset != null) labelTxt.font = fontAsset;

        return btn;
    }

    private void SetLayoutElement(GameObject go, float minHeight = -1, float flexibleHeight = -1)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (minHeight >= 0) le.minHeight = minHeight;
        if (flexibleHeight >= 0) le.flexibleHeight = flexibleHeight;
    }

    private TextAlignmentOptions ConvertAnchor(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }

    // =========================================================================
    //  Logic
    // =========================================================================

    private IEnumerator BindAndShow()
    {
        // Wait for PlayFabManager
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
            _panel.SetActive(false);
            yield break;
        }

        pm.OnLoginSuccess += HandleLoginSuccess;
        pm.OnLoginError += HandleLoginError;

        // If already logged in, hide panel
        if (pm.IsLoggedIn)
        {
            _panel.SetActive(false);
            yield break;
        }

        _panel.SetActive(true);
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
            _actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "CREATE ACCOUNT";
            _toggleText.text = "Already have an account?";
            _toggleButton.GetComponentInChildren<TextMeshProUGUI>().text = "Login";
        }
        else
        {
            _titleText.text = "LOGIN";
            _actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "LOGIN";
            _toggleText.text = "Don't have an account?";
            _toggleButton.GetComponentInChildren<TextMeshProUGUI>().text = "Register";
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
        _panel.SetActive(true);
        _logoutButton.gameObject.SetActive(false);
        ShowStatus("Logged out.", TextMuted);
    }

    private void HandleLoginSuccess(string username)
    {
        ShowStatus($"Welcome, {username}!", SuccessColor);
        // Hide panel after a short delay so the user sees the success message
        StartCoroutine(HideAfterDelay(0.8f));
        _logoutButton.gameObject.SetActive(true);
    }

    private void HandleLoginError(string error)
    {
        ShowStatus(error, ErrorColor);
        SetBusy(false);
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _panel.SetActive(false);
    }

    private void ShowStatus(string message, Color color)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
            _statusText.color = color;
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        if (_actionButton != null)
            _actionButton.interactable = !busy;
    }
}
