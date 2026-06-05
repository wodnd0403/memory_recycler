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
    public float acceleration = 28f;
    public float deceleration = 34f;
    public float airControl = 0.35f;
    public float inputDeadZone = 0.08f;
    public float jumpHeight = 1.25f;

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

    [Header("External Humanoid Model")]
    // Mixamo MR_Player_TPose 모델을 쓸 때 true. 절차형 RecyclerVisual 빌드/회전을 모두 건너뛴다.
    public bool useExternalHumanoidModel = true;
    public Transform externalVisualRoot;
    public Animator externalAnimator;
    // Run 전용 클립이 없어 Walk를 빠르게 재생해 달리기를 표현. 1=Walk 속도, 1.55≈Run 속도.
    public float walkAnimationPlaybackSpeed = 1f;
    public float runAnimationPlaybackSpeed = 1f;
    public float animatorParameterSmooth = 12f;
    public bool lockExternalVisualTransform = true;
    public bool autoGroundExternalVisual = true;
    public float externalVisualGroundPadding = 0.005f;
    public float externalFootGroundOffset = 0.075f;
    public bool stabilizeExternalClipRootMotion = true;
    public Vector3 externalVisualLocalPosition = Vector3.zero;
    public Vector3 externalVisualLocalEuler = Vector3.zero;
    public Vector3 externalVisualLocalScale = Vector3.one;

    [Header("Controller / Visual Alignment")]
    public bool autoFitControllerToExternalVisual = true;
    public bool preferHumanoidBoneControllerFit = true;
    public float controllerFitHeightPadding = 0.04f;
    public float controllerFitHeadPadding = 0.18f;
    public float controllerFitBottomPadding = 0.015f;
    public float controllerFitRadiusScale = 0.55f;
    public float controllerFitRadiusPadding = 0.02f;
    public float controllerFitMinHeight = 1.10f;
    public float controllerFitMaxHeight = 1.78f;
    public float controllerFitMinRadius = 0.16f;
    public float controllerFitMaxRadius = 0.27f;
    public bool logControllerFitDebug = false;
    public bool drawControllerFitDebugGizmos = true;

    [Header("Ground Probe")]
    public bool enableGroundProbe = true;
    public LayerMask groundProbeMask = ~0;
    public float groundProbeRadius = 0.24f;
    public float groundProbeDistance = 0.55f;
    public float groundSnapMaxDistance = 0.32f;
    public float groundSnapSpeed = 18f;
    public float groundProbeSlopeLimit = 50f;
    public float groundStickVelocity = -3.5f;
    public float jumpGroundProbeGraceTime = 0.16f;
    public float visualGroundClampRange = 0.35f;

    private CharacterController controller;
    private float verticalVelocity;

    // Animator 파라미터 해시 — 매 프레임 string lookup을 피한다.
    private static readonly int AnimMoveSpeed = Animator.StringToHash("MoveSpeed");
    private static readonly int AnimForward = Animator.StringToHash("Forward");
    private static readonly int AnimSide = Animator.StringToHash("Side");
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimVerticalSpeed = Animator.StringToHash("VerticalSpeed");
    private static readonly int AnimJumpTrigger = Animator.StringToHash("JumpTrigger");

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
    private Vector3 currentPlanarVelocity;
    private Transform externalHips;
    private Transform externalLeftFoot;
    private Transform externalRightFoot;
    private Transform externalLeftToes;
    private Transform externalRightToes;
    private Vector3 externalHipsDefaultLocalPosition;
    private Vector3 externalVisualBaseLocalPosition;
    private bool hasExternalVisualBaseLocalPosition;
    private readonly RaycastHit[] groundProbeHits = new RaycastHit[12];
    private bool groundProbeHasGround;
    private float groundProbeGap;
    private Vector3 groundProbeNormal = Vector3.up;
    private float skipGroundSnapUntil;
    private Bounds lastExternalVisualLocalBounds;
    private bool hasExternalVisualLocalBounds;
    private const float MinimumExternalVisualGroundPadding = 0.005f;
    private const float MaximumExternalVisualGroundPadding = 0.012f;

    public bool IsMoving { get; private set; }
    public bool IsRunning => isRunning && IsMoving;
    public float MoveBlend => moveBlend;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        PruneTripoVisualCollidersAtRuntime();

        // 외부 모델을 쓰지만 인스펙터 연결이 비어 있으면 자식에서 Animator를 자동 탐색해 와이어링.
        // 에디터 자동 셋업이 실행되지 않은 환경에서도 Play만 눌러 동작하게 만든다.
        if (useExternalHumanoidModel)
        {
            if (externalAnimator == null)
                externalAnimator = GetComponentInChildren<Animator>(true);
            if (externalAnimator != null)
            {
                if (externalVisualRoot == null)
                    externalVisualRoot = externalAnimator.transform;
                externalAnimator.applyRootMotion = false;
                if (!externalAnimator.gameObject.activeSelf)
                    externalAnimator.gameObject.SetActive(true);
            }
        }
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (useExternalHumanoidModel)
        {
            // 외부 모델(Mixamo MR_Player) 사용 시 절차형 비주얼 코드는 모두 비활성화.
            ConfigureExternalHumanoid();
            return;
        }

        if (refineHumanSilhouette)
            RefineHumanSilhouette();

        CacheAnimationRig();
    }

    // 외부 휴머노이드 모델용 초기 설정. Animator/visualRoot 자동 탐색 + Root Motion 비활성화.
    private void ConfigureExternalHumanoid()
    {
        externalVisualGroundPadding = Mathf.Clamp(externalVisualGroundPadding, MinimumExternalVisualGroundPadding, MaximumExternalVisualGroundPadding);
        externalFootGroundOffset = Mathf.Clamp(externalFootGroundOffset, 0f, 0.18f);

        if (externalAnimator == null)
            externalAnimator = GetComponentInChildren<Animator>();

        if (externalVisualRoot == null && externalAnimator != null)
            externalVisualRoot = externalAnimator.transform;

        if (externalAnimator != null)
        {
            externalAnimator.applyRootMotion = false;
            externalAnimator.updateMode = AnimatorUpdateMode.Normal;
            externalAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            externalAnimator.speed = 1f;
            if (externalAnimator.runtimeAnimatorController != null)
            {
                externalAnimator.Rebind();
                externalAnimator.Update(0f);
            }

            externalHips = externalAnimator.GetBoneTransform(HumanBodyBones.Hips);
            if (externalHips != null)
                externalHipsDefaultLocalPosition = externalHips.localPosition;
            externalLeftFoot = externalAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            externalRightFoot = externalAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            externalLeftToes = externalAnimator.GetBoneTransform(HumanBodyBones.LeftToes);
            externalRightToes = externalAnimator.GetBoneTransform(HumanBodyBones.RightToes);
        }

        if (externalVisualRoot != null)
        {
            externalVisualRoot.gameObject.SetActive(true);
            if (lockExternalVisualTransform)
            {
                externalVisualRoot.localPosition = externalVisualLocalPosition;
                externalVisualRoot.localEulerAngles = externalVisualLocalEuler;
                externalVisualRoot.localScale = externalVisualLocalScale;
            }

            CaptureExternalVisualBasePosition();
        }

        FitControllerToExternalVisual();
        GroundExternalVisualToController();
        FitControllerToExternalVisual();
        StabilizeExternalMotionDrift();
    }

    private static void PruneTripoVisualCollidersAtRuntime()
    {
        GameObject tripoRoot = GameObject.Find("Tripo Quality Pass");
        if (tripoRoot == null)
            return;

        Collider[] colliders = tripoRoot.GetComponentsInChildren<Collider>(true);
        for (int i = colliders.Length - 1; i >= 0; i--)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger)
                continue;

            GameObject colliderObject = collider.gameObject;
            if (!colliderObject.name.Contains("Gameplay Collider"))
                continue;

            if (Application.isPlaying)
                Object.Destroy(colliderObject);
            else
                Object.DestroyImmediate(colliderObject);
        }
    }

    private static void FitArchiveGameplayCollider(BoxCollider box)
    {
        Vector3 size = box.size;
        size.x = Mathf.Min(size.x, 9.8f);
        size.z = Mathf.Min(size.z, 8.2f);
        box.size = size;
        box.center = Vector3.zero;
    }

    private void Update()
    {
        // UI 패널이 열려 있는 동안에는 이동/애니메이션/단축키 입력을 모두 정지시킨다.
        if (UIManager3D.Instance != null && UIManager3D.Instance.IsGameplayInputBlocked())
        {
            ApplyIdleWhileBlocked();
            return;
        }

        Move();
        if (useExternalHumanoidModel)
            UpdateExternalAnimator();
        else
            UpdateAnimation();

        // Tab은 아카이브 열기 전용. 아카이브가 이미 열려 있으면 ToggleArchive가 닫아준다.
        if (Input.GetKeyDown(KeyCode.Tab) && UIManager3D.Instance != null)
            UIManager3D.Instance.ToggleArchive();

    }

    // UI 차단 중에도 중력은 계속 작용시켜 캐릭터가 공중에 멈춰 있지 않도록 한다.
    private void LateUpdate()
    {
        if (useExternalHumanoidModel)
        {
            GroundExternalVisualToController();
            StabilizeExternalMotionDrift();
        }
    }

    private void StabilizeExternalMotionDrift()
    {
        if (lockExternalVisualTransform && externalVisualRoot != null)
        {
            externalVisualRoot.localPosition = externalVisualLocalPosition;
            externalVisualRoot.localEulerAngles = externalVisualLocalEuler;
            externalVisualRoot.localScale = externalVisualLocalScale;
        }

        if (!stabilizeExternalClipRootMotion || externalHips == null)
            return;

        Vector3 hipsPosition = externalHips.localPosition;
        hipsPosition.x = externalHipsDefaultLocalPosition.x;
        hipsPosition.z = externalHipsDefaultLocalPosition.z;
        externalHips.localPosition = hipsPosition;
    }

    private void GroundExternalVisualToController()
    {
        if (!autoGroundExternalVisual || externalVisualRoot == null || controller == null)
            return;

        CaptureExternalVisualBasePosition();

        float visualGroundY;
        if (!TryGetExternalFootGroundY(out visualGroundY) && !TryGetExternalRendererGroundY(out visualGroundY))
            return;

        Vector3 controllerCenterWorld = transform.TransformPoint(controller.center);
        float controllerBottomY = controllerCenterWorld.y - controller.height * 0.5f + externalVisualGroundPadding;
        float yDelta = controllerBottomY - visualGroundY;
        if (Mathf.Abs(yDelta) < 0.003f)
            return;

        Vector3 localDelta = externalVisualRoot.parent != null
            ? externalVisualRoot.parent.InverseTransformVector(Vector3.up * yDelta)
            : Vector3.up * yDelta;
        float clampRange = Mathf.Max(0f, visualGroundClampRange);
        float targetY = externalVisualLocalPosition.y + localDelta.y;
        if (clampRange > 0f)
            targetY = Mathf.Clamp(targetY, externalVisualBaseLocalPosition.y - clampRange, externalVisualBaseLocalPosition.y + clampRange);

        externalVisualLocalPosition = new Vector3(externalVisualLocalPosition.x, targetY, externalVisualLocalPosition.z);
        externalVisualRoot.localPosition = externalVisualLocalPosition;
        RefreshExternalVisualLocalBounds();
    }

    private void CaptureExternalVisualBasePosition()
    {
        if (hasExternalVisualBaseLocalPosition)
            return;

        externalVisualBaseLocalPosition = externalVisualLocalPosition;
        hasExternalVisualBaseLocalPosition = true;
    }

    private bool TryGetExternalFootGroundY(out float groundY)
    {
        groundY = float.PositiveInfinity;
        bool found = false;
        AddFootGroundCandidate(externalLeftFoot, ref groundY, ref found);
        AddFootGroundCandidate(externalRightFoot, ref groundY, ref found);
        AddFootGroundCandidate(externalLeftToes, ref groundY, ref found);
        AddFootGroundCandidate(externalRightToes, ref groundY, ref found);

        if (!found)
            return false;

        groundY -= externalFootGroundOffset;
        return true;
    }

    private static void AddFootGroundCandidate(Transform bone, ref float groundY, ref bool found)
    {
        if (bone == null)
            return;

        groundY = Mathf.Min(groundY, bone.position.y);
        found = true;
    }

    private bool TryGetExternalRendererGroundY(out float groundY)
    {
        groundY = 0f;
        Renderer[] renderers = externalVisualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return false;

        bool hasBounds = false;
        Bounds visualBounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                visualBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                visualBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            return false;

        groundY = visualBounds.min.y;
        return true;
    }

    private void FitControllerToExternalVisual()
    {
        if (!autoFitControllerToExternalVisual || controller == null || externalVisualRoot == null)
            return;

        Bounds localBounds;
        string fitSource;
        if (!TryGetControllerFitLocalBounds(out localBounds, out fitSource))
            return;

        float height = Mathf.Clamp(
            localBounds.size.y + Mathf.Max(0f, controllerFitHeightPadding),
            Mathf.Max(0.2f, controllerFitMinHeight),
            Mathf.Max(controllerFitMinHeight, controllerFitMaxHeight));
        float horizontalExtent = Mathf.Max(localBounds.extents.x, localBounds.extents.z);
        float radius = horizontalExtent * Mathf.Max(0.1f, controllerFitRadiusScale) + Mathf.Max(0f, controllerFitRadiusPadding);
        radius = Mathf.Clamp(radius, Mathf.Max(0.05f, controllerFitMinRadius), Mathf.Max(controllerFitMinRadius, controllerFitMaxRadius));
        radius = Mathf.Min(radius, height * 0.48f);

        float bottom = localBounds.min.y - Mathf.Max(0f, controllerFitBottomPadding);
        Vector3 center = new Vector3(localBounds.center.x, bottom + height * 0.5f, localBounds.center.z);

        controller.height = height;
        controller.radius = radius;
        controller.center = center;

        lastExternalVisualLocalBounds = localBounds;
        hasExternalVisualLocalBounds = true;

        groundProbeRadius = Mathf.Min(groundProbeRadius, Mathf.Max(0.05f, radius * 0.82f));

        if (logControllerFitDebug)
        {
            float controllerBottom = center.y - height * 0.5f;
            Debug.Log(
                "[MR3D] Controller fit: height=" + height.ToString("0.###") +
                " radius=" + radius.ToString("0.###") +
                " center=" + center.ToString("F3") +
                " controllerBottom=" + controllerBottom.ToString("0.###") +
                " visualMinY=" + localBounds.min.y.ToString("0.###") +
                " visualHeight=" + localBounds.size.y.ToString("0.###") +
                " source=" + fitSource,
                this);
        }
    }

    private void RefreshExternalVisualLocalBounds()
    {
        Bounds localBounds;
        string fitSource;
        if (TryGetControllerFitLocalBounds(out localBounds, out fitSource))
        {
            lastExternalVisualLocalBounds = localBounds;
            hasExternalVisualLocalBounds = true;
        }
    }

    private bool TryGetControllerFitLocalBounds(out Bounds localBounds, out string source)
    {
        if (preferHumanoidBoneControllerFit && TryGetHumanoidControllerLocalBounds(out localBounds))
        {
            source = "humanoid bones";
            return true;
        }

        source = "renderer bounds";
        return TryGetExternalVisualLocalBounds(out localBounds);
    }

    private bool TryGetHumanoidControllerLocalBounds(out Bounds localBounds)
    {
        localBounds = default;
        if (externalAnimator == null || !externalAnimator.isHuman)
            return false;

        Transform head = externalAnimator.GetBoneTransform(HumanBodyBones.Head);
        Transform leftFoot = externalLeftFoot != null ? externalLeftFoot : externalAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = externalRightFoot != null ? externalRightFoot : externalAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
        Transform leftToes = externalLeftToes != null ? externalLeftToes : externalAnimator.GetBoneTransform(HumanBodyBones.LeftToes);
        Transform rightToes = externalRightToes != null ? externalRightToes : externalAnimator.GetBoneTransform(HumanBodyBones.RightToes);
        if (head == null || (leftFoot == null && rightFoot == null && leftToes == null && rightToes == null))
            return false;

        bool found = false;
        Bounds boneBounds = default;
        HumanBodyBones[] fitBones =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,
            HumanBodyBones.LeftShoulder,
            HumanBodyBones.RightShoulder,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightFoot,
            HumanBodyBones.LeftToes,
            HumanBodyBones.RightToes
        };

        for (int i = 0; i < fitBones.Length; i++)
        {
            Transform bone = externalAnimator.GetBoneTransform(fitBones[i]);
            if (bone == null)
                continue;

            Vector3 local = transform.InverseTransformPoint(bone.position);
            if (!found)
            {
                boneBounds = new Bounds(local, Vector3.zero);
                found = true;
            }
            else
            {
                boneBounds.Encapsulate(local);
            }
        }

        if (!found)
            return false;

        float bottom = float.PositiveInfinity;
        AddBoneBottomCandidate(leftFoot, ref bottom);
        AddBoneBottomCandidate(rightFoot, ref bottom);
        AddBoneBottomCandidate(leftToes, ref bottom);
        AddBoneBottomCandidate(rightToes, ref bottom);
        if (float.IsPositiveInfinity(bottom))
            return false;

        float top = transform.InverseTransformPoint(head.position).y + Mathf.Max(0f, controllerFitHeadPadding);
        if (top <= bottom + 0.2f)
            return false;

        Vector3 center = boneBounds.center;
        float halfWidth = Mathf.Max(Mathf.Abs(boneBounds.min.x - center.x), Mathf.Abs(boneBounds.max.x - center.x));
        float halfDepth = Mathf.Max(Mathf.Abs(boneBounds.min.z - center.z), Mathf.Abs(boneBounds.max.z - center.z));
        float horizontalExtent = Mathf.Max(halfWidth, halfDepth, 0.22f);

        Vector3 size = new Vector3(horizontalExtent * 2f, top - bottom, horizontalExtent * 2f);
        localBounds = new Bounds(new Vector3(center.x, (top + bottom) * 0.5f, center.z), size);
        return true;
    }

    private void AddBoneBottomCandidate(Transform bone, ref float bottom)
    {
        if (bone == null)
            return;

        float y = transform.InverseTransformPoint(bone.position).y - Mathf.Max(0f, externalFootGroundOffset);
        bottom = Mathf.Min(bottom, y);
    }

    private bool TryGetExternalVisualLocalBounds(out Bounds localBounds)
    {
        localBounds = default;
        if (externalVisualRoot == null)
            return false;

        Renderer[] renderers = externalVisualRoot.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            Bounds worldBounds = renderer.bounds;
            EncapsulateWorldBoundsInPlayerLocal(worldBounds, ref localBounds, ref found);
        }

        return found;
    }

    private void EncapsulateWorldBoundsInPlayerLocal(Bounds worldBounds, ref Bounds localBounds, ref bool found)
    {
        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;
        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                for (int z = 0; z <= 1; z++)
                {
                    Vector3 corner = new Vector3(
                        x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y,
                        z == 0 ? min.z : max.z);
                    Vector3 local = transform.InverseTransformPoint(corner);
                    if (!found)
                    {
                        localBounds = new Bounds(local, Vector3.zero);
                        found = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(local);
                    }
                }
            }
        }
    }

    private void ApplyIdleWhileBlocked()
    {
        if (controller == null)
            return;

        UpdateGroundProbe();
        if (IsStableGrounded() && verticalVelocity < 0f)
            verticalVelocity = groundStickVelocity;

        verticalVelocity += gravity * Time.deltaTime;
        CollisionFlags collisionFlags = controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
        UpdateGroundProbe();
        ApplyGroundSnap(collisionFlags);

        IsMoving = false;
        isRunning = false;
        currentPlanarVelocity = Vector3.zero;
        lastPlanarMove = Vector3.zero;
        moveBlend = Mathf.Lerp(moveBlend, 0f, Time.deltaTime * animationSmooth);
        localMoveForward = Mathf.Lerp(localMoveForward, 0f, Time.deltaTime * animationSmooth);
        localMoveSide = Mathf.Lerp(localMoveSide, 0f, Time.deltaTime * animationSmooth);

        // 외부 Animator도 Idle로 수렴시켜 UI 위에서 캐릭터가 계속 걷는 듯한 잔여 모션 방지.
        if (useExternalHumanoidModel)
            UpdateExternalAnimator();
    }

    // Animator 파라미터를 매 프레임 갱신.
    // MoveSpeed: 0(Idle) ↔ 0.5(Walk) ↔ 1.0(Run) — Locomotion BlendTree의 1D blend 입력.
    private void UpdateExternalAnimator()
    {
        if (externalAnimator == null)
            return;

        float targetSpeed = IsMoving ? moveBlend : 0f;
        float dampTime = 1f / Mathf.Max(1f, animatorParameterSmooth);
        externalAnimator.speed = 1f;

        externalAnimator.SetFloat(AnimMoveSpeed, targetSpeed, dampTime, Time.deltaTime);
        externalAnimator.SetFloat(AnimForward, localMoveForward, dampTime, Time.deltaTime);
        externalAnimator.SetFloat(AnimSide, localMoveSide, dampTime, Time.deltaTime);
        externalAnimator.SetBool(AnimIsMoving, IsMoving);
        externalAnimator.SetBool(AnimIsRunning, isRunning && IsMoving);
        externalAnimator.SetBool(AnimIsGrounded, controller != null && controller.isGrounded);
        externalAnimator.SetFloat(AnimVerticalSpeed, verticalVelocity);
    }

    // 점프 액션 추가 시 외부에서 호출하면 Animator 점프 진입. 현재는 미사용.
    public void TriggerJump()
    {
        if (useExternalHumanoidModel && externalAnimator != null)
            externalAnimator.SetTrigger(AnimJumpTrigger);
    }

    private void Move()
    {
        if (controller == null)
            return;

        UpdateGroundProbe();
        bool stableGrounded = IsStableGrounded();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 rawInput = new Vector2(horizontal, vertical);
        float inputMagnitude = Mathf.Clamp01(rawInput.magnitude);
        Vector2 moveInput = inputMagnitude >= inputDeadZone ? rawInput.normalized : Vector2.zero;
        Vector3 moveDirection = Vector3.zero;

        if (moveInput.sqrMagnitude > 0.001f)
        {
            Vector3 cameraForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
            moveDirection.Normalize();

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        if (stableGrounded && verticalVelocity < 0f)
            verticalVelocity = groundStickVelocity;

        bool wantsMove = moveDirection.sqrMagnitude > 0.001f;
        isRunning = Input.GetKey(KeyCode.LeftShift) && wantsMove;
        float targetSpeed = (isRunning ? runSpeed : walkSpeed) * inputMagnitude;
        Vector3 targetPlanarVelocity = wantsMove ? moveDirection * targetSpeed : Vector3.zero;
        float velocityChangeRate = targetPlanarVelocity.sqrMagnitude > currentPlanarVelocity.sqrMagnitude ? acceleration : deceleration;
        if (!stableGrounded)
            velocityChangeRate *= airControl;
        currentPlanarVelocity = Vector3.MoveTowards(currentPlanarVelocity, targetPlanarVelocity, velocityChangeRate * Time.deltaTime);

        if (stableGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            skipGroundSnapUntil = Time.time + Mathf.Max(0f, jumpGroundProbeGraceTime);
            TriggerJump();
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = currentPlanarVelocity;
        finalMove.y = verticalVelocity;

        CollisionFlags collisionFlags = controller.Move(finalMove * Time.deltaTime);
        if ((collisionFlags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            verticalVelocity = groundStickVelocity;
        UpdateGroundProbe();
        ApplyGroundSnap(collisionFlags);

        float planarSpeed = new Vector3(currentPlanarVelocity.x, 0f, currentPlanarVelocity.z).magnitude;
        float targetMoveBlend = runSpeed > 0.001f ? Mathf.Clamp01(planarSpeed / runSpeed) : 0f;
        float blendRate = (wantsMove ? acceleration : deceleration) / Mathf.Max(1f, runSpeed);
        moveBlend = Mathf.MoveTowards(moveBlend, targetMoveBlend, blendRate * Time.deltaTime);

        Vector3 localMove = planarSpeed > 0.01f ? transform.InverseTransformDirection(currentPlanarVelocity.normalized) : Vector3.zero;
        localMoveForward = Mathf.Lerp(localMoveForward, Mathf.Clamp(localMove.z, -1f, 1f), Time.deltaTime * animationSmooth);
        localMoveSide = Mathf.Lerp(localMoveSide, Mathf.Clamp(localMove.x, -1f, 1f), Time.deltaTime * animationSmooth);

        lastPlanarMove = currentPlanarVelocity;
        IsMoving = planarSpeed > 0.05f;
    }

    private void UpdateGroundProbe()
    {
        groundProbeHasGround = false;
        groundProbeGap = float.PositiveInfinity;
        groundProbeNormal = Vector3.up;

        if (!enableGroundProbe || controller == null)
            return;

        float radius = Mathf.Clamp(groundProbeRadius, 0.04f, Mathf.Max(0.05f, controller.radius * 0.95f));
        float lift = Mathf.Max(0.04f, controller.skinWidth + 0.025f);
        float probeDistance = Mathf.Max(0.05f, groundProbeDistance);
        Vector3 centerWorld = transform.TransformPoint(controller.center);
        Vector3 bottomSphereCenter = centerWorld - Vector3.up * Mathf.Max(0f, controller.height * 0.5f - controller.radius);
        Vector3 origin = bottomSphereCenter + Vector3.up * lift;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            radius,
            Vector3.down,
            groundProbeHits,
            probeDistance + lift,
            groundProbeMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.PositiveInfinity;
        Vector3 bestNormal = Vector3.up;
        float maxSlope = Mathf.Min(Mathf.Max(1f, groundProbeSlopeLimit), controller.slopeLimit + 1.5f);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundProbeHits[i];
            if (hit.collider == null || ShouldIgnoreGroundProbeHit(hit.collider.transform))
                continue;

            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlope)
                continue;

            float gap = Mathf.Max(0f, hit.distance - lift);
            if (gap < bestDistance)
            {
                bestDistance = gap;
                bestNormal = hit.normal;
            }
        }

        if (float.IsPositiveInfinity(bestDistance))
            return;

        groundProbeHasGround = true;
        groundProbeGap = bestDistance;
        groundProbeNormal = bestNormal;
    }

    private bool IsStableGrounded()
    {
        if (controller != null && controller.isGrounded)
            return true;

        if (!enableGroundProbe || !groundProbeHasGround || Time.time < skipGroundSnapUntil)
            return false;

        return groundProbeGap <= Mathf.Min(groundSnapMaxDistance, 0.08f) && Vector3.Dot(groundProbeNormal, Vector3.up) > 0.45f;
    }

    private void ApplyGroundSnap(CollisionFlags collisionFlags)
    {
        if (!enableGroundProbe || controller == null || Time.time < skipGroundSnapUntil)
            return;
        if ((collisionFlags & CollisionFlags.Below) != 0)
            return;
        if (!groundProbeHasGround || verticalVelocity > 0f)
            return;

        float maxDistance = Mathf.Max(0f, groundSnapMaxDistance);
        if (groundProbeGap <= 0.002f || groundProbeGap > maxDistance)
            return;

        float snapDistance = Mathf.Min(groundProbeGap, Mathf.Max(0f, groundSnapSpeed) * Time.deltaTime);
        if (snapDistance <= 0f)
            return;

        CollisionFlags snapFlags = controller.Move(Vector3.down * snapDistance);
        if ((snapFlags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            verticalVelocity = groundStickVelocity;
    }

    private bool ShouldIgnoreGroundProbeHit(Transform hitTransform)
    {
        if (hitTransform == null)
            return true;

        if (hitTransform == transform || hitTransform.IsChildOf(transform) || transform.IsChildOf(hitTransform))
            return true;

        int playerVisualLayer = LayerMask.NameToLayer("MR3D_PlayerVisual");
        return playerVisualLayer >= 0 && hitTransform.gameObject.layer == playerVisualLayer;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawControllerFitDebugGizmos)
            return;

        CharacterController gizmoController = controller != null ? controller : GetComponent<CharacterController>();
        if (gizmoController != null)
        {
            Vector3 centerWorld = transform.TransformPoint(gizmoController.center);
            float bottomY = centerWorld.y - gizmoController.height * 0.5f;
            float topY = centerWorld.y + gizmoController.height * 0.5f;

            Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.85f);
            Gizmos.DrawLine(new Vector3(centerWorld.x - 0.45f, bottomY, centerWorld.z), new Vector3(centerWorld.x + 0.45f, bottomY, centerWorld.z));
            Gizmos.DrawLine(new Vector3(centerWorld.x, bottomY, centerWorld.z - 0.45f), new Vector3(centerWorld.x, bottomY, centerWorld.z + 0.45f));
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.55f);
            Gizmos.DrawLine(new Vector3(centerWorld.x, bottomY, centerWorld.z), new Vector3(centerWorld.x, topY, centerWorld.z));
        }

        if (hasExternalVisualLocalBounds)
        {
            Gizmos.color = new Color(1f, 0.78f, 0.18f, 0.55f);
            Vector3 boundsCenter = transform.TransformPoint(lastExternalVisualLocalBounds.center);
            Vector3 boundsSize = Vector3.Scale(lastExternalVisualLocalBounds.size, transform.lossyScale);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }
    }
#endif

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
