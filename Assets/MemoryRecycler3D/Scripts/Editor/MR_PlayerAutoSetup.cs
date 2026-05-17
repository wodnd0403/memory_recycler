#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 사용자가 Play만 눌러도 동작하도록, 에디터 로드/스크립트 리로드/플레이 진입 직전에
// 1) MR_Player FBX 5종 임포트 정렬
// 2) MR_Player.controller 빌드
// 3) Prototype3D 씬의 Player_Recycler에 MR_Player_Model 부착 + 와이어링
// 을 자동으로 수행한다.
[InitializeOnLoad]
public static class MR_PlayerAutoSetup
{
    private const string MixamoFolder = "Assets/MemoryRecycler3D/ExternalAssets/Mixamo/Player";
    private const string TPosePath = MixamoFolder + "/MR_Player_TPose.fbx";
    private const string ControllerPath = "Assets/MemoryRecycler3D/Animations/Player/MR_Player.controller";
    private const string ScenePath = "Assets/MemoryRecycler3D/Scenes/Prototype3D.unity";
    private const string MixamoVisualName = "MR_Player_Model";
    private const string SetupCompletedKey = "MR3D_PlayerAutoSetup_Done_v3";

    static MR_PlayerAutoSetup()
    {
        // 에디터가 막 열렸을 때 한 번 실행. delayCall로 AssetDatabase가 준비된 후 실행.
        EditorApplication.delayCall += RunIfNeeded;
        // Play 진입 직전에 한 번 더 보장.
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingEditMode)
            EnsurePlayerSetup(force: true);
    }

    private static void RunIfNeeded()
    {
        if (SessionState.GetBool(SetupCompletedKey, false))
            return;
        EnsurePlayerSetup(force: false);
        SessionState.SetBool(SetupCompletedKey, true);
    }

    [MenuItem("Tools/Memory Recycler 3D/Apply MR_Player Setup Now")]
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
