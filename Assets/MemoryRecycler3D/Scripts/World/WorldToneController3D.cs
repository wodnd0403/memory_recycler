using System.Collections.Generic;
using UnityEngine;

public class WorldToneController3D : MonoBehaviour
{
    public static WorldToneController3D Instance { get; private set; }

    [Header("References")]
    public Light sunLight;

    [Header("World Memory Tone")]
    public Color preservedFog = new Color(0.35f, 0.42f, 0.55f);
    public Color deletedFog = new Color(0.12f, 0.12f, 0.14f);
    public Color neutralFog = new Color(0.22f, 0.24f, 0.28f);

    [Header("Day & Night")]
    public bool enableDayNightCycle = true;
    public float fullDayDurationSeconds = 180f;
    [Range(0f, 1f)] public float startTimeNormalized = 0.23f;

    [Header("Night Sky")]
    public bool enableNightSky = true;
    public int starCount = 150;
    public float starDomeRadius = 56f;
    public Color starColor = new Color(0.72f, 0.86f, 1f);
    public Color moonColor = new Color(0.70f, 0.80f, 1f);

    [Header("Apocalypse Atmosphere")]
    public bool enableApocalypseCityPass = true;

    private float timeOfDayNormalized;
    private readonly List<Light> streetLights = new List<Light>();
    private readonly List<Transform> stars = new List<Transform>();
    private readonly List<float> starBaseScales = new List<float>();

    private Transform nightSkyRoot;
    private Transform moonTransform;
    private Renderer moonRenderer;
    private Light moonLight;
    private Material starMaterial;
    private Material moonMaterial;
    private Material crackedConcreteMaterial;
    private Material sootMaterial;
    private Material rustMaterial;
    private Material deadWindowMaterial;
    private bool cityDecorated;

    public float TimeOfDayNormalized => timeOfDayNormalized;

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
        timeOfDayNormalized = startTimeNormalized;
        CacheStreetLights();
        if (enableNightSky)
            EnsureNightSky();
        if (enableApocalypseCityPass)
            DecorateApocalypticCity();
        RefreshWorldTone();
    }

    private void Update()
    {
        if (!enableDayNightCycle)
            return;

        if (fullDayDurationSeconds <= 1f)
            fullDayDurationSeconds = 1f;

        timeOfDayNormalized += Time.deltaTime / fullDayDurationSeconds;
        if (timeOfDayNormalized > 1f)
            timeOfDayNormalized -= 1f;

        RefreshWorldTone();
    }

    public void RefreshWorldTone()
    {
        float memoryT = 0.5f;
        if (GameState3D.Instance != null)
        {
            float tone = Mathf.Clamp(GameState3D.Instance.worldToneValue, -5f, 5f);
            memoryT = Mathf.InverseLerp(-5f, 5f, tone);
        }

        float solarDot = Mathf.Sin(timeOfDayNormalized * Mathf.PI * 2f - Mathf.PI * 0.5f);
        float daylight = Mathf.Clamp01((solarDot + 0.12f) / 1.12f);
        float night = 1f - daylight;

        RenderSettings.fog = true;
        Color memoryFog = Color.Lerp(deletedFog, preservedFog, memoryT);
        Color timeFog = Color.Lerp(new Color(0.04f, 0.055f, 0.08f), new Color(0.46f, 0.52f, 0.60f), daylight);
        RenderSettings.fogColor = Color.Lerp(memoryFog, timeFog, 0.55f);
        RenderSettings.fogDensity = Mathf.Lerp(0.038f, 0.02f, daylight) * Mathf.Lerp(1.22f, 0.92f, memoryT);

        Color nightAmbient = new Color(0.035f, 0.05f, 0.075f);
        Color dayAmbient = new Color(0.24f, 0.25f, 0.24f);
        Color memoryAmbient = Color.Lerp(new Color(0.08f, 0.08f, 0.09f), new Color(0.25f, 0.28f, 0.34f), memoryT);
        RenderSettings.ambientLight = Color.Lerp(memoryAmbient, Color.Lerp(nightAmbient, dayAmbient, daylight), 0.6f);

        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(timeOfDayNormalized * 360f - 90f, -28f, 0f);
            sunLight.intensity = Mathf.Lerp(0.02f, 1.05f, daylight) * Mathf.Lerp(0.85f, 1.15f, memoryT);
            Color sunrise = new Color(1.0f, 0.68f, 0.48f);
            Color midday = new Color(0.93f, 0.96f, 1.0f);
            Color moonlight = new Color(0.30f, 0.38f, 0.58f);
            Color dayColor = Color.Lerp(sunrise, midday, daylight);
            sunLight.color = Color.Lerp(moonlight, dayColor, daylight);
        }

        float streetLightIntensity = Mathf.Lerp(1.15f, 0.05f, daylight);
        for (int i = 0; i < streetLights.Count; i++)
        {
            if (streetLights[i] == null)
                continue;

            streetLights[i].intensity = streetLightIntensity;
            streetLights[i].enabled = streetLightIntensity > 0.08f;
        }

        if (enableNightSky)
            RefreshNightSky(night, memoryT);
    }

    public string GetTimePeriodLabel()
    {
        if (timeOfDayNormalized < 0.21f)
            return "새벽";
        if (timeOfDayNormalized < 0.46f)
            return "낮";
        if (timeOfDayNormalized < 0.71f)
            return "저녁";
        return "밤";
    }

    public string GetClockString()
    {
        float totalHours = timeOfDayNormalized * 24f;
        int hour = Mathf.FloorToInt(totalHours);
        int minute = Mathf.FloorToInt((totalHours - hour) * 60f);
        return hour.ToString("00") + ":" + minute.ToString("00");
    }

    private void CacheStreetLights()
    {
        streetLights.Clear();
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < allLights.Length; i++)
        {
            Light light = allLights[i];
            if (light == null || light == sunLight)
                continue;

            string parentName = light.transform.parent != null ? light.transform.parent.name : string.Empty;
            if (parentName.StartsWith("Broken Streetlight") || light.name.Contains("Streetlight"))
                streetLights.Add(light);
        }
    }

    private void EnsureNightSky()
    {
        if (nightSkyRoot != null)
            return;

        RemoveDuplicateSceneRoots("Night Sky Celestials");

        GameObject root = new GameObject("Night Sky Celestials");
        nightSkyRoot = root.transform;
        nightSkyRoot.position = Vector3.zero;

        starMaterial = CreateRuntimeMaterial("MR3D_Runtime_Star", starColor, true);
        moonMaterial = CreateRuntimeMaterial("MR3D_Runtime_Moon", moonColor, true);

        stars.Clear();
        starBaseScales.Clear();

        for (int i = 0; i < starCount; i++)
        {
            float azimuth = Range(i, 0.13f, 0f, Mathf.PI * 2f);
            float elevation = Range(i, 2.37f, 0.28f, 1.18f);
            float radius = Range(i, 4.91f, starDomeRadius * 0.76f, starDomeRadius);
            Vector3 direction = new Vector3(
                Mathf.Cos(elevation) * Mathf.Cos(azimuth),
                Mathf.Sin(elevation),
                Mathf.Cos(elevation) * Mathf.Sin(azimuth));

            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Night Star_" + i;
            star.transform.SetParent(nightSkyRoot);
            star.transform.localPosition = direction * radius;

            float scale = Range(i, 7.19f, 0.045f, 0.13f);
            star.transform.localScale = Vector3.one * scale;
            starBaseScales.Add(scale);
            stars.Add(star.transform);

            Renderer renderer = star.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = starMaterial;

            Collider collider = star.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
        }

        GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Moon";
        moon.transform.SetParent(nightSkyRoot);
        moon.transform.localScale = Vector3.one * 3.2f;
        moonRenderer = moon.GetComponent<Renderer>();
        if (moonRenderer != null)
            moonRenderer.sharedMaterial = moonMaterial;
        Collider moonCollider = moon.GetComponent<Collider>();
        if (moonCollider != null)
            Destroy(moonCollider);
        moonTransform = moon.transform;

        GameObject moonLightObject = new GameObject("Moon Light");
        moonLightObject.transform.SetParent(nightSkyRoot);
        moonLight = moonLightObject.AddComponent<Light>();
        moonLight.type = LightType.Directional;
        moonLight.intensity = 0f;
        moonLight.color = moonColor;
        moonLight.shadows = LightShadows.Soft;
    }

    private void RefreshNightSky(float night, float memoryT)
    {
        EnsureNightSky();

        float visibility = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 0.98f, night));
        nightSkyRoot.gameObject.SetActive(visibility > 0.03f);
        if (visibility <= 0.03f)
            return;

        float solarDot = Mathf.Sin(timeOfDayNormalized * Mathf.PI * 2f - Mathf.PI * 0.5f);
        float moonPath = timeOfDayNormalized * Mathf.PI * 2f + 0.55f;
        Vector3 moonDirection = new Vector3(
            Mathf.Cos(moonPath) * 0.48f,
            Mathf.Lerp(0.42f, 1f, Mathf.Clamp01(-solarDot)),
            Mathf.Sin(moonPath) * 0.48f).normalized;

        moonTransform.position = moonDirection * (starDomeRadius * 0.72f);
        float moonScale = Mathf.Lerp(2.5f, 3.6f, visibility);
        moonTransform.localScale = Vector3.one * moonScale;

        Color visibleMoon = Color.Lerp(new Color(0.42f, 0.48f, 0.62f), moonColor, visibility);
        SetMaterialColor(moonMaterial, visibleMoon * Mathf.Lerp(0.75f, 1.3f, visibility));
        SetEmissionColor(moonMaterial, visibleMoon * Mathf.Lerp(1.5f, 3.2f, visibility));

        Color visibleStar = Color.Lerp(new Color(0.38f, 0.48f, 0.62f), starColor, visibility);
        SetMaterialColor(starMaterial, visibleStar);
        SetEmissionColor(starMaterial, visibleStar * Mathf.Lerp(1.8f, 4.2f, visibility));

        for (int i = 0; i < stars.Count; i++)
        {
            if (stars[i] == null)
                continue;

            float twinkle = Mathf.Lerp(0.72f, 1.18f, Noise01(i, Time.time * 0.22f));
            stars[i].localScale = Vector3.one * starBaseScales[i] * visibility * twinkle;
        }

        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.LookRotation(-moonDirection, Vector3.up);
            moonLight.intensity = Mathf.Lerp(0f, 0.34f, visibility) * Mathf.Lerp(0.92f, 1.12f, memoryT);
            moonLight.color = Color.Lerp(new Color(0.26f, 0.34f, 0.55f), moonColor, visibility);
        }
    }

    private void DecorateApocalypticCity()
    {
        if (cityDecorated)
            return;

        cityDecorated = true;
        CleanupApocalypticCityDecorations();

        crackedConcreteMaterial = CreateRuntimeMaterial("MR3D_Runtime_CrackedConcrete", new Color(0.18f, 0.18f, 0.16f), false);
        sootMaterial = CreateRuntimeMaterial("MR3D_Runtime_Soot", new Color(0.025f, 0.027f, 0.026f), false);
        rustMaterial = CreateRuntimeMaterial("MR3D_Runtime_Rust", new Color(0.34f, 0.15f, 0.07f), false);
        deadWindowMaterial = CreateRuntimeMaterial("MR3D_Runtime_DeadWindow", new Color(0.018f, 0.024f, 0.028f), false);

        GameObject city = GameObject.Find("Silent City");
        if (city == null)
            return;

        int buildingIndex = 0;
        foreach (Transform child in city.transform)
        {
            if (child == null || !child.name.StartsWith("Silent Building"))
                continue;

            ApplyBuildingColorVariation(child, buildingIndex);
            DecorateBuilding(child, buildingIndex);
            DarkenBrokenWindows(child, buildingIndex);
            buildingIndex++;
        }
    }

    private void CleanupApocalypticCityDecorations()
    {
        string[] generatedNames =
        {
            "Soot Rain Stain",
            "Exposed Concrete Patch",
            "Hairline Crack",
            "Rust Exposed Edge",
            "Fresh Facade Rubble",
            "Broken Glass Slash"
        };

        for (int i = 0; i < generatedNames.Length; i++)
        {
            GameObject[] objects = FindSceneObjectsByName(generatedNames[i]);
            for (int j = 0; j < objects.Length; j++)
            {
                if (objects[j] != null)
                    Destroy(objects[j]);
            }
        }
    }

    private void ApplyBuildingColorVariation(Transform building, int index)
    {
        Renderer renderer = building.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Color concrete = Color.Lerp(new Color(0.13f, 0.135f, 0.125f), new Color(0.21f, 0.20f, 0.18f), Noise01(index, 3.3f));
        Color ashGreen = new Color(0.11f, 0.13f, 0.12f);
        Color color = Color.Lerp(concrete, ashGreen, Noise01(index, 8.8f) * 0.42f);

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        renderer.SetPropertyBlock(block);
    }

    private void DecorateBuilding(Transform building, int index)
    {
        Vector3 scale = building.localScale;

        for (int i = 0; i < 4; i++)
        {
            float x = Range(index, i + 1.1f, -scale.x * 0.38f, scale.x * 0.38f);
            float y = Range(index, i + 2.4f, -scale.y * 0.18f, scale.y * 0.34f);
            float height = Range(index, i + 3.9f, scale.y * 0.22f, scale.y * 0.48f);
            float width = Range(index, i + 5.2f, 0.045f, 0.09f);
            CreateFacadeBox(building, "Soot Rain Stain", new Vector3(x, y, scale.z * 0.5f + 0.065f), new Vector3(width, height, 0.035f), sootMaterial, Range(index, i + 8.1f, -4f, 4f));
        }

        for (int i = 0; i < 3; i++)
        {
            float x = Range(index, i + 11.2f, -scale.x * 0.36f, scale.x * 0.36f);
            float y = Range(index, i + 12.7f, -scale.y * 0.22f, scale.y * 0.38f);
            Vector3 size = new Vector3(Range(index, i + 13.6f, 0.5f, 1.25f), Range(index, i + 14.5f, 0.18f, 0.48f), 0.04f);
            CreateFacadeBox(building, "Exposed Concrete Patch", new Vector3(x, y, scale.z * 0.5f + 0.07f), size, crackedConcreteMaterial, Range(index, i + 15.9f, -8f, 8f));
        }

        for (int i = 0; i < 5; i++)
        {
            float x = Range(index, i + 21.5f, -scale.x * 0.44f, scale.x * 0.44f);
            float y = Range(index, i + 22.6f, -scale.y * 0.28f, scale.y * 0.43f);
            float length = Range(index, i + 23.7f, 0.55f, 1.6f);
            CreateFacadeBox(building, "Hairline Crack", new Vector3(x, y, scale.z * 0.5f + 0.085f), new Vector3(0.035f, length, 0.035f), sootMaterial, Range(index, i + 24.8f, -35f, 35f));
        }

        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            CreateFacadeBox(building, "Rust Exposed Edge", new Vector3(side * (scale.x * 0.5f + 0.075f), Range(index, i + 31.3f, -scale.y * 0.15f, scale.y * 0.2f), 0f), new Vector3(0.065f, scale.y * Range(index, i + 32.4f, 0.32f, 0.66f), 0.08f), rustMaterial, 0f);
        }

        for (int i = 0; i < 3; i++)
        {
            GameObject rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rubble.name = "Fresh Facade Rubble";
            rubble.transform.SetParent(building.parent);
            float side = Range(index, i + 40.2f, 0f, 1f) > 0.5f ? -1f : 1f;
            rubble.transform.position = building.position + new Vector3(side * Range(index, i + 41.1f, scale.x * 0.28f, scale.x * 0.58f), Range(index, i + 42.7f, 0.08f, 0.28f), Range(index, i + 43.8f, -scale.z * 0.35f, scale.z * 0.35f));
            rubble.transform.rotation = Quaternion.Euler(Range(index, i + 44.4f, -8f, 8f), Range(index, i + 45.5f, 0f, 180f), Range(index, i + 46.6f, -18f, 18f));
            rubble.transform.localScale = new Vector3(Range(index, i + 47.1f, 0.25f, 0.7f), Range(index, i + 48.2f, 0.12f, 0.34f), Range(index, i + 49.3f, 0.25f, 0.8f));
            Renderer rubbleRenderer = rubble.GetComponent<Renderer>();
            if (rubbleRenderer != null)
                rubbleRenderer.sharedMaterial = crackedConcreteMaterial;
        }
    }

    private void DarkenBrokenWindows(Transform building, int index)
    {
        int windowIndex = 0;
        foreach (Transform child in building)
        {
            if (child == null || !child.name.Contains("Window"))
                continue;

            float chance = Noise01(index * 37 + windowIndex, 5.5f);
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null && chance < 0.46f)
                renderer.sharedMaterial = deadWindowMaterial;

            if (chance < 0.24f)
            {
                CreateChildBox(child, "Broken Glass Slash", new Vector3(0f, 0f, -0.72f), new Vector3(0.08f, 0.95f, 0.08f), sootMaterial, Range(index, windowIndex + 2.2f, -28f, 28f));
            }

            windowIndex++;
        }
    }

    private void CreateFacadeBox(Transform building, string name, Vector3 worldOffset, Vector3 worldScale, Material material, float zRotation)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(building);
        SetChildWorldBox(box.transform, building.localScale, worldOffset, worldScale);
        box.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider = box.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private void CreateChildBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, float zRotation)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.localPosition = localPosition;
        box.transform.localScale = localScale;
        box.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider = box.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private void SetChildWorldBox(Transform child, Vector3 parentScale, Vector3 worldOffset, Vector3 worldScale)
    {
        child.localPosition = new Vector3(worldOffset.x / parentScale.x, worldOffset.y / parentScale.y, worldOffset.z / parentScale.z);
        child.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, worldScale.z / parentScale.z);
    }

    private Material CreateRuntimeMaterial(string name, Color color, bool emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Diffuse"));
        material.name = name;
        SetMaterialColor(material, color);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", emission ? 0.35f : 0.04f);

        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            SetEmissionColor(material, color * 2.4f);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            SetEmissionColor(material, Color.black);
        }

        return material;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private void SetEmissionColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", color);
    }

    private float Range(int seed, float salt, float min, float max)
    {
        return Mathf.Lerp(min, max, Noise01(seed, salt));
    }

    private float Noise01(int seed, float salt)
    {
        return Mathf.Repeat(Mathf.Sin(seed * 12.9898f + salt * 78.233f) * 43758.5453f, 1f);
    }

    private void RemoveDuplicateSceneRoots(string objectName)
    {
        GameObject[] objects = FindSceneObjectsByName(objectName);
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                Destroy(objects[i]);
        }
    }

    private GameObject[] FindSceneObjectsByName(string objectName)
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<GameObject> matches = new List<GameObject>();

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i] != null && allObjects[i].name == objectName)
                matches.Add(allObjects[i]);
        }

        return matches.ToArray();
    }
}
