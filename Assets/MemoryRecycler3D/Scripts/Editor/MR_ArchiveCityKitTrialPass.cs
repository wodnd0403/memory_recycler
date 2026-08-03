using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the generated Archive City Kit as a reversible visual-only trial.
/// It does not modify gameplay components, colliders, or the existing builders.
/// </summary>
public static class MR_ArchiveCityKitTrialPass
{
    private const string ScenePath = "Assets/MemoryRecycler3D/Scenes/Prototype3D.unity";
    private const string TrialRootName = "Archive City Kit Trial";
    private const string TrialPrefabPath = "Assets/MemoryRecycler3D/Resources/ArchiveCityKitTrial.prefab";
    private const string ModelRoot = "Assets/MemoryRecycler3D/Models/ArchiveCityKit/Generated/";
    private const string MaterialRoot = "Assets/MemoryRecycler3D/Materials/";

    private static readonly string[] ExistingVisualsToHide =
    {
        "Tripo Archive Tower",
        "Tripo Building Apartment_L",
        "Tripo Building Ruined_R",
        "Tripo Building Korean_L",
        "Tripo Street Lamp_L0",
        "Tripo Street Lamp_R0",
        "Tripo Street Lamp_L1",
        "Tripo Street Lamp_R1",
        "Tripo Street Lamp_L2",
        "Tripo Street Lamp_R2"
    };

    private readonly struct Replacement
    {
        public readonly string sourceName;
        public readonly string modelName;
        public readonly string instanceName;

        public Replacement(string sourceName, string modelName, string instanceName)
        {
            this.sourceName = sourceName;
            this.modelName = modelName;
            this.instanceName = instanceName;
        }
    }

    private static readonly Replacement[] BuildingReplacements =
    {
        new Replacement("Tripo Building Apartment_L", "RuinedLowrise_A.obj", "Trial Ruined Lowrise_L"),
        new Replacement("Tripo Building Ruined_R", "RuinedMidrise_B.obj", "Trial Ruined Midrise_R"),
        new Replacement("Tripo Building Korean_L", "RuinedTower_C.obj", "Trial Ruined Tower_L")
    };

    [MenuItem("Tools/Memory Recycler 3D/Archive City Kit/Apply Trial Layout")]
    public static void ApplyTrialLayout()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            Debug.LogError($"Archive City Kit trial requires the active scene '{ScenePath}'. Current: '{scene.path}'.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Archive City Kit Trial");

        RemoveTrialRoot(scene);

        GameObject trialRoot = new GameObject(TrialRootName);
        Undo.RegisterCreatedObjectUndo(trialRoot, "Create Archive City Kit Trial Root");
        SceneManager.MoveGameObjectToScene(trialRoot, scene);

        GameObject archive = InstantiateModel(
            scene,
            trialRoot.transform,
            "CentralMemoryArchive.obj",
            "Trial Central Memory Archive",
            new Vector3(0f, 0f, 35.5f),
            0f);

        if (archive != null)
        {
            AddArchiveLights(archive.transform);
        }

        foreach (Replacement replacement in BuildingReplacements)
        {
            GameObject source = FindInScene(scene, replacement.sourceName);
            if (source == null)
            {
                Debug.LogWarning($"Archive City Kit trial could not find source visual '{replacement.sourceName}'.");
                continue;
            }

            Vector3 position = source.transform.position;
            position.y = 0f;
            InstantiateModel(
                scene,
                trialRoot.transform,
                replacement.modelName,
                replacement.instanceName,
                position,
                ExtractYaw(source.transform));
        }

        GameObject lampModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot + "MemoryStreetLamp_A.obj");
        foreach (string sourceName in ExistingVisualsToHide)
        {
            if (!sourceName.StartsWith("Tripo Street Lamp_", StringComparison.Ordinal))
            {
                continue;
            }

            GameObject source = FindInScene(scene, sourceName);
            if (source == null || lampModel == null)
            {
                Debug.LogWarning($"Archive City Kit trial could not replace lamp '{sourceName}'.");
                continue;
            }

            Vector3 position = source.transform.position;
            position.y = 0f;
            GameObject lamp = InstantiateAsset(scene, lampModel, trialRoot.transform, "Trial " + sourceName);
            if (lamp == null)
            {
                continue;
            }

            lamp.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, ExtractYaw(source.transform), 0f));
            lamp.transform.localScale = Vector3.one;
            RemapMaterials(lamp);
        }

        foreach (string objectName in ExistingVisualsToHide)
        {
            GameObject existing = FindInScene(scene, objectName);
            if (existing == null || !existing.activeSelf)
            {
                continue;
            }

            Undo.RecordObject(existing, "Hide Existing Archive City Visual");
            existing.SetActive(false);
            EditorUtility.SetDirty(existing);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = trialRoot;
        Debug.Log("Archive City Kit trial layout applied. Use the Rollback menu item to restore the previous visuals.");
    }

    [MenuItem("Tools/Memory Recycler 3D/Archive City Kit/Rollback Trial Layout")]
    public static void RollbackTrialLayout()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            Debug.LogError($"Archive City Kit rollback requires the active scene '{ScenePath}'. Current: '{scene.path}'.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Rollback Archive City Kit Trial");

        RemoveTrialRoot(scene);
        foreach (string objectName in ExistingVisualsToHide)
        {
            GameObject existing = FindInScene(scene, objectName);
            if (existing == null || existing.activeSelf)
            {
                continue;
            }

            Undo.RecordObject(existing, "Restore Existing Archive City Visual");
            existing.SetActive(true);
            EditorUtility.SetDirty(existing);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Archive City Kit trial layout rolled back and previous visuals restored.");
    }

    [MenuItem("Tools/Memory Recycler 3D/Archive City Kit/Save Runtime Trial Prefab")]
    public static void SaveRuntimeTrialPrefab()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject trialRoot = FindInScene(scene, TrialRootName);
        if (trialRoot == null)
        {
            Debug.LogError($"Cannot save runtime trial prefab because '{TrialRootName}' is missing.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/MemoryRecycler3D/Resources"))
        {
            AssetDatabase.CreateFolder("Assets/MemoryRecycler3D", "Resources");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(trialRoot, TrialPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Failed to save runtime trial prefab: {TrialPrefabPath}");
            return;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Archive City Kit runtime trial prefab saved: {TrialPrefabPath}");
    }

    private static GameObject InstantiateModel(
        Scene scene,
        Transform parent,
        string modelName,
        string instanceName,
        Vector3 position,
        float yaw)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot + modelName);
        if (model == null)
        {
            Debug.LogError($"Archive City Kit model is missing: {ModelRoot + modelName}");
            return null;
        }

        GameObject instance = InstantiateAsset(scene, model, parent, instanceName);
        if (instance == null)
        {
            return null;
        }

        instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        instance.transform.localScale = Vector3.one;
        RemapMaterials(instance);
        return instance;
    }

    private static GameObject InstantiateAsset(Scene scene, GameObject model, Transform parent, string instanceName)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
        if (instance == null)
        {
            Debug.LogError($"Failed to instantiate Archive City Kit model '{model.name}'.");
            return null;
        }

        Undo.RegisterCreatedObjectUndo(instance, "Instantiate Archive City Kit Model");
        Undo.SetTransformParent(instance.transform, parent, "Parent Archive City Kit Model");
        instance.name = instanceName;
        return instance;
    }

    private static void AddArchiveLights(Transform archive)
    {
        CreatePointLight(archive, "Trial Archive Entrance Light", new Vector3(0f, 6.0f, -12.8f), 7.5f, 14f);
        CreatePointLight(archive, "Trial Archive Symbol Light", new Vector3(0f, 34.5f, -12.5f), 9.0f, 20f);
    }

    private static void CreatePointLight(Transform parent, string name, Vector3 localPosition, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(lightObject, "Create Archive City Kit Light");
        Undo.SetTransformParent(lightObject.transform, parent, "Parent Archive City Kit Light");
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.localRotation = Quaternion.identity;

        Light light = Undo.AddComponent<Light>(lightObject);
        light.type = LightType.Point;
        light.color = new Color(0.21f, 0.90f, 0.90f);
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static void RemapMaterials(GameObject instance)
    {
        Dictionary<string, Material> map = new Dictionary<string, Material>
        {
            { "MR_Concrete", LoadMaterial("MR3D_Building.mat") },
            { "MR_ConcreteDark", LoadMaterial("MR3D_BuildingTrim.mat") },
            { "MR_DarkMetal", LoadMaterial("MR3D_DarkPanel.mat") },
            { "MR_SecondaryMetal", LoadMaterial("MR3D_BuildingTrim.mat") },
            { "MR_WindowDark", LoadMaterial("MR3D_Window.mat") },
            { "MR_CyanEmission", LoadMaterial("MR3D_MemoryGlow.mat") },
            { "MR_DimCyan", LoadMaterial("MR3D_StoryTerminalGlow.mat") },
            { "MR_Rust", LoadMaterial("MR3D_CinematicGrime.mat") },
            { "MR_Debris", LoadMaterial("MR3D_Debris.mat") }
        };

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                string sourceName = material.name.Replace(" (Instance)", string.Empty);
                if (!map.TryGetValue(sourceName, out Material replacement) || replacement == null)
                {
                    continue;
                }

                materials[i] = replacement;
                changed = true;
            }

            if (changed)
            {
                Undo.RecordObject(renderer, "Remap Archive City Kit Materials");
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }
    }

    private static Material LoadMaterial(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + fileName);
    }

    private static float ExtractYaw(Transform source)
    {
        Vector3 euler = source.eulerAngles;
        bool tripoAxisConversion = Mathf.Abs(Mathf.DeltaAngle(euler.x, 270f)) < 20f ||
                                   Mathf.Abs(Mathf.DeltaAngle(euler.x, 90f)) < 20f;
        return tripoAxisConversion ? euler.z : euler.y;
    }

    private static void RemoveTrialRoot(Scene scene)
    {
        GameObject existingRoot = FindInScene(scene, TrialRootName);
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }
}
