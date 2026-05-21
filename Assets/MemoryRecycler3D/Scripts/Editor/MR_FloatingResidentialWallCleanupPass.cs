#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MR_FloatingResidentialWallCleanupPass
{
    private const string ScenePath = "Assets/MemoryRecycler3D/Scenes/Prototype3D.unity";
    private const string FloatingWallName = "Tripo Building Apartment_L";
    private static readonly Vector3 ResidentialLabelPosition = new Vector3(-13.5f, 2.5f, -11.6f);

    [MenuItem("Tools/Memory Recycler 3D/Advanced/Report Floating Residential Walls")]
    public static void ReportCandidates()
    {
        Scene scene = LoadOrGetPrototypeScene();
        if (!scene.IsValid())
            return;

        List<Renderer> candidates = FindCandidates(scene);
        Debug.Log("[MR_FloatingResidentialWallCleanupPass] Candidate count: " + candidates.Count);
        for (int i = 0; i < candidates.Count; i++)
        {
            Renderer renderer = candidates[i];
            Bounds bounds = renderer.bounds;
            Debug.Log("[MR_FloatingResidentialWallCleanupPass] Candidate " + i + ": " + GetPath(renderer.transform) + " bounds=" + bounds + " material=" + GetMaterialName(renderer));
        }
    }

    [MenuItem("Tools/Memory Recycler 3D/Advanced/Remove Floating Residential Walls")]
    public static void ApplyPass()
    {
        Scene scene = LoadOrGetPrototypeScene();
        if (!scene.IsValid())
            return;

        List<GameObject> targets = FindRemovalTargets(scene);
        for (int i = 0; i < targets.Count; i++)
        {
            Debug.Log("[MR_FloatingResidentialWallCleanupPass] Removing " + GetPath(targets[i].transform));
            Object.DestroyImmediate(targets[i]);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MR_FloatingResidentialWallCleanupPass] Removed floating residential walls: " + targets.Count);
    }

    private static List<GameObject> FindRemovalTargets(Scene scene)
    {
        List<GameObject> targets = new List<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            FindNamedObjectRecursive(roots[i].transform, FloatingWallName, targets);
        return targets;
    }

    private static void FindNamedObjectRecursive(Transform root, string objectName, List<GameObject> results)
    {
        if (root.name == objectName)
            results.Add(root.gameObject);

        for (int i = 0; i < root.childCount; i++)
            FindNamedObjectRecursive(root.GetChild(i), objectName, results);
    }

    private static List<Renderer> FindCandidates(Scene scene)
    {
        List<Renderer> candidates = new List<Renderer>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Renderer[] renderers = roots[i].GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                if (renderer == null || !renderer.enabled || IsProtected(renderer.gameObject))
                    continue;

                Bounds bounds = renderer.bounds;
                Vector3 center = bounds.center;
                Vector3 size = bounds.size;
                float horizontalDistance = Vector2.Distance(
                    new Vector2(center.x, center.z),
                    new Vector2(ResidentialLabelPosition.x, ResidentialLabelPosition.z));

                bool nearResidentialArea = horizontalDistance < 24f;
                bool tallAndBroad = size.y > 4.0f && Mathf.Max(size.x, size.z) > 3.0f;
                bool floatsOrDominatesView = bounds.min.y > 0.15f || size.y > 8.0f;
                if (nearResidentialArea && tallAndBroad && floatsOrDominatesView)
                    candidates.Add(renderer);
            }
        }

        candidates.Sort((a, b) =>
        {
            Bounds ba = a.bounds;
            Bounds bb = b.bounds;
            float scoreA = ba.size.y * Mathf.Max(ba.size.x, ba.size.z) + Mathf.Max(0f, ba.min.y) * 20f;
            float scoreB = bb.size.y * Mathf.Max(bb.size.x, bb.size.z) + Mathf.Max(0f, bb.min.y) * 20f;
            return scoreB.CompareTo(scoreA);
        });

        if (candidates.Count > 24)
            candidates.RemoveRange(24, candidates.Count - 24);

        return candidates;
    }

    private static bool IsProtected(GameObject go)
    {
        Transform t = go.transform;
        while (t != null)
        {
            string name = t.name;
            if (name.Contains("Player_Recycler") ||
                name.Contains("MR_Player_Model") ||
                name.Contains("Tripo Player Visual") ||
                name.Contains("Memory Orb") ||
                name.Contains("District Marker") ||
                name.Contains("District Label") ||
                name.Contains("Story Terminal") ||
                name.Contains("Central Archive Terminal") ||
                name.Contains("Tripo Archive Tower") ||
                name.Contains("Archive Glass"))
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
        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static string GetMaterialName(Renderer renderer)
    {
        if (renderer.sharedMaterial == null)
            return "(none)";

        return renderer.sharedMaterial.name;
    }
}
#endif
