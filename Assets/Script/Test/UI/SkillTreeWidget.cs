using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using cowsins;

public class SkillTreeWidget : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.Tab;

    private UIDocument _doc;
    private VisualElement _root;
    private VisualElement _card;
    private Label _sp;
    private readonly VisualElement[] _nodeContainer = new VisualElement[Trees];
    private readonly VisualElement[,] _nodes = new VisualElement[Trees, NodesPerTree];
    private readonly VisualElement[,] _lines = new VisualElement[Trees, NodesPerTree - 1];
    private readonly Label[] _cost = new Label[Trees];
    private readonly Label[] _next = new Label[Trees];
    private SkillTreeManager _mgr;
    private PlayerControl _playerControl;
    private Transform _canvasRoot;
    private bool _open;
    private bool _initialized;

    private static readonly string[] TreeNames = { "MOVEMENT", "AIM", "INTELLIGENCE" };
    private const int NodesPerTree = 5;
    private const int Trees = 3;

    public bool IsOpen => _open;

    private void OnEnable()
    {
        if (!_initialized) Initialize();
    }

    private void Initialize()
    {
        _doc = GetComponent<UIDocument>();
        if (_doc == null) return;
        _root = _doc.rootVisualElement.Q("SkillTreeWidget");
        if (_root == null) return;

        _card = _root.Q("card");
        _sp = _root.Q<Label>("sp");

        for (int t = 0; t < Trees; t++)
        {
            _nodeContainer[t] = _root.Q("nodes" + t);
            _cost[t] = _root.Q<Label>("cost" + t);
            _next[t] = _root.Q<Label>("next" + t);
        }

        BuildNodes();
        _initialized = true;
    }

    private void BuildNodes()
    {
        const float containerTop = 186f;
        const float halfCardH = 360f;
        const float halfNode = 30f;
        const float halfLine = 10f;

        for (int t = 0; t < Trees; t++)
        {
            for (int n = 0; n < NodesPerTree; n++)
            {
                var node = new VisualElement();
                node.AddToClassList("skill-node");
                node.AddToClassList("locked");
                node.style.position = Position.Absolute;
                node.style.left = 0;
                node.style.top = halfCardH - NodeY[n] - halfNode - containerTop;
                node.style.width = 60;
                node.style.height = 60;
                _nodeContainer[t].Add(node);

                var label = new Label((n + 1).ToString());
                label.AddToClassList("node-label");
                node.Add(label);

                int ti = t;
                node.RegisterCallback<ClickEvent>(_ => TryUpgrade(ti));

                if (n < NodesPerTree - 1)
                {
                    var line = new VisualElement();
                    line.AddToClassList("skill-line");
                    line.style.position = Position.Absolute;
                    line.style.left = 27;
                    line.style.top = halfCardH - LineY[n] - halfLine - containerTop;
                    line.style.width = 6;
                    line.style.height = 20;
                    _nodeContainer[t].Add(line);
                    _lines[t, n] = line;
                }

                _nodes[t, n] = node;
            }
        }
    }

    private static readonly float[] NodeY = { 130f, 50f, -30f, -110f, -190f };
    private static readonly float[] LineY = { 90f, 10f, -70f, -150f };

    private void Start()
    {
        _canvasRoot = GameObject.Find("GameUICanvas")?.transform;
        if (_mgr == null) _mgr = FindAnyObjectByType<SkillTreeManager>();
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerControl = player.GetComponentInChildren<PlayerControl>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
            bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
            bool journalOpen = JournalUI.Instance != null && JournalUI.Instance.IsOpen;
            if (_open) Close();
            else if (!gameOver && !pauseOpen && !journalOpen) Open();
        }
        if (_open) Refresh();
    }

    private void Open()
    {
        if (!_initialized) return;
        _open = true;
        _root.AddToClassList("open");
        Time.timeScale = 0f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        if (_playerControl != null)
            _playerControl.LoseControl();
        PauseManager.SetHUDVisible(_canvasRoot != null ? _canvasRoot : transform, false);
        Refresh();
    }

    public void Close()
    {
        if (!_open) return;
        _open = false;
        _root.RemoveFromClassList("open");
        bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
        if (!pauseOpen && !gameOver)
        {
            Time.timeScale = 1f;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            if (_playerControl != null)
                _playerControl.GrantControl();
            PauseManager.SetHUDVisible(_canvasRoot != null ? _canvasRoot : transform, true);
        }
    }

    private void TryUpgrade(int tree)
    {
        if (_mgr == null) return;
        bool ok = tree == 0 ? _mgr.UpgradeMovement() : tree == 1 ? _mgr.UpgradeAim() : _mgr.UpgradeIntelligence();
        if (ok) Refresh();
    }

    private void Refresh()
    {
        int sp = _mgr != null ? _mgr.CurrentSkillPoints : 0;
        _sp.text = "SKILL POINTS : " + sp;

        for (int t = 0; t < Trees; t++)
        {
            int lvl = _mgr == null ? 0 : (t == 0 ? _mgr.MovementLevel : t == 1 ? _mgr.AimLevel : _mgr.IntelligenceLevel);
            int cost = _mgr == null ? 0 : (t == 0 ? _mgr.NextMovementCost : t == 1 ? _mgr.NextAimCost : _mgr.NextIntelligenceCost);
            bool maxed = lvl >= SkillTreeManager.MaxLevel;
            bool canAfford = !maxed && _mgr != null && sp >= cost;

            for (int n = 0; n < NodesPerTree; n++)
            {
                var node = _nodes[t, n];
                bool unlocked = n < lvl;
                bool isNext = n == lvl && !maxed;

                node.ClearClassList();
                node.AddToClassList("skill-node");
                if (unlocked)
                    node.AddToClassList("unlocked");
                else if (isNext && canAfford)
                    node.AddToClassList("available");
                else
                    node.AddToClassList("locked");

                node.SetEnabled(isNext && canAfford);
            }

            for (int n = 0; n < NodesPerTree - 1; n++)
            {
                var line = _lines[t, n];
                bool upperUnlocked = (n + 1) < lvl;
                bool upperIsNext = (n + 1) == lvl && !maxed;

                if (upperUnlocked)
                    line.style.backgroundColor = new StyleColor(new Color(0.85f, 0.78f, 0.45f, 1f));
                else if (upperIsNext && canAfford)
                    line.style.backgroundColor = new StyleColor(new Color(0.31f, 0.878f, 0.541f, 1f));
                else if (upperIsNext)
                    line.style.backgroundColor = new StyleColor(new Color(0.62f, 0.66f, 0.72f, 1f));
                else
                    line.style.backgroundColor = new StyleColor(new Color(0.62f, 0.66f, 0.72f, 0.3f));
            }

            var costLabel = _cost[t];
            costLabel.text = maxed ? "MAXED" : ("NEXT  " + cost + " SP");
            costLabel.ClearClassList();
            costLabel.AddToClassList("cost-label");
            costLabel.AddToClassList(maxed ? "accent" : (canAfford ? "ready" : "muted"));

            _next[t].text = maxed ? "\u2014" : SkillTreeManager.GetNodeDescription(t, lvl + 1);
            _next[t].style.color = maxed
                ? new StyleColor(new Color(0.62f, 0.66f, 0.72f, 1f))
                : (canAfford ? new StyleColor(new Color(0.31f, 0.878f, 0.541f, 1f)) : new StyleColor(new Color(0.62f, 0.66f, 0.72f, 1f)));
        }
    }
}
