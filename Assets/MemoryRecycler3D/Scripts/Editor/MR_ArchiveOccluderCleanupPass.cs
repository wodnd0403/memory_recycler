#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MR_ArchiveOccluderCleanupPass
{
    private const string ScenePath = "Assets/MemoryRecycler3D/Scenes/Prototype3D.unity";
    private const float ArchiveCenterZ = 18f;

    [MenuItem("Tools/Memory Recycler 3D/Advanced/Remove Archive Blocking Slabs")]
    public static void ApplyPass()
    {
        Scene scene = LoadOrGetPrototypeScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("[MR_ArchiveOccluderCleanupPass] Prototype3D scene not found.");
            return;
        }

        List<GameObject> targets = FindArchiveBlockingSlabs(scene);
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
                continue;

            Debug.Log("[MR_ArchiveOccluderCleanupPass] Removing " + GetPath(targets[i].transform));
            Object.DestroyImmediate(targets[i]);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MR_ArchiveOccluderCleanupPass] Removed archive blocking slabs: " + targets.Count);
    }

    [MenuItem("Tools/Memory Recycler 3D/Advanced/Report Archive Blocking Slabs")]
    public static void ReportCandidates()
    {
        Scene scene = LoadOrGetPrototypeScene();
        if (!scene.IsValid())
            return;

        List<Renderer> renderers = FindLargeArchiveOccluderCandidates(scene);
        Debug.Log("[MR_ArchiveOccluderCleanupPass] Broad candidate count: " + renderers.Count);
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer renderer = renderers[i];
            Bounds bounds = renderer != null ? renderer.bounds : default;
            Debug.Log("[MR_ArchiveOccluderCleanupPass] Candidate " + i + ": " + GetPath(renderer.transform) + " bounds=" + bounds);
        }
    }

    private static List<GameObject> FindArchiveBlockingSlabs(Scene scene)
    {
        List<GameObject> targets = new List<GameObject>();
        HashSet<GameObject> seen = new HashSet<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Renderer[] renderers = roots[i].GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                if (renderer == null || !renderer.enabled)
                    continue;

                GameObject go = renderer.gameObject;
                if (IsTripoArchiveTower(go))
                    go = GetTopmostNamedParent(go.transform, "Tripo Archive Tower");

                if (go == null || seen.Contains(go) || IsProtected(go) || !IsArchiveCentralOccluder(go, renderer.bounds))
                    continue;

                seen.Add(go);
                targets.Add(go);
            }
        }

        return targets;
    }

    private static List<Renderer> FindLargeArchiveOccluderCandidates(Scene scene)
    {
        List<Renderer> candidates = new List<Renderer>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Renderer[] renderers = roots[i].GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                if (renderer == null || !renderer.enabled || IsUiOrPlayerProtected(renderer.gameObject))
                    continue;

                Bounds bounds = renderer.bounds;
                Vector3 center = bounds.center;
                Vector3 size = bounds.size;
                bool nearArchive = Mathf.Abs(center.x) < 18f && center.z > ArchiveCenterZ - 24f && center.z < ArchiveCenterZ + 18f;
                bool blocksView = size.y > 5.0f && Mathf.Max(size.x, size.z) > 4.5f;
                if (nearArchive && blocksView)
                    candidates.Add(renderer);
            }
        }

        candidates.Sort((a, b) =>
        {
            float scoreA = a.bounds.size.y * Mathf.Max(a.bounds.size.x, a.bounds.size.z);
            float scoreB = b.bounds.size.y * Mathf.Max(b.bounds.size.x, b.bounds.size.z);
            return scoreB.CompareTo(scoreA);
        });

        if (candidates.Count > 30)
            candidates.RemoveRange(30, candidates.Count - 30);

        return candidates;
    }

    private static bool IsArchiveCentralOccluder(GameObject go, Bounds bounds)
    {
        if (IsTripoArchiveTower(go))
            return true;

        Vector3 size = bounds.size;
        float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        float smallest = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        float verticalArea = Mathf.Max(size.x, size.z) * size.y;
        Vector3 center = bounds.center;

        bool nearArchiveView = Mathf.Abs(center.x) < 11.5f && center.z > ArchiveCenterZ - 16f && center.z < ArchiveCenterZ + 12f;
        bool largeFlatSlab = size.y > 7.0f && verticalArea > 55f && smallest < 1.2f && largest > 7.5f;

        return nearArchiveView && largeFlatSlab;
    }

    private static bool IsTripoArchiveTower(GameObject go)
    {
        Transform t = go != null ? go.transform : null;
        while (t != null)
        {
            if (t.name == "Tripo Archive Tower")
                return true;
            t = t.parent;
        }

        return false;
    }

    private static GameObject GetTopmostNamedParent(Transform transform, string objectName)
    {
        Transform current = transform;
        Transform match = null;
        while (current != null)
        {
            if (current.name == objectName)
                match = current;
            current = current.parent;
        }

        return match != null ? match.gameObject : null;
    }

    private static bool IsProtected(GameObject go)
    {
        if (IsTripoArchiveTower(go))
            return false;

        Transform t = go.transform;
        while (t != null)
        {
            string name = t.name;
            if (name.Contains("Archive Glass") ||
                name.Contains("Archive Tower") ||
                name.Contains("Central Archive Terminal") ||
                name.Contains("Memory Orb") ||
                name.Contains("Player_Recycler") ||
                name.Contains("MR_Player_Model") ||
                name.Contains("Tripo Player Visual") ||
                name.Contains("Story Terminal") ||
                name.Contains("Music Zone"))
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }

    private static bool IsUiOrPlayerProtected(GameObject go)
    {
        Transform t = go.transform;
        while (t != null)
        {
            string name = t.name;
            if (name.Contains("Memory Orb") ||
                name.Contains("Player_Recycler") ||
                name.Contains("MR_Player_Model") ||
                name.Contains("Tripo Player Visual") ||
                name.Contains("Story Terminal") ||
                name.Contains("Music Zone"))
            {
                return true;
            }

            t = t.parent;
        }

        return false;
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

    private static string GetPath(Transform transform)
    {
        if (transform == null)
            return "(null)";

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
#endif
