using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager3D : MonoBehaviour
{
    public static UIManager3D Instance { get; private set; }

    // 제출 시연용 엔딩 진입 임계값. Docs/07 §11 — 8개 중 5개 처리 시 엔딩 허용.
    public const int RequiredDecisionsForEnding = 5;

    private Canvas canvas;
    private Font uiFont;

    private GameObject objectivePanel;
    private Text objectiveText;
    private GameObject promptPanel;
    private Text promptText;
    private GameObject toastPanel;
    private Text toastText;
    private float toastUntil;
    private GameObject timePanel;
    private Text timeText;
    private GameObject startMenuPanel;
    private Button continueButton;

    private GameObject cardPanel;
    private Text cardTitle;
    private Text cardBody;
    private Button restoreButton;
    private Button preserveButton;
    private Button deleteButton;
    private Button editButton;

    private GameObject puzzlePanel;
    private Text puzzleTitle;
    private Text puzzleSelectedText;
    private Transform puzzlePiecesRoot;
    private readonly List<string> selectedPieces = new List<string>();
    // 퍼즐 조각 인덱스별 버튼을 저장해 중복 클릭 방지에 사용한다.
    private readonly List<Button> puzzlePieceButtons = new List<Button>();
    private readonly List<int> selectedPieceIndices = new List<int>();
    private MemoryRecord3D currentPuzzleRecord;

    private GameObject archivePanel;
    private Text archiveText;
    private GameObject lorePanel;
    private Text loreTitle;
    private Text loreBody;
    private GameObject echoPanel;
    private Text echoTitle;
    private Text echoBody;
    private MemoryRecord3D currentEchoRecord;
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
        ShowStartMenu();
    }

    private void Update()
    {
        if (toastPanel != null && toastPanel.activeSelf && Time.time > toastUntil)
            toastPanel.SetActive(false);

        UpdateTimeDisplay();
        UpdateObjectiveDisplay();
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
        toastUntil = Time.time + 2.4f;
    }

    // 시작 메뉴 또는 주요 패널 중 하나라도 열려 있으면 게임플레이 입력을 차단한다.
    public bool IsGameplayInputBlocked()
    {
        if (startMenuPanel != null && startMenuPanel.activeSelf)
            return true;

        return IsAnyMajorPanelOpen();
    }

    // 기억 카드/퍼즐/아카이브/도시 기록/복원 에코/엔딩 패널 중 하나라도 켜져 있는지 확인.
    public bool IsAnyMajorPanelOpen()
    {
        if (cardPanel != null && cardPanel.activeSelf) return true;
        if (puzzlePanel != null && puzzlePanel.activeSelf) return true;
        if (archivePanel != null && archivePanel.activeSelf) return true;
        if (lorePanel != null && lorePanel.activeSelf) return true;
        if (echoPanel != null && echoPanel.activeSelf) return true;
        if (endingPanel != null && endingPanel.activeSelf) return true;
        return false;
    }

    public void ShowMemoryCard(MemoryRecord3D record)
    {
        currentRecord = record;
        CloseAllMajorPanels();

        string decision = MemoryManager3D.DecisionToKorean(record.decision);
        string location = string.IsNullOrEmpty(record.memory.locationName) ? "위치 미상" : record.memory.locationName;
        string clue = string.IsNullOrEmpty(record.memory.archiveClue) ? "" : "\n\n[아카이브 단서]\n" + record.memory.archiveClue;

        cardTitle.text = record.memory.memoryTitle;
        cardBody.text =
            "위치: " + location + "\n" +
            "감정 태그: " + record.memory.emotion + "\n" +
            "손상률: " + record.memory.corruptionLevel + "%\n" +
            "처리 상태: " + decision + "\n\n" +
            record.memory.description +
            (record.restored ? "\n\n[복원된 기억]\n" + record.memory.restoredText + clue + GetDecisionGuideText(record) : "\n\n아직 복원되지 않은 기억입니다. 문장 조각을 맞춰 원문을 복구하세요.");

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
        sb.AppendLine("중앙 아카이브 - 회수 기록");
        sb.AppendLine("--------------------------------");

        List<MemoryRecord3D> records = MemoryManager3D.Instance != null
            ? MemoryManager3D.Instance.collectedMemories
            : new List<MemoryRecord3D>();
        if (records.Count == 0)
        {
            sb.AppendLine("아직 회수한 기억이 없습니다.");
        }
        else
        {
            for (int i = 0; i < records.Count; i++)
            {
                MemoryRecord3D record = records[i];
                string location = string.IsNullOrEmpty(record.memory.locationName) ? "위치 미상" : record.memory.locationName;
                sb.AppendLine((i + 1) + ". " + record.memory.memoryTitle);
                sb.AppendLine("   위치: " + location);
                sb.AppendLine("   감정: " + record.memory.emotion + " / 복원: " + (record.restored ? "완료" : "미완료") + " / 처리: " + MemoryManager3D.DecisionToKorean(record.decision));
            }
        }

        sb.AppendLine();
        sb.AppendLine("중앙 아카이브 결말 접속 조건: 처리 완료 기억 " + RequiredDecisionsForEnding + "개 이상");

        archiveText.text = sb.ToString();
        archivePanel.SetActive(true);
        UnlockCursor();
    }

    public void ShowArchiveLocked(int decided, int required)
    {
        CloseAllMajorPanels();
        if (MemoryAudio3D.Instance != null)
            MemoryAudio3D.Instance.PlayArchiveDenied();

        archiveText.text =
            "중앙 아카이브가 아직 열리지 않았습니다.\n\n" +
            "아카이브는 단순한 저장고가 아니라, 도시 전체의 기억 분류 시스템입니다.\n" +
            "충분한 기억을 복원하고 보존/삭제/재가공 중 하나로 처리해야 마지막 판단을 계산할 수 있습니다.\n\n" +
            "현재 처리 완료: " + decided + " / " + required + "\n\n" +
            "푸른 기억 구체를 더 회수하고, 복원 퍼즐을 완료한 뒤 선택을 내려 주세요.";

        archivePanel.SetActive(true);
        UnlockCursor();
    }

    public void ShowLore(string title, string body, string objectiveHint)
    {
        CloseAllMajorPanels();
        if (MemoryAudio3D.Instance != null)
            MemoryAudio3D.Instance.PlayLore();

        loreTitle.text = title;
        loreBody.text = body + (string.IsNullOrEmpty(objectiveHint) ? "" : "\n\n[탐사 힌트]\n" + objectiveHint);
        lorePanel.SetActive(true);
        UnlockCursor();
    }

    public void ShowMemoryEcho(MemoryRecord3D record)
    {
        if (record == null || record.memory == null)
            return;

        CloseAllMajorPanels();
        currentEchoRecord = record;

        string location = string.IsNullOrEmpty(record.memory.locationName) ? "위치 미상" : record.memory.locationName;
        string clue = string.IsNullOrEmpty(record.memory.archiveClue) ? "아카이브 단서가 아직 안정화되지 않았습니다." : record.memory.archiveClue;
        echoTitle.text = "기억 동기화: " + record.memory.memoryTitle;
        echoBody.text =
            "위치 신호: " + location + "\n" +
            "감정 잔향: " + record.memory.emotion + "\n\n" +
            record.memory.restoredText + "\n\n" +
            "[아카이브 반응]\n" + clue;

        if (MemoryAudio3D.Instance != null)
            MemoryAudio3D.Instance.PlayMemoryRestored();

        echoPanel.SetActive(true);
        UnlockCursor();
    }

    public void ShowEnding()
    {
        CloseAllMajorPanels();
        if (MemoryAudio3D.Instance != null)
            MemoryAudio3D.Instance.PlayArchiveOpen();

        int preserved = GameState3D.Instance != null ? GameState3D.Instance.preservedCount : 0;
        int deleted = GameState3D.Instance != null ? GameState3D.Instance.deletedCount : 0;
        int edited = GameState3D.Instance != null ? GameState3D.Instance.editedCount : 0;
        int decided = MemoryManager3D.Instance != null ? MemoryManager3D.Instance.CountDecidedMemories() : 0;

        string title;
        string body;

        if (decided < RequiredDecisionsForEnding)
        {
            title = "미완성 아카이브";
            body = "중앙 아카이브는 아직 충분한 판단 기록을 확보하지 못했습니다. 더 많은 기억을 복원하고 처리해야 최종 결론에 도달할 수 있습니다.";
        }
        else if (preserved >= deleted && preserved >= edited)
        {
            title = "복원 엔딩";
            body = "당신은 고통스러운 기억까지 보존하기로 했습니다. 도시는 다시 아프겠지만, 처음으로 자신이 무엇을 잃었는지 말할 수 있게 됩니다.";
        }
        else if (deleted > preserved && deleted >= edited)
        {
            title = "정적 엔딩";
            body = "당신은 대부분의 기억을 삭제했습니다. 도시는 조용해졌지만, 그 평온은 누구의 이름도 부르지 못하는 침묵에 가깝습니다.";
        }
        else
        {
            title = "재가공 엔딩";
            body = "당신은 기억을 있는 그대로 남기지 않고 새 질서로 편집했습니다. 도시는 움직이기 시작하지만, 그 안의 진실이 누구의 것인지는 불분명합니다.";
        }

        endingTitle.text = title;
        endingBody.text = body + "\n\n[선택 통계]\n보존: " + preserved + " / 삭제: " + deleted + " / 재가공: " + edited;
        endingPanel.SetActive(true);
        UnlockCursor();
    }

    private void BuildUI()
    {
        EnsureEventSystem();
        uiFont = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 32);
        if (uiFont == null)
            uiFont = Font.CreateDynamicFontFromOSFont("Arial", 32);

        GameObject canvasObject = new GameObject("MemoryRecycler3D_Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        objectivePanel = CreatePanel("ObjectivePanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(610f, 168f), new Vector2(28f, -28f), new Color(0.025f, 0.032f, 0.045f, 0.82f));
        objectiveText = CreateText(objectivePanel.transform, "ObjectiveText", "", 22, TextAnchor.UpperLeft, new Color(0.94f, 0.97f, 1f));
        SetRect(objectiveText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20f, 16f), new Vector2(-20f, -16f));

        promptPanel = CreatePanel("PromptPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(760f, 72f), new Vector2(0f, 80f), new Color(0f, 0f, 0f, 0.68f));
        promptText = CreateText(promptPanel.transform, "PromptText", "E : 상호작용", 28, TextAnchor.MiddleCenter, Color.white);
        Stretch(promptText.rectTransform);
        promptPanel.SetActive(false);

        toastPanel = CreatePanel("ToastPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(760f, 70f), new Vector2(0f, -96f), new Color(0.05f, 0.07f, 0.1f, 0.84f));
        toastText = CreateText(toastPanel.transform, "ToastText", "", 24, TextAnchor.MiddleCenter, Color.white);
        Stretch(toastText.rectTransform);
        toastPanel.SetActive(false);

        timePanel = CreatePanel("TimePanel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(290f, 70f), new Vector2(-28f, -28f), new Color(0.03f, 0.04f, 0.06f, 0.72f));
        timeText = CreateText(timePanel.transform, "TimeText", "시간대: 낮", 22, TextAnchor.MiddleCenter, Color.white);
        Stretch(timeText.rectTransform);

        BuildCardPanel();
        BuildPuzzlePanel();
        BuildArchivePanel();
        BuildLorePanel();
        BuildEchoPanel();
        BuildEndingPanel();
        BuildStartMenu();
    }

    private void BuildStartMenu()
    {
        startMenuPanel = CreatePanel("StartMenuPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.005f, 0.011f, 0.018f, 0.94f));
        RectTransform panelRect = startMenuPanel.GetComponent<RectTransform>();
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Text title = CreateText(startMenuPanel.transform, "StartTitle", "Memory Recycler 3D", 54, TextAnchor.MiddleCenter, new Color(0.88f, 0.96f, 1f));
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, 140f), new Vector2(520f, 225f));

        Text body = CreateText(startMenuPanel.transform, "StartBody", "폐허가 된 도시에서 푸른 기억 구체를 회수하고, 중앙 아카이브에서 기억의 운명을 선택하세요.", 24, TextAnchor.MiddleCenter, new Color(0.76f, 0.84f, 0.9f));
        SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-560f, 58f), new Vector2(560f, 128f));

        Button newGameButton = CreateButton(startMenuPanel.transform, "NewGameButton", "처음부터 시작", new Vector2(-145f, -20f), StartFreshGame);
        RectTransform newRect = newGameButton.GetComponent<RectTransform>();
        newRect.sizeDelta = new Vector2(270f, 64f);

        continueButton = CreateButton(startMenuPanel.transform, "ContinueGameButton", "저장된 시점부터", new Vector2(145f, -20f), ContinueSavedGame);
        RectTransform continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.sizeDelta = new Vector2(270f, 64f);

        Text hint = CreateText(startMenuPanel.transform, "StartHint", "WASD 이동 / Shift 달리기 / E 상호작용 / Tab 아카이브", 20, TextAnchor.MiddleCenter, new Color(0.62f, 0.70f, 0.76f));
        SetRect(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -130f), new Vector2(520f, -80f));

        startMenuPanel.SetActive(false);
    }

    private void ShowStartMenu()
    {
        if (startMenuPanel == null)
            return;

        bool hasSave = MemoryManager3D.HasSaveGame();
        continueButton.interactable = hasSave;
        Image continueImage = continueButton.GetComponent<Image>();
        if (continueImage != null)
            continueImage.color = hasSave ? new Color(0.16f, 0.21f, 0.29f, 0.96f) : new Color(0.08f, 0.10f, 0.13f, 0.82f);

        startMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        UnlockCursor();
    }

    private void HideStartMenu()
    {
        if (startMenuPanel != null)
            startMenuPanel.SetActive(false);

        Time.timeScale = 1f;
        LockCursor();
    }

    private void StartFreshGame()
    {
        if (MemoryManager3D.Instance != null)
            MemoryManager3D.Instance.StartNewGame();

        HideStartMenu();
        ShowToast("새 탐사를 시작합니다.");
    }

    private void ContinueSavedGame()
    {
        if (!MemoryManager3D.HasSaveGame())
        {
            ShowToast("저장된 진행이 없습니다.");
            return;
        }

        if (MemoryManager3D.Instance != null)
            MemoryManager3D.Instance.ContinueSavedGame();

        HideStartMenu();
        ShowToast("저장된 시점부터 이어합니다.");
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

    private void UpdateObjectiveDisplay()
    {
        if (objectiveText == null || MemoryManager3D.Instance == null)
            return;

        int collected = MemoryManager3D.Instance.CountCollectedMemories();
        int restored = MemoryManager3D.Instance.CountRestoredMemories();
        int decided = MemoryManager3D.Instance.CountDecidedMemories();
        int known = MemoryManager3D.Instance.CountKnownMemories();

        // 단계별 안내 임계값. 회수/복원은 시연용 가이드 수치이므로 엔딩 임계값과 분리해 둔다.
        const int CollectGuide = 3;
        const int RestoreGuide = 3;

        string next;
        if (collected < CollectGuide)
            next = "푸른 기억 구체를 찾아 최소 " + CollectGuide + "개 회수";
        else if (restored < RestoreGuide)
            next = "회수한 기억의 문장 조각을 복원";
        else if (decided < RequiredDecisionsForEnding)
            next = "복원된 기억을 보존/삭제/재가공으로 처리";
        else
            next = "중앙 아카이브 탑으로 이동해 결말 확인";

        objectiveText.text =
            "현재 목표\n" +
            next + "\n\n" +
            "회수 " + collected + " / " + known + "   복원 " + restored + "   처리 " + decided + " / " + RequiredDecisionsForEnding + "\n" +
            "Tab 아카이브  |  E 상호작용";
    }

    private void BuildCardPanel()
    {
        cardPanel = CreatePanel("MemoryCardPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 660f), Vector2.zero, new Color(0.04f, 0.05f, 0.07f, 0.95f));
        cardTitle = CreateText(cardPanel.transform, "CardTitle", "기억", 36, TextAnchor.MiddleLeft, Color.white);
        SetRect(cardTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, -92f), new Vector2(-40f, -25f));
        cardBody = CreateText(cardPanel.transform, "CardBody", "", 22, TextAnchor.UpperLeft, new Color(0.9f, 0.92f, 0.95f));
        SetRect(cardBody.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(44f, 116f), new Vector2(-44f, -105f));
        restoreButton = CreateButton(cardPanel.transform, "RestoreButton", "기억 복원", new Vector2(-285f, -270f), () => StartRestoreCurrent());
        preserveButton = CreateButton(cardPanel.transform, "PreserveButton", "보존", new Vector2(-285f, -270f), () => DecideCurrent(MemoryDecision3D.Preserve));
        deleteButton = CreateButton(cardPanel.transform, "DeleteButton", "삭제", new Vector2(0f, -270f), () => DecideCurrent(MemoryDecision3D.Delete));
        editButton = CreateButton(cardPanel.transform, "EditButton", "재가공", new Vector2(285f, -270f), () => DecideCurrent(MemoryDecision3D.Edit));
        CreateButton(cardPanel.transform, "CloseCardButton", "닫기", new Vector2(0f, -335f), CloseAllAndLock);
        cardPanel.SetActive(false);
    }

    private void BuildPuzzlePanel()
    {
        puzzlePanel = CreatePanel("PuzzlePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(980f, 720f), Vector2.zero, new Color(0.03f, 0.035f, 0.05f, 0.96f));
        puzzleTitle = CreateText(puzzlePanel.transform, "PuzzleTitle", "기억 복원 퍼즐", 34, TextAnchor.MiddleLeft, Color.white);
        SetRect(puzzleTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, -88f), new Vector2(-40f, -28f));
        Text guide = CreateText(puzzlePanel.transform, "PuzzleGuide", "문장 조각을 올바른 순서로 선택한 뒤 복원 확인을 누르세요.", 22, TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.9f));
        SetRect(guide.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, -138f), new Vector2(-40f, -98f));
        puzzleSelectedText = CreateText(puzzlePanel.transform, "SelectedText", "선택한 문장:", 22, TextAnchor.UpperLeft, new Color(0.95f, 0.95f, 0.85f));
        SetRect(puzzleSelectedText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(40f, 150f), new Vector2(-40f, 300f));

        GameObject root = new GameObject("PuzzlePiecesRoot");
        root.transform.SetParent(puzzlePanel.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        SetRect(rootRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, -90f), new Vector2(-40f, 115f));
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
        SetRect(archiveText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(44f, 92f), new Vector2(-44f, -42f));
        CreateButton(archivePanel.transform, "ClearSaveButton", "저장 초기화", new Vector2(-150f, -325f), () => MemoryManager3D.Instance.ClearSave());
        CreateButton(archivePanel.transform, "CloseArchiveButton", "닫기", new Vector2(150f, -325f), CloseAllAndLock);
        archivePanel.SetActive(false);
    }

    private void BuildLorePanel()
    {
        lorePanel = CreatePanel("LorePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(860f, 560f), Vector2.zero, new Color(0.035f, 0.042f, 0.055f, 0.96f));
        loreTitle = CreateText(lorePanel.transform, "LoreTitle", "도시 기록", 34, TextAnchor.MiddleLeft, Color.white);
        SetRect(loreTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, -88f), new Vector2(-40f, -28f));
        loreBody = CreateText(lorePanel.transform, "LoreBody", "", 23, TextAnchor.UpperLeft, new Color(0.9f, 0.93f, 0.96f));
        SetRect(loreBody.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(44f, 95f), new Vector2(-44f, -95f));
        CreateButton(lorePanel.transform, "CloseLoreButton", "닫기", new Vector2(0f, -255f), CloseAllAndLock);
        lorePanel.SetActive(false);
    }

    private void BuildEchoPanel()
    {
        echoPanel = CreatePanel("MemoryEchoPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.018f, 0.030f, 0.92f));
        RectTransform panelRect = echoPanel.GetComponent<RectTransform>();
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        echoTitle = CreateText(echoPanel.transform, "EchoTitle", "기억 동기화", 42, TextAnchor.MiddleCenter, Color.white);
        SetRect(echoTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(120f, -185f), new Vector2(-120f, -95f));
        echoBody = CreateText(echoPanel.transform, "EchoBody", "", 26, TextAnchor.UpperLeft, new Color(0.86f, 0.94f, 1f));
        SetRect(echoBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -155f), new Vector2(520f, 205f));
        CreateButton(echoPanel.transform, "EchoReturnButton", "기억 카드로 이동", new Vector2(-145f, -340f), () => ShowMemoryCard(currentEchoRecord));
        CreateButton(echoPanel.transform, "EchoCloseButton", "닫기", new Vector2(145f, -340f), CloseAllAndLock);
        echoPanel.SetActive(false);
    }

    private void BuildEndingPanel()
    {
        endingPanel = CreatePanel("EndingPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 580f), Vector2.zero, new Color(0.03f, 0.03f, 0.045f, 0.97f));
        endingTitle = CreateText(endingPanel.transform, "EndingTitle", "엔딩", 40, TextAnchor.MiddleCenter, Color.white);
        SetRect(endingTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, -105f), new Vector2(-40f, -40f));
        endingBody = CreateText(endingPanel.transform, "EndingBody", "", 24, TextAnchor.UpperLeft, new Color(0.9f, 0.92f, 0.95f));
        SetRect(endingBody.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(44f, 115f), new Vector2(-44f, -125f));
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
            MemoryManager3D.Instance.SaveGame();
            ShowMemoryEcho(currentRecord);
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

        puzzlePieceButtons.Clear();
        selectedPieceIndices.Clear();

        string[] pieces = currentPuzzleRecord.memory.sentencePieces;
        for (int i = 0; i < pieces.Length; i++)
        {
            int pieceIndex = i; // 클로저 캡처용 인덱스
            string piece = pieces[i];
            Button button = CreateButton(puzzlePiecesRoot, "Piece_" + i, piece, Vector2.zero, () => SelectPuzzlePiece(pieceIndex));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 58f);
            rect.anchoredPosition = new Vector2(0f, -i * 68f);
            puzzlePieceButtons.Add(button);
        }
    }

    // 중복 클릭으로 같은 조각이 두 번 들어가지 않도록 인덱스 기반으로 선택을 관리한다.
    private void SelectPuzzlePiece(int pieceIndex)
    {
        if (currentPuzzleRecord == null || currentPuzzleRecord.memory == null)
            return;

        string[] pieces = currentPuzzleRecord.memory.sentencePieces;
        if (pieceIndex < 0 || pieceIndex >= pieces.Length)
            return;

        if (selectedPieceIndices.Contains(pieceIndex))
            return;

        selectedPieceIndices.Add(pieceIndex);
        selectedPieces.Add(pieces[pieceIndex]);
        SetPuzzlePieceInteractable(pieceIndex, false);
        RefreshSelectedPiecesText();
    }

    private void RefreshSelectedPiecesText()
    {
        if (puzzleSelectedText == null)
            return;
        puzzleSelectedText.text = "선택한 문장:\n" + string.Join(" / ", selectedPieces.ToArray());
    }

    // 다시 선택 시 모든 버튼이 다시 눌릴 수 있도록 복구한다.
    private void ResetPuzzleSelection()
    {
        selectedPieces.Clear();
        selectedPieceIndices.Clear();
        for (int i = 0; i < puzzlePieceButtons.Count; i++)
            SetPuzzlePieceInteractable(i, true);
        RefreshSelectedPiecesText();
    }

    private void SetPuzzlePieceInteractable(int index, bool interactable)
    {
        if (index < 0 || index >= puzzlePieceButtons.Count)
            return;

        Button button = puzzlePieceButtons[index];
        if (button == null)
            return;

        button.interactable = interactable;
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = interactable
                ? new Color(0.16f, 0.21f, 0.29f, 0.96f)
                : new Color(0.08f, 0.11f, 0.16f, 0.74f);
        }
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
            ShowMemoryEcho(currentPuzzleRecord);
        }
        else
        {
            ShowToast("순서가 맞지 않습니다. 다시 조합해 보세요.");
        }
    }

    private void DecideCurrent(MemoryDecision3D decision)
    {
        if (currentRecord == null)
            return;

        MemoryManager3D.Instance.ApplyDecision(currentRecord.memory, decision);
        ShowMemoryCard(currentRecord);
    }

    private string GetDecisionGuideText(MemoryRecord3D record)
    {
        if (record == null || record.decision != MemoryDecision3D.Unchosen)
            return "";

        return
            "\n\n[처리 선택 안내]\n" +
            "보존: 기억을 원본 그대로 남깁니다. 고통도 남지만 진실과 증언이 유지됩니다.\n" +
            "삭제: 기억을 아카이브에서 지웁니다. 도시가 조용해지지만 잃어버린 사실도 함께 사라집니다.\n" +
            "재가공: 기억을 덜 위험한 형태로 다시 편집합니다. 상처는 줄지만 원본의 의미가 바뀔 수 있습니다.\n" +
            "이 선택들은 마지막 아카이브 결말에 반영됩니다.";
    }

    private void CloseAllMajorPanels()
    {
        if (cardPanel != null) cardPanel.SetActive(false);
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        if (archivePanel != null) archivePanel.SetActive(false);
        if (lorePanel != null) lorePanel.SetActive(false);
        if (echoPanel != null) echoPanel.SetActive(false);
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
        uiText.alignByGeometry = true;
        uiText.resizeTextForBestFit = false;
        uiText.raycastTarget = false;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        return uiText;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = new Color(0.16f, 0.21f, 0.29f, 0.96f);

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
