using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerMemoryLog3D : MonoBehaviour
{
    public static PlayerMemoryLog3D Instance { get; private set; }

    private const string SaveKey = "MR3D_PlayerMemoryLog_v1";

    public MemoryDecision3D firstDecision = MemoryDecision3D.Unchosen;
    public string firstDecisionMemoryId = "";
    public string firstDecisionMemoryTitle = "";
    public int preservedCount;
    public int deletedCount;
    public int editedCount;
    public int puzzleMistakeCount;
    public int restoreAbandonCount;
    public float playTimeSeconds;
    public float archiveStaySeconds;
    public bool endingReached;

    private bool archiveStayActive;
    private float lastSaveTime;
    private readonly Dictionary<string, float> activeRestoreStarts = new Dictionary<string, float>();

    [Serializable]
    private class LogSaveData
    {
        public MemoryDecision3D firstDecision;
        public string firstDecisionMemoryId;
        public string firstDecisionMemoryTitle;
        public int preservedCount;
        public int deletedCount;
        public int editedCount;
        public int puzzleMistakeCount;
        public int restoreAbandonCount;
        public float playTimeSeconds;
        public float archiveStaySeconds;
        public bool endingReached;
        public List<PuzzleMistakeEntry> puzzleMistakes = new List<PuzzleMistakeEntry>();
    }

    [Serializable]
    private class PuzzleMistakeEntry
    {
        public string memoryId;
        public string memoryTitle;
        public int count;
        public int abandonCount;
        public float restoreTimeSeconds;
    }

    private readonly List<PuzzleMistakeEntry> puzzleMistakes = new List<PuzzleMistakeEntry>();

    public static PlayerMemoryLog3D Ensure()
    {
        if (Instance != null)
            return Instance;

        PlayerMemoryLog3D existing = FindFirstObjectByType<PlayerMemoryLog3D>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject host = new GameObject("MR3D_PlayerMemoryLog");
        return host.AddComponent<PlayerMemoryLog3D>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadLog();
    }

    private void Update()
    {
        if (endingReached)
            return;

        playTimeSeconds += Time.unscaledDeltaTime;
        if (archiveStayActive)
            archiveStaySeconds += Time.unscaledDeltaTime;

        if (Time.unscaledTime - lastSaveTime > 10f)
            SaveLog();
    }

    public void ResetLog()
    {
        firstDecision = MemoryDecision3D.Unchosen;
        firstDecisionMemoryId = "";
        firstDecisionMemoryTitle = "";
        preservedCount = 0;
        deletedCount = 0;
        editedCount = 0;
        puzzleMistakeCount = 0;
        restoreAbandonCount = 0;
        playTimeSeconds = 0f;
        archiveStaySeconds = 0f;
        endingReached = false;
        archiveStayActive = false;
        puzzleMistakes.Clear();
        activeRestoreStarts.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    public void RecordDecision(MemoryData3D memory, MemoryDecision3D decision)
    {
        if (decision == MemoryDecision3D.Unchosen)
            return;

        if (firstDecision == MemoryDecision3D.Unchosen)
        {
            firstDecision = decision;
            firstDecisionMemoryId = memory != null ? memory.id : "";
            firstDecisionMemoryTitle = memory != null ? memory.memoryTitle : "";
        }

        switch (decision)
        {
            case MemoryDecision3D.Preserve:
                preservedCount++;
                break;
            case MemoryDecision3D.Delete:
                deletedCount++;
                break;
            case MemoryDecision3D.Edit:
                editedCount++;
                break;
        }

        SaveLog();
    }

    public void RecordPuzzleMistake(MemoryData3D memory)
    {
        puzzleMistakeCount++;

        PuzzleMistakeEntry entry = EnsureMemoryEntry(memory);

        entry.count++;
        SaveLog();
    }

    public void BeginMemoryRestore(MemoryData3D memory)
    {
        if (memory == null || string.IsNullOrEmpty(memory.id))
            return;

        EnsureMemoryEntry(memory);
        activeRestoreStarts[memory.id] = Time.unscaledTime;
    }

    public void RecordRestoreAbandoned(MemoryData3D memory)
    {
        PuzzleMistakeEntry entry = EnsureMemoryEntry(memory);
        if (entry == null)
            return;

        entry.abandonCount++;
        restoreAbandonCount++;
        SaveLog();
    }

    public void MarkMemoryRestored(MemoryData3D memory)
    {
        if (memory == null || string.IsNullOrEmpty(memory.id))
            return;

        PuzzleMistakeEntry entry = EnsureMemoryEntry(memory);
        if (entry == null)
            return;

        if (activeRestoreStarts.TryGetValue(memory.id, out float startedAt))
        {
            entry.restoreTimeSeconds += Mathf.Max(0f, Time.unscaledTime - startedAt);
            activeRestoreStarts.Remove(memory.id);
            SaveLog();
        }
    }

    public void SetArchivePresence(bool inside)
    {
        archiveStayActive = inside;
        if (!inside)
            SaveLog();
    }

    public void MarkEndingReached()
    {
        endingReached = true;
        archiveStayActive = false;
        SaveLog();
    }

    public int GetPuzzleMistakesFor(MemoryData3D memory)
    {
        PuzzleMistakeEntry entry = FindPuzzleMistakeEntry(memory != null ? memory.id : "");
        return entry != null ? entry.count : 0;
    }

    public int GetRestoreAbandonsFor(MemoryData3D memory)
    {
        PuzzleMistakeEntry entry = FindPuzzleMistakeEntry(memory != null ? memory.id : "");
        return entry != null ? entry.abandonCount : 0;
    }

    public int GetInstabilityFor(MemoryData3D memory)
    {
        PuzzleMistakeEntry entry = FindPuzzleMistakeEntry(memory != null ? memory.id : "");
        return entry != null ? Mathf.Max(0, entry.count + entry.abandonCount) : 0;
    }

    public int GetTotalInstability()
    {
        int total = 0;
        for (int i = 0; i < puzzleMistakes.Count; i++)
            total += Mathf.Max(0, puzzleMistakes[i].count + puzzleMistakes[i].abandonCount);
        return total;
    }

    public string GetInstabilityLabel(MemoryData3D memory)
    {
        int instability = GetInstabilityFor(memory);
        if (instability <= 0)
            return "안정";
        if (instability <= 2)
            return "흔들림";
        return "붕괴 직전";
    }

    public string BuildBehaviorReport(List<MemoryRecord3D> records)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[수거원 행동 기록]");

        if (firstDecision == MemoryDecision3D.Unchosen)
        {
            sb.AppendLine("- 첫 번째 선택은 아직 기록되지 않았다.");
        }
        else
        {
            string title = string.IsNullOrEmpty(firstDecisionMemoryTitle) ? "첫 번째 기억" : firstDecisionMemoryTitle;
            sb.AppendLine("- 수거원은 첫 번째 기억 \"" + title + "\"을 " + MemoryManager3D.DecisionToKorean(firstDecision) + "했다.");
        }

        sb.AppendLine("- 보존 " + preservedCount + "회 / 삭제 " + deletedCount + "회 / 재가공 " + editedCount + "회");

        if (puzzleMistakeCount <= 0)
            sb.AppendLine("- 그는 모든 문장을 한 번에 복원했다.");
        else if (puzzleMistakeCount == 1)
            sb.AppendLine("- 그는 한 번 문장을 잘못 복원했다.");
        else
            sb.AppendLine("- 그는 같은 폐도시 안에서 문장을 " + puzzleMistakeCount + "번이나 잘못 복원했다.");

        if (archiveStaySeconds >= 20f)
            sb.AppendLine("- 아카이브는 그의 망설임까지 보존했다. 체류 시간 " + FormatDuration(archiveStaySeconds) + ".");
        else if (archiveStaySeconds > 0f)
            sb.AppendLine("- 그는 중앙 아카이브 곁에 " + FormatDuration(archiveStaySeconds) + " 머물렀다.");

        sb.AppendLine("- 엔딩 도달 시간: " + FormatDuration(playTimeSeconds));

        MemoryRecord3D painfulPreserved = FindPainfulPreservedMemory(records);
        if (painfulPreserved != null && painfulPreserved.memory != null)
            sb.AppendLine("- 그는 아픈 기억 \"" + painfulPreserved.memory.memoryTitle + "\"을 남기는 쪽을 택했다.");

        if (deletedCount > 0)
            sb.AppendLine("- 삭제된 기억은 증언대에서 침묵으로 남았다.");
        if (editedCount > 0)
            sb.AppendLine("- 재가공된 기억은 부드러워졌지만, 원본과 어긋난 흔적을 남겼다.");
        if (GetTotalInstability() <= 0)
            sb.AppendLine("- 그는 기억을 거의 훼손하지 않고 복원했다.");
        else
            sb.AppendLine("- 아카이브는 복원 과정의 흔들림 " + GetTotalInstability() + "개를 함께 보존했다.");
        if (preservedCount > 0 && deletedCount > 0 && editedCount > 0)
            sb.AppendLine("- 아카이브는 그가 한 가지 원칙이 아니라, 매번 다른 죄책감으로 판단했다는 사실을 보존했다.");
        sb.AppendLine("- " + BuildEndingBehaviorSentence());

        return sb.ToString();
    }

    public string BuildBehaviorSummaryReport()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[수거원 행동 기록]");

        if (firstDecision == MemoryDecision3D.Unchosen)
            sb.AppendLine("- 첫 선택: 아직 없음");
        else
            sb.AppendLine("- 첫 선택: \"" + firstDecisionMemoryTitle + "\" " + MemoryManager3D.DecisionToKorean(firstDecision));

        sb.AppendLine("- 선택 통계: 보존 " + preservedCount + " / 삭제 " + deletedCount + " / 재가공 " + editedCount);
        sb.AppendLine("- 복원 실패: " + puzzleMistakeCount + "회 / 중단 " + restoreAbandonCount + "회");
        sb.AppendLine("- 아카이브 앞 체류: " + FormatDuration(archiveStaySeconds));
        sb.AppendLine("- " + BuildEndingBehaviorSentence());

        return sb.ToString();
    }

    public static string FormatDuration(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        int minutes = total / 60;
        int remainder = total % 60;
        return minutes + "분 " + remainder + "초";
    }

    public void SaveLog()
    {
        LogSaveData save = new LogSaveData
        {
            firstDecision = firstDecision,
            firstDecisionMemoryId = firstDecisionMemoryId,
            firstDecisionMemoryTitle = firstDecisionMemoryTitle,
            preservedCount = preservedCount,
            deletedCount = deletedCount,
            editedCount = editedCount,
            puzzleMistakeCount = puzzleMistakeCount,
            restoreAbandonCount = restoreAbandonCount,
            playTimeSeconds = playTimeSeconds,
            archiveStaySeconds = archiveStaySeconds,
            endingReached = endingReached,
            puzzleMistakes = new List<PuzzleMistakeEntry>(puzzleMistakes)
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
        lastSaveTime = Time.unscaledTime;
    }

    public void LoadLog()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
            return;

        LogSaveData save = JsonUtility.FromJson<LogSaveData>(PlayerPrefs.GetString(SaveKey));
        if (save == null)
            return;

        firstDecision = save.firstDecision;
        firstDecisionMemoryId = save.firstDecisionMemoryId ?? "";
        firstDecisionMemoryTitle = save.firstDecisionMemoryTitle ?? "";
        preservedCount = Mathf.Max(0, save.preservedCount);
        deletedCount = Mathf.Max(0, save.deletedCount);
        editedCount = Mathf.Max(0, save.editedCount);
        puzzleMistakeCount = Mathf.Max(0, save.puzzleMistakeCount);
        restoreAbandonCount = Mathf.Max(0, save.restoreAbandonCount);
        playTimeSeconds = Mathf.Max(0f, save.playTimeSeconds);
        archiveStaySeconds = Mathf.Max(0f, save.archiveStaySeconds);
        endingReached = save.endingReached;
        archiveStayActive = false;
        activeRestoreStarts.Clear();

        puzzleMistakes.Clear();
        if (save.puzzleMistakes != null)
            puzzleMistakes.AddRange(save.puzzleMistakes);
    }

    private PuzzleMistakeEntry EnsureMemoryEntry(MemoryData3D memory)
    {
        string id = memory != null ? memory.id : "";
        if (string.IsNullOrEmpty(id))
            return null;

        PuzzleMistakeEntry entry = FindPuzzleMistakeEntry(id);
        if (entry == null)
        {
            entry = new PuzzleMistakeEntry
            {
                memoryId = id,
                memoryTitle = memory != null ? memory.memoryTitle : "이름 없는 기억",
                count = 0,
                abandonCount = 0,
                restoreTimeSeconds = 0f
            };
            puzzleMistakes.Add(entry);
        }

        return entry;
    }

    private PuzzleMistakeEntry FindPuzzleMistakeEntry(string memoryId)
    {
        for (int i = 0; i < puzzleMistakes.Count; i++)
        {
            if (puzzleMistakes[i].memoryId == memoryId)
                return puzzleMistakes[i];
        }

        return null;
    }

    private string BuildEndingBehaviorSentence()
    {
        if (archiveStaySeconds >= 20f)
            return "그는 아카이브 앞에서 오래 멈춰 있었다. 그 망설임은 어떤 기억보다 선명하게 남았다.";
        if (puzzleMistakeCount >= 3)
            return "그는 문장을 여러 번 잘못 놓았다. 아카이브는 그 실수마저 복원 과정으로 보관했다.";

        bool preserveWins = preservedCount > deletedCount && preservedCount > editedCount;
        bool deleteWins = deletedCount > preservedCount && deletedCount > editedCount;
        bool editWins = editedCount > preservedCount && editedCount > deletedCount;

        if (preserveWins)
            return "아카이브는 도시의 상처를 닫지 않았다. 대신 수거원이 그것을 남겼다는 사실을 보존했다.";
        if (deleteWins)
            return "도시는 조용해졌다. 그러나 아카이브는 지워진 기억보다 지우려던 손길을 먼저 기록했다.";
        if (editWins)
            return "도시는 견딜 수 있는 이야기로 바뀌었다. 다만 아카이브는 그 이야기가 원본이 아니었음을 기억한다.";

        if (preservedCount + deletedCount + editedCount > 0)
            return "수거원은 어느 하나의 답도 선택하지 않았다. 그래서 아카이브는 그의 망설임까지 분류했다.";

        return "그는 기억을 처리하러 왔지만, 마지막으로 재활용된 것은 그의 선택이었다.";
    }

    private MemoryRecord3D FindPainfulPreservedMemory(List<MemoryRecord3D> records)
    {
        if (records == null)
            return null;

        for (int i = 0; i < records.Count; i++)
        {
            MemoryRecord3D record = records[i];
            if (record == null || record.memory == null || record.decision != MemoryDecision3D.Preserve)
                continue;

            if (record.memory.emotion == EmotionType3D.Loss || record.memory.emotion == EmotionType3D.Fear)
                return record;
        }

        return null;
    }
}
