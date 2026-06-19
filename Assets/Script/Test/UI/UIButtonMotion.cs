using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Smooth hover/press scale feedback for a uGUI Selectable, using unscaled time
/// so it works while paused (timeScale = 0). Pointer events require an EventSystem
/// with the active input module and a raycastable target Graphic.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIButtonMotion : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.06f;
    public float pressScale = 0.95f;
    public float smoothTime = 0.08f;

    private RectTransform rt;
    private Selectable selectable;
    private Vector3 baseScale = Vector3.one;
    private bool pointerInside;
    private bool pointerDown;

    private void Awake()
    {
        rt = (RectTransform)transform;
        selectable = GetComponent<Selectable>();
        baseScale = rt.localScale;
    }

    private void OnEnable()
    {
        pointerInside = false;
        pointerDown = false;
        if (rt != null) rt.localScale = baseScale;
    }

    private void OnDisable()
    {
        if (rt != null) rt.localScale = baseScale;
    }

    private bool Interactable => selectable == null || selectable.IsInteractable();

    private void Update()
    {
        float target = 1f;
        if (Interactable)
        {
            if (pointerDown && pointerInside) target = pressScale;
            else if (pointerInside) target = hoverScale;
        }
        Vector3 desired = baseScale * target;
        float k = (smoothTime <= 0f) ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothTime);
        rt.localScale = Vector3.Lerp(rt.localScale, desired, k);
    }

    public void OnPointerEnter(PointerEventData e) { pointerInside = true; }
    public void OnPointerExit(PointerEventData e) { pointerInside = false; }
    public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pointerDown = true; }
    public void OnPointerUp(PointerEventData e) { pointerDown = false; }
}
