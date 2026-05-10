using System;
using System.Collections.Generic;
using UnityEngine;

public class MemoryManager3D : MonoBehaviour
{
    public static MemoryManager3D Instance { get; private set; }

    private const string SaveKey = "MR3D_Save_v2";
    private const string PlayerObjectName = "Player_Recycler";
    private const float AutoSaveInterval = 10f;
    private static readonly Vector3 DefaultPlayerPosition = new Vector3(0f, 0f, -18f);

    public readonly List<MemoryRecord3D> collectedMemories = new List<MemoryRecord3D>();

    private readonly Dictionary<string, MemoryData3D> knownMemories = new Dictionary<string, MemoryData3D>();
    private readonly Dictionary<string, MemoryObject3D> memoryObjects = new Dictionary<string, MemoryObject3D>();
    private SaveData loadedSave;
    private bool loaded;
    private float nextAutoSaveTime;

    [Serializable]
    private class SaveData
    {
        public int preservedCount;
        public int deletedCount;
        public int editedCount;
        public bool hasPlayerPose;
        public Vector3 playerPosition;
        public float playerYaw;
        public List<MemoryEntry> memories = new List<MemoryEntry>();
    }

    [Serializable]
    private class MemoryEntry
    {
        public string id;
        public bool collected;
        public bool restored;
        public MemoryDecision3D decision;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        LoadGame();
    }

    private void Update()
    {
        if (!loaded || !HasSaveGame() || Time.unscaledTime < nextAutoSaveTime)
            return;

        SaveGame();
        nextAutoSaveTime = Time.unscaledTime + AutoSaveInterval;
    }

    public static bool HasSaveGame()
    {
        return PlayerPrefs.HasKey(SaveKey);
    }

    public void RegisterMemoryObject(MemoryObject3D memoryObject)
    {
        if (memoryObject == null || memoryObject.memoryData == null || string.IsNullOrEmpty(memoryObject.memoryData.id))
            return;

        string id = memoryObject.memoryData.id;
        knownMemories[id] = memoryObject.memoryData;
        memoryObjects[id] = memoryObject;

        if (loaded)
            ApplySavedState(memoryObject);
    }

    public void CollectMemory(MemoryData3D memory)
    {
        if (memory == null)
            return;

        MemoryRecord3D record = FindRecord(memory);
        if (record == null)
        {
            record = new MemoryRecord3D(memory);
            collectedMemories.Add(record);
        }

        SaveGame();
        UIManager3D.Instance.ShowMemoryCard(record);
    }

    public void MarkRestored(MemoryData3D memory)
    {
        MemoryRecord3D record = FindRecord(memory);
        if (record != null)
        {
            record.restored = true;
            SaveGame();
        }
    }

    public void ApplyDecision(MemoryData3D memory, MemoryDecision3D decision)
    {
        MemoryRecord3D record = FindRecord(memory);
        if (record == null)
            return;

        if (record.decision == MemoryDecision3D.Unchosen)
        {
            record.decision = decision;
            GameState3D.Instance.RecordDecision(decision);
            WorldToneController3D.Instance.RefreshWorldTone();
            SaveGame();
        }

        if (MemoryAudio3D.Instance != null)
            MemoryAudio3D.Instance.PlayDecision();
        UIManager3D.Instance.ShowToast("기억 처리 완료: " + DecisionToKorean(decision));
    }

    public MemoryRecord3D FindRecord(MemoryData3D memory)
    {
        for (int i = 0; i < collectedMemories.Count; i++)
        {
            if (collectedMemories[i].memory == memory)
                return collectedMemories[i];

            if (collectedMemories[i].memory != null && memory != null && collectedMemories[i].memory.id == memory.id)
                return collectedMemories[i];
        }

        return null;
    }

    public int CountCollectedMemories()
    {
        return collectedMemories.Count;
    }

    public int CountRestoredMemories()
    {
        int count = 0;
        for (int i = 0; i < collectedMemories.Count; i++)
        {
            if (collectedMemories[i].restored)
                count++;
        }
        return count;
    }

    public int CountDecidedMemories()
    {
        int count = 0;
        for (int i = 0; i < collectedMemories.Count; i++)
        {
            if (collectedMemories[i].decision != MemoryDecision3D.Unchosen)
                count++;
        }
        return count;
    }

    public int CountKnownMemories()
    {
        return Mathf.Max(knownMemories.Count, 8);
    }

    public void SaveGame()
    {
        SaveData save = new SaveData
        {
            preservedCount = GameState3D.Instance != null ? GameState3D.Instance.preservedCount : 0,
            deletedCount = GameState3D.Instance != null ? GameState3D.Instance.deletedCount : 0,
            editedCount = GameState3D.Instance != null ? GameState3D.Instance.editedCount : 0
        };

        CapturePlayerPose(save);

        for (int i = 0; i < collectedMemories.Count; i++)
        {
            MemoryRecord3D record = collectedMemories[i];
            if (record.memory == null || string.IsNullOrEmpty(record.memory.id))
                continue;

            save.memories.Add(new MemoryEntry
            {
                id = record.memory.id,
                collected = true,
                restored = record.restored,
                decision = record.decision
            });
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
        loadedSave = save;
        nextAutoSaveTime = Time.unscaledTime + AutoSaveInterval;
    }

    public void LoadGame()
    {
        loaded = true;
        collectedMemories.Clear();

        if (!PlayerPrefs.HasKey(SaveKey))
        {
            loadedSave = new SaveData();
            if (GameState3D.Instance != null)
                GameState3D.Instance.ResetState();
            return;
        }

        loadedSave = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
        if (loadedSave == null)
            loadedSave = new SaveData();

        if (GameState3D.Instance != null)
            GameState3D.Instance.SetDecisionCounts(loadedSave.preservedCount, loadedSave.deletedCount, loadedSave.editedCount);

        foreach (KeyValuePair<string, MemoryObject3D> pair in memoryObjects)
            ApplySavedState(pair.Value);

        ApplySavedPlayerPose();
        nextAutoSaveTime = Time.unscaledTime + AutoSaveInterval;
    }

    public void ContinueSavedGame()
    {
        LoadGame();
    }

    public void StartNewGame()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        loadedSave = new SaveData();
        loaded = true;
        collectedMemories.Clear();
        if (GameState3D.Instance != null)
            GameState3D.Instance.ResetState();

        foreach (KeyValuePair<string, MemoryObject3D> pair in memoryObjects)
            pair.Value.SetCollectedFromSave(false);

        ResetPlayerPose();
        if (WorldToneController3D.Instance != null)
            WorldToneController3D.Instance.RefreshWorldTone();
    }

    public void ClearSave()
    {
        StartNewGame();

        UIManager3D.Instance.ShowToast("저장된 아카이브 상태를 초기화했습니다.");
    }

    private void CapturePlayerPose(SaveData save)
    {
        Transform player = FindPlayerTransform();
        if (player == null)
            return;

        save.hasPlayerPose = true;
        save.playerPosition = player.position;
        save.playerYaw = player.eulerAngles.y;
    }

    private void ApplySavedPlayerPose()
    {
        if (loadedSave == null || !loadedSave.hasPlayerPose)
            return;

        ApplyPlayerPose(loadedSave.playerPosition, loadedSave.playerYaw);
    }

    private void ResetPlayerPose()
    {
        ApplyPlayerPose(DefaultPlayerPosition, 0f);
    }

    private void ApplyPlayerPose(Vector3 position, float yaw)
    {
        Transform player = FindPlayerTransform();
        if (player == null)
            return;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        player.position = position;
        player.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (controller != null)
            controller.enabled = true;
    }

    private Transform FindPlayerTransform()
    {
        GameObject player = GameObject.Find(PlayerObjectName);
        return player != null ? player.transform : null;
    }

    private void ApplySavedState(MemoryObject3D memoryObject)
    {
        if (memoryObject == null || memoryObject.memoryData == null)
            return;

        MemoryEntry entry = FindEntry(memoryObject.memoryData.id);
        bool collected = entry != null && entry.collected;
        memoryObject.SetCollectedFromSave(collected);

        if (!collected)
            return;

        MemoryRecord3D record = FindRecord(memoryObject.memoryData);
        if (record == null)
        {
            record = new MemoryRecord3D(memoryObject.memoryData);
            collectedMemories.Add(record);
        }

        record.restored = entry.restored;
        record.decision = entry.decision;
    }

    private MemoryEntry FindEntry(string id)
    {
        if (loadedSave == null || loadedSave.memories == null)
            return null;

        for (int i = 0; i < loadedSave.memories.Count; i++)
        {
            if (loadedSave.memories[i].id == id)
                return loadedSave.memories[i];
        }

        return null;
    }

    public static string DecisionToKorean(MemoryDecision3D decision)
    {
        switch (decision)
        {
            case MemoryDecision3D.Preserve: return "보존";
            case MemoryDecision3D.Delete: return "삭제";
            case MemoryDecision3D.Edit: return "재가공";
            default: return "미선택";
        }
    }
}
