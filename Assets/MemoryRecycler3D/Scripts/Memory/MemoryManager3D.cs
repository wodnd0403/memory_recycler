using System.Collections.Generic;
using UnityEngine;

public class MemoryManager3D : MonoBehaviour
{
    public static MemoryManager3D Instance { get; private set; }

    public readonly List<MemoryRecord3D> collectedMemories = new List<MemoryRecord3D>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

        UIManager3D.Instance.ShowMemoryCard(record);
    }

    public void MarkRestored(MemoryData3D memory)
    {
        MemoryRecord3D record = FindRecord(memory);
        if (record != null)
            record.restored = true;
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
        }

        UIManager3D.Instance.ShowToast($"기억 처리 완료: {DecisionToKorean(decision)}");
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
