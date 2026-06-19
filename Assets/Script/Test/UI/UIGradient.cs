using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vertical two-color gradient for any uGUI Graphic (Image/Text).
/// Overwrites vertex colors top->bottom. CanvasGroup handles fade independently.
/// </summary>
[AddComponentMenu("UI/Effects/UI Gradient (Vertical)")]
[DisallowMultipleComponent]
public class UIGradient : BaseMeshEffect
{
    public Color topColor = new Color(0.25f, 0.30f, 0.36f, 1f);
    public Color bottomColor = new Color(0.12f, 0.16f, 0.20f, 1f);

    private static readonly List<UIVertex> _verts = new List<UIVertex>();

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        _verts.Clear();
        vh.GetUIVertexStream(_verts);

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < _verts.Count; i++)
        {
            float y = _verts[i].position.y;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
        float height = Mathf.Max(0.0001f, maxY - minY);

        for (int i = 0; i < _verts.Count; i++)
        {
            UIVertex v = _verts[i];
            float t = (v.position.y - minY) / height; // 0 = bottom, 1 = top
            v.color = Color.Lerp(bottomColor, topColor, t);
            _verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(_verts);
    }

    public void SetColors(Color top, Color bottom)
    {
        topColor = top;
        bottomColor = bottom;
        if (graphic != null)
            graphic.SetVerticesDirty();
    }
}
