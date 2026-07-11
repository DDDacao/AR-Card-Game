using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class BatchModelImporterFixer : EditorWindow
{
    [MenuItem("Tools/Batch Fix Monster Importers")]
    public static void ShowWindow()
    {
        GetWindow<BatchModelImporterFixer>("Batch Fix Importers");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Fix Model Importer & Materials", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool helps fix obsolete importer settings and convert materials to URP safely, " +
            "avoiding the crashes caused by Unity's default Render Pipeline Converter.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("1. Fix Model Importer Settings (materialLocation)", GUILayout.Height(35)))
        {
            FixImporters();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("2. Convert All Existing .mat Materials to URP/Lit (RECOMMENDED)", GUILayout.Height(35)))
        {
            ConvertExistingMaterialsToURP();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("3. Extract & Convert Embedded Materials from FBX", GUILayout.Height(35)))
        {
            ExtractAndConvertFBXMaterials();
        }
    }

    private static void FixImporters()
    {
        string targetPath = "Assets/Monsters Full Pack Vol 1";
        if (!Directory.Exists(targetPath))
        {
            Debug.LogError($"Target directory does not exist: {targetPath}");
            return;
        }

        string[] fbxFiles = Directory.GetFiles(targetPath, "*.fbx", SearchOption.AllDirectories);
        int fixedCount = 0;

        try
        {
            for (int i = 0; i < fbxFiles.Length; i++)
            {
                string file = fbxFiles[i];
                string assetPath = file.Replace('\\', '/');
                
                EditorUtility.DisplayProgressBar(
                    "Fixing Importers", 
                    $"Processing {Path.GetFileName(assetPath)}...", 
                    (float)i / fbxFiles.Length
                );

                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer != null)
                {
                    if (importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
                    {
                        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                        importer.SaveAndReimport();
                        fixedCount++;
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Batch fix complete! Updated {fixedCount} FBX files to use InPrefab (Embedded) materials.");
        EditorUtility.DisplayDialog("Success", $"Updated {fixedCount} FBX files to InPrefab.", "OK");
    }

    private static void ConvertExistingMaterialsToURP()
    {
        string targetPath = "Assets/Monsters Full Pack Vol 1";
        if (!Directory.Exists(targetPath))
        {
            Debug.LogError($"Target directory does not exist: {targetPath}");
            return;
        }

        string[] matFiles = Directory.GetFiles(targetPath, "*.mat", SearchOption.AllDirectories);
        int convertedCount = 0;

        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null)
        {
            Debug.LogError("Could not find 'Universal Render Pipeline/Lit' Shader! Make sure you are in a URP project.");
            EditorUtility.DisplayDialog("Error", "Could not find URP/Lit Shader. Are you sure this is a URP project?", "OK");
            return;
        }

        try
        {
            for (int i = 0; i < matFiles.Length; i++)
            {
                string file = matFiles[i];
                string assetPath = file.Replace('\\', '/');
                
                EditorUtility.DisplayProgressBar(
                    "Upgrading Shaders", 
                    $"Processing {Path.GetFileName(assetPath)}...", 
                    (float)i / matFiles.Length
                );

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (mat != null)
                {
                    if (mat.shader.name != "Universal Render Pipeline/Lit")
                    {
                        mat.shader = urpShader;
                        EditorUtility.SetDirty(mat);
                        convertedCount++;
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Material upgrade complete! Converted {convertedCount} materials to URP/Lit.");
        EditorUtility.DisplayDialog("Success", $"Successfully converted {convertedCount} materials to URP/Lit.", "OK");
    }

    private static void ExtractAndConvertFBXMaterials()
    {
        string targetPath = "Assets/Monsters Full Pack Vol 1";
        if (!Directory.Exists(targetPath))
        {
            Debug.LogError($"Target directory does not exist: {targetPath}");
            return;
        }

        string[] fbxFiles = Directory.GetFiles(targetPath, "*.fbx", SearchOption.AllDirectories)
                                     .Where(f => !Path.GetFileName(f).Contains("@"))
                                     .ToArray();

        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        int extractedCount = 0;

        try
        {
            for (int i = 0; i < fbxFiles.Length; i++)
            {
                string file = fbxFiles[i];
                string assetPath = file.Replace('\\', '/');
                string dir = Path.GetDirectoryName(assetPath).Replace('\\', '/');
                string destFolder = dir + "/Materials";

                EditorUtility.DisplayProgressBar(
                    "Extracting Materials", 
                    $"Processing {Path.GetFileName(assetPath)}...", 
                    (float)i / fbxFiles.Length
                );

                var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                var materials = subAssets.Where(x => x is Material).Cast<Material>();

                if (!materials.Any()) continue;

                if (!Directory.Exists(destFolder))
                {
                    Directory.CreateDirectory(destFolder);
                    AssetDatabase.ImportAsset(destFolder);
                }

                foreach (Material material in materials)
                {
                    if (!AssetDatabase.IsSubAsset(material)) continue;

                    string matPath = Path.Combine(destFolder, material.name) + ".mat";
                    matPath = AssetDatabase.GenerateUniqueAssetPath(matPath);

                    string error = AssetDatabase.ExtractAsset(material, matPath);
                    if (string.IsNullOrEmpty(error))
                    {
                        Material extractedMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (extractedMat != null && urpShader != null)
                        {
                            extractedMat.shader = urpShader;
                            EditorUtility.SetDirty(extractedMat);
                        }
                        extractedCount++;
                    }
                    else
                    {
                        Debug.LogError($"Failed to extract material {material.name}: {error}");
                    }
                }
                
                AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Extraction complete! Extracted and upgraded {extractedCount} materials to URP/Lit.");
        EditorUtility.DisplayDialog("Success", $"Extracted and converted {extractedCount} materials to URP/Lit.", "OK");
    }
}
