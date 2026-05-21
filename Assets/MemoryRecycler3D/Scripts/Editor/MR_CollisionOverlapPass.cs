#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Docs/07 §16 — 제출 안정화 패치: 플레이어가 건물/잔해/파이프 안으로 박히지 않도록
// 정적 월드 오브젝트에 BoxCollider를 보강하고, 겹친 장식들은 ComputePenetration으로 살짝 분리한다.
// 핵심 스토리 오브젝트(기억 구체, 중앙 아카이브)는 절대 이동시키지 않는다.
public static class MR_CollisionOverlapPass
{
    private const string EnvironmentLayerName = "MR3D_Environment";
    private const string PlayerVisualLayerName = "MR3D_PlayerVisual";
    private const string ScenePath = "Assets/MemoryRecycler3D/Scenes/Prototype3D.unity";

    // 정적 월드 충돌이 들어가야 하는 루트 오브젝트 이름들.
    private static readonly string[] StaticWorldRoots =
    {
        "Silent City",
        "Cinematic Detail Root",
        "Tripo Quality Pass",
        "Main Avenue",
        "Abandoned City Ground",
    };

    // 충돌 보강 후보지만 isTrigger/Memory Orb/Archive 핵심은 건드리지 않는다.
    private static readonly string[] ProtectedNameFragments =
    {
        "Memory Orb",
        "Central Archive Terminal",
        "Terminal Beacon",
        "Story Progression",
        "Player_Recycler",
        "MR_Player_Model",
        "Tripo Player Visual",
        "RecyclerVisual",
        "Main Camera",
        "Sun Light",
        "Memory Blue Fill Light",
        "Managers",
        "WorldToneController",
        "Cinematic Post Process",
        "EventSystem",
    };

    private static readonly string[] DecorativeCollisionNameFragments =
    {
        "Cinematic ",
        "Ref Hanging Cable",
        "Ref Facade Pipe",
        "Tripo Wall Pipes",
        "Tripo Recycling Sign"
    };

    [MenuItem("Tools/Memory Recycler 3D/Advanced/Apply Collision & Overlap Pass Only")]
    public static void ApplyPass()
    {
        Scene scene = LoadOrGetPrototypeScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("[MR_CollisionOverlapPass] Prototype3D 씬을 찾을 수 없습니다.");
            return;
        }

        int envLayer = EnsureLayer(EnvironmentLayerName);
        int playerVisualLayer = EnsureLayer(PlayerVisualLayerName);

        int colliderAdded = 0;
        int colliderUpdated = 0;
        int layerAssigned = 0;
        int overlapResolved = 0;

        GameObject tripoRoot = FindRoot(scene, "Tripo Quality Pass");
        if (tripoRoot != null)
            PruneTripoVisualColliders(tripoRoot.transform, ref colliderUpdated);

        // 1) 정적 월드 콜라이더 보강
        for (int i = 0; i < StaticWorldRoots.Length; i++)
        {
            GameObject root = FindRoot(scene, StaticWorldRoots[i]);
            if (root == null)
                continue;
            AugmentStaticColliders(root.transform, envLayer, ref colliderAdded, ref colliderUpdated, ref layerAssigned);
        }

        // 2) 플레이어 비주얼 레이어 적용 — 카메라 SphereCast가 무시하도록.
        GameObject player = FindRoot(scene, "Player_Recycler");
        if (player != null)
            AssignPlayerVisualLayer(player.transform, playerVisualLayer);

        // 3) 카메라의 collisionMask를 Environment만 포함하도록 설정.
        ConfigureCameraMask(player, envLayer);

        // 4) 겹침 해소
        overlapResolved = ResolveOverlaps(scene, envLayer);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(string.Format(
            "[MR_CollisionOverlapPass] 콜라이더 추가 {0} / 갱신 {1} / 레이어 적용 {2} / 겹침 해소 {3}",
            colliderAdded, colliderUpdated, layerAssigned, overlapResolved));
    }

    private static Scene LoadOrGetPrototypeScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.path == ScenePath)
                return s;
        }
        if (!System.IO.File.Exists(ScenePath))
            return default;
        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void PruneTripoVisualColliders(Transform root, ref int removed)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = colliders.Length - 1; i >= 0; i--)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger)
                continue;

            GameObject go = collider.gameObject;
            if (go.name.Contains("Gameplay Collider"))
            {
                Object.DestroyImmediate(go);
                removed++;
            }
        }
    }

    private static void FitArchiveGameplayCollider(BoxCollider box)
    {
        Vector3 size = box.size;
        size.x = Mathf.Min(size.x, 9.8f);
        size.z = Mathf.Min(size.z, 8.2f);
        box.size = size;
        box.center = Vector3.zero;
        EditorUtility.SetDirty(box);
    }

    // ─── 1) 정적 월드 콜라이더 보강 ──────────────────────────────────────
    private static void AugmentStaticColliders(Transform root, int envLayer, ref int added, ref int updated, ref int layerAssigned)
    {
        // Renderer를 가진 자식 중에서 콜라이더가 필요한 오브젝트만 보강.
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;

            GameObject go = r.gameObject;
            if (IsProtected(go))
                continue;
            if (IsDecorativeCollisionOnly(go))
                continue;

            // 부모/자신에 트리거 콜라이더가 있으면 절대 건드리지 않는다 — 상호작용용.
            if (HasTriggerColliderInHierarchy(go))
                continue;

            // 너무 작은 장식은 충돌 추가 안 함. Renderer.bounds.size 합 < threshold 면 스킵.
            Bounds bounds = r.bounds;
            float maxExtent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxExtent < 0.12f)
            {
                AssignLayerIfDefault(go, envLayer, ref layerAssigned);
                continue;
            }

            // 이미 BoxCollider/MeshCollider가 있고 isTrigger=false면 크기만 갱신, 없으면 추가.
            if (TryApplyMeshCollider(go, envLayer, ref added, ref updated, ref layerAssigned))
                continue;

            Collider existing = go.GetComponent<Collider>();
            if (existing == null)
            {
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.isTrigger = false;
                ApplyLocalSpaceBox(box, r, bounds);
                added++;
            }
            else if (existing is BoxCollider boxExisting && !existing.isTrigger)
            {
                // 직접 만든 게 아닐 수 있으니 너무 작거나 비어 있을 때만 갱신.
                if (boxExisting.size.sqrMagnitude < 0.01f)
                {
                    ApplyLocalSpaceBox(boxExisting, r, bounds);
                    updated++;
                }
            }

            AssignLayerIfDefault(go, envLayer, ref layerAssigned);
        }
    }

    // Renderer bounds(월드)를 GameObject의 로컬 좌표계 BoxCollider로 변환.
    private static void ApplyLocalSpaceBox(BoxCollider box, Renderer renderer, Bounds worldBounds)
    {
        Transform t = box.transform;
        Vector3 localCenter = t.InverseTransformPoint(worldBounds.center);
        Vector3 lossy = t.lossyScale;
        Vector3 localSize = new Vector3(
            SafeDivide(worldBounds.size.x, Mathf.Abs(lossy.x)),
            SafeDivide(worldBounds.size.y, Mathf.Abs(lossy.y)),
            SafeDivide(worldBounds.size.z, Mathf.Abs(lossy.z)));

        // 너무 얇은 면은 플레이어가 통과할 수 있어 최소 두께 보장.
        localSize.x = Mathf.Max(localSize.x, 0.05f);
        localSize.y = Mathf.Max(localSize.y, 0.05f);
        localSize.z = Mathf.Max(localSize.z, 0.05f);

        box.center = localCenter;
        box.size = localSize;
        box.isTrigger = false;
        EditorUtility.SetDirty(box);
    }

    private static bool TryApplyMeshCollider(GameObject go, int envLayer, ref int added, ref int updated, ref int layerAssigned)
    {
        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return false;

        Collider[] existingColliders = go.GetComponents<Collider>();
        MeshCollider meshCollider = go.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = go.AddComponent<MeshCollider>();
            added++;
        }
        else
        {
            updated++;
        }

        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = false;
        meshCollider.isTrigger = false;
        EditorUtility.SetDirty(meshCollider);

        for (int i = 0; i < existingColliders.Length; i++)
        {
            Collider collider = existingColliders[i];
            if (collider == null || collider == meshCollider || collider.isTrigger)
                continue;

            Object.DestroyImmediate(collider);
            updated++;
        }

        AssignLayerIfDefault(go, envLayer, ref layerAssigned);
        return true;
    }

    private static float SafeDivide(float a, float b)
    {
        return Mathf.Abs(b) > 0.0001f ? a / b : a;
    }

    private static bool HasTriggerColliderInHierarchy(GameObject go)
    {
        Collider[] cols = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null && cols[i].isTrigger)
                return true;
        }
        // 부모 방향도 한 단계 확인.
        Collider parentCol = go.GetComponentInParent<Collider>();
        if (parentCol != null && parentCol.isTrigger)
            return true;
        return false;
    }

    private static bool IsProtected(GameObject go)
    {
        Transform t = go.transform;
        while (t != null)
        {
            string name = t.name;
            for (int i = 0; i < ProtectedNameFragments.Length; i++)
            {
                if (name.Contains(ProtectedNameFragments[i]))
                    return true;
            }
            t = t.parent;
        }
        return false;
    }

    private static bool IsDecorativeCollisionOnly(GameObject go)
    {
        Transform t = go.transform;
        while (t != null)
        {
            string name = t.name;
            for (int i = 0; i < DecorativeCollisionNameFragments.Length; i++)
            {
                if (name.Contains(DecorativeCollisionNameFragments[i]))
                    return true;
            }
            t = t.parent;
        }
        return false;
    }

    private static void AssignLayerIfDefault(GameObject go, int envLayer, ref int counter)
    {
        if (go.layer == 0)
        {
            go.layer = envLayer;
            counter++;
            EditorUtility.SetDirty(go);
        }
    }

    // ─── 2) 플레이어 비주얼 레이어 적용 ─────────────────────────────────
    private static void AssignPlayerVisualLayer(Transform root, int playerVisualLayer)
    {
        // CharacterController가 있는 루트는 그대로 두고(이동/충돌에 영향), 비주얼 자식만 PlayerVisual로.
        string[] visualNames = { "MR_Player_Model", "Tripo Player Visual", "RecyclerVisual" };
        for (int i = 0; i < visualNames.Length; i++)
        {
            Transform child = FindDeepChild(root, visualNames[i]);
            if (child == null)
                continue;
            SetLayerRecursive(child.gameObject, playerVisualLayer);
        }
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursive(t.gameObject, layer);
        EditorUtility.SetDirty(go);
    }

    // ─── 3) 카메라 collisionMask: Environment만 ────────────────────────
    private static void ConfigureCameraMask(GameObject player, int envLayer)
    {
        // OrbitCamera3D는 Main Camera에 붙어 있다.
        OrbitCamera3D[] cameras = Object.FindObjectsByType<OrbitCamera3D>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            OrbitCamera3D cam = cameras[i];
            if (cam == null)
                continue;
            cam.collisionMask = 1 << envLayer;
            EditorUtility.SetDirty(cam);
        }
    }

    // ─── 4) 겹침 해소 ─────────────────────────────────────────────────
    private static int ResolveOverlaps(Scene scene, int envLayer)
    {
        // 모든 환경 콜라이더 수집(트리거 제외).
        List<Collider> envColliders = new List<Collider>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Collider[] cols = roots[i].GetComponentsInChildren<Collider>(true);
            for (int j = 0; j < cols.Length; j++)
            {
                Collider c = cols[j];
                if (c == null || c.isTrigger)
                    continue;
                if (IsProtected(c.gameObject))
                    continue;
                if (IsDecorativeCollisionOnly(c.gameObject))
                    continue;
                if (c.gameObject.layer != envLayer)
                    continue;
                envColliders.Add(c);
            }
        }

        // Bounds broadphase + ComputePenetration로 최소 분리 거리 산출.
        int resolved = 0;
        const float MaxSeparationStep = 0.40f; // 한 번에 최대 40cm까지만 이동시켜 폭주 방지.
        const int MaxIterations = 2;

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            int movedThisPass = 0;
            for (int i = 0; i < envColliders.Count; i++)
            {
                Collider a = envColliders[i];
                if (a == null)
                    continue;
                Bounds boundsA = a.bounds;

                for (int j = i + 1; j < envColliders.Count; j++)
                {
                    Collider b = envColliders[j];
                    if (b == null)
                        continue;

                    // 같은 루트(부모 체인이 동일)면 의도된 컴포넌트 콜라이더로 간주, 스킵.
                    if (ShareImmediateParent(a.transform, b.transform))
                        continue;

                    if (!boundsA.Intersects(b.bounds))
                        continue;

                    Vector3 direction;
                    float distance;
                    bool overlap = Physics.ComputePenetration(
                        a, a.transform.position, a.transform.rotation,
                        b, b.transform.position, b.transform.rotation,
                        out direction, out distance);

                    if (!overlap || distance < 0.02f)
                        continue;

                    // b를 a로부터 멀어지는 방향으로 살짝 이동(최대 step 제한).
                    float moveDist = Mathf.Min(distance + 0.005f, MaxSeparationStep);
                    Vector3 offset = direction * moveDist;
                    // 수직 점프(천장으로 떠오름) 방지: 주로 수평 성분만 사용.
                    if (Mathf.Abs(offset.y) > Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.z)))
                    {
                        offset.y *= 0.25f;
                    }
                    b.transform.position += offset;
                    EditorUtility.SetDirty(b.transform);
                    resolved++;
                    movedThisPass++;
                    boundsA = a.bounds;
                }
            }
            if (movedThisPass == 0)
                break;
        }

        return resolved;
    }

    private static bool ShareImmediateParent(Transform a, Transform b)
    {
        if (a.parent == null || b.parent == null)
            return false;
        return a.parent == b.parent;
    }

    // ─── Layer / scene helpers ────────────────────────────────────────
    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0)
            return existing;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        if (layers == null)
            return 0;

        // 사용자 레이어 슬롯 8~31 중 빈 칸에 추가.
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (slot != null && string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        return 0;
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objectName)
                return roots[i];
        }
        return null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }
}
#endif
