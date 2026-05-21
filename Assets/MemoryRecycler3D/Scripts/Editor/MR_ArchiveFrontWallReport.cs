using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MR_ArchiveFrontWallReport
{
    private const float ArchiveCenterZ = 18f;
    private const string PrototypeScenePath = "Assets/MemoryRecycler3D/Scenes/Prototype3D.unity";

    [MenuItem("Tools/Memory Recycler 3D/Advanced/Remove Floating Ref Wall Panels")]
    public static void RemoveFloatingRefWallPanels()
    {
        Scene scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[MR_ArchiveFrontWallReport] No prototype scene is loaded.");
            return;
        }

        int removed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            removed += RemoveFloatingRefWallPanels(root.transform);
        }

        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log("[MR_ArchiveFrontWallReport] Removed floating Ref Wall Panels: " + removed);
    }

    [MenuItem("Tools/Memory Recycler 3D/Advanced/Report Archive Front Wall Candidates")]
    public static void ReportCandidates()
    {
        Scene scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[MR_ArchiveFrontWallReport] No active scene is loaded.");
            return;
        }

        List<Candidate> candidates = new List<Candidate>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                Vector3 size = bounds.size;
                Vector3 center = bounds.center;

                float horizontalThinness = Mathf.Min(size.x, size.z);
                float horizontalWidth = Mathf.Max(size.x, size.z);
                bool isLargeVerticalSheet = size.y >= 4f && horizontalWidth >= 3f;
                bool nearArchiveFront = center.z >= ArchiveCenterZ - 42f && center.z <= ArchiveCenterZ + 30f && Mathf.Abs(center.x) <= 42f;
                bool isFloating = bounds.min.y > 0.35f;
                bool isDarkOrGenerated = IsSuspiciousName(renderer.transform) || IsSuspiciousMaterial(renderer);

                if (isLargeVerticalSheet && nearArchiveFront && (isFloating || isDarkOrGenerated))
                {
                    candidates.Add(new Candidate(renderer, bounds));
                }
            }
        }

        candidates.Sort((a, b) =>
        {
            int zCompare = a.DistanceToArchive.CompareTo(b.DistanceToArchive);
            if (zCompare != 0)
            {
                return zCompare;
            }

            return b.SurfaceArea.CompareTo(a.SurfaceArea);
        });

        Debug.Log("[MR_ArchiveFrontWallReport] Archive-front wall candidates: " + candidates.Count);
        int count = Mathf.Min(candidates.Count, 80);
        for (int i = 0; i < count; i++)
        {
            Candidate candidate = candidates[i];
            Renderer renderer = candidate.Renderer;
            Bounds bounds = candidate.Bounds;
            Debug.Log(
                "[MR_ArchiveFrontWallReport] Candidate " + i +
                " path=" + GetPath(renderer.transform) +
                " bounds.center=" + Format(bounds.center) +
                " bounds.size=" + Format(bounds.size) +
                " bounds.minY=" + bounds.min.y.ToString("0.00") +
                " material=" + GetMaterialName(renderer) +
                " type=" + renderer.GetType().Name);
        }
    }

    private static bool IsSuspiciousName(Transform transform)
    {
        string path = GetPath(transform);
        return path.Contains("Haze Wall") ||
               path.Contains("Side Alley Wall") ||
               path.Contains("Background Block") ||
               path.Contains("Perimeter") ||
               path.Contains("Tripo") ||
               path.Contains("Wall");
    }

    private static bool IsSuspiciousMaterial(Renderer renderer)
    {
        string materialName = GetMaterialName(renderer);
        return materialName.Contains("Building") ||
               materialName.Contains("RoadPatch") ||
               materialName.Contains("DarkPanel") ||
               materialName.Contains("Concrete") ||
               materialName.Contains("Tripo");
    }

    private static string GetMaterialName(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null)
        {
            return "<none>";
        }

        return renderer.sharedMaterial.name;
    }

    private static string GetPath(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        StringBuilder builder = new StringBuilder(transform.name);
        Transform current = transform.parent;
        while (current != null)
        {
            builder.Insert(0, current.name + "/");
            current = current.parent;
        }

        return builder.ToString();
    }

    private static string Format(Vector3 value)
    {
        return "(" +
               value.x.ToString("0.00") + ", " +
               value.y.ToString("0.00") + ", " +
               value.z.ToString("0.00") + ")";
    }

    private static int RemoveFloatingRefWallPanels(Transform root)
    {
        int removed = 0;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            removed += RemoveFloatingRefWallPanels(child);

            if (child.name != "Ref Wall Panel")
            {
                continue;
            }

            Renderer renderer = child.GetComponent<Renderer>();
            Bounds bounds = renderer != null ? renderer.bounds : new Bounds(child.position, child.lossyScale);
            bool nearArchiveSightline = bounds.center.z >= ArchiveCenterZ - 28f &&
                                        bounds.center.z <= ArchiveCenterZ + 26f &&
                                        Mathf.Abs(bounds.center.x) <= 34f;
            bool floatingPanel = bounds.size.y >= 3.8f && bounds.min.y >= 1.8f;
            if (!nearArchiveSightline || !floatingPanel)
            {
                continue;
            }

            Debug.Log("[MR_ArchiveFrontWallReport] Removing " + GetPath(child) +
                      " bounds.center=" + Format(bounds.center) +
                      " bounds.size=" + Format(bounds.size));
            Object.DestroyImmediate(child.gameObject);
            removed++;
        }

        return removed;
    }

    private readonly struct Candidate
    {
        public Candidate(Renderer renderer, Bounds bounds)
        {
            Renderer = renderer;
            Bounds = bounds;
            DistanceToArchive = Mathf.Abs(bounds.center.z - ArchiveCenterZ);
            SurfaceArea = Mathf.Max(bounds.size.x, bounds.size.z) * bounds.size.y;
        }

        public Renderer Renderer { get; }
        public Bounds Bounds { get; }
        public float DistanceToArchive { get; }
        public float SurfaceArea { get; }
    }
}
