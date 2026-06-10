using System;
using System.Collections.Generic;
using UnityEngine;

public class MemoryManager3D : MonoBehaviour
{
    public static MemoryManager3D Instance { get; private set; }

    private const string SaveKey = "MR3D_Save_v2";
    private const string PlayerObjectName = "Player_Recycler";
    private const float AutoSaveInterval = 10f;
    private static readonly Vector3 DefaultPlayerPosition = new Vector3(0f, 0f, -22f);

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
        if (memoryObject == null)
            return;

        // 발표 중 데이터 누락으로 조용히 회수 불가가 되는 사고를 막기 위한 가드 경고.
        if (memoryObject.memoryData == null)
        {
            Debug.LogWarning("[MR3D] MemoryObject3D에 MemoryData3D가 비어 있어 등록 불가: " + memoryObject.name, memoryObject);
            return;
        }
        if (string.IsNullOrEmpty(memoryObject.memoryData.id))
        {
            Debug.LogWarning("[MR3D] MemoryData3D id가 비어 저장/복원에서 누락됩니다: " + memoryObject.memoryData.name, memoryObject);
            return;
        }

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
        UIManager3D.Instance.ShowMemoryCard(record, "MemoryOrb");
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

        if (!record.restored)
        {
            Debug.LogWarning("[MR3D] 복원되지 않은 기억은 처리할 수 없습니다: " + (memory != null ? memory.id : "null"), memory);
            return;
        }

        if (record.decision == MemoryDecision3D.Unchosen)
        {
            record.decision = decision;
            GameState3D.Instance.RecordDecision(decision);
            PlayerMemoryLog3D.Ensure().RecordDecision(memory, decision);
            WorldToneController3D.Instance.RefreshWorldTone();
            // 처리 완료 시점에 월드 잔상 UI를 숨긴다.
            MemoryLensEcho3D.NotifyStateChanged(memory, "processed");
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
            if (collectedMemories[i].restored && collectedMemories[i].decision != MemoryDecision3D.Unchosen)
                count++;
        }
        return count;
    }

    public int CountKnownMemories()
    {
        // HUD가 회수/처리 임계값과 일관되도록, 최소 표시 분모를 엔딩 임계값으로 맞춘다.
        // 씬에 등록된 기억이 임계값보다 적으면 임계값으로, 많으면 실제 개수로 표시.
        return Mathf.Max(knownMemories.Count, UIManager3D.RequiredDecisionsForEnding);
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
                decision = record.restored ? record.decision : MemoryDecision3D.Unchosen
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
            PlayerMemoryLog3D.Ensure().ResetLog();
            return;
        }

        loadedSave = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
        if (loadedSave == null)
            loadedSave = new SaveData();

        PlayerMemoryLog3D.Ensure().LoadLog();

        if (GameState3D.Instance != null)
            GameState3D.Instance.SetDecisionCounts(loadedSave.preservedCount, loadedSave.deletedCount, loadedSave.editedCount);

        foreach (KeyValuePair<string, MemoryObject3D> pair in memoryObjects)
            ApplySavedState(pair.Value);

        SyncDecisionCountsFromRecords();
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
        PlayerMemoryLog3D.Ensure().ResetLog();

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
        record.decision = entry.restored ? entry.decision : MemoryDecision3D.Unchosen;
    }

    private void SyncDecisionCountsFromRecords()
    {
        int preserved = 0;
        int deleted = 0;
        int edited = 0;

        for (int i = 0; i < collectedMemories.Count; i++)
        {
            MemoryRecord3D record = collectedMemories[i];
            if (record == null || !record.restored)
                continue;

            switch (record.decision)
            {
                case MemoryDecision3D.Preserve:
                    preserved++;
                    break;
                case MemoryDecision3D.Delete:
                    deleted++;
                    break;
                case MemoryDecision3D.Edit:
                    edited++;
                    break;
            }
        }

        if (GameState3D.Instance != null)
            GameState3D.Instance.SetDecisionCounts(preserved, deleted, edited);
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
