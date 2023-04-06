using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using System.Net.NetworkInformation;

public class MaskMapGenerator : EditorWindow
{
    Texture2D albedoTexture;
    Texture2D normalTexture;
    Texture2D metallicTexture;
    Texture2D roughnessTexture;
    Texture2D aoTexture;

    Texture2D maskMapPreview;

    float brightness = 1.0f;

    [MenuItem("Tools/Mask Map Generator")]
    public static void ShowWindow()
    {
        GetWindow<MaskMapGenerator>("Mask Map Generator");
    }

    private void OnEnable()
    {
        // Check if EventSystem.current is not null before registering
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }


    private void OnDisable()
    {
        // Check if EventSystem.current is not null before de-registering
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
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

        brightness = EditorGUILayout.Slider("Brightness", brightness, 0.0f, 2.0f);

        if (maskMapPreview == null)
        {
            maskMapPreview = new Texture2D(128, 128, TextureFormat.ARGB32, false);
        }

        UpdateMaskMapPreview();

        // Display a preview of the mask map
        EditorGUILayout.Space();
        Rect previewRect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(true));
        EditorGUI.DrawTextureTransparent(previewRect, maskMapPreview, ScaleMode.ScaleToFit);

        if (GUILayout.Button("Generate Mask Map"))
        {
            CalculateMaskMap();
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

        Color[] albedoPixels = albedoTexture.GetPixels();
        Color[] normalPixels = normalTexture.GetPixels();
        Color[] metallicPixels = metallicTexture.GetPixels();
        Color[] roughnessPixels = roughnessTexture.GetPixels();
        Color[] aoPixels = aoTexture.GetPixels();

        Color[] maskMapPixels = new Color[width * height];

        System.Threading.Tasks.Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                Color albedoPixel = albedoPixels[index];
                Color normalPixel = normalPixels[index];
                Color metallicPixel = metallicPixels[index];
                Color roughnessPixel = roughnessPixels[index];

                // Apply brightness adjustment
                albedoPixel *= brightness;
                normalPixel *= brightness;
                metallicPixel *= brightness;
                roughnessPixel *= brightness;

                maskMapPixels[index] = new Color(albedoPixel.r, normalPixel.g, metallicPixel.b, roughnessPixel.r);
            }
        });

        maskMap.SetPixels(maskMapPixels);
        maskMap.Apply();

        // Save the mask map to disk
        SaveMaskMapToAssetDatabase(maskMap);

        // Update the mask map preview texture
        maskMapPreview = maskMap;
    }

    void UpdateMaskMapPreview()
    {
        if (albedoTexture == null || normalTexture == null || metallicTexture == null || roughnessTexture == null || aoTexture == null)
        {
            return;
        }

        albedoTexture = MakeTextureReadable(albedoTexture);
        normalTexture = MakeTextureReadable(normalTexture);
        metallicTexture = MakeTextureReadable(metallicTexture);
        roughnessTexture = MakeTextureReadable(roughnessTexture);
        aoTexture = MakeTextureReadable(aoTexture);

        int width = maskMapPreview.width;
        int height = maskMapPreview.height;

        Texture2D maskMap = new Texture2D(width, height, TextureFormat.ARGB32, false);

        Color[] albedoPixels = albedoTexture.GetPixels();
        Color[] normalPixels = normalTexture.GetPixels();
        Color[] metallicPixels = metallicTexture.GetPixels();
        Color[] roughnessPixels = roughnessTexture.GetPixels();
        Color[] aoPixels = aoTexture.GetPixels();

        Color[] maskMapPixels = new Color[width * height];

        int albedoWidth = albedoTexture.width;
        int albedoHeight = albedoTexture.height;
        int normalWidth = normalTexture.width;
        int normalHeight = normalTexture.height;
        int metallicWidth = metallicTexture.width;
        int metallicHeight = metallicTexture.height;
        int roughnessWidth = roughnessTexture.width;
        int roughnessHeight = roughnessTexture.height;

        System.Threading.Tasks.Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                int albedoIndex = Mathf.FloorToInt(y * (float)albedoHeight / height) * albedoWidth + Mathf.FloorToInt(x * (float)albedoWidth / width);
                int normalIndex = Mathf.FloorToInt(y * (float)normalHeight / height) * normalWidth + Mathf.FloorToInt(x * (float)normalWidth / width);
                int metallicIndex = Mathf.FloorToInt(y * (float)metallicHeight / height) * metallicWidth + Mathf.FloorToInt(x * (float)metallicWidth / width);
                int roughnessIndex = Mathf.FloorToInt(y * (float)roughnessHeight / height) * roughnessWidth + Mathf.FloorToInt(x * (float)roughnessWidth / width);

                Color albedoPixel = albedoPixels[albedoIndex];
                Color normalPixel = normalPixels[normalIndex];
                Color metallicPixel = metallicPixels[metallicIndex];
                Color roughnessPixel = roughnessPixels[roughnessIndex];

                // Apply brightness adjustment
                albedoPixel *= brightness;
                normalPixel *= brightness;
                metallicPixel *= brightness;
                roughnessPixel *= brightness;

                maskMapPixels[index] = new Color(albedoPixel.r, normalPixel.g, metallicPixel.b, roughnessPixel.r);
            }
        });

        maskMap.SetPixels(maskMapPixels);
        maskMap.Apply();

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
