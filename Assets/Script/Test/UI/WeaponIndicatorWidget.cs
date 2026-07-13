using UnityEngine;
using UnityEngine.UIElements;

public class WeaponIndicatorWidget : MonoBehaviour
{
    public float fadeSpeed = 6f;

    private Label _weaponName;
    private VisualElement _weaponIcon;
    private VisualElement _root;
    private float _currentOpacity = 1f;
    private float _targetOpacity = 1f;

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;
        _root = root.Q("WeaponIndicator");
        _weaponName = _root.Q<Label>("WeaponName");
        _weaponIcon = _root.Q("WeaponIcon");
    }

    private void OnEnable()
    {
        var a = CowsinsHUDAdapter.Instance;
        if (a == null) return;
        a.OnWeaponChanged += OnWeapon;
        OnWeapon(a.WeaponName, a.WeaponIcon);
    }

    private void OnDisable()
    {
        var a = CowsinsHUDAdapter.Instance;
        if (a != null) a.OnWeaponChanged -= OnWeapon;
    }

    private void OnWeapon(string n, Sprite icon)
    {
        _weaponName.text = string.IsNullOrEmpty(n) ? string.Empty : n.ToUpperInvariant();
        if (icon != null)
            _weaponIcon.style.backgroundImage = new StyleBackground(icon);
        else
            _weaponIcon.style.backgroundImage = null;

        _currentOpacity = 0.25f;
        _targetOpacity = 1f;
        _root.style.opacity = _currentOpacity;
    }

    private void Update()
    {
        if (_currentOpacity >= _targetOpacity) return;
        _currentOpacity = Mathf.MoveTowards(_currentOpacity, _targetOpacity, fadeSpeed * Time.unscaledDeltaTime);
        _root.style.opacity = _currentOpacity;
    }
}
