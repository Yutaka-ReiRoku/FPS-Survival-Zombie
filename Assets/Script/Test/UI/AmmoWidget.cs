using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class AmmoWidget : MonoBehaviour
{
    [Header("Style")]
    [SerializeField] private string _infiniteSymbol = "\u221E";
    [SerializeField] private Color _normalColor = new Color(0.92f, 0.88f, 0.78f, 1f);
    [SerializeField] private Color _lowColor = new Color(0.85f, 0.35f, 0.15f, 1f);
    [Range(0f, 1f)] [SerializeField] private float _lowFraction = 0.34f;
    [SerializeField] private float _punchScale = 1.14f;
    [SerializeField] private float _punchDuration = 0.12f;

    private VisualElement _root;
    private Label _ammoValue;
    private Label _ammoReserve;
    private VisualElement _heatBarTrack;
    private VisualElement _heatBarFill;
    private VisualElement _ammoPunchRoot;

    private Vector3 _home = Vector3.one;
    private Coroutine _punch;

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) { enabled = false; return; }
        _root = doc.rootVisualElement.Q("AmmoCluster");
        if (_root == null) { enabled = false; return; }
        _ammoValue = _root.Q<Label>("AmmoValue");
        _ammoReserve = _root.Q<Label>("AmmoReserve");
        _heatBarTrack = _root.Q("HeatBarTrack");
        _heatBarFill = _root.Q("HeatBarFill");
        _ammoPunchRoot = _root.Q("AmmoPunchRoot");
    }

    private void OnEnable() { StartCoroutine(Bind()); }

    private void OnDisable()
    {
        var a = CowsinsHUDAdapter.Instance;
        if (a != null) { a.OnAmmoChanged -= OnAmmo; a.OnHeatChanged -= OnHeat; a.OnFired -= OnFired; }
        StopAllCoroutines();
    }

    private IEnumerator Bind()
    {
        var th = UITheme.Active;
        if (th != null) { _normalColor = th.ammoNormal; _lowColor = th.ammoLow; _punchScale = th.ammoPunchScale; }
        while (CowsinsHUDAdapter.Instance == null) yield return null;
        var a = CowsinsHUDAdapter.Instance;
        a.OnAmmoChanged += OnAmmo;
        a.OnHeatChanged += OnHeat;
        a.OnFired += OnFired;
        OnAmmo(a.Ammo, a.Reserve);
        OnHeat(a.Heat);
    }

    private void OnAmmo(int mag, int reserve)
    {
        var a = CowsinsHUDAdapter.Instance;
        if (_ammoValue != null)
        {
            _ammoValue.text = mag.ToString();
            bool low = a != null && a.MagazineSize > 0 && mag <= Mathf.CeilToInt(a.MagazineSize * _lowFraction);
            _ammoValue.style.color = low ? _lowColor : _normalColor;
        }
        if (_ammoReserve != null)
            _ammoReserve.text = (a != null && !a.LimitedReserve) ? _infiniteSymbol : reserve.ToString();
    }

    private void OnHeat(float heat)
    {
        if (_heatBarTrack == null) return;
        bool show = heat > 0.001f;
        _heatBarTrack.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (_heatBarFill != null)
        {
            _heatBarFill.style.width = Length.Percent(Mathf.Clamp01(heat) * 100f);
            _heatBarFill.style.backgroundColor = Color.Lerp(_normalColor, _lowColor, Mathf.Clamp01(heat));
        }
    }

    private void OnFired()
    {
        if (_ammoPunchRoot == null || _punch != null) return;
        _punch = StartCoroutine(Punch());
    }

    private IEnumerator Punch()
    {
        float t = 0f;
        while (t < _punchDuration)
        {
            float p = t / _punchDuration;
            float s = Mathf.Lerp(_punchScale, 1f, p);
            _ammoPunchRoot.style.scale = new Scale(new Vector3(s, s, 1f));
            t += Time.deltaTime;
            yield return null;
        }
        _ammoPunchRoot.style.scale = new Scale(_home);
        _punch = null;
    }
}
