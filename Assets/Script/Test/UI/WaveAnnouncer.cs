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

    private enum AnimState { Idle, FadeIn, Hold, FadeOut }
    private AnimState _state = AnimState.Idle;
    private float _timer;

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

        if (_state == AnimState.FadeIn)
        {
            _timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_timer / fadeIn);
            _root.style.opacity = t;
            if (t >= 1f)
            {
                _root.style.opacity = 1f;
                _state = AnimState.Hold;
                _timer = 0f;
            }
        }
        else if (_state == AnimState.Hold)
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= hold)
            {
                _state = AnimState.FadeOut;
                _timer = 0f;
            }
        }
        else if (_state == AnimState.FadeOut)
        {
            _timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_timer / fadeOut);
            _root.style.opacity = 1f - t;
            if (t >= 1f)
            {
                _root.style.opacity = 0f;
                _state = AnimState.Idle;
                _root.style.display = DisplayStyle.None;
            }
        }
    }

    private void Show()
    {
        _root.style.display = DisplayStyle.Flex;
        _root.style.opacity = 0f;
        _state = AnimState.FadeIn;
        _timer = 0f;
    }
}
