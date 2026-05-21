#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MR_MusicSetupPass
{
    private const string ScenePath = "Assets/MemoryRecycler3D/Scenes/Prototype3D.unity";
    private const string ExplorationPath = "Assets/MemoryRecycler3D/Audio/BGM/Exploration/MR_01_Exploration_Loop.wav";
    private const string NightPath = "Assets/MemoryRecycler3D/Audio/BGM/Night/MR_02_Night_Loop.wav";
    private const string ArchivePath = "Assets/MemoryRecycler3D/Audio/BGM/Archive/MR_03_CentralArchive_Loop.wav";
    private const string EndingPath = "Assets/MemoryRecycler3D/Audio/BGM/Ending/MR_04_Ending_Bittersweet_Loop.wav";

    [MenuItem("Tools/Memory Recycler 3D/Apply BGM Setup")]
    public static void ApplyPass()
    {
        Scene scene = GetOrOpenPrototypeScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[MR_MusicSetupPass] Prototype3D scene not found: " + ScenePath);
            return;
        }

        EnsureMusicManager(scene);
        EnsureCentralArchiveZone(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MR_MusicSetupPass] BGM setup applied.");
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

    private static void EnsureMusicManager(Scene scene)
    {
        GameObject managers = FindRoot(scene, "Managers");
        if (managers == null)
        {
            managers = new GameObject("Managers");
            SceneManager.MoveGameObjectToScene(managers, scene);
        }

        MemoryRecyclerMusicManager music = managers.GetComponent<MemoryRecyclerMusicManager>();
        if (music == null)
            music = managers.AddComponent<MemoryRecyclerMusicManager>();

        music.explorationLoop = LoadClip(ExplorationPath);
        music.nightLoop = LoadClip(NightPath);
        music.archiveLoop = LoadClip(ArchivePath);
        music.endingLoop = LoadClip(EndingPath);
        music.defaultVolume = 0.3f;
        music.fadeDuration = 1.4f;
        music.playOnStart = true;

        AudioSource[] sources = managers.GetComponents<AudioSource>();
        while (sources.Length < 2)
        {
            managers.AddComponent<AudioSource>();
            sources = managers.GetComponents<AudioSource>();
        }

        for (int i = 0; i < sources.Length; i++)
        {
            sources[i].playOnAwake = false;
            sources[i].loop = true;
            sources[i].spatialBlend = 0f;
            sources[i].volume = 0f;
        }

        EditorUtility.SetDirty(music);
        EditorUtility.SetDirty(managers);
    }

    private static void EnsureCentralArchiveZone(Scene scene)
    {
        GameObject zone = FindSceneObject(scene, "Central Archive Music Zone");
        if (zone == null)
        {
            zone = new GameObject("Central Archive Music Zone");
            SceneManager.MoveGameObjectToScene(zone, scene);
        }

        GameObject archive = FindSceneObject(scene, "Central Archive Terminal");
        Vector3 archivePosition = archive != null ? archive.transform.position : new Vector3(0f, 3f, 18f);
        zone.transform.position = new Vector3(archivePosition.x, 3f, archivePosition.z);
        zone.transform.rotation = Quaternion.identity;
        zone.transform.localScale = Vector3.one;

        BoxCollider box = zone.GetComponent<BoxCollider>();
        if (box == null)
            box = zone.AddComponent<BoxCollider>();

        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = new Vector3(34f, 10f, 34f);

        CentralArchiveMusicZone musicZone = zone.GetComponent<CentralArchiveMusicZone>();
        if (musicZone == null)
            musicZone = zone.AddComponent<CentralArchiveMusicZone>();

        EditorUtility.SetDirty(box);
        EditorUtility.SetDirty(musicZone);
        EditorUtility.SetDirty(zone);
    }

    private static AudioClip LoadClip(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
            Debug.LogWarning("[MR_MusicSetupPass] Missing BGM clip: " + path);
        return clip;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
                return roots[i];
        }

        return null;
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, name);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
#endif
