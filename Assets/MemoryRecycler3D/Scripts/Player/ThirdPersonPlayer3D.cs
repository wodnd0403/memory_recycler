using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonPlayer3D : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float rotationSpeed = 12f;
    public float gravity = -20f;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Procedural Animation")]
    public float walkAnimationSpeed = 7.5f;
    public float runAnimationSpeed = 11.5f;
    public float walkLimbAngle = 32f;
    public float runLimbAngle = 54f;
    public float bodyBobAmount = 0.055f;
    public float runBodyBobAmount = 0.11f;
    public float animationSmooth = 10f;

    [Header("Visual Refinement")]
    public bool refineHumanSilhouette = true;

    private CharacterController controller;
    private float verticalVelocity;

    private Transform visualRoot;
    private Transform torso;
    private Transform head;
    private Transform armL;
    private Transform forearmL;
    private Transform armR;
    private Transform forearmR;
    private Transform legL;
    private Transform legR;
    private Transform bootL;
    private Transform bootR;
    private Transform handL;
    private Transform handR;
    private Transform kneePadL;
    private Transform kneePadR;
    private Transform coat;
    private Transform coatSkirt;
    private Transform backpack;

    private Material casualJacketMaterial;
    private Material casualShirtMaterial;
    private Material casualPantsMaterial;
    private Material casualShoeMaterial;
    private Material casualSkinMaterial;

    private readonly Dictionary<Transform, Quaternion> defaultRotations = new Dictionary<Transform, Quaternion>();
    private readonly Dictionary<Transform, Vector3> defaultLocalPositions = new Dictionary<Transform, Vector3>();
    private Vector3 visualRootDefaultLocalPos;
    private float animationTime;
    private bool isRunning;
    private float moveBlend;
    private Vector3 lastPlanarMove;

    public bool IsMoving { get; private set; }
    public bool IsRunning => isRunning && IsMoving;
    public float MoveBlend => moveBlend;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (refineHumanSilhouette)
            RefineHumanSilhouette();

        CacheAnimationRig();
    }

    private void Update()
    {
        Move();
        UpdateAnimation();

        if (Input.GetKeyDown(KeyCode.Tab) && UIManager3D.Instance != null)
            UIManager3D.Instance.ToggleArchive();

        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 moveDirection = Vector3.zero;

        if (input.magnitude >= 0.1f)
        {
            Vector3 cameraForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            moveDirection = cameraForward * input.z + cameraRight * input.x;
            moveDirection.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        isRunning = Input.GetKey(KeyCode.LeftShift) && input.magnitude >= 0.1f;
        float speed = isRunning ? runSpeed : walkSpeed;
        Vector3 finalMove = moveDirection * speed;
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);

        lastPlanarMove = new Vector3(moveDirection.x, 0f, moveDirection.z) * speed;
        IsMoving = input.magnitude >= 0.1f && controller.isGrounded;
    }

    private void CacheAnimationRig()
    {
        visualRoot = FindDeepChild(transform, "RecyclerVisual");
        if (visualRoot == null)
            return;

        torso = FindDeepChild(visualRoot, "Torso");
        head = FindDeepChild(visualRoot, "Head");
        armL = FindDeepChild(visualRoot, "Arm_L");
        forearmL = FindDeepChild(visualRoot, "Forearm_L");
        armR = FindDeepChild(visualRoot, "Arm_R");
        forearmR = FindDeepChild(visualRoot, "Forearm_R");
        legL = FindDeepChild(visualRoot, "Leg_L");
        legR = FindDeepChild(visualRoot, "Leg_R");
        bootL = FindDeepChild(visualRoot, "Boot_L");
        bootR = FindDeepChild(visualRoot, "Boot_R");
        handL = FindDeepChild(visualRoot, "Hand_L");
        handR = FindDeepChild(visualRoot, "Hand_R");
        kneePadL = FindDeepChild(visualRoot, "Knee Pad_L");
        kneePadR = FindDeepChild(visualRoot, "Knee Pad_R");
        coat = FindDeepChild(visualRoot, "Coat");
        coatSkirt = FindDeepChild(visualRoot, "Coat Skirt");
        backpack = FindDeepChild(visualRoot, "Memory Pack");

        visualRootDefaultLocalPos = visualRoot.localPosition;
        CacheDefaultPose(torso);
        CacheDefaultPose(head);
        CacheDefaultPose(armL);
        CacheDefaultPose(forearmL);
        CacheDefaultPose(armR);
        CacheDefaultPose(forearmR);
        CacheDefaultPose(legL);
        CacheDefaultPose(legR);
        CacheDefaultPose(bootL);
        CacheDefaultPose(bootR);
        CacheDefaultPose(handL);
        CacheDefaultPose(handR);
        CacheDefaultPose(kneePadL);
        CacheDefaultPose(kneePadR);
        CacheDefaultPose(coat);
        CacheDefaultPose(coatSkirt);
        CacheDefaultPose(backpack);
    }

    private void CacheDefaultPose(Transform t)
    {
        if (t != null && !defaultRotations.ContainsKey(t))
            defaultRotations.Add(t, t.localRotation);
        if (t != null && !defaultLocalPositions.ContainsKey(t))
            defaultLocalPositions.Add(t, t.localPosition);
    }

    private void UpdateAnimation()
    {
        if (visualRoot == null)
            return;

        float targetBlend = IsMoving ? (isRunning ? 1f : 0.55f) : 0f;
        moveBlend = Mathf.Lerp(moveBlend, targetBlend, Time.deltaTime * animationSmooth);

        if (moveBlend > 0.01f)
        {
            float animSpeed = Mathf.Lerp(walkAnimationSpeed, runAnimationSpeed, isRunning ? 1f : 0f);
            animationTime += Time.deltaTime * animSpeed;
        }
        else
        {
            animationTime = Mathf.Lerp(animationTime, 0f, Time.deltaTime * 2f);
        }

        float stride = Mathf.Sin(animationTime);
        float counterStride = Mathf.Sin(animationTime + Mathf.PI);
        float armAngle = Mathf.Lerp(walkLimbAngle, runLimbAngle, isRunning ? 1f : 0f) * moveBlend;
        float legAngle = Mathf.Lerp(walkLimbAngle, runLimbAngle, isRunning ? 1f : 0f) * moveBlend;
        float bob = Mathf.Lerp(bodyBobAmount, runBodyBobAmount, isRunning ? 1f : 0f) * moveBlend;
        float sway = Mathf.Sin(animationTime * 0.5f) * 5.5f * moveBlend;
        float armReach = Mathf.Lerp(0.045f, 0.09f, isRunning ? 1f : 0f) * moveBlend;
        float stepReach = Mathf.Lerp(0.055f, 0.12f, isRunning ? 1f : 0f) * moveBlend;
        float footLift = Mathf.Lerp(0.055f, 0.13f, isRunning ? 1f : 0f) * moveBlend;
        float elbowBend = Mathf.Lerp(12f, 24f, isRunning ? 1f : 0f) * moveBlend;

        SetLocalRotation(armL, GetDefaultRotation(armL) * Quaternion.Euler(stride * armAngle, 0f, -9f * moveBlend + Mathf.Abs(stride) * 4f * moveBlend));
        SetLocalRotation(forearmL, GetDefaultRotation(forearmL) * Quaternion.Euler(stride * armAngle * 0.72f - Mathf.Abs(stride) * elbowBend, 0f, -5f * moveBlend));
        SetLocalRotation(armR, GetDefaultRotation(armR) * Quaternion.Euler(counterStride * armAngle, 0f, 9f * moveBlend - Mathf.Abs(counterStride) * 4f * moveBlend));
        SetLocalRotation(forearmR, GetDefaultRotation(forearmR) * Quaternion.Euler(counterStride * armAngle * 0.72f - Mathf.Abs(counterStride) * elbowBend, 0f, 5f * moveBlend));
        SetLocalRotation(legL, GetDefaultRotation(legL) * Quaternion.Euler(counterStride * legAngle, 0f, -2f * moveBlend));
        SetLocalRotation(legR, GetDefaultRotation(legR) * Quaternion.Euler(stride * legAngle, 0f, 2f * moveBlend));
        SetLocalRotation(bootL, GetDefaultRotation(bootL) * Quaternion.Euler(counterStride * legAngle * 0.35f + Mathf.Max(0f, counterStride) * 14f * moveBlend, 0f, 0f));
        SetLocalRotation(bootR, GetDefaultRotation(bootR) * Quaternion.Euler(stride * legAngle * 0.35f + Mathf.Max(0f, stride) * 14f * moveBlend, 0f, 0f));
        SetLocalRotation(handL, GetDefaultRotation(handL) * Quaternion.Euler(stride * armAngle * 0.55f, 0f, -8f * moveBlend));
        SetLocalRotation(handR, GetDefaultRotation(handR) * Quaternion.Euler(counterStride * armAngle * 0.55f, 0f, 8f * moveBlend));
        SetLocalRotation(kneePadL, GetDefaultRotation(kneePadL) * Quaternion.Euler(counterStride * legAngle, 0f, 0f));
        SetLocalRotation(kneePadR, GetDefaultRotation(kneePadR) * Quaternion.Euler(stride * legAngle, 0f, 0f));
        SetLocalRotation(torso, GetDefaultRotation(torso) * Quaternion.Euler(3f * moveBlend + Mathf.Abs(stride) * 2.5f * moveBlend, sway, -stride * 2f * moveBlend));
        SetLocalRotation(head, GetDefaultRotation(head) * Quaternion.Euler(-1.5f * moveBlend, -sway * 0.5f, 0f));
        SetLocalRotation(coat, GetDefaultRotation(coat) * Quaternion.Euler(-2f * moveBlend, 0f, 0f));
        SetLocalRotation(coatSkirt, GetDefaultRotation(coatSkirt) * Quaternion.Euler(1f * moveBlend + Mathf.Abs(counterStride) * 2f * moveBlend, 0f, 0f));
        SetLocalRotation(backpack, GetDefaultRotation(backpack) * Quaternion.Euler(Mathf.Abs(counterStride) * 4f * moveBlend, 0f, 0f));

        SetLocalPosition(armL, GetDefaultPosition(armL) + new Vector3(0f, 0f, -stride * armReach));
        SetLocalPosition(armR, GetDefaultPosition(armR) + new Vector3(0f, 0f, -counterStride * armReach));
        SetLocalPosition(forearmL, GetDefaultPosition(forearmL) + new Vector3(0f, Mathf.Abs(stride) * 0.018f * moveBlend, -stride * armReach * 1.2f));
        SetLocalPosition(forearmR, GetDefaultPosition(forearmR) + new Vector3(0f, Mathf.Abs(counterStride) * 0.018f * moveBlend, -counterStride * armReach * 1.2f));
        SetLocalPosition(handL, GetDefaultPosition(handL) + new Vector3(0f, Mathf.Abs(stride) * 0.02f * moveBlend, -stride * armReach * 1.45f));
        SetLocalPosition(handR, GetDefaultPosition(handR) + new Vector3(0f, Mathf.Abs(counterStride) * 0.02f * moveBlend, -counterStride * armReach * 1.45f));
        SetLocalPosition(legL, GetDefaultPosition(legL) + new Vector3(0f, Mathf.Max(0f, counterStride) * footLift * 0.25f, -counterStride * stepReach));
        SetLocalPosition(legR, GetDefaultPosition(legR) + new Vector3(0f, Mathf.Max(0f, stride) * footLift * 0.25f, -stride * stepReach));
        SetLocalPosition(bootL, GetDefaultPosition(bootL) + new Vector3(0f, Mathf.Max(0f, counterStride) * footLift, -counterStride * stepReach * 1.35f));
        SetLocalPosition(bootR, GetDefaultPosition(bootR) + new Vector3(0f, Mathf.Max(0f, stride) * footLift, -stride * stepReach * 1.35f));
        SetLocalPosition(kneePadL, GetDefaultPosition(kneePadL) + new Vector3(0f, Mathf.Max(0f, counterStride) * footLift * 0.42f, -counterStride * stepReach * 1.08f));
        SetLocalPosition(kneePadR, GetDefaultPosition(kneePadR) + new Vector3(0f, Mathf.Max(0f, stride) * footLift * 0.42f, -stride * stepReach * 1.08f));

        visualRoot.localPosition = visualRootDefaultLocalPos + Vector3.up * Mathf.Abs(stride) * bob;
    }

    private Quaternion GetDefaultRotation(Transform t)
    {
        if (t == null)
            return Quaternion.identity;

        if (defaultRotations.TryGetValue(t, out Quaternion rotation))
            return rotation;

        return t.localRotation;
    }

    private Vector3 GetDefaultPosition(Transform t)
    {
        if (t == null)
            return Vector3.zero;

        if (defaultLocalPositions.TryGetValue(t, out Vector3 position))
            return position;

        return t.localPosition;
    }

    private void SetLocalRotation(Transform t, Quaternion target)
    {
        if (t == null)
            return;

        t.localRotation = Quaternion.Slerp(t.localRotation, target, Time.deltaTime * animationSmooth);
    }

    private void SetLocalPosition(Transform t, Vector3 target)
    {
        if (t == null)
            return;

        t.localPosition = Vector3.Lerp(t.localPosition, target, Time.deltaTime * animationSmooth);
    }

    private void RefineHumanSilhouette()
    {
        Transform root = FindDeepChild(transform, "RecyclerVisual");
        if (root == null)
            return;

        if (controller != null)
        {
            controller.height = 2.05f;
            controller.radius = 0.31f;
            controller.center = new Vector3(0f, 1.02f, 0f);
        }

        walkLimbAngle = Mathf.Max(walkLimbAngle, 32f);
        runLimbAngle = Mathf.Max(runLimbAngle, 54f);
        bodyBobAmount = Mathf.Max(bodyBobAmount, 0.055f);
        runBodyBobAmount = Mathf.Max(runBodyBobAmount, 0.11f);
        animationSmooth = Mathf.Max(animationSmooth, 12f);

        EnsureCasualMaterials();
        RemoveSpaceSuitAccessories(root);

        SetLocalTransform(FindDeepChild(root, "Torso"), new Vector3(0f, 1.26f, 0f), Vector3.zero, new Vector3(0.42f, 0.50f, 0.28f));
        SetRendererMaterial(FindDeepChild(root, "Torso"), casualShirtMaterial);
        SetLocalTransform(FindDeepChild(root, "Coat"), new Vector3(0f, 1.24f, -0.01f), Vector3.zero, new Vector3(0.54f, 0.78f, 0.34f));
        SetRendererMaterial(FindDeepChild(root, "Coat"), casualJacketMaterial);
        SetLocalTransform(FindDeepChild(root, "Coat Skirt"), new Vector3(0f, 0.80f, 0f), Vector3.zero, new Vector3(0.46f, 0.18f, 0.30f));
        SetRendererMaterial(FindDeepChild(root, "Coat Skirt"), casualPantsMaterial);
        SetLocalTransform(FindDeepChild(root, "Head"), new Vector3(0f, 1.87f, 0.02f), Vector3.zero, Vector3.one * 0.285f);
        SetRendererMaterial(FindDeepChild(root, "Head"), casualSkinMaterial);

        SetLocalTransform(FindDeepChild(root, "Arm_L"), new Vector3(-0.38f, 1.28f, 0.01f), new Vector3(0f, 0f, -7f), new Vector3(0.085f, 0.46f, 0.085f));
        SetRendererMaterial(FindDeepChild(root, "Arm_L"), casualJacketMaterial);
        SetLocalTransform(FindDeepChild(root, "Forearm_L"), new Vector3(-0.46f, 0.78f, 0.025f), new Vector3(0f, 0f, -3f), new Vector3(0.075f, 0.40f, 0.075f));
        SetRendererMaterial(FindDeepChild(root, "Forearm_L"), casualSkinMaterial);
        SetLocalTransform(FindDeepChild(root, "Arm_R"), new Vector3(0.38f, 1.28f, 0.01f), new Vector3(0f, 0f, 7f), new Vector3(0.085f, 0.46f, 0.085f));
        SetRendererMaterial(FindDeepChild(root, "Arm_R"), casualJacketMaterial);
        SetLocalTransform(FindDeepChild(root, "Forearm_R"), new Vector3(0.46f, 0.78f, 0.025f), new Vector3(0f, 0f, 3f), new Vector3(0.075f, 0.40f, 0.075f));
        SetRendererMaterial(FindDeepChild(root, "Forearm_R"), casualSkinMaterial);

        SetLocalTransform(FindDeepChild(root, "Leg_L"), new Vector3(-0.15f, 0.55f, 0f), Vector3.zero, new Vector3(0.095f, 0.58f, 0.095f));
        SetRendererMaterial(FindDeepChild(root, "Leg_L"), casualPantsMaterial);
        SetLocalTransform(FindDeepChild(root, "Leg_R"), new Vector3(0.15f, 0.55f, 0f), Vector3.zero, new Vector3(0.095f, 0.58f, 0.095f));
        SetRendererMaterial(FindDeepChild(root, "Leg_R"), casualPantsMaterial);
        SetLocalTransform(FindDeepChild(root, "Boot_L"), new Vector3(-0.15f, 0.06f, 0.11f), Vector3.zero, new Vector3(0.16f, 0.09f, 0.30f));
        SetRendererMaterial(FindDeepChild(root, "Boot_L"), casualShoeMaterial);
        SetLocalTransform(FindDeepChild(root, "Boot_R"), new Vector3(0.15f, 0.06f, 0.11f), Vector3.zero, new Vector3(0.16f, 0.09f, 0.30f));
        SetRendererMaterial(FindDeepChild(root, "Boot_R"), casualShoeMaterial);

        CreateOrUpdatePrimitive(root, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 1.66f, 0.015f), Vector3.zero, new Vector3(0.085f, 0.11f, 0.085f), casualSkinMaterial);
        CreateOrUpdatePrimitive(root, "Hand_L", PrimitiveType.Sphere, new Vector3(-0.49f, 0.39f, 0.045f), Vector3.zero, Vector3.one * 0.075f, casualSkinMaterial);
        CreateOrUpdatePrimitive(root, "Hand_R", PrimitiveType.Sphere, new Vector3(0.49f, 0.39f, 0.045f), Vector3.zero, Vector3.one * 0.075f, casualSkinMaterial);
    }

    private void EnsureCasualMaterials()
    {
        if (casualJacketMaterial != null)
            return;

        casualJacketMaterial = CreateRuntimeMaterial("MR3D_Runtime_Worn_Jacket", new Color(0.12f, 0.14f, 0.13f), false);
        casualShirtMaterial = CreateRuntimeMaterial("MR3D_Runtime_Faded_Shirt", new Color(0.22f, 0.24f, 0.23f), false);
        casualPantsMaterial = CreateRuntimeMaterial("MR3D_Runtime_Work_Pants", new Color(0.08f, 0.09f, 0.10f), false);
        casualShoeMaterial = CreateRuntimeMaterial("MR3D_Runtime_Worn_Shoes", new Color(0.035f, 0.032f, 0.03f), false);
        casualSkinMaterial = CreateRuntimeMaterial("MR3D_Runtime_Skin", new Color(0.72f, 0.58f, 0.47f), false);
    }

    private void RemoveSpaceSuitAccessories(Transform root)
    {
        string[] accessoryNames =
        {
            "Hood",
            "Visor",
            "Shoulder Harness",
            "Knee Pad_L",
            "Knee Pad_R",
            "Memory Pack",
            "Memory Canister",
            "Wrist Scanner",
            "Antenna",
            "Antenna Tip"
        };

        for (int i = 0; i < accessoryNames.Length; i++)
        {
            Transform accessory = FindDeepChild(root, accessoryNames[i]);
            if (accessory != null)
            {
                accessory.gameObject.SetActive(false);
                Destroy(accessory.gameObject);
            }
        }

        Transform headLight = FindDeepChild(transform, "Recycler Small Light");
        if (headLight != null)
        {
            headLight.gameObject.SetActive(false);
            Destroy(headLight.gameObject);
        }
    }

    private void SetLocalTransform(Transform t, Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
    {
        if (t == null)
            return;

        t.localPosition = localPosition;
        t.localEulerAngles = localEuler;
        t.localScale = localScale;
    }

    private void CreateOrUpdatePrimitive(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Material material)
    {
        Transform existing = FindDeepChild(parent, name);
        GameObject part = existing != null ? existing.gameObject : GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent);
        SetLocalTransform(part.transform, localPosition, localEuler, localScale);

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private Material CreateRuntimeMaterial(string materialName, Color color, bool emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Diffuse");

        Material material = new Material(shader);
        material.name = materialName;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.08f);

        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 1.8f);
        }

        return material;
    }

    private void SetRendererMaterial(Transform t, Material material)
    {
        if (t == null || material == null)
            return;

        Renderer renderer = t.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private Material GetSharedMaterial(Transform t)
    {
        if (t == null)
            return null;

        Renderer renderer = t.GetComponent<Renderer>();
        return renderer != null ? renderer.sharedMaterial : null;
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
