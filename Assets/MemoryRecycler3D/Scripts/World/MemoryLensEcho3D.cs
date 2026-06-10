using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryLensEcho3D : MonoBehaviour
{
    private static readonly List<MemoryLensEcho3D> Instances = new List<MemoryLensEcho3D>();

    private const float DefaultCanvasScale = 0.0085f;
    private const float MaxVisibleDistance = 55f;

    private MemoryData3D memoryData;
    private Transform anchor;
    private Vector3 cachedAnchorPosition;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image frameImage;
    private Text titleText;
    private Text bodyText;
    private Text hintText;
    private Font uiFont;
    private bool wasVisible;

    // 표시 생명주기는 collected/restored/processed 상태로 결정한다(상호작용 여부로 숨기지 않는다).
    private bool showRestoredWaiting;
    private float solvingFeedbackUntil;
    private string lastStateToken = "";

    public MemoryData3D MemoryData
    {
        get { return memoryData; }
    }

    public static MemoryLensEcho3D EnsureFor(MemoryObject3D memoryObject)
    {
        if (memoryObject == null || memoryObject.memoryData == null)
            return null;
        if (memoryObject.memoryData.puzzleMode == MemoryPuzzleMode3D.Sequence)
            return null;

        MemoryLensEcho3D existing = FindByMemory(memoryObject.memoryData);
        if (existing != null)
        {
            existing.Bind(memoryObject);
            return existing;
        }

        GameObject host = new GameObject("MR3D_LensEcho_" + memoryObject.memoryData.id);
        MemoryLensEcho3D echo = host.AddComponent<MemoryLensEcho3D>();
        echo.Bind(memoryObject);
        return echo;
    }

    public static MemoryLensEcho3D FindByMemory(MemoryData3D memory)
    {
        if (memory == null)
            return null;

        for (int i = 0; i < Instances.Count; i++)
        {
            MemoryLensEcho3D echo = Instances[i];
            if (echo == null || echo.memoryData == null)
                continue;
            if (echo.memoryData == memory || echo.memoryData.id == memory.id)
                return echo;
        }

        return null;
    }

    // 회수/복원/처리 등 상태 변화 시점에 외부에서 호출해 즉시 갱신하고 사유 로그를 남긴다.
    public static void NotifyStateChanged(MemoryData3D memory, string reason)
    {
        MemoryLensEcho3D echo = FindByMemory(memory);
        if (echo == null)
            return;

        MemoryRecord3D record = MemoryManager3D.Instance != null ? MemoryManager3D.Instance.FindRecord(memory) : null;
        bool lensEnabled = UIManager3D.Instance != null && UIManager3D.Instance.IsMemoryLensOverlayActive;
        echo.RefreshState(record, lensEnabled, reason);
    }

    private void Awake()
    {
        if (!Instances.Contains(this))
            Instances.Add(this);

        BuildWorldCanvas();
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
    }

    private void Update()
    {
        ApplyCurrentState(null);
    }

    public void Bind(MemoryObject3D memoryObject)
    {
        if (memoryObject == null)
            return;

        memoryData = memoryObject.memoryData;
        anchor = memoryObject.transform;
        cachedAnchorPosition = anchor != null ? anchor.position : transform.position;
        gameObject.name = "MR3D_LensEcho_" + (memoryData != null ? memoryData.id : "Unknown");
        RefreshText(false, false, 0f);
    }

    public bool TryEvaluateLens(MemoryPuzzleMode3D mode, Camera camera, out float normalizedDistance, out bool occluderHit)
    {
        normalizedDistance = 99f;
        occluderHit = false;
        if (camera == null || canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
            return false;

        Vector3 viewport = camera.WorldToViewportPoint(GetFocusPosition());
        if (viewport.z <= 0f)
            return false;

        normalizedDistance = Vector2.Distance(new Vector2(viewport.x, viewport.y), new Vector2(0.5f, 0.5f));

        if (mode == MemoryPuzzleMode3D.LensOcclude)
            occluderHit = HasCenterStructure(camera);

        return true;
    }

    // 외부에서 명시적으로 상태를 갱신할 때 호출(회수/복원/처리). reason은 로그에만 사용.
    public void RefreshState(MemoryRecord3D record, bool lensEnabled, string reason)
    {
        ApplyState(record, lensEnabled, reason);
    }

    private void ApplyCurrentState(string reason)
    {
        MemoryRecord3D record = MemoryManager3D.Instance != null ? MemoryManager3D.Instance.FindRecord(memoryData) : null;
        bool lensEnabled = UIManager3D.Instance != null && UIManager3D.Instance.IsMemoryLensOverlayActive;
        ApplyState(record, lensEnabled, reason);
    }

    private void ApplyState(MemoryRecord3D record, bool lensEnabled, string reason)
    {
        if (memoryData == null || memoryData.puzzleMode == MemoryPuzzleMode3D.Sequence)
        {
            SetCanvasVisible(false);
            wasVisible = false;
            return;
        }

        bool restored = record != null && record.restored;
        bool processed = record != null && record.decision != MemoryDecision3D.Unchosen;
        MemoryDecision3D decision = record != null ? record.decision : MemoryDecision3D.Unchosen;

        Camera camera = Camera.main;
        bool inRange = camera == null || Vector3.Distance(camera.transform.position, GetAnchorPosition()) <= MaxVisibleDistance;

        // 숨김 조건은 오직 "처리 완료(decision != Unchosen)"와 거리 컬링뿐이다.
        // 회수/카드 열기/퍼즐 진행만으로는 절대 숨기지 않는다.
        bool render;
        string stateToken;
        if (processed)
        {
            render = false;
            stateToken = "processed";
        }
        else if (!inRange)
        {
            render = false;
            stateToken = "culled";
        }
        else
        {
            render = true;
            stateToken = restored ? "restored-waiting" : "active";
        }

        SetCanvasVisible(render);

        if (render)
        {
            showRestoredWaiting = restored;
            UpdatePose(camera);

            // 적극적 렌즈 풀이 피드백(SetLensFeedback)이 방금 호출됐다면 그 표현을 유지한다.
            if (Time.unscaledTime >= solvingFeedbackUntil)
            {
                RefreshText(lensEnabled, false, 0f);
                ApplyAppearance(restored, lensEnabled);
            }
        }
        else
        {
            wasVisible = false;
        }

        LogStateIfChanged(stateToken, restored, decision, reason);
    }

    // 핵심 수정: GameObject를 비활성화하지 않고 Canvas만 켜고 끈다.
    // 예전 코드는 canvas.gameObject.SetActive(false)로 자기 자신을 꺼서 Update가 멈췄고,
    // 한 번 숨겨지면 다시 나타나지 못했다(E 상호작용/이동 후 영구 소멸의 원인).
    private void SetCanvasVisible(bool visible)
    {
        if (canvas != null && canvas.enabled != visible)
            canvas.enabled = visible;
    }

    private void UpdatePose(Camera camera)
    {
        Vector3 targetPosition = GetAnchorPosition() + GetWorldOffset();
        transform.position = Vector3.Lerp(transform.position, targetPosition, wasVisible ? Time.deltaTime * 8f : 1f);
        wasVisible = true;

        if (camera != null)
            transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, Vector3.up);
    }

    private void ApplyAppearance(bool restored, bool lensEnabled)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = restored ? 0.34f : (lensEnabled ? 1f : 0.42f);

        if (frameImage != null)
        {
            frameImage.color = restored
                ? new Color(0.26f, 0.48f, 0.56f, 0.30f)
                : lensEnabled ? new Color(0.16f, 0.68f, 0.9f, 0.54f) : new Color(0.25f, 0.42f, 0.50f, 0.38f);
        }
    }

    private void LogStateIfChanged(string stateToken, bool restored, MemoryDecision3D decision, string reason)
    {
        bool changed = stateToken != lastStateToken;
        if (!changed && string.IsNullOrEmpty(reason))
            return;
        lastStateToken = stateToken;

        string id = SafeId();
        switch (stateToken)
        {
            case "processed":
                Debug.Log("[MR3D LensEcho] hide after processed id=" + id + " decision=" + decision);
                break;
            case "restored-waiting":
                Debug.Log("[MR3D LensEcho] restored, show 복원 완료/처리 대기 id=" + id + " restored=True decision=" + decision);
                break;
            case "culled":
                Debug.Log("[MR3D LensEcho] hide out-of-range id=" + id + " restored=" + restored + " decision=" + decision);
                break;
            default:
                if (!string.IsNullOrEmpty(reason) && reason.Contains("interact"))
                    Debug.Log("[MR3D LensEcho] keep visible on interact id=" + id + " restored=" + restored + " decision=" + decision);
                else
                    Debug.Log("[MR3D LensEcho] show active echo id=" + id + " restored=" + restored + " decision=" + decision + (string.IsNullOrEmpty(reason) ? "" : " reason=" + reason));
                break;
        }
    }

    private string SafeId()
    {
        return memoryData != null && !string.IsNullOrEmpty(memoryData.id) ? memoryData.id : "Unknown";
    }

    public void SetLensFeedback(bool activeLens, bool solving, float progress)
    {
        // 풀이 중에는 이 표현을 잠시(0.2s) 유지해 ApplyState의 기본 표현과 충돌하지 않게 한다.
        solvingFeedbackUntil = Time.unscaledTime + 0.2f;

        if (canvasGroup != null)
            canvasGroup.alpha = activeLens ? 1f : 0.42f;

        if (frameImage != null)
        {
            frameImage.color = solving
                ? new Color(0.42f, 0.92f, 1f, 0.72f)
                : activeLens ? new Color(0.16f, 0.68f, 0.9f, 0.54f) : new Color(0.25f, 0.42f, 0.50f, 0.38f);
        }

        showRestoredWaiting = false;
        RefreshText(activeLens, solving, progress);
    }

    private void BuildWorldCanvas()
    {
        uiFont = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 28);
        if (uiFont == null)
            uiFont = Font.CreateDynamicFontFromOSFont("Arial", 28);

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 8;
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform rootRect = gameObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(520f, 230f);
        transform.localScale = Vector3.one * DefaultCanvasScale;

        frameImage = gameObject.AddComponent<Image>();
        frameImage.color = new Color(0.20f, 0.46f, 0.56f, 0.34f);
        frameImage.raycastTarget = false;

        titleText = CreateWorldText("Title", "", 28, TextAnchor.MiddleLeft, new Color(0.82f, 0.96f, 1f));
        SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(24f, -54f), new Vector2(-24f, -12f));

        bodyText = CreateWorldText("Body", "", 25, TextAnchor.MiddleCenter, new Color(0.94f, 0.97f, 1f));
        bodyText.lineSpacing = 1.1f;
        SetRect(bodyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(28f, 54f), new Vector2(-28f, -62f));

        hintText = CreateWorldText("Hint", "", 18, TextAnchor.MiddleCenter, new Color(0.64f, 0.88f, 0.95f));
        SetRect(hintText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0f), new Vector2(22f, 14f), new Vector2(-22f, 50f));
    }

    private Text CreateWorldText(string name, string text, int size, TextAnchor alignment, Color color)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);
        Text uiText = child.AddComponent<Text>();
        uiText.font = uiFont;
        uiText.text = text;
        uiText.fontSize = size;
        uiText.alignment = alignment;
        uiText.color = color;
        uiText.alignByGeometry = true;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        uiText.raycastTarget = false;
        return uiText;
    }

    private Vector3 GetAnchorPosition()
    {
        if (anchor != null)
        {
            cachedAnchorPosition = anchor.position;
            return anchor.position;
        }

        return cachedAnchorPosition;
    }

    private Vector3 GetFocusPosition()
    {
        return transform.position;
    }

    private Vector3 GetWorldOffset()
    {
        switch (memoryData != null ? memoryData.puzzleMode : MemoryPuzzleMode3D.Sequence)
        {
            case MemoryPuzzleMode3D.LensOcclude:
                return new Vector3(0f, 2.35f, 0f);
            case MemoryPuzzleMode3D.Stillness:
                return new Vector3(0f, 2.05f, 0f);
            default:
                return new Vector3(0f, 2.25f, 0f);
        }
    }

    private bool HasCenterStructure(Camera camera)
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 80f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.transform == null)
            return false;
        if (hit.transform.CompareTag("Player"))
            return false;
        if (hit.transform.GetComponentInParent<MemoryObject3D>() != null)
            return false;

        return true;
    }

    private void RefreshText(bool activeLens, bool solving, float progress)
    {
        if (memoryData == null || titleText == null || bodyText == null || hintText == null)
            return;

        // 복원 완료 후 처리 대기 상태: 작고 흐릿한 "복원 완료 / 처리 대기"로 전환.
        if (showRestoredWaiting)
        {
            titleText.text = GetTitle() + " · 복원 완료";
            bodyText.text = GetBody(true);
            hintText.text = "처리 대기 — 기억 카드에서 보존 / 삭제 / 재가공을 선택하십시오.";
            return;
        }

        titleText.text = GetTitle();
        bodyText.text = GetBody(solving);
        hintText.text = GetHint(activeLens, solving, progress);
    }

    private string GetTitle()
    {
        switch (memoryData.puzzleMode)
        {
            case MemoryPuzzleMode3D.LensAlign:
                return "기억 잔상 - 파란 우산";
            case MemoryPuzzleMode3D.LensOcclude:
                return "기억 잔상 - 지워진 방송";
            case MemoryPuzzleMode3D.Stillness:
                return "기억 잔상 - 빈 파일";
            default:
                return "기억 잔상";
        }
    }

    private string GetBody(bool solving)
    {
        if (memoryData == null)
            return "";

        switch (memoryData.id)
        {
            case "MR3D_002":
                return solving
                    ? "비가 오던 날,\n나는 파란 우산을\n돌아올 사람에게 맡겼다."
                    : "비가 오던 날,\n나는 파란 □□을\n돌아올 사람에게 맡겼다.";
            case "MR3D_003":
                return solving
                    ? "사라진 이름들은\n안내문에 적히지 않았다."
                    : "방송: 모든 시민은\n안전하게 대피했습니다.";
            case "MR3D_006":
                return solving
                    ? "내가 들고 다닌 빈 파일은\n도시의 것이 아니라\n나 자신의 기억이었다."
                    : "빈 파일\n□□□□ □□□ □□□□";
            default:
                return string.IsNullOrEmpty(memoryData.puzzleHint) ? memoryData.memoryTitle : memoryData.puzzleHint;
        }
    }

    private string GetHint(bool activeLens, bool solving, float progress)
    {
        if (!activeLens)
            return "Tab 렌즈를 켜면 잔상이 선명해집니다.";
        if (solving)
            return "복원 중 " + Mathf.Clamp01(progress).ToString("P0");

        switch (memoryData.puzzleMode)
        {
            case MemoryPuzzleMode3D.LensAlign:
                return "중앙 마커와 잔상을 겹치십시오.";
            case MemoryPuzzleMode3D.LensOcclude:
                return "도시 구조물과 잔상을 함께 가리키십시오.";
            case MemoryPuzzleMode3D.Stillness:
                return "바라본 채 움직이지 마십시오.";
            default:
                return "";
        }
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
