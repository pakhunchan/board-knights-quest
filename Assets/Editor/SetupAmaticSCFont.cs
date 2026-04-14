using UnityEngine;
using UnityEditor;
using TMPro;

[InitializeOnLoad]
public static class SetupAmaticSCFont
{
    private const string TtfPath = "Assets/Fonts/AmaticSC-Bold.ttf";
    private const string FontAssetPath = "Assets/Fonts/AmaticSC-Bold SDF.asset";

    static SetupAmaticSCFont()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Setup AmaticSC Font")]
    public static void Run()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
        {
            if (existing.atlasPopulationMode != TMPro.AtlasPopulationMode.Dynamic)
            {
                existing.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                Debug.Log("[SetupAmaticSCFont] Switched existing asset to Dynamic atlas mode.");
            }
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null)
        {
            Debug.LogWarning("[SetupAmaticSCFont] TTF not found at " + TtfPath);
            return;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            font, 32, 5,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            512, 512);

        if (fontAsset == null)
        {
            Debug.LogError("[SetupAmaticSCFont] Failed to create font asset.");
            return;
        }

        fontAsset.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = "AmaticSC Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "AmaticSC Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SetupAmaticSCFont] Created font asset at " + FontAssetPath);
    }

    [MenuItem("Tools/Rebuild AmaticSC Font")]
    public static void Rebuild()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
            Debug.Log("[SetupAmaticSCFont] Deleted existing AmaticSC SDF asset.");
        }
        Run();
    }
}
