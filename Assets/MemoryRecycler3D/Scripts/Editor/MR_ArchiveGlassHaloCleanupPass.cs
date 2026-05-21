#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MR_ArchiveGlassHaloCleanupPass
{
    private const string ScenePath = "Assets/MemoryRecycler3D/Scenes/Prototype3D.unity";
    private const string HaloName = "Cinematic Archive Glass Halo";

    [MenuItem("Tools/Memory Recycler 3D/Advanced/Remove Archive Glass Halo")]
    public static void ApplyPass()
    {
        Scene scene = LoadOrGetPrototypeScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("[MR_ArchiveGlassHaloCleanupPass] Prototype3D scene not found.");
            return;
        }

        int removed = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            removed += RemoveByNameRecursive(roots[i].transform, HaloName);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MR_ArchiveGlassHaloCleanupPass] Removed archive glass halo objects: " + removed);
    }

    private static int RemoveByNameRecursive(Transform root, string objectName)
    {
        int removed = 0;
        for (int i = root.childCount - 1; i >= 0; i--)
            removed += RemoveByNameRecursive(root.GetChild(i), objectName);

        if (root.name == objectName)
        {
            Object.DestroyImmediate(root.gameObject);
            removed++;
        }

        return removed;
    }

    private static Scene LoadOrGetPrototypeScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.path == ScenePath)
                return scene;
        }

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }
}
#endif
