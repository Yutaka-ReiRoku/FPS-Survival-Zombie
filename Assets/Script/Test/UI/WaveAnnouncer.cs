using UnityEngine;
using UnityEngine.UIElements;

public class WaveAnnouncer : MonoBehaviour
{
    [Tooltip("Score bonus per cleared wave (mirrors WaveManager.NextWave: wave*500).")]
    public int bonusPerWave = 500;
    public float fadeIn = 0.3f, hold = 2.2f, fadeOut = 0.7f;

    private VisualElement _root;
    private Label _title;
    private Label _sub;
    private int _lastWave = -1;

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;
        _root = doc.rootVisualElement.Q("WaveAnnouncer");
        if (_root == null) return;
        _title = _root.Q<Label>("WaveTitle");
        _sub = _root.Q<Label>("WaveSub");
        _root.style.display = DisplayStyle.None;
    }

    private void Update()
    {
        var wm = WaveManager.Instance;
        if (wm == null) return;

        if (wm.currentWave != _lastWave)
        {
            bool first = _lastWave < 0;
            _lastWave = wm.currentWave;
            _title.text = "WAVE " + wm.currentWave;
            _sub.text = first ? "SURVIVE" : "WAVE CLEARED   +" + (wm.currentWave * bonusPerWave);
            Show();
        }
    }

    private void Show()
    {
        _root.style.display = DisplayStyle.Flex;
        _root.schedule.Execute(() => {
            _root.EnableInClassList("wave-visible", true);
        });

        float totalVisible = fadeIn + hold;
        _root.schedule.Execute(() => {
            _root.EnableInClassList("wave-visible", false);
        }).StartingIn(Mathf.RoundToInt(totalVisible * 1000f));

        _root.schedule.Execute(() => {
            _root.style.display = DisplayStyle.None;
        }).StartingIn(Mathf.RoundToInt((totalVisible + fadeOut) * 1000f));
    }
}
