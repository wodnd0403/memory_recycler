using UnityEngine;

public class GameState3D : MonoBehaviour
{
    public static GameState3D Instance { get; private set; }

    public int preservedCount;
    public int deletedCount;
    public int editedCount;

    public float worldToneValue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetState()
    {
        preservedCount = 0;
        deletedCount = 0;
        editedCount = 0;
        worldToneValue = 0f;
    }

    public void SetDecisionCounts(int preserved, int deleted, int edited)
    {
        preservedCount = Mathf.Max(0, preserved);
        deletedCount = Mathf.Max(0, deleted);
        editedCount = Mathf.Max(0, edited);
        worldToneValue = preservedCount - deletedCount + editedCount * 0.25f;
    }

    public void RecordDecision(MemoryDecision3D decision)
    {
        switch (decision)
        {
            case MemoryDecision3D.Preserve:
                preservedCount++;
                worldToneValue += 1f;
                break;
            case MemoryDecision3D.Delete:
                deletedCount++;
                worldToneValue -= 1f;
                break;
            case MemoryDecision3D.Edit:
                editedCount++;
                worldToneValue += 0.25f;
                break;
        }
    }
}
