using UnityEngine;

[CreateAssetMenu(fileName = "MemoryData3D", menuName = "Memory Recycler 3D/Memory Data")]
public class MemoryData3D : ScriptableObject
{
    [Header("Basic")]
    public string id;
    public string memoryTitle;
    [TextArea(3, 8)] public string description;
    public EmotionType3D emotion = EmotionType3D.Loss;
    [Range(0, 100)] public int corruptionLevel = 30;

    [Header("World Context")]
    public string locationName;
    [TextArea(2, 5)] public string archiveClue;

    [Header("Puzzle")]
    [TextArea(1, 3)] public string[] sentencePieces;
    [TextArea(1, 3)] public string[] correctOrder;

    [Header("Recovered Text")]
    [TextArea(3, 8)] public string restoredText;

    [Header("Player Memory Recycling")]
    [TextArea(1, 3)] public string puzzleHint;
    [TextArea(2, 5)] public string preserveTestimony;
    [TextArea(2, 5)] public string deleteTestimony;
    [TextArea(2, 5)] public string reprocessedText;
    [TextArea(1, 3)] public string reprocessWarning;
}
