using UnityEngine;
using UnityEditor;
using TMPro;
using TMPro.EditorUtilities;

[InitializeOnLoad]
public static class SetupFredokaFont
{
    private const string TtfPath = "Assets/Fonts/Fredoka-VariableFont_wdth,wght.ttf";
    private const string FontAssetPath = "Assets/Fonts/Fredoka-VariableFont_wdth,wght SDF.asset";

    static SetupFredokaFont()
    {
        // Defer to avoid running during import
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Setup Fredoka Font")]
    public static void Run()
    {
        // Skip if font asset already exists
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
        {
            EnsureDefault(existing);
            return;
        }

        // Load the TTF
        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null)
        {
            Debug.LogWarning("[SetupFredokaFont] TTF not found at " + TtfPath);
            return;
        }

        // Create the SDF font asset
        var fontAsset = TMP_FontAsset.CreateFontAsset(font);
        if (fontAsset == null)
        {
            Debug.LogError("[SetupFredokaFont] Failed to create font asset.");
            return;
        }

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        // Save the atlas texture as a sub-asset
        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = "Fredoka Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        // Save the material as a sub-asset
        if (fontAsset.material != null)
        {
            fontAsset.material.name = "Fredoka Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SetupFredokaFont] Created font asset at " + FontAssetPath);

        EnsureDefault(fontAsset);
    }

    private static void EnsureDefault(TMP_FontAsset fontAsset)
    {
        var settings = TMP_Settings.instance;
        if (settings == null)
        {
            Debug.LogWarning("[SetupFredokaFont] TMP Settings not found. Set the default font manually.");
            return;
        }

        // Use SerializedObject to modify the TMP Settings asset
        var so = new SerializedObject(settings);
        var prop = so.FindProperty("m_defaultFontAsset");
        if (prop == null)
        {
            Debug.LogWarning("[SetupFredokaFont] Could not find m_defaultFontAsset property.");
            return;
        }

        if (prop.objectReferenceValue == fontAsset)
            return;

        prop.objectReferenceValue = fontAsset;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("[SetupFredokaFont] Set Fredoka as default TMP font.");
    }
}
