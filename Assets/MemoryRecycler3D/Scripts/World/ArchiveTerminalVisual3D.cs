using UnityEngine;
using UnityEngine.UI;

// 중앙 아카이브 터미널을 "기억 심문 렌즈/터미널"처럼 보이게 하는 런타임 장식 빌더.
// 씬을 수정하지 않고 MR3D_Bootstrap이 Play 시작 시 자동 생성한다.
// 장식 전용이므로 모든 콜라이더를 제거해 플레이어 이동/접지/물리에 영향을 주지 않는다.
public class ArchiveTerminalVisual3D : MonoBehaviour
{
    public static ArchiveTerminalVisual3D Instance { get; private set; }

    private const float CanvasScale = 0.0105f;

    private Transform target;
    private bool built;

    private Light beaconLight;
    private float beaconBaseIntensity = 3.0f;
    private Transform lensCanvasTransform;
    private CanvasGroup lensCanvasGroup;
    private Image lensPanelImage;
    private Material accentMaterial;
    private Font uiFont;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Initialize(Transform archiveTarget)
    {
        if (built)
            return;

        target = archiveTarget;
        if (target == null)
        {
            Debug.LogWarning("[MR3D ArchiveVisual] no archive target; visual not built");
            return;
        }

        BuildStructure();
        BuildLensCanvas();
        built = true;
        Debug.Log("[MR3D ArchiveVisual] central archive terminal visual built at " + target.position);
    }

    private void Update()
    {
        if (!built)
            return;

        bool lensActive = UIManager3D.Instance != null && UIManager3D.Instance.IsMemoryLensOverlayActive;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.6f);

        if (beaconLight != null)
            beaconLight.intensity = beaconBaseIntensity + pulse * 0.7f + (lensActive ? 1.4f : 0f);

        // 렌즈 패널은 항상 카메라를 바라보고(빌보드), Tab 렌즈 ON 시 더 선명해진다.
        if (lensCanvasTransform != null && Camera.main != null)
        {
            lensCanvasTransform.rotation = Quaternion.LookRotation(
                lensCanvasTransform.position - Camera.main.transform.position, Vector3.up);
        }

        if (lensCanvasGroup != null)
            lensCanvasGroup.alpha = lensActive ? 1f : 0.82f;

        if (lensPanelImage != null)
        {
            float a = (lensActive ? 0.30f : 0.18f) + pulse * 0.04f;
            lensPanelImage.color = new Color(0.10f, 0.55f, 0.66f, a);
        }

        if (accentMaterial != null)
        {
            float e = (lensActive ? 2.4f : 1.3f) + pulse * 0.5f;
            accentMaterial.SetColor("_EmissionColor", new Color(0.18f, 0.95f, 0.92f) * e);
        }
    }

    private void BuildStructure()
    {
        Vector3 origin = target.position;

        Material trimMat = MakeMaterial(new Color(0.10f, 0.16f, 0.19f), new Color(0.06f, 0.32f, 0.34f) * 0.6f);
        accentMaterial = MakeMaterial(new Color(0.12f, 0.5f, 0.55f), new Color(0.18f, 0.95f, 0.92f) * 1.3f);
        Material lensBackingMat = MakeMaterial(new Color(0.03f, 0.08f, 0.10f), new Color(0.05f, 0.22f, 0.26f) * 0.5f);

        // 좌우 세로 기둥
        CreateBlock("ArchiveVisual_PillarL", origin + new Vector3(-2.35f, 0f, -0.15f), new Vector3(0.2f, 8.2f, 0.2f), trimMat);
        CreateBlock("ArchiveVisual_PillarR", origin + new Vector3(2.35f, 0f, -0.15f), new Vector3(0.2f, 8.2f, 0.2f), trimMat);

        // 상단 가로 프레임
        CreateBlock("ArchiveVisual_TopBeam", origin + new Vector3(0f, 3.95f, -0.15f), new Vector3(5.1f, 0.26f, 0.28f), trimMat);
        // 하단 받침
        CreateBlock("ArchiveVisual_BaseBeam", origin + new Vector3(0f, -3.6f, -0.2f), new Vector3(5.1f, 0.3f, 0.5f), trimMat);

        // 중앙 렌즈 백킹(반투명 유리판 뒤의 어두운 면) - 터미널 앞면(z 약 -0.8) 바깥으로 띄운다.
        CreateBlock("ArchiveVisual_LensBacking", origin + new Vector3(0f, 0.7f, -0.95f), new Vector3(3.5f, 2.5f, 0.08f), lensBackingMat);

        // 중앙 세로 발광 라인
        CreateBlock("ArchiveVisual_AccentLine", origin + new Vector3(0f, 0.4f, -1.0f), new Vector3(0.08f, 6.6f, 0.05f), accentMaterial);
        // 좌우 발광 라인
        CreateBlock("ArchiveVisual_AccentL", origin + new Vector3(-1.55f, 0.4f, -0.99f), new Vector3(0.05f, 5.2f, 0.05f), accentMaterial);
        CreateBlock("ArchiveVisual_AccentR", origin + new Vector3(1.55f, 0.4f, -0.99f), new Vector3(0.05f, 5.2f, 0.05f), accentMaterial);

        // 청록 비콘 라이트
        GameObject lightObject = new GameObject("ArchiveVisual_BeaconLight");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.position = origin + new Vector3(0f, 2.6f, -2.0f);
        beaconLight = lightObject.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.range = 18f;
        beaconLight.intensity = beaconBaseIntensity;
        beaconLight.color = new Color(0.16f, 0.95f, 0.86f);
    }

    private void BuildLensCanvas()
    {
        uiFont = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 40);
        if (uiFont == null)
            uiFont = Font.CreateDynamicFontFromOSFont("Arial", 40);

        GameObject canvasObject = new GameObject("ArchiveVisual_LensCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.position = target.position + new Vector3(0f, 0.9f, -1.2f);
        canvasObject.transform.localScale = Vector3.one * CanvasScale;
        lensCanvasTransform = canvasObject.transform;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 6;
        lensCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        lensCanvasGroup.interactable = false;
        lensCanvasGroup.blocksRaycasts = false;

        RectTransform rootRect = canvasObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(300f, 210f);

        lensPanelImage = canvasObject.AddComponent<Image>();
        lensPanelImage.color = new Color(0.10f, 0.55f, 0.66f, 0.18f);
        lensPanelImage.raycastTarget = false;

        Text title = CreateCanvasText(canvasObject.transform, "Title", "CENTRAL ARCHIVE", 34, FontStyle.Bold, new Color(0.78f, 0.98f, 1f), TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -64f), new Vector2(-12f, -10f));

        Text subtitle = CreateCanvasText(canvasObject.transform, "Subtitle", "기억 심문 터미널", 26, FontStyle.Normal, new Color(0.86f, 0.96f, 1f), TextAnchor.MiddleCenter);
        SetRect(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(12f, -6f), new Vector2(-12f, 42f));

        Text note = CreateCanvasText(canvasObject.transform, "Note", "처리 완료 3개 이상 접속 가능", 18, FontStyle.Normal, new Color(0.62f, 0.86f, 0.92f), TextAnchor.MiddleCenter);
        SetRect(note.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(12f, 16f), new Vector2(-12f, 64f));
    }

    private Text CreateCanvasText(Transform parent, string name, string text, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text uiText = go.AddComponent<Text>();
        uiText.font = uiFont;
        uiText.text = text;
        uiText.fontSize = size;
        uiText.fontStyle = style;
        uiText.alignment = anchor;
        uiText.color = color;
        uiText.alignByGeometry = true;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        uiText.raycastTarget = false;
        return uiText;
    }

    private void CreateBlock(string name, Vector3 worldPosition, Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;

        // 장식 전용: 콜라이더 제거(플레이어 이동/접지/물리 비간섭).
        Collider collider = block.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        block.transform.SetParent(transform, false);
        block.transform.position = worldPosition;
        block.transform.localScale = scale;

        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
    }

    private static Material MakeMaterial(Color baseColor, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material m = new Material(shader);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", baseColor);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", baseColor);
        m.color = baseColor;

        if (emission.maxColorComponent > 0f)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor"))
                m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        return m;
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
