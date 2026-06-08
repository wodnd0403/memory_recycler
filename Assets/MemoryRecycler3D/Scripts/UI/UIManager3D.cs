using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager3D : MonoBehaviour
{
    public static UIManager3D Instance { get; private set; }

    // 제출 시연용 엔딩 진입 임계값. 발표 5분 루프에 맞춰 8개 중 3개 처리 시 엔딩 허용.
    public const int RequiredDecisionsForEnding = 3;

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
    private Text startMenuSaveStatusText;
    private GameObject pauseMenuPanel;
    private GameObject optionsPanel;
    private Slider bgmVolumeSlider;
    private Text bgmVolumeValueText;

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
    private readonly List<int> puzzleDisplayIndices = new List<int>();
    private MemoryRecord3D currentPuzzleRecord;
    private float lensSolveHoldTime;
    private float stillnessTime;
    private Vector3 lastLensMousePosition;
    private string specialPuzzleStatus = "";

    private GameObject archivePanel;
    private Text archiveText;
    private Button reopenMemoryButton;
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
    private ScrollRect endingScrollRect;

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

        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEscapeInput();

        UpdateTimeDisplay();
        UpdateObjectiveDisplay();
        UpdateSpecialPuzzleMode();

        // 시작 메뉴/주요 패널이 켜져 있는데 다른 컴포넌트가 마우스를 잠가버리면 클릭 불가가 된다.
        // 매 프레임 가드: UI가 열려 있는 동안에는 항상 커서가 보이도록 보정.
        if (IsAnyUiPanelOpen() && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // 시작 메뉴를 포함해 어떤 UI 패널이라도 켜져 있는지 확인.
    private bool IsAnyUiPanelOpen()
    {
        if (startMenuPanel != null && startMenuPanel.activeSelf)
            return true;
        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
            return true;
        if (optionsPanel != null && optionsPanel.activeSelf)
            return true;
        return IsAnyMajorPanelOpen();
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
        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
            return true;
        if (optionsPanel != null && optionsPanel.activeSelf)
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

        cardTitle.text = GetMemoryDisplayTitle(record);
        cardBody.text =
            "위치: " + location + "\n" +
            "감정 태그: " + record.memory.emotion + "\n" +
            "손상률: " + record.memory.corruptionLevel + "%\n" +
            "처리 상태: " + decision + "\n\n" +
            record.memory.description +
            (record.restored ? "\n\n[복원된 기억]\n" + record.memory.restoredText + clue + GetDecisionGuideText(record) : "\n\n아직 복원되지 않은 기억입니다. 문장 조각을 맞춰 원문을 복구하세요.");

        if (record.restored && (record.decision == MemoryDecision3D.Delete || record.decision == MemoryDecision3D.Edit))
            cardBody.text += "\n\n[아카이브 재표시]\n" + GetMemoryDisplayText(record);

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
        sb.AppendLine("[회수 기록]");
        sb.AppendLine("--------------------------------");

        List<MemoryRecord3D> records = MemoryManager3D.Instance != null
            ? MemoryManager3D.Instance.collectedMemories
            : new List<MemoryRecord3D>();
        int decided = MemoryManager3D.Instance != null ? MemoryManager3D.Instance.CountDecidedMemories() : 0;
        if (records.Count == 0)
        {
            sb.AppendLine("아직 회수한 기억이 없습니다.");
            sb.AppendLine();
            sb.AppendLine("푸른 기억 구체를 찾아 E 키로 회수하세요.");
            sb.AppendLine("기억을 복원하고 처리하면 이곳에 기록됩니다.");
        }
        else
        {
            for (int i = 0; i < records.Count; i++)
            {
                MemoryRecord3D record = records[i];
                string location = string.IsNullOrEmpty(record.memory.locationName) ? "위치 미상" : record.memory.locationName;
                sb.AppendLine((i + 1) + ". " + GetMemoryStatusLabel(record) + " " + GetMemoryDisplayTitle(record));
                sb.AppendLine("   위치: " + location);
                sb.AppendLine("   감정: " + record.memory.emotion + " / 안정도: " + PlayerMemoryLog3D.Ensure().GetInstabilityLabel(record.memory) + " / 처리: " + MemoryManager3D.DecisionToKorean(record.decision));
            }
        }

        sb.AppendLine();
        sb.AppendLine("중앙 아카이브 접속 조건:");
        sb.AppendLine("처리 완료 기억 " + RequiredDecisionsForEnding + "개 이상");
        sb.AppendLine();
        sb.AppendLine("현재 진행:");
        sb.AppendLine(decided + " / " + RequiredDecisionsForEnding + " 처리 완료");
        sb.AppendLine();
        sb.Append(BuildArchiveProgressReaction(records, decided));

        archiveText.text = sb.ToString();
        RefreshArchiveActionButton(records);
        archivePanel.SetActive(true);
        UnlockCursor();
    }

    // 회수된 기억 중 가장 빠른 미처리 항목을 카드로 다시 열어준다.
    // 퍼즐을 닫거나 카드를 닫은 뒤 다시 진행할 동선이 없는 문제를 해결.
    public void OpenNextPendingMemoryCard()
    {
        if (MemoryManager3D.Instance == null)
            return;

        List<MemoryRecord3D> records = MemoryManager3D.Instance.collectedMemories;

        // 우선순위: 1) 복원 안 된 기억, 2) 복원됐지만 미선택 기억
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i] != null && records[i].memory != null && !records[i].restored)
            {
                ShowMemoryCard(records[i]);
                return;
            }
        }
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i] != null && records[i].memory != null && records[i].decision == MemoryDecision3D.Unchosen)
            {
                ShowMemoryCard(records[i]);
                return;
            }
        }

        ShowToast("처리 대기 중인 기억이 없습니다.");
    }

    private void RefreshArchiveActionButton(List<MemoryRecord3D> records)
    {
        bool hasPending = HasPendingMemory(records);
        SetButtonEnabled(reopenMemoryButton, hasPending, hasPending ? "다음 처리할 기억 열기" : "처리할 기억 없음");
    }

    private bool HasPendingMemory(List<MemoryRecord3D> records)
    {
        if (records == null)
            return false;

        for (int i = 0; i < records.Count; i++)
        {
            MemoryRecord3D record = records[i];
            if (record == null || record.memory == null)
                continue;

            if (!record.restored || record.decision == MemoryDecision3D.Unchosen)
                return true;
        }

        return false;
    }

    private int CountPendingRestore(List<MemoryRecord3D> records)
    {
        if (records == null)
            return 0;

        int count = 0;
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i] != null && records[i].memory != null && !records[i].restored)
                count++;
        }
        return count;
    }

    public void ShowArchiveLocked(int decided, int required)
    {
        CloseAllMajorPanels();
        if (MemoryAudio3D.Instance != null)
            MemoryAudio3D.Instance.PlayArchiveDenied();

        List<MemoryRecord3D> records = MemoryManager3D.Instance != null
            ? MemoryManager3D.Instance.collectedMemories
            : new List<MemoryRecord3D>();

        archiveText.text =
            "[결말 접속 터미널]\n" +
            "중앙 아카이브가 아직 열리지 않았습니다.\n\n" +
            "이 터미널은 회수 기록을 읽는 곳이 아니라, 마지막 판단을 계산하는 접속 장치입니다.\n" +
            "충분한 기억을 복원하고 보존/삭제/재가공 중 하나로 처리해야 결말에 접근할 수 있습니다.\n\n" +
            "접속 조건: 처리 완료 기억 " + required + "개 이상\n" +
            "현재 진행: " + decided + " / " + required + " 처리 완료\n\n" +
            (CountPendingRestore(records) > 0 ? "아카이브는 아직 복원되지 않은 기억의 소음을 감지했습니다.\n\n" : "") +
            "다음 행동: 푸른 기억 구체를 찾아 E로 회수하고, 기억 카드에서 복원과 처리를 완료하세요.\n\n" +
            BuildArchiveProgressReaction(records, decided);

        RefreshArchiveActionButton(records);
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
        echoTitle.text = "기억 동기화: " + GetMemoryDisplayTitle(record);
        echoBody.text =
            "위치 신호: " + location + "\n" +
            "감정 잔향: " + record.memory.emotion + "\n\n" +
            GetMemoryDisplayText(record) + "\n\n" +
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
        if (MemoryRecyclerMusicManager.Instance != null)
            MemoryRecyclerMusicManager.Instance.PlayEndingMusic();

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

        PlayerMemoryLog3D log = PlayerMemoryLog3D.Ensure();
        log.MarkEndingReached();
        List<MemoryRecord3D> records = MemoryManager3D.Instance != null
            ? MemoryManager3D.Instance.collectedMemories
            : new List<MemoryRecord3D>();

        endingTitle.text = title;
        endingBody.text =
            body +
            "\n\n[선택 통계]\n보존 " + preserved + " / 삭제 " + deleted + " / 재가공 " + edited +
            "\n\n" + log.BuildBehaviorSummaryReport() +
            "\n" + BuildArchiveTestimony(records);
        if (endingScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            endingScrollRect.verticalNormalizedPosition = 1f;
        }
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
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        objectivePanel = CreatePanel("ObjectivePanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(560f, 150f), new Vector2(28f, -28f), new Color(0.025f, 0.032f, 0.045f, 0.82f));
        objectiveText = CreateText(objectivePanel.transform, "ObjectiveText", "", 21, TextAnchor.UpperLeft, new Color(0.94f, 0.97f, 1f));
        SetRect(objectiveText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 14f), new Vector2(-18f, -14f));

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
        BuildPauseMenu();
        BuildOptionsPanel();
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

        Button newGameButton = CreateButton(startMenuPanel.transform, "NewGameButton", "새 게임 (저장 삭제)", new Vector2(-145f, -20f), StartFreshGame);
        RectTransform newRect = newGameButton.GetComponent<RectTransform>();
        newRect.sizeDelta = new Vector2(270f, 64f);
        Image newImage = newGameButton.GetComponent<Image>();
        if (newImage != null)
            newImage.color = new Color(0.24f, 0.36f, 0.30f, 0.96f);

        continueButton = CreateButton(startMenuPanel.transform, "ContinueGameButton", "이어하기", new Vector2(145f, -20f), ContinueSavedGame);
        RectTransform continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.sizeDelta = new Vector2(270f, 64f);

        startMenuSaveStatusText = CreateText(startMenuPanel.transform, "StartSaveStatus", "", 20, TextAnchor.MiddleCenter, new Color(0.80f, 0.86f, 0.74f));
        SetRect(startMenuSaveStatusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -78f), new Vector2(520f, -52f));

        Text hint = CreateText(startMenuPanel.transform, "StartHint", "WASD 이동 / Shift 달리기 / E 상호작용 / Tab 아카이브", 20, TextAnchor.MiddleCenter, new Color(0.62f, 0.70f, 0.76f));
        SetRect(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -130f), new Vector2(520f, -80f));

        startMenuPanel.SetActive(false);
    }

    private void BuildPauseMenu()
    {
        pauseMenuPanel = CreatePanel("PauseMenuPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.005f, 0.011f, 0.018f, 0.76f));
        RectTransform panelRect = pauseMenuPanel.GetComponent<RectTransform>();
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject menuBox = CreatePanel("PauseMenuBox", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 500f), Vector2.zero, new Color(0.035f, 0.045f, 0.060f, 0.96f));
        menuBox.transform.SetParent(pauseMenuPanel.transform, false);

        Text title = CreateText(menuBox.transform, "PauseTitle", "일시정지", 38, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(32f, -86f), new Vector2(-32f, -24f));

        Button resumeButton = CreateButton(menuBox.transform, "ResumeButton", "계속하기", new Vector2(0f, 92f), HidePauseMenu);
        Button saveButton = CreateButton(menuBox.transform, "SaveGameButton", "게임 저장", new Vector2(0f, 18f), SaveGameFromPauseMenu);
        Button optionsButton = CreateButton(menuBox.transform, "OptionsButton", "옵션", new Vector2(0f, -56f), ShowOptionsPanel);
        Button closeButton = CreateButton(menuBox.transform, "ClosePauseButton", "닫기", new Vector2(0f, -130f), HidePauseMenu);
        Button quitButton = CreateButton(menuBox.transform, "QuitGameButton", "게임 종료", new Vector2(0f, -204f), QuitFromPauseMenu);

        SetButtonSize(resumeButton, new Vector2(280f, 58f));
        SetButtonSize(saveButton, new Vector2(280f, 58f));
        SetButtonSize(optionsButton, new Vector2(280f, 58f));
        SetButtonSize(closeButton, new Vector2(280f, 58f));
        SetButtonSize(quitButton, new Vector2(280f, 58f));

        pauseMenuPanel.SetActive(false);
    }

    private void BuildOptionsPanel()
    {
        optionsPanel = CreatePanel("OptionsPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.005f, 0.011f, 0.018f, 0.78f));
        RectTransform panelRect = optionsPanel.GetComponent<RectTransform>();
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject box = CreatePanel("OptionsBox", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 350f), Vector2.zero, new Color(0.035f, 0.045f, 0.060f, 0.97f));
        box.transform.SetParent(optionsPanel.transform, false);

        Text title = CreateText(box.transform, "OptionsTitle", "옵션", 36, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(32f, -80f), new Vector2(-32f, -24f));

        Text label = CreateText(box.transform, "BgmVolumeLabel", "배경음악", 24, TextAnchor.MiddleLeft, new Color(0.92f, 0.96f, 1f));
        SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 22f), new Vector2(220f, 76f));

        bgmVolumeValueText = CreateText(box.transform, "BgmVolumeValue", "", 22, TextAnchor.MiddleRight, new Color(0.72f, 0.92f, 1f));
        SetRect(bgmVolumeValueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-180f, 22f), new Vector2(-58f, 76f));

        bgmVolumeSlider = CreateSlider(box.transform, "BgmVolumeSlider", new Vector2(0f, -14f), GetCurrentBgmVolume(), OnBgmVolumeChanged);

        Button backButton = CreateButton(box.transform, "OptionsBackButton", "뒤로", new Vector2(-145f, -118f), BackToPauseMenuFromOptions);
        Button closeButton = CreateButton(box.transform, "OptionsCloseButton", "게임으로 돌아가기", new Vector2(145f, -118f), HidePauseMenu);
        SetButtonSize(backButton, new Vector2(230f, 56f));
        SetButtonSize(closeButton, new Vector2(230f, 56f));

        RefreshBgmVolumeText(GetCurrentBgmVolume());
        optionsPanel.SetActive(false);
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

        if (startMenuSaveStatusText != null)
        {
            startMenuSaveStatusText.text = hasSave
                ? "이전 저장 데이터가 있습니다. '새 게임'을 누르면 즉시 삭제됩니다."
                : "저장된 데이터 없음 — 새 게임으로 시작하세요.";
            startMenuSaveStatusText.color = hasSave ? new Color(0.96f, 0.78f, 0.45f) : new Color(0.62f, 0.72f, 0.78f);
        }

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
        bool hadSave = MemoryManager3D.HasSaveGame();
        if (MemoryManager3D.Instance != null)
            MemoryManager3D.Instance.StartNewGame();

        HideStartMenu();
        ShowToast(hadSave
            ? "이전 저장을 삭제하고 새 탐사를 시작합니다."
            : "새 탐사를 시작합니다.");
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

    private void HandleEscapeInput()
    {
        if (startMenuPanel != null && startMenuPanel.activeSelf)
            return;

        if (optionsPanel != null && optionsPanel.activeSelf)
        {
            BackToPauseMenuFromOptions();
            return;
        }

        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            HidePauseMenu();
            return;
        }

        if (IsAnyMajorPanelOpen())
            return;

        ShowPauseMenu();
    }

    private void ShowPauseMenu()
    {
        if (pauseMenuPanel == null)
            return;

        CloseAllMajorPanels();
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        UnlockCursor();
    }

    private void HidePauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        Time.timeScale = 1f;
        LockCursor();
    }

    private void ShowOptionsPanel()
    {
        if (optionsPanel == null)
            return;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.SetValueWithoutNotify(GetCurrentBgmVolume());
        RefreshBgmVolumeText(GetCurrentBgmVolume());

        optionsPanel.SetActive(true);
        Time.timeScale = 0f;
        UnlockCursor();
    }

    private void BackToPauseMenuFromOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        Time.timeScale = 0f;
        UnlockCursor();
    }

    private void SaveGameFromPauseMenu()
    {
        if (MemoryManager3D.Instance == null)
        {
            ShowToast("저장 시스템을 찾을 수 없습니다.");
            return;
        }

        MemoryManager3D.Instance.SaveGame();
        ShowToast("게임을 저장했습니다.");
    }

    private void QuitFromPauseMenu()
    {
        if (MemoryManager3D.Instance != null)
            MemoryManager3D.Instance.SaveGame();

        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (MemoryRecyclerMusicManager.Instance != null)
            MemoryRecyclerMusicManager.Instance.SetBgmVolume(value);

        RefreshBgmVolumeText(value);
    }

    private float GetCurrentBgmVolume()
    {
        if (MemoryRecyclerMusicManager.Instance != null)
            return MemoryRecyclerMusicManager.Instance.BgmVolume;

        return 0.3f;
    }

    private void RefreshBgmVolumeText(float value)
    {
        if (bgmVolumeValueText != null)
            bgmVolumeValueText.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
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
        int pendingRestore = CountPendingRestore(MemoryManager3D.Instance.collectedMemories);

        // 단계별 안내 임계값. 회수/복원은 시연용 가이드 수치이므로 엔딩 임계값과 분리해 둔다.
        const int CollectGuide = 3;
        const int RestoreGuide = 3;

        string next;
        if (collected < CollectGuide)
            next = "푸른 기억 구체를 찾아 E로 회수하고, " + RequiredDecisionsForEnding + "개의 기억을 처리하세요.";
        else if (restored < RestoreGuide)
            next = "회수한 기억의 문장 조각을 복원하세요.";
        else if (decided < RequiredDecisionsForEnding)
            next = "복원된 기억을 보존/삭제/재가공으로 처리하세요.";
        else
            next = "중앙 아카이브 탑에서 E로 결말 접속";

        objectiveText.text =
            "현재 목표\n" +
            next + "\n\n" +
            "회수 " + collected + " / " + known + "   복원 " + restored + "   처리 " + decided + " / " + RequiredDecisionsForEnding + "\n" +
            (pendingRestore > 0 ? "복원 대기 기억 " + pendingRestore + "개\n" : "") +
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
        SetRect(puzzleSelectedText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, -170f), new Vector2(-40f, -78f));

        GameObject root = new GameObject("PuzzlePiecesRoot");
        root.transform.SetParent(puzzlePanel.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        SetRect(rootRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, -35f), new Vector2(-40f, 160f));
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
        SetRect(archiveText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(44f, 150f), new Vector2(-44f, -42f));
        reopenMemoryButton = CreateButton(archivePanel.transform, "ReopenMemoryButton", "다음 처리할 기억 열기", new Vector2(0f, -250f), OpenNextPendingMemoryCard);
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
        endingPanel = CreatePanel("EndingPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(980f, 720f), Vector2.zero, new Color(0.03f, 0.03f, 0.045f, 0.97f));
        endingTitle = CreateText(endingPanel.transform, "EndingTitle", "엔딩", 40, TextAnchor.MiddleCenter, Color.white);
        SetRect(endingTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, -92f), new Vector2(-40f, -30f));

        GameObject scrollObject = new GameObject("EndingScroll");
        scrollObject.transform.SetParent(endingPanel.transform, false);
        Image scrollImage = scrollObject.AddComponent<Image>();
        scrollImage.color = new Color(0.015f, 0.018f, 0.026f, 0.62f);
        Mask scrollMask = scrollObject.AddComponent<Mask>();
        scrollMask.showMaskGraphic = false;
        endingScrollRect = scrollObject.AddComponent<ScrollRect>();
        endingScrollRect.horizontal = false;
        endingScrollRect.vertical = true;
        endingScrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform scrollTransform = scrollObject.GetComponent<RectTransform>();
        SetRect(scrollTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(54f, 132f), new Vector2(-54f, -115f));

        endingBody = CreateText(scrollObject.transform, "EndingBody", "", 22, TextAnchor.UpperLeft, new Color(0.9f, 0.92f, 0.95f));
        RectTransform bodyRect = endingBody.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = new Vector2(0f, -16f);
        bodyRect.sizeDelta = new Vector2(-32f, 0f);
        ContentSizeFitter fitter = endingBody.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        endingScrollRect.content = bodyRect;
        endingScrollRect.viewport = scrollTransform;

        // 엔딩 후 흐름 정리: 타이틀로 / 새 게임 / 종료. 닫기 버튼은 발표 중 실수로 게임이 무한 지속되는 상황을 만들어 제거.
        CreateButton(endingPanel.transform, "EndingToTitleButton", "타이틀로", new Vector2(-260f, -305f), ReturnToTitleFromEnding);
        CreateButton(endingPanel.transform, "EndingNewGameButton", "새 게임", new Vector2(0f, -305f), StartNewGameFromEnding);
        CreateButton(endingPanel.transform, "EndingQuitButton", "종료", new Vector2(260f, -305f), QuitFromEnding);
        endingPanel.SetActive(false);
    }

    private void StartRestoreCurrent()
    {
        if (currentRecord == null || currentRecord.memory == null)
            return;

        // 퍼즐 데이터가 비어 있으면 발표 중 의미 없는 자동 통과가 되므로 차단한다.
        // 정상 자산은 sentencePieces/correctOrder 모두 채워져 있어야 한다.
        string[] pieces = currentRecord.memory.sentencePieces;
        string[] order = currentRecord.memory.correctOrder;
        bool puzzleMissing =
            pieces == null || pieces.Length == 0 ||
            order == null || order.Length == 0 ||
            pieces.Length != order.Length;
        if (puzzleMissing)
        {
            Debug.LogWarning("[MR3D] 퍼즐 데이터 누락/불일치 - 자동 복원을 차단합니다: " + currentRecord.memory.id, currentRecord.memory);
            ShowToast("이 기억은 퍼즐 데이터가 누락되어 복원할 수 없습니다.");
            return;
        }

        currentPuzzleRecord = currentRecord;
        selectedPieces.Clear();
        lensSolveHoldTime = 0f;
        stillnessTime = 0f;
        lastLensMousePosition = Input.mousePosition;
        specialPuzzleStatus = GetSpecialPuzzleIntro(currentPuzzleRecord.memory);
        CloseAllMajorPanels();
        SetPuzzlePanelLensMode(currentPuzzleRecord.memory.puzzleMode != MemoryPuzzleMode3D.Sequence);
        PlayerMemoryLog3D.Ensure().BeginMemoryRestore(currentPuzzleRecord.memory);
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
        puzzleDisplayIndices.Clear();

        string[] pieces = currentPuzzleRecord.memory.sentencePieces;
        BuildPuzzleDisplayOrder(pieces.Length);
        for (int i = 0; i < puzzleDisplayIndices.Count; i++)
        {
            int pieceIndex = puzzleDisplayIndices[i]; // 클로저 캡처용 원본 인덱스
            string piece = pieces[pieceIndex];
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

        string selected = selectedPieces.Count > 0 ? string.Join(" / ", selectedPieces.ToArray()) : "아직 선택한 조각이 없습니다.";
        string hint = GetPuzzleHint(currentPuzzleRecord);
        string stability = GetPuzzleStabilityText(currentPuzzleRecord);
        string special = GetSpecialPuzzleText();
        puzzleSelectedText.text =
            stability + "\n\n" +
            special +
            "선택한 문장:\n" + selected +
            (string.IsNullOrEmpty(hint) ? "" : "\n\n[아카이브 힌트]\n" + hint);
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
        int buttonIndex = puzzleDisplayIndices.Count > 0 ? puzzleDisplayIndices.IndexOf(index) : index;
        if (buttonIndex < 0 || buttonIndex >= puzzlePieceButtons.Count)
            return;

        Button button = puzzlePieceButtons[buttonIndex];
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

    private void BuildPuzzleDisplayOrder(int count)
    {
        for (int i = 0; i < count; i++)
            puzzleDisplayIndices.Add(i);

        if (count <= 1)
            return;

        int seed = currentPuzzleRecord != null && currentPuzzleRecord.memory != null
            ? GetStablePuzzleSeed(currentPuzzleRecord.memory.id)
            : count * 17;

        System.Random random = new System.Random(seed);
        for (int i = puzzleDisplayIndices.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            int value = puzzleDisplayIndices[i];
            puzzleDisplayIndices[i] = puzzleDisplayIndices[swapIndex];
            puzzleDisplayIndices[swapIndex] = value;
        }

        bool unchanged = true;
        for (int i = 0; i < puzzleDisplayIndices.Count; i++)
        {
            if (puzzleDisplayIndices[i] != i)
            {
                unchanged = false;
                break;
            }
        }

        if (unchanged)
        {
            int first = puzzleDisplayIndices[0];
            puzzleDisplayIndices[0] = puzzleDisplayIndices[count - 1];
            puzzleDisplayIndices[count - 1] = first;
        }
    }

    private int GetStablePuzzleSeed(string memoryId)
    {
        if (string.IsNullOrEmpty(memoryId))
            return 37;

        int seed = 23;
        for (int i = 0; i < memoryId.Length; i++)
            seed = seed * 31 + memoryId[i];
        return Mathf.Abs(seed);
    }

    private void UpdateSpecialPuzzleMode()
    {
        if (puzzlePanel == null || !puzzlePanel.activeSelf)
            return;
        if (currentPuzzleRecord == null || currentPuzzleRecord.memory == null || currentPuzzleRecord.restored)
            return;

        MemoryPuzzleMode3D mode = currentPuzzleRecord.memory.puzzleMode;
        switch (mode)
        {
            case MemoryPuzzleMode3D.LensAlign:
                UpdateLensPuzzle(mode, "기록지가 도시와 겹쳤습니다. 파란 우산의 빈칸이 복원됩니다.");
                break;
            case MemoryPuzzleMode3D.LensOcclude:
                UpdateLensPuzzle(mode, "가려진 단어 아래에서 누락된 이름이 감지되었습니다.");
                break;
            case MemoryPuzzleMode3D.Stillness:
                UpdateStillnessPuzzle();
                break;
        }
    }

    private void UpdateLensPuzzle(MemoryPuzzleMode3D mode, string successMessage)
    {
        Transform anchor = FindLensAnchor(currentPuzzleRecord.memory, mode);
        Camera camera = Camera.main;
        if (anchor == null || camera == null)
        {
            specialPuzzleStatus = "렌즈 앵커를 찾지 못했습니다. 아래 문장 조각 퍼즐로 복원할 수 있습니다.";
            RefreshSelectedPiecesText();
            return;
        }

        Vector3 screen = camera.WorldToScreenPoint(anchor.position);
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float distance = screen.z > 0f ? Vector2.Distance(new Vector2(screen.x, screen.y), center) : 9999f;
        float threshold = Mathf.Max(70f, Mathf.Min(Screen.width, Screen.height) * 0.085f);

        if (distance <= threshold)
        {
            lensSolveHoldTime += Time.unscaledDeltaTime;
            specialPuzzleStatus = "렌즈 정렬 중... " + Mathf.Clamp01(lensSolveHoldTime / 0.7f).ToString("P0");
            if (lensSolveHoldTime >= 0.7f)
                CompleteSpecialPuzzle(mode, successMessage);
        }
        else
        {
            lensSolveHoldTime = 0f;
            specialPuzzleStatus = mode == MemoryPuzzleMode3D.LensOcclude
                ? "거짓 안내 문장을 도시 구조물 아래에 겹치십시오. 화면 중앙 거리가 " + Mathf.RoundToInt(distance) + "px입니다."
                : "기록창의 빈칸을 도시 위에 겹치십시오. 화면 중앙 거리가 " + Mathf.RoundToInt(distance) + "px입니다.";
        }

        RefreshSelectedPiecesText();
    }

    private void UpdateStillnessPuzzle()
    {
        bool disturbed =
            Vector3.Distance(Input.mousePosition, lastLensMousePosition) > 1.5f ||
            Input.GetMouseButtonDown(0) ||
            Input.GetMouseButtonDown(1) ||
            Input.GetMouseButtonDown(2) ||
            Input.anyKeyDown;

        if (disturbed)
        {
            stillnessTime = 0f;
            specialPuzzleStatus = "빈 파일이 흔들렸습니다. 아무 입력 없이 멈추면 다시 열립니다.";
        }
        else
        {
            stillnessTime += Time.unscaledDeltaTime;
            specialPuzzleStatus = "정지 유지 중... " + Mathf.Clamp01(stillnessTime / 5f).ToString("P0");
            if (stillnessTime >= 5f)
                CompleteSpecialPuzzle(MemoryPuzzleMode3D.Stillness, "수거원이 멈추자, 빈 파일이 스스로를 열었습니다.");
        }

        lastLensMousePosition = Input.mousePosition;
        RefreshSelectedPiecesText();
    }

    private Transform FindLensAnchor(MemoryData3D memory, MemoryPuzzleMode3D mode)
    {
        if (memory != null && !string.IsNullOrEmpty(memory.id))
        {
            MemoryObject3D[] memoryObjects = FindObjectsByType<MemoryObject3D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < memoryObjects.Length; i++)
            {
                if (memoryObjects[i] != null && memoryObjects[i].memoryData != null && memoryObjects[i].memoryData.id == memory.id)
                    return memoryObjects[i].transform;
            }
        }

        if (mode == MemoryPuzzleMode3D.LensOcclude)
        {
            ArchiveTerminal3D archive = FindFirstObjectByType<ArchiveTerminal3D>();
            if (archive != null)
                return archive.transform;
        }

        return null;
    }

    private void CompleteSpecialPuzzle(MemoryPuzzleMode3D mode, string successMessage)
    {
        if (currentPuzzleRecord == null || currentPuzzleRecord.memory == null || currentPuzzleRecord.restored)
            return;

        MemoryManager3D.Instance.MarkRestored(currentPuzzleRecord.memory);
        PlayerMemoryLog3D.Ensure().MarkMemoryRestored(currentPuzzleRecord.memory, mode);
        ShowToast(successMessage);
        ShowMemoryEcho(currentPuzzleRecord);
    }

    private void SetPuzzlePanelLensMode(bool enabled)
    {
        if (puzzlePanel == null)
            return;

        Image image = puzzlePanel.GetComponent<Image>();
        if (image == null)
            return;

        image.color = enabled
            ? new Color(0.03f, 0.035f, 0.05f, 0.58f)
            : new Color(0.03f, 0.035f, 0.05f, 0.96f);
    }

    private string GetSpecialPuzzleIntro(MemoryData3D memory)
    {
        if (memory == null)
            return "";

        switch (memory.puzzleMode)
        {
            case MemoryPuzzleMode3D.LensAlign:
                return "이 기록은 화면 안에 없습니다. 도시 위에 겹치십시오.";
            case MemoryPuzzleMode3D.LensOcclude:
                return "가려야 보이는 문장이 있습니다.";
            case MemoryPuzzleMode3D.Stillness:
                return "빈 파일은 움직이는 수거원에게 열리지 않습니다.";
            default:
                return "";
        }
    }

    private string GetSpecialPuzzleText()
    {
        if (currentPuzzleRecord == null || currentPuzzleRecord.memory == null)
            return "";

        MemoryPuzzleMode3D mode = currentPuzzleRecord.memory.puzzleMode;
        if (mode == MemoryPuzzleMode3D.Sequence)
            return "";

        string modeName = mode == MemoryPuzzleMode3D.LensAlign ? "기억 렌즈: 겹치기" :
            mode == MemoryPuzzleMode3D.LensOcclude ? "기억 렌즈: 가리기" :
            "기억 렌즈: 멈추기";

        return "[" + modeName + "]\n" + specialPuzzleStatus + "\n\n";
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
            PlayerMemoryLog3D.Ensure().MarkMemoryRestored(currentPuzzleRecord.memory, MemoryPuzzleMode3D.Sequence);
            ShowToast("기억이 복원되었습니다.");
            ShowMemoryEcho(currentPuzzleRecord);
        }
        else
        {
            PlayerMemoryLog3D.Ensure().RecordPuzzleMistake(currentPuzzleRecord.memory);
            ShowToast(GetPuzzleMistakeFeedback(currentPuzzleRecord));
        }
    }

    private void DecideCurrent(MemoryDecision3D decision)
    {
        if (currentRecord == null)
            return;

        MemoryManager3D.Instance.ApplyDecision(currentRecord.memory, decision);
        ShowMemoryCard(currentRecord);
    }

    private string GetMemoryDisplayTitle(MemoryRecord3D record)
    {
        if (record == null || record.memory == null)
            return "이름 없는 기억";

        if (record.decision == MemoryDecision3D.Delete)
            return ObscureTitle(record.memory.memoryTitle);

        return record.memory.memoryTitle;
    }

    private string GetMemoryDisplayText(MemoryRecord3D record)
    {
        if (record == null || record.memory == null)
            return "";

        switch (record.decision)
        {
            case MemoryDecision3D.Delete:
                return "□□□\n\n" + GetDeleteTestimony(record.memory);
            case MemoryDecision3D.Edit:
                return BuildReworkedMemoryText(record.memory) + "\n\n[기록 불일치]\n" + GetReprocessWarning(record.memory);
            default:
                return record.memory.restoredText;
        }
    }

    private string BuildArchiveTestimony(List<MemoryRecord3D> records)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[아카이브 증언]");

        if (records == null || records.Count == 0)
        {
            sb.AppendLine("- 증언 없음.");
            return sb.ToString();
        }

        for (int i = 0; i < records.Count; i++)
        {
            MemoryRecord3D record = records[i];
            if (record == null || record.memory == null || record.decision == MemoryDecision3D.Unchosen)
                continue;

            sb.AppendLine("- " + GetMemoryDisplayTitle(record) + ": " + GetMemoryTestimony(record));
        }

        return sb.ToString();
    }

    private string GetMemoryTestimony(MemoryRecord3D record)
    {
        string instabilityNote = GetInstabilityTestimonyNote(record);
        switch (record.decision)
        {
            case MemoryDecision3D.Preserve:
                return GetPreserveTestimony(record.memory) + instabilityNote;
            case MemoryDecision3D.Delete:
                return GetDeleteTestimony(record.memory) + instabilityNote;
            case MemoryDecision3D.Edit:
                return BuildReworkedMemoryText(record.memory) + " (" + GetReprocessWarning(record.memory) + ")" + instabilityNote;
            default:
                return "미분류 기록.";
        }
    }

    private string GetInstabilityTestimonyNote(MemoryRecord3D record)
    {
        if (record == null || record.memory == null)
            return "";

        int instability = PlayerMemoryLog3D.Ensure().GetInstabilityFor(record.memory);
        if (instability < 3)
            return "";

        return " [복원 흔들림 감지]";
    }

    private string BuildReworkedMemoryText(MemoryData3D memory)
    {
        if (memory != null && !string.IsNullOrEmpty(memory.reprocessedText))
            return memory.reprocessedText;

        string source = memory != null ? memory.restoredText : "";
        if (string.IsNullOrEmpty(source))
            return "기억은 부드러운 빛으로 다시 쓰였다.";

        return "기억은 조금 덜 아픈 문장으로 재가공되었다. " + source;
    }

    private string GetPuzzleHint(MemoryRecord3D record)
    {
        if (record == null || record.memory == null)
            return "";

        if (!string.IsNullOrEmpty(record.memory.puzzleHint))
            return record.memory.puzzleHint;

        return "문장의 원인을 먼저 찾고, 그다음 남은 감정이 무엇을 요구하는지 따라가세요.";
    }

    private string GetPuzzleMistakeFeedback(MemoryRecord3D record)
    {
        int mistakes = PlayerMemoryLog3D.Ensure().GetPuzzleMistakesFor(record != null ? record.memory : null);

        if (mistakes <= 1)
            return "문장이 아직 불안정합니다.";
        if (mistakes == 2)
            return "기억의 가장자리가 흐려집니다.";

        return "아카이브가 이 복원을 불안정 기록으로 분류합니다.";
    }

    private string GetPuzzleStabilityText(MemoryRecord3D record)
    {
        if (record == null || record.memory == null)
            return "기억 안정도: 확인 중";

        PlayerMemoryLog3D log = PlayerMemoryLog3D.Ensure();
        int instability = log.GetInstabilityFor(record.memory);
        return "기억 안정도: " + log.GetInstabilityLabel(record.memory) + "  |  기록 불안정도 " + instability;
    }

    private string GetPreserveTestimony(MemoryData3D memory)
    {
        if (memory != null && !string.IsNullOrEmpty(memory.preserveTestimony))
            return memory.preserveTestimony;

        return memory != null && !string.IsNullOrEmpty(memory.restoredText)
            ? memory.restoredText
            : "원문 그대로 보존됨.";
    }

    private string GetDeleteTestimony(MemoryData3D memory)
    {
        if (memory != null && !string.IsNullOrEmpty(memory.deleteTestimony))
            return "증언 없음. " + memory.deleteTestimony;

        return "증언 없음. 삭제된 기억은 중앙 아카이브의 표면에 빈칸으로만 남았다.";
    }

    private string GetReprocessWarning(MemoryData3D memory)
    {
        if (memory != null && !string.IsNullOrEmpty(memory.reprocessWarning))
            return memory.reprocessWarning;

        return "문장은 더 아름다워졌지만, 원본과 완전히 일치하지 않습니다.";
    }

    private string BuildArchiveProgressReaction(List<MemoryRecord3D> records, int decided)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[아카이브 진행 반응]");

        if (records == null || records.Count == 0)
        {
            sb.AppendLine("- 아카이브는 아직 도시보다 수거원을 더 많이 기다리고 있습니다.");
            sb.AppendLine("- 첫 번째 기억을 회수하면 기록 회로가 열립니다.");
            sb.AppendLine("- 중앙 아카이브는 처리된 기억 " + RequiredDecisionsForEnding + "개 이상을 요구합니다.");
            return sb.ToString();
        }

        PlayerMemoryLog3D log = PlayerMemoryLog3D.Ensure();
        if (decided <= 0)
        {
            sb.AppendLine("- 회수 기록은 늘었지만, 수거원의 판단 패턴은 아직 비어 있습니다.");
        }
        else if (decided < RequiredDecisionsForEnding)
        {
            sb.AppendLine("- 아카이브가 수거원의 선택을 분류 중입니다. 처리 완료 " + decided + " / " + RequiredDecisionsForEnding + ".");
        }
        else
        {
            sb.AppendLine("- 결말 조건이 충족되었습니다. 이제 중앙 아카이브는 도시의 기억과 수거원의 행동을 함께 판정합니다.");
        }

        if (log.firstDecision != MemoryDecision3D.Unchosen)
            sb.AppendLine("- 첫 선택 패턴: " + MemoryManager3D.DecisionToKorean(log.firstDecision) + ".");
        if (log.puzzleMistakeCount > 0)
            sb.AppendLine("- 복원 오류 " + log.puzzleMistakeCount + "회가 행동 기록에 누적되었습니다.");
        int pendingRestore = CountPendingRestore(records);
        if (pendingRestore > 0)
            sb.AppendLine("- 미완성 기억 " + pendingRestore + "개가 아카이브 주변에서 흔들리고 있습니다.");
        if (log.GetTotalInstability() > 0)
            sb.AppendLine("- 기록 불안정도 총합: " + log.GetTotalInstability() + ".");

        return sb.ToString();
    }

    private string GetMemoryStatusLabel(MemoryRecord3D record)
    {
        if (record == null)
            return "[상태 없음]";
        if (!record.restored)
            return "[복원 대기]";
        if (record.decision == MemoryDecision3D.Unchosen)
            return "[선택 대기]";
        return "[처리 완료]";
    }

    private string ObscureTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return "□□□";
        if (title.Length <= 2)
            return "□□□";

        return title.Substring(0, 1) + "□□□" + title.Substring(title.Length - 1, 1);
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
            "\n[선택 결과 미리보기]\n" +
            "보존 -> 원문이 증언으로 남고, 상처도 함께 보존됩니다.\n" +
            "삭제 -> 증언은 공백으로 처리되며, 이 기억은 엔딩에서 말하지 못합니다.\n" +
            "재가공 -> 더 견딜 수 있는 문장으로 바뀌지만 기록 불일치가 남습니다.\n" +
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
        HandlePuzzleClosedBeforeRestore();
        CloseAllMajorPanels();
        LockCursor();
    }

    private void HandlePuzzleClosedBeforeRestore()
    {
        if (puzzlePanel == null || !puzzlePanel.activeSelf)
            return;
        if (currentPuzzleRecord == null || currentPuzzleRecord.memory == null || currentPuzzleRecord.restored)
            return;

        PlayerMemoryLog3D.Ensure().RecordRestoreAbandoned(currentPuzzleRecord.memory);
        ShowToast("복원이 중단되었습니다. 이 기억은 불안정한 상태로 남습니다.");
    }

    // 엔딩 후 타이틀(시작 메뉴)로 복귀. 저장은 유지하므로 이어하기 가능.
    private void ReturnToTitleFromEnding()
    {
        if (MemoryRecyclerMusicManager.Instance != null)
            MemoryRecyclerMusicManager.Instance.ReturnToGameplayMusic();
        CloseAllMajorPanels();
        ShowStartMenu();
    }

    // 엔딩에서 즉시 새 게임 시작. 저장 데이터 삭제.
    private void StartNewGameFromEnding()
    {
        if (MemoryRecyclerMusicManager.Instance != null)
            MemoryRecyclerMusicManager.Instance.ReturnToGameplayMusic();
        if (MemoryManager3D.Instance != null)
            MemoryManager3D.Instance.StartNewGame();
        CloseAllMajorPanels();
        Time.timeScale = 1f;
        LockCursor();
        ShowToast("새 탐사를 시작합니다.");
    }

    // 빌드에서는 Application.Quit, 에디터에서는 안내 토스트만 표시.
    private void QuitFromEnding()
    {
#if UNITY_EDITOR
        ShowToast("에디터에서는 종료할 수 없습니다. Play 버튼을 다시 누르세요.");
#else
        Application.Quit();
#endif
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

    private Slider CreateSlider(Transform parent, string name, Vector2 anchoredPosition, float value, UnityEngine.Events.UnityAction<float> action)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(430f, 36f);
        rect.anchoredPosition = anchoredPosition;

        Slider slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(value);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(go.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.08f, 0.11f, 0.15f, 0.95f);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        SetRect(backgroundRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(0f, -12f));

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        SetRect(fillAreaRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8f, 12f), new Vector2(-8f, -12f));

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.14f, 0.72f, 0.92f, 0.95f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect);

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        SetRect(handleAreaRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(-8f, 0f));

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.82f, 0.96f, 1f, 1f);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(28f, 28f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.onValueChanged.AddListener(action);
        return slider;
    }

    private void SetButtonSize(Button button, Vector2 size)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = size;
    }

    private void SetButtonEnabled(Button button, bool enabled, string label)
    {
        if (button == null)
            return;

        button.interactable = enabled;
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = enabled
                ? new Color(0.16f, 0.21f, 0.29f, 0.96f)
                : new Color(0.08f, 0.10f, 0.13f, 0.78f);
        }

        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = label;
            text.color = enabled ? Color.white : new Color(0.62f, 0.66f, 0.70f);
        }
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
