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
        RefreshVisibilityAndPose();
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
        if (camera == null || canvas == null || !canvas.gameObject.activeInHierarchy)
            return false;

        Vector3 viewport = camera.WorldToViewportPoint(GetFocusPosition());
        if (viewport.z <= 0f)
            return false;

        normalizedDistance = Vector2.Distance(new Vector2(viewport.x, viewport.y), new Vector2(0.5f, 0.5f));

        if (mode == MemoryPuzzleMode3D.LensOcclude)
            occluderHit = HasCenterStructure(camera);

        return true;
    }

    public void SetLensFeedback(bool activeLens, bool solving, float progress)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = activeLens ? 1f : 0.42f;

        if (frameImage != null)
        {
            frameImage.color = solving
                ? new Color(0.42f, 0.92f, 1f, 0.72f)
                : activeLens ? new Color(0.16f, 0.68f, 0.9f, 0.54f) : new Color(0.25f, 0.42f, 0.50f, 0.38f);
        }

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

    private void RefreshVisibilityAndPose()
    {
        Camera camera = Camera.main;
        bool visible = ShouldShow(camera);
        if (canvas != null && canvas.gameObject.activeSelf != visible)
            canvas.gameObject.SetActive(visible);

        if (!visible)
        {
            wasVisible = false;
            return;
        }

        Vector3 targetPosition = GetAnchorPosition() + GetWorldOffset();
        transform.position = Vector3.Lerp(transform.position, targetPosition, wasVisible ? Time.deltaTime * 8f : 1f);
        wasVisible = true;

        if (camera != null)
            transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, Vector3.up);

        bool lensActive = UIManager3D.Instance != null && UIManager3D.Instance.IsMemoryLensOverlayActive;
        if (canvasGroup != null && !lensActive)
            canvasGroup.alpha = 0.42f;
    }

    private bool ShouldShow(Camera camera)
    {
        if (memoryData == null || memoryData.puzzleMode == MemoryPuzzleMode3D.Sequence)
            return false;

        MemoryRecord3D record = MemoryManager3D.Instance != null ? MemoryManager3D.Instance.FindRecord(memoryData) : null;
        if (record != null && record.restored)
            return false;

        if (camera == null)
            return true;

        return Vector3.Distance(camera.transform.position, GetAnchorPosition()) <= MaxVisibleDistance;
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
