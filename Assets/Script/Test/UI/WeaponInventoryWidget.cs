using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Custom weapon-inventory slot strip on the unified HUD, fed by CowsinsHUDAdapter
/// (WeaponSlots / SelectedWeaponIndex / OnInventoryStructureChanged / OnWeaponSelected).
/// Self-builds a row of slots (icon + index); the selected slot is highlighted
/// (accent tint + slight scale). Empty slots are dimmed. The component stays active
/// (subscribed); slot count is rebuilt as inventory size changes. Engine-free.
/// </summary>
public class WeaponInventoryWidget : MonoBehaviour
{
    public float slotSize = 64f;
    public float spacing = 8f;

    private CowsinsHUDAdapter _adapter;
    private RectTransform _row;
    private readonly List<RectTransform> _slots = new List<RectTransform>();
    private readonly List<Image> _icons = new List<Image>();
    private readonly List<Image> _bgs = new List<Image>();
    private Color _slotBg = new Color(0.137f, 0.165f, 0.2f, 0.85f);
    private Color _selected = new Color(0.85f, 0.78f, 0.45f, 1f);
    private Color _emptyTint = new Color(1f, 1f, 1f, 0.12f);

    private void Awake()
    {
        var th = UITheme.Active;
        if (th != null) { _slotBg = th.surfaceTop; _selected = th.accent; }
        _row = (RectTransform)transform;
        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false; hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.spacing = spacing;
    }

    private void OnEnable() { StartCoroutine(Bind()); }

    private IEnumerator Bind()
    {
        float timeout = 12f;
        while (CowsinsHUDAdapter.Instance == null && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
        _adapter = CowsinsHUDAdapter.Instance;
        if (_adapter == null) yield break;
        _adapter.OnInventoryStructureChanged += Rebuild;
        _adapter.OnWeaponSelected += HandleSelected;
        Rebuild();
    }

    private void OnDisable()
    {
        if (_adapter != null)
        {
            _adapter.OnInventoryStructureChanged -= Rebuild;
            _adapter.OnWeaponSelected -= HandleSelected;
        }
        StopAllCoroutines();
    }

    private void Rebuild()
    {
        if (_adapter == null) return;
        var slots = _adapter.WeaponSlots;
        int n = slots != null ? slots.Length : 0;
        EnsureSlots(n);
        for (int i = 0; i < _slots.Count; i++)
        {
            bool used = i < n;
            _slots[i].gameObject.SetActive(used);
            if (!used) continue;
            var info = slots[i];
            _icons[i].sprite = info.icon;
            _icons[i].enabled = info.icon != null;
            _icons[i].color = info.occupied ? Color.white : _emptyTint;
            _bgs[i].color = _slotBg;
        }
        HandleSelected(_adapter.SelectedWeaponIndex);
    }

    private void HandleSelected(int index)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (!_slots[i].gameObject.activeSelf) continue;
            bool sel = i == index;
            _bgs[i].color = sel ? _selected : _slotBg;
            _slots[i].localScale = sel ? Vector3.one * 1.1f : Vector3.one;
        }
    }

    private void EnsureSlots(int count)
    {
        while (_slots.Count < count)
        {
            var slot = NewChild("Slot", _row);
            slot.sizeDelta = new Vector2(slotSize, slotSize);
            var le = slot.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = slotSize; le.preferredHeight = slotSize;
            var bg = slot.gameObject.AddComponent<Image>(); bg.color = _slotBg; bg.raycastTarget = false;
            var iconRT = NewChild("Icon", slot);
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(8, 8); iconRT.offsetMax = new Vector2(-8, -8);
            var icon = iconRT.gameObject.AddComponent<Image>(); icon.raycastTarget = false; icon.preserveAspect = true;
            var idxRT = NewChild("Index", slot);
            idxRT.anchorMin = new Vector2(0, 0); idxRT.anchorMax = new Vector2(0, 0); idxRT.pivot = new Vector2(0, 0);
            idxRT.anchoredPosition = new Vector2(4, 2); idxRT.sizeDelta = new Vector2(20, 20);
            var idx = idxRT.gameObject.AddComponent<TextMeshProUGUI>();
            idx.text = (_slots.Count + 1).ToString(); idx.fontSize = 16; idx.raycastTarget = false;
            PremiumUITheme.StyleValue(idx);
            _slots.Add(slot); _icons.Add(icon); _bgs.Add(bg);
        }
    }

    private RectTransform NewChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
}
