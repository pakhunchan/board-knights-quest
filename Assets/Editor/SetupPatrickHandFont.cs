using UnityEngine;
using UnityEditor;
using TMPro;

[InitializeOnLoad]
public static class SetupPatrickHandFont
{
    private const string TtfPath = "Assets/Fonts/PatrickHand-Regular.ttf";
    private const string FontAssetPath = "Assets/Fonts/PatrickHand-Regular SDF.asset";

    static SetupPatrickHandFont()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Setup PatrickHand Font")]
    public static void Run()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
        {
            // Ensure dynamic mode on existing asset
            if (existing.atlasPopulationMode != TMPro.AtlasPopulationMode.Dynamic)
            {
                existing.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                Debug.Log("[SetupPatrickHandFont] Switched existing asset to Dynamic atlas mode.");
            }
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null)
        {
            Debug.LogWarning("[SetupPatrickHandFont] TTF not found at " + TtfPath);
            return;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            font, 32, 5,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            512, 512);

        if (fontAsset == null)
        {
            Debug.LogError("[SetupPatrickHandFont] Failed to create font asset.");
            return;
        }

        // Dynamic mode: auto-rasterize missing glyphs (arrows, symbols) at runtime
        fontAsset.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = "PatrickHand Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "PatrickHand Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SetupPatrickHandFont] Created font asset at " + FontAssetPath);
    }

    [MenuItem("Tools/Rebuild PatrickHand Font")]
    public static void Rebuild()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
            Debug.Log("[SetupPatrickHandFont] Deleted existing PatrickHand SDF asset.");
        }
        Run();
    }
}
