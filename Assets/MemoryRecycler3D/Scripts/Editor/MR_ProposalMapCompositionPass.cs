#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 기획서의 맵 구성을 기존 Prototype3D 씬 위에 비파괴적으로 덧입히는 패스.
// MemoryObject3D, ArchiveTerminal3D 같은 기존 연결은 유지하고 위치/장식 루트만 정렬한다.
public static class MR_ProposalMapCompositionPass
{
    private const string ScenePath = "Assets/MemoryRecycler3D/Scenes/Prototype3D.unity";
    private const string GeneratedRootName = "Proposal Map Composition Pass";
    private const string EnvironmentLayerName = "MR3D_Environment";
    private static int environmentLayer;

    [MenuItem("Tools/Memory Recycler 3D/Apply Proposal Map Composition (Non-Destructive)")]
    public static void ApplyPassMenu()
    {
        ApplyPass();
    }

    public static void ApplyPass()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogWarning("[MR_ProposalMapCompositionPass] Prototype3D 씬을 찾을 수 없습니다: " + ScenePath);
            return;
        }

        Scene scene = GetOrOpenPrototypeScene();
        if (!scene.IsValid())
            return;

        environmentLayer = ResolveEnvironmentLayer();
        PruneVisualNoise(scene);
        GameObject root = ResetGeneratedRoot(scene);
        Materials mats = Materials.Load();

        ConfigureCameraMask(environmentLayer);
        PlaceCoreObjects(scene);
        BuildDistrictRoads(root.transform, mats);
        BuildDistrictLabels(root.transform, mats);
        BuildMemorySetPieces(root.transform, mats);
        BuildTraversalProps(root.transform, mats);
        BuildWallAttachedUtilities(root.transform, mats);
        PlaceExistingAssetAnchors(scene, root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[MR_ProposalMapCompositionPass] 제안서 기반 맵 구성을 비파괴 적용했습니다.");
    }

    private static Scene GetOrOpenPrototypeScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.path == ScenePath)
                return scene;
        }

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static GameObject ResetGeneratedRoot(Scene scene)
    {
        GameObject existing = FindSceneObject(scene, GeneratedRootName);
        if (existing != null)
            Object.DestroyImmediate(existing);

        GameObject root = new GameObject(GeneratedRootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static void PlaceCoreObjects(Scene scene)
    {
        SetWorldTransform(FindSceneObject(scene, "Player_Recycler"), new Vector3(0f, 0f, -22f), Vector3.zero, Vector3.one);
        SetWorldTransform(FindSceneObject(scene, "Main Camera"), new Vector3(0f, 3.1f, -28.2f), new Vector3(14f, 0f, 0f), Vector3.one);

        SetWorldTransform(FindSceneObject(scene, "Central Archive Terminal"), new Vector3(0f, 3.9f, 8f), Vector3.zero, new Vector3(3.4f, 7.8f, 1.6f));

        SetWorldTransform(FindSceneObject(scene, "Memory Orb_MR3D_001"), new Vector3(-13.5f, 1.25f, -7.0f), Vector3.zero, Vector3.one * 0.7f);
        SetWorldTransform(FindSceneObject(scene, "Memory Orb_MR3D_002"), new Vector3(-16.5f, 1.25f, 7.0f), Vector3.zero, Vector3.one * 0.7f);
        SetWorldTransform(FindSceneObject(scene, "Memory Orb_MR3D_003"), new Vector3(13.0f, 1.25f, -8.0f), Vector3.zero, Vector3.one * 0.7f);
        SetWorldTransform(FindSceneObject(scene, "Memory Orb_MR3D_004"), new Vector3(15.5f, 1.25f, 7.5f), Vector3.zero, Vector3.one * 0.7f);
        SetWorldTransform(FindSceneObject(scene, "Memory Orb_MR3D_005"), new Vector3(-8.5f, 1.25f, 22.0f), Vector3.zero, Vector3.one * 0.7f);
        SetWorldTransform(FindSceneObject(scene, "Memory Orb_MR3D_006"), new Vector3(4.5f, 1.25f, 23.5f), Vector3.zero, Vector3.one * 0.7f);
        SetWorldTransform(FindSceneObject(scene, "Memory Orb_MR3D_007"), new Vector3(17.5f, 1.25f, 19.0f), Vector3.zero, Vector3.one * 0.7f);
        SetWorldTransform(FindSceneObject(scene, "Memory Orb_MR3D_008"), new Vector3(0f, 1.25f, 12.4f), Vector3.zero, Vector3.one * 0.7f);

        SetWorldTransform(FindSceneObject(scene, "Story Terminal_Hospital"), new Vector3(9.5f, 1.15f, -10.8f), new Vector3(0f, -30f, 0f), new Vector3(0.72f, 1.35f, 0.12f));
        SetWorldTransform(FindSceneObject(scene, "Story Terminal_School"), new Vector3(-18.2f, 1.15f, 10.0f), new Vector3(0f, 42f, 0f), new Vector3(0.72f, 1.35f, 0.12f));
        SetWorldTransform(FindSceneObject(scene, "Story Terminal_Broadcast"), new Vector3(20.5f, 1.15f, 20.5f), new Vector3(0f, -64f, 0f), new Vector3(0.72f, 1.35f, 0.12f));
        SetWorldTransform(FindSceneObject(scene, "Story Terminal_ArchiveGate"), new Vector3(-6.2f, 1.15f, 11.3f), new Vector3(0f, 18f, 0f), new Vector3(0.72f, 1.35f, 0.12f));
    }

    private static void BuildDistrictRoads(Transform root, Materials mats)
    {
        CreateBox(root, "Central Ring Road", new Vector3(0f, 0.011f, 8f), Vector3.zero, new Vector3(23f, 0.022f, 23f), mats.road);
        CreateBox(root, "Central Archive Plaza", new Vector3(0f, 0.024f, 8f), Vector3.zero, new Vector3(10.5f, 0.030f, 10.5f), mats.darkPanel);
        CreateBox(root, "South Entry Road", new Vector3(0f, 0.019f, -9f), Vector3.zero, new Vector3(6.2f, 0.028f, 30f), mats.roadPatch);
        CreateBox(root, "Residential Spur Road", new Vector3(-10.5f, 0.019f, -5.5f), new Vector3(0f, 24f, 0f), new Vector3(4.6f, 0.028f, 15f), mats.roadPatch);
        CreateBox(root, "School Spur Road", new Vector3(-13.7f, 0.019f, 7.2f), new Vector3(0f, -28f, 0f), new Vector3(4.3f, 0.028f, 14f), mats.roadPatch);
        CreateBox(root, "Admin Spur Road", new Vector3(12.3f, 0.019f, 6.5f), new Vector3(0f, 28f, 0f), new Vector3(4.3f, 0.028f, 14f), mats.roadPatch);
        CreateBox(root, "Outer Ruins Path", new Vector3(4.2f, 0.019f, 20.3f), new Vector3(0f, -16f, 0f), new Vector3(5f, 0.028f, 20f), mats.roadPatch);
    }

    private static void BuildDistrictLabels(Transform root, Materials mats)
    {
        CreateDistrictMarker(root, "주거 구역", new Vector3(-13.5f, 0.08f, -7.0f), new Color(0.12f, 0.20f, 0.18f, 1f), mats);
        CreateDistrictMarker(root, "학교/기록 구역", new Vector3(-16.5f, 0.08f, 7.0f), new Color(0.14f, 0.16f, 0.23f, 1f), mats);
        CreateDistrictMarker(root, "기억 치료 병동", new Vector3(13.0f, 0.08f, -8.0f), new Color(0.10f, 0.18f, 0.22f, 1f), mats);
        CreateDistrictMarker(root, "행정 서버 구역", new Vector3(15.5f, 0.08f, 7.5f), new Color(0.19f, 0.13f, 0.11f, 1f), mats);
        CreateDistrictMarker(root, "외곽 폐허", new Vector3(3.8f, 0.08f, 21.5f), new Color(0.15f, 0.14f, 0.10f, 1f), mats);
        CreateDistrictMarker(root, "중앙 아카이브", new Vector3(0f, 0.09f, 8f), new Color(0.04f, 0.20f, 0.20f, 1f), mats);

        CreateLabel(root, "District Label Residential", "주거 구역", new Vector3(-13.5f, 2.5f, -11.6f), new Vector3(72f, 0f, 0f), 0.08f, mats.labelColor);
        CreateLabel(root, "District Label School", "학교 / 기록 구역", new Vector3(-19.8f, 2.5f, 6.0f), new Vector3(72f, 35f, 0f), 0.08f, mats.labelColor);
        CreateLabel(root, "District Label Hospital", "기억 치료 병동", new Vector3(12.5f, 2.5f, -12.6f), new Vector3(72f, 0f, 0f), 0.08f, mats.labelColor);
        CreateLabel(root, "District Label Admin", "행정 서버 구역", new Vector3(19.5f, 2.5f, 6.8f), new Vector3(72f, -35f, 0f), 0.08f, mats.labelColor);
        CreateLabel(root, "District Label Outer", "외곽 폐허", new Vector3(4.8f, 2.5f, 27.0f), new Vector3(72f, 180f, 0f), 0.08f, mats.labelColor);
    }

    private static void BuildMemorySetPieces(Transform root, Materials mats)
    {
        BuildResidentialMemory(root, mats);
        BuildSchoolMemory(root, mats);
        BuildHospitalMemory(root, mats);
        BuildAdminMemory(root, mats);
        BuildUmbrellaMemory(root, mats);
        BuildPlayerFileMemory(root, mats);
        BuildBroadcastMemory(root, mats);
        BuildArchiveLullabyMemory(root, mats);
    }

    private static void BuildResidentialMemory(Transform root, Materials mats)
    {
        Transform group = NewGroup(root, "Memory Set MR3D_001 Residential Table");
        CreateBox(group, "Broken Dining Table", new Vector3(-13.5f, 0.65f, -7.0f), Vector3.zero, new Vector3(2.0f, 0.16f, 1.25f), mats.boards, true);
        for (int i = 0; i < 4; i++)
        {
            float x = -14.25f + (i % 2) * 1.5f;
            float z = -7.45f + (i / 2) * 0.9f;
            CreateCylinder(group, "Table Leg", new Vector3(x, 0.32f, z), Vector3.zero, new Vector3(0.07f, 0.32f, 0.07f), mats.trim);
        }
        CreateCylinder(group, "Dropped Plate", new Vector3(-12.95f, 0.78f, -7.05f), new Vector3(90f, 0f, 0f), new Vector3(0.42f, 0.025f, 0.42f), mats.debris);
        CreateBox(group, "Family Photo Frame", new Vector3(-14.15f, 0.86f, -6.75f), new Vector3(0f, -18f, 8f), new Vector3(0.55f, 0.05f, 0.38f), mats.darkPanel);
    }

    private static void BuildSchoolMemory(Transform root, Materials mats)
    {
        Transform group = NewGroup(root, "Memory Set MR3D_002 School Album");
        for (int i = 0; i < 3; i++)
        {
            CreateBox(group, "Empty Desk", new Vector3(-17.8f + i * 1.2f, 0.65f, 6.0f + i * 0.22f), new Vector3(0f, -12f, 0f), new Vector3(0.85f, 0.15f, 0.55f), mats.boards, true);
            CreateBox(group, "Desk Legs", new Vector3(-17.8f + i * 1.2f, 0.35f, 6.0f + i * 0.22f), new Vector3(0f, -12f, 0f), new Vector3(0.72f, 0.5f, 0.08f), mats.trim);
        }
        CreateBox(group, "Open Graduation Album", new Vector3(-16.5f, 0.92f, 7.0f), new Vector3(0f, -22f, 0f), new Vector3(1.0f, 0.045f, 0.7f), mats.lightPanel);
        CreateBox(group, "Erased Name Strip", new Vector3(-16.5f, 0.98f, 7.0f), new Vector3(0f, -22f, 0f), new Vector3(0.85f, 0.025f, 0.08f), mats.accent);
    }

    private static void BuildHospitalMemory(Transform root, Materials mats)
    {
        Transform group = NewGroup(root, "Memory Set MR3D_003 Treatment Ward");
        CreateBox(group, "Treatment Bed", new Vector3(13.0f, 0.58f, -8.0f), new Vector3(0f, 20f, 0f), new Vector3(2.25f, 0.22f, 0.85f), mats.lightPanel, true);
        CreateBox(group, "Consent Monitor", new Vector3(14.05f, 1.25f, -8.7f), new Vector3(0f, -18f, 0f), new Vector3(0.75f, 0.95f, 0.12f), mats.terminal);
        CreateCylinder(group, "IV Stand", new Vector3(12.05f, 1.15f, -8.85f), Vector3.zero, new Vector3(0.045f, 0.85f, 0.045f), mats.trim);
        CreateBox(group, "Consent Paper", new Vector3(13.0f, 0.78f, -7.55f), new Vector3(0f, 20f, 0f), new Vector3(0.68f, 0.025f, 0.42f), mats.boards);
    }

    private static void BuildAdminMemory(Transform root, Materials mats)
    {
        Transform group = NewGroup(root, "Memory Set MR3D_004 Admin Server");
        for (int i = 0; i < 4; i++)
            CreateBox(group, "Server Rack", new Vector3(14.3f + i * 0.72f, 1.1f, 7.15f), Vector3.zero, new Vector3(0.46f, 1.8f, 0.52f), mats.darkPanel);
        CreateBox(group, "Emergency Order Plate", new Vector3(15.5f, 1.55f, 8.05f), new Vector3(0f, 180f, 0f), new Vector3(1.35f, 0.55f, 0.08f), mats.accent);
        CreateCylinder(group, "Data Core", new Vector3(15.5f, 0.85f, 7.5f), Vector3.zero, new Vector3(0.42f, 0.75f, 0.42f), mats.memory);
    }

    private static void BuildUmbrellaMemory(Transform root, Materials mats)
    {
        Transform group = NewGroup(root, "Memory Set MR3D_005 Blue Umbrella");
        CreateCylinder(group, "Umbrella Handle", new Vector3(-8.7f, 0.68f, 22.0f), new Vector3(0f, 0f, -16f), new Vector3(0.045f, 0.65f, 0.045f), mats.trim);
        CreateSphere(group, "Blue Umbrella Canopy", new Vector3(-8.5f, 1.1f, 22.0f), Vector3.zero, new Vector3(1.2f, 0.23f, 1.2f), mats.accent);
        CreateBox(group, "Subway Shelter Line", new Vector3(-7.7f, 0.28f, 22.8f), new Vector3(0f, -18f, 0f), new Vector3(2.2f, 0.08f, 0.22f), mats.marking);
    }

    private static void BuildPlayerFileMemory(Transform root, Materials mats)
    {
        Transform group = NewGroup(root, "Memory Set MR3D_006 Player File");
        CreateBox(group, "Locked Archive Cabinet", new Vector3(4.5f, 0.9f, 23.5f), new Vector3(0f, 18f, 0f), new Vector3(1.1f, 1.55f, 0.7f), mats.darkPanel, true);
        CreateBox(group, "Empty File Drawer", new Vector3(4.5f, 1.0f, 22.95f), new Vector3(0f, 18f, 0f), new Vector3(0.82f, 0.16f, 0.42f), mats.lightPanel);
        CreateBox(group, "Name Tag Glow", new Vector3(4.5f, 1.58f, 23.05f), new Vector3(0f, 18f, 0f), new Vector3(0.64f, 0.08f, 0.04f), mats.memory);
    }

    private static void BuildBroadcastMemory(Transform root, Materials mats)
    {
        Transform group = NewGroup(root, "Memory Set MR3D_007 Broadcast Mic");
        CreateCylinder(group, "Broadcast Mast", new Vector3(17.5f, 2.0f, 19.0f), Vector3.zero, new Vector3(0.07f, 2.0f, 0.07f), mats.trim);
        CreateSphere(group, "Rusty Microphone Head", new Vector3(17.5f, 1.2f, 18.35f), Vector3.zero, new Vector3(0.35f, 0.35f, 0.35f), mats.debris);
        CreateCylinder(group, "Microphone Handle", new Vector3(17.5f, 0.85f, 18.6f), new Vector3(65f, 0f, 0f), new Vector3(0.08f, 0.45f, 0.08f), mats.trim);
        CreateBox(group, "Broadcast Warning Panel", new Vector3(18.45f, 1.7f, 19.35f), new Vector3(0f, -40f, 0f), new Vector3(1.15f, 0.58f, 0.08f), mats.terminal);
    }

    private static void BuildArchiveLullabyMemory(Transform root, Materials mats)
    {
        Transform group = NewGroup(root, "Memory Set MR3D_008 Archive Lullaby");
        CreateCylinder(group, "Archive Lullaby Pedestal", new Vector3(0f, 0.42f, 12.4f), Vector3.zero, new Vector3(1.15f, 0.42f, 1.15f), mats.darkPanel, true);
        CreateSphere(group, "Lullaby Speaker Core", new Vector3(0f, 1.1f, 12.4f), Vector3.zero, new Vector3(0.48f, 0.48f, 0.48f), mats.memory);
        CreateBox(group, "Archive Command Ring A", new Vector3(0f, 1.32f, 12.4f), new Vector3(0f, 35f, 0f), new Vector3(1.8f, 0.035f, 0.08f), mats.accent);
        CreateBox(group, "Archive Command Ring B", new Vector3(0f, 1.32f, 12.4f), new Vector3(0f, -35f, 0f), new Vector3(1.8f, 0.035f, 0.08f), mats.accent);
    }

    private static void PlaceExistingAssetAnchors(Scene scene, Transform root)
    {
        PlacePrefab(scene, root, "Proposal Ruined Apartments Residential", "Assets/MemoryRecycler3D/ExternalAssets/Tripo/Buildings/RuinedApartmentBuilding/RuinedApartmentBuilding.fbx", new Vector3(-24f, 0f, -9f), new Vector3(0f, 24f, 0f), 3.8f);
        PlacePrefab(scene, root, "Proposal School Korean Ruin", "Assets/MemoryRecycler3D/ExternalAssets/Tripo/Buildings/KoreanRuinedBuilding/KoreanRuinedBuilding.fbx", new Vector3(-25f, 0f, 10f), new Vector3(0f, 42f, 0f), 3.5f);
        PlacePrefab(scene, root, "Proposal Hospital Concrete Ruin", "Assets/MemoryRecycler3D/ExternalAssets/Tripo/Buildings/RuinedConcreteBuilding/RuinedConcreteBuilding.fbx", new Vector3(24f, 0f, -9f), new Vector3(0f, -28f, 0f), 3.6f);
        PlacePrefab(scene, root, "Proposal Admin Tower", "Assets/MemoryRecycler3D/ExternalAssets/Tripo/Buildings/PostApocalypticTower/PostApocalypticTower.fbx", new Vector3(25f, 0f, 10f), new Vector3(0f, -36f, 0f), 3.4f);
        PlacePrefab(scene, root, "Proposal Outer City Block", "Assets/MemoryRecycler3D/ExternalAssets/Tripo/Buildings/RuinedCityBlock/RuinedCityBlock.fbx", new Vector3(0f, 0f, 33.5f), new Vector3(0f, 180f, 0f), 4.2f);
    }

    private static void PlacePrefab(Scene scene, Transform parent, string name, string path, Vector3 position, Vector3 euler, float scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            return;

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null)
            instance = Object.Instantiate(prefab);

        instance.name = name;
        instance.transform.SetParent(parent, true);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(euler);
        instance.transform.localScale = Vector3.one * scale;
        DisableColliders(instance);
        SnapBottomToGround(instance, 0f);
        EditorUtility.SetDirty(instance);
    }

    private static void BuildTraversalProps(Transform root, Materials mats)
    {
        Transform group = NewGroup(root, "Intentional Jumpable Debris");
        CreateJumpStack(group, "Residential Rubble Step", new Vector3(-11.7f, 0f, -8.9f), new Vector3(0f, -18f, 0f), mats);
        CreateJumpStack(group, "School Desk Step", new Vector3(-15.0f, 0f, 5.1f), new Vector3(0f, 12f, 0f), mats);
        CreateJumpStack(group, "Hospital Slab Step", new Vector3(10.8f, 0f, -7.2f), new Vector3(0f, 24f, 0f), mats);
        CreateJumpStack(group, "Outer Archive Step", new Vector3(2.4f, 0f, 21.6f), new Vector3(0f, -30f, 0f), mats);
    }

    private static void CreateJumpStack(Transform parent, string prefix, Vector3 basePosition, Vector3 euler, Materials mats)
    {
        CreateGroundedBox(parent, prefix + " Low", basePosition + new Vector3(-0.55f, 0f, -0.15f), euler, new Vector3(1.25f, 0.32f, 0.9f), mats.debris, true);
        CreateGroundedBox(parent, prefix + " Mid", basePosition + new Vector3(0.32f, 0f, 0.18f), euler + new Vector3(0f, 13f, 0f), new Vector3(1.0f, 0.58f, 0.72f), mats.boards, true);
        CreateGroundedBox(parent, prefix + " High", basePosition + new Vector3(0.92f, 0f, 0.05f), euler + new Vector3(0f, -9f, 0f), new Vector3(0.74f, 0.82f, 0.64f), mats.darkPanel, true);
    }

    private static void BuildWallAttachedUtilities(Transform root, Materials mats)
    {
        Transform group = NewGroup(root, "Intentional Wall Attached Utilities");
        CreateWallPipeSet(group, "Residential Wall Pipe", new Vector3(-22.2f, 1.55f, -8.0f), 24f, -1f, mats);
        CreateWallPipeSet(group, "Hospital Wall Pipe", new Vector3(22.0f, 1.55f, -8.4f), -28f, 1f, mats);
        CreateWallPipeSet(group, "Admin Wall Pipe", new Vector3(22.9f, 1.65f, 8.8f), -36f, 1f, mats);
    }

    private static void CreateWallPipeSet(Transform parent, string prefix, Vector3 position, float yaw, float side, Materials mats)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 along = rotation * Vector3.right;
        Vector3 outFromWall = rotation * Vector3.forward * side;
        Vector3 basePosition = position + outFromWall * 0.18f;

        CreateBox(parent, prefix + " Horizontal A", basePosition + along * 0.45f, new Vector3(0f, yaw, 0f), new Vector3(1.9f, 0.07f, 0.07f), mats.trim);
        CreateBox(parent, prefix + " Horizontal B", basePosition + Vector3.up * 0.42f - along * 0.10f, new Vector3(0f, yaw, 0f), new Vector3(1.35f, 0.06f, 0.06f), mats.trim);
        CreateBox(parent, prefix + " Vertical Drop", basePosition + Vector3.down * 0.42f - along * 0.52f, new Vector3(0f, yaw, 0f), new Vector3(0.08f, 0.95f, 0.08f), mats.trim);
        CreateBox(parent, prefix + " Junction Box", basePosition + Vector3.down * 0.88f - along * 0.52f, new Vector3(0f, yaw, 0f), new Vector3(0.34f, 0.28f, 0.18f), mats.darkPanel);
    }

    private static GameObject CreateGroundedBox(Transform parent, string name, Vector3 basePosition, Vector3 euler, Vector3 scale, Material material, bool keepCollider)
    {
        Vector3 position = basePosition + Vector3.up * (scale.y * 0.5f);
        return CreateBox(parent, name, position, euler, scale, material, keepCollider);
    }

    private static void CreateDistrictMarker(Transform root, string name, Vector3 position, Color color, Materials mats)
    {
        Material markerMat = new Material(mats.darkPanel);
        markerMat.name = "Runtime District Marker " + name;
        if (markerMat.HasProperty("_BaseColor"))
            markerMat.SetColor("_BaseColor", color);
        if (markerMat.HasProperty("_Color"))
            markerMat.SetColor("_Color", color);
        CreateBox(root, "District Marker " + name, position, Vector3.zero, new Vector3(5.8f, 0.04f, 5.8f), markerMat);
    }

    private static Transform NewGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 euler, Vector3 scale, Material material, bool keepCollider = false)
    {
        return CreatePrimitive(parent, name, PrimitiveType.Cube, position, euler, scale, material, keepCollider);
    }

    private static GameObject CreateCylinder(Transform parent, string name, Vector3 position, Vector3 euler, Vector3 scale, Material material, bool keepCollider = false)
    {
        return CreatePrimitive(parent, name, PrimitiveType.Cylinder, position, euler, scale, material, keepCollider);
    }

    private static GameObject CreateSphere(Transform parent, string name, Vector3 position, Vector3 euler, Vector3 scale, Material material, bool keepCollider = false)
    {
        return CreatePrimitive(parent, name, PrimitiveType.Sphere, position, euler, scale, material, keepCollider);
    }

    private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 euler, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(primitive);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null && keepCollider)
        {
            collider.isTrigger = false;
            go.layer = environmentLayer;
        }
        else if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return go;
    }

    private static void PruneVisualNoise(Scene scene)
    {
        GameObject instructions = FindSceneObject(scene, "Prototype Instructions");
        if (instructions != null)
        {
            instructions.SetActive(false);
            EditorUtility.SetDirty(instructions);
        }

        string[] prefixes =
        {
            "Cinematic Foreground",
            "Cinematic Background",
            "Cinematic Perimeter",
            "Cinematic Outer",
            "Cinematic Side Alley",
            "Cinematic Alley",
            "Cinematic Ring Road",
            "Cinematic Radial Road",
            "Cinematic Ring Roof Unit",
            "Cinematic Road Patch",
            "Cinematic Street Cross Shadow",
            "Cinematic Distant Gate",
            "Cinematic Collapsed Lintel",
            "Cinematic Far Archive Signal",
            "Cinematic Rooftop Pipe Cluster",
            "Cinematic Sagging Cable",
            "Cinematic Loose Cable Drop",
            "Cinematic Cable Clamp",
            "Ref Hanging Cable",
            "Ref Facade Pipe",
            "Tripo Wall Pipes",
            "Tripo Recycling Sign",
            "Proposal Rubble Residential",
            "Proposal Recycling Sign Gate"
        };

        List<GameObject> targets = new List<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            CollectObjectsWithPrefixes(roots[i].transform, prefixes, targets);

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null)
                Object.DestroyImmediate(targets[i]);
        }

        StripDecorativeColliders(scene);
    }

    private static void CollectObjectsWithPrefixes(Transform root, string[] prefixes, List<GameObject> targets)
    {
        for (int i = 0; i < prefixes.Length; i++)
        {
            if (root.name.StartsWith(prefixes[i]))
            {
                targets.Add(root.gameObject);
                return;
            }
        }

        foreach (Transform child in root)
            CollectObjectsWithPrefixes(child, prefixes, targets);
    }

    private static void StripDecorativeColliders(Scene scene)
    {
        string[] fragments =
        {
            "Cinematic ",
            "Ref Hanging Cable",
            "Ref Facade Pipe",
            "Tripo Wall Pipes",
            "Tripo Recycling Sign"
        };

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            StripDecorativeCollidersRecursive(roots[i].transform, fragments);
    }

    private static void StripDecorativeCollidersRecursive(Transform root, string[] fragments)
    {
        bool match = false;
        for (int i = 0; i < fragments.Length; i++)
        {
            if (root.name.Contains(fragments[i]))
            {
                match = true;
                break;
            }
        }

        if (match)
        {
            Collider[] colliders = root.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && !colliders[i].isTrigger)
                    Object.DestroyImmediate(colliders[i]);
            }
        }

        foreach (Transform child in root)
            StripDecorativeCollidersRecursive(child, fragments);
    }

    private static int ResolveEnvironmentLayer()
    {
        int existing = LayerMask.NameToLayer(EnvironmentLayerName);
        if (existing >= 0)
            return existing;

        Object[] tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets == null || tagAssets.Length == 0)
            return 0;

        SerializedObject tagManager = new SerializedObject(tagAssets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        if (layers == null)
            return 0;

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (slot != null && string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = EnvironmentLayerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        return 0;
    }

    private static void ConfigureCameraMask(int envLayer)
    {
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

    private static void SnapBottomToGround(GameObject go, float groundY)
    {
        if (go == null)
            return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float deltaY = groundY - bounds.min.y;
        if (Mathf.Abs(deltaY) > 0.001f)
            go.transform.position += Vector3.up * deltaY;
    }

    private static void CreateLabel(Transform parent, string name, string text, Vector3 position, Vector3 euler, float characterSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(euler);
        TextMesh mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.characterSize = characterSize;
        mesh.fontSize = 64;
        mesh.color = color;
    }

    private static void SetWorldTransform(GameObject go, Vector3 position, Vector3 euler, Vector3 scale)
    {
        if (go == null)
            return;

        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        EditorUtility.SetDirty(go.transform);
        EditorUtility.SetDirty(go);
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject found = FindInChildrenIncludingInactive(roots[i].transform, objectName);
            if (found != null)
                return found;
        }
        return null;
    }

    private static GameObject FindInChildrenIncludingInactive(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root.gameObject;

        foreach (Transform child in root)
        {
            GameObject found = FindInChildrenIncludingInactive(child, objectName);
            if (found != null)
                return found;
        }
        return null;
    }

    private static void DisableColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private struct Materials
    {
        public Material road;
        public Material roadPatch;
        public Material darkPanel;
        public Material lightPanel;
        public Material boards;
        public Material trim;
        public Material debris;
        public Material accent;
        public Material memory;
        public Material terminal;
        public Material marking;
        public Color labelColor;

        public static Materials Load()
        {
            Materials mats = new Materials
            {
                road = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_Road.mat"),
                roadPatch = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_CinematicRoadPatch.mat"),
                darkPanel = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_DarkPanel.mat"),
                lightPanel = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_WornBoards.mat"),
                boards = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_WornBoards.mat"),
                trim = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_BuildingTrim.mat"),
                debris = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_Debris.mat"),
                accent = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_RecyclerAccent.mat"),
                memory = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_MemoryGlow.mat"),
                terminal = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_Terminal.mat"),
                marking = LoadMat("Assets/MemoryRecycler3D/Materials/MR3D_Marking.mat"),
                labelColor = new Color(0.82f, 0.95f, 1f, 1f)
            };

            Material fallback = mats.road != null ? mats.road : new Material(Shader.Find("Standard"));
            if (mats.roadPatch == null) mats.roadPatch = fallback;
            if (mats.darkPanel == null) mats.darkPanel = fallback;
            if (mats.lightPanel == null) mats.lightPanel = fallback;
            if (mats.boards == null) mats.boards = fallback;
            if (mats.trim == null) mats.trim = fallback;
            if (mats.debris == null) mats.debris = fallback;
            if (mats.accent == null) mats.accent = fallback;
            if (mats.memory == null) mats.memory = fallback;
            if (mats.terminal == null) mats.terminal = fallback;
            if (mats.marking == null) mats.marking = fallback;
            return mats;
        }

        private static Material LoadMat(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }
}
#endif
