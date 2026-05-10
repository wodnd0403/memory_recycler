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
    public float backwardSpeedMultiplier = 0.62f;
    public float strafeSpeedMultiplier = 0.82f;

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
    public float backwardLimbAngleMultiplier = 0.74f;
    public float lookBackTurnAngle = 145f;

    [Header("Visual Refinement")]
    public bool refineHumanSilhouette = true;

    private CharacterController controller;
    private float verticalVelocity;

    private Transform visualRoot;
    private Transform torso;
    private Transform head;
    private Transform armPivotL;
    private Transform forearmPivotL;
    private Transform armPivotR;
    private Transform forearmPivotR;
    private Transform legPivotL;
    private Transform kneePivotL;
    private Transform legPivotR;
    private Transform kneePivotR;
    private Transform armL;
    private Transform forearmL;
    private Transform armR;
    private Transform forearmR;
    private Transform legL;
    private Transform shinL;
    private Transform legR;
    private Transform shinR;
    private Transform bootL;
    private Transform bootR;
    private Transform handL;
    private Transform handR;
    private Transform kneePadL;
    private Transform kneePadR;
    private Transform coat;
    private Transform coatSkirt;
    private Transform backpack;
    private Transform tripoVisual;

    private Material casualJacketMaterial;
    private Material casualShirtMaterial;
    private Material casualPantsMaterial;
    private Material casualShoeMaterial;
    private Material casualSkinMaterial;
    private Material casualHairMaterial;
    private Material casualBagMaterial;
    private Material casualGlowMaterial;
    private Material casualAmberMaterial;

    private readonly Dictionary<Transform, Quaternion> defaultRotations = new Dictionary<Transform, Quaternion>();
    private readonly Dictionary<Transform, Vector3> defaultLocalPositions = new Dictionary<Transform, Vector3>();
    private Vector3 visualRootDefaultLocalPos;
    private Vector3 tripoVisualDefaultLocalPos;
    private Quaternion tripoVisualDefaultLocalRotation;
    private float animationTime;
    private bool isRunning;
    private float moveBlend;
    private float localMoveForward;
    private float localMoveSide;
    private float lookBackBlend;
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
        if (UIManager3D.Instance != null && UIManager3D.Instance.IsGameplayInputBlocked())
            return;

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
        float inputMagnitude = Mathf.Clamp01(new Vector2(horizontal, vertical).magnitude);

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

            Vector3 facingDirection = moveDirection;
            if (input.z < -0.12f)
            {
                facingDirection = cameraForward + cameraRight * input.x * 0.45f;
                facingDirection.y = 0f;
                facingDirection.Normalize();
            }

            if (facingDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(facingDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        Vector3 localMove = moveDirection.sqrMagnitude > 0.001f ? transform.InverseTransformDirection(moveDirection) : Vector3.zero;
        localMoveForward = Mathf.Lerp(localMoveForward, Mathf.Clamp(localMove.z, -1f, 1f), Time.deltaTime * animationSmooth);
        localMoveSide = Mathf.Lerp(localMoveSide, Mathf.Clamp(localMove.x, -1f, 1f), Time.deltaTime * animationSmooth);

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        isRunning = Input.GetKey(KeyCode.LeftShift) && input.magnitude >= 0.1f;
        float speed = isRunning ? runSpeed : walkSpeed;
        if (vertical < -0.1f)
            speed *= backwardSpeedMultiplier;
        else if (Mathf.Abs(horizontal) > 0.1f && Mathf.Abs(vertical) < 0.35f)
            speed *= strafeSpeedMultiplier;

        Vector3 finalMove = moveDirection * speed;
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);

        lastPlanarMove = new Vector3(moveDirection.x, 0f, moveDirection.z) * speed;
        IsMoving = inputMagnitude >= 0.1f && controller.isGrounded;
    }

    private void CacheAnimationRig()
    {
        visualRoot = FindDeepChild(transform, "RecyclerVisual");
        if (visualRoot == null)
            return;

        tripoVisual = FindDeepChild(transform, "Tripo Player Visual");
        torso = FindDeepChild(visualRoot, "Torso");
        head = FindDeepChild(visualRoot, "Head");
        armPivotL = FindDeepChild(visualRoot, "ArmPivot_L");
        forearmPivotL = FindDeepChild(visualRoot, "ForearmPivot_L");
        armPivotR = FindDeepChild(visualRoot, "ArmPivot_R");
        forearmPivotR = FindDeepChild(visualRoot, "ForearmPivot_R");
        legPivotL = FindDeepChild(visualRoot, "LegPivot_L");
        kneePivotL = FindDeepChild(visualRoot, "KneePivot_L");
        legPivotR = FindDeepChild(visualRoot, "LegPivot_R");
        kneePivotR = FindDeepChild(visualRoot, "KneePivot_R");
        armL = FindDeepChild(visualRoot, "Arm_L");
        forearmL = FindDeepChild(visualRoot, "Forearm_L");
        armR = FindDeepChild(visualRoot, "Arm_R");
        forearmR = FindDeepChild(visualRoot, "Forearm_R");
        legL = FindDeepChild(visualRoot, "Leg_L");
        shinL = FindDeepChild(visualRoot, "Shin_L");
        legR = FindDeepChild(visualRoot, "Leg_R");
        shinR = FindDeepChild(visualRoot, "Shin_R");
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
        CacheDefaultPose(armPivotL);
        CacheDefaultPose(forearmPivotL);
        CacheDefaultPose(armPivotR);
        CacheDefaultPose(forearmPivotR);
        CacheDefaultPose(legPivotL);
        CacheDefaultPose(kneePivotL);
        CacheDefaultPose(legPivotR);
        CacheDefaultPose(kneePivotR);
        CacheDefaultPose(armL);
        CacheDefaultPose(forearmL);
        CacheDefaultPose(armR);
        CacheDefaultPose(forearmR);
        CacheDefaultPose(legL);
        CacheDefaultPose(shinL);
        CacheDefaultPose(legR);
        CacheDefaultPose(shinR);
        CacheDefaultPose(bootL);
        CacheDefaultPose(bootR);
        CacheDefaultPose(handL);
        CacheDefaultPose(handR);
        CacheDefaultPose(kneePadL);
        CacheDefaultPose(kneePadR);
        CacheDefaultPose(coat);
        CacheDefaultPose(coatSkirt);
        CacheDefaultPose(backpack);

        if (tripoVisual != null)
        {
            tripoVisualDefaultLocalPos = tripoVisual.localPosition;
            tripoVisualDefaultLocalRotation = tripoVisual.localRotation;
        }
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
        float forwardAmount = Mathf.Clamp(localMoveForward, -1f, 1f);
        float sideAmount = Mathf.Clamp(localMoveSide, -1f, 1f);
        float backwardAmount = Mathf.Clamp01(-forwardAmount);
        float lookBackTarget = (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftAlt) || backwardAmount > 0.45f) ? 1f : 0f;
        lookBackBlend = Mathf.Lerp(lookBackBlend, lookBackTarget, Time.deltaTime * animationSmooth);
        float forwardSign = backwardAmount > 0.35f ? -1f : 1f;
        float directionMultiplier = Mathf.Lerp(1f, backwardLimbAngleMultiplier, backwardAmount);
        float armAngle = Mathf.Lerp(walkLimbAngle, runLimbAngle, isRunning ? 1f : 0f) * moveBlend * directionMultiplier;
        float legAngle = Mathf.Lerp(walkLimbAngle, runLimbAngle, isRunning ? 1f : 0f) * moveBlend * directionMultiplier;
        float bob = Mathf.Lerp(bodyBobAmount, runBodyBobAmount, isRunning ? 1f : 0f) * moveBlend;
        float sway = (Mathf.Sin(animationTime * 0.5f) * 5.5f + sideAmount * 7f) * moveBlend;
        float elbowBase = Mathf.Lerp(8f, 16f, isRunning ? 1f : 0f) * moveBlend;
        float elbowFlex = Mathf.Lerp(16f, 28f, isRunning ? 1f : 0f) * moveBlend;
        float kneeBase = Mathf.Lerp(4f, 10f, isRunning ? 1f : 0f) * moveBlend;
        float kneeFlex = Mathf.Lerp(20f, 38f, isRunning ? 1f : 0f) * moveBlend;
        float footAngle = Mathf.Lerp(8f, 18f, isRunning ? 1f : 0f) * moveBlend;

        SetLocalRotation(armPivotL, GetDefaultRotation(armPivotL) * Quaternion.Euler(stride * armAngle * forwardSign, sideAmount * 10f * moveBlend, -5f * moveBlend - sideAmount * 7f * moveBlend));
        SetLocalRotation(forearmPivotL, GetDefaultRotation(forearmPivotL) * Quaternion.Euler(-elbowBase - Mathf.Abs(stride) * elbowFlex, sideAmount * 3f * moveBlend, 0f));
        SetLocalRotation(armPivotR, GetDefaultRotation(armPivotR) * Quaternion.Euler(counterStride * armAngle * forwardSign, sideAmount * 10f * moveBlend, 5f * moveBlend - sideAmount * 7f * moveBlend));
        SetLocalRotation(forearmPivotR, GetDefaultRotation(forearmPivotR) * Quaternion.Euler(-elbowBase - Mathf.Abs(counterStride) * elbowFlex, sideAmount * 3f * moveBlend, 0f));
        SetLocalRotation(handL, GetDefaultRotation(handL) * Quaternion.Euler(stride * armAngle * 0.18f * forwardSign, 0f, -4f * moveBlend));
        SetLocalRotation(handR, GetDefaultRotation(handR) * Quaternion.Euler(counterStride * armAngle * 0.18f * forwardSign, 0f, 4f * moveBlend));

        SetLocalRotation(legPivotL, GetDefaultRotation(legPivotL) * Quaternion.Euler(counterStride * legAngle * forwardSign, sideAmount * 6f * moveBlend, -1.5f * moveBlend - sideAmount * 5f * moveBlend));
        SetLocalRotation(kneePivotL, GetDefaultRotation(kneePivotL) * Quaternion.Euler(kneeBase + Mathf.Abs(counterStride) * kneeFlex * (0.45f + Mathf.Max(0f, counterStride * forwardSign) * 0.55f), 0f, 0f));
        SetLocalRotation(legPivotR, GetDefaultRotation(legPivotR) * Quaternion.Euler(stride * legAngle * forwardSign, sideAmount * 6f * moveBlend, 1.5f * moveBlend - sideAmount * 5f * moveBlend));
        SetLocalRotation(kneePivotR, GetDefaultRotation(kneePivotR) * Quaternion.Euler(kneeBase + Mathf.Abs(stride) * kneeFlex * (0.45f + Mathf.Max(0f, stride * forwardSign) * 0.55f), 0f, 0f));
        SetLocalRotation(bootL, GetDefaultRotation(bootL) * Quaternion.Euler((-counterStride * footAngle + Mathf.Max(0f, counterStride * forwardSign) * footAngle) * forwardSign, 0f, sideAmount * 3f * moveBlend));
        SetLocalRotation(bootR, GetDefaultRotation(bootR) * Quaternion.Euler((-stride * footAngle + Mathf.Max(0f, stride * forwardSign) * footAngle) * forwardSign, 0f, sideAmount * 3f * moveBlend));
        SetLocalRotation(kneePadL, GetDefaultRotation(kneePadL) * Quaternion.Euler(counterStride * legAngle, 0f, 0f));
        SetLocalRotation(kneePadR, GetDefaultRotation(kneePadR) * Quaternion.Euler(stride * legAngle, 0f, 0f));
        float lookBackYaw = lookBackTurnAngle * lookBackBlend;
        float lookBackLean = 7f * lookBackBlend;
        SetLocalRotation(torso, GetDefaultRotation(torso) * Quaternion.Euler((3f * forwardAmount + Mathf.Abs(stride) * 2.5f) * moveBlend - lookBackLean, sway + lookBackYaw * 0.32f, -stride * 2f * moveBlend - sideAmount * 4f * moveBlend));
        SetLocalRotation(head, GetDefaultRotation(head) * Quaternion.Euler(-1.5f * moveBlend, -sway * 0.5f + lookBackYaw, 0f));
        SetLocalRotation(coat, GetDefaultRotation(coat) * Quaternion.Euler(-2f * moveBlend, 0f, 0f));
        SetLocalRotation(coatSkirt, GetDefaultRotation(coatSkirt) * Quaternion.Euler(1f * moveBlend + Mathf.Abs(counterStride) * 2f * moveBlend, 0f, 0f));
        SetLocalRotation(backpack, GetDefaultRotation(backpack) * Quaternion.Euler(Mathf.Abs(counterStride) * 4f * moveBlend, 0f, 0f));

        visualRoot.localPosition = visualRootDefaultLocalPos + Vector3.up * Mathf.Abs(stride) * bob;
        UpdateTripoVisualMotion(stride, bob, sideAmount, forwardAmount);
    }

    private void UpdateTripoVisualMotion(float stride, float bob, float sideAmount, float forwardAmount)
    {
        if (tripoVisual == null)
            return;

        Vector3 targetPos = tripoVisualDefaultLocalPos + Vector3.up * Mathf.Abs(stride) * bob * 0.55f;
        Quaternion targetRot = tripoVisualDefaultLocalRotation * Quaternion.Euler(forwardAmount * 2.0f * moveBlend, sideAmount * 3.0f * moveBlend, -sideAmount * 4.0f * moveBlend);
        tripoVisual.localPosition = Vector3.Lerp(tripoVisual.localPosition, targetPos, Time.deltaTime * animationSmooth);
        tripoVisual.localRotation = Quaternion.Slerp(tripoVisual.localRotation, targetRot, Time.deltaTime * animationSmooth);
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

        SetLocalTransform(FindDeepChild(root, "Torso"), new Vector3(0f, 1.26f, 0f), Vector3.zero, new Vector3(0.46f, 0.52f, 0.31f));
        SetRendererMaterial(FindDeepChild(root, "Torso"), casualShirtMaterial);
        SetLocalTransform(FindDeepChild(root, "Coat"), new Vector3(0f, 1.21f, -0.01f), Vector3.zero, new Vector3(0.64f, 0.90f, 0.40f));
        SetRendererMaterial(FindDeepChild(root, "Coat"), casualJacketMaterial);
        SetLocalTransform(FindDeepChild(root, "Coat Skirt"), new Vector3(0f, 0.67f, 0f), Vector3.zero, new Vector3(0.56f, 0.46f, 0.34f));
        SetRendererMaterial(FindDeepChild(root, "Coat Skirt"), casualJacketMaterial);
        SetLocalTransform(FindDeepChild(root, "Head"), new Vector3(0f, 1.87f, 0.02f), Vector3.zero, Vector3.one * 0.285f);
        SetRendererMaterial(FindDeepChild(root, "Head"), casualSkinMaterial);

        BuildHumanoidLimbRig(root);
        BuildReferenceCharacterDetails(root);

        CreateOrUpdatePrimitive(root, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 1.66f, 0.015f), Vector3.zero, new Vector3(0.085f, 0.11f, 0.085f), casualSkinMaterial);
    }

    private void BuildHumanoidLimbRig(Transform root)
    {
        Transform leftArmPivot = CreateOrUpdatePivot(root, "ArmPivot_L", new Vector3(-0.39f, 1.47f, 0.015f), new Vector3(0f, 0f, -6f));
        Transform leftForearmPivot = CreateOrUpdatePivot(leftArmPivot, "ForearmPivot_L", new Vector3(0f, -0.48f, 0f), Vector3.zero);
        CreateOrUpdatePrimitive(leftArmPivot, "Shoulder_L", PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(0.15f, 0.12f, 0.13f), casualJacketMaterial);
        CreateOrUpdatePrimitive(leftArmPivot, "Arm_L", PrimitiveType.Cylinder, new Vector3(0f, -0.24f, 0f), Vector3.zero, new Vector3(0.135f, 0.255f, 0.135f), casualJacketMaterial);
        CreateOrUpdatePrimitive(leftForearmPivot, "Elbow_L", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.118f, casualJacketMaterial);
        CreateOrUpdatePrimitive(leftForearmPivot, "Forearm_L", PrimitiveType.Cylinder, new Vector3(0f, -0.23f, 0f), Vector3.zero, new Vector3(0.116f, 0.25f, 0.116f), casualJacketMaterial);
        CreateOrUpdatePrimitive(leftForearmPivot, "Hand_L", PrimitiveType.Sphere, new Vector3(0f, -0.51f, 0.035f), Vector3.zero, new Vector3(0.090f, 0.080f, 0.070f), casualShoeMaterial);

        Transform rightArmPivot = CreateOrUpdatePivot(root, "ArmPivot_R", new Vector3(0.39f, 1.47f, 0.015f), new Vector3(0f, 0f, 6f));
        Transform rightForearmPivot = CreateOrUpdatePivot(rightArmPivot, "ForearmPivot_R", new Vector3(0f, -0.48f, 0f), Vector3.zero);
        CreateOrUpdatePrimitive(rightArmPivot, "Shoulder_R", PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(0.15f, 0.12f, 0.13f), casualJacketMaterial);
        CreateOrUpdatePrimitive(rightArmPivot, "Arm_R", PrimitiveType.Cylinder, new Vector3(0f, -0.24f, 0f), Vector3.zero, new Vector3(0.135f, 0.255f, 0.135f), casualJacketMaterial);
        CreateOrUpdatePrimitive(rightForearmPivot, "Elbow_R", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.118f, casualJacketMaterial);
        CreateOrUpdatePrimitive(rightForearmPivot, "Forearm_R", PrimitiveType.Cylinder, new Vector3(0f, -0.23f, 0f), Vector3.zero, new Vector3(0.116f, 0.25f, 0.116f), casualJacketMaterial);
        CreateOrUpdatePrimitive(rightForearmPivot, "Hand_R", PrimitiveType.Sphere, new Vector3(0f, -0.51f, 0.035f), Vector3.zero, new Vector3(0.090f, 0.080f, 0.070f), casualShoeMaterial);

        Transform leftLegPivot = CreateOrUpdatePivot(root, "LegPivot_L", new Vector3(-0.15f, 0.97f, 0f), Vector3.zero);
        Transform leftKneePivot = CreateOrUpdatePivot(leftLegPivot, "KneePivot_L", new Vector3(0f, -0.56f, 0f), Vector3.zero);
        CreateOrUpdatePrimitive(leftLegPivot, "Leg_L", PrimitiveType.Cylinder, new Vector3(0f, -0.28f, 0f), Vector3.zero, new Vector3(0.135f, 0.29f, 0.135f), casualPantsMaterial);
        CreateOrUpdatePrimitive(leftKneePivot, "Knee_L", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.125f, casualPantsMaterial);
        CreateOrUpdatePrimitive(leftKneePivot, "Shin_L", PrimitiveType.Cylinder, new Vector3(0f, -0.27f, 0f), Vector3.zero, new Vector3(0.115f, 0.28f, 0.115f), casualPantsMaterial);
        CreateOrUpdatePrimitive(leftKneePivot, "Boot_L", PrimitiveType.Cube, new Vector3(0f, -0.58f, 0.10f), Vector3.zero, new Vector3(0.18f, 0.10f, 0.32f), casualShoeMaterial);

        Transform rightLegPivot = CreateOrUpdatePivot(root, "LegPivot_R", new Vector3(0.15f, 0.97f, 0f), Vector3.zero);
        Transform rightKneePivot = CreateOrUpdatePivot(rightLegPivot, "KneePivot_R", new Vector3(0f, -0.56f, 0f), Vector3.zero);
        CreateOrUpdatePrimitive(rightLegPivot, "Leg_R", PrimitiveType.Cylinder, new Vector3(0f, -0.28f, 0f), Vector3.zero, new Vector3(0.135f, 0.29f, 0.135f), casualPantsMaterial);
        CreateOrUpdatePrimitive(rightKneePivot, "Knee_R", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.125f, casualPantsMaterial);
        CreateOrUpdatePrimitive(rightKneePivot, "Shin_R", PrimitiveType.Cylinder, new Vector3(0f, -0.27f, 0f), Vector3.zero, new Vector3(0.115f, 0.28f, 0.115f), casualPantsMaterial);
        CreateOrUpdatePrimitive(rightKneePivot, "Boot_R", PrimitiveType.Cube, new Vector3(0f, -0.58f, 0.10f), Vector3.zero, new Vector3(0.18f, 0.10f, 0.32f), casualShoeMaterial);
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
        casualHairMaterial = CreateRuntimeMaterial("MR3D_Runtime_Dark_Hair", new Color(0.025f, 0.024f, 0.022f), false);
        casualBagMaterial = CreateRuntimeMaterial("MR3D_Runtime_Worn_Bag", new Color(0.075f, 0.07f, 0.06f), false);
        casualGlowMaterial = CreateRuntimeMaterial("MR3D_Runtime_Memory_Vial", new Color(0.08f, 0.85f, 1f), true);
        casualAmberMaterial = CreateRuntimeMaterial("MR3D_Runtime_Amber_Detail", new Color(0.95f, 0.48f, 0.09f), true);
    }

    private void BuildReferenceCharacterDetails(Transform root)
    {
        CreateOrUpdatePrimitive(root, "Hair Cap", PrimitiveType.Sphere, new Vector3(0f, 1.96f, -0.005f), Vector3.zero, new Vector3(0.31f, 0.17f, 0.27f), casualHairMaterial);
        CreateOrUpdatePrimitive(root, "Hair Back", PrimitiveType.Sphere, new Vector3(0f, 1.91f, -0.13f), Vector3.zero, new Vector3(0.27f, 0.18f, 0.16f), casualHairMaterial);
        CreateOrUpdatePrimitive(root, "Hair Side_L", PrimitiveType.Cube, new Vector3(-0.17f, 1.86f, 0.025f), new Vector3(0f, 0f, -8f), new Vector3(0.055f, 0.16f, 0.12f), casualHairMaterial);
        CreateOrUpdatePrimitive(root, "Hair Side_R", PrimitiveType.Cube, new Vector3(0.17f, 1.86f, 0.025f), new Vector3(0f, 0f, 8f), new Vector3(0.055f, 0.16f, 0.12f), casualHairMaterial);
        CreateOrUpdatePrimitive(root, "Hair Fringe_L", PrimitiveType.Cube, new Vector3(-0.08f, 1.91f, 0.18f), new Vector3(0f, 0f, -18f), new Vector3(0.07f, 0.11f, 0.035f), casualHairMaterial);
        CreateOrUpdatePrimitive(root, "Hair Fringe_R", PrimitiveType.Cube, new Vector3(0.08f, 1.91f, 0.18f), new Vector3(0f, 0f, 18f), new Vector3(0.07f, 0.11f, 0.035f), casualHairMaterial);
        CreateOrUpdatePrimitive(root, "Nose", PrimitiveType.Cube, new Vector3(0f, 1.85f, 0.205f), new Vector3(-8f, 0f, 0f), new Vector3(0.055f, 0.075f, 0.085f), casualSkinMaterial);
        CreateOrUpdatePrimitive(root, "Ear_L", PrimitiveType.Sphere, new Vector3(-0.205f, 1.855f, 0.025f), Vector3.zero, new Vector3(0.052f, 0.082f, 0.040f), casualSkinMaterial);
        CreateOrUpdatePrimitive(root, "Ear_R", PrimitiveType.Sphere, new Vector3(0.205f, 1.855f, 0.025f), Vector3.zero, new Vector3(0.052f, 0.082f, 0.040f), casualSkinMaterial);
        CreateOrUpdatePrimitive(root, "Chin", PrimitiveType.Cube, new Vector3(0f, 1.755f, 0.122f), new Vector3(8f, 0f, 0f), new Vector3(0.145f, 0.055f, 0.065f), casualSkinMaterial);
        CreateOrUpdatePrimitive(root, "Face Mask", PrimitiveType.Cube, new Vector3(0f, 1.82f, 0.215f), new Vector3(-5f, 0f, 0f), new Vector3(0.27f, 0.18f, 0.070f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Eye Light Band", PrimitiveType.Cube, new Vector3(0f, 1.90f, 0.250f), Vector3.zero, new Vector3(0.25f, 0.032f, 0.035f), casualGlowMaterial);
        CreateOrUpdatePrimitive(root, "Visor Housing", PrimitiveType.Cube, new Vector3(0f, 1.90f, 0.232f), Vector3.zero, new Vector3(0.31f, 0.070f, 0.040f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Raised Collar", PrimitiveType.Cube, new Vector3(0f, 1.58f, -0.11f), new Vector3(-8f, 0f, 0f), new Vector3(0.50f, 0.25f, 0.15f), casualJacketMaterial);
        CreateOrUpdatePrimitive(root, "Collar Guard_L", PrimitiveType.Cube, new Vector3(-0.29f, 1.58f, 0.02f), new Vector3(0f, 0f, -12f), new Vector3(0.08f, 0.22f, 0.15f), casualJacketMaterial);
        CreateOrUpdatePrimitive(root, "Collar Guard_R", PrimitiveType.Cube, new Vector3(0.29f, 1.58f, 0.02f), new Vector3(0f, 0f, 12f), new Vector3(0.08f, 0.22f, 0.15f), casualJacketMaterial);
        CreateOrUpdatePrimitive(root, "Long Coat Tail", PrimitiveType.Cube, new Vector3(0f, 0.50f, -0.04f), Vector3.zero, new Vector3(0.54f, 0.58f, 0.31f), casualJacketMaterial);
        CreateOrUpdatePrimitive(root, "Coat Back Seam", PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.245f), Vector3.zero, new Vector3(0.035f, 0.72f, 0.040f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Coat Hem_L", PrimitiveType.Cube, new Vector3(-0.19f, 0.44f, -0.06f), new Vector3(0f, 0f, 5f), new Vector3(0.18f, 0.34f, 0.25f), casualJacketMaterial);
        CreateOrUpdatePrimitive(root, "Coat Hem_R", PrimitiveType.Cube, new Vector3(0.19f, 0.44f, -0.06f), new Vector3(0f, 0f, -5f), new Vector3(0.18f, 0.34f, 0.25f), casualJacketMaterial);
        CreateOrUpdatePrimitive(root, "Crossbody Strap", PrimitiveType.Cube, new Vector3(-0.08f, 1.22f, -0.20f), new Vector3(0f, 0f, -22f), new Vector3(0.070f, 0.82f, 0.048f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Amber Strap Clips", PrimitiveType.Cube, new Vector3(-0.22f, 1.26f, -0.255f), new Vector3(0f, 0f, -22f), new Vector3(0.050f, 0.070f, 0.040f), casualAmberMaterial);
        CreateOrUpdatePrimitive(root, "Satchel", PrimitiveType.Cube, new Vector3(-0.25f, 0.88f, -0.33f), new Vector3(0f, 8f, -4f), new Vector3(0.34f, 0.25f, 0.18f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Memory Backpack", PrimitiveType.Cube, new Vector3(0f, 1.12f, -0.46f), Vector3.zero, new Vector3(0.43f, 0.58f, 0.16f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Backpack Top Module", PrimitiveType.Cube, new Vector3(0f, 1.43f, -0.55f), Vector3.zero, new Vector3(0.36f, 0.11f, 0.10f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Backpack Cyan Core", PrimitiveType.Cube, new Vector3(0f, 1.10f, -0.555f), Vector3.zero, new Vector3(0.070f, 0.24f, 0.035f), casualGlowMaterial);
        CreateOrUpdatePrimitive(root, "Backpack Side Canister_L", PrimitiveType.Cylinder, new Vector3(-0.29f, 1.08f, -0.50f), Vector3.zero, new Vector3(0.050f, 0.28f, 0.050f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Backpack Side Canister_R", PrimitiveType.Cylinder, new Vector3(0.29f, 1.08f, -0.50f), Vector3.zero, new Vector3(0.050f, 0.28f, 0.050f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Memory Vial", PrimitiveType.Cube, new Vector3(-0.08f, 0.82f, -0.45f), Vector3.zero, new Vector3(0.085f, 0.20f, 0.050f), casualGlowMaterial);
        CreateOrUpdatePrimitive(root, "Cinematic Backpack Light", PrimitiveType.Cube, new Vector3(-0.08f, 0.81f, -0.505f), Vector3.zero, new Vector3(0.070f, 0.24f, 0.040f), casualGlowMaterial);
        CreateOrUpdatePrimitive(root, "Cinematic Coat Shoulder Line_L", PrimitiveType.Cube, new Vector3(-0.31f, 1.50f, -0.17f), new Vector3(0f, 0f, -12f), new Vector3(0.18f, 0.030f, 0.035f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Cinematic Coat Shoulder Line_R", PrimitiveType.Cube, new Vector3(0.31f, 1.50f, -0.17f), new Vector3(0f, 0f, 12f), new Vector3(0.18f, 0.030f, 0.035f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Coat Amber Trim_L", PrimitiveType.Cube, new Vector3(-0.285f, 1.02f, 0.205f), Vector3.zero, new Vector3(0.030f, 0.62f, 0.030f), casualAmberMaterial);
        CreateOrUpdatePrimitive(root, "Coat Amber Trim_R", PrimitiveType.Cube, new Vector3(0.285f, 1.02f, 0.205f), Vector3.zero, new Vector3(0.030f, 0.62f, 0.030f), casualAmberMaterial);
        CreateOrUpdatePrimitive(root, "Coat Back Fold_L", PrimitiveType.Cube, new Vector3(-0.15f, 1.10f, -0.252f), new Vector3(0f, 0f, -4f), new Vector3(0.024f, 0.60f, 0.035f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Coat Back Fold_R", PrimitiveType.Cube, new Vector3(0.15f, 1.10f, -0.252f), new Vector3(0f, 0f, 4f), new Vector3(0.024f, 0.60f, 0.035f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Satchel Flap", PrimitiveType.Cube, new Vector3(-0.25f, 0.95f, -0.435f), new Vector3(0f, 8f, -4f), new Vector3(0.30f, 0.065f, 0.035f), casualBagMaterial);
        CreateOrUpdatePrimitive(root, "Satchel Buckle", PrimitiveType.Cube, new Vector3(-0.25f, 0.88f, -0.535f), Vector3.zero, new Vector3(0.060f, 0.050f, 0.030f), casualGlowMaterial);

        Transform leftForearm = FindDeepChild(root, "ForearmPivot_L");
        Transform rightForearm = FindDeepChild(root, "ForearmPivot_R");
        if (leftForearm != null)
        {
            CreateOrUpdatePrimitive(leftForearm, "Arm Scanner_L", PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.125f), new Vector3(8f, 0f, 0f), new Vector3(0.15f, 0.20f, 0.050f), casualBagMaterial);
            CreateOrUpdatePrimitive(leftForearm, "Scanner Glow_L", PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.157f), Vector3.zero, new Vector3(0.090f, 0.050f, 0.030f), casualGlowMaterial);
        }

        if (rightForearm != null)
        {
            CreateOrUpdatePrimitive(rightForearm, "Arm Scanner_R", PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.125f), new Vector3(8f, 0f, 0f), new Vector3(0.15f, 0.20f, 0.050f), casualBagMaterial);
            CreateOrUpdatePrimitive(rightForearm, "Scanner Glow_R", PrimitiveType.Cube, new Vector3(0f, -0.17f, 0.157f), Vector3.zero, new Vector3(0.090f, 0.050f, 0.030f), casualGlowMaterial);
        }
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

    private Transform CreateOrUpdatePivot(Transform parent, string name, Vector3 localPosition, Vector3 localEuler)
    {
        Transform pivot = FindDeepChild(parent, name);
        if (pivot == null)
        {
            GameObject pivotObject = new GameObject(name);
            pivot = pivotObject.transform;
        }

        pivot.SetParent(parent, false);
        pivot.localPosition = localPosition;
        pivot.localEulerAngles = localEuler;
        pivot.localScale = Vector3.one;
        return pivot;
    }

    private void CreateOrUpdatePrimitive(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Material material)
    {
        Transform existing = FindDeepChild(parent, name);
        if (existing == null && visualRoot != null)
            existing = FindDeepChild(visualRoot, name);
        GameObject part = existing != null ? existing.gameObject : GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
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
