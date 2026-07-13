using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CombatFeedbackHUD : MonoBehaviour
{
    public static CombatFeedbackHUD Instance;

    public Camera worldCamera;

    private static readonly string[] _numberStrings = new string[1000];
    private static string GetDamageString(int dmg, bool crit)
    {
        if (dmg >= 0 && dmg < 1000)
        {
            if (_numberStrings[dmg] == null)
                _numberStrings[dmg] = dmg.ToString();
            return crit ? (_numberStrings[dmg] + "!") : _numberStrings[dmg];
        }
        return crit ? (dmg + "!") : dmg.ToString();
    }

    private VisualElement _root, _hitmarker, _dmgContainer, _killContainer;
    private readonly List<VisualElement> _hitBars = new List<VisualElement>();
    private float _hitTimer, _hitDuration = 0.18f;
    private Color _hitNormal = new Color(1f, 1f, 1f, 0.9f);
    private Color _hitKill = new Color(0.95f, 0.32f, 0.27f, 1f);
    private Color _critColor = new Color(1f, 0.78f, 0.2f, 1f);

    private bool _critPending;
    private int _critFrame;
    public void FlagCriticalHit() { _critPending = true; _critFrame = Time.frameCount; }

    private class Dmg { public VisualElement ve; public Label label; public float life; public Vector2 vel; public bool active; }
    private readonly List<Dmg> _pool = new List<Dmg>();
    private const int PoolSize = 28;
    private float _dmgLife = 0.8f;

    private class Kill { public VisualElement entry; public Label label; public float life; public bool active; }
    private readonly List<Kill> _kills = new List<Kill>();
    private readonly List<Kill> _killPool = new List<Kill>();
    private float _killLife = 4f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;
        _root = doc.rootVisualElement.Q("CombatFeedbackHUD");
        if (_root == null) return;
        _hitmarker = _root.Q("Hitmarker");
        _dmgContainer = _root.Q("DamageNumbers");
        _killContainer = _root.Q("Killfeed");

        BuildHitmarkerBars();
        BuildDamagePool();
    }

    private void BuildHitmarkerBars()
    {
        if (_hitmarker.childCount > 0) return;
        var offsets = new Vector2[] { new Vector2(-12, -12), new Vector2(12, -12), new Vector2(-12, 12), new Vector2(12, 12) };
        var angles = new float[] { 45, -45, -45, 45 };
        for (int i = 0; i < 4; i++)
        {
            var bar = new VisualElement();
            bar.AddToClassList("hitmarker-bar");
            bar.style.width = 14;
            bar.style.height = 4;
            bar.style.left = 20 + offsets[i].x - 7;
            bar.style.top = 20 - offsets[i].y - 2;
            bar.style.rotate = new Rotate(Angle.Degrees(angles[i]));
            bar.style.backgroundColor = _hitNormal;
            _hitmarker.Add(bar);
            _hitBars.Add(bar);
        }
    }

    private void BuildDamagePool()
    {
        if (_dmgContainer.childCount > 0) return;
        for (int i = 0; i < PoolSize; i++)
        {
            var ve = new VisualElement();
            ve.AddToClassList("dmg-number");
            ve.style.display = DisplayStyle.None;
            var label = new Label();
            ve.Add(label);
            _dmgContainer.Add(ve);
            _pool.Add(new Dmg { ve = ve, label = label, active = false });
        }
    }

    public void ShowHit(Vector3 worldPos, float damage, bool headshot)
    {
        bool crit = _critPending && Time.frameCount == _critFrame;
        _critPending = false;

        _hitTimer = _hitDuration;
        float scale = crit ? 1.6f : headshot ? 1.4f : 1f;
        _hitmarker.style.scale = new Scale(Vector2.one * scale);
        var col = crit ? _critColor : headshot ? _hitKill : _hitNormal;
        foreach (var bar in _hitBars)
            bar.style.backgroundColor = col;

        var d = GetDmg();
        if (d == null) return;
        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam != null)
        {
            Vector3 sp = cam.WorldToScreenPoint(worldPos + Vector3.up * 1.6f);
            if (sp.z < 0f) { d.active = false; d.ve.style.display = DisplayStyle.None; return; }
            var panel = GetComponent<UIDocument>().rootVisualElement.panel;
            var panelPos = RuntimePanelUtils.ScreenToPanel(panel, sp);
            d.ve.style.left = panelPos.x + Random.Range(-18f, 18f);
            d.ve.style.top = panelPos.y;
        }
        int dmgInt = Mathf.Max(1, Mathf.RoundToInt(damage));
        d.label.text = GetDamageString(dmgInt, crit);
        d.label.style.color = crit ? _critColor : headshot ? _hitKill : Color.white;
        d.label.style.fontSize = crit ? 42 : headshot ? 36 : 28;
        d.ve.style.opacity = 1f;
        d.life = _dmgLife;
        d.vel = new Vector2(Random.Range(-10f, 10f), -90f);
        d.active = true;
        d.ve.style.display = DisplayStyle.Flex;
    }

    public void ShowKill(string name)
    {
        Kill k = null;
        for (int i = 0; i < _killPool.Count; i++)
        {
            if (!_killPool[i].active)
            {
                k = _killPool[i];
                break;
            }
        }

        if (k == null)
        {
            var entry = new VisualElement();
            entry.AddToClassList("killfeed-entry");
            var label = new Label();
            label.AddToClassList("killfeed-entry-label");
            entry.Add(label);
            k = new Kill { entry = entry, label = label, active = true };
            _killPool.Add(k);
        }
        else
        {
            k.active = true;
        }

        k.entry.style.display = DisplayStyle.Flex;
        k.entry.style.opacity = 1f;
        k.entry.RemoveFromHierarchy();
        _killContainer.Insert(0, k.entry);
        k.label.text = "Killed  " + (string.IsNullOrEmpty(name) ? "Zombie" : name);
        k.life = _killLife;
        _kills.Add(k);

        while (_kills.Count > 6)
        {
            var oldest = _kills[0];
            _kills.RemoveAt(0);
            oldest.active = false;
            oldest.entry.style.display = DisplayStyle.None;
        }
    }

    private Dmg GetDmg()
    {
        for (int i = 0; i < _pool.Count; i++)
            if (!_pool[i].active) return _pool[i];
        Dmg oldest = null;
        float min = float.MaxValue;
        foreach (var d in _pool)
            if (d.life < min) { min = d.life; oldest = d; }
        return oldest;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (_critPending && Time.frameCount != _critFrame)
            _critPending = false;

        if (_hitTimer > 0f)
        {
            _hitTimer -= dt;
            _hitmarker.style.opacity = Mathf.Clamp01(_hitTimer / _hitDuration);
            if (_hitTimer <= 0f) _hitmarker.style.opacity = 0f;
        }

        for (int i = 0; i < _pool.Count; i++)
        {
            var d = _pool[i];
            if (!d.active) continue;
            d.life -= dt;
            if (d.life <= 0f)
            {
                d.active = false;
                d.ve.style.opacity = 0f;
                d.ve.style.display = DisplayStyle.None;
                continue;
            }
            d.ve.style.left = d.ve.style.left.value.value + d.vel.x * dt;
            d.ve.style.top = d.ve.style.top.value.value + d.vel.y * dt;
            d.ve.style.opacity = Mathf.Clamp01(d.life / _dmgLife);
        }

        for (int i = _kills.Count - 1; i >= 0; i--)
        {
            var k = _kills[i];
            k.life -= dt;
            if (k.life <= 0f)
            {
                _kills.RemoveAt(i);
                k.active = false;
                k.entry.style.display = DisplayStyle.None;
                continue;
            }
            if (k.life < 0.6f)
                k.entry.style.opacity = Mathf.Clamp01(k.life / 0.6f);
        }
    }
}
