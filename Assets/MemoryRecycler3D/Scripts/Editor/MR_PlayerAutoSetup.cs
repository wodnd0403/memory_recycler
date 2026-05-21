#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// MR_Player FBX 정렬 + Animator Controller 빌드 + Player_Recycler 와이어링 단위 모듈.
// 자동 실행은 MR_FullSetupPipeline이 통합 관리하므로 여기서는 [InitializeOnLoad]를 제거했다.
// 외부에서 메뉴 또는 EnsurePlayerSetup(force) 호출로만 동작한다.
public static class MR_PlayerAutoSetup
{
    private const string MixamoFolder = "Assets/MemoryRecycler3D/ExternalAssets/Mixamo/Player";
    private const string TPosePath = MixamoFolder + "/MR_Player_TPose.fbx";
    private const string ControllerPath = "Assets/MemoryRecycler3D/Animations/Player/MR_Player.controller";
    private const string ScenePath = "Assets/MemoryRecycler3D/Scenes/Prototype3D.unity";
    private const string MixamoVisualName = "MR_Player_Model";
    private const string PlayerTextureMaterialPath = "Assets/MemoryRecycler3D/Materials/Tripo/PostApocalypticExplorer.mat";

    // 단독 메뉴는 디버깅용으로 유지. 일반 사용은 통합 메뉴(Apply Full Setup)를 권장.
    [MenuItem("Tools/Memory Recycler 3D/Advanced/Apply MR_Player Setup Only")]
    public static void ApplyNow()
    {
        EnsurePlayerSetup(force: true);
    }

    public static void EnsurePlayerSetup(bool force)
    {
        if (!File.Exists(TPosePath))
        {
            Debug.LogWarning("[MR_PlayerAutoSetup] MR_Player_TPose.fbx가 없습니다: " + TPosePath);
            return;
        }

        ReimportFbxIfNeeded(force);

        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) == null || force)
            MR_PlayerAnimatorBuilder.BuildController();

        WirePlayerInScene(force);
    }

    private static void ReimportFbxIfNeeded(bool force)
    {
        // 1) TPose 먼저 임포트해 Avatar 생성을 보장.
        ModelImporter tpose = AssetImporter.GetAtPath(TPosePath) as ModelImporter;
        if (tpose == null || tpose.animationType != ModelImporterAnimationType.Human || force)
        {
            AssetDatabase.ImportAsset(TPosePath, ImportAssetOptions.ForceUpdate);
        }

        // 2) 나머지 FBX. Postprocessor가 TPose Avatar를 Copy하도록 설정.
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { MixamoFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.EndsWith("MR_Player_TPose.fbx", System.StringComparison.OrdinalIgnoreCase))
                continue;

            ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
            bool needsReimport = force || mi == null
                || mi.animationType != ModelImporterAnimationType.Human
                || mi.sourceAvatar == null;
            if (needsReimport)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    private static void WirePlayerInScene(bool force)
    {
        // 현재 열려 있는 씬이 Prototype3D면 그대로, 아니면 로드.
        Scene targetScene = default;
        bool found = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.path == ScenePath)
            {
                targetScene = s;
                found = true;
                break;
            }
        }

        if (!found)
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogWarning("[MR_PlayerAutoSetup] Prototype3D.unity 없음: " + ScenePath);
                return;
            }
            targetScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject player = FindRoot(targetScene, "Player_Recycler");
        if (player == null)
        {
            Debug.LogWarning("[MR_PlayerAutoSetup] Player_Recycler 루트를 찾지 못했습니다. SceneBuilder 메뉴를 먼저 실행하세요.");
            return;
        }

        // 자식에서 MR_Player_Model 찾기
        Transform mixamoVisual = FindDeepChild(player.transform, MixamoVisualName);

        if (mixamoVisual == null)
        {
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TPosePath);
            if (fbxPrefab == null)
            {
                Debug.LogWarning("[MR_PlayerAutoSetup] TPose 프리팹 로드 실패");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(fbxPrefab, targetScene) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(fbxPrefab);
                SceneManager.MoveGameObjectToScene(instance, targetScene);
            }
            instance.name = MixamoVisualName;
            instance.transform.SetParent(player.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            mixamoVisual = instance.transform;
            Debug.Log("[MR_PlayerAutoSetup] " + MixamoVisualName + " 인스턴스 생성 완료");
        }

        mixamoVisual.localPosition = Vector3.zero;
        mixamoVisual.localRotation = Quaternion.identity;
        mixamoVisual.localScale = Vector3.one;
        mixamoVisual.gameObject.SetActive(true);
        ApplyPlayerTextureMaterial(mixamoVisual);

        // 기존 비주얼 비활성화
        Transform tripoVisual = FindDeepChild(player.transform, "Tripo Player Visual");
        if (tripoVisual != null && tripoVisual != mixamoVisual)
            tripoVisual.gameObject.SetActive(false);
        Transform proceduralVisual = FindDeepChild(player.transform, "RecyclerVisual");
        if (proceduralVisual != null)
            proceduralVisual.gameObject.SetActive(false);

        // ThirdPersonPlayer3D 연결
        ThirdPersonPlayer3D playerScript = player.GetComponent<ThirdPersonPlayer3D>();
        if (playerScript != null)
        {
            playerScript.useExternalHumanoidModel = true;
            playerScript.externalVisualRoot = mixamoVisual;
            playerScript.acceleration = 28f;
            playerScript.deceleration = 34f;
            playerScript.airControl = 0.35f;
            playerScript.inputDeadZone = 0.08f;
            playerScript.jumpHeight = 1.25f;
            playerScript.runAnimationPlaybackSpeed = 1f;
            playerScript.animatorParameterSmooth = 12f;
            playerScript.lockExternalVisualTransform = true;
            playerScript.autoGroundExternalVisual = true;
            playerScript.externalVisualGroundPadding = 0.12f;
            playerScript.stabilizeExternalClipRootMotion = true;
            playerScript.externalVisualLocalPosition = Vector3.zero;
            playerScript.externalVisualLocalEuler = Vector3.zero;
            playerScript.externalVisualLocalScale = Vector3.one;

            Animator animator = mixamoVisual.GetComponent<Animator>();
            if (animator == null)
                animator = mixamoVisual.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                RuntimeAnimatorController rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
                if (rac != null)
                    animator.runtimeAnimatorController = rac;
                playerScript.externalAnimator = animator;
                EditorUtility.SetDirty(animator);
            }
            EditorUtility.SetDirty(playerScript);
        }

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(targetScene);
        EditorSceneManager.SaveScene(targetScene);
        Debug.Log("[MR_PlayerAutoSetup] Player_Recycler 와이어링 + 씬 저장 완료");
    }

    private static void ApplyPlayerTextureMaterial(Transform visualRoot)
    {
        if (visualRoot == null)
            return;

        Material texturedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PlayerTextureMaterialPath);
        if (texturedMaterial == null)
            return;

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = texturedMaterial;
                EditorUtility.SetDirty(renderer);
                continue;
            }

            for (int m = 0; m < materials.Length; m++)
                materials[m] = texturedMaterial;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }
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
