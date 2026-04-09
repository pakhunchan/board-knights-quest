using UnityEngine;
using UnityEditor;
using TMPro;

[InitializeOnLoad]
public static class SetupCinzelFont
{
    private const string TtfPath = "Assets/Fonts/Cinzel-VariableFont_wght.ttf";
    private const string FontAssetPath = "Assets/Fonts/Cinzel SDF.asset";

    static SetupCinzelFont()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Setup Cinzel Font")]
    public static void Run()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
            return;

        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null)
        {
            Debug.LogWarning("[SetupCinzelFont] TTF not found at " + TtfPath);
            return;
        }

        // Create with full ASCII set: 512x512 atlas, 32px sampling, 5px padding
        var fontAsset = TMP_FontAsset.CreateFontAsset(
            font, 32, 5,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            512, 512);

        if (fontAsset == null)
        {
            Debug.LogError("[SetupCinzelFont] Failed to create font asset.");
            return;
        }

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = "Cinzel Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "Cinzel Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SetupCinzelFont] Created font asset at " + FontAssetPath);
    }

    [MenuItem("Tools/Rebuild Cinzel Font")]
    public static void Rebuild()
    {
        // Delete existing and recreate
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
            Debug.Log("[SetupCinzelFont] Deleted existing Cinzel SDF asset.");
        }
        Run();
    }
}
