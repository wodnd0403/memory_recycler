using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads the reversible Archive City Kit visual trial at runtime without
/// serializing the generated models into Prototype3D.unity.
/// </summary>
public static class ArchiveCityKitTrialBootstrap3D
{
    public const bool TrialEnabled = true;

    private const string PrototypeSceneName = "Prototype3D";
    private const string TrialResourceName = "ArchiveCityKitTrial";
    private const string RuntimeRootName = "Archive City Kit Trial (Runtime)";

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void LoadTrialKit()
    {
        if (!TrialEnabled || SceneManager.GetActiveScene().name != PrototypeSceneName)
        {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(TrialResourceName);
        if (prefab == null)
        {
            Debug.LogError($"Archive City Kit trial prefab is missing from Resources: {TrialResourceName}");
            return;
        }

        GameObject existingRuntimeRoot = GameObject.Find(RuntimeRootName);
        if (existingRuntimeRoot != null)
        {
            UnityEngine.Object.Destroy(existingRuntimeRoot);
        }

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        instance.name = RuntimeRootName;

        foreach (string objectName in ExistingVisualsToHide)
        {
            GameObject existing = FindSceneObject(objectName);
            if (existing != null && existing != instance && !existing.transform.IsChildOf(instance.transform))
            {
                existing.SetActive(false);
            }
        }

        ArchiveCityBackdropExpansion3D.Build(instance.transform);

        Debug.Log("Archive City Kit runtime trial loaded. Set TrialEnabled to false to roll it back.");
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name.Equals(objectName, StringComparison.Ordinal))
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }
}
