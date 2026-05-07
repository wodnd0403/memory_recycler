using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager3D : MonoBehaviour
{
    public static UIManager3D Instance { get; private set; }

    private Canvas canvas;
    private Font uiFont;

    private GameObject promptPanel;
    private Text promptText;

    private GameObject toastPanel;
    private Text toastText;
    private float toastUntil;

    private GameObject timePanel;
    private Text timeText;

    private GameObject cardPanel;
    private Text cardTitle;
    private Text cardBody;
    private Button restoreButton;
    private Button preserveButton;
    private Button deleteButton;
    private Button editButton;
    private Button closeCardButton;

    private GameObject puzzlePanel;
    private Text puzzleTitle;
    private Text puzzleSelectedText;
    private Transform puzzlePiecesRoot;
    private readonly List<string> selectedPieces = new List<string>();
    private MemoryRecord3D currentPuzzleRecord;

    private GameObject archivePanel;
    private Text archiveText;

    private GameObject endingPanel;
    private Text endingTitle;
    private Text endingBody;

    private MemoryRecord3D currentRecord;

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
        BuildUI();
    }

    private void Update()
    {
        if (toastPanel != null && toastPanel.activeSelf && Time.time > toastUntil)
            toastPanel.SetActive(false);

        UpdateTimeDisplay();
    }

    public void ShowPrompt(string message)
    {
        if (promptText == null)
            return;

        promptText.text = message;
        promptPanel.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    public void ShowToast(string message)
    {
        if (toastText == null)
            return;

        toastText.text = message;
        toastPanel.SetActive(true);
        toastUntil = Time.time + 2.2f;
    }

    public void ShowMemoryCard(MemoryRecord3D record)
    {
        currentRecord = record;
        CloseAllMajorPanels();

        string decision = MemoryManager3D.DecisionToKorean(record.decision);
        cardTitle.text = record.memory.memoryTitle;
        cardBody.text =
            $"감정 태그: {record.memory.emotion}\n" +
            $"손상도: {record.memory.corruptionLevel}%\n" +
            $"처리 상태: {decision}\n\n" +
            record.memory.description +
            (record.restored ? "\n\n[복원된 기억]\n" + record.memory.restoredText : "\n\n아직 복원되지 않은 기억입니다.");

        restoreButton.gameObject.SetActive(!record.restored);
        preserveButton.gameObject.SetActive(record.restored && record.decision == MemoryDecision3D.Unchosen);
        deleteButton.gameObject.SetActive(record.restored && record.decision == MemoryDecision3D.Unchosen);
        editButton.gameObject.SetActive(record.restored && record.decision == MemoryDecision3D.Unchosen);

        cardPanel.SetActive(true);
        UnlockCursor();
    }

    public void ToggleArchive()
    {
        if (archivePanel.activeSelf)
        {
            archivePanel.SetActive(false);
            LockCursor();
            return;
        }

        ShowArchive();
    }

    public void ShowArchive()
    {
        CloseAllMajorPanels();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("중앙 아카이브 - 회수된 기억 목록");
        sb.AppendLine("--------------------------------");

        List<MemoryRecord3D> records = MemoryManager3D.Instance.collectedMemories;
        if (records.Count == 0)
        {
            sb.AppendLine("아직 회수된 기억이 없습니다.");
        }
        else
        {
            for (int i = 0; i < records.Count; i++)
            {
                MemoryRecord3D record = records[i];
                sb.AppendLine($"{i + 1}. {record.memory.memoryTitle}");
                sb.AppendLine($"   감정: {record.memory.emotion} / 복원: {(record.restored ? "완료" : "미완료")} / 처리: {MemoryManager3D.DecisionToKorean(record.decision)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("아카이브 단말기에 접근해 E를 누르면 현재 선택을 기준으로 엔딩을 확인합니다.");

        archiveText.text = sb.ToString();
        archivePanel.SetActive(true);
        UnlockCursor();
    }

    public void ShowEnding()
    {
        CloseAllMajorPanels();

        int preserved = GameState3D.Instance.preservedCount;
        int deleted = GameState3D.Instance.deletedCount;
        int edited = GameState3D.Instance.editedCount;
        int decided = MemoryManager3D.Instance.CountDecidedMemories();

        string title;
        string body;

        if (decided < 3)
        {
            title = "미완성 아카이브";
            body = "도시는 아직 충분한 기억을 되찾지 못했습니다. 더 많은 기억을 회수하고 복원해야 중앙 아카이브의 결론에 도달할 수 있습니다.";
        }
        else if (preserved >= deleted && preserved >= edited)
        {
            title = "복원 엔딩";
            body = "플레이어는 고통스러운 진실까지 보존하기로 결정했습니다. 도시는 혼란을 마주하지만, 처음으로 자기 자신의 과거를 다시 말할 수 있게 됩니다.";
        }
        else if (deleted > preserved && deleted >= edited)
        {
            title = "정적 엔딩";
            body = "플레이어는 대부분의 기억을 삭제했습니다. 도시는 평온해 보이지만, 그 평온은 아무것도 남지 않은 침묵에 가깝습니다.";
        }
        else
        {
            title = "편집 엔딩";
            body = "플레이어는 기억을 있는 그대로 남기지 않고 재가공했습니다. 도시는 새로운 질서를 얻지만, 그것이 진실인지 위로인지는 아무도 확신하지 못합니다.";
        }

        endingTitle.text = title;
        endingBody.text = body + $"\n\n[선택 통계]\n보존: {preserved} / 삭제: {deleted} / 재가공: {edited}";
        endingPanel.SetActive(true);
        UnlockCursor();
    }

    private void BuildUI()
    {
        EnsureEventSystem();
        uiFont = Font.CreateDynamicFontFromOSFont("Arial", 18);

        GameObject canvasObject = new GameObject("MemoryRecycler3D_Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObject.AddComponent<GraphicRaycaster>();

        promptPanel = CreatePanel("PromptPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(620f, 70f), new Vector2(0f, 80f), new Color(0f, 0f, 0f, 0.65f));
        promptText = CreateText(promptPanel.transform, "PromptText", "E : 상호작용", 28, TextAnchor.MiddleCenter, Color.white);
        Stretch(promptText.rectTransform);
        promptPanel.SetActive(false);

        toastPanel = CreatePanel("ToastPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(700f, 70f), new Vector2(0f, -90f), new Color(0.05f, 0.07f, 0.1f, 0.8f));
        toastText = CreateText(toastPanel.transform, "ToastText", "", 24, TextAnchor.MiddleCenter, Color.white);
        Stretch(toastText.rectTransform);
        toastPanel.SetActive(false);

        timePanel = CreatePanel("TimePanel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(290f, 70f), new Vector2(-28f, -28f), new Color(0.03f, 0.04f, 0.06f, 0.72f));
        timeText = CreateText(timePanel.transform, "TimeText", "시간대: 낮", 22, TextAnchor.MiddleCenter, Color.white);
        Stretch(timeText.rectTransform);

        BuildCardPanel();
        BuildPuzzlePanel();
        BuildArchivePanel();
        BuildEndingPanel();
    }

    private void UpdateTimeDisplay()
    {
        if (timeText == null)
            return;

        if (WorldToneController3D.Instance == null)
        {
            timeText.text = "시간대: 확인 중";
            return;
        }

        timeText.text = "시간대: " + WorldToneController3D.Instance.GetTimePeriodLabel() + "  |  " + WorldToneController3D.Instance.GetClockString();
    }

    private void BuildCardPanel()
    {
        cardPanel = CreatePanel("MemoryCardPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(860f, 650f), Vector2.zero, new Color(0.04f, 0.05f, 0.07f, 0.94f));

        cardTitle = CreateText(cardPanel.transform, "CardTitle", "기억", 36, TextAnchor.MiddleLeft, Color.white);
        SetRect(cardTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, -35f), new Vector2(40f, 90f));

        cardBody = CreateText(cardPanel.transform, "CardBody", "", 23, TextAnchor.UpperLeft, new Color(0.9f, 0.92f, 0.95f));
        SetRect(cardBody.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-40f, 25f), new Vector2(40f, -115f));

        restoreButton = CreateButton(cardPanel.transform, "RestoreButton", "기억 복원", new Vector2(-270f, -270f), () => StartRestoreCurrent());
        preserveButton = CreateButton(cardPanel.transform, "PreserveButton", "보존", new Vector2(-270f, -270f), () => DecideCurrent(MemoryDecision3D.Preserve));
        deleteButton = CreateButton(cardPanel.transform, "DeleteButton", "삭제", new Vector2(0f, -270f), () => DecideCurrent(MemoryDecision3D.Delete));
        editButton = CreateButton(cardPanel.transform, "EditButton", "재가공", new Vector2(270f, -270f), () => DecideCurrent(MemoryDecision3D.Edit));
        closeCardButton = CreateButton(cardPanel.transform, "CloseCardButton", "닫기", new Vector2(0f, -335f), CloseAllAndLock);

        cardPanel.SetActive(false);
    }

    private void BuildPuzzlePanel()
    {
        puzzlePanel = CreatePanel("PuzzlePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(980f, 720f), Vector2.zero, new Color(0.03f, 0.035f, 0.05f, 0.96f));

        puzzleTitle = CreateText(puzzlePanel.transform, "PuzzleTitle", "기억 복원 퍼즐", 34, TextAnchor.MiddleLeft, Color.white);
        SetRect(puzzleTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, -35f), new Vector2(40f, 85f));

        Text guide = CreateText(puzzlePanel.transform, "PuzzleGuide", "문장 조각을 올바른 순서로 선택한 뒤 확인을 누르세요.", 22, TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.9f));
        SetRect(guide.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, -100f), new Vector2(40f, 130f));

        puzzleSelectedText = CreateText(puzzlePanel.transform, "SelectedText", "선택된 문장: ", 22, TextAnchor.UpperLeft, new Color(0.95f, 0.95f, 0.85f));
        SetRect(puzzleSelectedText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-40f, 160f), new Vector2(40f, 310f));

        GameObject root = new GameObject("PuzzlePiecesRoot");
        root.transform.SetParent(puzzlePanel.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        SetRect(rootRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40f, -90f), new Vector2(40f, 115f));
        puzzlePiecesRoot = root.transform;

        CreateButton(puzzlePanel.transform, "CheckPuzzleButton", "복원 확인", new Vector2(-260f, -300f), CheckPuzzle);
        CreateButton(puzzlePanel.transform, "ResetPuzzleButton", "다시 선택", new Vector2(0f, -300f), ResetPuzzleSelection);
        CreateButton(puzzlePanel.transform, "ClosePuzzleButton", "닫기", new Vector2(260f, -300f), CloseAllAndLock);

        puzzlePanel.SetActive(false);
    }

    private void BuildArchivePanel()
    {
        archivePanel = CreatePanel("ArchivePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(980f, 720f), Vector2.zero, new Color(0.04f, 0.04f, 0.055f, 0.96f));
        archiveText = CreateText(archivePanel.transform, "ArchiveText", "", 22, TextAnchor.UpperLeft, Color.white);
        SetRect(archiveText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-40f, 80f), new Vector2(40f, -40f));
        CreateButton(archivePanel.transform, "CloseArchiveButton", "닫기", new Vector2(0f, -325f), CloseAllAndLock);
        archivePanel.SetActive(false);
    }

    private void BuildEndingPanel()
    {
        endingPanel = CreatePanel("EndingPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 580f), Vector2.zero, new Color(0.03f, 0.03f, 0.045f, 0.97f));
        endingTitle = CreateText(endingPanel.transform, "EndingTitle", "엔딩", 40, TextAnchor.MiddleCenter, Color.white);
        SetRect(endingTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, -45f), new Vector2(-40f, 110f));
        endingBody = CreateText(endingPanel.transform, "EndingBody", "", 24, TextAnchor.UpperLeft, new Color(0.9f, 0.92f, 0.95f));
        SetRect(endingBody.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-40f, 95f), new Vector2(40f, -130f));
        CreateButton(endingPanel.transform, "CloseEndingButton", "닫기", new Vector2(0f, -260f), CloseAllAndLock);
        endingPanel.SetActive(false);
    }

    private void StartRestoreCurrent()
    {
        if (currentRecord == null || currentRecord.memory == null)
            return;

        if (currentRecord.memory.sentencePieces == null || currentRecord.memory.sentencePieces.Length == 0)
        {
            currentRecord.restored = true;
            ShowMemoryCard(currentRecord);
            return;
        }

        currentPuzzleRecord = currentRecord;
        selectedPieces.Clear();
        CloseAllMajorPanels();
        puzzleTitle.text = "기억 복원: " + currentPuzzleRecord.memory.memoryTitle;
        RefreshPuzzlePieces();
        RefreshSelectedPiecesText();
        puzzlePanel.SetActive(true);
        UnlockCursor();
    }

    private void RefreshPuzzlePieces()
    {
        foreach (Transform child in puzzlePiecesRoot)
            Destroy(child.gameObject);

        string[] pieces = currentPuzzleRecord.memory.sentencePieces;
        for (int i = 0; i < pieces.Length; i++)
        {
            string piece = pieces[i];
            Button button = CreateButton(puzzlePiecesRoot, "Piece_" + i, piece, Vector2.zero, () => SelectPuzzlePiece(piece));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 58f);
            rect.anchoredPosition = new Vector2(0f, -i * 68f);
        }
    }

    private void SelectPuzzlePiece(string piece)
    {
        selectedPieces.Add(piece);
        RefreshSelectedPiecesText();
    }

    private void RefreshSelectedPiecesText()
    {
        puzzleSelectedText.text = "선택된 문장:\n" + string.Join(" / ", selectedPieces.ToArray());
    }

    private void ResetPuzzleSelection()
    {
        selectedPieces.Clear();
        RefreshSelectedPiecesText();
    }

    private void CheckPuzzle()
    {
        if (currentPuzzleRecord == null || currentPuzzleRecord.memory == null)
            return;

        string[] correctOrder = currentPuzzleRecord.memory.correctOrder;
        bool solved = selectedPieces.Count == correctOrder.Length;

        if (solved)
        {
            for (int i = 0; i < correctOrder.Length; i++)
            {
                if (selectedPieces[i] != correctOrder[i])
                {
                    solved = false;
                    break;
                }
            }
        }

        if (solved)
        {
            MemoryManager3D.Instance.MarkRestored(currentPuzzleRecord.memory);
            ShowToast("기억이 복원되었습니다.");
            ShowMemoryCard(currentPuzzleRecord);
        }
        else
        {
            ShowToast("순서가 맞지 않습니다. 다시 조합해보세요.");
        }
    }

    private void DecideCurrent(MemoryDecision3D decision)
    {
        if (currentRecord == null)
            return;

        MemoryManager3D.Instance.ApplyDecision(currentRecord.memory, decision);
        ShowMemoryCard(currentRecord);
    }

    private void CloseAllMajorPanels()
    {
        if (cardPanel != null) cardPanel.SetActive(false);
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        if (archivePanel != null) archivePanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);
    }

    private void CloseAllAndLock()
    {
        CloseAllMajorPanels();
        LockCursor();
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return go;
    }

    private Text CreateText(Transform parent, string name, string text, int size, TextAnchor anchor, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text uiText = go.AddComponent<Text>();
        uiText.font = uiFont;
        uiText.text = text;
        uiText.fontSize = size;
        uiText.alignment = anchor;
        uiText.color = color;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        return uiText;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.3f, 0.95f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(230f, 58f);
        rect.anchoredPosition = anchoredPosition;

        Text text = CreateText(go.transform, "Text", label, 22, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);

        return button;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
