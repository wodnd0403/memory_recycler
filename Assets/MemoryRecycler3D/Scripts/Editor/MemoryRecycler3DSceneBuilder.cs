#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class MemoryRecycler3DSceneBuilder
{
    private const string Root = "Assets/MemoryRecycler3D";
    private const string DataPath = Root + "/Data/Generated";
    private const string ScenePath = Root + "/Scenes/Prototype3D.unity";
    private const string MaterialPath = Root + "/Materials";
    private const string TexturePath = Root + "/Textures";
    private const string GeneratedTexturePath = TexturePath + "/Generated";
    private const string TripoAssetPath = Root + "/ExternalAssets/Tripo";
    private const string TripoMaterialPath = MaterialPath + "/Tripo";
    private const string CinematicVolumeProfilePath = DataPath + "/MR3D_CinematicVolumeProfile.asset";

    [MenuItem("Tools/Memory Recycler 3D/Build Prototype Scene")]
    public static void BuildPrototypeScene()
    {
        EnsureFolder(Root + "/Data");
        EnsureFolder(DataPath);
        EnsureFolder(Root + "/Scenes");
        EnsureFolder(MaterialPath);
        EnsureFolder(TexturePath);
        EnsureFolder(GeneratedTexturePath);
        EnsurePlayerTag();
        EnsureCinematicTextureAssets();

        Material groundMat = CreateMaterial("MR3D_Ground", new Color(0.055f, 0.060f, 0.055f), false);
        Material roadMat = CreateMaterial("MR3D_Road", new Color(0.075f, 0.078f, 0.074f), false);
        Material buildingMat = CreateMaterial("MR3D_Building", new Color(0.13f, 0.135f, 0.125f), false);
        Material trimMat = CreateMaterial("MR3D_BuildingTrim", new Color(0.08f, 0.083f, 0.078f), false);
        Material windowMat = CreateMaterial("MR3D_Window", new Color(0.04f, 0.09f, 0.11f), true);
        Material debrisMat = CreateMaterial("MR3D_Debris", new Color(0.16f, 0.155f, 0.145f), false);

        Material suitMat = CreateMaterial("MR3D_RecyclerSuit", new Color(0.15f, 0.18f, 0.20f), false);
        Material coatMat = CreateMaterial("MR3D_RecyclerCoat", new Color(0.16f, 0.19f, 0.20f), false);
        Material skinMat = CreateMaterial("MR3D_Skin", new Color(0.78f, 0.68f, 0.60f), false);
        Material accentMat = CreateMaterial("MR3D_RecyclerAccent", new Color(0.25f, 0.7f, 0.95f), true);
        Material memoryMat = CreateMaterial("MR3D_MemoryGlow", new Color(0.35f, 0.85f, 1f), true);
        Material terminalMat = CreateMaterial("MR3D_Terminal", new Color(0.1f, 0.9f, 0.75f), true);
        ApplyCinematicMaterialTextures();

        MemoryData3D[] memories = CreateMemoryAssets();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Prototype3D";

        CreateManagers();
        Light sun = CreateLighting();
        CreateWorldTone(sun);
        CreateGround(groundMat, roadMat, debrisMat);
        CreateCityBlocks(buildingMat, trimMat, windowMat, debrisMat, accentMat);
        GameObject player = CreatePlayer(suitMat, coatMat, skinMat, accentMat);
        CreateCamera(player.transform);
        CreateMemories(memories, memoryMat);
        CreateArchiveTerminal(terminalMat, trimMat);
        CreateInstructionsSign();
        ApplyPhotoReferenceSceneLook(scene);
        ApplyCinematicReferenceLook(scene);

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.105f, 0.135f, 0.18f);
        RenderSettings.fogDensity = 0.046f;
        RenderSettings.ambientLight = new Color(0.075f, 0.09f, 0.115f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Memory Recycler 3D", "업데이트된 3D 프로토타입 씬 생성 완료\n" + ScenePath, "확인");
    }

    [MenuItem("Tools/Memory Recycler 3D/Apply Cinematic Reference Look")]
    public static void ApplyCinematicReferenceLook()
    {
        EnsureFolder(Root + "/Data");
        EnsureFolder(DataPath);
        EnsureFolder(MaterialPath);
        EnsureFolder(TexturePath);
        EnsureFolder(GeneratedTexturePath);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int removed = RemoveAllSceneObjectsWithPrefix(scene, "Cinematic ");

        EnsureCinematicTextureAssets();
        ApplyCinematicMaterialTextures();
        ApplyAdultMaleScenePose(scene);
        ApplyPhotoReferenceSceneLook(scene);
        ApplyCinematicReferenceLook(scene);

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Memory Recycler 3D cinematic reference look applied. Removed objects: " + removed);
    }

    [MenuItem("Tools/Memory Recycler 3D/Clean Prototype Scene")]
    public static void CleanPrototypeScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int removed = 0;

        removed += RemoveDuplicateRootObjects(scene, "Managers");
        removed += RemoveDuplicateRootObjects(scene, "Player_Recycler");
        removed += RemoveDuplicateRootObjects(scene, "Main Camera");
        removed += RemoveDuplicateRootObjects(scene, "WorldToneController");
        removed += RemoveDuplicateRootObjects(scene, "Sun Light");
        removed += RemoveDuplicateRootObjects(scene, "Memory Blue Fill Light");
        removed += RemoveDuplicateRootObjects(scene, "Silent City");
        removed += RemoveDuplicateRootObjects(scene, "Abandoned City Ground");
        removed += RemoveDuplicateRootObjects(scene, "Main Avenue");
        removed += RemoveDuplicateRootObjects(scene, "Central Archive Terminal");
        removed += RemoveDuplicateRootObjects(scene, "Tripo Quality Pass");
        removed += RemoveDuplicateRootObjects(scene, "Prototype Instructions");

        removed += RemoveAllSceneObjectsByName(scene, "Night Sky Celestials");
        removed += RemoveAllSceneObjectsByName(scene, "Soot Rain Stain");
        removed += RemoveAllSceneObjectsByName(scene, "Exposed Concrete Patch");
        removed += RemoveAllSceneObjectsByName(scene, "Hairline Crack");
        removed += RemoveAllSceneObjectsByName(scene, "Rust Exposed Edge");
        removed += RemoveAllSceneObjectsByName(scene, "Fresh Facade Rubble");
        removed += RemoveAllSceneObjectsByName(scene, "Broken Glass Slash");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Facade Pipe");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Neon Strip");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Wall Sign");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Recycle Mark");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Sign Panel");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Sign Glow");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Recycle Glyph");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Boarded Window");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Rooftop Antenna");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Wall Panel");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Hanging Cable");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Door Glow");
        removed += RemoveAllSceneObjectsByName(scene, "Ref Distant Block");
        removed += RemoveAllSceneObjectsWithPrefix(scene, "Cinematic ");

        removed += RemoveAllSceneObjectsByName(scene, "Hood");
        removed += RemoveAllSceneObjectsByName(scene, "Visor");
        removed += RemoveAllSceneObjectsByName(scene, "Shoulder Harness");
        removed += RemoveAllSceneObjectsByName(scene, "Knee Pad_L");
        removed += RemoveAllSceneObjectsByName(scene, "Knee Pad_R");
        removed += RemoveAllSceneObjectsByName(scene, "Memory Pack");
        removed += RemoveAllSceneObjectsByName(scene, "Memory Canister");
        removed += RemoveAllSceneObjectsByName(scene, "Wrist Scanner");
        removed += RemoveAllSceneObjectsByName(scene, "Antenna");
        removed += RemoveAllSceneObjectsByName(scene, "Antenna Tip");
        removed += RemoveAllSceneObjectsByName(scene, "Recycler Small Light");

        EnsureCinematicTextureAssets();
        ApplyCinematicMaterialTextures();
        ApplyAdultMaleScenePose(scene);
        ApplyPhotoReferenceSceneLook(scene);
        ApplyCinematicReferenceLook(scene);

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Memory Recycler 3D scene cleanup complete. Removed objects: " + removed);
    }

    private static int RemoveDuplicateRootObjects(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        bool foundFirst = false;
        int removed = 0;

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null || roots[i].name != objectName)
                continue;

            if (!foundFirst)
            {
                foundFirst = true;
                continue;
            }

            Object.DestroyImmediate(roots[i]);
            removed++;
        }

        return removed;
    }

    private static int RemoveAllSceneObjectsByName(Scene scene, string objectName)
    {
        List<GameObject> matches = new List<GameObject>();
        CollectSceneObjectsByName(scene, objectName, matches);

        for (int i = 0; i < matches.Count; i++)
        {
            if (matches[i] != null)
                Object.DestroyImmediate(matches[i]);
        }

        return matches.Count;
    }

    private static void CollectSceneObjectsByName(Scene scene, string objectName, List<GameObject> matches)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            CollectSceneObjectsByName(roots[i].transform, objectName, matches);
    }

    private static void CollectSceneObjectsByName(Transform root, string objectName, List<GameObject> matches)
    {
        if (root == null)
            return;

        if (root.name == objectName)
            matches.Add(root.gameObject);

        for (int i = 0; i < root.childCount; i++)
            CollectSceneObjectsByName(root.GetChild(i), objectName, matches);
    }

    private static int RemoveAllSceneObjectsWithPrefix(Scene scene, string prefix)
    {
        List<GameObject> matches = new List<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            CollectSceneObjectsWithPrefix(roots[i].transform, prefix, matches);

        for (int i = 0; i < matches.Count; i++)
        {
            if (matches[i] != null)
                Object.DestroyImmediate(matches[i]);
        }

        return matches.Count;
    }

    private static void CollectSceneObjectsWithPrefix(Transform root, string prefix, List<GameObject> matches)
    {
        if (root == null)
            return;

        if (root.name.StartsWith(prefix))
        {
            matches.Add(root.gameObject);
            return;
        }

        for (int i = 0; i < root.childCount; i++)
            CollectSceneObjectsWithPrefix(root.GetChild(i), prefix, matches);
    }

    private static void ApplyAdultMaleScenePose(Scene scene)
    {
        GameObject player = FindRoot(scene, "Player_Recycler");
        if (player == null)
            return;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.height = 2.05f;
            controller.radius = 0.31f;
            controller.center = new Vector3(0f, 1.02f, 0f);
            EditorUtility.SetDirty(controller);
        }

        Transform visual = FindDeepChild(player.transform, "RecyclerVisual");
        if (visual == null)
            return;

        Material jacketMat = CreateMaterial("MR3D_RecyclerCoat", new Color(0.12f, 0.14f, 0.13f), false);
        Material shirtMat = CreateMaterial("MR3D_RecyclerSuit", new Color(0.22f, 0.24f, 0.23f), false);
        Material pantsMat = CreateMaterial("MR3D_WorkPants", new Color(0.08f, 0.09f, 0.10f), false);
        Material skinMat = CreateMaterial("MR3D_Skin", new Color(0.72f, 0.58f, 0.47f), false);
        Material shoeMat = CreateMaterial("MR3D_WornShoes", new Color(0.035f, 0.032f, 0.03f), false);
        Material hairMat = CreateMaterial("MR3D_DarkHair", new Color(0.025f, 0.024f, 0.022f), false);
        Material bagMat = CreateMaterial("MR3D_WornBag", new Color(0.075f, 0.07f, 0.06f), false);
        Material glowMat = CreateMaterial("MR3D_MemoryGlow", new Color(0.08f, 0.85f, 1f), true);
        Material amberMat = CreateMaterial("MR3D_AmberDetail", new Color(0.95f, 0.48f, 0.09f), true);

        SetScenePart(visual, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.26f, 0f), Vector3.zero, new Vector3(0.46f, 0.52f, 0.31f), shirtMat);
        SetScenePart(visual, "Coat", PrimitiveType.Cube, new Vector3(0f, 1.21f, -0.01f), Vector3.zero, new Vector3(0.64f, 0.90f, 0.40f), jacketMat);
        SetScenePart(visual, "Coat Skirt", PrimitiveType.Cube, new Vector3(0f, 0.67f, 0f), Vector3.zero, new Vector3(0.56f, 0.46f, 0.34f), jacketMat);
        SetScenePart(visual, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.87f, 0.02f), Vector3.zero, Vector3.one * 0.285f, skinMat);
        SetScenePart(visual, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 1.66f, 0.015f), Vector3.zero, new Vector3(0.085f, 0.11f, 0.085f), skinMat);

        BuildHumanoidSceneLimbRig(visual, jacketMat, skinMat, pantsMat, shoeMat);
        BuildReferenceCharacterDetails(visual, jacketMat, skinMat, hairMat, bagMat, glowMat, amberMat);
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == objectName)
                return roots[i];
        }

        return null;
    }

    private static void SetScenePart(Transform visual, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Material material)
    {
        Transform part = FindDeepChild(visual, name);
        if (part == null)
        {
            GameObject partObject = GameObject.CreatePrimitive(primitive);
            partObject.name = name;
            partObject.transform.SetParent(visual, false);
            part = partObject.transform;
        }

        part.localPosition = localPosition;
        part.localEulerAngles = localEuler;
        part.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        EditorUtility.SetDirty(part.gameObject);
    }

    private static void BuildHumanoidSceneLimbRig(Transform visual, Material jacketMat, Material skinMat, Material pantsMat, Material shoeMat)
    {
        Transform leftArmPivot = CreateScenePivot(visual, "ArmPivot_L", new Vector3(-0.39f, 1.47f, 0.015f), new Vector3(0f, 0f, -6f));
        Transform leftForearmPivot = CreateScenePivot(leftArmPivot, "ForearmPivot_L", new Vector3(0f, -0.48f, 0f), Vector3.zero);
        SetScenePartUnder(visual, leftArmPivot, "Shoulder_L", PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(0.15f, 0.12f, 0.13f), jacketMat);
        SetScenePartUnder(visual, leftArmPivot, "Arm_L", PrimitiveType.Cylinder, new Vector3(0f, -0.24f, 0f), Vector3.zero, new Vector3(0.135f, 0.255f, 0.135f), jacketMat);
        SetScenePartUnder(visual, leftForearmPivot, "Elbow_L", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.118f, jacketMat);
        SetScenePartUnder(visual, leftForearmPivot, "Forearm_L", PrimitiveType.Cylinder, new Vector3(0f, -0.23f, 0f), Vector3.zero, new Vector3(0.116f, 0.25f, 0.116f), jacketMat);
        SetScenePartUnder(visual, leftForearmPivot, "Hand_L", PrimitiveType.Sphere, new Vector3(0f, -0.51f, 0.035f), Vector3.zero, new Vector3(0.090f, 0.080f, 0.070f), shoeMat);

        Transform rightArmPivot = CreateScenePivot(visual, "ArmPivot_R", new Vector3(0.39f, 1.47f, 0.015f), new Vector3(0f, 0f, 6f));
        Transform rightForearmPivot = CreateScenePivot(rightArmPivot, "ForearmPivot_R", new Vector3(0f, -0.48f, 0f), Vector3.zero);
        SetScenePartUnder(visual, rightArmPivot, "Shoulder_R", PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(0.15f, 0.12f, 0.13f), jacketMat);
        SetScenePartUnder(visual, rightArmPivot, "Arm_R", PrimitiveType.Cylinder, new Vector3(0f, -0.24f, 0f), Vector3.zero, new Vector3(0.135f, 0.255f, 0.135f), jacketMat);
        SetScenePartUnder(visual, rightForearmPivot, "Elbow_R", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.118f, jacketMat);
        SetScenePartUnder(visual, rightForearmPivot, "Forearm_R", PrimitiveType.Cylinder, new Vector3(0f, -0.23f, 0f), Vector3.zero, new Vector3(0.116f, 0.25f, 0.116f), jacketMat);
        SetScenePartUnder(visual, rightForearmPivot, "Hand_R", PrimitiveType.Sphere, new Vector3(0f, -0.51f, 0.035f), Vector3.zero, new Vector3(0.090f, 0.080f, 0.070f), shoeMat);

        Transform leftLegPivot = CreateScenePivot(visual, "LegPivot_L", new Vector3(-0.15f, 0.97f, 0f), Vector3.zero);
        Transform leftKneePivot = CreateScenePivot(leftLegPivot, "KneePivot_L", new Vector3(0f, -0.56f, 0f), Vector3.zero);
        SetScenePartUnder(visual, leftLegPivot, "Leg_L", PrimitiveType.Cylinder, new Vector3(0f, -0.28f, 0f), Vector3.zero, new Vector3(0.135f, 0.29f, 0.135f), pantsMat);
        SetScenePartUnder(visual, leftKneePivot, "Knee_L", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.125f, pantsMat);
        SetScenePartUnder(visual, leftKneePivot, "Shin_L", PrimitiveType.Cylinder, new Vector3(0f, -0.27f, 0f), Vector3.zero, new Vector3(0.115f, 0.28f, 0.115f), pantsMat);
        SetScenePartUnder(visual, leftKneePivot, "Boot_L", PrimitiveType.Cube, new Vector3(0f, -0.58f, 0.10f), Vector3.zero, new Vector3(0.18f, 0.10f, 0.32f), shoeMat);

        Transform rightLegPivot = CreateScenePivot(visual, "LegPivot_R", new Vector3(0.15f, 0.97f, 0f), Vector3.zero);
        Transform rightKneePivot = CreateScenePivot(rightLegPivot, "KneePivot_R", new Vector3(0f, -0.56f, 0f), Vector3.zero);
        SetScenePartUnder(visual, rightLegPivot, "Leg_R", PrimitiveType.Cylinder, new Vector3(0f, -0.28f, 0f), Vector3.zero, new Vector3(0.135f, 0.29f, 0.135f), pantsMat);
        SetScenePartUnder(visual, rightKneePivot, "Knee_R", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.125f, pantsMat);
        SetScenePartUnder(visual, rightKneePivot, "Shin_R", PrimitiveType.Cylinder, new Vector3(0f, -0.27f, 0f), Vector3.zero, new Vector3(0.115f, 0.28f, 0.115f), pantsMat);
        SetScenePartUnder(visual, rightKneePivot, "Boot_R", PrimitiveType.Cube, new Vector3(0f, -0.58f, 0.10f), Vector3.zero, new Vector3(0.18f, 0.10f, 0.32f), shoeMat);
    }

    private static void BuildReferenceCharacterDetails(Transform visual, Material jacketMat, Material skinMat, Material hairMat, Material bagMat, Material glowMat, Material amberMat)
    {
        SetScenePart(visual, "Hair Cap", PrimitiveType.Sphere, new Vector3(0f, 1.96f, -0.005f), Vector3.zero, new Vector3(0.31f, 0.17f, 0.27f), hairMat);
        SetScenePart(visual, "Hair Back", PrimitiveType.Sphere, new Vector3(0f, 1.91f, -0.13f), Vector3.zero, new Vector3(0.27f, 0.18f, 0.16f), hairMat);
        SetScenePart(visual, "Hair Side_L", PrimitiveType.Cube, new Vector3(-0.17f, 1.86f, 0.025f), new Vector3(0f, 0f, -8f), new Vector3(0.055f, 0.16f, 0.12f), hairMat);
        SetScenePart(visual, "Hair Side_R", PrimitiveType.Cube, new Vector3(0.17f, 1.86f, 0.025f), new Vector3(0f, 0f, 8f), new Vector3(0.055f, 0.16f, 0.12f), hairMat);
        SetScenePart(visual, "Hair Fringe_L", PrimitiveType.Cube, new Vector3(-0.08f, 1.91f, 0.18f), new Vector3(0f, 0f, -18f), new Vector3(0.07f, 0.11f, 0.035f), hairMat);
        SetScenePart(visual, "Hair Fringe_R", PrimitiveType.Cube, new Vector3(0.08f, 1.91f, 0.18f), new Vector3(0f, 0f, 18f), new Vector3(0.07f, 0.11f, 0.035f), hairMat);
        SetScenePart(visual, "Nose", PrimitiveType.Cube, new Vector3(0f, 1.85f, 0.205f), new Vector3(-8f, 0f, 0f), new Vector3(0.055f, 0.075f, 0.085f), skinMat);
        SetScenePart(visual, "Ear_L", PrimitiveType.Sphere, new Vector3(-0.205f, 1.855f, 0.025f), Vector3.zero, new Vector3(0.052f, 0.082f, 0.040f), skinMat);
        SetScenePart(visual, "Ear_R", PrimitiveType.Sphere, new Vector3(0.205f, 1.855f, 0.025f), Vector3.zero, new Vector3(0.052f, 0.082f, 0.040f), skinMat);
        SetScenePart(visual, "Chin", PrimitiveType.Cube, new Vector3(0f, 1.755f, 0.122f), new Vector3(8f, 0f, 0f), new Vector3(0.145f, 0.055f, 0.065f), skinMat);
        SetScenePart(visual, "Face Mask", PrimitiveType.Cube, new Vector3(0f, 1.82f, 0.215f), new Vector3(-5f, 0f, 0f), new Vector3(0.27f, 0.18f, 0.070f), bagMat);
        SetScenePart(visual, "Eye Light Band", PrimitiveType.Cube, new Vector3(0f, 1.90f, 0.250f), Vector3.zero, new Vector3(0.25f, 0.032f, 0.035f), glowMat);
        SetScenePart(visual, "Visor Housing", PrimitiveType.Cube, new Vector3(0f, 1.90f, 0.232f), Vector3.zero, new Vector3(0.31f, 0.070f, 0.040f), bagMat);
        SetScenePart(visual, "Raised Collar", PrimitiveType.Cube, new Vector3(0f, 1.58f, -0.11f), new Vector3(-8f, 0f, 0f), new Vector3(0.50f, 0.25f, 0.15f), jacketMat);
        SetScenePart(visual, "Collar Guard_L", PrimitiveType.Cube, new Vector3(-0.29f, 1.58f, 0.02f), new Vector3(0f, 0f, -12f), new Vector3(0.08f, 0.22f, 0.15f), jacketMat);
        SetScenePart(visual, "Collar Guard_R", PrimitiveType.Cube, new Vector3(0.29f, 1.58f, 0.02f), new Vector3(0f, 0f, 12f), new Vector3(0.08f, 0.22f, 0.15f), jacketMat);
        SetScenePart(visual, "Long Coat Tail", PrimitiveType.Cube, new Vector3(0f, 0.50f, -0.04f), Vector3.zero, new Vector3(0.54f, 0.58f, 0.31f), jacketMat);
        SetScenePart(visual, "Coat Back Seam", PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.245f), Vector3.zero, new Vector3(0.035f, 0.72f, 0.040f), bagMat);
        SetScenePart(visual, "Coat Hem_L", PrimitiveType.Cube, new Vector3(-0.19f, 0.44f, -0.06f), new Vector3(0f, 0f, 5f), new Vector3(0.18f, 0.34f, 0.25f), jacketMat);
        SetScenePart(visual, "Coat Hem_R", PrimitiveType.Cube, new Vector3(0.19f, 0.44f, -0.06f), new Vector3(0f, 0f, -5f), new Vector3(0.18f, 0.34f, 0.25f), jacketMat);
        SetScenePart(visual, "Crossbody Strap", PrimitiveType.Cube, new Vector3(-0.08f, 1.22f, -0.20f), new Vector3(0f, 0f, -22f), new Vector3(0.070f, 0.82f, 0.048f), bagMat);
        SetScenePart(visual, "Amber Strap Clips", PrimitiveType.Cube, new Vector3(-0.22f, 1.26f, -0.255f), new Vector3(0f, 0f, -22f), new Vector3(0.050f, 0.070f, 0.040f), amberMat);
        SetScenePart(visual, "Satchel", PrimitiveType.Cube, new Vector3(-0.25f, 0.88f, -0.33f), new Vector3(0f, 8f, -4f), new Vector3(0.34f, 0.25f, 0.18f), bagMat);
        SetScenePart(visual, "Memory Backpack", PrimitiveType.Cube, new Vector3(0f, 1.12f, -0.46f), Vector3.zero, new Vector3(0.43f, 0.58f, 0.16f), bagMat);
        SetScenePart(visual, "Backpack Top Module", PrimitiveType.Cube, new Vector3(0f, 1.43f, -0.55f), Vector3.zero, new Vector3(0.36f, 0.11f, 0.10f), bagMat);
        SetScenePart(visual, "Backpack Cyan Core", PrimitiveType.Cube, new Vector3(0f, 1.10f, -0.555f), Vector3.zero, new Vector3(0.070f, 0.24f, 0.035f), glowMat);
        SetScenePart(visual, "Backpack Side Canister_L", PrimitiveType.Cylinder, new Vector3(-0.29f, 1.08f, -0.50f), new Vector3(0f, 0f, 0f), new Vector3(0.050f, 0.28f, 0.050f), bagMat);
        SetScenePart(visual, "Backpack Side Canister_R", PrimitiveType.Cylinder, new Vector3(0.29f, 1.08f, -0.50f), new Vector3(0f, 0f, 0f), new Vector3(0.050f, 0.28f, 0.050f), bagMat);
        SetScenePart(visual, "Memory Vial", PrimitiveType.Cube, new Vector3(-0.08f, 0.82f, -0.45f), Vector3.zero, new Vector3(0.085f, 0.20f, 0.050f), glowMat);
        SetScenePart(visual, "Coat Amber Trim_L", PrimitiveType.Cube, new Vector3(-0.285f, 1.02f, 0.205f), Vector3.zero, new Vector3(0.030f, 0.62f, 0.030f), amberMat);
        SetScenePart(visual, "Coat Amber Trim_R", PrimitiveType.Cube, new Vector3(0.285f, 1.02f, 0.205f), Vector3.zero, new Vector3(0.030f, 0.62f, 0.030f), amberMat);
        SetScenePart(visual, "Coat Back Fold_L", PrimitiveType.Cube, new Vector3(-0.15f, 1.10f, -0.252f), new Vector3(0f, 0f, -4f), new Vector3(0.024f, 0.60f, 0.035f), bagMat);
        SetScenePart(visual, "Coat Back Fold_R", PrimitiveType.Cube, new Vector3(0.15f, 1.10f, -0.252f), new Vector3(0f, 0f, 4f), new Vector3(0.024f, 0.60f, 0.035f), bagMat);
        SetScenePart(visual, "Satchel Flap", PrimitiveType.Cube, new Vector3(-0.25f, 0.95f, -0.435f), new Vector3(0f, 8f, -4f), new Vector3(0.30f, 0.065f, 0.035f), bagMat);
        SetScenePart(visual, "Satchel Buckle", PrimitiveType.Cube, new Vector3(-0.25f, 0.88f, -0.535f), Vector3.zero, new Vector3(0.060f, 0.050f, 0.030f), glowMat);

        Transform leftForearm = FindDeepChild(visual, "ForearmPivot_L");
        Transform rightForearm = FindDeepChild(visual, "ForearmPivot_R");
        if (leftForearm != null)
        {
            SetScenePartUnder(visual, leftForearm, "Arm Scanner_L", PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.125f), new Vector3(8f, 0f, 0f), new Vector3(0.15f, 0.20f, 0.050f), bagMat);
            SetScenePartUnder(visual, leftForearm, "Scanner Glow_L", PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.157f), Vector3.zero, new Vector3(0.090f, 0.050f, 0.030f), glowMat);
        }

        if (rightForearm != null)
        {
            SetScenePartUnder(visual, rightForearm, "Arm Scanner_R", PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.125f), new Vector3(8f, 0f, 0f), new Vector3(0.15f, 0.20f, 0.050f), bagMat);
            SetScenePartUnder(visual, rightForearm, "Scanner Glow_R", PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.157f), Vector3.zero, new Vector3(0.090f, 0.050f, 0.030f), glowMat);
        }
    }

    private static Transform CreateScenePivot(Transform parent, string name, Vector3 localPosition, Vector3 localEuler)
    {
        Transform pivot = FindDeepChild(parent, name);
        if (pivot == null)
        {
            GameObject pivotObject = new GameObject(name);
            pivot = pivotObject.transform;
        }

        pivot.SetParent(parent, false);
        pivot.localPosition = localPosition;
        pivot.localEulerAngles = localEuler;
        pivot.localScale = Vector3.one;
        EditorUtility.SetDirty(pivot.gameObject);
        return pivot;
    }

    private static void SetScenePartUnder(Transform searchRoot, Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Material material)
    {
        Transform part = FindDeepChild(searchRoot, name);
        if (part == null)
        {
            GameObject partObject = GameObject.CreatePrimitive(primitive);
            partObject.name = name;
            part = partObject.transform;
        }

        part.SetParent(parent, false);
        part.localPosition = localPosition;
        part.localEulerAngles = localEuler;
        part.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        EditorUtility.SetDirty(part.gameObject);
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void ApplyPhotoReferenceSceneLook(Scene scene)
    {
        GameObject city = FindRoot(scene, "Silent City");
        if (city == null)
            return;

        Material cyanMat = CreateMaterial("MR3D_RecyclerAccent", new Color(0.08f, 0.78f, 0.92f), true);
        Material pipeMat = CreateMaterial("MR3D_BuildingTrim", new Color(0.06f, 0.07f, 0.075f), false);
        Material darkPanelMat = CreateMaterial("MR3D_DarkPanel", new Color(0.025f, 0.032f, 0.038f), false);
        Material boardMat = CreateMaterial("MR3D_WornBoards", new Color(0.11f, 0.095f, 0.075f), false);
        Material buildingMat = CreateMaterial("MR3D_Building", new Color(0.11f, 0.12f, 0.125f), false);

        int index = 0;
        foreach (Transform child in city.transform)
        {
            if (child == null || !child.name.StartsWith("Silent Building"))
                continue;

            AddReferenceBuildingDetails(child, index, cyanMat, pipeMat, darkPanelMat, boardMat);
            index++;
        }

        AddReferenceDistantBlocks(city.transform, buildingMat, pipeMat);
    }

    private static void AddReferenceBuildingDetails(Transform building, int index, Material cyanMat, Material pipeMat, Material darkPanelMat, Material boardMat)
    {
        Vector3 scale = building.localScale;
        float frontZ = scale.z * 0.5f + 0.075f;
        bool leftSide = building.position.x < 0f;

        CreateReferenceFacadeBox(building, "Ref Wall Panel", new Vector3(0f, scale.y * 0.03f, frontZ), new Vector3(scale.x * 0.76f, scale.y * 0.46f, 0.045f), darkPanelMat, 0f);

        for (int i = 0; i < 3; i++)
        {
            float x = -scale.x * 0.35f + i * scale.x * 0.32f + ReferenceRange(index, i + 0.2f, -0.18f, 0.18f);
            CreateReferenceFacadeBox(building, "Ref Facade Pipe", new Vector3(x, scale.y * 0.03f, frontZ + 0.045f), new Vector3(0.055f, scale.y * ReferenceRange(index, i + 1.7f, 0.38f, 0.78f), 0.05f), pipeMat, 0f);
        }

        CreateReferenceFacadeBox(building, "Ref Neon Strip", new Vector3(leftSide ? scale.x * 0.35f : -scale.x * 0.35f, -scale.y * 0.02f, frontZ + 0.06f), new Vector3(0.07f, scale.y * 0.24f, 0.055f), cyanMat, 0f);
        CreateReferenceFacadeBox(building, "Ref Door Glow", new Vector3(leftSide ? scale.x * 0.25f : -scale.x * 0.25f, -scale.y * 0.36f, frontZ + 0.07f), new Vector3(0.42f, 0.055f, 0.055f), cyanMat, 0f);

        CreateReferenceSignPanel(building, scale, frontZ, index, leftSide, darkPanelMat, cyanMat);
        CreateReferenceRecycleGlyph(building, scale, frontZ, index, leftSide, cyanMat);

        for (int i = 0; i < 2; i++)
        {
            float x = ReferenceRange(index, i + 5.4f, -scale.x * 0.26f, scale.x * 0.30f);
            float y = ReferenceRange(index, i + 6.1f, -scale.y * 0.22f, scale.y * 0.15f);
            CreateReferenceFacadeBox(building, "Ref Boarded Window", new Vector3(x, y, frontZ + 0.085f), new Vector3(0.58f, 0.10f, 0.055f), boardMat, ReferenceRange(index, i + 6.8f, -18f, 18f));
            CreateReferenceFacadeBox(building, "Ref Boarded Window", new Vector3(x, y + 0.12f, frontZ + 0.09f), new Vector3(0.52f, 0.09f, 0.055f), boardMat, ReferenceRange(index, i + 7.3f, -22f, 22f));
        }

        CreateReferenceFacadeBox(building, "Ref Rooftop Antenna", new Vector3(ReferenceRange(index, 10.2f, -scale.x * 0.25f, scale.x * 0.25f), scale.y * 0.5f + 0.85f, 0f), new Vector3(0.035f, 1.15f, 0.035f), pipeMat, 0f);
        CreateReferenceCable(building, scale, pipeMat, index);
    }

    private static void CreateReferenceFacadeBox(Transform building, string name, Vector3 worldOffset, Vector3 worldScale, Material material, float zRotation)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(building, false);
        SetChildWorldBox(box.transform, building.localScale, worldOffset, worldScale);
        box.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        box.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(box.GetComponent<Collider>());
    }

    private static void CreateReferenceSignPanel(Transform building, Vector3 scale, float frontZ, int index, bool leftSide, Material panelMat, Material glowMat)
    {
        bool vertical = index % 3 == 0;
        float x = leftSide ? -scale.x * 0.28f : scale.x * 0.26f;
        float y = scale.y * (vertical ? 0.12f : 0.20f);
        Vector3 panelScale = vertical ? new Vector3(0.36f, 1.06f, 0.055f) : new Vector3(0.90f, 0.30f, 0.055f);
        CreateReferenceFacadeBox(building, "Ref Sign Panel", new Vector3(x, y, frontZ + 0.08f), panelScale, panelMat, 0f);

        if (vertical)
        {
            for (int i = 0; i < 4; i++)
            {
                float barY = y + 0.32f - i * 0.20f;
                CreateReferenceFacadeBox(building, "Ref Sign Glow", new Vector3(x, barY, frontZ + 0.115f), new Vector3(0.060f, 0.12f, 0.060f), glowMat, 0f);
            }
        }
        else
        {
            CreateReferenceFacadeBox(building, "Ref Sign Glow", new Vector3(x - 0.24f, y, frontZ + 0.115f), new Vector3(0.060f, 0.20f, 0.060f), glowMat, 0f);
            CreateReferenceFacadeBox(building, "Ref Sign Glow", new Vector3(x + 0.03f, y + 0.055f, frontZ + 0.115f), new Vector3(0.32f, 0.045f, 0.060f), glowMat, 0f);
            CreateReferenceFacadeBox(building, "Ref Sign Glow", new Vector3(x + 0.12f, y - 0.060f, frontZ + 0.115f), new Vector3(0.22f, 0.045f, 0.060f), glowMat, 0f);
        }
    }

    private static void CreateReferenceRecycleGlyph(Transform building, Vector3 scale, float frontZ, int index, bool leftSide, Material material)
    {
        if (index % 2 != 0)
            return;

        float x = leftSide ? -scale.x * 0.27f : scale.x * 0.28f;
        float y = scale.y * 0.34f;
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f + 18f;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(x + Mathf.Cos(radians) * 0.11f, y + Mathf.Sin(radians) * 0.09f, frontZ + 0.12f);
            CreateReferenceFacadeBox(building, "Ref Recycle Glyph", offset, new Vector3(0.18f, 0.045f, 0.060f), material, angle);
        }
    }

    private static void CreateReferenceCable(Transform building, Vector3 scale, Material material, int index)
    {
        float frontZ = scale.z * 0.5f + 0.1f;
        float y = scale.y * ReferenceRange(index, 12.1f, 0.22f, 0.40f);
        CreateReferenceFacadeBox(building, "Ref Hanging Cable", new Vector3(0f, y, frontZ), new Vector3(scale.x * 0.72f, 0.035f, 0.035f), material, ReferenceRange(index, 12.7f, -5f, 5f));
    }

    private static void AddReferenceDistantBlocks(Transform cityRoot, Material buildingMat, Material trimMat)
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Ref Distant Block";
            block.transform.SetParent(cityRoot, false);
            float x = -9f + i * 4.5f;
            block.transform.position = new Vector3(x, 3.8f + i * 0.35f, 27f + i * 3f);
            block.transform.localScale = new Vector3(4.4f, 7.6f + i * 0.8f, 4.2f);
            block.GetComponent<Renderer>().sharedMaterial = buildingMat;
            Object.DestroyImmediate(block.GetComponent<Collider>());

            GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cube);
            antenna.name = "Ref Rooftop Antenna";
            antenna.transform.SetParent(block.transform, false);
            antenna.transform.localPosition = new Vector3(0.2f, 0.56f, 0f);
            antenna.transform.localScale = new Vector3(0.015f, 0.28f, 0.015f);
            antenna.GetComponent<Renderer>().sharedMaterial = trimMat;
            Object.DestroyImmediate(antenna.GetComponent<Collider>());
        }
    }

    private static void ApplyCinematicReferenceLook(Scene scene)
    {
        ConfigureCinematicRenderSettings();
        ApplyCinematicLighting(scene);
        ApplyCinematicCamera(scene);
        ApplyCinematicPostProcessing(scene);
        ApplyCinematicCityDetails(scene);
        ApplyTripoAssetUpgrade(scene);
        ApplyStoryProgressionPass(scene);
    }

    private static void ConfigureCinematicRenderSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.075f, 0.105f, 0.145f);
        RenderSettings.fogDensity = 0.052f;
        RenderSettings.ambientLight = new Color(0.052f, 0.066f, 0.086f);
        RenderSettings.skybox = null;
    }

    private static void ApplyCinematicLighting(Scene scene)
    {
        GameObject sunObject = FindRoot(scene, "Sun Light");
        if (sunObject != null)
        {
            Light sun = sunObject.GetComponent<Light>();
            if (sun != null)
            {
                sun.intensity = 0.18f;
                sun.color = new Color(0.48f, 0.58f, 0.78f);
                sun.shadows = LightShadows.Soft;
                sunObject.transform.rotation = Quaternion.Euler(16f, -28f, 0f);
                EditorUtility.SetDirty(sunObject);
            }
        }

        GameObject fillObject = FindRoot(scene, "Memory Blue Fill Light");
        if (fillObject != null)
        {
            Light fill = fillObject.GetComponent<Light>();
            if (fill != null)
            {
                fill.range = 70f;
                fill.intensity = 0.85f;
                fill.color = new Color(0.10f, 0.26f, 0.45f);
                fillObject.transform.position = new Vector3(0f, 7.4f, -6f);
                EditorUtility.SetDirty(fillObject);
            }
        }
    }

    private static void ApplyCinematicCamera(Scene scene)
    {
        GameObject cameraObject = FindRoot(scene, "Main Camera");
        GameObject player = FindRoot(scene, "Player_Recycler");
        if (cameraObject == null || player == null)
            return;

        Camera camera = cameraObject.GetComponent<Camera>();
        if (camera != null)
        {
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 320f;
            camera.allowHDR = true;

            UniversalAdditionalCameraData cameraData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
                cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
        }

        OrbitCamera3D orbit = cameraObject.GetComponent<OrbitCamera3D>();
        if (orbit != null)
        {
            orbit.offset = new Vector3(0f, 2.95f, -6.45f);
            orbit.minPitch = -24f;
            orbit.maxPitch = 74f;
            orbit.baseLookHeight = 1.45f;
            orbit.lookUpHeight = 5.2f;
            orbit.lookDownHeight = 1.05f;
            orbit.followSmooth = 14f;
            orbit.minCameraHeightAboveTarget = 0.85f;
            orbit.absoluteMinCameraY = 0.62f;
            orbit.target = player.transform;
        }

        cameraObject.transform.position = player.transform.position + new Vector3(0f, 2.95f, -6.45f);
        cameraObject.transform.LookAt(player.transform.position + Vector3.up * 1.42f);
        EditorUtility.SetDirty(cameraObject);
    }

    private static void ApplyCinematicPostProcessing(Scene scene)
    {
        EnsureFolder(Root + "/Data");
        EnsureFolder(DataPath);

        GameObject volumeObject = FindRoot(scene, "Cinematic Post Process Volume");
        if (volumeObject == null)
        {
            volumeObject = new GameObject("Cinematic Post Process Volume");
            SceneManager.MoveGameObjectToScene(volumeObject, scene);
        }

        Volume volume = volumeObject.GetComponent<Volume>();
        if (volume == null)
            volume = volumeObject.AddComponent<Volume>();

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(CinematicVolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, CinematicVolumeProfilePath);
        }

        Bloom bloom = GetOrAddVolumeOverride<Bloom>(profile);
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.72f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 1.35f;

        Vignette vignette = GetOrAddVolumeOverride<Vignette>(profile);
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.36f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.58f;
        vignette.color.overrideState = true;
        vignette.color.value = new Color(0.018f, 0.026f, 0.038f);

        ColorAdjustments color = GetOrAddVolumeOverride<ColorAdjustments>(profile);
        color.postExposure.overrideState = true;
        color.postExposure.value = -0.42f;
        color.contrast.overrideState = true;
        color.contrast.value = 24f;
        color.saturation.overrideState = true;
        color.saturation.value = -24f;
        color.colorFilter.overrideState = true;
        color.colorFilter.value = new Color(0.78f, 0.90f, 1.0f);

        FilmGrain grain = GetOrAddVolumeOverride<FilmGrain>(profile);
        grain.intensity.overrideState = true;
        grain.intensity.value = 0.22f;

        volume.isGlobal = true;
        volume.priority = 32f;
        volume.weight = 1f;
        volume.sharedProfile = profile;

        EditorUtility.SetDirty(profile);
        EditorUtility.SetDirty(volumeObject);
    }

    private static T GetOrAddVolumeOverride<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
            component = profile.Add<T>(true);

        component.active = true;
        return component;
    }

    private static void ApplyCinematicCityDetails(Scene scene)
    {
        GameObject city = FindRoot(scene, "Silent City");
        if (city == null)
            return;

        Transform root = CreateCinematicDetailRoot(scene);
        Material buildingMat = CreateMaterial("MR3D_Building", new Color(0.075f, 0.086f, 0.092f), false);
        Material roadMat = CreateMaterial("MR3D_Road", new Color(0.045f, 0.050f, 0.055f), false);
        Material trimMat = CreateMaterial("MR3D_BuildingTrim", new Color(0.035f, 0.040f, 0.045f), false);
        Material panelMat = CreateMaterial("MR3D_DarkPanel", new Color(0.017f, 0.023f, 0.030f), false);
        Material boardMat = CreateMaterial("MR3D_WornBoards", new Color(0.095f, 0.078f, 0.060f), false);
        Material grimeMat = CreateMaterial("MR3D_CinematicGrime", new Color(0.028f, 0.034f, 0.038f), false);
        Material roadPatchMat = CreateMaterial("MR3D_CinematicRoadPatch", new Color(0.030f, 0.035f, 0.040f), false);
        Material debrisMat = CreateMaterial("MR3D_Debris", new Color(0.12f, 0.125f, 0.120f), false);
        Material puddleMat = CreateCinematicPuddleMaterial();
        Material cyanMat = CreateMaterial("MR3D_RecyclerAccent", new Color(0.055f, 0.72f, 0.88f), true);
        ApplyCinematicMaterialTextures();

        GameObject avenue = FindRoot(scene, "Main Avenue");
        if (avenue != null)
        {
            avenue.transform.position = new Vector3(0f, 0.02f, 0f);
            avenue.transform.localScale = new Vector3(12.5f, 0.04f, 58f);
            if (avenue.TryGetComponent(out Renderer avenueRenderer))
                avenueRenderer.sharedMaterial = roadMat;
            EditorUtility.SetDirty(avenue);
        }

        GameObject ground = FindRoot(scene, "Abandoned City Ground");
        if (ground != null)
        {
            ground.transform.localScale = new Vector3(14f, 1f, 14f);
            EditorUtility.SetDirty(ground);
        }

        ExpandCinematicCityLayout(city.transform);
        BuildArchiveTower(scene, cyanMat, trimMat, panelMat);

        int index = 0;
        foreach (Transform child in city.transform)
        {
            if (child == null || !child.name.StartsWith("Silent Building"))
                continue;

            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = buildingMat;

            AddCinematicBuildingDetails(child, index, grimeMat, panelMat, trimMat, boardMat, cyanMat);
            AddCinematicFacadeStructure(child, index, panelMat, trimMat, debrisMat, grimeMat);
            AddCinematicConcreteDamage(child, index, debrisMat, grimeMat, trimMat);
            AddCinematicFacadeBaseWear(child, index, grimeMat, trimMat);
            index++;
        }

        AlignCinematicStreetLights(city.transform);
        AddCinematicRoadDetails(root, roadPatchMat, debrisMat, boardMat);
        AddCinematicAvenueDepthCues(root, panelMat, trimMat, cyanMat, roadPatchMat);
        AddCinematicBackgroundDepth(root, buildingMat, trimMat);
        AddCinematicOverheadCables(root, trimMat);
        AddCinematicWetHighlights(root, puddleMat, cyanMat);
        AddCinematicForegroundFraming(root, buildingMat, trimMat, cyanMat);
        AddCinematicPlayerSilhouette(scene, cyanMat, trimMat);
        AddCinematicWindowDepth(city.transform, panelMat, trimMat);
        AddCinematicRoofBreakup(city.transform, buildingMat, trimMat);
        AddCinematicSideAlleyDepth(root, buildingMat, trimMat, cyanMat);
    }

    private static Transform CreateCinematicDetailRoot(Scene scene)
    {
        GameObject root = FindRoot(scene, "Cinematic Detail Root");
        if (root == null)
        {
            root = new GameObject("Cinematic Detail Root");
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root.transform;
    }

    private static void AddCinematicBuildingDetails(Transform building, int index, Material grimeMat, Material panelMat, Material trimMat, Material boardMat, Material cyanMat)
    {
        Vector3 scale = building.localScale;
        float frontZ = scale.z * 0.5f + 0.105f;
        bool leftSide = building.position.x < 0f;

        for (int i = 0; i < 5; i++)
        {
            float x = ReferenceRange(index, i + 42.1f, -scale.x * 0.36f, scale.x * 0.36f);
            float y = ReferenceRange(index, i + 43.6f, -scale.y * 0.18f, scale.y * 0.34f);
            float width = ReferenceRange(index, i + 44.2f, 0.30f, 0.82f);
            float height = ReferenceRange(index, i + 45.7f, 0.42f, 1.55f);
            CreateReferenceFacadeBox(building, "Cinematic Grime Patch", new Vector3(x, y, frontZ), new Vector3(width, height, 0.035f), grimeMat, ReferenceRange(index, i + 46.3f, -8f, 8f));
        }

        for (int i = 0; i < 4; i++)
        {
            float x = ReferenceRange(index, i + 52.0f, -scale.x * 0.38f, scale.x * 0.38f);
            float y = ReferenceRange(index, i + 53.0f, -scale.y * 0.18f, scale.y * 0.38f);
            float length = ReferenceRange(index, i + 54.0f, 0.72f, 1.55f);
            CreateReferenceFacadeBox(building, "Cinematic Wall Crack", new Vector3(x, y, frontZ + 0.022f), new Vector3(0.035f, length, 0.035f), grimeMat, ReferenceRange(index, i + 55.0f, -36f, 36f));
        }

        float signX = leftSide ? -scale.x * 0.30f : scale.x * 0.30f;
        float signY = scale.y * 0.23f;
        CreateReferenceFacadeBox(building, "Cinematic Sign Plate", new Vector3(signX, signY, frontZ + 0.035f), new Vector3(0.50f, 1.05f, 0.050f), panelMat, 0f);
        for (int i = 0; i < 5; i++)
        {
            float barY = signY + 0.36f - i * 0.18f;
            CreateReferenceFacadeBox(building, "Cinematic Sign Glyph", new Vector3(signX, barY, frontZ + 0.075f), new Vector3(0.055f, 0.115f, 0.060f), cyanMat, 0f);
        }

        for (int i = 0; i < 2; i++)
        {
            float ventX = ReferenceRange(index, i + 62.1f, -scale.x * 0.26f, scale.x * 0.26f);
            float ventY = ReferenceRange(index, i + 63.1f, -scale.y * 0.08f, scale.y * 0.26f);
            CreateReferenceFacadeBox(building, "Cinematic Vent Panel", new Vector3(ventX, ventY, frontZ + 0.045f), new Vector3(0.55f, 0.32f, 0.055f), panelMat, 0f);
            for (int l = 0; l < 3; l++)
                CreateReferenceFacadeBox(building, "Cinematic Vent Slat", new Vector3(ventX, ventY - 0.10f + l * 0.10f, frontZ + 0.085f), new Vector3(0.42f, 0.025f, 0.055f), trimMat, 0f);
        }

        for (int i = 0; i < 2; i++)
        {
            float x = ReferenceRange(index, i + 68.1f, -scale.x * 0.26f, scale.x * 0.26f);
            float y = ReferenceRange(index, i + 69.1f, -scale.y * 0.30f, -scale.y * 0.08f);
            CreateReferenceFacadeBox(building, "Cinematic Leaning Board", new Vector3(x, y, frontZ + 0.065f), new Vector3(0.22f, 1.05f, 0.070f), boardMat, ReferenceRange(index, i + 70.2f, -18f, 18f));
        }
    }

    private static void AddCinematicRoadDetails(Transform root, Material roadPatchMat, Material debrisMat, Material boardMat)
    {
        for (int i = 0; i < 22; i++)
        {
            float z = -20f + i * 2.0f + ReferenceRange(i, 71.3f, -0.55f, 0.55f);
            float x = ReferenceRange(i, 72.9f, -2.7f, 2.7f);
            Vector3 size = new Vector3(ReferenceRange(i, 73.7f, 0.75f, 2.6f), 0.018f, ReferenceRange(i, 74.4f, 0.55f, 2.2f));
            CreateCinematicWorldBox(root, "Cinematic Road Patch", new Vector3(x, 0.065f, z), size, roadPatchMat, new Vector3(0f, ReferenceRange(i, 75.1f, -8f, 8f), 0f));
        }

        for (int i = 0; i < 16; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            Vector3 position = new Vector3(side * ReferenceRange(i, 77.2f, 5.0f, 7.2f), 0.13f, ReferenceRange(i, 78.1f, -24f, 27f));
            Vector3 size = new Vector3(ReferenceRange(i, 79.3f, 0.25f, 0.85f), ReferenceRange(i, 80.2f, 0.08f, 0.24f), ReferenceRange(i, 81.1f, 0.35f, 1.2f));
            CreateCinematicWorldBox(root, "Cinematic Street Rubble", position, size, debrisMat, new Vector3(0f, ReferenceRange(i, 82.5f, 0f, 180f), ReferenceRange(i, 83.4f, -10f, 10f)));

            if (i % 2 == 0)
                AddCinematicRubbleCluster(root, position + new Vector3(-side * 0.18f, 0.025f, 0.12f), i, debrisMat);
        }

        for (int i = 0; i < 6; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            CreateCinematicWorldBox(root, "Cinematic Fallen Board", new Vector3(side * ReferenceRange(i, 84.4f, 6.0f, 7.8f), 0.17f, ReferenceRange(i, 85.4f, -20f, 24f)), new Vector3(0.24f, 0.10f, 1.65f), boardMat, new Vector3(ReferenceRange(i, 86.4f, -4f, 4f), ReferenceRange(i, 87.4f, -32f, 32f), ReferenceRange(i, 88.4f, -12f, 12f)));
        }
    }

    private static void ExpandCinematicCityLayout(Transform cityRoot)
    {
        Vector3[] expandedPositions =
        {
            new Vector3(-14.5f, 2.8f, -22f), new Vector3(14.8f, 3.8f, -21f),
            new Vector3(-15.5f, 4.6f, -10f), new Vector3(15.2f, 4.2f, -10f),
            new Vector3(-14.8f, 3.1f, 2f), new Vector3(14.6f, 5.4f, 3f),
            new Vector3(-15.0f, 4.8f, 15f), new Vector3(15.4f, 6.2f, 16f),
            new Vector3(-20.2f, 3.4f, 8f), new Vector3(20.2f, 4.1f, 8f)
        };

        for (int i = 0; i < expandedPositions.Length; i++)
        {
            Transform building = FindDeepChild(cityRoot, "Silent Building_" + i);
            if (building == null)
                continue;

            building.position = expandedPositions[i];
            EditorUtility.SetDirty(building.gameObject);
        }
    }

    private static void BuildArchiveTower(Scene scene, Material cyanMat, Material trimMat, Material panelMat)
    {
        GameObject terminal = FindRoot(scene, "Central Archive Terminal");
        if (terminal == null)
            return;

        terminal.transform.position = new Vector3(0f, 3.9f, 31.5f);
        terminal.transform.localScale = new Vector3(3.4f, 7.8f, 1.6f);

        Renderer renderer = terminal.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = panelMat;

        Collider collider = terminal.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
            collider.enabled = true;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(TripoAssetPath + "/Buildings/PostApocalypticTower/PostApocalypticTower.fbx") != null)
        {
            ConfigureArchiveTriggerOnly(terminal);
            EditorUtility.SetDirty(terminal);
            return;
        }

        SetTerminalPart(terminal.transform, "Archive Tower Core", PrimitiveType.Cube, new Vector3(0f, 0.04f, -0.08f), Vector3.zero, new Vector3(0.62f, 1.03f, 0.42f), panelMat);
        SetTerminalPart(terminal.transform, "Archive Tower Crown", PrimitiveType.Cube, new Vector3(0f, 0.58f, 0f), Vector3.zero, new Vector3(1.18f, 0.16f, 1.10f), trimMat);
        SetTerminalPart(terminal.transform, "Archive Tower Base", PrimitiveType.Cube, new Vector3(0f, -0.54f, 0.04f), Vector3.zero, new Vector3(1.28f, 0.16f, 1.16f), trimMat);
        SetTerminalPart(terminal.transform, "Archive Vertical Light", PrimitiveType.Cube, new Vector3(0f, 0.03f, -0.53f), Vector3.zero, new Vector3(0.055f, 0.72f, 0.045f), cyanMat);
        SetTerminalPart(terminal.transform, "Archive Memory Eye", PrimitiveType.Sphere, new Vector3(0f, 0.35f, -0.56f), Vector3.zero, new Vector3(0.18f, 0.18f, 0.045f), cyanMat);

        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -0.42f : 0.42f;
            float y = -0.34f + (i % 2) * 0.46f;
            SetTerminalPart(terminal.transform, "Archive Side Light_" + i, PrimitiveType.Cube, new Vector3(x, y, -0.53f), Vector3.zero, new Vector3(0.050f, 0.20f, 0.040f), cyanMat);
        }

        GameObject beacon = FindRoot(scene, "Terminal Beacon");
        if (beacon == null)
        {
            beacon = new GameObject("Terminal Beacon");
            SceneManager.MoveGameObjectToScene(beacon, scene);
        }

        beacon.transform.position = terminal.transform.position + Vector3.up * 5.0f;
        Light light = beacon.GetComponent<Light>();
        if (light == null)
            light = beacon.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 24f;
        light.intensity = 4.0f;
        light.color = new Color(0.1f, 1f, 0.75f);

        EditorUtility.SetDirty(terminal);
        EditorUtility.SetDirty(beacon);
    }

    private static void ConfigureArchiveTriggerOnly(GameObject terminal)
    {
        terminal.name = "Central Archive Terminal";
        terminal.transform.position = new Vector3(0f, 1.8f, 32.0f);
        terminal.transform.localScale = new Vector3(4.2f, 3.6f, 4.2f);

        Renderer renderer = terminal.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;

        string[] visualChildren =
        {
            "Archive Tower Core",
            "Archive Tower Crown",
            "Archive Tower Base",
            "Archive Vertical Light",
            "Archive Memory Eye"
        };

        for (int i = 0; i < visualChildren.Length; i++)
        {
            Transform child = FindDeepChild(terminal.transform, visualChildren[i]);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        for (int i = terminal.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = terminal.transform.GetChild(i);
            if (child != null && child.name.StartsWith("Archive Side Light_"))
                Object.DestroyImmediate(child.gameObject);
        }

        Collider collider = terminal.GetComponent<Collider>();
        if (collider == null)
            collider = terminal.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.enabled = true;
    }

    private static void SetTerminalPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Material material)
    {
        Transform part = FindDeepChild(parent, name);
        if (part == null)
        {
            GameObject partObject = GameObject.CreatePrimitive(primitive);
            partObject.name = name;
            part = partObject.transform;
        }

        part.SetParent(parent, false);
        part.localPosition = localPosition;
        part.localEulerAngles = localEuler;
        part.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        EditorUtility.SetDirty(part.gameObject);
    }

    private static void ApplyTripoAssetUpgrade(Scene scene)
    {
        AssetDatabase.ImportAsset(TripoAssetPath, ImportAssetOptions.ImportRecursive);

        GameObject existing = FindRoot(scene, "Tripo Quality Pass");
        if (existing != null)
            Object.DestroyImmediate(existing);
        RemoveAllSceneObjectsWithPrefix(scene, "Broken Streetlight_");

        GameObject root = new GameObject("Tripo Quality Pass");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = Vector3.zero;

        PlaceTripoPrefab(scene, root.transform, "Tripo Building Ruined Concrete_L", "Buildings/RuinedConcreteBuilding/RuinedConcreteBuilding.fbx", new Vector3(-19.2f, 0f, -20.0f), new Vector3(0f, 92f, 0f), 13.6f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Building Post Apocalyptic_R", "Buildings/PostApocalypticBuilding/PostApocalypticBuilding.fbx", new Vector3(19.3f, 0f, -18.2f), new Vector3(0f, -90f, 0f), 14.8f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Building Apartment_L", "Buildings/RuinedApartmentBuilding/RuinedApartmentBuilding.fbx", new Vector3(-19.8f, 0f, -6.2f), new Vector3(0f, 88f, 0f), 16.4f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Building Ruined_R", "Buildings/RuinedBuilding/RuinedBuilding.fbx", new Vector3(19.6f, 0f, -1.2f), new Vector3(0f, -92f, 0f), 14.2f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Building Korean_L", "Buildings/KoreanRuinedBuilding/KoreanRuinedBuilding.fbx", new Vector3(-19.5f, 0f, 11.4f), new Vector3(0f, 94f, 0f), 13.8f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Building City Block_R", "Buildings/RuinedCityBlock/RuinedCityBlock.fbx", new Vector3(19.4f, 0f, 14.8f), new Vector3(0f, -88f, 0f), 11.4f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Background Block_L", "Buildings/RuinedCityBlock/RuinedCityBlock.fbx", new Vector3(-29.0f, 0f, 32.0f), new Vector3(0f, 38f, 0f), 15.6f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Background Block_R", "Buildings/RuinedConcreteBuilding/RuinedConcreteBuilding.fbx", new Vector3(29.0f, 0f, 33.5f), new Vector3(0f, -34f, 0f), 17.2f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Archive Tower", "Buildings/PostApocalypticTower/PostApocalypticTower.fbx", new Vector3(0f, 0f, 33.5f), new Vector3(0f, 270f, 0f), 24.0f, true);

        PlaceTripoPrefab(scene, root.transform, "Tripo Street Lamp_L0", "Props/StreetLamps/StreetLamps.fbx", new Vector3(-7.5f, 0f, -17.0f), new Vector3(0f, 15f, 0f), 6.2f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Street Lamp_R0", "Props/StreetLamps/StreetLamps.fbx", new Vector3(7.5f, 0f, -11.0f), new Vector3(0f, -165f, 0f), 6.2f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Street Lamp_L1", "Props/StreetLamps/StreetLamps.fbx", new Vector3(-7.6f, 0f, 3.5f), new Vector3(0f, -4f, 0f), 6.0f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Street Lamp_R1", "Props/StreetLamps/StreetLamps.fbx", new Vector3(7.6f, 0f, 10.5f), new Vector3(0f, 176f, 0f), 6.0f, true);

        PlaceTripoPrefab(scene, root.transform, "Tripo Rubble Near_L", "Environment/ConcreteRubble/ConcreteRubble.fbx", new Vector3(-4.2f, 0f, -10.0f), new Vector3(0f, 28f, 0f), 0.72f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Rubble Near_R", "Environment/ConcreteRubble/ConcreteRubble.fbx", new Vector3(4.7f, 0f, -3.2f), new Vector3(0f, -52f, 0f), 0.68f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Rubble Far_L", "Environment/ConcreteRubble/ConcreteRubble.fbx", new Vector3(-5.6f, 0f, 15.5f), new Vector3(0f, 103f, 0f), 0.82f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Rubble Far_R", "Environment/ConcreteRubble/ConcreteRubble.fbx", new Vector3(5.3f, 0f, 21.0f), new Vector3(0f, -16f, 0f), 0.76f, true);

        PlaceTripoPrefab(scene, root.transform, "Tripo Wall Pipes_L", "Props/Pipes/Pipes.fbx", new Vector3(-9.2f, 0f, -6.8f), new Vector3(0f, 88f, 0f), 2.3f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Wall Pipes_R", "Props/Pipes/Pipes.fbx", new Vector3(9.2f, 0f, 5.4f), new Vector3(0f, -92f, 0f), 2.1f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Air Conditioner_L", "Props/AirConditioners/AirConditioners.fbx", new Vector3(-9.0f, 0f, 1.8f), new Vector3(0f, 90f, 0f), 1.25f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Window Barricade_R", "Props/WindowBarricades/WindowBarricades.fbx", new Vector3(9.0f, 0f, -13.8f), new Vector3(0f, -90f, 0f), 1.5f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Metal Door_L", "Props/MetalDoors/MetalDoors.fbx", new Vector3(-8.9f, 0f, 8.6f), new Vector3(0f, 90f, 0f), 1.8f, true);
        PlaceTripoPrefab(scene, root.transform, "Tripo Recycling Sign_R", "Props/RecyclingSigns/RecyclingSigns.fbx", new Vector3(8.9f, 0f, 18.2f), new Vector3(0f, -90f, 0f), 1.45f, true);

        PlaceTripoPlayerVisual(scene);

        EditorUtility.SetDirty(root);
    }

    private static GameObject PlaceTripoPrefab(Scene scene, Transform parent, string name, string relativePath, Vector3 position, Vector3 euler, float targetHeight, bool removeColliders)
    {
        string assetPath = TripoAssetPath + "/" + relativePath;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning("Missing Tripo asset: " + assetPath);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null)
        {
            instance = Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(instance, scene);
        }

        instance.name = name;
        instance.transform.position = position;
        instance.transform.rotation = GetTripoWorldRotation(euler);
        instance.transform.localScale = Vector3.one;
        instance.transform.SetParent(parent, true);

        Material material = CreateTripoMaterial(Path.GetFileNameWithoutExtension(assetPath), assetPath);
        ApplyMaterialRecursive(instance.transform, material);
        NormalizeImportedModel(instance.transform, position.y, targetHeight);

        if (removeColliders)
            RemoveCollidersRecursive(instance.transform);

        SetStaticRecursive(instance.transform, parent.name == "Tripo Quality Pass");
        EditorUtility.SetDirty(instance);
        return instance;
    }

    private static void PlaceTripoPlayerVisual(Scene scene)
    {
        GameObject player = FindRoot(scene, "Player_Recycler");
        if (player == null)
            return;

        Transform existing = FindDeepChild(player.transform, "Tripo Player Visual");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject visual = PlaceTripoPrefab(scene, player.transform, "Tripo Player Visual", "Characters/PostApocalypticExplorer/PostApocalypticExplorer.fbx", player.transform.position, player.transform.eulerAngles, 2.05f, true);
        if (visual == null)
            return;

        visual.transform.SetParent(player.transform, true);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
        NormalizeImportedModel(visual.transform, player.transform.position.y, 2.05f);

        Transform proceduralVisual = FindDeepChild(player.transform, "RecyclerVisual");
        if (proceduralVisual != null)
            proceduralVisual.gameObject.SetActive(false);

        EditorUtility.SetDirty(player);
    }

    private static Quaternion GetTripoWorldRotation(Vector3 euler)
    {
        return Quaternion.Euler(0f, euler.y, 0f) * Quaternion.Euler(-90f, 0f, 0f);
    }

    private static Material CreateTripoMaterial(string name, string assetPath)
    {
        EnsureFolder(TripoMaterialPath);
        string materialPath = TripoMaterialPath + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        string folder = Path.GetDirectoryName(assetPath).Replace("\\", "/");
        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + baseName + ".fbm/BaseColor.jpeg");

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", baseColor);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", baseColor);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.18f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyMaterialRecursive(Transform root, Material material)
    {
        if (material == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sharedMaterial = material;
    }

    private static void NormalizeImportedModel(Transform root, float targetBottomY, float targetHeight)
    {
        if (root == null || targetHeight <= 0.01f)
            return;

        if (!TryGetRendererBounds(root, out Bounds bounds) || bounds.size.y <= 0.001f)
            return;

        float scale = targetHeight / bounds.size.y;
        root.localScale *= scale;

        if (!TryGetRendererBounds(root, out bounds))
            return;

        Vector3 position = root.position;
        position.y += targetBottomY - bounds.min.y;
        root.position = position;
    }

    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(root.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return hasBounds;
    }

    private static void ApplyStoryProgressionPass(Scene scene)
    {
        GameObject existing = FindRoot(scene, "Story Progression Pass");
        if (existing != null)
            Object.DestroyImmediate(existing);

        GameObject root = new GameObject("Story Progression Pass");
        SceneManager.MoveGameObjectToScene(root, scene);

        GameObject managers = FindRoot(scene, "Managers");
        if (managers != null && managers.GetComponent<MemoryAudio3D>() == null)
        {
            managers.AddComponent<MemoryAudio3D>();
            EditorUtility.SetDirty(managers);
        }

        Material panelMat = CreateMaterial("MR3D_StoryTerminalPanel", new Color(0.035f, 0.055f, 0.065f), false);
        Material glowMat = CreateMaterial("MR3D_StoryTerminalGlow", new Color(0.08f, 0.75f, 0.95f), true);

        CreateStoryTerminal(scene, root.transform, "Story Terminal_Hospital", new Vector3(7.8f, 1.15f, -8.5f), new Vector3(0f, -28f, 0f),
            "병원 기록 단말",
            "초기 기억 삭제 기술은 치료 장비로 승인되었습니다. 그러나 승인 문서에는 '고통의 제거'와 '증언의 제거'가 같은 절차로 묶여 있습니다.",
            "치료 동의서 기억을 복원하면 도시가 왜 침묵했는지 알 수 있습니다.",
            panelMat, glowMat);

        CreateStoryTerminal(scene, root.transform, "Story Terminal_School", new Vector3(-11.5f, 1.15f, 3.5f), new Vector3(0f, 42f, 0f),
            "폐교 출석 단말",
            "졸업 앨범에서 지워진 이름은 한 명이 아니었습니다. 행정 서버는 집단 괴롭힘, 사고, 목격자 기록을 같은 묶음으로 보관했습니다.",
            "후회와 죄책감 태그의 기억을 비교해 보세요.",
            panelMat, glowMat);

        CreateStoryTerminal(scene, root.transform, "Story Terminal_Broadcast", new Vector3(9.5f, 1.15f, 14.0f), new Vector3(0f, -64f, 0f),
            "방송국 중계 단말",
            "마지막 재난 방송은 송출되지 않았습니다. 누군가 시민 대피 문구를 지우고, 대신 '안정을 위해 잠들라'는 문장을 덮어썼습니다.",
            "녹슨 방송 마이크와 중앙 아카이브의 자장가를 복원하면 마지막 명령의 윤곽이 드러납니다.",
            panelMat, glowMat);

        CreateStoryTerminal(scene, root.transform, "Story Terminal_ArchiveGate", new Vector3(-4.6f, 1.15f, 25.5f), new Vector3(0f, 8f, 0f),
            "아카이브 관문 릴레이",
            "중앙 아카이브는 단순 저장소가 아닙니다. 회수원이 내린 선택을 바탕으로 도시의 다음 상태를 계산하는 판단 장치입니다.",
            "기억 5개 이상을 복원하고 처리해야 최종 접속이 열립니다.",
            panelMat, glowMat);

        GameObject terminal = FindRoot(scene, "Central Archive Terminal");
        if (terminal != null)
        {
            ArchiveTerminal3D archive = terminal.GetComponent<ArchiveTerminal3D>();
            if (archive == null)
                archive = terminal.AddComponent<ArchiveTerminal3D>();
            archive.requiredDecisions = 5;
            EditorUtility.SetDirty(terminal);
        }

        EditorUtility.SetDirty(root);
    }

    private static void CreateStoryTerminal(Scene scene, Transform parent, string name, Vector3 position, Vector3 euler, string title, string body, string hint, Material panelMat, Material glowMat)
    {
        GameObject terminal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        terminal.name = name;
        SceneManager.MoveGameObjectToScene(terminal, scene);
        terminal.transform.SetParent(parent, true);
        terminal.transform.position = position;
        terminal.transform.rotation = Quaternion.Euler(euler);
        terminal.transform.localScale = new Vector3(0.72f, 1.35f, 0.12f);
        terminal.GetComponent<Renderer>().sharedMaterial = panelMat;

        BoxCollider collider = terminal.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(3.0f, 1.4f, 3.0f);

        CityLoreTerminal3D lore = terminal.AddComponent<CityLoreTerminal3D>();
        lore.terminalTitle = title;
        lore.terminalBody = body;
        lore.objectiveHint = hint;

        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glow.name = "Terminal Cyan Line";
        glow.transform.SetParent(terminal.transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.08f, -0.56f);
        glow.transform.localScale = new Vector3(0.12f, 0.74f, 0.04f);
        glow.GetComponent<Renderer>().sharedMaterial = glowMat;
        Object.DestroyImmediate(glow.GetComponent<Collider>());

        GameObject lightObject = new GameObject("Terminal Hint Light");
        lightObject.transform.SetParent(terminal.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.1f, -1.0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 4.5f;
        light.intensity = 1.0f;
        light.color = new Color(0.12f, 0.78f, 1f);
    }

    private static void RemoveCollidersRecursive(Transform root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            Object.DestroyImmediate(colliders[i]);
    }

    private static void SetStaticRecursive(Transform root, bool isStatic)
    {
        root.gameObject.isStatic = isStatic;
        foreach (Transform child in root)
            SetStaticRecursive(child, isStatic);
    }

    private static void AddCinematicFacadeStructure(Transform building, int index, Material panelMat, Material trimMat, Material debrisMat, Material grimeMat)
    {
        Vector3 scale = building.localScale;
        float frontZ = scale.z * 0.5f + 0.145f;
        float doorX = building.position.x < 0f ? scale.x * 0.24f : -scale.x * 0.24f;
        float doorY = -scale.y * 0.34f;

        CreateReferenceFacadeBox(building, "Cinematic Door Recess", new Vector3(doorX, doorY, frontZ), new Vector3(0.72f, 1.18f, 0.070f), panelMat, 0f);
        CreateReferenceFacadeBox(building, "Cinematic Door Header", new Vector3(doorX, doorY + 0.64f, frontZ + 0.045f), new Vector3(0.86f, 0.085f, 0.080f), trimMat, 0f);
        CreateReferenceFacadeBox(building, "Cinematic Door Side Frame", new Vector3(doorX - 0.42f, doorY, frontZ + 0.045f), new Vector3(0.065f, 1.18f, 0.075f), trimMat, 0f);
        CreateReferenceFacadeBox(building, "Cinematic Door Side Frame", new Vector3(doorX + 0.42f, doorY, frontZ + 0.045f), new Vector3(0.065f, 1.18f, 0.075f), trimMat, 0f);

        for (int i = 0; i < 4; i++)
        {
            float y = -scale.y * 0.32f + i * (scale.y * 0.18f);
            CreateReferenceFacadeBox(building, "Cinematic Panel Seam", new Vector3(0f, y, frontZ + 0.020f), new Vector3(scale.x * 0.78f, 0.025f, 0.035f), grimeMat, ReferenceRange(index, i + 171.4f, -1.5f, 1.5f));
        }

        for (int i = 0; i < 3; i++)
        {
            float x = -scale.x * 0.30f + i * (scale.x * 0.30f);
            CreateReferenceFacadeBox(building, "Cinematic Vertical Panel Seam", new Vector3(x, scale.y * 0.03f, frontZ + 0.022f), new Vector3(0.024f, scale.y * 0.48f, 0.035f), grimeMat, ReferenceRange(index, i + 174.4f, -1.0f, 1.0f));
        }

        if (index % 2 == 0)
        {
            CreateReferenceFacadeBox(building, "Cinematic Collapsed Lintel", new Vector3(-doorX * 0.55f, -scale.y * 0.42f, frontZ + 0.060f), new Vector3(0.78f, 0.16f, 0.090f), debrisMat, ReferenceRange(index, 181.4f, -10f, 10f));
            CreateReferenceFacadeBox(building, "Cinematic Wall Chunk", new Vector3(-doorX * 0.70f, -scale.y * 0.50f + 0.18f, frontZ + 0.075f), new Vector3(0.30f, 0.22f, 0.120f), debrisMat, ReferenceRange(index, 182.4f, -18f, 18f));
        }
    }

    private static void AddCinematicRubbleCluster(Transform root, Vector3 center, int index, Material debrisMat)
    {
        for (int i = 0; i < 4; i++)
        {
            float x = center.x + ReferenceRange(index, i + 191.1f, -0.34f, 0.34f);
            float z = center.z + ReferenceRange(index, i + 192.1f, -0.38f, 0.38f);
            Vector3 size = new Vector3(ReferenceRange(index, i + 193.1f, 0.10f, 0.34f), ReferenceRange(index, i + 194.1f, 0.045f, 0.12f), ReferenceRange(index, i + 195.1f, 0.12f, 0.42f));
            CreateCinematicWorldBox(root, "Cinematic Concrete Fragment", new Vector3(x, 0.075f + size.y * 0.5f, z), size, debrisMat, new Vector3(ReferenceRange(index, i + 196.1f, -8f, 8f), ReferenceRange(index, i + 197.1f, 0f, 180f), ReferenceRange(index, i + 198.1f, -12f, 12f)));
        }
    }

    private static void AddCinematicConcreteDamage(Transform building, int index, Material debrisMat, Material grimeMat, Material trimMat)
    {
        Vector3 scale = building.localScale;
        float frontZ = scale.z * 0.5f + 0.158f;

        for (int i = 0; i < 3; i++)
        {
            float x = ReferenceRange(index, i + 141.4f, -scale.x * 0.34f, scale.x * 0.34f);
            float y = ReferenceRange(index, i + 142.4f, -scale.y * 0.22f, scale.y * 0.36f);
            float width = ReferenceRange(index, i + 143.4f, 0.46f, 1.18f);
            float height = ReferenceRange(index, i + 144.4f, 0.28f, 0.92f);
            CreateReferenceFacadeBox(building, "Cinematic Exposed Concrete Skin", new Vector3(x, y, frontZ), new Vector3(width, height, 0.052f), debrisMat, ReferenceRange(index, i + 145.4f, -5f, 5f));
        }

        for (int i = 0; i < 2; i++)
        {
            float x = ReferenceRange(index, i + 151.6f, -scale.x * 0.26f, scale.x * 0.26f);
            float y = ReferenceRange(index, i + 152.6f, -scale.y * 0.08f, scale.y * 0.30f);
            CreateReferenceFacadeBox(building, "Cinematic Burned Concrete Scar", new Vector3(x, y, frontZ + 0.035f), new Vector3(ReferenceRange(index, i + 153.6f, 0.38f, 0.80f), ReferenceRange(index, i + 154.6f, 0.20f, 0.58f), 0.045f), grimeMat, ReferenceRange(index, i + 155.6f, -12f, 12f));
        }

        if (index % 2 == 1)
        {
            float rebarX = building.position.x < 0f ? scale.x * 0.24f : -scale.x * 0.24f;
            CreateReferenceFacadeBox(building, "Cinematic Exposed Rebar", new Vector3(rebarX, scale.y * 0.08f, frontZ + 0.060f), new Vector3(0.040f, 1.10f, 0.045f), trimMat, 9f);
            CreateReferenceFacadeBox(building, "Cinematic Exposed Rebar", new Vector3(rebarX + 0.14f, scale.y * 0.03f, frontZ + 0.062f), new Vector3(0.035f, 0.82f, 0.045f), trimMat, -7f);
        }
    }

    private static void AlignCinematicStreetLights(Transform cityRoot)
    {
        List<Transform> lights = new List<Transform>();
        foreach (Transform child in cityRoot)
        {
            if (child != null && child.name.StartsWith("Broken Streetlight_"))
                lights.Add(child);
        }

        lights.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        for (int i = 0; i < lights.Count; i++)
        {
            Transform lightRoot = lights[i];
            float side = i % 2 == 0 ? -1f : 1f;
            float z = -16.5f + (i / 2) * 8.0f;
            lightRoot.localPosition = new Vector3(side * 6.8f, 0f, z);
            lightRoot.localEulerAngles = new Vector3(0f, side > 0f ? 180f : 0f, side * ReferenceRange(i, 161.2f, -3.0f, 2.0f));
            lightRoot.localScale = Vector3.one;

            Transform lamp = FindDeepChild(lightRoot, "Streetlight Lamp");
            if (lamp != null)
                lamp.localScale = Vector3.one * 0.115f;

            EditorUtility.SetDirty(lightRoot.gameObject);
        }
    }

    private static void AddCinematicFacadeBaseWear(Transform building, int index, Material grimeMat, Material trimMat)
    {
        Vector3 scale = building.localScale;
        float frontZ = scale.z * 0.5f + 0.132f;

        CreateReferenceFacadeBox(building, "Cinematic Base Soot Band", new Vector3(0f, -scale.y * 0.42f, frontZ), new Vector3(scale.x * 0.86f, 0.20f, 0.040f), grimeMat, 0f);

        for (int i = 0; i < 3; i++)
        {
            float x = ReferenceRange(index, i + 121.3f, -scale.x * 0.34f, scale.x * 0.34f);
            float y = ReferenceRange(index, i + 122.3f, -scale.y * 0.38f, -scale.y * 0.20f);
            float height = ReferenceRange(index, i + 123.3f, 0.32f, 0.86f);
            CreateReferenceFacadeBox(building, "Cinematic Rain Streak", new Vector3(x, y, frontZ + 0.028f), new Vector3(0.032f, height, 0.040f), grimeMat, ReferenceRange(index, i + 124.3f, -8f, 8f));
        }

        if (index % 3 == 0)
        {
            float pipeX = building.position.x < 0f ? scale.x * 0.40f : -scale.x * 0.40f;
            CreateReferenceFacadeBox(building, "Cinematic Facade Downpipe", new Vector3(pipeX, -scale.y * 0.05f, frontZ + 0.055f), new Vector3(0.070f, scale.y * 0.72f, 0.065f), trimMat, 0f);
        }
    }

    private static void AddCinematicAvenueDepthCues(Transform root, Material panelMat, Material trimMat, Material cyanMat, Material roadPatchMat)
    {
        for (int i = 0; i < 6; i++)
        {
            float z = -20f + i * 7.2f;
            float width = Mathf.Lerp(4.8f, 2.0f, i / 5f);
            CreateCinematicWorldBox(root, "Cinematic Street Cross Shadow", new Vector3(0f, 0.064f, z + 0.55f), new Vector3(width, 0.010f, 0.34f), roadPatchMat, new Vector3(0f, ReferenceRange(i, 131.2f, -5f, 5f), 0f));
        }

        CreateCinematicWorldBox(root, "Cinematic Distant Memory Gate", new Vector3(0f, 2.6f, 32.2f), new Vector3(1.15f, 4.1f, 0.16f), cyanMat, Vector3.zero);
        CreateCinematicWorldBox(root, "Cinematic Distant Gate Core", new Vector3(0f, 2.45f, 32.08f), new Vector3(0.62f, 3.15f, 0.10f), panelMat, Vector3.zero);
        CreateCinematicWorldBox(root, "Cinematic Distant Gate Cap", new Vector3(0f, 4.58f, 32.0f), new Vector3(1.45f, 0.10f, 0.18f), trimMat, Vector3.zero);

        for (int i = 0; i < 4; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float z = 8f + i * 7.0f;
            CreateCinematicWorldBox(root, "Cinematic Tiny Signal", new Vector3(side * 6.6f, 1.15f, z), new Vector3(0.050f, 0.38f, 0.050f), cyanMat, Vector3.zero);
        }
    }

    private static void AddCinematicBackgroundDepth(Transform root, Material buildingMat, Material trimMat)
    {
        for (int i = 0; i < 8; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float z = 31f + i * 5.2f;
            float height = 8.2f + (i % 4) * 1.45f;
            GameObject block = CreateCinematicWorldBox(root, "Cinematic Background Block", new Vector3(side * (12.0f + i * 0.9f), height * 0.5f, z), new Vector3(4.0f + i * 0.20f, height, 4.6f), buildingMat, Vector3.zero);
            CreateCinematicWorldBox(block.transform, "Cinematic Roof Antenna", new Vector3(0.32f, 0.56f, 0f), new Vector3(0.035f, 0.26f, 0.035f), trimMat, Vector3.zero, true);
        }
    }

    private static void AddCinematicOverheadCables(Transform root, Material trimMat)
    {
        for (int i = 0; i < 7; i++)
        {
            float z = -18f + i * 6.3f;
            float y = 4.6f + (i % 3) * 0.34f;
            CreateCinematicWorldBox(root, "Cinematic Overhead Cable", new Vector3(0f, y, z), new Vector3(22.0f, 0.032f, 0.032f), trimMat, new Vector3(0f, ReferenceRange(i, 91.4f, -4f, 4f), ReferenceRange(i, 92.4f, -3f, 3f)));
        }
    }

    private static void AddCinematicWetHighlights(Transform root, Material puddleMat, Material cyanMat)
    {
        for (int i = 0; i < 10; i++)
        {
            float z = -22f + i * 5.2f + ReferenceRange(i, 94.5f, -0.7f, 0.7f);
            float x = ReferenceRange(i, 95.5f, -3.6f, 3.6f);
            Vector3 size = new Vector3(ReferenceRange(i, 96.5f, 0.55f, 1.8f), 0.012f, ReferenceRange(i, 97.5f, 0.18f, 0.62f));
            CreateCinematicWorldBox(root, "Cinematic Wet Puddle", new Vector3(x, 0.083f, z), size, puddleMat, new Vector3(0f, ReferenceRange(i, 98.5f, -14f, 14f), 0f));

            if (i % 3 == 0)
            {
                CreateCinematicWorldBox(root, "Cinematic Neon Reflection", new Vector3(x + 0.12f, 0.091f, z), new Vector3(size.x * 0.45f, 0.010f, 0.035f), cyanMat, new Vector3(0f, ReferenceRange(i, 99.5f, -10f, 10f), 0f));
            }
        }
    }

    private static void AddCinematicForegroundFraming(Transform root, Material buildingMat, Material trimMat, Material cyanMat)
    {
        CreateCinematicWorldBox(root, "Cinematic Foreground Pipe_L", new Vector3(-8.25f, 2.1f, -8.2f), new Vector3(0.075f, 3.1f, 0.075f), trimMat, Vector3.zero);
        CreateCinematicWorldBox(root, "Cinematic Foreground Pipe_R", new Vector3(8.25f, 2.4f, -7.1f), new Vector3(0.075f, 3.4f, 0.075f), trimMat, Vector3.zero);
        CreateCinematicWorldBox(root, "Cinematic Foreground Neon", new Vector3(-8.18f, 1.9f, -7.0f), new Vector3(0.052f, 0.92f, 0.052f), cyanMat, Vector3.zero);
    }

    private static void AddCinematicPlayerSilhouette(Scene scene, Material cyanMat, Material trimMat)
    {
        GameObject player = FindRoot(scene, "Player_Recycler");
        if (player == null)
            return;

        Transform visual = FindDeepChild(player.transform, "RecyclerVisual");
        if (visual != null)
        {
            SetScenePart(visual, "Cinematic Backpack Light", PrimitiveType.Cube, new Vector3(-0.08f, 0.81f, -0.505f), Vector3.zero, new Vector3(0.070f, 0.24f, 0.040f), cyanMat);
            SetScenePart(visual, "Cinematic Coat Shoulder Line_L", PrimitiveType.Cube, new Vector3(-0.31f, 1.50f, -0.17f), new Vector3(0f, 0f, -12f), new Vector3(0.18f, 0.030f, 0.035f), trimMat);
            SetScenePart(visual, "Cinematic Coat Shoulder Line_R", PrimitiveType.Cube, new Vector3(0.31f, 1.50f, -0.17f), new Vector3(0f, 0f, 12f), new Vector3(0.18f, 0.030f, 0.035f), trimMat);
        }

        GameObject rimLight = new GameObject("Cinematic Player Rim Light");
        rimLight.transform.SetParent(player.transform, false);
        rimLight.transform.localPosition = new Vector3(0f, 1.35f, -1.15f);
        Light light = rimLight.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 3.2f;
        light.intensity = 0.85f;
        light.color = new Color(0.12f, 0.46f, 0.72f);
        light.shadows = LightShadows.None;
    }

    private static void AddCinematicWindowDepth(Transform cityRoot, Material panelMat, Material trimMat)
    {
        foreach (Transform building in cityRoot)
        {
            if (building == null || !building.name.StartsWith("Silent Building"))
                continue;

            Vector3 scale = building.localScale;
            foreach (Transform child in building)
            {
                if (child == null || !child.name.StartsWith("Window_Front"))
                    continue;

                Vector3 local = child.localPosition;
                Vector3 worldOffset = new Vector3(local.x * scale.x, local.y * scale.y, local.z * scale.z + 0.018f);
                Vector3 windowWorld = new Vector3(Mathf.Abs(child.localScale.x * scale.x), Mathf.Abs(child.localScale.y * scale.y), Mathf.Abs(child.localScale.z * scale.z));
                float width = Mathf.Max(0.42f, windowWorld.x);
                float height = Mathf.Max(0.34f, windowWorld.y);

                CreateReferenceFacadeBox(building, "Cinematic Window Recess", worldOffset + new Vector3(0f, 0f, 0.012f), new Vector3(width + 0.16f, height + 0.16f, 0.040f), panelMat, 0f);
                CreateReferenceFacadeBox(building, "Cinematic Window Top Shadow", worldOffset + new Vector3(0f, height * 0.5f + 0.10f, 0.038f), new Vector3(width + 0.22f, 0.040f, 0.050f), trimMat, 0f);
                CreateReferenceFacadeBox(building, "Cinematic Window Side Shadow", worldOffset + new Vector3(-width * 0.5f - 0.10f, 0f, 0.038f), new Vector3(0.040f, height + 0.18f, 0.050f), trimMat, 0f);
                CreateReferenceFacadeBox(building, "Cinematic Window Side Shadow", worldOffset + new Vector3(width * 0.5f + 0.10f, 0f, 0.038f), new Vector3(0.040f, height + 0.18f, 0.050f), trimMat, 0f);
            }
        }
    }

    private static void AddCinematicRoofBreakup(Transform cityRoot, Material buildingMat, Material trimMat)
    {
        int index = 0;
        foreach (Transform building in cityRoot)
        {
            if (building == null || !building.name.StartsWith("Silent Building"))
                continue;

            Vector3 scale = building.localScale;
            for (int i = 0; i < 4; i++)
            {
                float x = ReferenceRange(index, i + 101.2f, -scale.x * 0.42f, scale.x * 0.42f);
                float z = ReferenceRange(index, i + 102.2f, -scale.z * 0.42f, scale.z * 0.42f);
                float width = ReferenceRange(index, i + 103.2f, 0.30f, 0.86f);
                float height = ReferenceRange(index, i + 104.2f, 0.16f, 0.62f);
                CreateReferenceFacadeBox(building, "Cinematic Broken Roof Lip", new Vector3(x, scale.y * 0.5f + height * 0.5f, z), new Vector3(width, height, 0.18f), buildingMat, ReferenceRange(index, i + 105.2f, -6f, 6f));
            }

            for (int i = 0; i < 3; i++)
            {
                float x = ReferenceRange(index, i + 106.2f, -scale.x * 0.32f, scale.x * 0.32f);
                float z = ReferenceRange(index, i + 107.2f, -scale.z * 0.32f, scale.z * 0.32f);
                CreateReferenceFacadeBox(building, "Cinematic Rooftop Pipe Cluster", new Vector3(x, scale.y * 0.5f + 0.42f, z), new Vector3(0.055f, 0.84f, 0.055f), trimMat, 0f);
            }

            index++;
        }
    }

    private static void AddCinematicSideAlleyDepth(Transform root, Material buildingMat, Material trimMat, Material cyanMat)
    {
        for (int i = 0; i < 6; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float z = -8f + i * 5.8f;
            CreateCinematicWorldBox(root, "Cinematic Side Alley Wall", new Vector3(side * 9.6f, 2.1f, z), new Vector3(0.32f, 4.2f, 3.6f), buildingMat, new Vector3(0f, side * 12f, 0f));
            CreateCinematicWorldBox(root, "Cinematic Alley Pipe", new Vector3(side * 9.25f, 2.0f, z + 0.42f), new Vector3(0.070f, 2.9f, 0.070f), trimMat, Vector3.zero);

            if (i % 3 == 0)
                CreateCinematicWorldBox(root, "Cinematic Alley Blue Pin", new Vector3(side * 9.12f, 1.45f, z - 0.75f), new Vector3(0.060f, 0.58f, 0.060f), cyanMat, Vector3.zero);
        }
    }

    private static Material CreateCinematicPuddleMaterial()
    {
        Material material = CreateMaterial("MR3D_CinematicPuddle", new Color(0.012f, 0.020f, 0.030f), false);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.78f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0.0f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateCinematicTransparentMaterial(string name, Color color, float smoothness)
    {
        Material material = CreateMaterial(name, color, false);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateCinematicWorldBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Vector3 euler, bool local = false)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        if (local)
        {
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.transform.localEulerAngles = euler;
        }
        else
        {
            box.transform.position = position;
            box.transform.localScale = scale;
            box.transform.eulerAngles = euler;
        }

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider = box.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        return box;
    }

    private static float ReferenceRange(int seed, float salt, float min, float max)
    {
        float n = Mathf.Repeat(Mathf.Sin(seed * 12.9898f + salt * 78.233f) * 43758.5453f, 1f);
        return Mathf.Lerp(min, max, n);
    }

    private static void CreateManagers()
    {
        GameObject managers = new GameObject("Managers");
        managers.AddComponent<GameState3D>();
        managers.AddComponent<MemoryManager3D>();
        managers.AddComponent<MemoryAudio3D>();
        managers.AddComponent<UIManager3D>();
    }

    private static Light CreateLighting()
    {
        GameObject sunObject = new GameObject("Sun Light");
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.8f;
        sun.color = new Color(0.78f, 0.84f, 1f);
        sunObject.transform.rotation = Quaternion.Euler(35f, -28f, 0f);

        GameObject fill = new GameObject("Memory Blue Fill Light");
        Light fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.range = 60f;
        fillLight.intensity = 1.25f;
        fillLight.color = new Color(0.18f, 0.42f, 0.72f);
        fill.transform.position = new Vector3(0f, 8f, 2f);

        return sun;
    }

    private static void CreateWorldTone(Light sun)
    {
        GameObject worldTone = new GameObject("WorldToneController");
        WorldToneController3D controller = worldTone.AddComponent<WorldToneController3D>();
        controller.sunLight = sun;
    }

    private static void CreateGround(Material groundMat, Material roadMat, Material debrisMat)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Abandoned City Ground";
        ground.transform.localScale = new Vector3(14f, 1f, 14f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        GameObject avenue = GameObject.CreatePrimitive(PrimitiveType.Cube);
        avenue.name = "Main Avenue";
        avenue.transform.position = new Vector3(0f, 0.02f, 0f);
        avenue.transform.localScale = new Vector3(12.5f, 0.04f, 58f);
        avenue.GetComponent<Renderer>().sharedMaterial = roadMat;

        for (int i = 0; i < 10; i++)
        {
            GameObject marking = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marking.name = "Road Marking_" + i;
            marking.transform.position = new Vector3(0f, 0.05f, -18f + i * 4f);
            marking.transform.localScale = new Vector3(0.22f, 0.01f, 1.8f);
            marking.GetComponent<Renderer>().sharedMaterial = CreateMaterial("MR3D_Marking", new Color(0.6f, 0.65f, 0.7f), false);
        }

        for (int i = 0; i < 16; i++)
        {
            GameObject rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rubble.name = "Rubble_" + i;
            float side = (i % 2 == 0) ? -7.4f : 7.4f;
            rubble.transform.position = new Vector3(side + Random.Range(-1.8f, 1.8f), 0.14f + Random.Range(0f, 0.12f), -24f + i * 3.0f);
            rubble.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), Random.Range(0f, 12f));
            rubble.transform.localScale = new Vector3(Random.Range(0.3f, 0.9f), Random.Range(0.18f, 0.4f), Random.Range(0.35f, 1.0f));
            rubble.GetComponent<Renderer>().sharedMaterial = debrisMat;
        }
    }

    private static void CreateCityBlocks(Material buildingMat, Material trimMat, Material windowMat, Material debrisMat, Material accentMat)
    {
        GameObject cityRoot = new GameObject("Silent City");

        Vector3[] positions =
        {
            new Vector3(-14.5f, 2.8f, -22f), new Vector3(14.8f, 3.8f, -21f),
            new Vector3(-15.5f, 4.6f, -10f), new Vector3(15.2f, 4.2f, -10f),
            new Vector3(-14.8f, 3.1f, 2f), new Vector3(14.6f, 5.4f, 3f),
            new Vector3(-15.0f, 4.8f, 15f), new Vector3(15.4f, 6.2f, 16f),
            new Vector3(-20.2f, 3.4f, 8f), new Vector3(20.2f, 4.1f, 8f)
        };

        Vector3[] scales =
        {
            new Vector3(6f, 5.6f, 6f), new Vector3(6.5f, 7.6f, 5.4f),
            new Vector3(5.6f, 9.2f, 6.5f), new Vector3(6.4f, 8.4f, 6.4f),
            new Vector3(6.2f, 6.2f, 5.2f), new Vector3(7.0f, 10.8f, 7.0f),
            new Vector3(6.0f, 9.6f, 5.6f), new Vector3(7.4f, 12.4f, 7.2f),
            new Vector3(4.0f, 6.8f, 4.0f), new Vector3(4.2f, 8.2f, 4.2f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            CreateBuilding(cityRoot.transform, i, positions[i], scales[i], buildingMat, trimMat, windowMat, debrisMat, accentMat);
        }

        for (int i = 0; i < 8; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float z = -16.5f + (i / 2) * 8.0f;
            CreateStreetLight(cityRoot.transform, i, new Vector3(side * 6.8f, 0f, z), trimMat, accentMat);
        }
    }

    private static void CreateBuilding(Transform parent, int index, Vector3 position, Vector3 scale, Material buildingMat, Material trimMat, Material windowMat, Material debrisMat, Material accentMat)
    {
        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = "Silent Building_" + index;
        building.transform.SetParent(parent);
        building.transform.position = position;
        building.transform.localScale = scale;
        building.GetComponent<Renderer>().sharedMaterial = buildingMat;

        CreateFacadeWindows(building.transform, scale, windowMat, true);
        CreateFacadeWindows(building.transform, scale, windowMat, false);
        CreateBuildingDetails(building.transform, scale, trimMat, debrisMat, accentMat, index);
    }

    private static void CreateFacadeWindows(Transform building, Vector3 scale, Material windowMat, bool front)
    {
        int floors = Mathf.Max(2, Mathf.RoundToInt(scale.y / 1.8f));
        int cols = Mathf.Max(2, Mathf.RoundToInt(scale.x / 1.7f));
        float z = (front ? 1f : -1f) * (scale.z * 0.5f + 0.03f);
        Vector3 desiredWindowScale = new Vector3(0.75f, 0.55f, 0.06f);

        for (int y = 0; y < floors; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                if ((x + y) % 3 == 0) continue;
                GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
                window.name = front ? "Window_Front" : "Window_Back";
                window.transform.SetParent(building);
                float wx = -scale.x * 0.33f + x * (scale.x * 0.66f / Mathf.Max(1, cols - 1));
                float wy = -scale.y * 0.34f + y * (scale.y * 0.68f / Mathf.Max(1, floors - 1));
                SetChildWorldBox(window.transform, scale, new Vector3(wx, wy, z), desiredWindowScale);
                window.GetComponent<Renderer>().sharedMaterial = windowMat;
            }
        }
    }

    private static void CreateBuildingDetails(Transform building, Vector3 scale, Material trimMat, Material debrisMat, Material accentMat, int index)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "Edge Trim";
            trim.transform.SetParent(building);
            SetChildWorldBox(trim.transform, scale, new Vector3(s * (scale.x * 0.5f + 0.06f), 0f, 0f), new Vector3(0.12f, scale.y * 1.02f, scale.z * 1.02f));
            trim.GetComponent<Renderer>().sharedMaterial = trimMat;
        }

        GameObject roofUnit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofUnit.name = "Rooftop Unit";
        roofUnit.transform.SetParent(building);
        SetChildWorldBox(roofUnit.transform, scale, new Vector3(0f, scale.y * 0.5f + 0.3f, 0f), new Vector3(scale.x * 0.28f, 0.25f, scale.z * 0.28f));
        roofUnit.GetComponent<Renderer>().sharedMaterial = trimMat;

        if (index % 2 == 0)
        {
            GameObject tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = "Water Tank";
            tank.transform.SetParent(building);
            tank.transform.localPosition = new Vector3((scale.x * 0.18f) / scale.x, (scale.y * 0.5f + 0.85f) / scale.y, 0f);
            tank.transform.localScale = new Vector3(0.28f / scale.x, 0.45f / scale.y, 0.28f / scale.z);
            tank.GetComponent<Renderer>().sharedMaterial = trimMat;
        }
        else
        {
            GameObject brokenSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brokenSign.name = "Broken Neon Sign";
            brokenSign.transform.SetParent(building);
            SetChildWorldBox(brokenSign.transform, scale, new Vector3(0f, scale.y * 0.15f, scale.z * 0.5f + 0.08f), new Vector3(scale.x * 0.35f, 0.18f, 0.06f));
            brokenSign.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            brokenSign.GetComponent<Renderer>().sharedMaterial = accentMat;
        }

        for (int i = 0; i < 3; i++)
        {
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.name = "Facade Debris";
            debris.transform.SetParent(building);
            Vector3 worldOffset = new Vector3(Random.Range(-scale.x * 0.35f, scale.x * 0.35f), -scale.y * 0.5f + 0.12f, scale.z * 0.5f + Random.Range(-0.4f, 0.4f));
            Vector3 worldSize = new Vector3(Random.Range(0.2f, 0.6f), Random.Range(0.1f, 0.22f), Random.Range(0.2f, 0.5f));
            SetChildWorldBox(debris.transform, scale, worldOffset, worldSize);
            debris.transform.localRotation = Quaternion.Euler(Random.Range(0f, 8f), Random.Range(0f, 180f), Random.Range(0f, 18f));
            debris.GetComponent<Renderer>().sharedMaterial = debrisMat;
        }
    }

    private static void SetChildWorldBox(Transform child, Vector3 parentScale, Vector3 worldOffset, Vector3 worldScale)
    {
        child.localPosition = new Vector3(worldOffset.x / parentScale.x, worldOffset.y / parentScale.y, worldOffset.z / parentScale.z);
        child.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, worldScale.z / parentScale.z);
    }

    private static void CreateStreetLight(Transform parent, int index, Vector3 position, Material poleMat, Material lampMat)
    {
        GameObject root = new GameObject("Broken Streetlight_" + index);
        root.transform.SetParent(parent);
        root.transform.position = position;

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Streetlight Pole";
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        pole.transform.localScale = new Vector3(0.08f, 1.6f, 0.08f);
        pole.GetComponent<Renderer>().sharedMaterial = poleMat;

        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Streetlight Arm";
        arm.transform.SetParent(root.transform);
        arm.transform.localPosition = new Vector3(0.28f, 3.05f, 0f);
        arm.transform.localScale = new Vector3(0.55f, 0.06f, 0.06f);
        arm.GetComponent<Renderer>().sharedMaterial = poleMat;

        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lamp.name = "Streetlight Lamp";
        lamp.transform.SetParent(root.transform);
        lamp.transform.localPosition = new Vector3(0.55f, 2.9f, 0f);
        lamp.transform.localScale = Vector3.one * 0.16f;
        lamp.GetComponent<Renderer>().sharedMaterial = lampMat;

        Light light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 7f;
        light.intensity = 0.9f;
        light.color = new Color(0.22f, 0.72f, 1f);

        float side = position.x > 0f ? 1f : -1f;
        root.transform.localEulerAngles = new Vector3(0f, side > 0f ? 180f : 0f, side * ReferenceRange(index, 161.2f, -3.0f, 2.0f));
    }

    private static GameObject CreatePlayer(Material suitMat, Material coatMat, Material skinMat, Material accentMat)
    {
        GameObject player = new GameObject("Player_Recycler");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0f, -18f);

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2.05f;
        controller.radius = 0.31f;
        controller.center = new Vector3(0f, 1.02f, 0f);
        ThirdPersonPlayer3D movement = player.AddComponent<ThirdPersonPlayer3D>();

        GameObject visual = new GameObject("RecyclerVisual");
        visual.transform.SetParent(player.transform);
        visual.transform.localPosition = Vector3.zero;

        // torso
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(visual.transform);
        torso.transform.localPosition = new Vector3(0f, 1.26f, 0f);
        torso.transform.localScale = new Vector3(0.46f, 0.52f, 0.31f);
        torso.GetComponent<Renderer>().sharedMaterial = suitMat;
        Object.DestroyImmediate(torso.GetComponent<Collider>());

        // coat shell
        GameObject coat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        coat.name = "Coat";
        coat.transform.SetParent(visual.transform);
        coat.transform.localPosition = new Vector3(0f, 1.24f, -0.01f);
        coat.transform.localScale = new Vector3(0.60f, 0.80f, 0.38f);
        coat.GetComponent<Renderer>().sharedMaterial = coatMat;
        Object.DestroyImmediate(coat.GetComponent<Collider>());

        GameObject coatSkirt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        coatSkirt.name = "Coat Skirt";
        coatSkirt.transform.SetParent(visual.transform);
        coatSkirt.transform.localPosition = new Vector3(0f, 0.80f, 0f);
        coatSkirt.transform.localScale = new Vector3(0.50f, 0.18f, 0.32f);
        coatSkirt.GetComponent<Renderer>().sharedMaterial = suitMat;
        Object.DestroyImmediate(coatSkirt.GetComponent<Collider>());

        // head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(visual.transform);
        head.transform.localPosition = new Vector3(0f, 1.87f, 0.02f);
        head.transform.localScale = Vector3.one * 0.285f;
        head.GetComponent<Renderer>().sharedMaterial = skinMat;
        Object.DestroyImmediate(head.GetComponent<Collider>());

        BuildHumanoidSceneLimbRig(visual.transform, coatMat, skinMat, suitMat, accentMat);
        CreateLimb(visual.transform, "Neck", new Vector3(0f, 1.66f, 0.015f), Vector3.zero, new Vector3(0.085f, 0.11f, 0.085f), skinMat);

        return player;
    }

    private static void CreateLimb(Transform parent, string name, Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Material mat, PrimitiveType primitive = PrimitiveType.Cylinder)
    {
        GameObject limb = GameObject.CreatePrimitive(primitive);
        limb.name = name;
        limb.transform.SetParent(parent);
        limb.transform.localPosition = localPosition;
        limb.transform.localEulerAngles = localEuler;
        limb.transform.localScale = localScale;
        limb.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(limb.GetComponent<Collider>());
    }

    private static void CreateCamera(Transform player)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.fieldOfView = 65f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 250f;
        AudioListener listener = cameraObject.AddComponent<AudioListener>();
        listener.enabled = true;

        OrbitCamera3D orbit = cameraObject.AddComponent<OrbitCamera3D>();
        orbit.target = player;
        orbit.minPitch = -35f;
        orbit.maxPitch = 70f;
        orbit.baseLookHeight = 1.45f;
        orbit.lookUpHeight = 5.2f;
        orbit.lookDownHeight = 1.05f;
        cameraObject.transform.position = player.position + new Vector3(0f, 3.4f, -6.2f);

        ThirdPersonPlayer3D movement = player.GetComponent<ThirdPersonPlayer3D>();
        movement.cameraTransform = cameraObject.transform;
    }

    private static void CreateMemories(MemoryData3D[] memories, Material material)
    {
        Vector3[] positions =
        {
            new Vector3(-9f, 1.3f, -9f),
            new Vector3(8f, 1.3f, -8f),
            new Vector3(-12f, 1.3f, 4f),
            new Vector3(4f, 1.3f, 6f),
            new Vector3(13f, 1.3f, 2f),
            new Vector3(-2f, 1.3f, 14f),
            new Vector3(10f, 1.3f, 14f),
            new Vector3(-16f, 1.3f, -16f)
        };

        for (int i = 0; i < memories.Length && i < positions.Length; i++)
        {
            GameObject memory = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            memory.name = "Memory Orb_" + memories[i].id;
            memory.transform.position = positions[i];
            memory.transform.localScale = Vector3.one * 0.7f;
            memory.GetComponent<Renderer>().sharedMaterial = material;

            SphereCollider collider = memory.GetComponent<SphereCollider>();
            collider.radius = 1.4f;
            collider.isTrigger = true;

            MemoryObject3D objectScript = memory.AddComponent<MemoryObject3D>();
            objectScript.memoryData = memories[i];

            GameObject lightObject = new GameObject("Memory Glow Light");
            lightObject.transform.SetParent(memory.transform);
            lightObject.transform.localPosition = Vector3.zero;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 7f;
            light.intensity = 1.8f;
            light.color = new Color(0.3f, 0.85f, 1f);
        }
    }

    private static void CreateArchiveTerminal(Material terminalMat, Material trimMat)
    {
        GameObject terminal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        terminal.name = "Central Archive Terminal";
        terminal.transform.position = new Vector3(0f, 3.9f, 31.5f);
        terminal.transform.localScale = new Vector3(3.4f, 7.8f, 1.6f);
        terminal.GetComponent<Renderer>().sharedMaterial = terminalMat;
        ArchiveTerminal3D archive = terminal.AddComponent<ArchiveTerminal3D>();
        archive.requiredDecisions = 5;

        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "Terminal Frame";
        frame.transform.SetParent(terminal.transform);
        frame.transform.localPosition = new Vector3(0f, 0f, -0.50f);
        frame.transform.localScale = new Vector3(1.08f, 0.72f, 0.12f);
        frame.GetComponent<Renderer>().sharedMaterial = trimMat;

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crown.name = "Archive Tower Crown";
        crown.transform.SetParent(terminal.transform);
        crown.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        crown.transform.localScale = new Vector3(1.18f, 0.16f, 1.10f);
        crown.GetComponent<Renderer>().sharedMaterial = trimMat;

        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "Archive Vertical Light";
        beam.transform.SetParent(terminal.transform);
        beam.transform.localPosition = new Vector3(0f, 0.03f, -0.53f);
        beam.transform.localScale = new Vector3(0.055f, 0.72f, 0.045f);
        beam.GetComponent<Renderer>().sharedMaterial = terminalMat;

        GameObject lightObject = new GameObject("Terminal Beacon");
        lightObject.transform.position = terminal.transform.position + Vector3.up * 5f;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 24f;
        light.intensity = 4f;
        light.color = new Color(0.1f, 1f, 0.75f);
    }

    private static void CreateInstructionsSign()
    {
        GameObject sign = new GameObject("Prototype Instructions");
        sign.transform.position = new Vector3(0f, 2.6f, -12.5f);
        sign.transform.rotation = Quaternion.identity;
        TextMesh text = sign.AddComponent<TextMesh>();
        text.text = "Memory Recycler 3D\nWASD 이동 / Shift 달리기 / E 기억 회수 / Tab 아카이브\n푸른 구체를 찾아 기억을 복원하세요.";
        text.fontSize = 54;
        text.characterSize = 0.065f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;
    }

    private static MemoryData3D[] CreateMemoryAssets()
    {
        return new[]
        {
            CreateMemory("MR3D_001", "식탁 아래의 웃음", "아파트 폐허의 식탁 아래에서 발견된 가족의 기억입니다. 웃음소리는 선명하지만 얼굴은 흐려져 있습니다.", EmotionType3D.Love, 22,
                new[] { "아버지는 접시를 놓쳤다", "아이는 처음으로 웃었다", "어머니는 그 순간을 지우지 말자고 했다" },
                new[] { "아버지는 접시를 놓쳤다", "아이는 처음으로 웃었다", "어머니는 그 순간을 지우지 말자고 했다" },
                "사소한 실수가 가족의 가장 따뜻한 장면으로 남아 있었습니다."),

            CreateMemory("MR3D_002", "졸업 앨범의 빈칸", "폐교 복도에 흩어진 졸업 앨범입니다. 한 학생의 이름만 반복해서 지워져 있습니다.", EmotionType3D.Regret, 48,
                new[] { "우리는 모른 척했다", "그 아이의 책상은 매일 비어 있었다", "앨범에서 이름을 지우면 죄책감도 사라질 줄 알았다" },
                new[] { "그 아이의 책상은 매일 비어 있었다", "우리는 모른 척했다", "앨범에서 이름을 지우면 죄책감도 사라질 줄 알았다" },
                "집단의 침묵은 한 사람의 부재보다 오래 남았습니다."),

            CreateMemory("MR3D_003", "첫 번째 치료 동의서", "기억 삭제 기술이 처음 도입된 병원 기록입니다. 문서에는 치료라는 말과 회피라는 말이 함께 남아 있습니다.", EmotionType3D.Hope, 31,
                new[] { "처음 목적은 상처를 덜어주는 것이었다", "사람들은 고통을 줄이는 법을 배웠다", "곧 누구도 고통의 이유를 묻지 않았다" },
                new[] { "처음 목적은 상처를 덜어주는 것이었다", "사람들은 고통을 줄이는 법을 배웠다", "곧 누구도 고통의 이유를 묻지 않았다" },
                "치료는 필요했지만, 도시는 어느 순간 질문까지 삭제하기 시작했습니다."),

            CreateMemory("MR3D_004", "행정 구역의 야간 명령", "중앙 행정 서버에서 떨어져 나온 명령 로그입니다. 특정 사건 이후 도시 전체의 기억이 일괄 정리되었습니다.", EmotionType3D.Guilt, 72,
                new[] { "사건명은 기록하지 않는다", "관련자의 슬픔은 공공 안정을 해친다", "모든 시민에게 선택권이 있었다고 발표한다" },
                new[] { "사건명은 기록하지 않는다", "관련자의 슬픔은 공공 안정을 해친다", "모든 시민에게 선택권이 있었다고 발표한다" },
                "도시는 시민을 보호한다는 명목으로 시민의 증언을 지웠습니다."),

            CreateMemory("MR3D_005", "미아의 파란 우산", "비가 오지 않는 지하철역에서 젖은 우산이 발견되었습니다. 우산 안쪽에는 '나를 기억해줘'라고 적혀 있습니다.", EmotionType3D.Loss, 63,
                new[] { "미아는 마지막 열차를 기다렸다", "방송은 대피가 끝났다고 말했다", "그러나 플랫폼에는 아직 한 사람이 남아 있었다" },
                new[] { "방송은 대피가 끝났다고 말했다", "그러나 플랫폼에는 아직 한 사람이 남아 있었다", "미아는 마지막 열차를 기다렸다" },
                "미아는 도시가 지운 사건의 마지막 목격자였을 가능성이 있습니다."),

            CreateMemory("MR3D_006", "수거원의 빈 파일", "주인공의 이름으로 잠긴 아카이브 파일입니다. 파일 내부에는 삭제 요청자가 본인이라는 기록이 있습니다.", EmotionType3D.Fear, 80,
                new[] { "나는 목격했다", "나는 증언하지 않았다", "나는 잊는 편을 선택했다" },
                new[] { "나는 목격했다", "나는 증언하지 않았다", "나는 잊는 편을 선택했다" },
                "주인공도 도시의 침묵을 만든 사람 중 하나였습니다."),

            CreateMemory("MR3D_007", "녹슨 방송 마이크", "도시 재난 방송국의 마이크입니다. 마지막 방송은 송출되지 못한 채 내부 저장소에만 남았습니다.", EmotionType3D.Anger, 55,
                new[] { "아직 사람들이 남아 있습니다", "철수 명령은 거짓입니다", "이 방송을 끊지 마십시오" },
                new[] { "아직 사람들이 남아 있습니다", "철수 명령은 거짓입니다", "이 방송을 끊지 마십시오" },
                "진실은 방송되기 직전에 차단되었습니다."),

            CreateMemory("MR3D_008", "중앙 아카이브의 자장가", "아카이브 깊은 곳에서 반복 재생되는 짧은 노래입니다. 누군가 도시 전체를 잠재우려 했던 흔적입니다.", EmotionType3D.Peace, 35,
                new[] { "잠들면 아프지 않다", "잊으면 미워하지 않는다", "깨어나면 다시 선택해야 한다" },
                new[] { "잠들면 아프지 않다", "잊으면 미워하지 않는다", "깨어나면 다시 선택해야 한다" },
                "평온은 때로 진실을 덮는 가장 부드러운 폭력이 됩니다.")
        };
    }

    private static MemoryData3D CreateMemory(string id, string title, string description, EmotionType3D emotion, int corruption, string[] pieces, string[] order, string restoredText)
    {
        string assetPath = DataPath + "/" + id + ".asset";
        MemoryData3D data = AssetDatabase.LoadAssetAtPath<MemoryData3D>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MemoryData3D>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        data.id = id;
        data.memoryTitle = title;
        data.description = description;
        data.emotion = emotion;
        data.corruptionLevel = corruption;
        data.sentencePieces = pieces;
        data.correctOrder = order;
        data.restoredText = restoredText;
        ApplyMemoryWorldContext(data);
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void ApplyMemoryWorldContext(MemoryData3D data)
    {
        switch (data.id)
        {
            case "MR3D_001":
                data.locationName = "무너진 주거 구역";
                data.archiveClue = "도시는 가족 단위 기록부터 지우기 시작했습니다. 사적인 웃음은 공적 질서보다 오래 남았습니다.";
                break;
            case "MR3D_002":
                data.locationName = "폐교 복도";
                data.archiveClue = "개인의 이름을 지우면 사건도 사라진다고 믿었던 시기의 기록입니다.";
                break;
            case "MR3D_003":
                data.locationName = "기억 치료 병동";
                data.archiveClue = "치료라는 이름의 삭제가 도시 전체의 표준 절차가 된 첫 흔적입니다.";
                break;
            case "MR3D_004":
                data.locationName = "행정 서버 구역";
                data.archiveClue = "중앙 명령은 시민을 보호한다는 문장으로 시민의 증언을 잠갔습니다.";
                break;
            case "MR3D_005":
                data.locationName = "지하철 대피 통로";
                data.archiveClue = "실종자 기록은 삭제되지 않았습니다. 다만 검색되지 않게 분류되었습니다.";
                break;
            case "MR3D_006":
                data.locationName = "회수원 개인 파일";
                data.archiveClue = "주인공은 피해자이자 기록 관리자였습니다. 마지막 선택은 본인의 과거까지 향합니다.";
                break;
            case "MR3D_007":
                data.locationName = "재난 방송국";
                data.archiveClue = "마지막 방송이 차단된 순간부터 도시는 진실 대신 안정된 침묵을 택했습니다.";
                break;
            case "MR3D_008":
                data.locationName = "중앙 아카이브 심층부";
                data.archiveClue = "자장가는 도시를 달래기 위한 노래가 아니라, 판단을 멈추게 하는 명령어였습니다.";
                break;
        }
    }

    private enum CinematicTextureKind
    {
        Concrete,
        Road,
        Window,
        SignPanel,
        Grime,
        Debris
    }

    private static void EnsureCinematicTextureAssets()
    {
        EnsureFolder(TexturePath);
        EnsureFolder(GeneratedTexturePath);

        CreateProceduralTexture("MR3D_CinematicConcrete", CinematicTextureKind.Concrete, 512);
        CreateProceduralTexture("MR3D_CinematicRoad", CinematicTextureKind.Road, 512);
        CreateProceduralTexture("MR3D_CinematicWindowDust", CinematicTextureKind.Window, 512);
        CreateProceduralTexture("MR3D_CinematicSignPanel", CinematicTextureKind.SignPanel, 512);
        CreateProceduralTexture("MR3D_CinematicGrime", CinematicTextureKind.Grime, 512);
        CreateProceduralTexture("MR3D_CinematicBrokenConcrete", CinematicTextureKind.Debris, 512);
        AssetDatabase.Refresh();
    }

    private static void ApplyCinematicMaterialTextures()
    {
        AssignMaterialTexture(CreateMaterial("MR3D_Building", new Color(0.062f, 0.071f, 0.076f), false), "MR3D_CinematicConcrete", new Vector2(2.25f, 2.25f), 0.018f);
        AssignMaterialTexture(CreateMaterial("MR3D_BuildingTrim", new Color(0.030f, 0.034f, 0.036f), false), "MR3D_CinematicGrime", new Vector2(1.4f, 1.4f), 0.018f);
        AssignMaterialTexture(CreateMaterial("MR3D_Debris", new Color(0.12f, 0.125f, 0.120f), false), "MR3D_CinematicBrokenConcrete", new Vector2(1.35f, 1.35f), 0.015f);
        AssignMaterialTexture(CreateMaterial("MR3D_Road", new Color(0.045f, 0.050f, 0.055f), false), "MR3D_CinematicRoad", new Vector2(2.0f, 7.0f), 0.018f);
        AssignMaterialTexture(CreateMaterial("MR3D_Window", new Color(0.018f, 0.030f, 0.038f), false), "MR3D_CinematicWindowDust", new Vector2(1.0f, 1.0f), 0.18f);
        AssignMaterialTexture(CreateMaterial("MR3D_DarkPanel", new Color(0.017f, 0.023f, 0.030f), false), "MR3D_CinematicSignPanel", new Vector2(1.0f, 1.0f), 0.055f);
        AssignMaterialTexture(CreateMaterial("MR3D_CinematicGrime", new Color(0.028f, 0.034f, 0.038f), false), "MR3D_CinematicGrime", new Vector2(1.0f, 1.0f), 0.01f);
        AssignMaterialTexture(CreateMaterial("MR3D_CinematicRoadPatch", new Color(0.030f, 0.035f, 0.040f), false), "MR3D_CinematicRoad", new Vector2(1.0f, 1.0f), 0.01f);
    }

    private static void AssignMaterialTexture(Material material, string textureName, Vector2 tiling, float smoothness)
    {
        if (material == null)
            return;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GeneratedTexturePath + "/" + textureName + ".png");
        if (texture == null)
            return;

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", tiling);
        }

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        EditorUtility.SetDirty(material);
    }

    private static void CreateProceduralTexture(string fileName, CinematicTextureKind kind, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                pixels[y * size + x] = EvaluateCinematicTexture(kind, u, v);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        string assetPath = GeneratedTexturePath + "/" + fileName + ".png";
        File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(assetPath);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }
    }

    private static Color EvaluateCinematicTexture(CinematicTextureKind kind, float u, float v)
    {
        switch (kind)
        {
            case CinematicTextureKind.Road:
                return EvaluateRoadTexture(u, v);
            case CinematicTextureKind.Window:
                return EvaluateWindowTexture(u, v);
            case CinematicTextureKind.SignPanel:
                return EvaluateSignPanelTexture(u, v);
            case CinematicTextureKind.Grime:
                return EvaluateGrimeTexture(u, v);
            case CinematicTextureKind.Debris:
                return EvaluateDebrisTexture(u, v);
            default:
                return EvaluateConcreteTexture(u, v);
        }
    }

    private static Color EvaluateConcreteTexture(float u, float v)
    {
        float grain = FractalNoise(u * 9.5f, v * 11.5f, 11f);
        float pores = FractalNoise(u * 38f, v * 44f, 17f) * 0.24f;
        float slabX = StepLine(Mathf.Repeat(u * 2.0f + SmoothNoise(v * 2.0f, 0f, 18f) * 0.035f, 1f), 0.010f);
        float slabY = StepLine(Mathf.Repeat(v * 3.0f + SmoothNoise(0f, u * 2.0f, 19f) * 0.025f, 1f), 0.009f);
        float seam = Mathf.Max(slabX, slabY);
        float crackA = 1f - Mathf.Clamp01(Mathf.Abs(u - (0.27f + Mathf.Sin(v * 16f) * 0.018f)) * 180f);
        float crackB = 1f - Mathf.Clamp01(Mathf.Abs(u - (0.70f + Mathf.Sin(v * 22f + 2.2f) * 0.014f)) * 220f);
        float spall = Mathf.Pow(FractalNoise(u * 5.5f, v * 7.0f, 21f), 4.2f);
        float rain = Mathf.Pow(SmoothNoise(u * 20f, v * 5f, 23f), 2.7f) * Mathf.Lerp(0.34f, 0.04f, v);
        float tone = 0.078f + grain * 0.105f + pores - seam * 0.075f - Mathf.Max(crackA, crackB) * 0.12f - rain * 0.13f + spall * 0.050f;
        float cold = Mathf.Clamp01(tone);
        return new Color(cold * 0.72f, cold * 0.84f, cold * 0.92f, 1f);
    }

    private static Color EvaluateRoadTexture(float u, float v)
    {
        float grain = FractalNoise(u * 12f, v * 18f, 31f);
        float tileX = StepLine(Mathf.Repeat(u * 4.0f, 1f), 0.010f);
        float tileY = StepLine(Mathf.Repeat(v * 7.0f, 1f), 0.010f);
        float oil = Mathf.Pow(SmoothNoise(u * 7f, v * 10f, 37f), 4.0f);
        float crack = 1f - Mathf.Clamp01(Mathf.Abs(v - (0.38f + Mathf.Sin(u * 18f) * 0.018f)) * 160f);
        float tone = 0.050f + grain * 0.07f - Mathf.Max(tileX, tileY) * 0.035f - oil * 0.045f - crack * 0.08f;
        return new Color(tone * 0.82f, tone * 0.90f, tone, 1f);
    }

    private static Color EvaluateWindowTexture(float u, float v)
    {
        float dust = FractalNoise(u * 14f, v * 14f, 41f);
        float vertical = Mathf.Pow(SmoothNoise(u * 26f, v * 3f, 43f), 2.3f) * (1f - v);
        float slash = 1f - Mathf.Clamp01(Mathf.Abs((u - v * 0.62f) - 0.20f) * 120f);
        float tone = 0.035f + dust * 0.055f + vertical * 0.10f - slash * 0.12f;
        return new Color(tone * 0.60f, tone * 0.82f, tone, 1f);
    }

    private static Color EvaluateSignPanelTexture(float u, float v)
    {
        float grain = FractalNoise(u * 20f, v * 20f, 53f);
        float edge = Mathf.Max(Mathf.Max(StepLine(u, 0.040f), StepLine(1f - u, 0.040f)), Mathf.Max(StepLine(v, 0.040f), StepLine(1f - v, 0.040f)));
        float scratch = Mathf.Pow(SmoothNoise(u * 28f, v * 6f, 59f), 3.1f);
        float tone = 0.035f + grain * 0.055f + edge * 0.075f - scratch * 0.035f;
        return new Color(tone * 0.65f, tone * 0.82f, tone, 1f);
    }

    private static Color EvaluateGrimeTexture(float u, float v)
    {
        float soot = Mathf.Pow(FractalNoise(u * 10f, v * 18f, 67f), 2.1f);
        float drip = Mathf.Pow(SmoothNoise(u * 18f, v * 2f, 71f), 2.4f) * Mathf.Lerp(0.85f, 0.15f, v);
        float tone = 0.018f + soot * 0.065f + drip * 0.080f;
        return new Color(tone * 0.75f, tone * 0.84f, tone, 1f);
    }

    private static Color EvaluateDebrisTexture(float u, float v)
    {
        float aggregate = FractalNoise(u * 42f, v * 42f, 83f);
        float chipped = Mathf.Pow(FractalNoise(u * 8f, v * 10f, 89f), 2.6f);
        float edgeCrackA = 1f - Mathf.Clamp01(Mathf.Abs((u + Mathf.Sin(v * 18f) * 0.025f) - 0.42f) * 90f);
        float edgeCrackB = 1f - Mathf.Clamp01(Mathf.Abs((v + Mathf.Sin(u * 15f + 1.2f) * 0.020f) - 0.62f) * 105f);
        float exposedStone = Mathf.Pow(SmoothNoise(u * 55f, v * 55f, 97f), 5.0f);
        float dust = Mathf.Pow(1f - v, 1.7f) * 0.045f;
        float tone = 0.088f + aggregate * 0.095f + chipped * 0.055f + exposedStone * 0.09f - Mathf.Max(edgeCrackA, edgeCrackB) * 0.12f + dust;
        tone = Mathf.Clamp01(tone);
        return new Color(tone * 0.82f, tone * 0.86f, tone * 0.84f, 1f);
    }

    private static float StepLine(float value, float width)
    {
        return value < width || value > 1f - width ? 1f : 0f;
    }

    private static float FractalNoise(float x, float y, float seed)
    {
        float total = 0f;
        float amplitude = 0.5f;
        float frequency = 1f;
        for (int i = 0; i < 4; i++)
        {
            total += SmoothNoise(x * frequency, y * frequency, seed + i * 13.1f) * amplitude;
            frequency *= 2.0f;
            amplitude *= 0.5f;
        }

        return Mathf.Clamp01(total);
    }

    private static float SmoothNoise(float x, float y, float seed)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float tx = Mathf.SmoothStep(0f, 1f, x - x0);
        float ty = Mathf.SmoothStep(0f, 1f, y - y0);
        float a = Hash01(x0, y0, seed);
        float b = Hash01(x0 + 1, y0, seed);
        float c = Hash01(x0, y0 + 1, seed);
        float d = Hash01(x0 + 1, y0 + 1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    private static float Hash01(float x, float y, float seed)
    {
        return Mathf.Repeat(Mathf.Sin(x * 127.1f + y * 311.7f + seed * 74.7f) * 43758.5453f, 1f);
    }

    private static Material CreateMaterial(string name, Color color, bool emission)
    {
        string assetPath = MaterialPath + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", emission ? 0.45f : 0.08f);

        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 1.8f);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folderName = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void EnsurePlayerTag()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
            if (tag.stringValue == "Player")
                return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
        newTag.stringValue = "Player";
        tagManager.ApplyModifiedProperties();
    }
}
#endif
