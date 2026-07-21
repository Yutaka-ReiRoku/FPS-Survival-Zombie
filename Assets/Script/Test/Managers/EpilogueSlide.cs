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

        // Copy panelSettings from an existing screen-space UIDocument so the panel actually renders.
        // Must filter out WorldSpacePanelSettings — see UIPanelSettingsUtil for details.
        var ssDoc = UIPanelSettingsUtil.FindScreenSpaceUIDocument(doc);
        if (ssDoc != null)
        {
            doc.panelSettings = ssDoc.panelSettings;
        }
        if (doc.panelSettings == null)
        {
            doc.panelSettings = UIPanelSettingsUtil.FindScreenSpacePanelSettingsAsset();
        }

        var asset = Resources.Load<VisualTreeAsset>("EpilogueSlide");
        if (asset == null) return;
        asset.CloneTree(doc.rootVisualElement);

        _root = doc.rootVisualElement.Q("EpilogueRoot");
        if (_root == null) return;
        _root.pickingMode = PickingMode.Ignore;
        _root.style.opacity = 0f;

        var bg = _root.Q("Background");
        if (bg != null) bg.style.backgroundColor = backgroundColor;

        _illustrationEl = _root.Q("Illustration");
        if (_illustrationEl != null && illustration != null)
        {
            _illustrationEl.style.backgroundImage = new StyleBackground(illustration);
            _illustrationEl.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        }

        _text = _root.Q<Label>("BodyText");
        if (_text != null)
        {
            _text.text = bodyText;
            _text.style.color = textColor;
        }
    }
}
