using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class EpilogueSlide : MonoBehaviour
{
    [Header("Content")]
    [TextArea(3, 8)]
    public string bodyText =
        "Dịch bệnh đã được kiểm soát, đã tìm ra phương thuốc, nhưng danh tính người đem " +
        "phương thuốc về cho các nhà khoa học vẫn là một ẩn số.";

    [Tooltip("Optional illustration shown above the text. Leave empty for now — assign later.")]
    public Sprite illustration;

    [Header("Timing")]
    public float fadeIn = 1f;
    public float hold = 6f;
    public float fadeOut = 1f;

    [Header("Visuals")]
    public Color backgroundColor = Color.black;
    public Color textColor = new Color(0.92f, 0.92f, 0.92f, 1f);

    private bool _played;
    private VisualElement _root;
    private VisualElement _illustrationEl;
    private Label _text;
    private GameObject _docGO;

    public void Play(Action onComplete = null)
    {
        if (_played) { onComplete?.Invoke(); return; }
        _played = true;
        StartCoroutine(PlayRoutine(onComplete));
    }

    private IEnumerator PlayRoutine(Action onComplete)
    {
        Build();

        float prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        _root.style.opacity = 1f;
        yield return new WaitForSecondsRealtime(fadeIn + hold);
        _root.style.opacity = 0f;
        yield return new WaitForSecondsRealtime(fadeOut);

        Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
        Destroy(_docGO);

        onComplete?.Invoke();
    }

    private void Build()
    {
        _docGO = new GameObject("EpilogueSlide_Doc", typeof(UIDocument));
        _docGO.transform.SetParent(transform, false);
        var doc = _docGO.GetComponent<UIDocument>();
        doc.sortingOrder = 1500;
        var sheet = Resources.Load<StyleSheet>("EpilogueSlide");
        if (sheet != null) doc.rootVisualElement.styleSheets.Add(sheet);

        _root = new VisualElement();
        _root.name = "EpilogueRoot";
        _root.style.position = Position.Absolute;
        _root.style.left = 0;
        _root.style.right = 0;
        _root.style.top = 0;
        _root.style.bottom = 0;
        _root.style.opacity = 0f;
        _root.pickingMode = PickingMode.Ignore;

        var bg = new VisualElement();
        bg.name = "Background";
        bg.style.position = Position.Absolute;
        bg.style.left = 0;
        bg.style.right = 0;
        bg.style.top = 0;
        bg.style.bottom = 0;
        bg.style.backgroundColor = backgroundColor;
        _root.Add(bg);

        var container = new VisualElement();
        container.name = "Content";
        container.style.position = Position.Absolute;
        container.style.left = Length.Percent(50);
        container.style.top = Length.Percent(50);
        container.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
        container.style.alignItems = Align.Center;
        container.style.justifyContent = Justify.Center;
        container.style.width = Length.Percent(80);
        _root.Add(container);

        _illustrationEl = new VisualElement();
        _illustrationEl.name = "Illustration";
        _illustrationEl.style.width = 720;
        _illustrationEl.style.height = 420;
        _illustrationEl.style.marginBottom = 30;
        if (illustration != null)
        {
            _illustrationEl.style.backgroundImage = new StyleBackground(illustration);
            _illustrationEl.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        }
        container.Add(_illustrationEl);

        _text = new Label(bodyText);
        _text.name = "BodyText";
        _text.style.fontSize = 34;
        _text.style.color = textColor;
        _text.style.unityTextAlign = TextAnchor.MiddleCenter;
        _text.style.whiteSpace = WhiteSpace.Normal;
        container.Add(_text);

        doc.rootVisualElement.Add(_root);
    }
}
