using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

public class MaskMapGenerator : EditorWindow
{
    Texture2D albedoTexture;
    Texture2D normalTexture;
    Texture2D metallicTexture;
    Texture2D roughnessTexture;
    Texture2D aoTexture;

    Texture2D maskMapPreview;

    [MenuItem("Tools/Mask Map Generator")]
    public static void ShowWindow()
    {
        GetWindow<MaskMapGenerator>("Mask Map Generator");
    }

    private void OnEnable()
{
    // Register this script with the event system to receive mouse events
    EventSystem.current.SetSelectedGameObject(null);
}

    private void OnDisable()
    {
        // Deregister this script from the event system when the window is closed
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            normal = { textColor = new Color(0.25f, 0.5f, 1) }
        };

        EditorGUILayout.BeginVertical("Box");

        EditorGUILayout.LabelField("TIBITS MASK MAP GENERATOR", titleStyle);
        EditorGUILayout.Space();

        albedoTexture = (Texture2D)EditorGUILayout.ObjectField("Albedo Texture", albedoTexture, typeof(Texture2D), false);
        normalTexture = (Texture2D)EditorGUILayout.ObjectField("Normal Texture", normalTexture, typeof(Texture2D), false);
        metallicTexture = (Texture2D)EditorGUILayout.ObjectField("Metallic Texture", metallicTexture, typeof(Texture2D), false);
        roughnessTexture = (Texture2D)EditorGUILayout.ObjectField("Roughness Texture", roughnessTexture, typeof(Texture2D), false);
        aoTexture = (Texture2D)EditorGUILayout.ObjectField("AO Texture", aoTexture, typeof(Texture2D), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Mask Map"))
        {
            CalculateMaskMap();
        }

        // Display a preview of the mask map
        if (maskMapPreview != null)
        {
            EditorGUILayout.Space();
            Rect previewRect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(true));
            EditorGUI.DrawTextureTransparent(previewRect, maskMapPreview, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.EndVertical();
    }

    void CalculateMaskMap()
    {
        if (albedoTexture == null || normalTexture == null || metallicTexture == null || roughnessTexture == null || aoTexture == null)
        {
            Debug.LogError("Please assign all textures before generating the mask map.");
            return;
        }

        albedoTexture = MakeTextureReadable(albedoTexture);
        normalTexture = MakeTextureReadable(normalTexture);
        metallicTexture = MakeTextureReadable(metallicTexture);
        roughnessTexture = MakeTextureReadable(roughnessTexture);
        aoTexture = MakeTextureReadable(aoTexture);

        int width = albedoTexture.width;
        int height = albedoTexture.height;

        Texture2D maskMap = new Texture2D(width, height, TextureFormat.ARGB32, true);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color albedoPixel = albedoTexture.GetPixel(x, y);
                Color normalPixel = normalTexture.GetPixel(x, y);
                Color metallicPixel = metallicTexture.GetPixel(x, y);
                Color roughnessPixel = roughnessTexture.GetPixel(x, y);
                Color aoPixel = aoTexture.GetPixel(x, y);

                Color maskMapPixel = new Color(albedoPixel.r, normalPixel.g, metallicPixel.b, roughnessPixel.r);
                maskMap.SetPixel(x, y, maskMapPixel);
            }
        }

        maskMap.Apply();

        // Save the mask map to disk
        SaveMaskMapToAssetDatabase(maskMap);

        // Update the mask map preview texture
        maskMapPreview = maskMap;
    }

    void SaveMaskMapToAssetDatabase(Texture2D maskMap)
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Mask Map", "mask_map", "png", "Please enter a file name to save the mask map.");
        if (path.Length != 0)
        {
            byte[] pngData = ImageConversion.EncodeToPNG(maskMap);
            if (pngData != null)
            {
                System.IO.File.WriteAllBytes(path, pngData);
                AssetDatabase.ImportAsset(path);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.SaveAndReimport();
                }
                Debug.Log("Mask map saved at " + path);
            }
        }
    }

    Texture2D MakeTextureReadable(Texture2D source)
    {
        if (!source.isReadable)
        {
            string path = AssetDatabase.GetAssetPath(source);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                return (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
            }
        }
        return source;
    }
}

