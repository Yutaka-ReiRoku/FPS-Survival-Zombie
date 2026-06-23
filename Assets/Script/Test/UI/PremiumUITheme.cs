using UnityEngine;
using TMPro;

/// <summary>
/// Premium TMP styling helpers: display/header/body fonts + material effects
/// (face gradient, dark outline, soft underlay drop-shadow, subtle glow).
/// Verified against TMP_SDF.shader property names + UNDERLAY_ON/GLOW_ON keywords.
/// </summary>
public static class PremiumUITheme
{
    public const string FULL_SDF = "TextMeshPro/Distance Field";
    const string Root = "Assets/Others/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/";

    public static TMP_FontAsset GetDisplay() { var t = UITheme.Active; if (t != null && t.displayFont != null) return t.displayFont; return Load("Anton SDF"); }
    public static TMP_FontAsset GetHeader()  { var t = UITheme.Active; if (t != null && t.headerFont != null) return t.headerFont; return Load("Oswald Bold SDF"); }
    public static TMP_FontAsset GetBody()    { var t = UITheme.Active; if (t != null && t.bodyFont != null) return t.bodyFont; return Load("Roboto-Bold SDF"); }

    static TMP_FontAsset Load(string name)
    {
#if UNITY_EDITOR
        var fa = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + name + ".asset");
        if (fa != null) return fa;
#endif
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/" + name);
    }

    static Material Inst(TMP_Text t)
    {
        Material m = t.fontMaterial; // per-object instance
        var full = Shader.Find(FULL_SDF);
        if (full != null && m.shader != full) m.shader = full;
        return m;
    }

    /// Big hero title: vertical face gradient + dark outline + soft drop shadow + subtle glow.
    public static void StyleTitle(TMP_Text t, Color top, Color bottom, Color glow, bool useGlow)
    {
        var f = GetDisplay(); if (f != null) t.font = f;
        t.enableVertexGradient = true;
        t.colorGradient = new VertexGradient(top, top, bottom, bottom);
        Material m = Inst(t);
        m.SetColor("_FaceColor", Color.white);
        m.SetFloat("_FaceDilate", 0f);
        m.SetColor("_OutlineColor", new Color(0.165f, 0.024f, 0.031f, 1f));
        m.SetFloat("_OutlineWidth", 0.16f);
        m.SetFloat("_OutlineSoftness", 0.05f);
        m.EnableKeyword("UNDERLAY_ON");
        m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.78f));
        m.SetFloat("_UnderlayOffsetX", 0.6f);
        m.SetFloat("_UnderlayOffsetY", -0.7f);
        m.SetFloat("_UnderlayDilate", 0.15f);
        m.SetFloat("_UnderlaySoftness", 0.35f);
        if (useGlow)
        {
            m.EnableKeyword("GLOW_ON");
            m.SetColor("_GlowColor", glow);
            m.SetFloat("_GlowOuter", 0.18f);
            m.SetFloat("_GlowInner", 0.05f);
            m.SetFloat("_GlowPower", 0.5f);
        }
        else m.DisableKeyword("GLOW_ON");
        t.UpdateMeshPadding();
        t.havePropertiesChanged = true;
    }

    /// Header (Oswald) with light outline + shadow.
    public static void StyleHeader(TMP_Text t)
    {
        var f = GetHeader(); if (f != null) t.font = f;
        ApplyLabel(t, 0.12f, 0.6f);
    }

    /// Button label (Oswald): light outline + light shadow for legibility on gradients.
    public static void StyleLabel(TMP_Text t)
    {
        var f = GetHeader(); if (f != null) t.font = f;
        ApplyLabel(t, 0.13f, 0.6f);
    }

    /// Numeric value (Roboto): subtle shadow only.
    public static void StyleValue(TMP_Text t)
    {
        var f = GetBody(); if (f != null) t.font = f;
        ApplyLabel(t, 0.0f, 0.5f);
    }

    static void ApplyLabel(TMP_Text t, float outline, float shadowAlpha)
    {
        t.enableVertexGradient = false;
        Material m = Inst(t);
        m.SetColor("_FaceColor", Color.white);
        m.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 1f));
        m.SetFloat("_OutlineWidth", outline);
        m.SetFloat("_OutlineSoftness", 0.05f);
        m.EnableKeyword("UNDERLAY_ON");
        m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, shadowAlpha));
        m.SetFloat("_UnderlayOffsetX", 0.4f);
        m.SetFloat("_UnderlayOffsetY", -0.4f);
        m.SetFloat("_UnderlayDilate", 0f);
        m.SetFloat("_UnderlaySoftness", 0.3f);
        m.DisableKeyword("GLOW_ON");
        t.UpdateMeshPadding();
        t.havePropertiesChanged = true;
    }
}
